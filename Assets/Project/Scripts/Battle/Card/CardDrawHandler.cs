using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Data;

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

        #region === Getter ===
        /// <summary>가중치 드로우 함수. null이면 균등 드로우 (기존 동작).</summary>
        private readonly Func<HwaTuCard, float> _drawWeightFunc;

        #endregion

        #region === 생성 ===

        /// <summary>
        /// CardDrawHandler를 생성한다.
        /// </summary>
        /// <param name="pile">카드 데이터를 관리하는 CardPile</param>
        /// <param name="drawWeightFunc">가중치 드로우 함수. null이면 균등 드로우.</param>
        public CardDrawHandler(CardPile pile, Func<HwaTuCard, float> drawWeightFunc = null)
        {
            _pile = pile;
            _drawWeightFunc = drawWeightFunc;
        }
 
        #endregion

        #region === 드로우 ===

        /// <summary>
        /// DrawPile에서 count장을 Hand로 드로우한다.
        /// 카드가 부족하면 내부에서 묘지 재활용까지 알아서 처리한다.
        /// 
        /// 호출자: DeckSystem
        /// 호출자는 "카드 뽑아줘" 하고 결과만 받으면 된다.
        /// 
        /// 백로그 4번: "'화투패 산'에서 k장(초기 값 5)의 카드가 손패로 들어온다."
        /// </summary>
        /// <returns>새로 뽑힌 카드 목록</returns>
        public List<HwaTuCard> DrawCards(int count)
        {

            // 가중치 함수가 있으면 가중치 드로우, 없으면 기존 균등 드로우
            List<HwaTuCard> drawn = new List<HwaTuCard>();
            int remainDrawCardsCount = count;

            // 1차 드로우: 현재 뽑을 산에 있는 카드를 가능한 만큼 모두 뽑기
            int firstDrawCount = Mathf.Min(remainDrawCardsCount, _pile.DrawPile.Count);
            if (firstDrawCount > 0)
            {
                if (_drawWeightFunc != null)
                    drawn.AddRange(_pile.MoveDrawToHandWeighted(firstDrawCount, _drawWeightFunc));
                else
                    drawn.AddRange(_pile.MoveDrawToHand(firstDrawCount));

                remainDrawCardsCount -= firstDrawCount;
            }

            // 카드가 부족하다면 묘지 재활용 후 2차 드로우
            if (remainDrawCardsCount > 0)
            {
                ReturnDiscardToDrawPile();

                // 2차 드로우: 재활용 후 채워진 산에서 남은 카드 수만큼 다시 뽑기
                int secondDrawCount = Mathf.Min(remainDrawCardsCount, _pile.DrawPile.Count);
                
                // 가능한 만큼만 뽑고 더 이상 카드가 없으면 로직 자동 종료 (더 이상 뽑을 카드 자체가 존재하지 않는다)
                if (secondDrawCount > 0)
                {
                    if (_drawWeightFunc != null)
                        drawn.AddRange(_pile.MoveDrawToHandWeighted(secondDrawCount, _drawWeightFunc));
                    else
                        drawn.AddRange(_pile.MoveDrawToHand(secondDrawCount));
                }
            }
 
            Debug.Log($"[CardDrawHandler] {drawn.Count}장 드로우 완료. " +
                      $"가중치: {(_drawWeightFunc != null ? "적용" : "없음")}.");
 
            return drawn;
        }

        #endregion

        #region === 리롤 ===

        /// <summary>
        /// 지정한 카드들을 DrawPile에 되돌리고 같은 수만큼 재드로우한다.
        /// 
        /// 호출자: DeckSystem (플레이어 리롤 요청 시)
        /// "이 카드들 리롤해줘" → 반납, 셔플, 재드로우를 알아서 한다.
        /// 
        /// 백로그 4번: "원하는 장수만큼 선택한 뒤, '리롤' 버튼을 눌러
        /// 다시 '뽑을 화투패 산'에 넣고, 같은 수만큼 무작위로 뽑아온다."
        /// </summary>
        /// <param name="cardsToReturn">손패에서 되돌릴 카드 목록</param>
        /// <returns>새로 뽑은 카드 목록. 리롤 불가 시 빈 목록.</returns>
        public List<HwaTuCard> Reroll(List<HwaTuCard> cardsToReturn)
        {
            if (cardsToReturn == null || cardsToReturn.Count == 0)
            {
                Debug.LogWarning("[CardDrawHandler] 리롤할 카드가 없습니다.");
                return new List<HwaTuCard>();
            }

            // 1. 선택한 카드를 Hand → DrawPile로 반납
            int returned = _pile.MoveSelectedCardsToDrawPile(cardsToReturn);

            // 실제로 반납된 카드가 0장이면 리롤을 취소
            if (returned == 0) return new List<HwaTuCard>();

            // 2. 셔플
            _pile.ShuffleDrawPile();

            // 3. 반납한 수만큼 재드로우
            var redrawn = DrawCards(returned);

            Debug.Log($"[CardDrawHandler] 리롤 완료. {returned}장 반납 → {redrawn.Count}장 재드로우.");

            return redrawn;
        }
        
        #endregion

        #region === 턴 정리 ===
        /// <summary>
        /// Hand와 SelectedCards의 모든 카드를 DiscardPile로 이동한다.
        /// 호출자: DeckSystem
        /// </summary>
        public void MoveAllToDiscard()
        {
            _pile.MoveAllToDiscard();
        }
        #endregion

        #region === 내부 로직 ===

        /// <summary>
        /// 외부에서 DrawPile에서 뽑을 카드가 부족하면 호출한다.
        /// 이것이 호출되면 CardPile에서 버려진 카드들을 뽑을 카드로 바꾼다.
        /// 호출자는 이 과정을 알 필요 없다.
        /// 
        /// 백로그 5번: "'뽑을 화투패 산에 있는 카드 수'가 부족하다면,
        /// '버려진 화투패 산'에 있는 모든 화투패들을 다시 되돌린다."
        /// </summary>
        private void ReturnDiscardToDrawPile()
        {

            int recycled = _pile.RecycleDiscardPile();

            if (recycled > 0)
            {
                Debug.Log($"[CardDrawHandler] 카드 부족 → 묘지 {recycled}장 재활용");
            }
        }

        #endregion
    }
}
