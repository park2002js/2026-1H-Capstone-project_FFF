using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FFF.UI.Common
{
    public sealed class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private const float TooltipWidth = 360f;
        private const float ScreenPadding = 12f;
        private static readonly Vector2 MouseOffset = new Vector2(18f, -18f);

        private static ItemTooltipTrigger _currentOwner;
        private static Canvas _tooltipCanvas;
        private static RectTransform _tooltipPanel;
        private static CanvasGroup _tooltipGroup;
        private static TextMeshProUGUI _categoryText;
        private static TextMeshProUGUI _titleText;
        private static TextMeshProUGUI _descriptionText;

        [SerializeField] private string _category;
        [SerializeField] private string _title;
        [SerializeField] private string _description;

        public void SetContent(string title, string description, string category)
        {
            _title = string.IsNullOrWhiteSpace(title) ? "알 수 없는 아이템" : title.Trim();
            _description = string.IsNullOrWhiteSpace(description) ? "효과 설명 없음" : description.Trim();
            _category = string.IsNullOrWhiteSpace(category) ? string.Empty : category.Trim();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrWhiteSpace(_title) && string.IsNullOrWhiteSpace(_description))
                return;

            Show(this, eventData.position);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_currentOwner == this)
                PositionTooltip(eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_currentOwner == this)
                Hide();
        }

        private void OnDisable()
        {
            if (_currentOwner == this)
                Hide();
        }

        private static void Show(ItemTooltipTrigger owner, Vector2 screenPosition)
        {
            EnsureTooltip();

            _currentOwner = owner;
            _categoryText.gameObject.SetActive(!string.IsNullOrWhiteSpace(owner._category));
            _categoryText.text = owner._category;
            _titleText.text = owner._title;
            _descriptionText.text = owner._description;

            _tooltipPanel.gameObject.SetActive(true);
            _tooltipGroup.alpha = 1f;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipPanel);
            PositionTooltip(screenPosition);
        }

        private static void Hide()
        {
            _currentOwner = null;

            if (_tooltipGroup != null)
                _tooltipGroup.alpha = 0f;

            if (_tooltipPanel != null)
                _tooltipPanel.gameObject.SetActive(false);
        }

        private static void EnsureTooltip()
        {
            if (_tooltipPanel != null)
                return;

            int uiLayer = LayerMask.NameToLayer("UI");
            GameObject canvasObject = new GameObject("ItemTooltipCanvas");
            canvasObject.layer = uiLayer >= 0 ? uiLayer : 0;

            _tooltipCanvas = canvasObject.AddComponent<Canvas>();
            _tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _tooltipCanvas.overrideSorting = true;
            _tooltipCanvas.sortingOrder = 5000;

            _tooltipGroup = canvasObject.AddComponent<CanvasGroup>();
            _tooltipGroup.alpha = 0f;
            _tooltipGroup.interactable = false;
            _tooltipGroup.blocksRaycasts = false;

            GameObject panelObject = CreateUIObject("Panel_ItemTooltip", canvasObject.transform, canvasObject.layer);
            _tooltipPanel = panelObject.GetComponent<RectTransform>();
            _tooltipPanel.anchorMin = Vector2.zero;
            _tooltipPanel.anchorMax = Vector2.zero;
            _tooltipPanel.pivot = new Vector2(0f, 1f);
            _tooltipPanel.sizeDelta = new Vector2(TooltipWidth, 0f);

            Image background = panelObject.AddComponent<Image>();
            background.color = new Color(0.055f, 0.065f, 0.075f, 0.96f);
            background.raycastTarget = false;

            Shadow shadow = panelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            shadow.effectDistance = new Vector2(0f, -5f);

            VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 14, 16);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panelObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _categoryText = CreateText("Text_Category", panelObject.transform, 17, FontStyles.Bold,
                new Color(1f, 0.78f, 0.34f, 1f));
            _titleText = CreateText("Text_Title", panelObject.transform, 23, FontStyles.Bold, Color.white);
            _descriptionText = CreateText("Text_Description", panelObject.transform, 19, FontStyles.Normal,
                new Color(0.9f, 0.94f, 0.96f, 1f));

            _tooltipPanel.gameObject.SetActive(false);
        }

        private static void PositionTooltip(Vector2 screenPosition)
        {
            if (_tooltipPanel == null)
                return;

            Canvas.ForceUpdateCanvases();

            Vector2 size = _tooltipPanel.rect.size;
            Vector2 position = screenPosition + MouseOffset;
            position.x = Mathf.Clamp(position.x, ScreenPadding, Mathf.Max(ScreenPadding, Screen.width - size.x - ScreenPadding));
            position.y = Mathf.Clamp(position.y, size.y + ScreenPadding, Mathf.Max(size.y + ScreenPadding, Screen.height - ScreenPadding));
            _tooltipPanel.anchoredPosition = position;
        }

        private static GameObject CreateUIObject(string name, Transform parent, int layer)
        {
            GameObject go = new GameObject(name);
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, int fontSize, FontStyles style, Color color)
        {
            GameObject go = CreateUIObject(name, parent, parent.gameObject.layer);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            GameUIFont.Apply(text);
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }
    }
}
