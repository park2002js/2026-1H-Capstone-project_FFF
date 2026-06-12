using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FFF.Core;
using FFF.Data;
using FFF.UI.Common;
using FFF.UI.Core;

namespace FFF.UI.Treasure
{
    /// <summary>
    /// TreasureScene entry point. Builds the runtime UI when no authored UI is present.
    /// </summary>
    public class TreasureSceneSetup : MonoBehaviour
    {
        [SerializeField] private TreasureUIComponent _treasureUI;
        [SerializeField] private bool _buildRuntimeUIIfMissing = true;

        [Header("교체용 이미지")]
        [SerializeField] private Sprite _backgroundSprite;
        [SerializeField] private Sprite _chestSprite;

        [Header("폰트")]
        [SerializeField] private TMP_FontAsset _fontAsset;

        private static Sprite _generatedBackgroundSprite;
        private static Sprite _generatedChestSprite;
        private static Sprite _generatedGlowSprite;

        private static readonly Color Ink = new Color(0.19f, 0.12f, 0.04f, 1f);
        private static readonly Color Parchment = new Color(0.74f, 0.64f, 0.46f, 0.96f);

        private void Start()
        {
            if (_treasureUI == null && _buildRuntimeUIIfMissing)
                _treasureUI = BuildRuntimeTreasureUI();

            if (_treasureUI == null)
            {
                Debug.LogError("[TreasureSceneSetup] TreasureUI를 찾을 수 없습니다.");
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTreasureSceneReady(_treasureUI);
                return;
            }

            Debug.LogWarning("[TreasureSceneSetup] GameManager가 없어 TreasureScene 단독 테스트 모드로 표시합니다.");
            _treasureUI.SetReward(CreateStandaloneReward());
            _treasureUI.OnLeave = () => SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
            _treasureUI.OnRewardClaimed = _ => SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
            _treasureUI.Initialize();
            _treasureUI.Show();
        }

        private void OnDestroy()
        {
            GameManager.Instance?.UnregisterScreen(UIScreenNames.TREASURE);
        }

        private TreasureUIComponent BuildRuntimeTreasureUI()
        {
            EnsureCamera();
            EnsureEventSystem();

            GameObject canvasGo = new GameObject("TreasureCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject root = CreateUIObject("TreasureUI", canvasGo.transform);
            StretchToParent(root.GetComponent<RectTransform>());
            root.AddComponent<CanvasGroup>();
            TreasureUIComponent treasureUI = root.AddComponent<TreasureUIComponent>();

            CreateBackground(root.transform);
            TextMeshProUGUI titleText = CreateTitleRibbon(root.transform);
            TextMeshProUGUI messageText = CreateText(
                "Text_TreasureMessage",
                root.transform,
                "낡은 상자 안에서 희미한 빛이 새어 나옵니다.",
                24,
                TextAlignmentOptions.Center,
                new Color(0.96f, 0.91f, 0.78f, 1f),
                new Vector2(720f, 44f),
                new Vector2(0f, 184f));
            messageText.fontStyle = FontStyles.Bold;

            GameObject chestGroup = BuildChestGroup(root.transform, out Button chestButton);
            GameObject rewardGroup = BuildRewardGroup(root.transform, out Button rewardButton, out Image rewardIcon,
                out TextMeshProUGUI rewardFallbackText, out TextMeshProUGUI rewardNameText,
                out TextMeshProUGUI rewardDescriptionText, out TextMeshProUGUI rewardHintText);
            Button passButton = CreateTextButton(
                "Button_PassTreasure",
                root.transform,
                "지나가기",
                new Vector2(158f, 50f),
                new Vector2(-94f, 58f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Color(0.12f, 0.15f, 0.16f, 0.82f),
                Color.white);

            treasureUI.Bind(chestGroup, rewardGroup, chestButton, rewardButton, passButton, rewardIcon,
                titleText, messageText, rewardFallbackText, rewardNameText, rewardDescriptionText, rewardHintText);
            return treasureUI;
        }

        private void CreateBackground(Transform parent)
        {
            GameObject background = CreateImage("BackgroundImage", parent, Color.white);
            StretchToParent(background.GetComponent<RectTransform>());

            Image image = background.GetComponent<Image>();
            image.sprite = _backgroundSprite != null ? _backgroundSprite : GetGeneratedBackgroundSprite();
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private TextMeshProUGUI CreateTitleRibbon(Transform parent)
        {
            GameObject ribbon = CreateImage("TitleRibbon", parent, Parchment);
            RectTransform rect = ribbon.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(420f, 68f);
            rect.anchoredPosition = new Vector2(0f, -70f);
            ribbon.AddComponent<Shadow>().effectDistance = new Vector2(4f, -4f);

            TextMeshProUGUI title = CreateText("Text_TreasureTitle", ribbon.transform, "보물 상자", 30,
                TextAlignmentOptions.Center, Ink, new Vector2(380f, 54f), Vector2.zero);
            title.fontStyle = FontStyles.Bold;
            return title;
        }

        private GameObject BuildChestGroup(Transform parent, out Button chestButton)
        {
            GameObject group = CreateUIObject("ChestGroup", parent);
            RectTransform groupRect = group.GetComponent<RectTransform>();
            groupRect.sizeDelta = new Vector2(430f, 330f);
            groupRect.anchoredPosition = new Vector2(0f, -34f);

            GameObject glow = CreateImage("ChestGlow", group.transform, Color.white);
            RectTransform glowRect = glow.GetComponent<RectTransform>();
            glowRect.sizeDelta = new Vector2(420f, 420f);
            glowRect.anchoredPosition = new Vector2(0f, 0f);
            Image glowImage = glow.GetComponent<Image>();
            glowImage.sprite = GetGeneratedGlowSprite();
            glowImage.raycastTarget = false;

            GameObject chestObject = CreateImage("Button_Chest", group.transform, Color.white);
            RectTransform chestRect = chestObject.GetComponent<RectTransform>();
            chestRect.sizeDelta = new Vector2(290f, 232f);
            chestRect.anchoredPosition = new Vector2(0f, -8f);
            Image chestImage = chestObject.GetComponent<Image>();
            chestImage.sprite = _chestSprite != null ? _chestSprite : GetGeneratedChestSprite();
            chestImage.preserveAspect = true;

            chestButton = chestObject.AddComponent<Button>();
            chestButton.targetGraphic = chestImage;

            TextMeshProUGUI label = CreateText("Text_OpenChest", group.transform, "열기", 22,
                TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.48f, 1f),
                new Vector2(120f, 34f), new Vector2(0f, -142f));
            label.fontStyle = FontStyles.Bold;
            return group;
        }

        private GameObject BuildRewardGroup(
            Transform parent,
            out Button rewardButton,
            out Image rewardIcon,
            out TextMeshProUGUI rewardFallbackText,
            out TextMeshProUGUI rewardNameText,
            out TextMeshProUGUI rewardDescriptionText,
            out TextMeshProUGUI rewardHintText)
        {
            GameObject group = CreateUIObject("RewardGroup", parent);
            RectTransform groupRect = group.GetComponent<RectTransform>();
            groupRect.sizeDelta = new Vector2(540f, 430f);
            groupRect.anchoredPosition = new Vector2(0f, -42f);

            GameObject glow = CreateImage("RewardGlow", group.transform, Color.white);
            RectTransform glowRect = glow.GetComponent<RectTransform>();
            glowRect.sizeDelta = new Vector2(460f, 460f);
            glowRect.anchoredPosition = new Vector2(0f, 42f);
            Image glowImage = glow.GetComponent<Image>();
            glowImage.sprite = GetGeneratedGlowSprite();
            glowImage.raycastTarget = false;

            GameObject itemButtonObject = CreateImage("Button_RewardItem", group.transform, new Color(0.08f, 0.09f, 0.09f, 0.84f));
            RectTransform itemRect = itemButtonObject.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(300f, 276f);
            itemRect.anchoredPosition = new Vector2(0f, 42f);
            itemButtonObject.AddComponent<Outline>().effectColor = new Color(1f, 0.78f, 0.26f, 0.7f);

            rewardButton = itemButtonObject.AddComponent<Button>();
            rewardButton.targetGraphic = itemButtonObject.GetComponent<Image>();

            GameObject iconObject = CreateUIObject("Image_RewardIcon", itemButtonObject.transform);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(190f, 190f);
            iconRect.anchoredPosition = new Vector2(0f, 24f);
            rewardIcon = iconObject.AddComponent<Image>();
            rewardIcon.preserveAspect = true;
            rewardIcon.raycastTarget = false;

            rewardFallbackText = CreateText("Text_RewardFallback", itemButtonObject.transform, "장신구", 27,
                TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.52f, 1f),
                new Vector2(230f, 70f), new Vector2(0f, 30f));
            rewardFallbackText.fontStyle = FontStyles.Bold;

            rewardNameText = CreateText("Text_RewardName", group.transform, "빈 상자", 28,
                TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.58f, 1f),
                new Vector2(460f, 42f), new Vector2(0f, -134f));
            rewardNameText.fontStyle = FontStyles.Bold;

            rewardDescriptionText = CreateText("Text_RewardDescription", group.transform, "가져갈 수 있는 장신구가 없습니다.", 20,
                TextAlignmentOptions.Center, new Color(0.94f, 0.9f, 0.78f, 1f),
                new Vector2(500f, 62f), new Vector2(0f, -184f));
            rewardDescriptionText.enableAutoSizing = true;
            rewardDescriptionText.fontSizeMin = 16;
            rewardDescriptionText.fontSizeMax = 20;

            rewardHintText = CreateText("Text_RewardHint", group.transform, "눌러서 가져가기", 19,
                TextAlignmentOptions.Center, new Color(0.83f, 0.93f, 0.84f, 1f),
                new Vector2(260f, 32f), new Vector2(0f, -232f));
            rewardHintText.fontStyle = FontStyles.Bold;

            group.SetActive(false);
            return group;
        }

        private TreasureUIComponent.TreasureRewardModel CreateStandaloneReward()
        {
            ItemDataSO[] allItems = Resources.LoadAll<ItemDataSO>("SO/Item");
            var candidates = new List<ItemDataSO>();
            for (int i = 0; i < allItems.Length; i++)
            {
                ItemDataSO item = allItems[i];
                if (item != null && item.Type == ItemType.Accessory)
                    candidates.Add(item);
            }

            if (candidates.Count == 0)
                return null;

            ItemDataSO reward = candidates[Random.Range(0, candidates.Count)];
            return new TreasureUIComponent.TreasureRewardModel
            {
                Id = reward.Id,
                DisplayName = reward.DisplayName,
                Description = reward.Description,
                Icon = reward.Icon
            };
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
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            TextMeshProUGUI text = CreateText($"Text_{name}", go.transform, label, 20, TextAlignmentOptions.Center,
                textColor, size, Vector2.zero);
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16;
            text.fontSizeMax = 20;
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
            GameUIFont.Apply(label, _fontAsset);
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

        private static GameObject CreateUIObject(string name, Transform parent)
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
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraGo = new GameObject("Main Camera");
                mainCamera = cameraGo.AddComponent<Camera>();
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = Color.black;
                mainCamera.orthographic = true;
                cameraGo.tag = "MainCamera";
                cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            }

            if (Object.FindFirstObjectByType<AudioListener>() == null)
                mainCamera.gameObject.AddComponent<AudioListener>();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }

        private static Sprite GetGeneratedBackgroundSprite()
        {
            if (_generatedBackgroundSprite != null)
                return _generatedBackgroundSprite;

            const int width = 1280;
            const int height = 720;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color top = new Color(0.025f, 0.035f, 0.038f, 1f);
            Color middle = new Color(0.065f, 0.08f, 0.075f, 1f);
            Color floor = new Color(0.13f, 0.105f, 0.07f, 1f);
            Vector2 glowCenter = new Vector2(width * 0.5f, height * 0.49f);

            for (int y = 0; y < height; y++)
            {
                float vertical = y / (height - 1f);
                for (int x = 0; x < width; x++)
                {
                    float floorBlend = Mathf.SmoothStep(0.38f, 0.86f, 1f - vertical);
                    Color color = Color.Lerp(Color.Lerp(top, middle, vertical), floor, floorBlend);

                    float dx = (x - glowCenter.x) / width;
                    float dy = (y - glowCenter.y) / height;
                    float glow = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx * 5.5f + dy * dy * 11f));
                    color = Color.Lerp(color, new Color(0.48f, 0.34f, 0.12f, 1f), glow * 0.34f);

                    float edgeX = Mathf.Abs(x - width * 0.5f) / (width * 0.5f);
                    float edgeY = Mathf.Abs(y - height * 0.5f) / (height * 0.5f);
                    float vignette = Mathf.Clamp01(Mathf.Max(edgeX, edgeY) * 0.72f);
                    color *= Mathf.Lerp(1f, 0.34f, vignette);
                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
            }

            DrawEllipse(texture, 640, 158, 520, 68, new Color(0.04f, 0.035f, 0.028f, 0.62f));
            DrawEllipse(texture, 412, 176, 170, 28, new Color(0.16f, 0.13f, 0.09f, 0.52f));
            DrawEllipse(texture, 860, 186, 210, 34, new Color(0.15f, 0.12f, 0.085f, 0.46f));
            DrawCircle(texture, 274, 202, 42, new Color(0.1f, 0.11f, 0.1f, 0.78f));
            DrawCircle(texture, 316, 204, 25, new Color(0.16f, 0.13f, 0.1f, 0.78f));
            DrawCircle(texture, 984, 194, 48, new Color(0.11f, 0.1f, 0.09f, 0.72f));
            DrawCircle(texture, 1032, 200, 32, new Color(0.17f, 0.13f, 0.08f, 0.72f));

            texture.Apply();
            _generatedBackgroundSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            _generatedBackgroundSprite.hideFlags = HideFlags.HideAndDontSave;
            return _generatedBackgroundSprite;
        }

        private static Sprite GetGeneratedChestSprite()
        {
            if (_generatedChestSprite != null)
                return _generatedChestSprite;

            const int width = 256;
            const int height = 200;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Fill(texture, new Color(0f, 0f, 0f, 0f));
            DrawEllipse(texture, 128, 30, 100, 15, new Color(0f, 0f, 0f, 0.34f));
            DrawRect(texture, 42, 38, 214, 116, new Color(0.38f, 0.17f, 0.07f, 1f));
            DrawRect(texture, 48, 45, 208, 108, new Color(0.65f, 0.31f, 0.12f, 1f));
            DrawRect(texture, 42, 113, 214, 151, new Color(0.47f, 0.2f, 0.08f, 1f));
            DrawRect(texture, 54, 121, 202, 148, new Color(0.78f, 0.42f, 0.17f, 1f));
            DrawRect(texture, 38, 100, 218, 118, new Color(0.95f, 0.68f, 0.22f, 1f));
            DrawRect(texture, 116, 38, 140, 151, new Color(0.9f, 0.58f, 0.18f, 1f));
            DrawRect(texture, 108, 72, 148, 108, new Color(0.16f, 0.11f, 0.07f, 1f));
            DrawRect(texture, 116, 78, 140, 108, new Color(0.98f, 0.77f, 0.25f, 1f));
            DrawRect(texture, 123, 84, 133, 100, new Color(0.2f, 0.13f, 0.06f, 1f));
            DrawRect(texture, 38, 35, 218, 45, new Color(0.19f, 0.1f, 0.05f, 1f));
            DrawRect(texture, 38, 148, 218, 158, new Color(0.2f, 0.1f, 0.04f, 1f));
            DrawRect(texture, 52, 122, 200, 128, new Color(1f, 0.66f, 0.24f, 0.64f));

            texture.Apply();
            _generatedChestSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            _generatedChestSprite.hideFlags = HideFlags.HideAndDontSave;
            return _generatedChestSprite;
        }

        private static Sprite GetGeneratedGlowSprite()
        {
            if (_generatedGlowSprite != null)
                return _generatedGlowSprite;

            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxDistance = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.2f) * 0.72f;
                    texture.SetPixel(x, y, new Color(1f, 0.72f, 0.18f, alpha));
                }
            }

            texture.Apply();
            _generatedGlowSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            _generatedGlowSprite.hideFlags = HideFlags.HideAndDontSave;
            return _generatedGlowSprite;
        }

        private static void Fill(Texture2D texture, Color color)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                    texture.SetPixel(x, y, color);
            }
        }

        private static void DrawRect(Texture2D texture, int xMin, int yMin, int xMax, int yMax, Color color)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                    BlendPixel(texture, x, y, color);
            }
        }

        private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            int radiusSqr = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy <= radiusSqr)
                        BlendPixel(texture, x, y, color);
                }
            }
        }

        private static void DrawEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            float radiusXSqr = radiusX * radiusX;
            float radiusYSqr = radiusY * radiusY;
            for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    if (dx * dx / radiusXSqr + dy * dy / radiusYSqr <= 1f)
                        BlendPixel(texture, x, y, color);
                }
            }
        }

        private static void BlendPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
                return;

            Color current = texture.GetPixel(x, y);
            float alpha = Mathf.Clamp01(color.a);
            Color blended = Color.Lerp(current, new Color(color.r, color.g, color.b, 1f), alpha);
            blended.a = Mathf.Clamp01(current.a + alpha);
            texture.SetPixel(x, y, blended);
        }
    }
}
