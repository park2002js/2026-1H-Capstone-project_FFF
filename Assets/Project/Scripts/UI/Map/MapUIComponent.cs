using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FFF.UI.Core;
using FFF.Map;

namespace FFF.UI.Map
{
    /// <summary>
    /// 맵 화면 전체를 담당하는 UI 컴포넌트.
    /// 노드·간선을 코드로 직접 생성하므로 별도 프리팹이 필요 없다.
    /// </summary>
    public class MapUIComponent : BaseUIComponent
    {
        /// <summary>GameManager가 연결하는 노드 선택 델리게이트.</summary>
        public Action<int> OnNodeSelected;

        [Header("레이아웃")]
        [SerializeField] private RectTransform _mapContainer;
        [SerializeField] private float _nodeSpacingX = 100f;
        [SerializeField] private float _nodeSpacingY = 80f;
        [SerializeField] private float _nodeSize = 60f;
        [SerializeField] private float _edgeThickness = 4f;

        [Header("방 타입 아이콘 (선택 — 없으면 색상으로 구분)")]
        [SerializeField] private Sprite _iconMonster;
        [SerializeField] private Sprite _iconElite;
        [SerializeField] private Sprite _iconEvent;
        [SerializeField] private Sprite _iconTreasure;
        [SerializeField] private Sprite _iconRest;
        [SerializeField] private Sprite _iconShop;
        [SerializeField] private Sprite _iconBoss;

        private MapData _mapData;
        private readonly Dictionary<MapNode, MapNodeView> _nodeViews = new Dictionary<MapNode, MapNodeView>();

        public void SetMapData(MapData data)
        {
            _mapData = data;
        }

        protected override void OnShow()
        {
            if (_mapData != null) BuildMap();
        }

        protected override void OnHide()
        {
            ClearMap();
        }

        // ====================================================================
        // 맵 빌드
        // ====================================================================

        private void BuildMap()
        {
            ClearMap();
            if (_mapData == null) return;

            // 노드 뷰 생성
            for (int floor = 0; floor < MapData.FLOORS; floor++)
                for (int col = 0; col < MapData.COLUMNS; col++)
                {
                    var node = _mapData.GetNode(floor, col);
                    if (node != null) SpawnNodeView(node);
                }

            if (_mapData.BossNode != null)
                SpawnNodeView(_mapData.BossNode);

            // 간선 뷰 생성 (SetAsFirstSibling으로 노드 뒤에 렌더링)
            foreach (var kvp in _nodeViews)
            {
                foreach (var next in kvp.Key.Next)
                {
                    if (!_nodeViews.TryGetValue(next, out var nextView)) continue;
                    SpawnEdgeView(
                        kvp.Value.RectTransform.anchoredPosition,
                        nextView.RectTransform.anchoredPosition
                    );
                }
            }
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
            rect.sizeDelta = new Vector2(_nodeSize, _nodeSize);
            rect.anchoredPosition = GetNodePosition(node.Floor, node.Column);

            var bg = go.AddComponent<Image>();
            bg.color = GetRoomColor(node.RoomType);

            go.AddComponent<Button>();

            // 아이콘 자식 오브젝트
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(_nodeSize * 0.65f, _nodeSize * 0.65f);
            iconRect.anchoredPosition = Vector2.zero;
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;

            Sprite sprite = GetIcon(node.RoomType);
            if (sprite != null)
                iconImg.sprite = sprite;
            else
                iconGo.SetActive(false);

            var view = go.AddComponent<MapNodeView>();
            view.Setup(node, OnNodeClicked);

            _nodeViews[node] = view;
        }

        // ====================================================================
        // 간선 생성 (코드로 GameObject 구성)
        // ====================================================================

        private void SpawnEdgeView(Vector2 from, Vector2 to)
        {
            var go = new GameObject("Edge");
            go.transform.SetParent(_mapContainer, false);
            go.transform.SetAsFirstSibling();

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(10f, _edgeThickness); // 너비는 Setup()에서 덮어씀

            var img = go.AddComponent<Image>();
            img.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            img.raycastTarget = false;

            var edge = go.AddComponent<MapEdgeView>();
            edge.Setup(from, to);
        }

        // ====================================================================
        // 노드 클릭 처리
        // ====================================================================

        private void OnNodeClicked(MapNode node)
        {
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
            float y = floor * _nodeSpacingY;
            return new Vector2(x, y);
        }

        private static Color GetRoomColor(RoomType type)
        {
            return type switch
            {
                RoomType.Monster  => new Color(0.85f, 0.35f, 0.35f),
                RoomType.Elite    => new Color(0.75f, 0.20f, 0.20f),
                RoomType.Event    => new Color(0.40f, 0.65f, 0.90f),
                RoomType.Treasure => new Color(0.95f, 0.80f, 0.20f),
                RoomType.Rest     => new Color(0.35f, 0.75f, 0.45f),
                RoomType.Shop     => new Color(0.80f, 0.55f, 0.90f),
                RoomType.Boss     => new Color(0.20f, 0.10f, 0.10f),
                _                 => Color.white
            };
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
    }
}
