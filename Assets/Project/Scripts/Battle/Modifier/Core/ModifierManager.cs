using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Card; // DeckSystem 참조용

namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 등록된 Modifier들의 수명을 관리하고 지정된 타이밍에 효과 파이프라인을 실행하는 
    /// 파이프라인(Interceptor) 매니저입니다.
    /// </summary>
    /* * === 기술적 부채 ===
     * 1. Instance 사용을 제한하는 것 (당장의 구현을 위해 임시 static 사용).
     * 2. 스레딩을 이용해서, 서로 다른 Modifier 효과 발동 시 그것들을 병렬로 처리하게 해서 Delegate 처리 최적화 요망.
     * 3. Invoke() 대신 GetInvocationList() 사용 요망.
     * 4. 족보 시스템 베이스 수정.
     * 5. 악세서리와 조커를 Item으로 묶어서 관리.
     * 6. 기존에 최적화를 위해 존재했던 '상태 변경 감지 플래그' DirtyBit를 제거함.
     * 7. 지금은 내부에서 switch가 
     */
    public class ModifierManager : MonoBehaviour
    {
        // 개발을 위해 임시로 인스턴스를 전역 변수화 하여 어디서든 참조하여 API 호출이 가능하도록 함
        public static ModifierManager Instance { get; private set; }

        /// <summary>
        /// 현재 활성화된 모든 모디파이어(아이템/버프) 목록.
        /// (※ BattleModifier는 조건과 효과가 조립된 Composition 형태의 객체입니다)
        /// </summary>
        private readonly List<BattleModifier> _activeModifiers = new List<BattleModifier>();

        /// <summary>
        /// 효과별 독립된 파이프라인 (Delegate)
        /// Item이나 버프, 기믹 등 모든 Modifier 효과들은 자신의 효과에 맞춰 여기에 추가한다.
        /// </summary>
        private Action<ModifierContext> _onStrengthModifiers;
        private Action<ModifierContext> _onDamageModifiers;
        private Action<ModifierContext> _onDrawCountModifiers;
        private Action<ModifierContext> _onMaxRerollsModifiers;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            // 씬 전환 시 정적 인스턴스 초기화 (DonDestroy가 아님)
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #region === 목록 관리 (Add / Remove) ===

        /// <summary>
        /// 새로운 모디파이어(버프/디버프)를 파이프라인에 등록합니다.
        /// </summary>
        public void AddModifier(BattleModifier modifier)
        {
            if (modifier == null)
            {
                Debug.Log("[ModifierManager] 전달되어 온 modifier가 Null임");
                return;
            }

            _activeModifiers.Add(modifier);
            Action<ModifierContext> action = (ctx) =>
            {
                if (modifier.IsActive(ctx))
                {
                    modifier.ApplyToContext(ctx);
                }
            };

            modifier.AttachedAction = action;

            switch (modifier.TargetType)
            {
                case ModifierValueType.Strength: 
                    _onStrengthModifiers += action; break;
                case ModifierValueType.Damage: 
                    _onDamageModifiers += action; break;
                case ModifierValueType.DrawCount: 
                    _onDrawCountModifiers += action; break;
                case ModifierValueType.MaxRerolls: 
                    _onMaxRerollsModifiers += action; break;
            }

            Debug.Log($"[ModifierManager] 버프 등록 완료: {modifier.Id} (대상: {modifier.TargetType})");
        }

        /// <summary>
        /// 특정 모디파이어를 파이프라인(delegate)에서 제거합니다.
        /// (예: 장신구 해제, 수명 만료 등)
        /// 제거될 파이프라인을 결정하는 것은 Manager 내부에서이지만, 그걸을 명확하게 구분하도록 명시할 책임은 
        /// 생성되는 아이템에 있다.(즉, 생성될 때부터 명확하게 이 내부구조를 알고 명시해야 함)
        /// </summary>
        public void RemoveModifier(BattleModifier modifier)
        {
            if (_activeModifiers.Remove(modifier) && modifier.AttachedAction != null)
            {
                switch (modifier.TargetType)
                {
                    case ModifierValueType.Strength: 
                        _onStrengthModifiers -= modifier.AttachedAction; break;
                    case ModifierValueType.Damage: 
                        _onDamageModifiers -= modifier.AttachedAction; break;
                    case ModifierValueType.DrawCount: 
                        _onDrawCountModifiers -= modifier.AttachedAction; break;
                    case ModifierValueType.MaxRerolls: 
                        _onMaxRerollsModifiers -= modifier.AttachedAction; break;
                }
            }
        }

        #endregion

        #region === 턴 결산 로직 ===

        /// <summary>
        /// 턴 종료 시 호출되어 (기존 DeckSystem이 하던 일 대체)
        /// 버프들의 수명(Turn)을 깎고, 만료된 항목을 깔끔하게 치웁니다.
        /// </summary>
        public void TickModifiers()
        {
            // 리스트 중간에서 삭제(RemoveAt)가 일어나므로 반드시 역순 순회해야 합니다.
            for (int i = _activeModifiers.Count - 1; i >= 0; i--)
            {
                if (_activeModifiers[i].TickTurn())
                {
                    RemoveModifier(_activeModifiers[i]);
                }
            }
        }

        #endregion

        #region === 파이프라인 실행 메서드 ===

       // 호출 시 공용 바구니(Context)의 수치를 초기화한 뒤 모든 Modifier를 통과시킴
        public void ProcessStrength(ModifierContext context)
        {
            context.Reset();
            _onStrengthModifiers?.Invoke(context);
        }

        public void ProcessDamage(ModifierContext context)
        {
            context.Reset();
            _onDamageModifiers?.Invoke(context);
        }
        
        public void ProcessDrawCount(ModifierContext context)
        {
            context.Reset();
            _onDrawCountModifiers?.Invoke(context);
        }
        
        public void ProcessMaxRerolls(ModifierContext context)
        {
            context.Reset();
            _onMaxRerollsModifiers?.Invoke(context);
        }

        #endregion

        
    }
}