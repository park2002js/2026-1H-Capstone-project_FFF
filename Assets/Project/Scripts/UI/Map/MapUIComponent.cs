using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FFF.UI.Core;
using FFF.Map;
using FFF.Audio;

namespace FFF.UI.Map
{
    /// <summary>
    /// 맵 화면 전체를 담당하는 UI 컴포넌트.
    /// 노드·간선을 코드로 직접 생성하므로 별도 프리팹이 필요 없다.
    /// </summary>
    public class MapUIComponent : BaseUIComponent
    {
        public sealed class EncounterChoice
        {
            public string Label;
            public Action OnSelected;
            public bool Interactable;

            public EncounterChoice(string label, Action onSelected, bool interactable = true)
            {
                Label = label;
                OnSelected = onSelected;
                Interactable = interactable;
            }
        }

        public sealed class EncounterViewModel
        {
            public string Title;
            public string Story;
            public Sprite Artwork;
            public readonly List<EncounterChoice> Choices = new List<EncounterChoice>();
        }

        /// <summary>GameManager가 연결하는 노드 선택 델리게이트.</summary>
        public Action<int> OnNodeSelected;

        [Header("레이아웃")]
        [SerializeField] private RectTransform _mapContainer;
        
        [Header("노드 배경 (단색 네모 대신 띄울 이미지)")]
        [SerializeField] private Sprite _nodeBackgroundSprite;
        
        [SerializeField] private float _nodeSpacingX = 100f;
        [SerializeField] private float _nodeSpacingY = 80f;
        [SerializeField] private float _nodeSize = 60f;
        [SerializeField] private float _edgeThickness = 4f;
        [Header("아이콘 크기 비율")]
        [SerializeField, Range(0.1f, 1f)] private float _iconScaleRatio = 0.65f;

        [Header("방 타입 아이콘 (선택 — 없으면 색상으로 구분)")]
        [SerializeField] private Sprite _iconMonster;
        [SerializeField] private Sprite _iconElite;
        [SerializeField] private Sprite _iconEvent;
        [SerializeField] private Sprite _iconTreasure;
        [SerializeField] private Sprite _iconRest;
        [SerializeField] private Sprite _iconShop;
        [SerializeField] private Sprite _iconBoss;

        [Header("랜덤 인카운터")]
        [SerializeField] private Sprite _defaultEncounterArtwork;

        private static Sprite _visitedRingShadowSprite;
        private static Sprite _visitedRingHighlightSprite;
        private static Sprite _generatedEncounterArtwork;

        private MapData _mapData;
        private bool _nodeSelectionEnabled = true;
        private readonly Dictionary<MapNode, MapNodeView> _nodeViews = new Dictionary<MapNode, MapNodeView>();
        private GameObject _encounterOverlay;
        private TextMeshProUGUI _encounterTitleText;
        private TextMeshProUGUI _encounterStoryText;
        private Image _encounterArtworkImage;
        private TextMeshProUGUI _encounterArtworkFallbackText;
        private Transform _encounterChoiceContainer;

        public void SetMapData(MapData data)
        {
            _mapData = data;
        }

        public void SetNodeSelectionEnabled(bool enabled)
        {
            _nodeSelectionEnabled = enabled;

            foreach (KeyValuePair<MapNode, MapNodeView> pair in _nodeViews)
            {
                if (pair.Value != null && pair.Key != null)
                    pair.Value.SetState(enabled && pair.Key.IsReachable, pair.Key.IsVisited);
            }
        }

        public void ShowEncounter(EncounterViewModel encounter)
        {
            if (encounter == null)
                return;

            EnsureEncounterUI();
            SetNodeSelectionEnabled(false);

            _encounterOverlay.SetActive(true);
            _encounterOverlay.transform.SetAsLastSibling();

            _encounterTitleText.text = string.IsNullOrWhiteSpace(encounter.Title)
                ? "기묘한 조우"
                : encounter.Title;
            _encounterStoryText.text = string.IsNullOrWhiteSpace(encounter.Story)
                ? "아직 적히지 않은 이야기입니다."
                : encounter.Story;

            Sprite artwork = encounter.Artwork != null
                ? encounter.Artwork
                : _defaultEncounterArtwork != null
                    ? _defaultEncounterArtwork
                    : GetGeneratedEncounterArtwork();
            _encounterArtworkImage.sprite = artwork;
            _encounterArtworkImage.enabled = artwork != null;
            _encounterArtworkFallbackText.gameObject.SetActive(artwork == null);

            ClearChildren(_encounterChoiceContainer);
            IReadOnlyList<EncounterChoice> choices = encounter.Choices;
            if (choices == null || choices.Count == 0)
            {
                CreateEncounterChoiceButton(new EncounterChoice("떠난다", HideEncounter));
                return;
            }

            for (int i = 0; i < choices.Count && i < 3; i++)
                CreateEncounterChoiceButton(choices[i]);
        }

        public void HideEncounter()
        {
            if (_encounterOverlay != null)
                _encounterOverlay.SetActive(false);

            SetNodeSelectionEnabled(true);
        }

        public void BuildReadOnlyMap(RectTransform mapContainer, MapData data)
        {
            _mapContainer = mapContainer;
            _mapData = data;
            _nodeSelectionEnabled = false;
            BuildMap();
        }

        protected override void OnShow()
        {
            if (_mapData != null) BuildMap();
        }

        protected override void OnHide()
        {
            if (_encounterOverlay != null)
                _encounterOverlay.SetActive(false);

            ClearMap();
        }

        // ====================================================================
        // 맵 빌드
        // ====================================================================

        private void BuildMap()
        {
            ClearMap();
            if (_mapData == null) return;

            // [추가] 맵 컨테이너(Content)의 기준점을 하단 중앙(Bottom Center)으로 강제 설정
            _mapContainer.anchorMin = new Vector2(0.5f, 0f);
            _mapContainer.anchorMax = new Vector2(0.5f, 0f);
            _mapContainer.pivot = new Vector2(0.5f, 0f);

            // [추가] 보스층까지 포함하여 맵 전체의 높이를 동적으로 계산하여 스크롤 영역 확보
            float totalHeight = (MapData.FLOORS + 2) * _nodeSpacingY;
            _mapContainer.sizeDelta = new Vector2(_mapContainer.sizeDelta.x, totalHeight);
            
            // [추가] 맵에 진입할 때마다 스크롤을 맨 아래(1층)로 초기화
            _mapContainer.anchoredPosition = Vector2.zero;

            // [수정] 노드보다 '간선'을 먼저 생성합니다. 
            // 이렇게 하면 자연스럽게 배경 -> 선 -> 노드 순으로 위로 덮어씌워지며 렌더링됩니다.
            for (int floor = 0; floor < MapData.FLOORS; floor++)
            {
                for (int col = 0; col < MapData.COLUMNS; col++)
                {
                    var node = _mapData.GetNode(floor, col);
                    if (node == null) continue;

                    Vector2 fromPos = GetNodePosition(node.Floor, node.Column);
                    foreach (var next in node.Next)
                    {
                        Vector2 toPos = GetNodePosition(next.Floor, next.Column);
                        SpawnEdgeView(fromPos, toPos);
                    }
                }
            }

            // 노드 뷰 생성
            for (int floor = 0; floor < MapData.FLOORS; floor++)
                for (int col = 0; col < MapData.COLUMNS; col++)
                {
                    var node = _mapData.GetNode(floor, col);
                    if (node != null) SpawnNodeView(node);
                }

            if (_mapData.BossNode != null)
                SpawnNodeView(_mapData.BossNode);
        }

        // ====================================================================
        // 노드 생성 (코드로 GameObject 구성)
        // ====================================================================

        private void SpawnNodeView(MapNode node)
        {
            // 루트 오브젝트: RectTransform + Image(배경) + Button
            var go = new GameObject($"Node_{node.Floor}_{node.Column}");
            go.transform.SetParent(_mapContainer, false);

            var rect = go.AddComponent<RectTransform>();
            
            // [수정] 생성되는 노드의 앵커를 부모의 '맨 아래 중앙'으로 설정
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            
            rect.sizeDelta = new Vector2(_nodeSize, _nodeSize);
            rect.anchoredPosition = GetNodePosition(node.Floor, node.Column);

            var bg = go.AddComponent<Image>();
            
            // [수정] Inspector에 이미지를 등록했다면 기본 네모 대신 해당 이미지를 사용합니다.
            if (_nodeBackgroundSprite != null)
            {
                bg.sprite = _nodeBackgroundSprite;
                bg.color = Color.clear;
            }
            else
            {
                bg.color = GetFallbackNodeColor(node.RoomType);
            }
            
            bg.raycastTarget = _nodeSelectionEnabled;

            Button button = go.AddComponent<Button>();
            button.interactable = _nodeSelectionEnabled;

            // 아이콘 자식 오브젝트
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(_nodeSize * _iconScaleRatio, _nodeSize * _iconScaleRatio);
            iconRect.anchoredPosition = Vector2.zero;
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true; // 원본 이미지 비율 유지 (찌그러짐/잘림 방지)

            Sprite sprite = GetIcon(node.RoomType);
            if (sprite != null)
            {
                iconImg.sprite = sprite;
                iconImg.color = Color.white;
            }
            else
            {
                iconImg.sprite = null;
                iconImg.color = GetFallbackNodeColor(node.RoomType);
                iconGo.SetActive(_nodeBackgroundSprite != null);
            }

            var markerGo = new GameObject("VisitedMarker");
            markerGo.transform.SetParent(go.transform, false);
            var markerRect = markerGo.AddComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(_nodeSize * 1.42f, _nodeSize * 1.42f);
            markerRect.anchoredPosition = Vector2.zero;
            markerGo.transform.SetAsLastSibling();

            CreateVisitedRingImage(markerGo.transform, "VisitedRingShadow", _nodeSize * 1.42f,
                GetVisitedRingSprite(ref _visitedRingShadowSprite, new Color(0.04f, 0.02f, 0.01f, 1f), 20));
            markerGo.SetActive(node.IsVisited);

            var view = go.AddComponent<MapNodeView>();
            view.Setup(node, _nodeSelectionEnabled ? OnNodeClicked : null);
            view.SetState(_nodeSelectionEnabled && node.IsReachable, node.IsVisited);

            _nodeViews[node] = view;
        }

        private void CreateVisitedRingImage(Transform parent, string name, float size, Sprite sprite)
        {
            var ringGo = new GameObject(name);
            ringGo.transform.SetParent(parent, false);

            var ringRect = ringGo.AddComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(size, size);
            ringRect.anchoredPosition = Vector2.zero;

            var image = ringGo.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static Sprite GetVisitedRingSprite(ref Sprite cache, Color color, int thickness)
        {
            if (cache != null)
                return cache;

            cache = CreateVisitedRingSprite(color, thickness);
            return cache;
        }

        private static Sprite CreateVisitedRingSprite(Color color, int thickness)
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color clear = new Color(0f, 0f, 0f, 0f);
            float center = (size - 1f) * 0.5f;
            float outerRadius = center - 2f;
            float innerRadius = Mathf.Max(0f, outerRadius - thickness);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float outerAlpha = Mathf.Clamp01(outerRadius - distance + 1f);
                    float innerAlpha = Mathf.Clamp01(distance - innerRadius + 1f);
                    float alpha = Mathf.Min(outerAlpha, innerAlpha);

                    if (alpha <= 0f)
                    {
                        texture.SetPixel(x, y, clear);
                    }
                    else
                    {
                        Color pixel = color;
                        pixel.a *= alpha;
                        texture.SetPixel(x, y, pixel);
                    }
                }
            }

            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        // ====================================================================
        // 간선 생성 (코드로 GameObject 구성)
        // ====================================================================

        private void SpawnEdgeView(Vector2 from, Vector2 to)
        {
            var go = new GameObject("Edge");
            go.transform.SetParent(_mapContainer, false);

            var rect = go.AddComponent<RectTransform>();
            
            // [수정] 생성되는 선(Edge)의 앵커도 부모의 '맨 아래 중앙'으로 설정
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            
            rect.sizeDelta = new Vector2(10f, _edgeThickness); // 너비는 Setup()에서 덮어씀

            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.75f); // 검은색 실선 (투명도 50%)
            img.raycastTarget = false;

            var edge = go.AddComponent<MapEdgeView>();
            edge.Setup(from, to);
        }

        // ====================================================================
        // 노드 클릭 처리
        // ====================================================================

        private void OnNodeClicked(MapNode node)
        {
            if (!_nodeSelectionEnabled || node == null)
                return;

            int nodeId = node.RoomType == RoomType.Boss
                ? MapData.FLOORS * MapData.COLUMNS
                : node.Floor * MapData.COLUMNS + node.Column;

            OnNodeSelected?.Invoke(nodeId);
        }

        // ====================================================================
        // 헬퍼
        // ====================================================================

        private Vector2 GetNodePosition(int floor, int col)
        {
            float x = (col - (MapData.COLUMNS - 1) * 0.5f) * _nodeSpacingX;
            
            // 맨 아래(y=0)에 1층 노드가 딱 붙지 않도록 +1 하여 하단 여백 추가
            float y = (floor + 1) * _nodeSpacingY;
            return new Vector2(x, y);
        }

        private Sprite GetIcon(RoomType type)
        {
            return type switch
            {
                RoomType.Monster  => _iconMonster,
                RoomType.Elite    => _iconElite,
                RoomType.Event    => _iconEvent,
                RoomType.Treasure => _iconTreasure,
                RoomType.Rest     => _iconRest,
                RoomType.Shop     => _iconShop,
                RoomType.Boss     => _iconBoss,
                _                 => null
            };
        }

        private static Color GetFallbackNodeColor(RoomType type)
        {
            return type switch
            {
                RoomType.Monster => new Color(0.72f, 0.16f, 0.14f, 1f),
                RoomType.Elite => new Color(0.86f, 0.32f, 0.18f, 1f),
                RoomType.Event => new Color(0.45f, 0.28f, 0.74f, 1f),
                RoomType.Treasure => new Color(0.94f, 0.72f, 0.2f, 1f),
                RoomType.Rest => new Color(0.25f, 0.56f, 0.78f, 1f),
                RoomType.Shop => new Color(0.28f, 0.68f, 0.36f, 1f),
                RoomType.Boss => new Color(0.56f, 0.08f, 0.08f, 1f),
                _ => new Color(0.42f, 0.46f, 0.5f, 1f)
            };
        }

        private void ClearMap()
        {
            // 컨테이너가 할당되지 않았을 때의 NullReferenceException 방지
            if (_mapContainer == null) return;

            foreach (Transform child in _mapContainer)
            {
                if (child.name == "Background") continue;
                Destroy(child.gameObject);
            }
            _nodeViews.Clear();
        }

        private void EnsureEncounterUI()
        {
            if (_encounterOverlay != null)
                return;

            _encounterOverlay = CreateUIObject("RandomEncounterOverlay", transform);
            RectTransform overlayRect = _encounterOverlay.GetComponent<RectTransform>();
            Stretch(overlayRect);

            Image dim = _encounterOverlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            GameObject panel = CreateUIObject("EncounterPanel", _encounterOverlay.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            Center(panelRect, new Vector2(1120f, 620f), Vector2.zero);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.025f, 0.023f, 0.02f, 0.96f);

            GameObject ribbon = CreateUIObject("TitleRibbon", panel.transform);
            RectTransform ribbonRect = ribbon.GetComponent<RectTransform>();
            Center(ribbonRect, new Vector2(420f, 66f), new Vector2(-300f, 284f));
            Image ribbonImage = ribbon.AddComponent<Image>();
            ribbonImage.color = new Color(0.72f, 0.62f, 0.48f, 1f);

            _encounterTitleText = CreateEncounterText(
                "Text_EncounterTitle",
                ribbon.transform,
                "기묘한 조우",
                30,
                TextAlignmentOptions.Center,
                new Color(0.19f, 0.12f, 0.04f, 1f),
                Vector2.zero,
                new Vector2(380f, 54f),
                FontStyles.Bold);

            GameObject artFrame = CreateUIObject("EncounterArtworkFrame", panel.transform);
            RectTransform artRect = artFrame.GetComponent<RectTransform>();
            Center(artRect, new Vector2(380f, 380f), new Vector2(-330f, 54f));
            Image artFrameImage = artFrame.AddComponent<Image>();
            artFrameImage.color = new Color(0.08f, 0.07f, 0.08f, 1f);

            GameObject artObject = CreateUIObject("Image_EncounterArtwork", artFrame.transform);
            RectTransform imageRect = artObject.GetComponent<RectTransform>();
            Stretch(imageRect, new Vector2(18f, 18f), new Vector2(-18f, -18f));
            _encounterArtworkImage = artObject.AddComponent<Image>();
            _encounterArtworkImage.preserveAspect = true;
            _encounterArtworkImage.raycastTarget = false;

            _encounterArtworkFallbackText = CreateEncounterText(
                "Text_EncounterArtworkFallback",
                artFrame.transform,
                "?",
                126,
                TextAlignmentOptions.Center,
                new Color(0.78f, 0.72f, 0.58f, 1f),
                Vector2.zero,
                new Vector2(340f, 340f),
                FontStyles.Bold);

            _encounterStoryText = CreateEncounterText(
                "Text_EncounterStory",
                panel.transform,
                "",
                25,
                TextAlignmentOptions.TopLeft,
                new Color(0.93f, 0.9f, 0.82f, 1f),
                new Vector2(230f, 104f),
                new Vector2(620f, 292f));
            _encounterStoryText.textWrappingMode = TextWrappingModes.Normal;
            _encounterStoryText.enableAutoSizing = true;
            _encounterStoryText.fontSizeMin = 18;
            _encounterStoryText.fontSizeMax = 25;
            _encounterStoryText.overflowMode = TextOverflowModes.Truncate;

            GameObject choiceContainer = CreateUIObject("EncounterChoices", panel.transform);
            RectTransform choicesRect = choiceContainer.GetComponent<RectTransform>();
            Center(choicesRect, new Vector2(680f, 166f), new Vector2(220f, -190f));
            VerticalLayoutGroup layout = choiceContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 13f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _encounterChoiceContainer = choiceContainer.transform;

            _encounterOverlay.SetActive(false);
        }

        private Button CreateEncounterChoiceButton(EncounterChoice choice)
        {
            GameObject go = CreateUIObject("Button_EncounterChoice", _encounterChoiceContainer);
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = 680f;
            layout.preferredHeight = 46f;
            layout.minHeight = 46f;

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.14f, 0.42f, 0.42f, 0.96f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = choice != null && choice.Interactable && choice.OnSelected != null;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.14f, 0.42f, 0.42f, 0.96f);
            colors.highlightedColor = new Color(0.2f, 0.56f, 0.55f, 1f);
            colors.pressedColor = new Color(0.1f, 0.28f, 0.3f, 1f);
            colors.disabledColor = new Color(0.16f, 0.16f, 0.16f, 0.65f);
            button.colors = colors;

            TextMeshProUGUI label = CreateEncounterText(
                "Text_Choice",
                go.transform,
                choice != null ? choice.Label : "",
                22,
                TextAlignmentOptions.Center,
                Color.white,
                Vector2.zero,
                new Vector2(640f, 40f),
                FontStyles.Bold);
            label.enableAutoSizing = true;
            label.fontSizeMin = 16;
            label.fontSizeMax = 22;
            label.overflowMode = TextOverflowModes.Ellipsis;

            EncounterChoice captured = choice;
            button.onClick.AddListener(() =>
            {
                SoundManager.PlayDefaultUiClick();
                captured?.OnSelected?.Invoke();
            });

            return button;
        }

        private TextMeshProUGUI CreateEncounterText(
            string name,
            Transform parent,
            string text,
            int fontSize,
            TextAlignmentOptions alignment,
            Color color,
            Vector2 anchoredPosition,
            Vector2 size,
            FontStyles style = FontStyles.Normal)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            Center(rect, size, anchoredPosition);

            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.fontStyle = style;
            label.raycastTarget = false;
            return label;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private static void Center(RectTransform rect, Vector2 size, Vector2 anchoredPosition)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Sprite GetGeneratedEncounterArtwork()
        {
            if (_generatedEncounterArtwork != null)
                return _generatedEncounterArtwork;

            const int width = 384;
            const int height = 384;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color bottom = new Color(0.04f, 0.08f, 0.1f, 1f);
            Color top = new Color(0.16f, 0.09f, 0.22f, 1f);
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            float maxDistance = Mathf.Sqrt(center.x * center.x + center.y * center.y);

            for (int y = 0; y < height; y++)
            {
                float t = y / (height - 1f);
                for (int x = 0; x < width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                    Color color = Color.Lerp(bottom, top, t);
                    color *= Mathf.Lerp(1.05f, 0.48f, Mathf.Clamp01(distance));
                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
            }

            DrawCircle(texture, 300, 258, 58, new Color(0.11f, 0.36f, 0.36f, 1f));
            DrawCircle(texture, 305, 253, 44, new Color(0.21f, 0.72f, 0.64f, 1f));
            DrawDiamond(texture, 305, 253, 35, new Color(0.92f, 0.78f, 0.35f, 1f));
            DrawDiamond(texture, 305, 253, 20, new Color(1f, 0.92f, 0.55f, 1f));
            DrawCircle(texture, 294, 268, 6, new Color(1f, 0.98f, 0.82f, 1f));

            Color sleeve = new Color(0.09f, 0.08f, 0.12f, 1f);
            Color skin = new Color(0.72f, 0.55f, 0.42f, 1f);
            Color skinLight = new Color(0.86f, 0.68f, 0.5f, 1f);
            DrawRect(texture, 18, 165, 120, 223, sleeve);
            DrawRect(texture, 70, 180, 205, 210, skin);
            DrawCircle(texture, 203, 195, 34, skinLight);
            DrawRect(texture, 212, 212, 284, 230, skinLight);
            DrawCircle(texture, 285, 221, 9, skinLight);
            DrawRect(texture, 214, 189, 273, 205, skinLight);
            DrawCircle(texture, 274, 197, 8, skinLight);
            DrawRect(texture, 207, 165, 256, 181, skin);
            DrawCircle(texture, 257, 173, 8, skin);
            DrawCircle(texture, 181, 204, 9, new Color(0.95f, 0.78f, 0.58f, 1f));

            DrawCircle(texture, 74, 292, 18, new Color(0.28f, 0.2f, 0.48f, 1f));
            DrawCircle(texture, 95, 306, 7, new Color(0.72f, 0.63f, 0.94f, 1f));
            DrawCircle(texture, 125, 86, 14, new Color(0.2f, 0.5f, 0.44f, 1f));
            DrawCircle(texture, 142, 95, 5, new Color(0.7f, 0.95f, 0.82f, 1f));

            texture.Apply();
            _generatedEncounterArtwork = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            _generatedEncounterArtwork.hideFlags = HideFlags.HideAndDontSave;
            return _generatedEncounterArtwork;
        }

        private static void DrawRect(Texture2D texture, int xMin, int yMin, int xMax, int yMax, Color color)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                    SetPixelSafe(texture, x, y, color);
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
                        SetPixelSafe(texture, x, y, color);
                }
            }
        }

        private static void DrawDiamond(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    int distance = Mathf.Abs(x - centerX) + Mathf.Abs(y - centerY);
                    if (distance <= radius)
                        SetPixelSafe(texture, x, y, color);
                }
            }
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
                return;

            texture.SetPixel(x, y, color);
        }
    }
}
