using System;
using UnityEngine;
using UnityEngine.UI;
using FFF.Map;
using FFF.Audio;

namespace FFF.UI.Map
{
    /// <summary>
    /// 맵 위의 개별 노드(방) 하나를 표현하는 UI 컴포넌트.
    /// MapUIComponent.SpawnNodeView()에서 코드로 생성된다 — 프리팹 불필요.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MapNodeView : MonoBehaviour
    {
        public RectTransform RectTransform { get; private set; }

        private Button _button;
        private Image _iconImage;
        private GameObject _visitedMarker;
        private MapVisitedRingGraphic _visitedRing;
        private MapNode _node;
        private Action<MapNode> _onClick;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            _button = GetComponent<Button>();
            _iconImage = transform.Find("Icon")?.GetComponent<Image>();
            _visitedMarker = transform.Find("VisitedMarker")?.gameObject;
            _visitedRing = GetComponentInChildren<MapVisitedRingGraphic>(includeInactive: true);
            _button.onClick.AddListener(HandleClick);
        }

        public void Setup(MapNode node, Action<MapNode> onClick)
        {
            _node = node;
            _onClick = onClick;
        }

        public void SetIcon(Sprite sprite)
        {
            if (_iconImage != null) _iconImage.sprite = sprite;
        }

        public void SetColor(Color color)
        {
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = color;
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }

        public void SetState(bool isReachable, bool isVisited)
        {
            SetInteractable(isReachable && !isVisited);

            if (_iconImage != null)
            {
                Color color = _iconImage.color;
                color.a = 1f;
                _iconImage.color = color;
            }

            if (_visitedMarker != null)
                _visitedMarker.SetActive(isVisited);
            else if (_visitedRing != null)
                _visitedRing.gameObject.SetActive(isVisited);
        }

        private void HandleClick()
        {
            SoundManager.PlayDefaultUiClick();
            _onClick?.Invoke(_node);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

}
