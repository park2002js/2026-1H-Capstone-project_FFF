using UnityEngine;
using System.Collections.Generic;
using FFF.Core;
using FFF.UI.Battle;
using FFF.Core.Events;
using FFF.Battle.Data;
using FFF.Audio;
using FFF.Data;

namespace FFF.Battle.FSM
{
    public class BattleEndManager : MonoBehaviour
    {
        private static readonly string[] AccessoryRewardPoolIds =
        {
            "Accessory_001",
            "Accessory_002",
            "Accessory_003",
            "Accessory_004",
            "Accessory_005"
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
                SoundManager.PlaySfxSound(SoundIds.SfxBattleVictory);
                _rewardClaimed = false;
                _battleUI.ShowRewardSelection(
                    CreateRewardCategoryOptions(),
                    ShowRewardCandidates,
                    OnReturnToMapButtonClicked,
                    "보상 종류 하나를 선택하세요.",
                    isFinalRewardSelection: false,
                    hideRewardDetailsUntilSelection: false);
                return;
            }

            SoundManager.PlaySfxSound(SoundIds.SfxBattleDefeat);
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

            PlayerDataBattle player = GetCurrentPlayerData();
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
            if (HasAvailableAccessoryReward(player))
            {
                rewards.Add(new BattleUIComponent.RewardOption
                {
                    Id = "RewardCategory_Accessory",
                    Kind = BattleUIComponent.RewardKind.Accessory,
                    DisplayName = "장신구 보상",
                    Category = "장신구",
                    Description = "랜덤 장신구\n3개 중 1개 선택"
                });
            }

            Shuffle(rewards);
            return rewards;
        }

        private void ShowRewardCandidates(BattleUIComponent.RewardOption categoryReward)
        {
            if (categoryReward == null)
                return;

            List<BattleUIComponent.RewardOption> candidates = CreateRewardCandidates(categoryReward.Kind);
            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[BattleEnd] 선택 가능한 {categoryReward.Category} 후보가 없어 보상 종류 선택으로 되돌립니다.");
                _battleUI.ShowRewardSelection(
                    CreateRewardCategoryOptions(),
                    ShowRewardCandidates,
                    OnReturnToMapButtonClicked,
                    "선택 가능한 후보가 없습니다. 다른 보상 종류를 선택하세요.",
                    isFinalRewardSelection: false,
                    hideRewardDetailsUntilSelection: false);
                return;
            }

            _battleUI.ShowRewardSelection(
                candidates,
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
            var rewards = new List<BattleUIComponent.RewardOption>();
            // 1. TableSystemManager로부터 셔플된 조커 ID 추출
            List<string> itemIds = GameManager.Instance.TableSystem.PopItemIds(ItemType.Joker, count);
            
            foreach (string id in itemIds)
            {
                // 2. 객체 생성
                ItemBase item = ItemFactory.CreateItem(id);
                if (item == null) continue;
                
                // 3. 객체 데이터(이미지, 텍스트)를 RewardOption에 맵핑
                rewards.Add(new BattleUIComponent.RewardOption
                {
                    Id = $"Reward_Joker_{item.Id}",
                    Kind = BattleUIComponent.RewardKind.Joker,
                    PayloadId = item.Id,
                    DisplayName = item.DisplayName,
                    Category = "조커 카드",
                    Description = item.Description,
                    Artwork = item.Icon
                });
            }
            return rewards;
        }

        private List<BattleUIComponent.RewardOption> CreateRandomAccessoryRewards(int count)
        {
            Debug.Log("[BattleEnd] 장신구 보상 UI 렌더링 시작");
            PlayerDataBattle player = GetCurrentPlayerData();
            var rewards = new List<BattleUIComponent.RewardOption>();
            var usedRewardIds = new HashSet<string>();

            List<string> itemIds = PopRewardItemIds(ItemType.Accessory, count);
            foreach (string id in itemIds)
            {
                TryAddAccessoryReward(rewards, usedRewardIds, player, id);

                if (rewards.Count >= count)
                    break;
            }

            if (rewards.Count < count)
            {
                List<string> fallbackIds = GetAvailableAccessoryRewardIds(player);
                Shuffle(fallbackIds);
                foreach (string id in fallbackIds)
                {
                    TryAddAccessoryReward(rewards, usedRewardIds, player, id);

                    if (rewards.Count >= count)
                        break;
                }
            }

            if (rewards.Count == 0)
                Debug.LogWarning("[BattleEnd] 생성 가능한 장신구 보상 후보가 없습니다.");

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
                    if (player.EquippedAccessoryIds != null && player.EquippedAccessoryIds.Contains(reward.PayloadId))
                    {
                        Debug.LogWarning($"[BattleEnd] 이미 보유 중인 장신구 보상은 받을 수 없습니다: {reward.PayloadId}");
                        return;
                    }

                    player.AddAccessory(reward.PayloadId);
                    break;
            }

            _rewardClaimed = true;
            _battleUI.SetDeckCards(player.DeckCardIds);

            // 수정됨: UI 렌더링 요구사항에 맞추기 위해 임시 ItemBase 객체 리스트 생성
            List<ItemBase> tempAccessories = new List<ItemBase>();
            foreach (var id in player.EquippedAccessoryIds) tempAccessories.Add(ItemFactory.CreateItem(id));
            
            List<ItemBase> tempJokers = new List<ItemBase>();
            foreach (var id in player.HeldJokerIds) tempJokers.Add(ItemFactory.CreateItem(id));

            _battleUI.SetupItemIcons(tempAccessories, tempJokers);
            Debug.Log($"[BattleEnd] 보상 획득: {reward.Category} / {reward.DisplayName} ({reward.PayloadId})");
        }

        private PlayerDataBattle GetCurrentPlayerData()
        {
            return _battleManager != null && _battleManager.Context != null
                ? _battleManager.Context.PlayerData
                : null;
        }

        private bool HasAvailableAccessoryReward(PlayerDataBattle player)
        {
            return GetAvailableAccessoryRewardIds(player).Count > 0;
        }

        private List<string> GetAvailableAccessoryRewardIds(PlayerDataBattle player)
        {
            var availableIds = new List<string>();
            foreach (string id in AccessoryRewardPoolIds)
            {
                if (IsOwnedAccessory(player, id))
                    continue;

                availableIds.Add(id);
            }

            return availableIds;
        }

        private bool TryAddAccessoryReward(
            List<BattleUIComponent.RewardOption> rewards,
            HashSet<string> usedRewardIds,
            PlayerDataBattle player,
            string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || IsOwnedAccessory(player, itemId) || !usedRewardIds.Add(itemId))
                return false;

            ItemBase item = ItemFactory.CreateItem(itemId);
            if (item == null)
                return false;

            rewards.Add(new BattleUIComponent.RewardOption
            {
                Id = $"Reward_Accessory_{item.Id}",
                Kind = BattleUIComponent.RewardKind.Accessory,
                PayloadId = item.Id,
                DisplayName = item.DisplayName,
                Category = "장신구",
                Description = item.Description,
                Artwork = item.Icon
            });

            return true;
        }

        private bool IsOwnedAccessory(PlayerDataBattle player, string itemId)
        {
            return player != null &&
                   player.EquippedAccessoryIds != null &&
                   player.EquippedAccessoryIds.Contains(itemId);
        }

        private List<string> PopRewardItemIds(ItemType itemType, int count)
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.TableSystem == null)
                return new List<string>();

            return gameManager.TableSystem.PopItemIds(itemType, count);
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

            Debug.Log("[BattleEnd] 재시작 대신 타이틀로 돌아갑니다.");
            FFF.Core.SceneLoader.LoadScene(FFF.Core.SceneLoader.SceneNames.TITLE);
        }

        public void OnTitleButtonClicked()
        {
            SoundManager.PlayDefaultUiClick();

            Debug.Log("[BattleEnd] 타이틀로 돌아갑니다.");
            // 씬 이름은 실제 프로젝트의 Title 씬 이름("TitleScene" 등)으로 맞춰주세요.
            FFF.Core.SceneLoader.LoadScene(FFF.Core.SceneLoader.SceneNames.TITLE);
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
            FFF.Core.SceneLoader.LoadScene(FFF.Core.SceneLoader.SceneNames.MAP);
        }

        #endregion
    }
}
