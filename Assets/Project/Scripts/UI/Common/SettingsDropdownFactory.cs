using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FFF.UI.Common
{
    public static class SettingsDropdownFactory
    {
        public static TMP_Dropdown CreateTMPDropdown(
            string name,
            Transform parent,
            IReadOnlyList<string> options,
            int selectedIndex,
            Vector2 size,
            Vector2 position)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.22f, 0.24f, 0.29f, 1f);

            TMP_Dropdown dropdown = go.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = image;

            TextMeshProUGUI caption = CreateText("Label", go.transform, string.Empty, 18,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            Stretch(caption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(14f, 4f), new Vector2(-46f, -4f));
            dropdown.captionText = caption;

            TextMeshProUGUI arrow = CreateText("Arrow", go.transform, "▼", 18,
                TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-42f, 0f), new Vector2(-8f, 0f));

            GameObject template = CreateDropdownTemplate(go.transform, size.x);
            dropdown.template = template.GetComponent<RectTransform>();
            dropdown.itemText = template.transform.Find("Viewport/Content/Item/Item Label")?.GetComponent<TextMeshProUGUI>();

            dropdown.options.Clear();
            for (int i = 0; i < options.Count; i++)
                dropdown.options.Add(new TMP_Dropdown.OptionData(options[i]));

            dropdown.value = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, options.Count - 1));
            dropdown.RefreshShownValue();
            return dropdown;
        }

        private static GameObject CreateDropdownTemplate(Transform parent, float width)
        {
            GameObject template = CreateUIObject("Template", parent);
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.sizeDelta = new Vector2(0f, 150f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);

            Image templateImage = template.AddComponent<Image>();
            templateImage.color = new Color(0.1f, 0.11f, 0.14f, 0.98f);

            ScrollRect scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject viewport = CreateUIObject("Viewport", template.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 34f);
            contentRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject item = CreateUIObject("Item", content.transform);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(width - 8f, 34f);

            Image itemImage = item.AddComponent<Image>();
            itemImage.color = new Color(0.18f, 0.2f, 0.24f, 1f);

            Toggle itemToggle = item.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemImage;

            GameObject checkmark = CreateUIObject("Item Checkmark", item.transform);
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            SetRect(checkRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(18f, 18f), new Vector2(18f, 0f));

            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(1f, 0.84f, 0.08f, 1f);
            itemToggle.graphic = checkImage;

            TextMeshProUGUI itemLabel = CreateText("Item Label", item.transform, string.Empty, 17,
                TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            Stretch(itemLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(42f, 2f), new Vector2(-8f, -2f));

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            template.SetActive(false);
            return template;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.layer = parent != null ? parent.gameObject.layer : 0;
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static TextMeshProUGUI CreateText(
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

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
