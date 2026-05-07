using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FFF.Data;
using FFF.UI.Animation;
using FFF.Audio;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FFF.UI.Battle
{
    /// <summary>
    /// 개별 카드 UI를 담당. 클릭 시 시각적 피드백(애니메이션 연출)을 줍니다.
    /// </summary>
    public class CardUIComponent : MonoBehaviour
    {
        [Serializable]
        private class CardArtworkDefinition
        {
            [SerializeField] private string _cardId;
            [SerializeField] private Sprite _artwork;

            public string CardId => _cardId;
            public Sprite Artwork => _artwork;
        }

        [SerializeField] private List<CardArtworkDefinition> _artworkDefinitions = new();
        [SerializeField] private Image _cardImage;
        [SerializeField] private TextMeshProUGUI _cardNameText;
        [SerializeField] private Button _cardButton;

        public HwaTuCard CardData { get; private set; }
        private Action<CardUIComponent> _onClickCallback;

        public void Setup(HwaTuCard cardData, Action<CardUIComponent> onClickCallback, Sprite artworkOverride = null)
        {
            CardData = cardData;
            _onClickCallback = onClickCallback;
            EnsureReferences();

            Sprite artwork = ResolveArtwork(cardData, artworkOverride);

            if (_cardImage != null && artwork != null)
            {
                _cardImage.sprite = artwork;
                _cardImage.type = Image.Type.Simple;
                _cardImage.preserveAspect = true;
                _cardImage.color = Color.white;
            }

            if (_cardNameText != null)
            {
                _cardNameText.text = cardData.DisplayName;
                _cardNameText.gameObject.SetActive(true);
            }

            if (_cardButton != null)
            {
                _cardButton.onClick.RemoveAllListeners();
                _cardButton.onClick.AddListener(HandleClick);
            }
            
            SetSelected(false); // 초기 상태는 선택 해제 (크기 1.0)
        }

        public void SetSelected(bool isSelected)
        {
            // 기존 즉시 스케일 (CardAnimator가 없을 경우의 폴백)
            transform.localScale = isSelected ? new Vector3(1.1f, 1.1f, 1.1f) : Vector3.one;

            // CardAnimator가 프리팹에 붙어있으면 부드러운 연출로 위임
            var animator = GetComponent<CardAnimator>();
            if (animator != null)
            {
                if (isSelected) animator.PlaySelect();
                else animator.PlayDeselect();
            }
        }

        private void HandleClick()
        {
            SoundManager.PlayDefaultUiClick();
            _onClickCallback?.Invoke(this);
        }

        private Sprite ResolveArtwork(HwaTuCard cardData, Sprite artworkOverride)
        {
            if (artworkOverride != null)
                return artworkOverride;

            Sprite artwork = FindLocalArtwork(cardData.CardId);
            if (artwork != null)
                return artwork;

            artwork = HwaTuCardDatabase.GetArtwork(cardData.CardId);
            if (artwork != null)
                return artwork;

#if UNITY_EDITOR
            artwork = LoadFlowerCardSpriteInEditor(cardData.CardId);
            if (artwork != null)
                return artwork;

            artwork = LoadFlowerCardAtlasSpriteInEditor(cardData);
            if (artwork != null)
                return artwork;
#endif

            Debug.LogWarning($"[CardUIComponent] 카드 이미지가 없어 텍스트로 표시합니다. CardId: {cardData.CardId}, Month: {cardData.Month}, Type: {cardData.Type}");
            return null;
        }

        private void EnsureReferences()
        {
            if (_cardImage == null)
                _cardImage = GetComponent<Image>();

            if (_cardButton == null)
                _cardButton = GetComponent<Button>();
        }

        private Sprite FindLocalArtwork(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || _artworkDefinitions == null)
                return null;

            foreach (CardArtworkDefinition definition in _artworkDefinitions)
            {
                if (definition != null && definition.CardId == cardId)
                    return definition.Artwork;
            }

            return null;
        }

#if UNITY_EDITOR
        private Sprite LoadFlowerCardSpriteInEditor(string cardId)
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

        private string GetFlowerCardPrefabName(string cardId)
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

        private Sprite LoadFlowerCardAtlasSpriteInEditor(HwaTuCard cardData)
        {
            string spriteName = GetFlowerCardAtlasSpriteName(cardData);
            if (string.IsNullOrEmpty(spriteName))
                return null;

            const string path = "Assets/Project/Prefabs/FlowerCards/Textures/cards_list.png";
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            }

            return null;
        }

        private string GetFlowerCardAtlasSpriteName(HwaTuCard cardData)
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

        private string GetFlowerCardAtlasSpriteNameByMonthAndType(HwaTuCard cardData)
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
