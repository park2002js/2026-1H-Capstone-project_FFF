using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Card; // DeckSystem 참조용

namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 모든 버프와 아이템 효과를 중앙 통제하는 파이프라인(Interceptor) 매니저입니다.
    /// 
    /// ── 핵심 책임 ──
    /// 1. 활성화된 ItemModifier 목록 관리.
    /// 2. (캐싱) 리롤/드로우 수치 변화 감지 시 DeckSystem에 최종 합산 값 강제 주입(Push).
    /// 3. (실시간) 공격력/데미지 등 코어 시스템의 수치 연산 요청 시 파이프라인 통과.
    /// </summary>
    public class ModifierManager : MonoBehaviour
    {
        [Header("=== 시스템 참조 ===")]
        [Tooltip("계산된 보너스 수치를 수동으로 주입(Push)할 대상")]
        [SerializeField] private DeckSystem _deckSystem;

        /// <summary>
        /// 현재 활성화된 모든 모디파이어(아이템/버프) 목록.
        /// (※ ItemModifier는 조건과 효과가 조립된 Composition 형태의 객체입니다)
        /// </summary>
        private readonly List<ItemModifier> _activeModifiers = new List<ItemModifier>();

        /// <summary>
        /// 상태 변경 감지 플래그 (Dirty Bit).
        /// true일 때만 캐싱 대상(리롤, 드로우)을 재계산하여 최적화합니다.
        /// </summary>
        private bool _isDirty = false;

        /// <summary>
        /// 어댑터 매핑 표: 
        /// 파이프라인에서 가로챈 값(ValueType)을 DeckSystem의 어떤 함수(Action)에 주입할지 정의합니다.
        /// 향후 새로운 캐싱 스탯이 생겨도 이 표에 한 줄만 추가하면 if-else 지옥을 피할 수 있습니다.
        /// </summary>
        private Dictionary<ModifierValueType, Action<int>> _pushTargets;

        private void Awake()
        {
            // 초기화 시 파이프라인의 출구를 DeckSystem의 Setter 함수들과 연결해 둡니다.
            // DeckSystem은 Modifier를 모르고, 매니저만 연결선을 쥐고 통제합니다.
            _pushTargets = new Dictionary<ModifierValueType, Action<int>>
            {
                { ModifierValueType.MaxRerolls, _deckSystem.SetBonusMaxRerolls },
                { ModifierValueType.DrawCount, _deckSystem.SetBonusDrawCount }
                // [확장 예시] { ModifierValueType.MaxSelect, _deckSystem.SetBonusMaxSelect }
            };
        }

        #region === 목록 관리 (Add / Remove) ===

        /// <summary>
        /// 새로운 모디파이어(버프/디버프)를 파이프라인에 등록합니다.
        /// </summary>
        public void AddModifier(ItemModifier modifier)
        {
            if (modifier == null) return;

            _activeModifiers.Add(modifier);
            SetDirty(); // 리스트에 변화가 생겼으므로 즉시 갱신 예약

            Debug.Log($"[ModifierManager] 버프 등록 완료: {modifier.Id} (대상: {modifier.ValueType})");
        }

        /// <summary>
        /// 특정 모디파이어를 파이프라인에서 제거합니다.
        /// (예: 장신구 해제, 수명 만료 등)
        /// </summary>
        public void RemoveModifier(ItemModifier modifier)
        {
            if (_activeModifiers.Remove(modifier))
            {
                SetDirty(); // 리스트에서 빠졌으므로 갱신 예약
                Debug.Log($"[ModifierManager] 버프 제거됨: {modifier.Id}");
            }
        }

        /// <summary>
        /// 내부 데이터 변화를 마킹하고, 즉시 동기화를 시도합니다.
        /// </summary>
        private void SetDirty()
        {
            _isDirty = true;
            SyncValues(); 
        }

        #endregion

        #region === 캐싱 및 Push 로직 (Dirty Bit 최적화) ===

        /// <summary>
        /// 턴 내내 고정되는 값(리롤, 드로우)들을 모아서 DeckSystem과 동기화(Push)합니다.
        /// 
        /// [ 아키텍처 주의사항 (향후 확장 계획)]
        /// 현재 이 파이프라인은 '덧셈/뺄셈(단순 누적)' 효과에만 완벽히 동작하도록 설계되어 있습니다.
        /// 
        /// 향후 기획에서 "수치 2배 폭증(곱산)"이나 "무조건 5로 고정(덮어쓰기)" 같은 효과가 추가된다면,
        /// 획득 순서에 따라 결과가 뒤틀리는 문제가 발생합니다.
        /// 
        /// TODO: 복잡한 연산이 기획될 경우, 아래 과정을 거치도록 리팩토링 필요
        /// 1. ItemModifier 내부에 ModifierPhase(BaseAdd, Multiplier, Override) Enum 추가.
        /// 2. 파이프라인 순회 직전, _activeModifiers를 Phase 순으로 정렬(Sort)하는 로직 추가.
        /// </summary>
        ///
        /// 나중에 추가될 우선순위 단계
        // public enum ModifierPhase 
        // {
        //     BaseAdd,     // 1단계: 단순 합산 (+5)
        //     Multiplier,  // 2단계: 배율 곱산 (*2)
        //     Override     // 3단계: 덮어쓰기 / 무시 (고정치)
        // }
        private void SyncValues()
        {
            if (!_isDirty) return;
            if (_deckSystem == null) return;

            // 1. 계산할 값들의 최종 합산을 담아둘 장부
            var Totals = new Dictionary<ModifierValueType, int>();

            // 2. 파이프라인 순회 (if-else 분기 없이 매핑 표 기반으로 일괄 처리)
            foreach (var mod in _activeModifiers)
            {
                // 우리가 캐싱하기로 약속한 타입(매핑 표에 존재)이고, 발동 조건이 맞는다면
                if (_pushTargets.ContainsKey(mod.ValueType) && mod.IsActive(context: null))
                {
                    if (!Totals.ContainsKey(mod.ValueType))
                        Totals[mod.ValueType] = 0; // 값이 없으면 0으로 초기화

                    // [현재 구조] 획득 순서대로 단순 누적 연산 진행
                    Totals[mod.ValueType] = mod.ApplyEffect(Totals[mod.ValueType]);
                }
            }

            // 3. 연산이 끝난 최종 결과들을 각자의 타겟 함수로 발사 (일괄 Push)
            foreach (var kvp in Totals)
            {
                _pushTargets[kvp.Key].Invoke(kvp.Value); 
            }

            // 4. 리스트에서 버프가 빠져서 아예 누적 연산이 돌지 않은 스탯은 0으로 원상복구
            foreach (var type in _pushTargets.Keys)
            {
                if (!Totals.ContainsKey(type))
                    _pushTargets[type].Invoke(0);
            }

            _isDirty = false; // 동기화 완료
        }

        #endregion

        #region === 실시간 파이프라인 (Interceptor) ===

        /// <summary>
        /// 기본값을 파이프라인에 통과시켜 모든 버프가 적용된 '최종 가공 값'을 반환합니다.
        /// (CombatCalculator 등 코어 시스템이 데미지/공격력을 실시간으로 연산할 때 호출합니다)
        /// </summary>
        /// <param name="type">가로챌 값의 종류 (Strength, Damage 등)</param>
        /// <param name="baseValue">아이템이 적용되기 전의 쌩얼(순수) 기본값</param>
        /// <param name="context">조건 판별에 필요한 문맥 정보 (예: SeotdaResult)</param>
        /// <returns>모든 효과가 연쇄 적용된 최종값</returns>
        public int ProcessValue(ModifierValueType type, int baseValue, ModifierContext context = null)
        {
            int currentValue = baseValue;

            // TODO: (위쪽 SyncValues 주석 참조) 곱산/덮어쓰기 로직 도입 시, 
            // 여기서도 _activeModifiers를 Phase 순으로 우선 정렬해야 합니다.

            foreach (var mod in _activeModifiers)
            {
                // 타입이 일치하고, 발동 조건(Condition)을 충족했다면 가로채기 발생!
                if (mod.ValueType == type && mod.IsActive(context))
                {
                    int newValue = mod.ApplyEffect(currentValue);
                    
                    // 장부 기록: 어떤 아이템이 수치를 어떻게 바꿨는지 추적
                    Debug.Log($"[Modifier Log] 파이프라인({type}) 가로채기: [{mod.Id}] 발동! ({currentValue} -> {newValue})");
                    
                    currentValue = newValue;
                }
            }

            return currentValue;
        }

        #endregion

        #region === 턴 결산 로직 ===

        /// <summary>
        /// 턴 종료 시 호출되어 (기존 DeckSystem이 하던 일 대체)
        /// 버프들의 수명(Turn)을 깎고, 만료된 항목을 깔끔하게 치웁니다.
        /// </summary>
        public void TickModifiers()
        {
            bool hasExpired = false;

            // 리스트 중간에서 삭제(RemoveAt)가 일어나므로 반드시 역순 순회해야 합니다.
            for (int i = _activeModifiers.Count - 1; i >= 0; i--)
            {
                // TickTurn() 내에서 턴을 깎고, 수명이 0 이하가 되면 true를 반환함
                if (_activeModifiers[i].TickTurn())
                {
                    Debug.Log($"[ModifierManager] 버프 수명 만료 (제거됨): {_activeModifiers[i].Id}");
                    _activeModifiers.RemoveAt(i);
                    hasExpired = true;
                }
            }

            // 만료되어 사라진 버프가 하나라도 있다면, 
            // 깎인 스탯을 DeckSystem에 다시 동기화하기 위해 Dirty Bit를 켭니다.
            if (hasExpired)
            {
                SetDirty(); 
            }
        }

        #endregion
    }
}