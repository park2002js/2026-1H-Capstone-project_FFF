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
        private const string CARD_ATLAS_RESOURCE_PATH = "Cards/cards_list";
        private const string CARD_ASSET_FOLDER = "Assets/Project/Resources/Cards";
        private const string CARD_ATLAS_ASSET_PATH = "Assets/Project/Resources/Cards/cards_list.png";

        private static Dictionary<string, Sprite> _flowerCardAtlasSpritesByName;
        private static bool _flowerCardAtlasLoaded;

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

        /// <summary>
        /// CardId의 앞면 Sprite를 SO 아트워크 → Resources 아틀라스 → 에디터 전용 프리팹/아틀라스 순으로 최대한 찾아 반환한다.
        /// 배틀/스테이지 등 모든 화면이 동일한 방식으로 카드 이미지를 얻도록 통일하는 진입점.
        /// </summary>
        public static Sprite ResolveArtwork(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;

            Sprite artwork = GetArtwork(cardId);
            if (artwork != null)
                return artwork;

            artwork = LoadFlowerCardAtlasSpriteFromResources(cardId);
            if (artwork != null)
                return artwork;

#if UNITY_EDITOR
            artwork = LoadFlowerCardFrontSpriteInEditor(cardId);
            if (artwork != null)
                return artwork;

            HwaTuCard cardData = FindById(cardId);
            if (cardData != null)
            {
                artwork = LoadFlowerCardAtlasSpriteInEditor(cardData);
                if (artwork != null)
                    return artwork;
            }
#endif

            return null;
        }

        private static Sprite LoadFlowerCardAtlasSpriteFromResources(string cardId)
        {
            string spriteName = GetFlowerCardAtlasSpriteName(cardId);
            if (string.IsNullOrEmpty(spriteName))
                return null;

            EnsureFlowerCardAtlasLoaded();
            return _flowerCardAtlasSpritesByName != null
                && _flowerCardAtlasSpritesByName.TryGetValue(spriteName, out Sprite sprite)
                    ? sprite
                    : null;
        }

        private static void EnsureFlowerCardAtlasLoaded()
        {
            if (_flowerCardAtlasLoaded)
                return;

            _flowerCardAtlasLoaded = true;
            _flowerCardAtlasSpritesByName = new Dictionary<string, Sprite>();

            Sprite[] sprites = Resources.LoadAll<Sprite>(CARD_ATLAS_RESOURCE_PATH);
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"[HwaTuCardDatabase] 카드 아틀라스 스프라이트를 찾지 못했습니다. Resources/{CARD_ATLAS_RESOURCE_PATH} 확인 필요.");
                return;
            }

            foreach (Sprite sprite in sprites)
            {
                if (sprite != null && !_flowerCardAtlasSpritesByName.ContainsKey(sprite.name))
                    _flowerCardAtlasSpritesByName.Add(sprite.name, sprite);
            }
        }

        private static string GetFlowerCardAtlasSpriteName(string cardId)
        {
            switch (cardId)
            {
                case "M1_Gwang": return "cards_list_0";
                case "M1_Pi": return "cards_list_2";
                case "M2_Yeolkkeut": return "cards_list_4";
                case "M2_Pi": return "cards_list_7";
                case "M3_Gwang": return "cards_list_9";
                case "M3_Pi": return "cards_list_11";
                case "M4_Yeolkkeut": return "cards_list_13";
                case "M4_Pi": return "cards_list_16";
                case "M5_Yeolkkeut": return "cards_list_18";
                case "M5_Pi": return "cards_list_20";
                case "M6_Yeolkkeut": return "cards_list_22";
                case "M6_Pi": return "cards_list_24";
                case "M7_Yeolkkeut": return "cards_list_27";
                case "M7_Pi": return "cards_list_29";
                case "M8_Gwang": return "cards_list_31";
                case "M8_Pi": return "cards_list_33";
                case "M9_Yeolkkeut": return "cards_list_36";
                case "M9_Pi": return "cards_list_38";
                case "M10_Yeolkkeut": return "cards_list_40";
                case "M10_Pi": return "cards_list_42";
                default: return null;
            }
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

#if UNITY_EDITOR
        private static Sprite LoadFlowerCardFrontSpriteInEditor(string cardId)
        {
            string prefabName = GetFlowerCardPrefabName(cardId);
            if (string.IsNullOrEmpty(prefabName))
                return null;

            string path = $"Assets/Project/Prefabs/FlowerCards/Prefabs/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return null;

            SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null && renderer.gameObject.name == "Front")
                    return renderer.sprite;
            }

            return null;
        }

        private static string GetFlowerCardPrefabName(string cardId)
        {
            switch (cardId)
            {
                case "M1_Gwang": return "01_Jan1";
                case "M1_Pi": return "01_Jan3";
                case "M2_Yeolkkeut": return "02_Feb1";
                case "M2_Pi": return "02_Feb3";
                case "M3_Gwang": return "03_Mar1";
                case "M3_Pi": return "03_Mar3";
                case "M4_Yeolkkeut": return "04_Apr1";
                case "M4_Pi": return "04_Apr3";
                case "M5_Yeolkkeut": return "05_May1";
                case "M5_Pi": return "05_May3";
                case "M6_Yeolkkeut": return "06_Jun1";
                case "M6_Pi": return "06_Jun3";
                case "M7_Yeolkkeut": return "07_Jul1";
                case "M7_Pi": return "07_Jul3";
                case "M8_Gwang": return "08_Aug1";
                case "M8_Pi": return "08_Aug3";
                case "M9_Yeolkkeut": return "09_Sep1";
                case "M9_Pi": return "09_Sep3";
                case "M10_Yeolkkeut": return "10_Oct1";
                case "M10_Pi": return "10_Oct3";
                default: return null;
            }
        }

        private static Sprite LoadFlowerCardAtlasSpriteInEditor(HwaTuCard cardData)
        {
            string spriteName = GetFlowerCardAtlasSpriteName(cardData);
            if (string.IsNullOrEmpty(spriteName))
                return null;

            const string path = CARD_ATLAS_ASSET_PATH;
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            }

            return null;
        }

        private static string GetFlowerCardAtlasSpriteName(HwaTuCard cardData)
        {
            if (cardData == null) return null;

            switch (cardData.CardId)
            {
                case "M1_Gwang": return "cards_list_0";
                case "M1_Pi": return "cards_list_2";
                case "M2_Yeolkkeut": return "cards_list_4";
                case "M2_Pi": return "cards_list_7";
                case "M3_Gwang": return "cards_list_9";
                case "M3_Pi": return "cards_list_11";
                case "M4_Yeolkkeut": return "cards_list_13";
                case "M4_Pi": return "cards_list_16";
                case "M5_Yeolkkeut": return "cards_list_18";
                case "M5_Pi": return "cards_list_20";
                case "M6_Yeolkkeut": return "cards_list_22";
                case "M6_Pi": return "cards_list_24";
                case "M7_Yeolkkeut": return "cards_list_27";
                case "M7_Pi": return "cards_list_29";
                case "M8_Gwang": return "cards_list_31";
                case "M8_Pi": return "cards_list_33";
                case "M9_Yeolkkeut": return "cards_list_36";
                case "M9_Pi": return "cards_list_38";
                case "M10_Yeolkkeut": return "cards_list_40";
                case "M10_Pi": return "cards_list_42";
            }

            return GetFlowerCardAtlasSpriteNameByMonthAndType(cardData);
        }

        private static string GetFlowerCardAtlasSpriteNameByMonthAndType(HwaTuCard cardData)
        {
            int month = (int)cardData.Month;
            switch (cardData.Type)
            {
                case CardType.Gwang:
                    if (month == 1) return "cards_list_0";
                    if (month == 3) return "cards_list_9";
                    if (month == 8) return "cards_list_31";
                    break;
                case CardType.Yeolkkeut:
                    if (month == 2) return "cards_list_4";
                    if (month == 4) return "cards_list_13";
                    if (month == 5) return "cards_list_18";
                    if (month == 6) return "cards_list_22";
                    if (month == 7) return "cards_list_27";
                    if (month == 9) return "cards_list_36";
                    if (month == 10) return "cards_list_40";
                    break;
                case CardType.Pi:
                    if (month == 1) return "cards_list_2";
                    if (month == 2) return "cards_list_7";
                    if (month == 3) return "cards_list_11";
                    if (month == 4) return "cards_list_16";
                    if (month == 5) return "cards_list_20";
                    if (month == 6) return "cards_list_24";
                    if (month == 7) return "cards_list_29";
                    if (month == 8) return "cards_list_33";
                    if (month == 9) return "cards_list_38";
                    if (month == 10) return "cards_list_42";
                    break;
            }

            return null;
        }
#endif
    }
}
