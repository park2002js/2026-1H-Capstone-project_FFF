using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.FSM;
using FFF.Data;
using FFF.Core.Events;

namespace FFF.Battle.Item.Joker
{
    /// <summary>
    /// 조커 카드의 보유, 사용, 효과 집계를 관리한다.
    /// 
    /// ── 핵심 책임 ──
    /// 1. 플레이어가 보유한 조커 카드 목록 관리.
    /// 2. 플레이어가 조커를 사용하면 → 해당 조커의 효과를 적용하고 목록에서 제거.
    /// 3. 데미지 배율 등 "현재 활성화된 효과 총합"을 getter로 제공.
    ///    → 데미지 계산 코드는 GetDamageMultiplier()만 호출하면 됨.
    ///    → 어떤 조커가 발동됐는지 알 필요 없음 (알빠노).
    /// 
    /// ── 설계 원칙 ──
    /// - BattleManager.OnTurnEnd에 구독하여 턴 종료 시 임시 효과를 자동 초기화.
    /// - 외부(데미지 계산)는 Get 함수로 결과만 가져간다.
    /// </summary>
    public class JokerManager : MonoBehaviour
    {
        [Header("=== 참조 ===")]
        [Tooltip("카드 시스템. 조커 효과 적용 대상.")]
        [SerializeField] private Card.DeckSystem _deckSystem;

        /// <summary>플레이어가 보유한 조커 목록.</summary>
        private readonly List<JokerBase> _heldJokers = new();

        /// <summary>보유 조커 목록. 읽기 전용.</summary>
        public IReadOnlyList<JokerBase> HeldJokers => _heldJokers;

        /// <summary>
        /// 현재 턴에 적용 중인 데미지 배율.
        /// 조커 효과가 이 값을 변경한다.
        /// 기본값 1.0 = 배율 없음.
        /// </summary>
        private float _damageMultiplier = 1.0f;

        /// <summary>
        /// 현재 턴에 적용 중인 데미지 배율 조건 함수.
        /// null이면 무조건 적용, 아니면 조건 충족 시에만 적용.
        /// </summary>
        private Func<SeotdaResult, bool> _damageMultiplierCondition = null;

        #region === Unity 생명주기 ===

        [Header("=== 수신할 이벤트 ===")]
        [Tooltip("BattleManager가 방송하는 TurnEnd 이벤트")]
        [SerializeField] private GameEvent _onTurnEndEvent;

        private void OnEnable()
        {
            if (_onTurnEndEvent != null)
                _onTurnEndEvent.Subscribe(ResetTurnEffects);
        }

        private void OnDisable()
        {
            if (_onTurnEndEvent != null)
                _onTurnEndEvent.Unsubscribe(ResetTurnEffects);
        }

        #endregion

        #region === 외부 호출: 조커 획득/사용 ===

        /// <summary>
        /// 조커 카드를 획득한다 (상점/보상 등에서).
        /// </summary>
        public void AddJoker(JokerBase joker)
        {
            if (joker == null) return;

            _heldJokers.Add(joker);
            Debug.Log($"[JokerManager] 조커 획득: {joker.DisplayName}");
        }

        /// <summary>
        /// 플레이어가 조커를 사용한다.
        /// TurnProceed 상태에서 카드 선택 완료 전에 호출.
        /// 사용 후 조커는 소멸되어 목록에서 제거된다.
        /// </summary>
        /// <param name="jokerIndex">사용할 조커의 인덱스</param>
        /// <returns>사용 성공 여부</returns>
        public bool UseJoker(int jokerIndex)
        {
            if (jokerIndex < 0 || jokerIndex >= _heldJokers.Count)
            {
                Debug.LogWarning($"[JokerManager] 잘못된 조커 인덱스: {jokerIndex}");
                return false;
            }

            var joker = _heldJokers[jokerIndex];

            var context = new JokerContext
            {
                DeckSystem = _deckSystem,
                JokerManager = this
            };

            bool success = joker.Use(context);

            if (success)
            {
                _heldJokers.RemoveAt(jokerIndex);
                Debug.Log($"[JokerManager] 조커 사용 완료 및 소멸: {joker.DisplayName}. 남은 조커: {_heldJokers.Count}개");
            }

            return success;
        }

        #endregion

        #region === Getter: 외부에서 효과 값 조회 (알빠노 패턴) ===

        /// <summary>
        /// 현재 턴의 데미지 배율을 반환한다.
        /// 
        /// 데미지 계산 코드는 이 함수만 호출하면 된다.
        /// 어떤 조커가 발동되었는지, 조건이 뭔지 알 필요 없다.
        /// 
        /// 사용 예:
        ///   int finalDamage = (int)(basePower * jokerManager.GetDamageMultiplier(seotdaResult));
        /// </summary>
        /// <param name="result">현재 족보 판정 결과 (조건부 배율 판단용)</param>
        /// <returns>데미지 배율. 기본 1.0.</returns>
        public float GetDamageMultiplier(SeotdaResult result)
        {
            // 조건 함수가 없으면 무조건 배율 적용
            if (_damageMultiplierCondition == null)
            {
                return _damageMultiplier;
            }

            // 조건 충족 시에만 배율 적용
            if (_damageMultiplierCondition(result))
            {
                return _damageMultiplier;
            }

            return 1.0f;
        }

        #endregion

        #region === 조커 효과가 호출하는 내부 setter ===

        /// <summary>
        /// 데미지 배율을 설정한다.
        /// 구체 조커의 Activate()에서 호출.
        /// </summary>
        /// <param name="multiplier">배율 값</param>
        /// <param name="condition">조건 함수. null이면 무조건 적용.</param>
        public void SetDamageMultiplier(float multiplier, Func<SeotdaResult, bool> condition = null)
        {
            _damageMultiplier = multiplier;
            _damageMultiplierCondition = condition;

            Debug.Log($"[JokerManager] 데미지 배율 설정: x{multiplier}");
        }

        #endregion

        #region === 턴 종료 시 임시 효과 초기화 ===

        /// <summary>
        /// 턴 종료 시 호출 (BattleManager.OnTurnEnd).
        /// 한 턴짜리 효과들을 초기화한다.
        /// </summary>
        private void ResetTurnEffects()
        {
            _damageMultiplier = 1.0f;
            _damageMultiplierCondition = null;

            Debug.Log("[JokerManager] 턴 종료 → 임시 효과 초기화");
        }

        #endregion
    }
}
