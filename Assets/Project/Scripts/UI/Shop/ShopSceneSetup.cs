using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FFF.Core;
using FFF.Data;
using FFF.UI.Battle;
using FFF.UI.Core;

namespace FFF.UI.Shop
{
    /// <summary>
    /// ShopScene 진입 시 상점 UI를 준비한다.
    /// 씬에 직접 배치된 UI가 없으면 프로토타입 UI를 런타임에 생성한다.
    /// </summary>
    public class ShopSceneSetup : MonoBehaviour
    {
        private sealed class ShopItemDefinition
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string Category;
            public readonly string Description;
            public readonly int Price;
            public readonly Sprite Artwork;
            public readonly ShopUIComponent.ShopItemKind Kind;
            public readonly string PayloadId;
            public readonly HwaTuCard CardData;

            public ShopItemDefinition(
                string id,
                string displayName,
                string category,
                string description,
                int price,
                Sprite artwork = null,
                ShopUIComponent.ShopItemKind kind = ShopUIComponent.ShopItemKind.Accessory,
                string payloadId = null,
                HwaTuCard cardData = null)
            {
                Id = id;
                DisplayName = displayName;
                Category = category;
                Description = description;
                Price = price;
                Artwork = artwork;
                Kind = kind;
                PayloadId = payloadId;
                CardData = cardData;
            }
        }

        [Serializable]
        private sealed class ShopAccessoryOffer
        {
            public string Id;
            public string DisplayName;
            public string Description;
            public int Price = 100;
            public Sprite Icon = null;
        }

        [SerializeField] private ShopUIComponent _shopUI;
        [SerializeField] private bool _buildRuntimeUIIfMissing = true;

        [Header("교체용 이미지")]
        [SerializeField] private Sprite _backgroundSprite;
        [SerializeField] private Sprite _merchantCatSprite;
        [SerializeField] private Sprite _dialogueBoxSprite;

        [Header("폰트")]
        [SerializeField] private TMP_FontAsset _fontAsset;

        [Header("상인 대화")]
        [SerializeField, TextArea(2, 3)]
        private string _merchantIntroText = "고양이 한 마리가 말 없이 당신을 바라보고 있습니다. 물건을 구할 수 있을 듯 합니다.";

        [Header("상점 구성")]
        [SerializeField] private GameObject _cardUIPrefab;
        [SerializeField] private int _cardOfferCount = 5;
        [SerializeField] private int _accessoryOfferCount = 2;
        [SerializeField] private int _cardRemovalPrice = 75;
        [SerializeField]
        private List<ShopAccessoryOffer> _accessoryOfferPool = new List<ShopAccessoryOffer>
        {
            new ShopAccessoryOffer
            {
                Id = "ACC_NORIGAE",
                DisplayName = "노리개",
                Description = "전투 시작 시\n리롤 +1",
                Price = 88
            },
            new ShopAccessoryOffer
            {
                Id = "ACC_DAMAGE_BONUS",
                DisplayName = "은장도",
                Description = "승리 피해량\n+5",
                Price = 149
            },
            new ShopAccessoryOffer
            {
                Id = "ACC_JADE_RING",
                DisplayName = "옥가락지",
                Description = "족보 공격력\n소폭 증가",
                Price = 126
            },
            new ShopAccessoryOffer
            {
                Id = "ACC_GAT",
                DisplayName = "갓",
                Description = "첫 턴 방어적\n보정 획득",
                Price = 112
            },
            new ShopAccessoryOffer
            {
                Id = "ACC_MAPE",
                DisplayName = "마패",
                Description = "땡 공격 시 공격력 +50",
                Price = 150
            }
        };

        private static readonly Color Parchment = new Color(0.73f, 0.78f, 0.63f, 0.96f);
        private static readonly Color Ink = new Color(0.13f, 0.12f, 0.1f, 1f);

        private void Start()
        {
            if (_shopUI == null && _buildRuntimeUIIfMissing)
                _shopUI = BuildRuntimeShopUI();

            if (_shopUI == null)
            {
                Debug.LogError("[ShopSceneSetup] ShopUI를 찾을 수 없습니다.");
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShopSceneReady(_shopUI);
            }
            else
            {
                Debug.LogWarning("[ShopSceneSetup] GameManager가 없어 ShopScene 단독 테스트 모드로 표시합니다.");
                _shopUI.OnLeave = () => SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
                _shopUI.Initialize();
                _shopUI.Show();
            }
        }

        private void OnDestroy()
        {
            GameManager.Instance?.UnregisterScreen(UIScreenNames.SHOP);
        }

        private ShopUIComponent BuildRuntimeShopUI()
        {
            EnsureCamera();
            EnsureEventSystem();

            GameObject canvasGo = new GameObject("ShopCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject root = CreateUIObject("ShopUI", canvasGo.transform);
            StretchToParent(root.GetComponent<RectTransform>());
            root.AddComponent<CanvasGroup>();
            ShopUIComponent shopUI = root.AddComponent<ShopUIComponent>();

            CreateBackground(root.transform);

            Button passButton = CreateTextButton(
                "Button_PassShop",
                root.transform,
                "지나가기",
                new Vector2(154f, 44f),
                new Vector2(-82f, -38f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Color(0.12f, 0.15f, 0.18f, 0.62f),
                Color.white);

            Button merchantButton = BuildMerchant(root.transform);
            GameObject dialogueGroup = BuildDialogueGroup(root.transform);

            TextMeshProUGUI feedbackText = CreateText(
                "Text_Feedback",
                root.transform,
                "",
                23,
                TextAlignmentOptions.Center,
                Color.white,
                new Vector2(760f, 36f),
                new Vector2(0f, -322f));
            feedbackText.fontStyle = FontStyles.Bold;
            feedbackText.gameObject.SetActive(false);

            GameObject shopPanel = BuildShopPanel(root.transform, out Button closeButton, out Button leaveButton,
                out TextMeshProUGUI goldText, out List<ShopUIComponent.ShopItemBinding> itemBindings,
                out ShopUIComponent.CardRemovalPanelBinding cardRemovalPanel);

            shopUI.Bind(dialogueGroup, shopPanel, merchantButton, passButton, closeButton, leaveButton,
                goldText, feedbackText, itemBindings, cardRemovalPanel);

            return shopUI;
        }

        private void CreateBackground(Transform parent)
        {
            GameObject background = CreateImage("BackgroundImage", parent, new Color(255f, 255f, 255f, 255f));
            RectTransform rect = background.GetComponent<RectTransform>();
            StretchToParent(rect);

            Image image = background.GetComponent<Image>();
            image.sprite = _backgroundSprite;
            image.preserveAspect = false;
        }

        private Button BuildMerchant(Transform parent)
        {
            GameObject buttonGo = CreateUIObject("MerchantCatButton", parent);
            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500f, 430f);
            rect.anchoredPosition = new Vector2(0f, -34f);

            Image image = buttonGo.AddComponent<Image>();
            image.sprite = _merchantCatSprite;
            image.color = _merchantCatSprite == null
                ? new Color(0.95f, 0.72f, 0.48f, 0.92f)
                : Color.white;
            image.preserveAspect = true;

            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;

            return button;
        }

        private GameObject BuildDialogueGroup(Transform parent)
        {
            GameObject group = CreateUIObject("DialogueGroup", parent);
            RectTransform groupRect = group.GetComponent<RectTransform>();
            groupRect.anchorMin = new Vector2(0.5f, 0f);
            groupRect.anchorMax = new Vector2(0.5f, 0f);
            groupRect.pivot = new Vector2(0.5f, 0f);
            groupRect.sizeDelta = new Vector2(1040f, 142f);
            groupRect.anchoredPosition = new Vector2(0f, 24f);

            Image boxImage = group.AddComponent<Image>();
            boxImage.sprite = _dialogueBoxSprite;
            boxImage.color = _dialogueBoxSprite == null
                ? new Color(0.08f, 0.07f, 0.06f, 0.82f)
                : Color.white;
            boxImage.preserveAspect = false;
            boxImage.raycastTarget = false;

            if (_dialogueBoxSprite == null)
            {
                Outline outline = group.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.9f, 0.68f, 0.55f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            TextMeshProUGUI message = CreateText(
                "Text_DialogueMessage",
                group.transform,
                _merchantIntroText,
                24,
                TextAlignmentOptions.MidlineLeft,
                Color.white,
                new Vector2(900f, 86f),
                new Vector2(0f, 8f));
            message.fontStyle = FontStyles.Bold;
            message.overflowMode = TextOverflowModes.Ellipsis;

            return group;
        }

        private GameObject BuildShopPanel(
            Transform parent,
            out Button closeButton,
            out Button leaveButton,
            out TextMeshProUGUI goldText,
            out List<ShopUIComponent.ShopItemBinding> itemBindings,
            out ShopUIComponent.CardRemovalPanelBinding cardRemovalPanel)
        {
            GameObject panel = CreateImage("ShopBundlePanel", parent, Parchment);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1110f, 610f);
            panelRect.anchoredPosition = new Vector2(0f, -2f);
            panel.AddComponent<Shadow>().effectDistance = new Vector2(8f, -8f);

            TextMeshProUGUI title = CreateText("Text_ShopTitle", panel.transform, "상점보따리",
                34, TextAlignmentOptions.Left, Ink, new Vector2(360f, 48f), new Vector2(-334f, 268f));
            title.fontStyle = FontStyles.Bold;

            goldText = CreateText("Text_Gold", panel.transform, "엽전 216", 26,
                TextAlignmentOptions.Right, new Color(0.94f, 0.77f, 0.27f, 1f),
                new Vector2(220f, 44f), new Vector2(390f, 270f));
            goldText.fontStyle = FontStyles.Bold;

            closeButton = CreateTextButton("Button_CloseShop", panel.transform, "보따리 닫기",
                new Vector2(138f, 42f), new Vector2(454f, 268f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Color(0.24f, 0.28f, 0.25f, 0.92f), Color.white);

            GameObject gridGo = CreateUIObject("ShopItemGrid", panel.transform);
            RectTransform gridRect = gridGo.GetComponent<RectTransform>();
            gridRect.sizeDelta = new Vector2(920f, 446f);
            gridRect.anchoredPosition = new Vector2(18f, 2f);

            GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(166f, 206f);
            grid.spacing = new Vector2(22f, 24f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperLeft;

            itemBindings = new List<ShopUIComponent.ShopItemBinding>();
            foreach (ShopItemDefinition item in CreateShopItems())
            {
                itemBindings.Add(CreateShopItemCard(gridGo.transform, item));
            }

            leaveButton = CreateTextButton("Button_LeaveShop", panel.transform, "지나가기",
                new Vector2(188f, 54f), new Vector2(-462f, -266f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Color(0.55f, 0.14f, 0.13f, 1f), Color.white);

            cardRemovalPanel = BuildCardRemovalPanel(panel.transform);

            panel.SetActive(false);
            return panel;
        }

        private ShopUIComponent.CardRemovalPanelBinding BuildCardRemovalPanel(Transform parent)
        {
            GameObject panel = CreateImage("CardRemovalPanel", parent, new Color(0.16f, 0.17f, 0.15f, 0.96f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(960f, 500f);
            panelRect.anchoredPosition = new Vector2(0f, -8f);
            panel.AddComponent<Outline>().effectColor = new Color(0.88f, 0.78f, 0.5f, 0.8f);

            TextMeshProUGUI title = CreateText("Text_RemovalTitle", panel.transform, "삭제할 카드 선택",
                30, TextAlignmentOptions.Left, Color.white, new Vector2(360f, 44f), new Vector2(-260f, 218f));
            title.fontStyle = FontStyles.Bold;

            TextMeshProUGUI selectedText = CreateText("Text_SelectedRemovalCard", panel.transform, "삭제할 카드 1장을 선택하세요.",
                21, TextAlignmentOptions.Right, new Color(1f, 0.84f, 0.32f, 1f),
                new Vector2(360f, 38f), new Vector2(250f, 218f));
            selectedText.fontStyle = FontStyles.Bold;

            GameObject scrollRoot = CreateImage("RemovalScrollView", panel.transform, new Color(0.08f, 0.09f, 0.08f, 0.52f));
            RectTransform scrollRect = scrollRoot.GetComponent<RectTransform>();
            scrollRect.sizeDelta = new Vector2(900f, 330f);
            scrollRect.anchoredPosition = new Vector2(0f, 22f);

            ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;

            GameObject viewport = CreateImage("Viewport", scrollRoot.transform, new Color(0f, 0f, 0f, 0f));
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            StretchToParent(viewportRect);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(0f, 0.5f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.sizeDelta = new Vector2(900f, 300f);
            contentRect.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup layout = content.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 18f;
            layout.padding = new RectOffset(24, 24, 0, 0);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            Button cancelButton = CreateTextButton("Button_CancelRemoval", panel.transform, "취소",
                new Vector2(128f, 44f), new Vector2(250f, -218f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Color(0.29f, 0.3f, 0.28f, 1f), Color.white);

            Button confirmButton = CreateTextButton("Button_ConfirmRemoval", panel.transform, "삭제",
                new Vector2(128f, 44f), new Vector2(390f, -218f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Color(0.62f, 0.18f, 0.16f, 1f), Color.white);
            confirmButton.interactable = false;

            panel.SetActive(false);
            return new ShopUIComponent.CardRemovalPanelBinding
            {
                Panel = panel,
                CardGrid = content.transform,
                ConfirmButton = confirmButton,
                CancelButton = cancelButton,
                SelectedText = selectedText,
                CardPrefab = _cardUIPrefab
            };
        }

        private ShopUIComponent.ShopItemBinding CreateShopItemCard(Transform parent, ShopItemDefinition item)
        {
            GameObject card = CreateImage($"Item_{item.Id}", parent, new Color(0.23f, 0.24f, 0.29f, 1f));
            card.AddComponent<Outline>().effectColor = new Color(0.55f, 0.42f, 0.7f, 1f);
            Button button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();

            if (item.Kind == ShopUIComponent.ShopItemKind.Card)
            {
                CreateCardOfferVisual(card.transform, item.CardData, item.Artwork, button);
            }
            else
            {
                TextMeshProUGUI name = CreateText("Text_Name", card.transform, item.DisplayName, 18,
                    TextAlignmentOptions.Center, Color.white, new Vector2(152f, 32f), new Vector2(0f, 76f));
                name.fontStyle = FontStyles.Bold;

                GameObject art = CreateImage("ItemArt", card.transform, GetItemColor(item.Category));
                RectTransform artRect = art.GetComponent<RectTransform>();
                artRect.sizeDelta = new Vector2(132f, 74f);
                artRect.anchoredPosition = new Vector2(0f, 26f);

                Image artImage = art.GetComponent<Image>();
                if (item.Artwork != null)
                {
                    artImage.sprite = item.Artwork;
                    artImage.color = Color.white;
                    artImage.preserveAspect = true;
                }
                else
                {
                    CreateText("Text_Category", art.transform, item.Category, 18, TextAlignmentOptions.Center,
                        Color.white, new Vector2(126f, 32f), Vector2.zero).fontStyle = FontStyles.Bold;
                }

                CreateText("Text_Description", card.transform, item.Description, 15,
                    TextAlignmentOptions.Center, new Color(0.95f, 0.92f, 0.84f, 1f),
                    new Vector2(146f, 70f), new Vector2(0f, -42f));
            }

            TextMeshProUGUI price = CreateText("Text_Price", card.transform, $"{item.Price}전", 19,
                TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.32f, 1f),
                new Vector2(100f, 34f), new Vector2(0f, -116f));
            price.fontStyle = FontStyles.Bold;

            TextMeshProUGUI state = CreateText("Text_State", card.transform, "", 17,
                TextAlignmentOptions.Center, Color.white, new Vector2(126f, 34f), new Vector2(0f, -6f));
            state.fontStyle = FontStyles.Bold;

            GameObject soldOverlay = CreateImage("SoldOverlay", card.transform, new Color(0f, 0f, 0f, 0.58f));
            StretchToParent(soldOverlay.GetComponent<RectTransform>());
            soldOverlay.SetActive(false);

            return new ShopUIComponent.ShopItemBinding
            {
                Id = item.Id,
                DisplayName = item.DisplayName,
                Kind = item.Kind,
                PayloadId = item.PayloadId,
                Price = item.Price,
                Button = button,
                PriceText = price,
                StateText = state,
                SoldOverlay = soldOverlay
            };
        }

        private void CreateCardOfferVisual(Transform parent, HwaTuCard cardData, Sprite artwork, Button purchaseButton)
        {
            if (_cardUIPrefab == null || cardData == null)
            {
                TextMeshProUGUI name = CreateText("Text_CardName", parent, cardData != null ? cardData.DisplayName : "화투 카드",
                    20, TextAlignmentOptions.Center, Color.white, new Vector2(144f, 72f), new Vector2(0f, 12f));
                name.fontStyle = FontStyles.Bold;
                return;
            }

            GameObject cardObject = Instantiate(_cardUIPrefab, parent, false);
            cardObject.name = $"CardVisual_{cardData.CardId}";

            RectTransform rect = cardObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(112f, 168f);
                rect.anchoredPosition = new Vector2(0f, 15f);
            }

            CardUIComponent cardView = cardObject.GetComponent<CardUIComponent>();
            if (cardView != null)
                cardView.Setup(cardData, _ => { }, artwork);

            ConfigureShopCardName(cardObject);

            Button childButton = cardObject.GetComponent<Button>();
            if (childButton != null && purchaseButton != null)
            {
                childButton.onClick.RemoveAllListeners();
                childButton.onClick.AddListener(() => purchaseButton.onClick.Invoke());
            }
        }

        private void ConfigureShopCardName(GameObject cardObject)
        {
            TextMeshProUGUI nameText = cardObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (nameText == null)
                return;

            nameText.enableAutoSizing = false;
            nameText.fontSize = 13f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform rect = nameText.GetComponent<RectTransform>();
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(132f, 24f);
            rect.anchoredPosition = new Vector2(0f, -8f);
        }

        private List<ShopItemDefinition> CreateShopItems()
        {
            var items = new List<ShopItemDefinition>();
            AddRandomCardOffers(items);
            AddRandomAccessoryOffers(items);
            items.Add(new ShopItemDefinition(
                "SERVICE_CARD_REMOVAL",
                "카드 제거",
                "서비스",
                "덱에서 카드 1장을\n제거합니다.",
                _cardRemovalPrice,
                kind: ShopUIComponent.ShopItemKind.CardRemoval));
            return items;
        }

        private void AddRandomCardOffers(List<ShopItemDefinition> items)
        {
            List<HwaTuCard> cards = HwaTuCardDatabase.CreateAllCards();
            Shuffle(cards);

            int count = Mathf.Min(Mathf.Max(0, _cardOfferCount), cards.Count);
            for (int i = 0; i < count; i++)
            {
                HwaTuCard card = cards[i];
                items.Add(new ShopItemDefinition(
                    $"CARD_{card.CardId}",
                    card.DisplayName,
                    "화투",
                    string.Empty,
                    GetCardPrice(card),
                    HwaTuCardDatabase.GetArtwork(card.CardId),
                    ShopUIComponent.ShopItemKind.Card,
                    card.CardId,
                    card));
            }
        }

        private void AddRandomAccessoryOffers(List<ShopItemDefinition> items)
        {
            var offers = new List<ShopAccessoryOffer>();
            foreach (ShopAccessoryOffer offer in _accessoryOfferPool)
            {
                if (offer != null && !string.IsNullOrEmpty(offer.Id))
                    offers.Add(offer);
            }

            Shuffle(offers);

            int count = Mathf.Min(Mathf.Max(0, _accessoryOfferCount), offers.Count);
            for (int i = 0; i < count; i++)
            {
                ShopAccessoryOffer offer = offers[i];
                items.Add(new ShopItemDefinition(
                    $"ACCESSORY_{offer.Id}",
                    offer.DisplayName,
                    "장신구",
                    offer.Description,
                    offer.Price,
                    offer.Icon,
                    ShopUIComponent.ShopItemKind.Accessory,
                    offer.Id));
            }
        }

        private static int GetCardPrice(HwaTuCard card)
        {
            if (card == null)
                return 80;

            return card.Type switch
            {
                CardType.Gwang => 149,
                CardType.Yeolkkeut => 97,
                CardType.Tti => 88,
                CardType.Pi => 58,
                _ => 80
            };
        }

        private static void Shuffle<T>(IList<T> list)
        {
            if (list == null)
                return;

            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        private static Color GetItemColor(string category)
        {
            switch (category)
            {
                case "카드":
                    return new Color(0.31f, 0.55f, 0.78f, 1f);
                case "장신구":
                    return new Color(0.78f, 0.45f, 0.24f, 1f);
                case "조커":
                    return new Color(0.5f, 0.31f, 0.67f, 1f);
                default:
                    return new Color(0.35f, 0.62f, 0.42f, 1f);
            }
        }

        private Button CreateTextButton(
            string name,
            Transform parent,
            string label,
            Vector2 size,
            Vector2 anchoredPosition,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color background,
            Color textColor)
        {
            GameObject go = CreateImage(name, parent, background);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Button button = go.AddComponent<Button>();
            CreateText($"Text_{name}", go.transform, label, 20, TextAlignmentOptions.Center,
                textColor, size, Vector2.zero).fontStyle = FontStyles.Bold;
            return button;
        }

        private TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            int fontSize,
            TextAlignmentOptions alignment,
            Color color,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            GameObject go = CreateUIObject(name, parent);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            if (_fontAsset != null)
                label.font = _fontAsset;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return label;
        }

        private GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            return go;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
                return;

            GameObject cameraGo = new GameObject("Main Camera");
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }
    }
}
