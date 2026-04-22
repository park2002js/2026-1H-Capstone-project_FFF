using System;
using UnityEngine;
using UnityEngine.UI;
using FFF.Map;

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
        private Image _icon;
        private MapNode _node;
        private Action<MapNode> _onClick;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            _button = GetComponent<Button>();
            _icon = GetComponentInChildren<Image>(includeInactive: true);
            _button.onClick.AddListener(HandleClick);
        }

        public void Setup(MapNode node, Action<MapNode> onClick)
        {
            _node = node;
            _onClick = onClick;
        }

        public void SetIcon(Sprite sprite)
        {
            if (_icon != null) _icon.sprite = sprite;
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

        private void HandleClick()
        {
            _onClick?.Invoke(_node);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }
}
