using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환용
using System.Collections.Generic;
using FFF.Battle.FSM;
using FFF.UI.Battle;
using FFF.Core.Events;
using FFF.Battle.Data;
using FFF.Audio;
using FFF.Data;

namespace FFF.Battle.FSM
{
    public class BattleEndManager : MonoBehaviour
    {
        private readonly struct RewardCatalogItem
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string Description;

            public RewardCatalogItem(string id, string displayName, string description)
            {
                Id = id;
                DisplayName = displayName;
                Description = description;
            }
        }

        private static readonly RewardCatalogItem[] JokerRewardPool =
        {
            new RewardCatalogItem("JKR_REROLL_BURST", "리롤 폭죽 조커", "전투 중 사용할 수 있는\n조커 카드"),
            new RewardCatalogItem("JKR_HIGH_CARD", "광패 조커", "높은 족보를 노리는\n조커 카드"),
            new RewardCatalogItem("JKR_DOUBLE_PIP", "쌍피 조커", "피 카드에 힘을 싣는\n조커 카드"),
            new RewardCatalogItem("JKR_LUCKY_CHARM", "행운 부적 조커", "위기를 넘기는\n조커 카드")
        };

        private static readonly RewardCatalogItem[] AccessoryRewardPool =
        {
            new RewardCatalogItem("ACC_REROLL_BONUS", "노리개", "전투 시작 시\n리롤 +1"),
            new RewardCatalogItem("ACC_DAMAGE_BONUS", "은장도", "승리 피해량\n+5"),
            new RewardCatalogItem("ACC_JADE_RING", "옥가락지", "족보 공격력\n소폭 증가"),
            new RewardCatalogItem("ACC_GAT", "갓", "첫 턴 방어적\n보정 획득")
        };

        [Header("=== 시스템 참조 ===")]
        [SerializeField] private BattleManager _battleManager;
        [SerializeField] private BattleUIComponent _battleUI;

        [Header("=== 수신할 이벤트 ===")]
        [SerializeField] private GameEvent _onBattleEndEvent;

        private bool _rewardClaimed;

        private void OnEnable()
        {
            if (_onBattleEndEvent != null) _onBattleEndEvent.Subscribe(HandleBattleEndEnter);
        }

        private void OnDisable()
        {
            if (_onBattleEndEvent != null) _onBattleEndEvent.Unsubscribe(HandleBattleEndEnter);
        }

        private void HandleBattleEndEnter()
        {
            Debug.Log("========== [BattleEnd] 전투 종료 및 결과 표시 ==========");

            // 1. 기존 전투 UI 싹 지우기 (옵션)
            _battleUI.ClearHandUI();
            _battleUI.SetTurnProceedUIVisibility(false);
            _battleUI.SetTurnReadyUIVisibility(false);

            // 2. 바구니(Context) 열어보기 (핵심 로직)
            bool isPlayerWin = _battleManager.Context.IsPlayerWinner;

            if (isPlayerWin)
            {
                _rewardClaimed = false;
                _battleUI.ShowRewardSelection(
                    CreateRewardCategoryOptions(),
                    ShowRewardCandidates,
                    OnReturnToMapButtonClicked,
                    "물음표 보따리 하나를 선택하세요.",
                    isFinalRewardSelection: false,
                    hideRewardDetailsUntilSelection: true);
                return;
            }

            _battleUI.ShowBattleResult("Game Over\n<size=50>플레이어 패배</size>");
        }

        private List<BattleUIComponent.RewardOption> CreateRewardCategoryOptions()
        {
            var rewards = new List<BattleUIComponent.RewardOption>
            {
                new BattleUIComponent.RewardOption
                {
                    Id = "RewardCategory_Card",
                    Kind = BattleUIComponent.RewardKind.HwaTuCard,
                    DisplayName = "화투 카드 보상",
                    Category = "화투 카드",
                    Description = "랜덤 화투 카드\n3장 중 1장 선택"
                }
            };

            PlayerDataBattle player = _battleManager != null && _battleManager.Context != null
                ? _battleManager.Context.PlayerData
                : null;
            if (player == null || player.HeldJokerIds.Count < PlayerDataSO.MaxHeldJokerCount)
            {
                rewards.Add(new BattleUIComponent.RewardOption
                {
                    Id = "RewardCategory_Joker",
                    Kind = BattleUIComponent.RewardKind.Joker,
                    DisplayName = "조커 카드 보상",
                    Category = "조커 카드",
                    Description = "랜덤 조커 카드\n3장 중 1장 선택"
                });
            }

            rewards.Add(new BattleUIComponent.RewardOption
            {
                Id = "RewardCategory_Accessory",
                Kind = BattleUIComponent.RewardKind.Accessory,
                DisplayName = "장신구 보상",
                Category = "장신구",
                Description = "랜덤 장신구\n3개 중 1개 선택"
            });

            Shuffle(rewards);
            return rewards;
        }

        private void ShowRewardCandidates(BattleUIComponent.RewardOption categoryReward)
        {
            if (categoryReward == null)
                return;

            _battleUI.ShowRewardSelection(
                CreateRewardCandidates(categoryReward.Kind),
                ClaimReward,
                OnReturnToMapButtonClicked,
                $"{categoryReward.Category} 후보 3개 중 하나를 선택하세요.",
                isFinalRewardSelection: true,
                hideRewardDetailsUntilSelection: false);
        }

        private List<BattleUIComponent.RewardOption> CreateRewardCandidates(BattleUIComponent.RewardKind kind)
        {
            return kind switch
            {
                BattleUIComponent.RewardKind.HwaTuCard => CreateRandomCardRewards(3),
                BattleUIComponent.RewardKind.Joker => CreateRandomJokerRewards(3),
                BattleUIComponent.RewardKind.Accessory => CreateRandomAccessoryRewards(3),
                _ => new List<BattleUIComponent.RewardOption>()
            };
        }

        private List<BattleUIComponent.RewardOption> CreateRandomCardRewards(int count)
        {
            List<HwaTuCard> cards = HwaTuCardDatabase.CreateAllCards();
            Shuffle(cards);

            var rewards = new List<BattleUIComponent.RewardOption>();
            var usedCardIds = new HashSet<string>();
            foreach (HwaTuCard card in cards)
            {
                if (card == null || string.IsNullOrEmpty(card.CardId) || !usedCardIds.Add(card.CardId))
                    continue;

                rewards.Add(new BattleUIComponent.RewardOption
                {
                    Id = $"Reward_Card_{card.CardId}",
                    Kind = BattleUIComponent.RewardKind.HwaTuCard,
                    PayloadId = card.CardId,
                    DisplayName = card.DisplayName,
                    Category = "화투 카드",
                    Description = "덱에 추가됩니다.",
                    Artwork = HwaTuCardDatabase.GetArtwork(card.CardId)
                });

                if (rewards.Count >= count)
                    break;
            }

            if (rewards.Count == 0)
            {
                rewards.Add(new BattleUIComponent.RewardOption
                {
                    Id = "Reward_Card_M1_Pi",
                    Kind = BattleUIComponent.RewardKind.HwaTuCard,
                    PayloadId = "M1_Pi",
                    DisplayName = "화투 카드",
                    Category = "화투 카드",
                    Description = "덱에 추가됩니다.",
                    Artwork = HwaTuCardDatabase.GetArtwork("M1_Pi")
                });
            }

            return rewards;
        }

        private List<BattleUIComponent.RewardOption> CreateRandomJokerRewards(int count)
        {
            return CreateCatalogRewards(
                JokerRewardPool,
                count,
                BattleUIComponent.RewardKind.Joker,
                "조커 카드",
                item => $"Reward_Joker_{item.Id}");
        }

        private List<BattleUIComponent.RewardOption> CreateRandomAccessoryRewards(int count)
        {
            return CreateCatalogRewards(
                AccessoryRewardPool,
                count,
                BattleUIComponent.RewardKind.Accessory,
                "장신구",
                item => $"Reward_Accessory_{item.Id}");
        }

        private List<BattleUIComponent.RewardOption> CreateCatalogRewards(
            IReadOnlyList<RewardCatalogItem> pool,
            int count,
            BattleUIComponent.RewardKind kind,
            string category,
            System.Func<RewardCatalogItem, string> idSelector)
        {
            var candidates = new List<RewardCatalogItem>(pool ?? new List<RewardCatalogItem>());
            Shuffle(candidates);

            var rewards = new List<BattleUIComponent.RewardOption>();
            foreach (RewardCatalogItem item in candidates)
            {
                rewards.Add(new BattleUIComponent.RewardOption
                {
                    Id = idSelector != null ? idSelector(item) : $"Reward_{item.Id}",
                    Kind = kind,
                    PayloadId = item.Id,
                    DisplayName = item.DisplayName,
                    Category = category,
                    Description = item.Description
                });

                if (rewards.Count >= count)
                    break;
            }

            return rewards;
        }

        private void ClaimReward(BattleUIComponent.RewardOption reward)
        {
            if (_rewardClaimed || reward == null)
                return;

            PlayerDataBattle player = _battleManager.Context.PlayerData;
            if (player == null)
                return;

            switch (reward.Kind)
            {
                case BattleUIComponent.RewardKind.HwaTuCard:
                    player.AddDeckCard(reward.PayloadId);
                    break;
                case BattleUIComponent.RewardKind.Joker:
                    if (player.HeldJokerIds.Count >= PlayerDataSO.MaxHeldJokerCount)
                    {
                        Debug.LogWarning("[BattleEnd] 조커 보유 한도에 도달하여 조커 보상을 받을 수 없습니다.");
                        return;
                    }

                    player.AddJoker(reward.PayloadId);
                    break;
                case BattleUIComponent.RewardKind.Accessory:
                    player.AddAccessory(reward.PayloadId);
                    break;
            }

            _rewardClaimed = true;
            _battleUI.SetDeckCards(player.DeckCardIds);
            _battleUI.SetupItemIcons(player.EquippedAccessoryIds, player.HeldJokerIds);
            Debug.Log($"[BattleEnd] 보상 획득: {reward.Category} / {reward.DisplayName} ({reward.PayloadId})");
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        #region === 버튼 클릭 콜백 ===

        public void OnRestartButtonClicked()
        {
            SoundManager.PlayDefaultUiClick();

            Debug.Log("[BattleEnd] 전투를 다시 시작합니다.");
            // 현재 활성화된 씬(BattleScene)을 다시 로드하여 모든 것을 완전 초기화합니다.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnTitleButtonClicked()
        {
            SoundManager.PlayDefaultUiClick();

            Debug.Log("[BattleEnd] 타이틀로 돌아갑니다.");
            // 씬 이름은 실제 프로젝트의 Title 씬 이름("TitleScene" 등)으로 맞춰주세요.
            SceneManager.LoadScene("TitleScene"); 
        }

        /// <summary>
        /// "맵으로 돌아가기" 또는 "계속하기" 버튼에 연결할 콜백입니다.
        /// </summary>
        public void OnReturnToMapButtonClicked()
        {
            SoundManager.PlayDefaultUiClick();

            Debug.Log("[BattleEnd] 맵으로 귀환을 요청합니다.");

            // 현재 BattleContext에 저장되어 있는 최종 로컬 데이터를 꺼냅니다.
            PlayerDataBattle finalData = _battleManager.Context.PlayerData;

            // GameManager에게 맵으로 돌아가면서 이 데이터를 원본에 반영해달라고 요청합니다.
            FFF.Core.GameManager gameManager = FFF.Core.GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.HandleReturnToMap(finalData);
                return;
            }

            Debug.LogWarning("[BattleEnd] GameManager가 없어 StageScene으로 직접 이동합니다. 전투 보상은 MasterData에 저장되지 않습니다.");
            SceneManager.LoadScene(FFF.Core.SceneLoader.SceneNames.MAP);
        }

        #endregion
    }
}
