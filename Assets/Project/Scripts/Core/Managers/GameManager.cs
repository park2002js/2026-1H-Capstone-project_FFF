using System.Collections.Generic;
using UnityEngine;
using FFF.UI.Core;
using FFF.UI.Title;
using FFF.UI.Main;
using FFF.UI.Map;
using FFF.UI.Shop;
using FFF.Map;
using FFF.UI.Battle;
using FFF.Data;
using FFF.Battle.Data;
using FFF.Audio;

namespace FFF.Core
{
    /// <summary>
    /// 최상위 게임 흐름 통솔자 (MVP - Presenter 최상위).
    ///
    /// 역할:
    /// - 씬 전환 결정
    /// - 각 씬의 View(UIComponent)에 델리게이트 연결
    /// - UIManager를 통해 화면 표시 명령
    /// - 씬 간 데이터(적 목록, 플레이어 데이터)를 전달
    ///
    /// 각 SceneSetup이 씬 준비 완료 시 OnXxxSceneReady()를 호출하면,
    /// GameManager가 View에 델리게이트를 연결하고 UIManager에 표시를 지시한다.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {

        [Header("=== Master Data ===")]
        [Tooltip("Map 및 게임 전반에서 원본으로 사용할 플레이어 데이터 SO")]
        [SerializeField] private PlayerDataSO _masterPlayerData;
        public PlayerDataSO MasterPlayerData => _masterPlayerData;

        private MapData _currentMapData;
        public MapData CurrentMapData => _currentMapData;

        // === 적 목록 관리 필드 ===
        private List<string> _normalEnemyList = new List<string>();
        private List<string> _eliteEnemyList = new List<string>();
        private List<string> _bossEnemyList = new List<string>();
        private System.Random _enemyRng;

        public string TargetEnemyId { get; private set; } // 전투로 넘길 적 ID 보관소

        // ================================================================
        // 씬별 준비 완료 알림 (SceneSetup → GameManager)
        // ================================================================

        public void OnTitleSceneReady(TitleUIComponent view)
        {
            view.OnExit = HandleTitleExit;
            UIManager.Instance.RegisterScreen(UIScreenNames.TITLE, view);
            UIManager.Instance.ShowScreen(UIScreenNames.TITLE);
            SoundManager.EnsureExists().PlaySceneBgm(SceneLoader.SceneNames.TITLE);
        }

        public void OnMainSceneReady(MainUIComponent view)
        {
            view.OnNewGame  = HandleNewGame;
            view.OnContinue = HandleContinue;
            UIManager.Instance.RegisterScreen(UIScreenNames.MAIN, view);
            UIManager.Instance.ShowScreen(UIScreenNames.MAIN);
            SoundManager.EnsureExists().PlaySceneBgm(SceneLoader.SceneNames.MAIN);
        }
        
        // ================================================================
        // 맵 생성 및 로드 시 적 목록 초기화 트리거
        // ================================================================
        public MapData GetOrCreateRunMap(bool useRandomSeed, int fixedSeed)
        {
            if (_currentMapData != null)
                return _currentMapData;

            if (TryRestoreRunMapFromPlayerData())
                return _currentMapData;

            int seed = useRandomSeed ? Random.Range(1, int.MaxValue) : fixedSeed;
            _currentMapData = new MapGenerator().Generate(seed);
            InitializeMapProgress(_currentMapData);
            SaveRunMapProgress();

            // 맵 생성 완료 후 해당 시드값으로 적 목록 생성
            InitializeEnemyLists(seed);

            Debug.Log($"[GameManager] 스테이지 맵 생성 및 적 목록 초기화 완료. seed={seed}");

            return _currentMapData;
        }

        public void OnMapSceneReady(MapUIComponent view, MapData mapData)
        {
            _currentMapData = mapData;
            view.SetMapData(mapData);
            view.OnNodeSelected = HandleStageSelect;
            UIManager.Instance.RegisterScreen(UIScreenNames.MAP, view);
            UIManager.Instance.ShowScreen(UIScreenNames.MAP);
            SoundManager.EnsureExists().PlaySceneBgm(SceneLoader.SceneNames.MAP);
        }

        public void OnBattleSceneReady(BattleUIComponent view)
        {
            UIManager.Instance.RegisterScreen(UIScreenNames.BATTLE, view);
            UIManager.Instance.ShowScreen(UIScreenNames.BATTLE);
            SoundManager.EnsureExists().PlaySceneBgm(SceneLoader.SceneNames.BATTLE);
        }

        public void OnShopSceneReady(ShopUIComponent view)
        {
            view.OnLeave = HandleShopLeave;
            view.OnAddDeckCard = HandleShopAddDeckCard;
            view.OnAddAccessory = HandleShopAddAccessory;
            view.OnDeckCardIdsRequested = GetShopDeckCardIds;
            view.OnRemoveDeckCard = HandleShopRemoveDeckCard;
            view.OnGoldRequested = GetPlayerGold;
            view.OnSpendGold = HandleSpendGold;
            UIManager.Instance.RegisterScreen(UIScreenNames.SHOP, view);
            UIManager.Instance.ShowScreen(UIScreenNames.SHOP);
            SoundManager.EnsureExists().PlaySceneBgm(SceneLoader.SceneNames.MAP);
        }

        public void UnregisterScreen(string screenName)
        {
            UIManager.Instance?.UnregisterScreen(screenName);
        }

        // ================================================================
        // 게임 흐름 결정 (GameManager 내부)
        // ================================================================
        private void HandleTitleExit()
        {
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAIN);
        }

        private void HandleNewGame()
        {
            ResetPlayerData();
            ResetRunMap();
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
        }

        private void HandleContinue()
        {
            if (_currentMapData == null)
                TryRestoreRunMapFromPlayerData();

            SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
        }

        private void ResetRunMap()
        {
            _currentMapData = null;
        }

        private void ResetPlayerData()
        {
            if (_masterPlayerData == null)
            {
                Debug.LogWarning("[GameManager] 초기화할 PlayerDataSO가 없습니다.");
                return;
            }

            _masterPlayerData.ResetToInitialState();
            Debug.Log("[GameManager] 플레이어 데이터를 초기 상태로 되돌렸습니다.");
        }

        /// <summary>
        /// 특정 스테이지로 이동할 때 호출됩니다.
        /// </summary>
        private void HandleStageSelect(int nodeId)
        {
            Debug.Log($"[GameManager] 스테이지 선택: nodeId={nodeId}");

            MapNode selectedNode = ResolveSelectedNode(nodeId);
            if (!CanSelectMapNode(selectedNode))
            {
                Debug.LogWarning($"[GameManager] 아직 선택할 수 없는 스테이지입니다. nodeId={nodeId}");
                SoundManager.PlaySfxSound(SoundIds.SfxMapNodeLocked);
                return;
            }

            VisitMapNode(selectedNode);
            SaveRunMapProgress();

            if (selectedNode.RoomType == RoomType.Shop)
            {
                SoundManager.PlaySfxSound(SoundIds.SfxMapEnterShop);
                SceneLoader.LoadScene(SceneLoader.SceneNames.SHOP);
                return;
            }

            // 시드값을 받아올 수 없는 경우 임의값 1을 전달
            int currentSeed = _currentMapData != null ? _currentMapData.Seed : 1;

            // RoomType에 맞춰 몬스터 ID를 추출하여 배정
            TargetEnemyId = PopEnemyFromList(selectedNode.RoomType, currentSeed);

            SoundManager.PlaySfxSound(selectedNode.RoomType == RoomType.Boss ? SoundIds.SfxMapEnterBoss : SoundIds.SfxMapEnterBattle);
            SceneLoader.LoadScene(SceneLoader.SceneNames.BATTLE);
        }

        private MapNode ResolveSelectedNode(int nodeId)
        {
            if (_currentMapData == null)
                return null;

            int bossNodeId = MapData.FLOORS * MapData.COLUMNS;
            if (nodeId == bossNodeId)
                return _currentMapData.BossNode;

            int floor = nodeId / MapData.COLUMNS;
            int column = nodeId % MapData.COLUMNS;
            return _currentMapData.GetNode(floor, column);
        }

        private bool CanSelectMapNode(MapNode node)
        {
            return node != null && node.IsReachable && !node.IsVisited;
        }

        private void InitializeMapProgress(MapData mapData)
        {
            if (mapData == null)
                return;

            foreach (var node in EnumerateMapNodes(mapData))
            {
                node.IsReachable = false;
                node.IsVisited = false;
            }

            foreach (var node in mapData.GetFloor(0))
            {
                node.IsReachable = true;
            }
        }

        private void VisitMapNode(MapNode selectedNode)
        {
            if (_currentMapData == null || selectedNode == null)
                return;

            foreach (var node in EnumerateMapNodes(_currentMapData))
            {
                node.IsReachable = false;
            }

            selectedNode.IsVisited = true;

            foreach (var nextNode in selectedNode.Next)
            {
                if (!nextNode.IsVisited)
                    nextNode.IsReachable = true;
            }
        }

        private IEnumerable<MapNode> EnumerateMapNodes(MapData mapData)
        {
            for (int floor = 0; floor < MapData.FLOORS; floor++)
            {
                for (int column = 0; column < MapData.COLUMNS; column++)
                {
                    var node = mapData.GetNode(floor, column);
                    if (node != null)
                        yield return node;
                }
            }

            if (mapData.BossNode != null)
                yield return mapData.BossNode;
        }

        private bool TryRestoreRunMapFromPlayerData()
        {
            if (_masterPlayerData == null ||
                !_masterPlayerData.HasSavedMapProgress ||
                _masterPlayerData.SavedMapSeed < 0)
            {
                return false;
            }

            MapData restoredMap = new MapGenerator().Generate(_masterPlayerData.SavedMapSeed);
            ApplySavedMapProgress(restoredMap);
            _currentMapData = restoredMap;

            // 복원된 맵의 시드값으로 적 목록 재생성
            InitializeEnemyLists(_masterPlayerData.SavedMapSeed);

            Debug.Log($"[GameManager] 시드값 기반으로 저장된 스테이지 맵 및 적 List 재생성. seed={_masterPlayerData.SavedMapSeed}");
            return true;
        }

        private void ApplySavedMapProgress(MapData mapData)
        {
            if (mapData == null)
                return;

            foreach (var node in EnumerateMapNodes(mapData))
            {
                node.IsVisited = false;
                node.IsReachable = false;
            }

            ApplyNodeIds(mapData, _masterPlayerData.SavedVisitedNodeIds, isVisited: true);
            ApplyNodeIds(mapData, _masterPlayerData.SavedReachableNodeIds, isVisited: false);

            if (!HasAnyReachableNode(mapData))
            {
                foreach (var node in mapData.GetFloor(0))
                    node.IsReachable = true;
            }
        }

        private void ApplyNodeIds(MapData mapData, IReadOnlyList<int> nodeIds, bool isVisited)
        {
            if (nodeIds == null)
                return;

            for (int i = 0; i < nodeIds.Count; i++)
            {
                MapNode node = ResolveNode(mapData, nodeIds[i]);
                if (node == null)
                    continue;

                if (isVisited)
                    node.IsVisited = true;
                else if (!node.IsVisited)
                    node.IsReachable = true;
            }
        }

        private bool HasAnyReachableNode(MapData mapData)
        {
            foreach (var node in EnumerateMapNodes(mapData))
            {
                if (node.IsReachable)
                    return true;
            }

            return false;
        }

        private void SaveRunMapProgress()
        {
            if (_masterPlayerData == null || _currentMapData == null)
                return;

            var visitedNodeIds = new List<int>();
            var reachableNodeIds = new List<int>();

            foreach (var node in EnumerateMapNodes(_currentMapData))
            {
                int nodeId = GetNodeId(node);
                if (node.IsVisited)
                    visitedNodeIds.Add(nodeId);
                if (node.IsReachable)
                    reachableNodeIds.Add(nodeId);
            }

            _masterPlayerData.SaveMapProgress(_currentMapData.Seed, visitedNodeIds, reachableNodeIds);
        }

        private MapNode ResolveNode(MapData mapData, int nodeId)
        {
            if (mapData == null)
                return null;

            int bossNodeId = MapData.FLOORS * MapData.COLUMNS;
            if (nodeId == bossNodeId)
                return mapData.BossNode;

            int floor = nodeId / MapData.COLUMNS;
            int column = nodeId % MapData.COLUMNS;
            return mapData.GetNode(floor, column);
        }

        private int GetNodeId(MapNode node)
        {
            if (node == null)
                return -1;

            return node.RoomType == RoomType.Boss
                ? MapData.FLOORS * MapData.COLUMNS
                : node.Floor * MapData.COLUMNS + node.Column;
        }

        private void HandleShopLeave()
        {
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
        }

        private IReadOnlyList<string> GetShopDeckCardIds()
        {
            return _masterPlayerData != null && _masterPlayerData.DeckCardIds != null
                ? _masterPlayerData.DeckCardIds
                : new List<string>();
        }

        private void HandleShopAddDeckCard(string cardId)
        {
            _masterPlayerData?.AddDeckCard(cardId);
        }

        private void HandleShopAddAccessory(string accessoryId)
        {
            _masterPlayerData?.AddAccessory(accessoryId);
        }

        private void HandleShopRemoveDeckCard(string cardId)
        {
            if (_masterPlayerData != null && !_masterPlayerData.RemoveDeckCard(cardId))
                Debug.LogWarning($"[GameManager] 제거할 카드가 덱에 없습니다. CardId={cardId}");
        }

        private int GetPlayerGold()
        {
            return _masterPlayerData != null ? _masterPlayerData.CurrentGold : 0;
        }

        private bool HandleSpendGold(int amount)
        {
            return _masterPlayerData != null && _masterPlayerData.SpendGold(amount);
        }

        /// <summary>
        /// 전투가 끝나고 Map으로 돌아갈 때 호출됩니다.
        /// </summary>
        public void HandleReturnToMap(PlayerDataBattle finalBattleData)
        {
            Debug.Log("[GameManager] 전투 종료. 맵으로 귀환하며 데이터를 동기화합니다.");

            // 1. PlayerDataUpdater 객체 생성
            PlayerDataUpdater updater = new PlayerDataUpdater();
            
            // 2. 동기화 실행 (로컬 데이터 -> SO 데이터)
            updater.SyncBattleDataToMaster(finalBattleData, _masterPlayerData);

            // 3. 동기화 완료 후 Map 씬으로 이동
            // (updater 객체는 이 메서드가 끝나면 지역 변수이므로 가비지 컬렉터에 의해 자동으로 메모리에서 제거됨)
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
        }

        // ================================================================
        #region 적 목록 초기화 및 셔플 로직

        /// <summary>
        /// 시드값을 기반으로 등급별 적 목록을 구성하고 셔플합니다.
        /// </summary>
        private void InitializeEnemyLists(int seed)
        {
            _enemyRng = new System.Random(seed);

            _normalEnemyList = new List<string> { "Enemy_001", "Enemy_002", "Enemy_003", "Enemy_004", "Enemy_005" };
            ShuffleList(_normalEnemyList, _enemyRng);

            _eliteEnemyList = new List<string> { "Enemy_006", "Enemy_007" };
            ShuffleList(_eliteEnemyList, _enemyRng);

            _bossEnemyList = new List<string> { "Enemy_008" };
            ShuffleList(_bossEnemyList, _enemyRng);

            Debug.Log($"[GameManager] 등급별 적 목록 초기화 및 셔플 완료. 적용된 시드값: {seed}");
        }

        /// <summary>
        /// Fisher-Yates 알고리즘을 이용해 리스트 요소를 무작위로 섞습니다.
        /// </summary>
        private void ShuffleList(List<string> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// 룸 타입에 맞는 적 목록에서 마지막 요소를 꺼내(Pop) 반환합니다.
        /// 리스트가 비어있을 경우 원본 ID 목록으로 재충전 후 다시 셔플합니다.
        /// </summary>
        private string PopEnemyFromList(RoomType roomType, int fallbackSeed)
        {
            // RNG가 초기화되지 않은 예외 상황 시 폴백(Fallback) 시드값으로 생성
            if (_enemyRng == null)
            {
                _enemyRng = new System.Random(fallbackSeed);
            }

            List<string> targetList;
            List<string> defaultIds;

            switch (roomType)
            {
                // 엘리트 룸 타입이 MapSystem 내부에 구현되어 있다고 가정합니다. (구현에 따라 Enum명 변경 요망)
                case RoomType.Elite: 
                    targetList = _eliteEnemyList;
                    defaultIds = new List<string> { "Enemy_006", "Enemy_007" };
                    break;
                case RoomType.Boss:
                    targetList = _bossEnemyList;
                    defaultIds = new List<string> { "Enemy_008" };
                    break;
                default: 
                    targetList = _normalEnemyList;
                    defaultIds = new List<string> { "Enemy_001", "Enemy_002", "Enemy_003", "Enemy_004", "Enemy_005" };
                    break;
            }

            // 리스트 고갈 시 재충전 및 셔플 진행
            if (targetList.Count == 0)
            {
                targetList.AddRange(defaultIds);
                ShuffleList(targetList, _enemyRng);
                Debug.Log($"[GameManager] {roomType} 등급 적 목록이 고갈되어 리스트를 재충전하고 셔플했습니다.");
            }

            string selectedEnemyId = targetList[targetList.Count - 1];
            targetList.RemoveAt(targetList.Count - 1);

            return selectedEnemyId;
        }
        #endregion
    }
}
