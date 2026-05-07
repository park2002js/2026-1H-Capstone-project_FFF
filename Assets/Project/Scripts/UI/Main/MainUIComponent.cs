using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FFF.UI.Core;
using FFF.Audio;

namespace FFF.UI.Main
{
    public class MainUIComponent : BaseUIComponent
    {
        private const string NewGameWarningTitle = "처음부터 시작";
        private const string NewGameWarningMessage = "이전 플레이 데이터가 초기화됩니다.\n처음부터 다시 시작할까요?";
        private const string SettingsTitle = "환경 설정";
        private const string QualityPrefsKey = "FFF.Settings.Quality";
        private const string FullscreenPrefsKey = "FFF.Settings.Fullscreen";
        private const string FrameRatePrefsKey = "FFF.Settings.FrameRate";
        private const string ResolutionWidthPrefsKey = "FFF.Settings.ResolutionWidth";
        private const string ResolutionHeightPrefsKey = "FFF.Settings.ResolutionHeight";

        public Action OnNewGame;
        public Action OnContinue;

        [Header("UI 참조")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingsButton;

        private GameObject _newGameConfirmDialog;
        private GameObject _settingsDialog;
        private Font _dialogFont;

        protected override void Awake()
        {
            base.Awake();
            ApplySavedDisplaySettings();
            
            if (_newGameButton != null)
            {
                _newGameButton.onClick.AddListener(OnNewGameButton_Clicked);
            }
            
            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(OnContinueButton_Clicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsButton_Clicked);
            }
        }

        private void OnNewGameButton_Clicked()
        {
            Debug.Log("[MainUI] New Game 버튼 클릭! 초기화 경고창을 표시합니다.");
            SoundManager.PlayDefaultUiClick();
            ShowNewGameConfirmDialog();
        }

        private void OnContinueButton_Clicked()
        {
            Debug.Log("[MainUI] Continue 버튼 클릭! GameManager로 이벤트를 전달합니다.");
            SoundManager.PlayDefaultUiClick();
            OnContinue?.Invoke();
        }

        private void OnSettingsButton_Clicked()
        {
            Debug.Log("[MainUI] Settings 버튼 클릭! 환경설정 창을 표시합니다.");
            SoundManager.PlayDefaultUiClick();
            ShowSettingsDialog();
        }

        private void ShowNewGameConfirmDialog()
        {
            if (_newGameConfirmDialog == null)
                _newGameConfirmDialog = BuildNewGameConfirmDialog();

            if (_newGameConfirmDialog != null)
            {
                _newGameConfirmDialog.SetActive(true);
                _newGameConfirmDialog.transform.SetAsLastSibling();
            }
        }

        private void ShowSettingsDialog()
        {
            if (_settingsDialog == null)
                _settingsDialog = BuildSettingsDialog();

            if (_settingsDialog != null)
            {
                _settingsDialog.SetActive(true);
                _settingsDialog.transform.SetAsLastSibling();
            }
        }

        private GameObject BuildNewGameConfirmDialog()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            GameObject overlay = CreateUIObject("NewGameConfirmDialog", parent);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.68f);

            GameObject panel = CreateUIObject("Panel", overlay.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560f, 300f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.12f, 0.13f, 0.16f, 1f);

            CreateText("Text_Title", panel.transform, NewGameWarningTitle, 34, FontStyle.Bold,
                TextAnchor.MiddleCenter, new RectOffset(32, 32, 20, 230));

            CreateText("Text_Message", panel.transform, NewGameWarningMessage, 23, FontStyle.Normal,
                TextAnchor.MiddleCenter, new RectOffset(44, 44, 104, 96));

            Button cancelButton = CreateDialogButton("Button_Cancel", panel.transform, "취소",
                new Vector2(-115f, -98f), new Color(0.36f, 0.38f, 0.43f, 1f));
            cancelButton.onClick.AddListener(() =>
            {
                SoundManager.PlayDefaultUiClick();
                overlay.SetActive(false);
            });

            Button confirmButton = CreateDialogButton("Button_Confirm", panel.transform, "처음부터",
                new Vector2(115f, -98f), new Color(0.72f, 0.18f, 0.18f, 1f));
            confirmButton.onClick.AddListener(() =>
            {
                SoundManager.PlayDefaultUiClick();
                overlay.SetActive(false);
                OnNewGame?.Invoke();
            });

            overlay.SetActive(false);
            return overlay;
        }

        private GameObject BuildSettingsDialog()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            GameObject overlay = CreateUIObject("SettingsDialog", parent);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.68f);

            GameObject panel = CreateUIObject("Panel", overlay.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(820f, 660f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.12f, 0.13f, 0.16f, 1f);

            CreateAnchoredText("Text_SettingsTitle", panel.transform, SettingsTitle, 36, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white, new Vector2(500f, 50f), new Vector2(0f, 284f));

            CreateAnchoredText("Text_GraphicsHeader", panel.transform, "그래픽", 27, FontStyle.Bold,
                TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.3f, 1f), new Vector2(620f, 38f), new Vector2(0f, 228f));

            BuildGraphicsSettings(panel.transform);

            CreateAnchoredText("Text_SoundHeader", panel.transform, "사운드", 27, FontStyle.Bold,
                TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.3f, 1f), new Vector2(620f, 38f), new Vector2(0f, -18f));

            BuildSoundSettings(panel.transform);

            Button closeButton = CreateDialogButton("Button_CloseSettings", panel.transform, "닫기",
                new Vector2(0f, -282f), new Color(0.36f, 0.38f, 0.43f, 1f));
            closeButton.onClick.AddListener(() =>
            {
                SoundManager.PlayDefaultUiClick();
                overlay.SetActive(false);
            });

            overlay.SetActive(false);
            return overlay;
        }

        private void BuildGraphicsSettings(Transform parent)
        {
            CreateSettingsLabel(parent, "해상도", 174f);
            List<Vector2Int> resolutions = BuildResolutionOptions();
            List<string> resolutionLabels = new List<string>();
            for (int i = 0; i < resolutions.Count; i++)
                resolutionLabels.Add($"{resolutions[i].x} x {resolutions[i].y}");

            Dropdown resolutionDropdown = CreateDropdown("Dropdown_Resolution", parent, resolutionLabels,
                GetCurrentResolutionIndex(resolutions), new Vector2(320f, 42f), new Vector2(70f, 174f));
            resolutionDropdown.onValueChanged.AddListener(index =>
            {
                if (index < 0 || index >= resolutions.Count) return;
                Vector2Int resolution = resolutions[index];
                Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreen);
                PlayerPrefs.SetInt(ResolutionWidthPrefsKey, resolution.x);
                PlayerPrefs.SetInt(ResolutionHeightPrefsKey, resolution.y);
                PlayerPrefs.Save();
            });

            CreateSettingsLabel(parent, "화질", 124f);
            string[] qualityNames = QualitySettings.names;
            List<string> qualityLabels = new List<string>();
            if (qualityNames != null && qualityNames.Length > 0)
                qualityLabels.AddRange(qualityNames);
            else
                qualityLabels.Add("기본");

            int qualityIndex = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, qualityLabels.Count - 1);
            Dropdown qualityDropdown = CreateDropdown("Dropdown_Quality", parent, qualityLabels,
                qualityIndex, new Vector2(320f, 42f), new Vector2(70f, 124f));
            qualityDropdown.onValueChanged.AddListener(index =>
            {
                if (qualityNames == null || qualityNames.Length == 0) return;
                QualitySettings.SetQualityLevel(index, true);
                PlayerPrefs.SetInt(QualityPrefsKey, index);
                PlayerPrefs.Save();
            });

            CreateSettingsLabel(parent, "전체 화면", 74f);
            Toggle fullscreenToggle = CreateToggle("Toggle_Fullscreen", parent, "사용", Screen.fullScreen,
                new Vector2(0f, 74f), new Vector2(180f, 34f));
            fullscreenToggle.onValueChanged.AddListener(isOn =>
            {
                Screen.fullScreen = isOn;
                PlayerPrefs.SetInt(FullscreenPrefsKey, isOn ? 1 : 0);
                PlayerPrefs.Save();
            });

            CreateSettingsLabel(parent, "프레임 제한", 24f);
            List<string> frameLabels = new List<string> { "30 FPS", "60 FPS", "120 FPS", "무제한" };
            int[] frameValues = { 30, 60, 120, -1 };
            Dropdown frameDropdown = CreateDropdown("Dropdown_FrameRate", parent, frameLabels,
                GetCurrentFrameRateIndex(frameValues), new Vector2(320f, 42f), new Vector2(70f, 24f));
            frameDropdown.onValueChanged.AddListener(index =>
            {
                if (index < 0 || index >= frameValues.Length) return;
                ApplyFrameRate(frameValues[index]);
            });
        }

        private void BuildSoundSettings(Transform parent)
        {
            CreateVolumeRow(parent, "전체 음량", SoundBus.Master, -72f);
            CreateVolumeRow(parent, "배경음", SoundBus.Bgm, -122f);
            CreateVolumeRow(parent, "효과음", SoundBus.Sfx, -172f);
            CreateVolumeRow(parent, "UI 효과음", SoundBus.Ui, -222f);
        }

        private void CreateVolumeRow(Transform parent, string label, SoundBus bus, float y)
        {
            SoundManager soundManager = SoundManager.EnsureExists();

            CreateSettingsLabel(parent, label, y);
            Text percentText = CreateAnchoredText($"Text_{bus}Percent", parent, ToPercent(soundManager.GetVolume(bus)),
                18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(70f, 30f), new Vector2(245f, y));

            Slider slider = CreateSlider($"Slider_{bus}", parent, soundManager.GetVolume(bus),
                new Vector2(280f, 28f), new Vector2(35f, y));
            slider.onValueChanged.AddListener(value =>
            {
                soundManager.SetVolume(bus, value);
                percentText.text = ToPercent(value);
            });

            Toggle muteToggle = CreateToggle($"Toggle_{bus}Mute", parent, "음소거", soundManager.IsMuted(bus),
                new Vector2(335f, y), new Vector2(120f, 34f));
            muteToggle.onValueChanged.AddListener(isOn => soundManager.SetMute(bus, isOn));
        }

        private void ApplySavedDisplaySettings()
        {
            if (PlayerPrefs.HasKey(QualityPrefsKey) && QualitySettings.names.Length > 0)
            {
                int quality = Mathf.Clamp(PlayerPrefs.GetInt(QualityPrefsKey), 0, QualitySettings.names.Length - 1);
                QualitySettings.SetQualityLevel(quality, true);
            }

            if (PlayerPrefs.HasKey(FullscreenPrefsKey))
                Screen.fullScreen = PlayerPrefs.GetInt(FullscreenPrefsKey) == 1;

            if (PlayerPrefs.HasKey(FrameRatePrefsKey))
                Application.targetFrameRate = PlayerPrefs.GetInt(FrameRatePrefsKey);

            if (PlayerPrefs.HasKey(ResolutionWidthPrefsKey) && PlayerPrefs.HasKey(ResolutionHeightPrefsKey))
            {
                int width = PlayerPrefs.GetInt(ResolutionWidthPrefsKey);
                int height = PlayerPrefs.GetInt(ResolutionHeightPrefsKey);
                if (width > 0 && height > 0)
                    Screen.SetResolution(width, height, Screen.fullScreen);
            }
        }

        private static void ApplyFrameRate(int frameRate)
        {
            Application.targetFrameRate = frameRate;
            PlayerPrefs.SetInt(FrameRatePrefsKey, frameRate);
            PlayerPrefs.Save();
        }

        private static int GetCurrentFrameRateIndex(int[] frameValues)
        {
            int savedFrameRate = PlayerPrefs.GetInt(FrameRatePrefsKey, Application.targetFrameRate);
            for (int i = 0; i < frameValues.Length; i++)
            {
                if (frameValues[i] == savedFrameRate)
                    return i;
            }

            return 1;
        }

        private static List<Vector2Int> BuildResolutionOptions()
        {
            var options = new List<Vector2Int>();
            var seen = new HashSet<string>();
            Resolution[] resolutions = Screen.resolutions;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string key = $"{resolutions[i].width}x{resolutions[i].height}";
                if (seen.Add(key))
                    options.Add(new Vector2Int(resolutions[i].width, resolutions[i].height));
            }

            if (options.Count == 0)
                options.Add(new Vector2Int(Screen.width, Screen.height));

            return options;
        }

        private static int GetCurrentResolutionIndex(IReadOnlyList<Vector2Int> resolutions)
        {
            int width = PlayerPrefs.GetInt(ResolutionWidthPrefsKey, Screen.width);
            int height = PlayerPrefs.GetInt(ResolutionHeightPrefsKey, Screen.height);

            for (int i = 0; i < resolutions.Count; i++)
            {
                if (resolutions[i].x == width && resolutions[i].y == height)
                    return i;
            }

            return 0;
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

        private Text CreateAnchoredText(string name, Transform parent, string text, int fontSize, FontStyle style,
            TextAnchor alignment, Color color, Vector2 size, Vector2 position)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Text label = go.AddComponent<Text>();
            label.text = text;
            label.font = ResolveDialogFont(fontSize);
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private Text CreateSettingsLabel(Transform parent, string text, float y)
        {
            return CreateAnchoredText($"Text_Label_{text}", parent, text, 20, FontStyle.Bold,
                TextAnchor.MiddleLeft, Color.white, new Vector2(170f, 32f), new Vector2(-250f, y));
        }

        private Dropdown CreateDropdown(string name, Transform parent, List<string> options, int selectedIndex,
            Vector2 size, Vector2 position)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.22f, 0.24f, 0.29f, 1f);

            Dropdown dropdown = go.AddComponent<Dropdown>();
            dropdown.targetGraphic = image;

            Text caption = CreateAnchoredText("Label", go.transform, "", 18, FontStyle.Bold,
                TextAnchor.MiddleLeft, Color.white, new Vector2(size.x - 54f, size.y), new Vector2(-18f, 0f));
            dropdown.captionText = caption;

            CreateAnchoredText("Arrow", go.transform, "▼", 18, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white, new Vector2(36f, size.y), new Vector2(size.x * 0.5f - 24f, 0f));

            GameObject template = CreateDropdownTemplate(go.transform, size.x);
            dropdown.template = template.GetComponent<RectTransform>();

            dropdown.options.Clear();
            for (int i = 0; i < options.Count; i++)
                dropdown.options.Add(new Dropdown.OptionData(options[i]));

            dropdown.value = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, options.Count - 1));
            dropdown.RefreshShownValue();
            return dropdown;
        }

        private GameObject CreateDropdownTemplate(Transform parent, float width)
        {
            GameObject template = CreateUIObject("Template", parent);
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.sizeDelta = new Vector2(0f, 174f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);

            Image templateImage = template.AddComponent<Image>();
            templateImage.color = new Color(0.1f, 0.11f, 0.14f, 0.98f);

            ScrollRect scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject viewport = CreateUIObject("Viewport", template.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);

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
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.sizeDelta = new Vector2(18f, 18f);
            checkRect.anchoredPosition = new Vector2(18f, 0f);
            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(1f, 0.84f, 0.08f, 1f);
            itemToggle.graphic = checkImage;

            Text itemLabel = CreateAnchoredText("Item Label", item.transform, "", 17, FontStyle.Normal,
                TextAnchor.MiddleLeft, Color.white, new Vector2(width - 54f, 32f), new Vector2(24f, 0f));

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            Dropdown dropdown = parent.GetComponent<Dropdown>();
            if (dropdown != null)
                dropdown.itemText = itemLabel;

            template.SetActive(false);
            return template;
        }

        private Toggle CreateToggle(string name, Transform parent, string label, bool isOn, Vector2 position, Vector2 size)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Toggle toggle = go.AddComponent<Toggle>();

            GameObject box = CreateUIObject("Box", go.transform);
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(26f, 26f);
            boxRect.anchoredPosition = new Vector2(13f, 0f);
            Image boxImage = box.AddComponent<Image>();
            boxImage.color = new Color(0.24f, 0.26f, 0.31f, 1f);

            GameObject check = CreateUIObject("Checkmark", box.transform);
            RectTransform checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.sizeDelta = new Vector2(16f, 16f);
            checkRect.anchoredPosition = Vector2.zero;
            Image checkImage = check.AddComponent<Image>();
            checkImage.color = new Color(1f, 0.84f, 0.08f, 1f);

            CreateAnchoredText("Label", go.transform, label, 18, FontStyle.Bold,
                TextAnchor.MiddleLeft, Color.white, new Vector2(size.x - 36f, size.y), new Vector2(24f, 0f));

            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = isOn;
            return toggle;
        }

        private Slider CreateSlider(string name, Transform parent, float value, Vector2 size, Vector2 position)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Slider slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(value);

            GameObject background = CreateUIObject("Background", go.transform);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(0f, 10f);
            backgroundRect.anchoredPosition = Vector2.zero;
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.24f, 0.26f, 0.31f, 1f);

            GameObject fillArea = CreateUIObject("Fill Area", go.transform);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.offsetMin = new Vector2(5f, -5f);
            fillAreaRect.offsetMax = new Vector2(-5f, 5f);

            GameObject fill = CreateUIObject("Fill", fillArea.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(1f, 0.84f, 0.08f, 1f);

            GameObject handleArea = CreateUIObject("Handle Slide Area", go.transform);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handle = CreateUIObject("Handle", handleArea.transform);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22f, 22f);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle style,
            TextAnchor alignment, RectOffset offset)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(offset.left, offset.bottom);
            rect.offsetMax = new Vector2(-offset.right, -offset.top);

            Text label = go.AddComponent<Text>();
            label.text = text;
            label.font = ResolveDialogFont(fontSize);
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private Button CreateDialogButton(string name, Transform parent, string label, Vector2 position, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(170f, 58f);
            rect.anchoredPosition = position;

            Image image = go.AddComponent<Image>();
            image.color = color;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            CreateButtonLabel($"Text_{name}", go.transform, label);
            return button;
        }

        private Text CreateButtonLabel(string name, Transform parent, string text)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 6f);
            rect.offsetMax = new Vector2(-8f, -6f);

            Text label = go.AddComponent<Text>();
            label.text = text;
            label.font = ResolveDialogFont(20);
            label.fontSize = 20;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private Font ResolveDialogFont(int size)
        {
            if (_dialogFont != null)
                return _dialogFont;

            _dialogFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, size);
            if (_dialogFont == null)
                _dialogFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return _dialogFont;
        }
    }
}
