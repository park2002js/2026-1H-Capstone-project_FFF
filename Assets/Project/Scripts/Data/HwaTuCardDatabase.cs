using System.Collections.Generic;
using UnityEngine;

namespace FFF.Data
{
    /// <summary>
    /// 화투 카드 데이터를 SO 에셋에서 로드하여 제공한다.
    /// 
    /// ── 변경 이력 ──
    /// 기존: static 클래스에서 20장을 코드로 하드코딩 생성
    /// 변경: Resources 폴더의 HwaTuCardSO 에셋들을 로드하여 HwaTuCard 리스트로 변환
    /// 
    /// ── 사용법 ──
    /// 1. Unity Editor에서 Create > FFF > Data > HwaTu Card 로 개별 SO 에셋 생성
    /// 2. 생성한 에셋을 Resources/Cards/ 폴더에 배치
    /// 3. 코드에서 HwaTuCardDatabase.CreateAllCards() 호출 (기존과 동일)
    /// 
    /// ── 설계 원칙 ──
    /// 반환 타입은 기존과 동일한 List&lt;HwaTuCard&gt;이므로,
    /// DeckSystem, CardPile, SeotdaJudge 등 외부 코드는 변경 불필요.
    /// </summary>
    public static class HwaTuCardDatabase
    {
        private const string CARDS_RESOURCE_PATH = "Cards";

        /// <summary>
        /// Resources/Cards/ 폴더에 있는 모든 HwaTuCardSO 에셋을 로드하여
        /// HwaTuCard 리스트로 변환하여 반환한다.
        /// </summary>
        public static List<HwaTuCard> CreateAllCards()
        {
            HwaTuCardSO[] cardSOs = Resources.LoadAll<HwaTuCardSO>(CARDS_RESOURCE_PATH);

            if (cardSOs == null || cardSOs.Length == 0)
            {
                Debug.LogError($"[HwaTuCardDatabase] Resources/{CARDS_RESOURCE_PATH}/ 에 카드 SO 에셋이 없습니다!");
                return new List<HwaTuCard>();
            }

            var cards = new List<HwaTuCard>(cardSOs.Length);

            foreach (var so in cardSOs)
            {
                cards.Add(so.ToHwaTuCard());
            }

            Debug.Log($"[HwaTuCardDatabase] SO 에셋에서 {cards.Count}장 로드 완료.");
            return cards;
        }

        /// <summary>
        /// CardId로 카드를 찾는다.
        /// 매번 전체 로드하므로, 반복 호출 시에는 CreateAllCards() 결과를 캐싱하여 사용할 것.
        /// </summary>
        public static HwaTuCard FindById(string cardId)
        {
            var allCards = CreateAllCards();
            return allCards.Find(c => c.CardId == cardId);
        }
    }
}
