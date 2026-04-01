using System;
using System.Collections.Generic;
using UnityEngine;

namespace FFF.Battle.Card
{
    /// <summary>
    /// 전투 중 카드 덱을 관리하는 시스템.
    /// 
    /// 아키텍처 다이어그램에서 "카드 드로우 시스템"에 해당한다.
    /// Controller 영역에 위치하며, UI와는 S.O 이벤트 채널로 통신한다.
    /// 
    /// 카드 영역 구성:
    /// - DrawPile (뽑을 화투패 산): 아직 뽑지 않은 카드들
    /// - Hand (손패): 현재 턴에 플레이어가 보유한 카드들
    /// - SelectedCards (최종 선택): 플레이어가 최종 제출할 카드 2장
    /// - DiscardPile (버려진 화투패 산): 사용 완료된 카드들
    /// 
    /// 흐름 (백로그 4번, 5번 기준):
    /// 1. 전투 시작 → 48장 덱 셔플 → DrawPile에 적재
    /// 2. 턴 시작 → DrawPile에서 k장(기본 5) 드로우 → Hand로 이동
    /// 3. 리롤 → Hand에서 선택한 카드를 DrawPile에 되돌리고, 같은 수만큼 재드로우
    /// 4. 최종 선택 → Hand에서 2장을 SelectedCards로 이동
    /// 5. 턴 종료 → Hand + SelectedCards → DiscardPile로 이동
    /// 6. 다음 턴 드로우 시, DrawPile 부족하면 DiscardPile을 DrawPile로 되돌림
    /// </summary>
    public class DeckSystem
    {
        /// <summary>
        /// 뽑을 화투패 산.
        /// </summary>
        public List<Data.HwaTuCard> DrawPile { get; private set; } = new List<Data.HwaTuCard>();

        /// <summary>
        /// 플레이어 손패.
        /// </summary>
        public List<Data.HwaTuCard> Hand { get; private set; } = new List<Data.HwaTuCard>();

        /// <summary>
        /// 최종 선택된 카드 (최대 2장).
        /// </summary>
        public List<Data.HwaTuCard> SelectedCards { get; private set; } = new List<Data.HwaTuCard>();

        /// <summary>
        /// 버려진 화투패 산.
        /// </summary>
        public List<Data.HwaTuCard> DiscardPile { get; private set; } = new List<Data.HwaTuCard>();

        /// <summary>
        /// 한 턴에 뽑는 카드 수. 백로그 4번: "k장(초기 값 5)"
        /// </summary>
        public int DrawCount { get; set; } = 5;

        /// <summary>
        /// 리롤 남은 횟수. 백로그 4번: "r번(초기 값 1)"
        /// </summary>
        public int RerollsRemaining { get; private set; }

        /// <summary>
        /// 리롤 최대 횟수 (턴 시작 시 이 값으로 초기화).
        /// </summary>
        public int MaxRerolls { get; set; } = 1;

        /// <summary>
        /// 최종 선택 가능한 카드 수. 백로그 5번: "화투패 2장"
        /// </summary>
        public int MaxSelectedCards { get; set; } = 2;

        /// <summary>
        /// 무작위 시드 기반 셔플용 Random 인스턴스.
        /// 백로그 1번: "무작위 시드에 기반하여"
        /// </summary>
        private System.Random _random;

        // === 이벤트: UI에 상태 변화를 알리기 위한 콜백 ===
        /// <summary>카드가 드로우되었을 때 (드로우된 카드 목록)</summary>
        public event Action<List<Data.HwaTuCard>> OnCardsDrawn;

        /// <summary>리롤이 수행되었을 때 (반환된 카드, 새로 뽑은 카드)</summary>
        public event Action<List<Data.HwaTuCard>, List<Data.HwaTuCard>> OnRerolled;

        /// <summary>카드가 최종 선택되었을 때 (선택된 카드)</summary>
        public event Action<Data.HwaTuCard> OnCardSelected;

        /// <summary>카드가 선택 해제되었을 때 (해제된 카드)</summary>
        public event Action<Data.HwaTuCard> OnCardDeselected;

        /// <summary>턴이 종료되었을 때</summary>
        public event Action OnTurnEnded;

        /// <summary>버려진 산이 뽑을 산으로 되돌려졌을 때</summary>
        public event Action OnDiscardRecycled;

        /// <summary>
        /// 전투 시작 시 덱을 초기화한다.
        /// </summary>
        /// <param name="allCards">플레이어의 전체 카드 목록 (48장 또는 덱빌딩으로 수정된 목록)</param>
        /// <param name="seed">무작위 시드 (-1이면 랜덤 시드 사용)</param>
        public void Initialize(List<Data.HwaTuCard> allCards, int seed = -1)
        {
            _random = seed >= 0 ? new System.Random(seed) : new System.Random();

            DrawPile.Clear();
            Hand.Clear();
            SelectedCards.Clear();
            DiscardPile.Clear();

            // 모든 카드를 뽑을 산에 넣고 셔플
            DrawPile.AddRange(allCards);
            Shuffle(DrawPile);

            RerollsRemaining = MaxRerolls;

            Debug.Log($"[DeckSystem] 덱 초기화 완료. DrawPile: {DrawPile.Count}장, 시드: {seed}");
        }

        /// <summary>
        /// 턴 시작 시 호출. DrawPile에서 k장을 Hand로 드로우한다.
        /// 백로그 4번: "'화투패 산'에서 k장(초기 값 5)의 카드가 손패로 들어온다."
        /// </summary>
        public List<Data.HwaTuCard> DrawCards()
        {
            // 뽑을 산이 부족하면 버려진 산을 되돌림
            if (DrawPile.Count < DrawCount)
            {
                RecycleDiscardPile();
            }

            int actualDraw = Mathf.Min(DrawCount, DrawPile.Count);
            var drawnCards = new List<Data.HwaTuCard>();

            for (int i = 0; i < actualDraw; i++)
            {
                var card = DrawPile[0];
                DrawPile.RemoveAt(0);
                Hand.Add(card);
                drawnCards.Add(card);
            }

            RerollsRemaining = MaxRerolls;

            Debug.Log($"[DeckSystem] {actualDraw}장 드로우. Hand: {Hand.Count}장, DrawPile 남은: {DrawPile.Count}장");

            OnCardsDrawn?.Invoke(drawnCards);
            return drawnCards;
        }

        /// <summary>
        /// 리롤을 수행한다.
        /// 백로그 4번: "손패로 들어온 카드 중 원하는 장수만큼 선택한 뒤, '리롤' 버튼을 눌러
        /// 다시 '뽑을 화투패 산'에 넣고, 넣은 카드의 장수 만큼 '뽑을 화투패 산'에서 무작위로 뽑아온다."
        /// </summary>
        /// <param name="cardsToReturn">손패에서 되돌릴 카드 목록</param>
        /// <returns>새로 뽑은 카드 목록. 리롤 실패 시 빈 목록.</returns>
        public List<Data.HwaTuCard> Reroll(List<Data.HwaTuCard> cardsToReturn)
        {
            if (RerollsRemaining <= 0)
            {
                Debug.LogWarning("[DeckSystem] 리롤 기회가 없습니다.");
                return new List<Data.HwaTuCard>();
            }

            if (cardsToReturn == null || cardsToReturn.Count == 0)
            {
                Debug.LogWarning("[DeckSystem] 리롤할 카드가 없습니다.");
                return new List<Data.HwaTuCard>();
            }

            // 선택한 카드를 손패에서 우선 제거 (아직 뽑을 산에 넣지 않음)
            List<Data.HwaTuCard> validReturnedCards = new List<Data.HwaTuCard>();
            foreach (var card in cardsToReturn)
            {
                if (Hand.Remove(card))
                {
                    validReturnedCards.Add(card);
                }
                else
                {
                    Debug.LogWarning($"[DeckSystem] 손패에 없는 카드 리롤 시도: {card}");
                }
            }

            // 남은 뽑을 산이 부족하면 버려진 산을 되돌림
            if (DrawPile.Count < validReturnedCards.Count)
            {
                RecycleDiscardPile();
            }

            // 뽑을 산 + 버려진 산을 합쳐도 리롤할 장수보다 부족한 극단적인 경우에만, 
            // 어쩔 수 없이 방금 리롤한 카드를 덱에 먼저 넣음
            if (DrawPile.Count < validReturnedCards.Count)
            {
                DrawPile.AddRange(validReturnedCards);
                Shuffle(DrawPile);
                validReturnedCards.Clear(); // 덱에 넣었으므로 리스트 비움
            }

            // 되돌린 장수만큼 '새로운' 카드를 드로우
            int drawAmount = Mathf.Min(cardsToReturn.Count, DrawPile.Count);
            var newCards = new List<Data.HwaTuCard>();

            for (int i = 0; i < drawAmount; i++)
            {
                var card = DrawPile[0];
                DrawPile.RemoveAt(0);
                Hand.Add(card);
                newCards.Add(card);
            }

            // 드로우가 끝난 뒤, 리롤했던 카드가 남아있다면 이제 뽑을 산에 넣고 셔플
            if (validReturnedCards.Count > 0)
            {
                DrawPile.AddRange(validReturnedCards);
                Shuffle(DrawPile);
            }

            RerollsRemaining--;

            Debug.Log($"[DeckSystem] 리롤 완료. 반환: {cardsToReturn.Count}장, 새 드로우: {newCards.Count}장, 남은 리롤: {RerollsRemaining}");

            OnRerolled?.Invoke(cardsToReturn, newCards);
            return newCards;
        }

        /// <summary>
        /// 손패에서 카드를 최종 선택한다.
        /// 백로그 5번: "마우스 커서로 손패에 있는 카드를 선택하면, 해당 카드는 중앙으로 이동"
        /// </summary>
        /// <returns>선택 성공 여부</returns>
        public bool SelectCard(Data.HwaTuCard card)
        {
            if (SelectedCards.Count >= MaxSelectedCards)
            {
                Debug.LogWarning($"[DeckSystem] 이미 {MaxSelectedCards}장 선택됨. 더 이상 선택 불가.");
                return false;
            }

            if (!Hand.Contains(card))
            {
                Debug.LogWarning($"[DeckSystem] 손패에 없는 카드 선택 시도: {card}");
                return false;
            }

            Hand.Remove(card);
            SelectedCards.Add(card);

            Debug.Log($"[DeckSystem] 카드 선택: {card}. 선택된 카드: {SelectedCards.Count}/{MaxSelectedCards}");

            OnCardSelected?.Invoke(card);
            return true;
        }

        /// <summary>
        /// 선택한 카드를 선택 해제하여 손패로 되돌린다.
        /// 백로그 5번: "중앙으로 이동된 카드를 선택하면, 다시 손패의 카드들 위치로 이동"
        /// </summary>
        /// <returns>선택 해제 성공 여부</returns>
        public bool DeselectCard(Data.HwaTuCard card)
        {
            if (!SelectedCards.Contains(card))
            {
                Debug.LogWarning($"[DeckSystem] 선택되지 않은 카드 해제 시도: {card}");
                return false;
            }

            SelectedCards.Remove(card);
            Hand.Add(card);

            Debug.Log($"[DeckSystem] 카드 선택 해제: {card}. 선택된 카드: {SelectedCards.Count}/{MaxSelectedCards}");

            OnCardDeselected?.Invoke(card);
            return true;
        }

        /// <summary>
        /// 최종 선택이 완료되었는지 확인한다.
        /// 백로그 5번: "최종적으로 선택된 카드가 2장이 되면 '턴 종료' 버튼이 활성화"
        /// </summary>
        public bool IsSelectionComplete()
        {
            return SelectedCards.Count >= MaxSelectedCards;
        }

        /// <summary>
        /// 턴을 종료한다.
        /// 백로그 5번: "손패에 있던 카드들과 최종 선택한 2장의 카드는 '버려진 화투패 산'으로 이동"
        /// </summary>
        public void EndTurn()
        {
            // 손패의 남은 카드 → 버려진 산
            DiscardPile.AddRange(Hand);
            Hand.Clear();

            // 최종 선택 카드 → 버려진 산
            DiscardPile.AddRange(SelectedCards);
            SelectedCards.Clear();

            Debug.Log($"[DeckSystem] 턴 종료. DiscardPile: {DiscardPile.Count}장, DrawPile: {DrawPile.Count}장");

            OnTurnEnded?.Invoke();
        }

        /// <summary>
        /// 버려진 산의 카드를 뽑을 산으로 되돌린다.
        /// 백로그 5번: "'뽑을 화투패 산에 있는 카드 수'가 k장만큼의 카드보다 부족하다면,
        /// '버려진 화투패 산'에 있는 모든 화투패들을 다시 '뽑을 화투패 산'으로 되돌린다."
        /// </summary>
        private void RecycleDiscardPile()
        {
            if (DiscardPile.Count == 0)
            {
                Debug.LogWarning("[DeckSystem] 버려진 산도 비어있습니다.");
                return;
            }

            Debug.Log($"[DeckSystem] 버려진 산 {DiscardPile.Count}장을 뽑을 산으로 되돌림");

            DrawPile.AddRange(DiscardPile);
            DiscardPile.Clear();
            Shuffle(DrawPile);

            OnDiscardRecycled?.Invoke();
        }

        /// <summary>
        /// 덱에 카드를 추가한다. 상점에서 카드 구매 시 사용.
        /// 백로그 9번: "상점에서 특정 카드를 구매"
        /// </summary>
        public void AddCardToDeck(Data.HwaTuCard card)
        {
            DrawPile.Add(card);
            Shuffle(DrawPile);
            Debug.Log($"[DeckSystem] 덱에 카드 추가: {card}. DrawPile: {DrawPile.Count}장");
        }

        /// <summary>
        /// 덱에서 카드를 제거한다. 상점에서 카드 제거 시 사용.
        /// 백로그 9번: "플레이어가 가지고 있는 화투패 중 특정 카드를 제거 가능"
        /// </summary>
        public bool RemoveCardFromDeck(Data.HwaTuCard card)
        {
            // 뽑을 산, 버려진 산 어디에 있든 제거
            bool removed = DrawPile.Remove(card) || DiscardPile.Remove(card);

            if (removed)
            {
                Debug.Log($"[DeckSystem] 덱에서 카드 제거: {card}");
            }
            else
            {
                Debug.LogWarning($"[DeckSystem] 덱에 없는 카드 제거 시도: {card}");
            }

            return removed;
        }

        /// <summary>
        /// Fisher-Yates 셔플 알고리즘.
        /// </summary>
        private void Shuffle(List<Data.HwaTuCard> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }

        /// <summary>
        /// 전체 카드 수를 반환한다 (모든 영역 합계).
        /// 디버그 및 검증용.
        /// </summary>
        public int GetTotalCardCount()
        {
            return DrawPile.Count + Hand.Count + SelectedCards.Count + DiscardPile.Count;
        }

        /// <summary>
        /// 현재 상태를 문자열로 반환한다.
        /// </summary>
        public override string ToString()
        {
            return $"[DeckSystem] Draw: {DrawPile.Count} | Hand: {Hand.Count} | Selected: {SelectedCards.Count}/{MaxSelectedCards} | Discard: {DiscardPile.Count} | Rerolls: {RerollsRemaining}/{MaxRerolls}";
        }
    }
}
