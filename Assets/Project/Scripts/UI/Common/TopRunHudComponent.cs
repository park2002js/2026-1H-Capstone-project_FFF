using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FFF.Data;
using FFF.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FFF.UI.Common
{
    public class TopRunHudComponent : MonoBehaviour
    {
        private static readonly Vector2 HudReferenceResolution = new Vector2(2560f, 1440f);
        private static readonly Vector2 JokerSlotSize = new Vector2(132f, 128f);
        private static readonly Vector2 JokerVisualSize = new Vector2(150f, 188f);
        private static readonly Vector2 AccessorySlotSize = new Vector2(100f, 100f);
        private static readonly Vector2 AccessoryVisualSize = new Vector2(160f, 160f);
        private GameObject _hudCanvasRoot;
        private GameObject _root;
        private GameObject _accessoryRow;
        private TextMeshProUGUI _healthText;
        private TextMeshProUGUI _goldText;
        private Transform _jokerSlotRoot;
        private Transform _accessorySlotRoot;
        private GameObject _deckOverlay;
        private GameObject _settingsOverlay;
        private readonly List<string> _currentDeckCardIds = new List<string>();

        private void OnEnable()
        {
            if (_hudCanvasRoot != null)
                _hudCanvasRoot.SetActive(true);

            if (_root != null)
            {
                _root.SetActive(true);
                _root.transform.SetAsLastSibling();
            }

            if (_accessoryRow != null)
                _accessoryRow.SetActive(true);
        }

        private void OnDisable()
        {
            if (_hudCanvasRoot != null)
                _hudCanvasRoot.SetActive(false);

            if (_root != null)
                _root.SetActive(false);

            if (_accessoryRow != null)
                _accessoryRow.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_hudCanvasRoot != null)
            {
                Destroy(_hudCanvasRoot);
                return;
            }

            if (_root != null)
                Destroy(_root);

            if (_accessoryRow != null)
                Destroy(_accessoryRow);
        }

        public void SetPlayerData(PlayerDataSO playerData)
        {
            if (playerData == null)
                return;

            EnsureBuilt();
            SetDeckCards(playerData.DeckCardIds);
            SetPlayerHealth(playerData.CurrentHealth, playerData.MaxHealth);
            SetPlayerGold(playerData.CurrentGold);
            SetupItemIcons(playerData.EquippedAccessoryIds, playerData.HeldJokerIds);
        }

        public void SetDeckCards(IReadOnlyList<string> deckCardIds)
        {
            _currentDeckCardIds.Clear();
            if (deckCardIds != null)
                _currentDeckCardIds.AddRange(deckCardIds);
        }

        public void SetPlayerHealth(int current, int max)
        {
            EnsureBuilt();
            if (_healthText != null)
                _healthText.text = $"{current}/{max}";
        }

        public void SetPlayerGold(int gold)
        {
            EnsureBuilt();
            if (_goldText != null)
                _goldText.text = gold.ToString();
        }

        public void SetupItemIcons(IReadOnlyList<string> accessoryIds, IReadOnlyList<string> jokerIds)
        {
            EnsureBuilt();

            ClearChildren(_jokerSlotRoot);
            ClearChildren(_accessorySlotRoot);

            int accessoryCount = accessoryIds != null ? accessoryIds.Count : 0;
            if (accessoryIds != null)
            {
                foreach (string accessoryId in accessoryIds)
                    CreateItemSlot(accessoryId, _accessorySlotRoot, isJoker: false);
            }

            if (accessoryCount == 0)
                CreateEmptySlot(_accessorySlotRoot, isJoker: false);

            int jokerCount = jokerIds != null ? Mathf.Min(jokerIds.Count, PlayerDataSO.MaxHeldJokerCount) : 0;
            for (int i = 0; i < jokerCount; i++)
                CreateItemSlot(jokerIds[i], _jokerSlotRoot, isJoker: true);

            for (int i = jokerCount; i < PlayerDataSO.MaxHeldJokerCount; i++)
                CreateEmptySlot(_jokerSlotRoot, isJoker: true);
        }

        private void EnsureBuilt()
        {
            if (_root != null)
            {
                if (_hudCanvasRoot != null)
                {
                    _hudCanvasRoot.SetActive(isActiveAndEnabled);
                    _hudCanvasRoot.transform.SetAsLastSibling();
                }

                _root.SetActive(isActiveAndEnabled);
                _root.transform.SetAsLastSibling();
                if (_accessoryRow != null)
                    _accessoryRow.SetActive(isActiveAndEnabled);
                return;
            }

            Transform parent = EnsureHudCanvasRoot();

            _root = CreateUIObject("TopRunHud", parent);
            RectTransform rootRect = _root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.sizeDelta = new Vector2(0f, 150f);
            rootRect.anchoredPosition = Vector2.zero;
            _root.transform.SetAsLastSibling();

            Image background = _root.AddComponent<Image>();
            background.color = new Color(0.08f, 0.12f, 0.15f, 0.92f);
            background.raycastTarget = false;

            BuildHealthBlock(_root.transform);
            BuildGoldBlock(_root.transform);
            BuildJokerBlock(_root.transform);
            BuildHudButtons(_root.transform);
            BuildAccessoryRow(parent);
        }

        private Transform EnsureHudCanvasRoot()
        {
            if (_hudCanvasRoot != null)
                return _hudCanvasRoot.transform;

            Canvas sourceCanvas = GetComponentInParent<Canvas>();

            _hudCanvasRoot = new GameObject("TopRunHudCanvas");
            _hudCanvasRoot.layer = gameObject.layer;
            _hudCanvasRoot.transform.SetAsLastSibling();

            Canvas canvas = _hudCanvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sourceCanvas != null ? sourceCanvas.sortingOrder + 100 : 100;

            CanvasScaler scaler = _hudCanvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            _hudCanvasRoot.AddComponent<GraphicRaycaster>();
            return _hudCanvasRoot.transform;
        }

        private void BuildHealthBlock(Transform parent)
        {
            TextMeshProUGUI label = CreateText("Text_HealthLabel", parent, "♥", 38,
                TextAlignmentOptions.Center, FontStyles.Bold);
            label.color = new Color(1f, 0.25f, 0.28f, 1f);
            SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(52f, 52f), new Vector2(38f, -1f));

            _healthText = CreateText("Text_TopHealth", parent, "0/0", 30,
                TextAlignmentOptions.Left, FontStyles.Bold);
            _healthText.color = new Color(1f, 0.35f, 0.35f, 1f);
            SetRect(_healthText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(220f, 52f), new Vector2(182f, -1f));
        }

        private void BuildGoldBlock(Transform parent)
        {
            TextMeshProUGUI label = CreateText("Text_GoldLabel", parent, "엽전", 30,
                TextAlignmentOptions.Center, FontStyles.Bold);
            label.color = new Color(1f, 0.82f, 0.2f, 1f);
            SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(84f, 52f), new Vector2(340f, -1f));

            _goldText = CreateText("Text_TopGold", parent, "0", 30,
                TextAlignmentOptions.Left, FontStyles.Bold);
            _goldText.color = new Color(1f, 0.84f, 0.28f, 1f);
            SetRect(_goldText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(132f, 52f), new Vector2(454f, -1f));
        }

        private void BuildJokerBlock(Transform parent)
        {
            GameObject block = CreateUIObject("JokerHudBlock", parent);
            SetRect(block.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(456f, 138f), new Vector2(756f, 0f));

            Image bg = block.AddComponent<Image>();
            bg.color = Color.clear;
            bg.raycastTarget = false;
            block.AddComponent<RectMask2D>();

            GameObject layoutGo = CreateUIObject("Layout_Jokers", block.transform);
            _jokerSlotRoot = layoutGo.transform;
            RectTransform layoutRect = layoutGo.GetComponent<RectTransform>();
            layoutRect.anchorMin = Vector2.zero;
            layoutRect.anchorMax = Vector2.one;
            layoutRect.offsetMin = new Vector2(16f, 5f);
            layoutRect.offsetMax = new Vector2(-16f, -5f);
            layoutRect.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup layout = layoutGo.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void BuildAccessoryRow(Transform parent)
        {
            _accessoryRow = CreateUIObject("AccessoryHudRow", parent);
            RectTransform rect = _accessoryRow.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 118f);
            rect.anchoredPosition = new Vector2(0f, -150f);

            Image bg = _accessoryRow.AddComponent<Image>();
            bg.color = Color.clear;
            bg.raycastTarget = false;

            GameObject layoutGo = CreateUIObject("Layout_Accessories", _accessoryRow.transform);
            _accessorySlotRoot = layoutGo.transform;
            RectTransform layoutRect = layoutGo.GetComponent<RectTransform>();
            layoutRect.anchorMin = Vector2.zero;
            layoutRect.anchorMax = Vector2.one;
            layoutRect.offsetMin = new Vector2(22f, 6f);
            layoutRect.offsetMax = new Vector2(-22f, -6f);

            HorizontalLayoutGroup layout = layoutGo.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void BuildHudButtons(Transform parent)
        {
            CreateHudButton("Button_TopDeck", parent, "덱", new Vector2(-158f, 0f), ShowDeckOverlay);
            CreateHudButton("Button_TopSettings", parent, "설정", new Vector2(-68f, 0f), ShowSettingsOverlay);
        }

        private Button CreateHudButton(string name, Transform parent, string label, Vector2 position, System.Action onClick)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(76f, 58f), position);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.2f, 0.23f, 0.95f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                SoundManager.PlayDefaultUiClick();
                onClick?.Invoke();
            });

            TextMeshProUGUI text = CreateText($"Text_{name}", go.transform, label, 21,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private void CreateItemSlot(string itemId, Transform parent, bool isJoker)
        {
            if (string.IsNullOrEmpty(itemId) || parent == null)
                return;

            GameObject slot = CreateUIObject(itemId, parent);
            ConfigureSlot(slot, isJoker);

            Image background = slot.AddComponent<Image>();
            background.color = Color.clear;
            background.raycastTarget = false;

            Sprite sprite = ResolveBuiltInItemSprite(itemId);
            if (sprite != null)
            {
                GameObject visual = CreateUIObject("HudItemVisual", slot.transform);
                RectTransform visualRect = visual.GetComponent<RectTransform>();
                visualRect.anchorMin = new Vector2(0.5f, 0.5f);
                visualRect.anchorMax = new Vector2(0.5f, 0.5f);
                visualRect.pivot = new Vector2(0.5f, 0.5f);
                visualRect.sizeDelta = isJoker ? JokerVisualSize : AccessoryVisualSize;
                visualRect.anchoredPosition = Vector2.zero;

                Image image = visual.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = Color.white;
                return;
            }

            TextMeshProUGUI fallback = CreateText("Text_ItemFallback", slot.transform, GetFallbackLabel(itemId), 18,
                TextAlignmentOptions.Center, FontStyles.Bold);
            fallback.color = new Color(1f, 0.91f, 0.62f, 1f);
            SetStretch(fallback.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void CreateEmptySlot(Transform parent, bool isJoker)
        {
            if (parent == null)
                return;

            GameObject slot = CreateUIObject(isJoker ? "EmptyJokerSlot" : "EmptyAccessorySlot", parent);
            Image image = slot.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            ConfigureSlot(slot, isJoker);
        }

        private static void ConfigureSlot(GameObject slot, bool isJoker)
        {
            Vector2 size = isJoker ? JokerSlotSize : AccessorySlotSize;

            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            LayoutElement layout = slot.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            layout.minWidth = size.x;
            layout.minHeight = size.y;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        private static Sprite ResolveBuiltInItemSprite(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

#if UNITY_EDITOR
            string path = itemId switch
            {
                "ACC_REROLL_BONUS" => "Assets/Project/Art/Accessories/norigae.png",
                "ACC_DAMAGE_BONUS" => "Assets/Project/Art/Accessories/silverknife.png",
                "ACC_JADE_RING" => "Assets/Project/Art/Accessories/jadering.png",
                "ACC_GAT" => "Assets/Project/Art/Accessories/gat.png",
                "ACC_MAPE" => "Assets/Project/Art/Accessories/mape.png",
                "ACC_NORIGAE" => "Assets/Project/Art/Accessories/norigae.png",
                "JKR_REROLL_BURST" => "Assets/Project/Art/Joker/boone.png",
                "JKR_HIGH_CARD" => "Assets/Project/Art/Joker/yangban.png",
                "JKR_DOUBLE_PIP" => "Assets/Project/Art/Joker/mokjoong.png",
                "JKR_LUCKY_CHARM" => "Assets/Project/Art/Joker/gaksi.png",
                _ => null
            };

            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }

        private static string GetFallbackLabel(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return "?";

            if (itemId.StartsWith("JKR_"))
                return "J";

            if (itemId.StartsWith("ACC_"))
                return "A";

            return "?";
        }

        private void ShowDeckOverlay()
        {
            if (_deckOverlay == null)
                BuildDeckOverlay();

            _deckOverlay.SetActive(true);
            _deckOverlay.transform.SetAsLastSibling();
            RefreshDeckOverlay();
        }

        private void BuildDeckOverlay()
        {
            _deckOverlay = CreateOverlayRoot("DeckOverlay");
            GameObject panel = CreatePanel(_deckOverlay.transform, "보유 덱", new Vector2(900f, 560f));

            GameObject scrollRoot = CreateUIObject("DeckScrollView", panel.transform);
            RectTransform scrollRect = scrollRoot.GetComponent<RectTransform>();
            SetRect(scrollRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(820f, 390f), new Vector2(0f, -10f));

            Image scrollBg = scrollRoot.AddComponent<Image>();
            scrollBg.color = new Color(0.05f, 0.06f, 0.07f, 0.62f);

            ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            GameObject viewport = CreateUIObject("Viewport", scrollRoot.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(12f, 12f);
            viewportRect.offsetMax = new Vector2(-12f, -12f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = CreateUIObject("DeckContent", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 390f);

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(110f, 178f);
            grid.spacing = new Vector2(14f, 24f);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            Button close = CreateOverlayButton("Button_CloseDeck", panel.transform, "닫기", new Vector2(0f, -234f), () => _deckOverlay.SetActive(false));
            close.transform.SetAsLastSibling();
            _deckOverlay.SetActive(false);
        }

        private void RefreshDeckOverlay()
        {
            EnsureDeckCardsForOverlay();

            Transform content = _deckOverlay != null ? _deckOverlay.transform.Find("Panel/DeckScrollView/Viewport/DeckContent") : null;
            ClearChildren(content);
            if (content == null)
                return;

            if (_currentDeckCardIds.Count == 0)
            {
                TextMeshProUGUI empty = CreateText("Text_EmptyDeck", content, "덱에 카드가 없습니다.", 22,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                SetStretch(empty.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                return;
            }

            foreach (string cardId in _currentDeckCardIds)
                CreateDeckCardTile(content, cardId);

            if (content is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        private void EnsureDeckCardsForOverlay()
        {
            if (_currentDeckCardIds.Count > 0)
                return;

            foreach (HwaTuCard card in HwaTuCardDatabase.CreateDefaultInitialDeck())
            {
                if (card != null && !string.IsNullOrEmpty(card.CardId))
                    _currentDeckCardIds.Add(card.CardId);
            }
        }

        private void CreateDeckCardTile(Transform parent, string cardId)
        {
            HwaTuCard card = HwaTuCardDatabase.FindById(cardId);
            GameObject tile = CreateUIObject($"DeckCard_{cardId}", parent);
            Image background = tile.AddComponent<Image>();
            background.color = new Color(0.16f, 0.18f, 0.22f, 1f);

            RectTransform rect = tile.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(110f, 178f);

            Sprite artwork = HwaTuCardDatabase.GetArtwork(cardId);
            if (artwork != null)
            {
                GameObject artObject = CreateUIObject("Image_Card", tile.transform);
                RectTransform artRect = artObject.GetComponent<RectTransform>();
                SetRect(artRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(90f, 124f), new Vector2(0f, -70f));
                Image art = artObject.AddComponent<Image>();
                art.sprite = artwork;
                art.preserveAspect = true;
                art.raycastTarget = false;
            }

            TextMeshProUGUI name = CreateText("Text_CardName", tile.transform, card != null ? card.DisplayName : cardId, 14,
                TextAlignmentOptions.Center, FontStyles.Bold);
            name.textWrappingMode = TextWrappingModes.Normal;
            SetRect(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(100f, 42f), new Vector2(0f, 25f));
        }

        private void ShowSettingsOverlay()
        {
            if (_settingsOverlay == null)
                BuildSettingsOverlay();

            _settingsOverlay.SetActive(true);
            _settingsOverlay.transform.SetAsLastSibling();
        }

        private void BuildSettingsOverlay()
        {
            _settingsOverlay = CreateOverlayRoot("RunSettingsOverlay");
            GameObject panel = CreatePanel(_settingsOverlay.transform, "환경 설정", new Vector2(660f, 460f));

            CreateSoundSlider(panel.transform, "전체 소리", SoundBus.Master, 118f);
            CreateSoundSlider(panel.transform, "배경음", SoundBus.Bgm, 56f);
            CreateSoundSlider(panel.transform, "효과음", SoundBus.Sfx, -6f);
            CreateSoundSlider(panel.transform, "UI 효과음", SoundBus.Ui, -68f);

            Button close = CreateOverlayButton("Button_CloseSettings", panel.transform, "닫기", new Vector2(0f, -178f), () => _settingsOverlay.SetActive(false));
            close.transform.SetAsLastSibling();
            _settingsOverlay.SetActive(false);
        }

        private void CreateSoundSlider(Transform parent, string label, SoundBus bus, float y)
        {
            SoundManager soundManager = SoundManager.EnsureExists();

            TextMeshProUGUI name = CreateText($"Text_{bus}Label", parent, label, 20,
                TextAlignmentOptions.Left, FontStyles.Bold);
            SetRect(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(140f, 34f), new Vector2(-205f, y));

            TextMeshProUGUI valueText = CreateText($"Text_{bus}Value", parent, ToPercent(soundManager.GetVolume(bus)), 19,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetRect(valueText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 34f), new Vector2(178f, y));

            Slider slider = CreateOverlaySlider($"Slider_{bus}", parent, soundManager.GetVolume(bus), new Vector2(250f, 24f), new Vector2(-20f, y));
            slider.onValueChanged.AddListener(value =>
            {
                soundManager.SetVolume(bus, value);
                valueText.text = ToPercent(value);
            });
        }

        private GameObject CreateOverlayRoot(string name)
        {
            Transform parent = EnsureHudCanvasRoot();

            GameObject overlay = CreateUIObject(name, parent);
            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;
            return overlay;
        }

        private GameObject CreatePanel(Transform parent, string title, Vector2 size)
        {
            GameObject panel = CreateUIObject("Panel", parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, Vector2.zero);

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.16f, 0.98f);

            TextMeshProUGUI titleText = CreateText("Text_Title", panel.transform, title, 30,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420f, 54f), new Vector2(0f, -42f));
            return panel;
        }

        private Button CreateOverlayButton(string name, Transform parent, string label, Vector2 position, System.Action onClick)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 46f), position);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.32f, 0.35f, 0.4f, 1f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                SoundManager.PlayDefaultUiClick();
                onClick?.Invoke();
            });

            TextMeshProUGUI text = CreateText($"Text_{name}", go.transform, label, 20,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetStretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private Slider CreateOverlaySlider(string name, Transform parent, float value, Vector2 size, Vector2 position)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);

            Slider slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(value);

            GameObject background = CreateUIObject("Background", go.transform);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.sizeDelta = new Vector2(0f, 8f);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.22f, 0.25f, 0.29f, 1f);

            GameObject fill = CreateUIObject("Fill", background.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(1f, 0.84f, 0.14f, 1f);

            GameObject handle = CreateUIObject("Handle", go.transform);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22f, 22f);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static string ToPercent(float value)
        {
            return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            int fontSize,
            TextAlignmentOptions alignment,
            FontStyles style)
        {
            GameObject go = CreateUIObject(name, parent);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            GameUIFont.Apply(label);
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void SetStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }
}
