using System.Collections.Generic;
using UnityEngine;

namespace FFF.Battle.Card
{
    /// <summary>
    /// 4개 카드 영역의 순수 데이터 관리자.
    /// 
    /// ── 핵심 책임 ──
    /// 뽑을 산(DrawPile), 손패(Hand), 선택(SelectedCards), 묘지(DiscardPile)을
    /// 들고 있으면서, "이 카드를 여기서 저기로 옮겨"라고 하면 알아서 옮긴다.
    /// 
    /// ── 설계 원칙 ──
    /// - 게임 로직(언제 드로우할지, 리롤 횟수 등)은 모른다.
    /// - 상위(CardDrawHandler, CardSelectionHandler 등)가 명령하면 시키는 대로 한다.
    /// - 외부에서는 IReadOnlyList getter로 읽기만 가능하다.
    /// </summary>
    public class CardPile
    {
        private List<Data.HwaTuCard> _drawPile = new();
        private List<Data.HwaTuCard> _hand = new();
        private List<Data.HwaTuCard> _selectedCards = new();
        private List<Data.HwaTuCard> _discardPile = new();

        private System.Random _random;

        #region === Getter (외부 읽기 전용) ===

        /// <summary>뽑을 화투패 산.</summary>
        public IReadOnlyList<Data.HwaTuCard> DrawPile => _drawPile;

        /// <summary>현재 손패.</summary>
        public IReadOnlyList<Data.HwaTuCard> Hand => _hand;

        /// <summary>최종 선택된 카드.</summary>
        public IReadOnlyList<Data.HwaTuCard> SelectedCards => _selectedCards;

        /// <summary>버려진 화투패 산 (묘지).</summary>
        public IReadOnlyList<Data.HwaTuCard> DiscardPile => _discardPile;

        /// <summary>전체 카드 수 (모든 영역 합계). 디버그/검증용.</summary>
        public int TotalCardCount =>
            _drawPile.Count + _hand.Count + _selectedCards.Count + _discardPile.Count;

        #endregion

        #region === 초기화 ===

        /// <summary>
        /// 모든 영역을 비우고, 전달받은 카드를 DrawPile에 넣고 셔플한다.
        /// </summary>
        public void Initialize(List<Data.HwaTuCard> allCards, int seed = -1)
        {
            _random = seed >= 0 ? new System.Random(seed) : new System.Random();

            _drawPile.Clear();
            _hand.Clear();
            _selectedCards.Clear();
            _discardPile.Clear();

            _drawPile.AddRange(allCards);
            Shuffle(_drawPile);

            Debug.Log($"[CardPile] 초기화 완료. DrawPile: {_drawPile.Count}장");
        }

        #endregion

        #region === 카드 이동 명령 ===

        /// <summary>
        /// DrawPile 맨 위에서 count장을 Hand로 이동한다.
        /// DrawPile에 있는 만큼만 이동. 부족 판단은 호출자 책임.
        /// </summary>
        /// <returns>실제로 이동된 카드 목록</returns>
        public List<Data.HwaTuCard> MoveDrawToHand(int count)
        {
            int actual = Mathf.Min(count, _drawPile.Count);
            var moved = new List<Data.HwaTuCard>(actual);

            for (int i = 0; i < actual; i++)
            {
                var card = _drawPile[0];
                _drawPile.RemoveAt(0);
                _hand.Add(card);
                moved.Add(card);
            }

            return moved;
        }

        /// <summary>
        /// 지정한 카드들을 Hand에서 제거하고 DrawPile로 되돌린다.
        /// </summary>
        /// <returns>실제로 되돌려진 카드 수</returns>
        public int MoveHandToDrawPile(List<Data.HwaTuCard> cards)
        {
            int count = 0;

            foreach (var card in cards)
            {
                if (_hand.Remove(card))
                {
                    _drawPile.Add(card);
                    count++;
                }
                else
                {
                    Debug.LogWarning($"[CardPile] Hand에 없는 카드 반납 시도: {card}");
                }
            }

            return count;
        }

        /// <summary>
        /// Hand에서 카드 하나를 SelectedCards로 이동한다.
        /// </summary>
        /// <returns>이동 성공 여부</returns>
        public bool MoveHandToSelected(Data.HwaTuCard card)
        {
            if (!_hand.Remove(card)) return false;

            _selectedCards.Add(card);
            return true;
        }

        /// <summary>
        /// SelectedCards에서 카드 하나를 Hand로 되돌린다.
        /// </summary>
        /// <returns>이동 성공 여부</returns>
        public bool MoveSelectedToHand(Data.HwaTuCard card)
        {
            if (!_selectedCards.Remove(card)) return false;

            _hand.Add(card);
            return true;
        }

        /// <summary>
        /// Hand + SelectedCards의 모든 카드를 DiscardPile로 이동한다.
        /// </summary>
        public void MoveAllToDiscard()
        {
            _discardPile.AddRange(_hand);
            _discardPile.AddRange(_selectedCards);

            int moved = _hand.Count + _selectedCards.Count;
            _hand.Clear();
            _selectedCards.Clear();

            Debug.Log($"[CardPile] {moved}장 → DiscardPile 이동. Discard 총: {_discardPile.Count}장");
        }

        /// <summary>
        /// DiscardPile의 모든 카드를 DrawPile로 되돌리고 셔플한다.
        /// </summary>
        /// <returns>되돌려진 카드 수</returns>
        public int RecycleDiscardPile()
        {
            if (_discardPile.Count == 0) return 0;

            int recycled = _discardPile.Count;
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);

            Debug.Log($"[CardPile] 묘지 재활용: {recycled}장 → DrawPile. Draw 총: {_drawPile.Count}장");

            return recycled;
        }

        /// <summary>
        /// DrawPile을 셔플한다.
        /// </summary>
        public void ShuffleDrawPile()
        {
            Shuffle(_drawPile);
        }

        #endregion

        #region === 내부 유틸 ===

        /// <summary>Fisher-Yates 셔플.</summary>
        private void Shuffle(List<Data.HwaTuCard> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }

        #endregion
    }
}
