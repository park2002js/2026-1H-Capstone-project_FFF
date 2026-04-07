using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Core.Events;

namespace FFF.Battle.Card
{
    /// <summary>
    /// 카드 시스템의 진입점 (파사드).
    /// 
    /// ── 핵심 책임 ──
    /// 외부(FSM 상태들, UI)에서 호출하면, 적절한 하위 시스템에 위임하고,
    /// 결과를 SO Event Channel로 알린다.
    /// 이 클래스 자체는 로직을 갖지 않는다. 오직 연결과 위임만 한다.
    /// 
    /// ── 하위 시스템 ──
    /// - CardPile: 카드 더미 데이터 (4개 영역)
    /// - CardDrawHandler: 드로우/리롤 로직
    /// - CardSelectionHandler: 카드 선택/해제 로직
    /// 
    /// ── 호출 흐름 ──
    /// BattleManager(FSM)
    ///   └── TurnReadyState   → DeckSystem.DrawCards() / Reroll()
    ///   └── TurnProceedState → DeckSystem.SelectedCards (getter)
    ///   └── TurnEndState     → DeckSystem.CleanupForNextTurn()
    /// </summary>
    public class DeckSystem : MonoBehaviour
    {
        #region === Inspector: SO Event Channels ===

        [Header("=== SO Event Channels (→ UI 알림) ===")]

        [Tooltip("카드 드로우 완료. UI는 Hand getter로 조회.")]
        [SerializeField] private GameEvent _onCardsDrawn;

        [Tooltip("리롤 완료. UI는 Hand getter로 갱신된 손패 조회.")]
        [SerializeField] private GameEvent _onRerolled;

        [Tooltip("카드 선택/해제 변경. UI는 SelectedCards, Hand getter로 조회.")]
        [SerializeField] private GameEvent _onSelectionChanged;

        [Tooltip("턴 정리 완료.")]
        [SerializeField] private GameEvent _onTurnCleanedUp;

        [Tooltip("묘지 재활용 발생. 연출용.")]
        [SerializeField] private GameEvent _onDiscardRecycled;

        #endregion

        #region === Inspector: 설정값 ===

        [Header("=== 카드 시스템 설정 ===")]

        [Tooltip("한 턴에 뽑는 카드 수 (k). 백로그 4번: 초기 값 5")]
        [SerializeField] private int _drawCount = 5;

        [Tooltip("턴당 리롤 최대 횟수 (r). 백로그 4번: 초기 값 1")]
        [SerializeField] private int _maxRerolls = 1;

        [Tooltip("최종 선택 가능한 카드 수. 백로그 5번: 2장")]
        [SerializeField] private int _maxSelectCount = 2;

        #endregion

        #region === 하위 시스템 ===

        private CardPile _pile;
        private CardDrawHandler _drawHandler;
        private CardSelectionHandler _selectionHandler;

        /// <summary>장신구 등 외부 효과에 의한 보너스 리롤 횟수.</summary>
        private int _bonusRerolls = 0;
 
        /// <summary>장신구 등 외부 효과에 의한 가중치 드로우 함수.</summary>
        private Func<Data.HwaTuCard, float> _drawWeightFunc = null;
 
        #endregion

        #region === Getter (외부 → 데이터 조회) ===

        // CardPile 데이터를 그대로 노출. 외부는 CardPile의 존재를 모른다.

        /// <summary>뽑을 화투패 산. 읽기 전용.</summary>
        public IReadOnlyList<Data.HwaTuCard> DrawPile => _pile.DrawPile;

        /// <summary>현재 손패. 읽기 전용.</summary>
        public IReadOnlyList<Data.HwaTuCard> Hand => _pile.Hand;

        /// <summary>최종 선택된 카드. 읽기 전용.</summary>
        public IReadOnlyList<Data.HwaTuCard> SelectedCards => _pile.SelectedCards;

        /// <summary>버려진 화투패 산 (묘지). 읽기 전용.</summary>
        public IReadOnlyList<Data.HwaTuCard> DiscardPile => _pile.DiscardPile;

        /// <summary>남은 리롤 횟수.</summary>
        public int RerollsRemaining => _drawHandler.RerollsRemaining;

        /// <summary>리롤 가능 여부.</summary>
        public bool CanReroll => _drawHandler.CanReroll;

        /// <summary>최종 선택 완료 여부 (2장).</summary>
        public bool IsSelectionComplete => _selectionHandler.IsSelectionComplete;

        /// <summary>전체 카드 수. 디버그용.</summary>
        public int TotalCardCount => _pile.TotalCardCount;

        #endregion

        #region === Setter: 외부 효과 적용 (장신구/조커 매니저가 호출) ===
 
        /// <summary>
        /// 보너스 리롤 횟수를 증감한다.
        /// 장신구 등 외부 효과에서 호출. Initialize() 시 반영된다.
        /// 호출자가 누구인지는 DeckSystem이 알 필요 없다.
        /// </summary>
        public void AddBonusRerolls(int bonus)
        {
            _bonusRerolls += bonus;
            Debug.Log($"[DeckSystem] 보너스 리롤 변경: {_bonusRerolls} (변경량: {(bonus >= 0 ? "+" : "")}{bonus})");
        }
 
        /// <summary>
        /// 가중치 드로우 함수를 설정한다.
        /// 장신구 등 외부 효과에서 호출. Initialize() 시 CardDrawHandler에 전달된다.
        /// null로 설정하면 균등 드로우로 복귀.
        /// </summary>
        public void SetDrawWeightFunc(Func<Data.HwaTuCard, float> func)
        {
            _drawWeightFunc = func;
            Debug.Log($"[DeckSystem] 가중치 드로우 함수 {(func != null ? "설정" : "해제")}");
        }
 
        /// <summary>
        /// 현재 턴의 남은 리롤 횟수를 즉시 증가시킨다.
        /// 조커 등 1회성 효과에서 호출.
        /// 다음 턴 DrawCards() 시 자동 리셋되므로 해제 불필요.
        /// </summary>
        public void AddTempRerolls(int bonus)
        {
            _drawHandler.AddTempRerolls(bonus);
        }
 
        #endregion
 
        #region === 초기화 ===

        /// <summary>
        /// 전투 시작 시 호출. 하위 시스템을 생성하고 덱을 구성한다.
        /// 
        /// 호출자: 초기화 로직 (BattleScene 진입 시)
        /// "이 카드들로 전투 준비해" → 내부에서 알아서 세팅.
        /// </summary>
        public void Initialize(List<Data.HwaTuCard> allCards, int seed = -1)
        {
            // 하위 시스템 생성
            _pile = new CardPile();
            _drawHandler = new CardDrawHandler(_pile, _drawCount, _maxRerolls + _bonusRerolls, _drawWeightFunc);
            _selectionHandler = new CardSelectionHandler(_pile, _maxSelectCount);

            // CardPile 초기화 (셔플 포함)
            _pile.Initialize(allCards, seed);

            Debug.Log($"[DeckSystem] 전투 초기화 완료. 카드 {allCards.Count}장, 시드: {seed}, " +
                      $"보너스 리롤: {_bonusRerolls}, 가중치 드로우: {(_drawWeightFunc != null ? "적용" : "없음")}");
        }

        #endregion

        #region === TurnReadyState에서 호출 ===

        /// <summary>
        /// 카드 k장을 드로우한다.
        /// CardDrawHandler에게 위임. SO Event 발행.
        /// </summary>
        public List<Data.HwaTuCard> DrawCards()
        {
            var drawn = _drawHandler.DrawCards();
            _onCardsDrawn?.Raise();
            return drawn;
        }

        /// <summary>
        /// 리롤을 수행한다.
        /// CardDrawHandler에게 위임. SO Event 발행.
        /// </summary>
        public List<Data.HwaTuCard> Reroll(List<Data.HwaTuCard> cardsToReturn)
        {
            var redrawn = _drawHandler.Reroll(cardsToReturn);

            if (redrawn.Count > 0)
            {
                _onRerolled?.Raise();
            }

            return redrawn;
        }

        #endregion

        #region === UI 인터랙션에서 호출 ===

        /// <summary>
        /// 손패에서 카드를 선택한다.
        /// CardSelectionHandler에게 위임. SO Event 발행.
        /// </summary>
        public bool SelectCard(Data.HwaTuCard card)
        {
            bool success = _selectionHandler.SelectCard(card);

            if (success)
            {
                _onSelectionChanged?.Raise();
            }

            return success;
        }

        /// <summary>
        /// 카드 선택을 해제한다.
        /// CardSelectionHandler에게 위임. SO Event 발행.
        /// </summary>
        public bool DeselectCard(Data.HwaTuCard card)
        {
            bool success = _selectionHandler.DeselectCard(card);

            if (success)
            {
                _onSelectionChanged?.Raise();
            }

            return success;
        }

        #endregion

        #region === TurnEndState에서 호출 ===

        /// <summary>
        /// 턴 종료 시 카드 정리.
        /// CardPile에게 직접 위임 (Hand + Selected → Discard).
        /// </summary>
        public void CleanupForNextTurn()
        {
            _pile.MoveAllToDiscard();
            _onTurnCleanedUp?.Raise();

            Debug.Log($"[DeckSystem] 턴 정리 완료. {this}");
        }

        #endregion

        #region === 디버그 ===

        public override string ToString()
        {
            return $"[DeckSystem] Draw:{_pile.DrawPile.Count} | Hand:{_pile.Hand.Count} | " +
                   $"Selected:{_pile.SelectedCards.Count}/{_maxSelectCount} | " +
                   $"Discard:{_pile.DiscardPile.Count} | Reroll:{_drawHandler.RerollsRemaining}/{_maxRerolls}";
        }

        #endregion
    }
}
