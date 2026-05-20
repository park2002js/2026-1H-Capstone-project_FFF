using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FFF.Core
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        private const float FadeDuration = 0.25f;
        private const float MinimumVisibleTime = 0.45f;
        private const string LoadingMessage = "\ubd88\ub7ec\uc624\ub294 \uc911";

        private static LoadingScreenController _instance;

        private CanvasGroup _canvasGroup;
        private RectTransform _progressFill;
        private Text _messageText;
        private Text _progressText;
        private Font _font;
        private Coroutine _loadingRoutine;
        private float _dotTimer;

        public bool IsLoading => _loadingRoutine != null;

        public static LoadingScreenController EnsureExists()
        {
            if (_instance != null)
                return _instance;

            _instance = FindFirstObjectByType<LoadingScreenController>();
            if (_instance != null)
                return _instance;

            GameObject go = new GameObject(nameof(LoadingScreenController));
            _instance = go.AddComponent<LoadingScreenController>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
            HideInstant();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            if (_messageText == null || _canvasGroup == null || _canvasGroup.alpha <= 0f)
                return;

            _dotTimer += Time.unscaledDeltaTime;
            int dotCount = Mathf.FloorToInt(_dotTimer / 0.35f) % 4;
            _messageText.text = LoadingMessage + new string('.', dotCount);
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[LoadingScreen] Scene name is empty.");
                return;
            }

            if (_loadingRoutine != null)
            {
                Debug.LogWarning($"[LoadingScreen] Already loading a scene. Ignored: {sceneName}");
                return;
            }

            _loadingRoutine = StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            BuildUI();
            SetProgress(0f);
            _dotTimer = 0f;
            _canvasGroup.blocksRaycasts = true;

            float visibleStartedAt = Time.unscaledTime;
            yield return FadeTo(1f, FadeDuration);

            AsyncOperation operation = null;
            Exception loadException = null;
            try
            {
                operation = SceneManager.LoadSceneAsync(sceneName);
            }
            catch (Exception exception)
            {
                loadException = exception;
            }

            if (loadException != null)
            {
                Debug.LogError($"[LoadingScreen] Failed to start scene load: {sceneName}\n{loadException}");
                yield return FadeTo(0f, FadeDuration);
                _loadingRoutine = null;
                yield break;
            }

            if (operation == null)
            {
                Debug.LogError($"[LoadingScreen] Failed to start scene load: {sceneName}");
                yield return FadeTo(0f, FadeDuration);
                _loadingRoutine = null;
                yield break;
            }

            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                SetProgress(operation.progress / 0.9f);
                yield return null;
            }

            SetProgress(1f);

            float remainingVisibleTime = MinimumVisibleTime - (Time.unscaledTime - visibleStartedAt);
            if (remainingVisibleTime > 0f)
                yield return new WaitForSecondsRealtime(remainingVisibleTime);

            operation.allowSceneActivation = true;

            while (!operation.isDone)
                yield return null;

            yield return null;
            yield return FadeTo(0f, FadeDuration);
            _loadingRoutine = null;
        }

        private void BuildUI()
        {
            if (_canvasGroup != null)
                return;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            GameObject background = CreateUIObject("Background", transform);
            StretchToParent(background.GetComponent<RectTransform>());
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.03f, 0.03f, 0.035f, 0.96f);

            GameObject content = CreateUIObject("Content", transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(720f, 220f);
            contentRect.anchoredPosition = Vector2.zero;

            _messageText = CreateText(
                "Message",
                content.transform,
                LoadingMessage,
                44,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.98f, 0.94f, 0.86f, 1f),
                new Vector2(720f, 72f),
                new Vector2(0f, 42f));

            _progressText = CreateText(
                "ProgressText",
                content.transform,
                "0%",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.78f, 0.32f, 1f),
                new Vector2(180f, 36f),
                new Vector2(0f, -28f));

            GameObject track = CreateUIObject("ProgressTrack", content.transform);
            RectTransform trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.5f, 0.5f);
            trackRect.anchorMax = new Vector2(0.5f, 0.5f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.sizeDelta = new Vector2(560f, 14f);
            trackRect.anchoredPosition = new Vector2(0f, -70f);

            Image trackImage = track.AddComponent<Image>();
            trackImage.color = new Color(1f, 1f, 1f, 0.16f);

            GameObject fill = CreateUIObject("ProgressFill", track.transform);
            _progressFill = fill.GetComponent<RectTransform>();
            _progressFill.anchorMin = Vector2.zero;
            _progressFill.anchorMax = new Vector2(0f, 1f);
            _progressFill.offsetMin = Vector2.zero;
            _progressFill.offsetMax = Vector2.zero;

            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(1f, 0.7f, 0.18f, 1f);
        }

        private void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (_progressFill != null)
            {
                _progressFill.anchorMax = new Vector2(progress, 1f);
                _progressFill.offsetMin = Vector2.zero;
                _progressFill.offsetMax = Vector2.zero;
            }

            if (_progressText != null)
                _progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = false;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _canvasGroup.blocksRaycasts = targetAlpha > 0.001f;
            _canvasGroup.interactable = false;
        }

        private void HideInstant()
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private GameObject CreateUIObject(string objectName, Transform parent)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Text CreateText(
            string objectName,
            Transform parent,
            string text,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            GameObject go = CreateUIObject(objectName, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Text label = go.AddComponent<Text>();
            label.text = text;
            label.font = ResolveFont(fontSize);
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private Font ResolveFont(int size)
        {
            if (_font != null)
                return _font;

            _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, size);
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return _font;
        }
    }
}
