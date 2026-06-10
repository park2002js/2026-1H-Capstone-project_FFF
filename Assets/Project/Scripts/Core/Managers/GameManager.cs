using System.Collections.Generic;
using UnityEngine;
using FFF.UI.Core;
using FFF.UI.Title;
using FFF.UI.Main;
using FFF.UI.Map;
using FFF.UI.Shop;
using FFF.UI.Rest;
using FFF.UI.Treasure;
using FFF.UI.Common;
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
        private enum MapEncounterKind
        {
            Battle,
            Shop,
            Reward
        }

        private readonly struct RewardCatalogItem
        {
            public readonly string Id;
            public readonly string DisplayName;

            public RewardCatalogItem(string id, string displayName)
            {
                Id = id;
                DisplayName = displayName;
            }
        }

        private sealed class MapEventRewardCandidate
        {
            public string Label;
            public string Feedback;
            public string SoundId;
            public System.Action Apply;
        }

        private static readonly RewardCatalogItem[] EventJokerRewardPool =
        {
            new RewardCatalogItem("Jocker_001", "각시탈"),
            new RewardCatalogItem("Jocker_002", "분이"),
            new RewardCatalogItem("Jocker_003", "목자"),
            new RewardCatalogItem("Jocker_004", "양반"),
            new RewardCatalogItem("Jocker_005", "소무")
        };

        private static readonly RewardCatalogItem[] EventAccessoryRewardPool =
        {
            new RewardCatalogItem("Accessory_001", "옥가락지"),
            new RewardCatalogItem("Accessory_002", "갓"),
            new RewardCatalogItem("Accessory_003", "은장도"),
            new RewardCatalogItem("Accessory_004", "마패"),
            new RewardCatalogItem("Accessory_005", "노리개")
        };

        [Header("=== Master Data ===")]
        [Tooltip("Map 및 게임 전반에서 원본으로 사용할 플레이어 데이터 SO")]
        [SerializeField] private PlayerDataSO _masterPlayerData;
        public PlayerDataSO MasterPlayerData => _masterPlayerData;

        [Header("=== 랜덤 인카운터 아트워크 ===")]
        [SerializeField] private Sprite _battleEncounterArtwork;
        [SerializeField] private Sprite _shopEncounterArtwork;
        [SerializeField] private Sprite _rewardEncounterArtwork;

        private MapData _currentMapData;
        public MapData CurrentMapData => _currentMapData;
        private MapUIComponent _activeMapView;
        private TopRunHudComponent _activeRunHud;
        private int _lastSelectedMapNodeId = -1;
        
        // === <ver 1.2.2 System> 신규 추가 ===
        // ================================================================
        // 씬 간 공유되는 데이터 관리
        // ================================================================
        /// <summary> TableSystemManager 접근용 프로퍼티 </summary>
        public TableSystemManager TableSystem { get; private set; }
        
        /// <summary> 전투 씬으로 전달할 데이터를 담은 구조체 </summary>
        public BattleEntryData CurrentBattleEntryData { get; private set; }

        // === 적 목록 관리 필드 ===
        private List<string> _normalEnemyList = new List<string>();
        private List<string> _eliteEnemyList = new List<string>();
        private List<string> _bossEnemyList = new List<string>();
        private System.Random _enemyRng;

        public string TargetEnemyId { get; private set; } // 전투로 넘길 적 ID 보관소
        public RoomType TargetRoomType { get; private set; } = RoomType.Monster;
        public string TargetEnemyVisualId { get; private set; }

        private readonly List<string> _runtimeSeenNormalEnemyVisualIds = new List<string>();

        protected override void OnInitialize()
        {
            base.OnInitialize(); // 상위 Singleton의 OnInitialize가 있다면 호출
            
            // TableSystemManager 객체 초기화 생성
            TableSystem = new TableSystemManager();
        }

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
            view.OnQuit = HandleQuit;
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

            // TableSystemManager 시드값 전달 및 테이블 초기화 트리거
            TableSystem.Initialize(seed);

            Debug.Log($"[GameManager] 스테이지 맵 생성 및 적 목록 초기화 완료. seed={seed}");

            return _currentMapData;
        }

        public MapData GetCurrentOrRestoreRunMap()
        {
            if (_currentMapData != null)
                return _currentMapData;

            TryRestoreRunMapFromPlayerData();
            return _currentMapData;
        }

        public void OnMapSceneReady(MapUIComponent view, MapData mapData)
        {
            _currentMapData = mapData;
            _activeMapView = view;
            view.SetMapData(mapData);
            view.OnNodeSelected = HandleStageSelect;
            UIManager.Instance.RegisterScreen(UIScreenNames.MAP, view);
            UIManager.Instance.ShowScreen(UIScreenNames.MAP);
            HydrateRunHud(view);
            if (_lastSelectedMapNodeId >= 0)
                view.FocusNode(_lastSelectedMapNodeId);
            SoundManager.EnsureExists().PlaySceneBgm(SceneLoader.SceneNames.MAP);
        }

        public void OnBattleSceneReady(BattleUIComponent view)
        {
            UIManager.Instance.RegisterScreen(UIScreenNames.BATTLE, view);
            UIManager.Instance.ShowScreen(UIScreenNames.BATTLE);
            HydrateBattleHud(view);
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
            HydrateRunHud(view);
            SoundManager.EnsureExists().PlaySceneBgm(SceneLoader.SceneNames.SHOP);
        }

        public void OnRestSceneReady(RestUIComponent view)
        {
            view.OnRestRequested = HandleRestRequested;
            if (_masterPlayerData != null)
                view.SetPlayerHealth(_masterPlayerData.CurrentHealth, _masterPlayerData.MaxHealth);

            UIManager.Instance.RegisterScreen(UIScreenNames.REST, view);
            UIManager.Instance.ShowScreen(UIScreenNames.REST);
            HydrateRunHud(view);
            SoundManager.EnsureExists().PlaySceneBgm(SceneLoader.SceneNames.REST);
        }

        public void OnTreasureSceneReady(TreasureUIComponent view)
        {
            view.OnLeave = HandleTreasureLeave;
            view.OnRewardClaimed = HandleTreasureRewardClaimed;
            view.SetReward(CreateTreasureReward());

            UIManager.Instance.RegisterScreen(UIScreenNames.TREASURE, view);
            UIManager.Instance.ShowScreen(UIScreenNames.TREASURE);
            HydrateRunHud(view);
            SoundManager.EnsureExists().PlaySceneBgm(SceneLoader.SceneNames.TREASURE);
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

        private void HandleQuit()
        {
            Debug.Log("[GameManager] 게임 종료 요청을 처리합니다.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ResetRunMap()
        {
            _currentMapData = null;
            _lastSelectedMapNodeId = -1;
        }

        private void ResetPlayerData()
        {
            if (_masterPlayerData == null)
            {
                Debug.LogWarning("[GameManager] 초기화할 PlayerDataSO가 없습니다.");
                _runtimeSeenNormalEnemyVisualIds.Clear();
                return;
            }

            _masterPlayerData.ResetToInitialState();
            _runtimeSeenNormalEnemyVisualIds.Clear();
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
            _lastSelectedMapNodeId = nodeId;
            SaveRunMapProgress();

            if (selectedNode.RoomType == RoomType.Event)
            {
                ShowRandomMapEncounter(selectedNode);
                return;
            }

            if (selectedNode.RoomType == RoomType.Shop)
            {
                EnterShopFromMap();
                return;
            }

            if (selectedNode.RoomType == RoomType.Rest)
            {
                EnterRestFromMap();
                return;
            }

            if (selectedNode.RoomType == RoomType.Treasure)
            {
                EnterTreasureFromMap();
                return;
            }

            EnterBattleFromMap(selectedNode.RoomType);
        }

        private void ShowRandomMapEncounter(MapNode selectedNode)
        {
            if (_activeMapView == null)
            {
                EnterBattleFromMap(RoomType.Monster);
                return;
            }

            MapEncounterKind kind = RollMapEncounterKind(selectedNode);
            switch (kind)
            {
                case MapEncounterKind.Battle:
                    ShowBattleEncounter();
                    break;
                case MapEncounterKind.Shop:
                    ShowShopEncounter();
                    break;
                default:
                    ShowRewardEncounter(selectedNode);
                    break;
            }
        }

        private MapEncounterKind RollMapEncounterKind(MapNode node)
        {
            var rng = new System.Random(BuildMapNodeSeed(node, 9173));
            int roll = rng.Next(0, 100);
            if (roll < 35)
                return MapEncounterKind.Battle;
            if (roll < 60)
                return MapEncounterKind.Shop;
            return MapEncounterKind.Reward;
        }

        private void ShowBattleEncounter()
        {
            var encounter = new MapUIComponent.EncounterViewModel
            {
                Title = "낯선 기척",
                Story = "길 위의 안개가 갈라지자 누군가가 길목을 막아섭니다.\n\n돌아갈 길은 이미 사라졌고, 남은 것은 정면으로 맞서는 일뿐입니다.",
                Artwork = _battleEncounterArtwork
            };
            encounter.Choices.Add(new MapUIComponent.EncounterChoice("맞선다", () => EnterBattleFromMap(RoomType.Monster)));
            _activeMapView.ShowEncounter(encounter);
        }

        private void ShowShopEncounter()
        {
            var encounter = new MapUIComponent.EncounterViewModel
            {
                Title = "길목의 보따리",
                Story = "빛이 거의 닿지 않는 길목에 낡은 보따리 하나가 놓여 있습니다.\n\n보따리 옆의 표식은 이곳이 잠깐 숨을 고르고 물건을 살 수 있는 자리임을 알려줍니다.",
                Artwork = _shopEncounterArtwork
            };
            encounter.Choices.Add(new MapUIComponent.EncounterChoice("상점을 살펴본다", EnterShopFromMap));
            _activeMapView.ShowEncounter(encounter);
        }

        private void ShowRewardEncounter(MapNode selectedNode)
        {
            var encounter = new MapUIComponent.EncounterViewModel
            {
                Title = "미지의 손",
                Story = "장막 너머에서 정체를 알 수 없는 손이 천천히 나타납니다.\n\n세부 이야기는 이곳에 작성하면 됩니다. 선택한 보상만 손 안에 남습니다.",
                Artwork = _rewardEncounterArtwork
            };

            foreach (MapUIComponent.EncounterChoice choice in BuildRewardEncounterChoices(selectedNode))
                encounter.Choices.Add(choice);

            _activeMapView.ShowEncounter(encounter);
        }

        private List<MapUIComponent.EncounterChoice> BuildRewardEncounterChoices(MapNode selectedNode)
        {
            var rng = new System.Random(BuildMapNodeSeed(selectedNode, 41389));
            var rewards = new List<MapEventRewardCandidate>();

            int goldAmount = rng.Next(25, 51);
            rewards.Add(new MapEventRewardCandidate
            {
                Label = $"{goldAmount}전 받기",
                Feedback = $"장막 너머의 손이 {goldAmount}전을 남기고 사라졌습니다.",
                SoundId = SoundIds.SfxGoldGain,
                Apply = () => _masterPlayerData?.AddGold(goldAmount)
            });

            MapEventRewardCandidate cardReward = CreateRandomCardEventReward(rng);
            if (cardReward != null)
                rewards.Add(cardReward);

            MapEventRewardCandidate jokerReward = CreateRandomJokerEventReward(rng);
            if (jokerReward != null)
                rewards.Add(jokerReward);

            MapEventRewardCandidate accessoryReward = CreateRandomAccessoryEventReward(rng);
            if (accessoryReward != null)
                rewards.Add(accessoryReward);

            ShuffleList(rewards, rng);
            int choiceCount = Mathf.Min(rewards.Count, rng.Next(1, 4));
            var choices = new List<MapUIComponent.EncounterChoice>();
            for (int i = 0; i < choiceCount; i++)
            {
                MapEventRewardCandidate reward = rewards[i];
                choices.Add(new MapUIComponent.EncounterChoice(reward.Label, () => ClaimMapEventReward(reward)));
            }

            return choices;
        }

        private MapEventRewardCandidate CreateRandomCardEventReward(System.Random rng)
        {
            List<HwaTuCard> cards = HwaTuCardDatabase.CreateAllCards();
            if (cards == null || cards.Count == 0)
                return null;

            HwaTuCard card = cards[rng.Next(0, cards.Count)];
            return new MapEventRewardCandidate
            {
                Label = $"{card.DisplayName} 받기",
                Feedback = $"{card.DisplayName} 카드가 덱에 추가되었습니다.",
                SoundId = SoundIds.SfxRewardClaim,
                Apply = () => _masterPlayerData?.AddDeckCard(card.CardId)
            };
        }

        private MapEventRewardCandidate CreateRandomJokerEventReward(System.Random rng)
        {
            if (_masterPlayerData != null && _masterPlayerData.HeldJokerIds != null &&
                _masterPlayerData.HeldJokerIds.Count >= PlayerDataSO.MaxHeldJokerCount)
            {
                return null;
            }

            RewardCatalogItem? item = PickUnownedCatalogReward(EventJokerRewardPool, _masterPlayerData?.HeldJokerIds, rng);
            if (!item.HasValue)
                return null;

            RewardCatalogItem reward = item.Value;
            return new MapEventRewardCandidate
            {
                Label = $"{reward.DisplayName} 받기",
                Feedback = $"{reward.DisplayName}가 조커 보유 목록에 추가되었습니다.",
                SoundId = SoundIds.SfxRewardClaim,
                Apply = () => _masterPlayerData?.AddJoker(reward.Id)
            };
        }

        private MapEventRewardCandidate CreateRandomAccessoryEventReward(System.Random rng)
        {
            RewardCatalogItem? item = PickUnownedCatalogReward(EventAccessoryRewardPool, _masterPlayerData?.EquippedAccessoryIds, rng);
            if (!item.HasValue)
                return null;

            RewardCatalogItem reward = item.Value;
            ItemBase rewardItem = ItemFactory.CreateItem(reward.Id);
            string displayName = rewardItem != null ? rewardItem.DisplayName : reward.DisplayName;
            return new MapEventRewardCandidate
            {
                Label = $"{displayName} 받기",
                Feedback = $"{displayName} 장신구를 얻었습니다.",
                SoundId = SoundIds.SfxItemEquip,
                Apply = () =>
                {
                    if (_masterPlayerData == null)
                        return;

                    _masterPlayerData.EquippedAccessoryIds ??= new List<string>();
                    if (_masterPlayerData.EquippedAccessoryIds.Contains(reward.Id))
                    {
                        Debug.LogWarning($"[GameManager] 이미 보유 중인 장신구 보상을 무시합니다: {reward.Id}");
                        return;
                    }

                    _masterPlayerData.AddAccessory(reward.Id);
                }
            };
        }

        private RewardCatalogItem? PickUnownedCatalogReward(
            IReadOnlyList<RewardCatalogItem> pool,
            IReadOnlyCollection<string> ownedIds,
            System.Random rng)
        {
            if (pool == null || pool.Count == 0)
                return null;

            var owned = ownedIds != null ? new HashSet<string>(ownedIds) : new HashSet<string>();
            var candidates = new List<RewardCatalogItem>();
            var usedIds = new HashSet<string>();
            for (int i = 0; i < pool.Count; i++)
            {
                RewardCatalogItem item = pool[i];
                if (string.IsNullOrEmpty(item.Id) || !usedIds.Add(item.Id) || owned.Contains(item.Id))
                    continue;

                candidates.Add(item);
            }

            if (candidates.Count == 0)
                return null;

            return candidates[rng.Next(0, candidates.Count)];
        }

        private void ClaimMapEventReward(MapEventRewardCandidate reward)
        {
            if (reward == null)
                return;

            reward.Apply?.Invoke();
            RefreshActiveRunHud();
            if (!string.IsNullOrEmpty(reward.SoundId))
                SoundManager.PlaySfxSound(reward.SoundId);

            var result = new MapUIComponent.EncounterViewModel
            {
                Title = "손 안의 보상",
                Story = reward.Feedback
            };
            result.Choices.Add(new MapUIComponent.EncounterChoice("떠난다", CompleteMapEncounter));
            _activeMapView.ShowEncounter(result);
        }

        private void CompleteMapEncounter()
        {
            SaveRunMapProgress();
            _activeMapView?.HideEncounter();
        }

        private void EnterShopFromMap()
        {
            SoundManager.PlaySfxSound(SoundIds.SfxMapEnterShop);
            SceneLoader.LoadScene(SceneLoader.SceneNames.SHOP);
        }

        private int enemyBonusCount = 0; // 난이도 조절을 위해 임시로 추가한 변수. 실제로는 더 고도화된 난이도 조절 객체가 필요
        private void EnterRestFromMap()
        {
            SoundManager.PlayUiSound(SoundIds.UiConfirm);
            SceneLoader.LoadScene(SceneLoader.SceneNames.REST);
        }

        private void EnterTreasureFromMap()
        {
            SoundManager.PlaySfxSound(SoundIds.SfxMapNodeSelect);
            SceneLoader.LoadScene(SceneLoader.SceneNames.TREASURE);
        }

        private void EnterBattleFromMap(RoomType roomType)
        {
            // 시드값을 받아올 수 없는 경우 임의값 1을 전달
            int currentSeed = _currentMapData != null ? _currentMapData.Seed : 1;

            // TableSystemManager API를 호출하여 RoomType에 맞는 몬스터 ID 추출
            string targetEnemyId = TableSystem.PopEnemyId(roomType);
            TargetEnemyId = targetEnemyId;
            TargetRoomType = roomType;
            TargetEnemyVisualId = null;

            // TODO: 난이도 조절 시스템 매니저 객체 자체를 생성
            // 층이 올라갈 때마다 기본체력에서 10의 체력을 추가로 부여.
            int bonusHealth = 10;
            int bonusStr = 2;

            // 전투 전달용 데이터 구조체 패키징
            CurrentBattleEntryData = new BattleEntryData
            {
                TargetEnemyId = targetEnemyId,
                RoomType = roomType,
                EnemyBonusHealth = bonusHealth * enemyBonusCount,
                EnemyBonusStrength = bonusStr * enemyBonusCount,
                PlayerMasterData = _masterPlayerData
            };
            enemyBonusCount++;

            SoundManager.PlaySfxSound(roomType == RoomType.Boss ? SoundIds.SfxMapEnterBoss : SoundIds.SfxMapEnterBattle);
            SceneLoader.LoadScene(SceneLoader.SceneNames.BATTLE);
        }

        public string SelectEnemyVisualId(IReadOnlyList<string> candidateVisualIds)
        {
            if (!string.IsNullOrEmpty(TargetEnemyVisualId))
                return TargetEnemyVisualId;

            TargetEnemyVisualId = ResolveEnemyVisualId(TargetRoomType, candidateVisualIds);
            return TargetEnemyVisualId;
        }

        private string ResolveEnemyVisualId(RoomType roomType, IReadOnlyList<string> candidateVisualIds)
        {
            if (roomType == RoomType.Elite)
                return HasVisualId(candidateVisualIds, "Elite") ? "Elite" : null;

            if (roomType == RoomType.Boss)
                return HasVisualId(candidateVisualIds, "Boss") ? "Boss" : null;

            if (roomType != RoomType.Monster)
                return null;

            List<string> candidates = BuildVisualCandidateList(candidateVisualIds);
            if (candidates.Count == 0)
                return null;

            List<string> seenVisualIds = GetSeenNormalEnemyVisualIds();
            string selectedId = null;
            foreach (string candidate in candidates)
            {
                if (!seenVisualIds.Contains(candidate))
                {
                    selectedId = candidate;
                    break;
                }
            }

            if (string.IsNullOrEmpty(selectedId))
            {
                seenVisualIds.Clear();
                selectedId = candidates[0];
            }

            seenVisualIds.Add(selectedId);
            Debug.Log($"[GameManager] 일반 몬스터 외형 선택: {selectedId}");
            return selectedId;
        }

        private static List<string> BuildVisualCandidateList(IReadOnlyList<string> candidateVisualIds)
        {
            var candidates = new List<string>();
            var usedIds = new HashSet<string>();

            if (candidateVisualIds == null)
                return candidates;

            for (int i = 0; i < candidateVisualIds.Count; i++)
            {
                string id = candidateVisualIds[i];
                if (string.IsNullOrWhiteSpace(id) || !usedIds.Add(id))
                    continue;

                if (IsSpecialEnemyVisualId(id))
                    continue;

                candidates.Add(id);
            }

            return candidates;
        }

        private static bool HasVisualId(IReadOnlyList<string> candidateVisualIds, string targetId)
        {
            if (candidateVisualIds == null || string.IsNullOrWhiteSpace(targetId))
                return false;

            for (int i = 0; i < candidateVisualIds.Count; i++)
            {
                if (string.Equals(candidateVisualIds[i], targetId, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsSpecialEnemyVisualId(string id)
        {
            return string.Equals(id, "Elite", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "Boss", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "TheCat", System.StringComparison.OrdinalIgnoreCase);
        }

        private List<string> GetSeenNormalEnemyVisualIds()
        {
            if (_masterPlayerData != null)
            {
                _masterPlayerData.SeenNormalEnemyVisualIds ??= new List<string>();
                return _masterPlayerData.SeenNormalEnemyVisualIds;
            }

            return _runtimeSeenNormalEnemyVisualIds;
        }

        private int BuildMapNodeSeed(MapNode node, int salt)
        {
            unchecked
            {
                int seed = _currentMapData != null ? _currentMapData.Seed : 1;
                seed = seed * 397 ^ salt;
                seed = seed * 397 ^ (node != null ? node.Floor : 0);
                seed = seed * 397 ^ (node != null ? node.Column : 0);
                return seed & int.MaxValue;
            }
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

            // 복원된 맵 시드값 기반 TableSystemManager 초기화 트리거
            TableSystem.Initialize(_masterPlayerData.SavedMapSeed);

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
            RefreshActiveRunHud();
        }

        private void HandleShopAddAccessory(string accessoryId)
        {
            _masterPlayerData?.AddAccessory(accessoryId);
            RefreshActiveRunHud();
        }

        private void HandleShopRemoveDeckCard(string cardId)
        {
            if (_masterPlayerData != null && !_masterPlayerData.RemoveDeckCard(cardId))
                Debug.LogWarning($"[GameManager] 제거할 카드가 덱에 없습니다. CardId={cardId}");
            RefreshActiveRunHud();
        }

        private TreasureUIComponent.TreasureRewardModel CreateTreasureReward()
        {
            ItemDataSO itemData = PickTreasureAccessoryData();
            if (itemData == null)
                return null;

            return new TreasureUIComponent.TreasureRewardModel
            {
                Id = itemData.Id,
                DisplayName = string.IsNullOrWhiteSpace(itemData.DisplayName) ? itemData.Id : itemData.DisplayName,
                Description = itemData.Description,
                Icon = itemData.Icon
            };
        }

        private ItemDataSO PickTreasureAccessoryData()
        {
            List<ItemDataSO> candidates = GetUnownedTreasureAccessoryData();
            if (candidates.Count == 0)
                return null;

            if (TableSystem != null)
            {
                int attempts = Mathf.Max(candidates.Count, EventAccessoryRewardPool.Length);
                for (int i = 0; i < attempts; i++)
                {
                    List<string> itemIds = TableSystem.PopItemIds(ItemType.Accessory, 1);
                    if (itemIds == null || itemIds.Count == 0)
                        break;

                    ItemDataSO tableCandidate = FindItemData(candidates, itemIds[0]);
                    if (tableCandidate != null)
                        return tableCandidate;
                }
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private List<ItemDataSO> GetUnownedTreasureAccessoryData()
        {
            var result = new List<ItemDataSO>();
            var ownedIds = _masterPlayerData != null && _masterPlayerData.EquippedAccessoryIds != null
                ? new HashSet<string>(_masterPlayerData.EquippedAccessoryIds, System.StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            ItemDataSO[] itemData = Resources.LoadAll<ItemDataSO>("SO/Item");
            for (int i = 0; i < EventAccessoryRewardPool.Length; i++)
            {
                string accessoryId = EventAccessoryRewardPool[i].Id;
                if (ownedIds.Contains(accessoryId))
                    continue;

                ItemDataSO data = FindItemData(itemData, accessoryId);
                if (data != null)
                    result.Add(data);
            }

            return result;
        }

        private static ItemDataSO FindItemData(IReadOnlyList<ItemDataSO> itemData, string itemId)
        {
            if (itemData == null || string.IsNullOrWhiteSpace(itemId))
                return null;

            for (int i = 0; i < itemData.Count; i++)
            {
                ItemDataSO data = itemData[i];
                if (data != null && string.Equals(data.Id, itemId, System.StringComparison.OrdinalIgnoreCase))
                    return data;
            }

            return null;
        }

        private void HandleTreasureRewardClaimed(string accessoryId)
        {
            if (_masterPlayerData == null)
            {
                Debug.LogWarning("[GameManager] 보상을 저장할 PlayerDataSO가 없어 Map으로 돌아갑니다.");
                SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
                return;
            }

            if (string.IsNullOrWhiteSpace(accessoryId))
            {
                Debug.LogWarning("[GameManager] 비어 있는 보물 보상을 무시합니다.");
                SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
                return;
            }

            _masterPlayerData.EquippedAccessoryIds ??= new List<string>();
            if (!HasAccessory(accessoryId))
            {
                _masterPlayerData.AddAccessory(accessoryId);
                SoundManager.PlaySfxSound(SoundIds.SfxItemEquip);
                Debug.Log($"[GameManager] 보물 장신구 획득: {accessoryId}");
            }
            else
            {
                Debug.LogWarning($"[GameManager] 이미 보유 중인 보물 장신구를 무시합니다: {accessoryId}");
            }

            RefreshActiveRunHud();
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
        }

        private bool HasAccessory(string accessoryId)
        {
            if (_masterPlayerData == null || _masterPlayerData.EquippedAccessoryIds == null)
                return false;

            for (int i = 0; i < _masterPlayerData.EquippedAccessoryIds.Count; i++)
            {
                if (string.Equals(_masterPlayerData.EquippedAccessoryIds[i], accessoryId, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void HandleTreasureLeave()
        {
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
        }

        private int GetPlayerGold()
        {
            return _masterPlayerData != null ? _masterPlayerData.CurrentGold : 0;
        }

        private bool HandleSpendGold(int amount)
        {
            if (_masterPlayerData == null || !_masterPlayerData.SpendGold(amount))
                return false;

            RefreshActiveRunHud();
            return true;
        }

        private void HandleRestRequested()
        {
            if (_masterPlayerData == null)
            {
                Debug.LogWarning("[GameManager] 휴식할 PlayerDataSO가 없어 Map으로 돌아갑니다.");
                SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
                return;
            }

            int beforeHealth = _masterPlayerData.CurrentHealth;
            int healAmount = Mathf.Max(1, Mathf.CeilToInt(_masterPlayerData.MaxHealth * 0.15f));
            _masterPlayerData.CurrentHealth = Mathf.Clamp(
                _masterPlayerData.CurrentHealth + healAmount,
                0,
                _masterPlayerData.MaxHealth);

            RefreshActiveRunHud();
            Debug.Log($"[GameManager] 휴식 완료: {beforeHealth} -> {_masterPlayerData.CurrentHealth} (+{healAmount})");
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
        }

        private void HydrateRunHud(BaseUIComponent view)
        {
            if (view == null || _masterPlayerData == null)
                return;

            TopRunHudComponent hud = view.GetComponent<TopRunHudComponent>();
            if (hud == null)
                hud = view.gameObject.AddComponent<TopRunHudComponent>();

            _activeRunHud = hud;
            RefreshActiveRunHud();
        }

        private void RefreshActiveRunHud()
        {
            if (_activeRunHud == null || _masterPlayerData == null)
                return;

            _activeRunHud.SetPlayerData(_masterPlayerData);
        }

        private void HydrateBattleHud(BattleUIComponent view)
        {
            if (view == null || _masterPlayerData == null)
                return;

            GetCurrentOrRestoreRunMap();

            // BattleStartManager에서 구성하도록 이곳의 기능을 잠시 차단
            // view.SetPlayerHealth(_masterPlayerData.CurrentHealth, _masterPlayerData.MaxHealth);
            // view.SetPlayerGold(_masterPlayerData.CurrentGold);
            // view.SetDeckCards(_masterPlayerData.DeckCardIds);
            // view.SetupItemIcons(_masterPlayerData.EquippedAccessories, _masterPlayerData.HeldJokerIds);
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

        public void HandleGameCompleted(PlayerDataBattle finalBattleData)
        {
            Debug.Log("[GameManager] Final boss defeated. Syncing battle data and loading EndingScene.");

            PlayerDataUpdater updater = new PlayerDataUpdater();
            updater.SyncBattleDataToMaster(finalBattleData, _masterPlayerData);

            SceneLoader.LoadSceneImmediate(SceneLoader.SceneNames.ENDING);
        }


        /// <summary>
        /// Fisher-Yates 알고리즘을 이용해 리스트 요소를 무작위로 섞습니다.
        /// </summary>
        private void ShuffleList<T>(List<T> list, System.Random rng)
        {
            if (list == null || rng == null)
                return;

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
