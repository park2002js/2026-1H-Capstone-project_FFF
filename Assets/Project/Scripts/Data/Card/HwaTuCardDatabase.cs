using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FFF.Data
{
    /// <summary>
    /// 화투 카드 SO 원본을 런타임 카드 데이터로 복사하여 제공한다.
    /// </summary>
    public static class HwaTuCardDatabase
    {
        private const string CARDS_RESOURCE_PATH = "Cards";
        private const string CARD_ASSET_FOLDER = "Assets/Project/ScriptableObjects/Cards";

        private static readonly string[] DefaultInitialDeckIds =
        {
            "M1_Pi",
            "M2_Yeolkkeut",
            "M3_Pi",
            "M4_Yeolkkeut",
            "M5_Yeolkkeut",
            "M6_Yeolkkeut",
            "M7_Yeolkkeut",
            "M8_Pi",
            "M9_Yeolkkeut",
            "M10_Yeolkkeut"
        };

        /// <summary>
        /// 모든 HwaTuCardSO 원본을 새 HwaTuCard 인스턴스로 복사한다.
        /// </summary>
        public static List<HwaTuCard> CreateAllCards()
        {
            HwaTuCardSO[] cardSOs = LoadCardSOs();

            if (cardSOs == null || cardSOs.Length == 0)
            {
                Debug.LogError($"[HwaTuCardDatabase] 카드 SO 에셋을 찾지 못했습니다. Resources/{CARDS_RESOURCE_PATH}/ 또는 {CARD_ASSET_FOLDER} 확인 필요.");
                return new List<HwaTuCard>();
            }

            var cards = new List<HwaTuCard>(cardSOs.Length);

            foreach (var so in cardSOs)
            {
                cards.Add(CreateCardCopy(so));
            }

            Debug.Log($"[HwaTuCardDatabase] SO 에셋에서 {cards.Count}장 로드 완료.");
            return cards;
        }

        /// <summary>
        /// 카드 ID 목록 순서대로 새 HwaTuCard 인스턴스를 만든다.
        /// 같은 ID가 여러 번 들어오면 같은 원본 SO에서 여러 장이 복사된다.
        /// </summary>
        public static List<HwaTuCard> CreateCardsFromIds(IReadOnlyList<string> cardIds)
        {
            var cards = new List<HwaTuCard>();
            if (cardIds == null || cardIds.Count == 0) return cards;

            HwaTuCardSO[] cardSOs = LoadCardSOs();
            if (cardSOs == null || cardSOs.Length == 0)
            {
                Debug.LogError($"[HwaTuCardDatabase] 카드 SO 에셋을 찾지 못했습니다. {CARD_ASSET_FOLDER} 확인 필요.");
                return cards;
            }

            foreach (string cardId in cardIds)
            {
                if (string.IsNullOrEmpty(cardId)) continue;

                HwaTuCardSO source = FindSourceById(cardSOs, cardId);
                if (source == null)
                {
                    Debug.LogWarning($"[HwaTuCardDatabase] CardId '{cardId}'에 해당하는 카드 SO가 없습니다.");
                    continue;
                }

                cards.Add(CreateCardCopy(source));
            }

            return cards;
        }

        /// <summary>
        /// 1~10월이 각각 1장씩 들어간 기본 초기 덱을 만든다.
        /// </summary>
        public static List<HwaTuCard> CreateDefaultInitialDeck()
        {
            return CreateCardsFromIds(DefaultInitialDeckIds);
        }

        /// <summary>
        /// CardId로 카드 복사본을 찾는다.
        /// </summary>
        public static HwaTuCard FindById(string cardId)
        {
            return CreateCardsFromIds(new[] { cardId }).Find(c => c.CardId == cardId);
        }

        /// <summary>
        /// CardId에 연결된 카드 앞면 Sprite를 반환한다.
        /// </summary>
        public static Sprite GetArtwork(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;

            HwaTuCardSO[] cardSOs = LoadCardSOs();
            if (cardSOs == null || cardSOs.Length == 0) return null;

            return FindSourceById(cardSOs, cardId)?.Artwork;
        }

        private static HwaTuCard CreateCardCopy(HwaTuCardSO source)
        {
            return source.ToHwaTuCard();
        }

        private static HwaTuCardSO FindSourceById(IReadOnlyList<HwaTuCardSO> cardSOs, string cardId)
        {
            foreach (HwaTuCardSO so in cardSOs)
            {
                if (so != null && so.CardId == cardId)
                    return so;
            }

            return null;
        }

        private static HwaTuCardSO[] LoadCardSOs()
        {
            HwaTuCardSO[] cardSOs = Resources.LoadAll<HwaTuCardSO>(CARDS_RESOURCE_PATH);
            if (cardSOs != null && cardSOs.Length > 0)
                return cardSOs;

#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:HwaTuCardSO", new[] { CARD_ASSET_FOLDER });
            var loaded = new List<HwaTuCardSO>(guids.Length);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                HwaTuCardSO so = AssetDatabase.LoadAssetAtPath<HwaTuCardSO>(path);
                if (so != null)
                    loaded.Add(so);
            }

            loaded.Sort((a, b) => string.CompareOrdinal(a.CardId, b.CardId));
            return loaded.ToArray();
#else
            return cardSOs;
#endif
        }
    }
}
