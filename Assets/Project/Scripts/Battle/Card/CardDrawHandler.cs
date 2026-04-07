using System;
using System.Collections.Generic;
using UnityEngine;

namespace FFF.Battle.Card
{
    /// <summary>
    /// 카드 드로우 및 리롤 로직을 담당한다.
    /// 
    /// ── 핵심 책임 ──
    /// "카드 n장 뽑아줘" → CardPile에게 이동 명령을 내려 알아서 처리한다.
    /// "이 카드들 리롤해줘" → CardPile에게 반납/재드로우 명령을 내린다.
    /// 드로우 시 카드 부족하면 묘지 재활용을 CardPile에게 시킨다.
    /// 
    /// ── 설계 원칙 ──
    /// - CardPile의 내부 구조를 알 필요 없다. "옮겨"라고만 한다.
    /// - 리롤 횟수 관리는 이 클래스의 책임이다.
    /// - 호출자(TurnReadyState)는 DrawCards()만 부르면 된다. 나머지는 알빠노.
    /// </summary>
    public class CardDrawHandler
    {
        private readonly CardPile _pile;
        private readonly int _drawCount;
        private readonly int _maxRerolls;

        private int _rerollsRemaining;

        #region === Getter ===

        /// <summary>남은 리롤 횟수.</summary>
        public int RerollsRemaining => _rerollsRemaining;

        /// <summary>가중치 드로우 함수. null이면 균등 드로우 (기존 동작).</summary>
        private readonly Func<Data.HwaTuCard, float> _drawWeightFunc;

        /// <summary>리롤 가능 여부.</summary>
        public bool CanReroll => _rerollsRemaining > 0;

        /// <summary>한 턴 드로우 장수.</summary>
        public int DrawCount => _drawCount;

        #endregion

        #region === 생성 ===

        /// <summary>
        /// CardDrawHandler를 생성한다.
        /// </summary>
        /// <param name="pile">카드 데이터를 관리하는 CardPile</param>
        /// <param name="drawCount">한 턴에 뽑는 카드 수 (k). 기본 5.</param>
        /// <param name="maxRerolls">턴당 리롤 최대 횟수 (r). 기본 1.</param>
        /// <param name="drawWeightFunc">가중치 드로우 함수. null이면 균등 드로우.</param>
        public CardDrawHandler(CardPile pile, int drawCount = 5, int maxRerolls = 1,
                               Func<Data.HwaTuCard, float> drawWeightFunc = null)
        {
            _pile = pile;
            _drawCount = drawCount;
            _maxRerolls = maxRerolls;
            _rerollsRemaining = _maxRerolls;
            _drawWeightFunc = drawWeightFunc;
        }
 
        #endregion

        #region === 드로우 ===

        /// <summary>
        /// DrawPile에서 k장을 Hand로 드로우한다.
        /// 카드가 부족하면 내부에서 묘지 재활용까지 알아서 처리한다.
        /// 리롤 횟수도 초기화된다.
        /// 
        /// 호출자: TurnReadyState
        /// 호출자는 "카드 뽑아줘" 하고 결과만 받으면 된다.
        /// 
        /// 백로그 4번: "'화투패 산'에서 k장(초기 값 5)의 카드가 손패로 들어온다."
        /// </summary>
        /// <returns>새로 뽑힌 카드 목록</returns>
        public List<Data.HwaTuCard> DrawCards()
        {
            EnsureDrawPileHasEnough(_drawCount);
 
            // 가중치 함수가 있으면 가중치 드로우, 없으면 기존 균등 드로우
            List<Data.HwaTuCard> drawn;
            if (_drawWeightFunc != null)
            {
                drawn = _pile.MoveDrawToHandWeighted(_drawCount, _drawWeightFunc);
            }
            else
            {
                drawn = _pile.MoveDrawToHand(_drawCount);
            }
 
            _rerollsRemaining = _maxRerolls;
 
            Debug.Log($"[CardDrawHandler] {drawn.Count}장 드로우 완료. " +
                      $"가중치: {(_drawWeightFunc != null ? "적용" : "없음")}. 남은 리롤: {_rerollsRemaining}");
 
            return drawn;
        }

        #endregion

        #region === 리롤 ===

        /// <summary>
        /// 지정한 카드들을 DrawPile에 되돌리고 같은 수만큼 재드로우한다.
        /// 
        /// 호출자: TurnReadyState (플레이어 리롤 요청 시)
        /// "이 카드들 리롤해줘" → 반납, 셔플, 재드로우를 알아서 한다.
        /// 
        /// 백로그 4번: "원하는 장수만큼 선택한 뒤, '리롤' 버튼을 눌러
        /// 다시 '뽑을 화투패 산'에 넣고, 같은 수만큼 무작위로 뽑아온다."
        /// </summary>
        /// <param name="cardsToReturn">손패에서 되돌릴 카드 목록</param>
        /// <returns>새로 뽑은 카드 목록. 리롤 불가 시 빈 목록.</returns>
        public List<Data.HwaTuCard> Reroll(List<Data.HwaTuCard> cardsToReturn)
        {
            if (!CanReroll)
            {
                Debug.LogWarning("[CardDrawHandler] 리롤 기회가 없습니다.");
                return new List<Data.HwaTuCard>();
            }

            if (cardsToReturn == null || cardsToReturn.Count == 0)
            {
                Debug.LogWarning("[CardDrawHandler] 리롤할 카드가 없습니다.");
                return new List<Data.HwaTuCard>();
            }

            // 1. 선택한 카드를 Hand → DrawPile로 반납
            int returned = _pile.MoveHandToDrawPile(cardsToReturn);

            // 2. 셔플
            _pile.ShuffleDrawPile();

            // 3. 반납한 수만큼 재드로우
            EnsureDrawPileHasEnough(returned);
            var redrawn = _pile.MoveDrawToHand(returned);

            _rerollsRemaining--;

            Debug.Log($"[CardDrawHandler] 리롤 완료. {returned}장 반납 → {redrawn.Count}장 재드로우. 남은 리롤: {_rerollsRemaining}");

            return redrawn;
        }

        /// <summary>
        /// 현재 턴의 남은 리롤 횟수를 즉시 증가시킨다.
        /// 다음 턴 DrawCards() 호출 시 _rerollsRemaining이 _maxRerolls로 리셋되므로
        /// 자연스럽게 1턴짜리 효과가 된다.
        /// 
        /// 호출자: DeckSystem (조커 효과 등)
        /// 호출자는 왜 리롤이 늘어나는지 알 필요 없다.
        /// </summary>
        public void AddTempRerolls(int bonus)
        {
            _rerollsRemaining += bonus;
            Debug.Log($"[CardDrawHandler] 임시 리롤 추가: +{bonus}. 현재 남은 리롤: {_rerollsRemaining}");
        }
        
        #endregion

        #region === 내부 로직 ===

        /// <summary>
        /// DrawPile에 필요한 만큼 카드가 있는지 확인하고,
        /// 부족하면 묘지 재활용을 CardPile에게 시킨다.
        /// 호출자는 이 과정을 알 필요 없다.
        /// 
        /// 백로그 5번: "'뽑을 화투패 산에 있는 카드 수'가 부족하다면,
        /// '버려진 화투패 산'에 있는 모든 화투패들을 다시 되돌린다."
        /// </summary>
        private void EnsureDrawPileHasEnough(int needed)
        {
            if (_pile.DrawPile.Count < needed)
            {
                int recycled = _pile.RecycleDiscardPile();

                if (recycled > 0)
                {
                    Debug.Log($"[CardDrawHandler] 카드 부족 → 묘지 {recycled}장 재활용");
                }
            }
        }

        #endregion
    }
}
