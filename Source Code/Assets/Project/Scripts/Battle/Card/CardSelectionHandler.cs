using UnityEngine;
using FFF.Data;

namespace FFF.Battle.Card
{
    /// <summary>
    /// 카드 선택 및 해제 로직을 담당한다.
    /// 
    /// ── 핵심 책임 ──
    /// "이 카드 선택할게" → CardPile에게 Hand→SelectedCards 이동을 시킨다.
    /// "이 카드 선택 취소할게" → CardPile에게 SelectedCards→Hand 이동을 시킨다.
    /// 최대 선택 수 제한을 관리한다.
    /// 
    /// ── 설계 원칙 ──
    /// - CardPile의 내부 구조를 알 필요 없다. "옮겨"라고만 한다.
    /// - 선택 수 제한 판단은 이 클래스의 책임이다.
    /// - 호출자(UI 인터랙션)는 SelectCard()만 부르면 된다.
    /// </summary>
    public class CardSelectionHandler
    {
        private readonly CardPile _pile;
        public int _maxSelectCount { get; private set; } = 2;

        #region === Getter ===

        /// <summary>최종 선택이 완료되었는지 (2장 선택됨).</summary>
        public bool IsSelectionComplete => _pile.SelectedCards.Count >= _maxSelectCount;

        /// <summary>현재 선택된 카드 수.</summary>
        public int CurrentSelectCount => _pile.SelectedCards.Count;

        /// <summary>최대 선택 가능 수.</summary>
        public int MaxSelectCount => _maxSelectCount;

        #endregion

        #region === 생성 ===

        /// <summary>
        /// CardSelectionHandler를 생성한다.
        /// </summary>
        /// <param name="pile">카드 데이터를 관리하는 CardPile</param>
        /// <param name="maxSelectCount">최종 선택 가능한 카드 수. 기본 2.</param>
        public CardSelectionHandler(CardPile pile, int maxSelectCount = 2)
        {
            _pile = pile;
            _maxSelectCount = maxSelectCount;
        }

        public void SetMaxSelectionLimit(int limit)
        {
            _maxSelectCount = limit;
        }

        #endregion

        #region === 선택 / 해제 ===

        /// <summary>
        /// 손패에서 카드를 선택하여 SelectedCards로 이동한다.
        /// 
        /// 호출자: UI 인터랙션 (플레이어가 카드 클릭)
        /// "이 카드 선택할게" → 내부에서 CardPile에 이동 명령을 내린다.
        /// 
        /// 백로그 5번: 최종 선택 카드 2장
        /// </summary>
        /// <returns>선택 성공 여부</returns>
        public bool SelectCard(HwaTuCard card)
        {
            if (IsSelectionComplete)
            {
                Debug.LogWarning($"[CardSelectionHandler] 최대 선택 수({_maxSelectCount})에 도달.");
                return false;
            }

            if (!_pile.MoveHandToSelected(card))
            {
                Debug.LogWarning($"[CardSelectionHandler] 손패에 없는 카드 선택 시도: {card}");
                return false;
            }

            Debug.Log($"[CardSelectionHandler] 카드 선택: {card}. ({CurrentSelectCount}/{_maxSelectCount})");
            return true;
        }

        /// <summary>
        /// 선택을 해제하여 Hand로 되돌린다.
        /// 
        /// 호출자: UI 인터랙션 (플레이어가 선택된 카드 클릭)
        /// "이 카드 선택 취소할게"
        /// </summary>
        /// <returns>해제 성공 여부</returns>
        public bool DeselectCard(HwaTuCard card)
        {
            if (!_pile.MoveSelectedToHand(card))
            {
                Debug.LogWarning($"[CardSelectionHandler] 선택되지 않은 카드 해제 시도: {card}");
                return false;
            }

            Debug.Log($"[CardSelectionHandler] 카드 선택 해제: {card}. ({CurrentSelectCount}/{_maxSelectCount})");
            return true;
        }

        #endregion
    }
}
