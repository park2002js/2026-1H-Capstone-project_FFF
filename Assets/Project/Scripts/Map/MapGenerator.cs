using System;
using System.Collections.Generic;

namespace FFF.Map
{
    /// <summary>
    /// 시드 기반 맵 생성기. Slay the Spire 방식의 7x15 노드 맵을 생성한다.
    ///
    /// 생성 순서:
    /// 1. 6개의 경로를 1층~15층까지 생성 (교차 금지)
    /// 2. 경로 위의 노드에 방 타입 배정 (고정 층 먼저, 나머지 랜덤)
    /// 3. 15층 전체를 보스 노드에 연결
    ///
    /// 확률 기준: A20 승천 (몬스터 45%, 이벤트 22%, 엘리트 16%, 휴식 12%, 상점 5%)
    /// </summary>
    public class MapGenerator
    {
        private const int FLOORS = MapData.FLOORS;
        private const int COLUMNS = MapData.COLUMNS;
        private const int PATH_COUNT = 6;
        private const int MAX_ATTEMPTS = 100;

        // A20 기준 가중 풀: 각 타입이 100개 항목에서 차지하는 비율
        private static readonly RoomType[] _weightedPool = BuildWeightedPool();

        private System.Random _rng;

        public MapData Generate(int seed)
        {
            _rng = new System.Random(seed);
            var data = new MapData { Seed = seed };

            GeneratePaths(data);
            AssignRoomTypes(data);
            LinkBossNode(data);

            return data;
        }

        // ====================================================================
        // 1단계: 경로 생성
        // ====================================================================

        private void GeneratePaths(MapData data)
        {
            // floorEdges[f] = f층 → f+1층 사이의 (fromCol, toCol) 간선 목록 (교차 검사용)
            var floorEdges = new List<(int from, int to)>[FLOORS - 1];
            for (int i = 0; i < FLOORS - 1; i++)
                floorEdges[i] = new List<(int, int)>();

            int prevStartCol = -1;

            for (int p = 0; p < PATH_COUNT; p++)
            {
                int startCol = PickStartColumn(p, prevStartCol);
                prevStartCol = startCol;

                EnsureNode(data, 0, startCol);
                int currentCol = startCol;

                for (int floor = 0; floor < FLOORS - 1; floor++)
                {
                    int nextCol = PickNextColumn(currentCol, floor, floorEdges);

                    EnsureNode(data, floor, currentCol);
                    EnsureNode(data, floor + 1, nextCol);

                    var fromNode = data.Nodes[floor, currentCol];
                    var toNode = data.Nodes[floor + 1, nextCol];

                    if (!fromNode.Next.Contains(toNode))
                    {
                        fromNode.Next.Add(toNode);
                        toNode.Prev.Add(fromNode);
                    }

                    floorEdges[floor].Add((currentCol, nextCol));
                    currentCol = nextCol;
                }
            }
        }

        private int PickStartColumn(int pathIndex, int prevStartCol)
        {
            // 첫 번째와 두 번째 경로의 시작 열은 반드시 달라야 한다
            if (pathIndex != 1)
                return _rng.Next(0, COLUMNS);

            int col;
            int attempts = 0;
            do
            {
                col = _rng.Next(0, COLUMNS);
                attempts++;
            }
            while (col == prevStartCol && attempts < MAX_ATTEMPTS);

            return col;
        }

        private int PickNextColumn(int currentCol, int floor, List<(int from, int to)>[] floorEdges)
        {
            int[] dxOptions = ShuffledDx();
            foreach (int dx in dxOptions)
            {
                int nextCol = Math.Max(0, Math.Min(COLUMNS - 1, currentCol + dx));
                if (!WouldCross(currentCol, nextCol, floor, floorEdges))
                    return nextCol;
            }
            return currentCol; // 유효한 방향이 없을 경우 제자리 유지
        }

        // 두 간선 (from→to)와 (a→b)가 교차하는지 확인
        // 교차: 출발점 순서와 도착점 순서가 반대인 경우
        private bool WouldCross(int from, int to, int floor, List<(int from, int to)>[] floorEdges)
        {
            foreach (var (a, b) in floorEdges[floor])
            {
                if ((from < a && to > b) || (from > a && to < b))
                    return true;
            }
            return false;
        }

        // Fisher-Yates 셔플로 {-1, 0, 1} 순서를 랜덤화
        private int[] ShuffledDx()
        {
            int[] dx = { -1, 0, 1 };
            for (int i = 2; i > 0; i--)
            {
                int j = _rng.Next(0, i + 1);
                int tmp = dx[i]; dx[i] = dx[j]; dx[j] = tmp;
            }
            return dx;
        }

        private static void EnsureNode(MapData data, int floor, int col)
        {
            if (data.Nodes[floor, col] == null)
                data.Nodes[floor, col] = new MapNode { Floor = floor, Column = col };
        }

        // ====================================================================
        // 2단계: 방 타입 배정
        // ====================================================================

        private void AssignRoomTypes(MapData data)
        {
            // 고정 층 먼저 배정
            SetFloorType(data, 0, RoomType.Monster);   // 1층: 전부 몬스터
            SetFloorType(data, 8, RoomType.Treasure);  // 9층: 전부 상자
            SetFloorType(data, FLOORS - 1, RoomType.Rest); // 15층: 전부 휴식

            // 나머지 층을 위에서부터 랜덤 배정 (같은 층 내 좌→우 순서)
            for (int floor = 1; floor < FLOORS - 1; floor++)
            {
                if (floor == 8) continue;
                for (int col = 0; col < COLUMNS; col++)
                {
                    var node = data.Nodes[floor, col];
                    if (node == null || node.RoomType != RoomType.None) continue;
                    node.RoomType = PickRoomType(node, floor);
                }
            }
        }

        private void SetFloorType(MapData data, int floor, RoomType type)
        {
            for (int col = 0; col < COLUMNS; col++)
            {
                if (data.Nodes[floor, col] != null)
                    data.Nodes[floor, col].RoomType = type;
            }
        }

        private RoomType PickRoomType(MapNode node, int floor)
        {
            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                var type = _weightedPool[_rng.Next(0, _weightedPool.Length)];
                if (IsValidType(node, type, floor))
                    return type;
            }
            return RoomType.Monster; // 모든 시도 실패 시 몬스터로 대체
        }

        private bool IsValidType(MapNode node, RoomType type, int floor)
        {
            // 엘리트·휴식은 6층(인덱스 5) 미만 배정 불가
            if (floor < 5 && (type == RoomType.Elite || type == RoomType.Rest))
                return false;

            // 휴식은 14층(인덱스 13) 배정 불가
            if (floor == 13 && type == RoomType.Rest)
                return false;

            // 제한 타입(엘리트·상점·휴식)은 직접 연결된 이전 방과 같은 타입 불가
            bool isRestricted = type == RoomType.Elite || type == RoomType.Shop || type == RoomType.Rest;
            if (isRestricted)
            {
                foreach (var prev in node.Prev)
                {
                    if (prev.RoomType == type) return false;
                }
            }

            // 같은 부모에서 나오는 형제 노드(이미 배정된)와 타입 중복 불가
            foreach (var prev in node.Prev)
            {
                if (prev.Next.Count < 2) continue;
                foreach (var sibling in prev.Next)
                {
                    if (sibling != node && sibling.RoomType == type)
                        return false;
                }
            }

            return true;
        }

        // ====================================================================
        // 3단계: 보스 연결
        // ====================================================================

        private void LinkBossNode(MapData data)
        {
            data.BossNode = new MapNode { Floor = FLOORS, Column = 0, RoomType = RoomType.Boss };
            for (int col = 0; col < COLUMNS; col++)
            {
                var node = data.Nodes[FLOORS - 1, col];
                if (node == null) continue;
                node.Next.Add(data.BossNode);
                data.BossNode.Prev.Add(node);
            }
        }

        // ====================================================================
        // 가중 풀 초기화
        // ====================================================================

        private static RoomType[] BuildWeightedPool()
        {
            var pool = new List<RoomType>();
            AddToPool(pool, RoomType.Monster, 45);
            AddToPool(pool, RoomType.Event, 22);
            AddToPool(pool, RoomType.Elite, 16);
            AddToPool(pool, RoomType.Rest, 12);
            AddToPool(pool, RoomType.Shop, 5);
            return pool.ToArray();
        }

        private static void AddToPool(List<RoomType> pool, RoomType type, int count)
        {
            for (int i = 0; i < count; i++) pool.Add(type);
        }
    }
}
