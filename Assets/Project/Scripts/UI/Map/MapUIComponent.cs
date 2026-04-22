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
                // 배경 이미지가 지정되지 않았을 때 나타나는 기본 '하얀색 네모'를 투명하게 숨깁니다.
                bg.color = Color.clear;
            }
            
            go.AddComponent<Button>();

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
