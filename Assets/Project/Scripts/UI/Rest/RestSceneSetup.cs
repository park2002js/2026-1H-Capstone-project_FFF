using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FFF.Core;
using FFF.UI.Common;
using FFF.UI.Core;

namespace FFF.UI.Rest
{
    /// <summary>
    /// RestScene 진입 시 휴식 UI를 준비한다.
    /// 씬에 직접 배치된 UI가 없으면 런타임에 기본 UI를 생성한다.
    /// </summary>
    public class RestSceneSetup : MonoBehaviour
    {
        [SerializeField] private RestUIComponent _restUI;
        [SerializeField] private bool _buildRuntimeUIIfMissing = true;

        [Header("교체용 이미지")]
        [SerializeField] private Sprite _backgroundSprite;

        [Header("폰트")]
        [SerializeField] private TMP_FontAsset _fontAsset;

        [Header("휴식 스토리")]
        [SerializeField, TextArea(2, 5)]
        private string _storyText = "거친 길 끝에 잠시 숨을 고를 수 있는 자리가 나타났습니다.\n따뜻한 기운이 상처를 감싸고, 다시 나아갈 힘이 돌아옵니다.";

        private static readonly Color StoryPanelColor = new Color(0.04f, 0.055f, 0.06f, 0.74f);
        private static readonly Color RestButtonColor = new Color(0.18f, 0.46f, 0.38f, 0.96f);
        private static readonly Color RestButtonTextColor = new Color(0.97f, 0.94f, 0.84f, 1f);

        private void Start()
        {
            if (_restUI == null && _buildRuntimeUIIfMissing)
                _restUI = BuildRuntimeRestUI();

            if (_restUI == null)
            {
                Debug.LogError("[RestSceneSetup] RestUI를 찾을 수 없습니다.");
                return;
            }

            _restUI.SetStory(_storyText);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRestSceneReady(_restUI);
            }
            else
            {
                Debug.LogWarning("[RestSceneSetup] GameManager가 없어 RestScene 단독 테스트 모드로 표시합니다.");
                _restUI.SetPlayerHealth(70, 100);
                _restUI.Initialize();
                _restUI.Show();
            }
        }

        private void OnDestroy()
        {
            GameManager.Instance?.UnregisterScreen(UIScreenNames.REST);
        }

        private RestUIComponent BuildRuntimeRestUI()
        {
            EnsureCamera();
            EnsureEventSystem();

            GameObject canvasGo = new GameObject("RestCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject root = CreateUIObject("RestUI", canvasGo.transform);
            StretchToParent(root.GetComponent<RectTransform>());
            root.AddComponent<CanvasGroup>();
            RestUIComponent restUI = root.AddComponent<RestUIComponent>();

            CreateBackground(root.transform);
            TextMeshProUGUI storyText = CreateStoryBlock(root.transform);
            TextMeshProUGUI feedbackText = CreateText(
                "Text_RestFeedback",
                root.transform,
                "",
                22,
                TextAlignmentOptions.Center,
                new Color(0.94f, 0.91f, 0.78f, 1f),
                new Vector2(600f, 40f),
                new Vector2(0f, -274f));
            feedbackText.fontStyle = FontStyles.Bold;
            feedbackText.gameObject.SetActive(false);

            TextMeshProUGUI healthText = CreateText(
                "Text_RestHealth",
                root.transform,
                "체력 - / -",
                24,
                TextAlignmentOptions.Right,
                new Color(1f, 0.86f, 0.64f, 1f),
                new Vector2(260f, 38f),
                new Vector2(-140f, 120f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f));
            healthText.fontStyle = FontStyles.Bold;

            Button restButton = CreateTextButton(
                "Button_Rest",
                root.transform,
                "휴식",
                new Vector2(174f, 58f),
                new Vector2(-96f, 56f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                RestButtonColor,
                RestButtonTextColor,
                out TextMeshProUGUI restButtonText);

            restUI.Bind(restButton, restButtonText, storyText, healthText, feedbackText);
            return restUI;
        }

        private void CreateBackground(Transform parent)
        {
            GameObject background = CreateImage("BackgroundImage", parent, new Color(0.09f, 0.11f, 0.12f, 1f));
            StretchToParent(background.GetComponent<RectTransform>());

            Image image = background.GetComponent<Image>();
            image.sprite = _backgroundSprite;
            image.preserveAspect = false;
            image.color = _backgroundSprite != null ? Color.white : image.color;
        }

        private TextMeshProUGUI CreateStoryBlock(Transform parent)
        {
            GameObject panel = CreateImage("StoryPanel", parent, StoryPanelColor);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.sizeDelta = new Vector2(820f, 132f);
            panelRect.anchoredPosition = new Vector2(0f, -178f);
            panel.AddComponent<Outline>().effectColor = new Color(0.9f, 0.82f, 0.58f, 0.42f);

            TextMeshProUGUI story = CreateText(
                "Text_RestStory",
                panel.transform,
                _storyText,
                26,
                TextAlignmentOptions.Center,
                new Color(0.96f, 0.94f, 0.86f, 1f),
                new Vector2(760f, 90f),
                Vector2.zero);
            story.fontStyle = FontStyles.Bold;
            story.enableAutoSizing = true;
            story.fontSizeMin = 20;
            story.fontSizeMax = 26;
            story.overflowMode = TextOverflowModes.Ellipsis;
            return story;
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
            Color textColor,
            out TextMeshProUGUI labelText)
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
            labelText = CreateText($"Text_{name}", go.transform, label, 22, TextAlignmentOptions.Center,
                textColor, size, Vector2.zero);
            labelText.fontStyle = FontStyles.Bold;
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 17;
            labelText.fontSizeMax = 22;
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
            return CreateText(name, parent, text, fontSize, alignment, color, size, anchoredPosition,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }

        private TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string text,
            int fontSize,
            TextAlignmentOptions alignment,
            Color color,
            Vector2 size,
            Vector2 anchoredPosition,
            Vector2 anchorMin,
            Vector2 anchorMax)
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
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
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
    }
}
