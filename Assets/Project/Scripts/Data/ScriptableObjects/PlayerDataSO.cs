using System.Collections.Generic;
using UnityEngine;

namespace FFF.Data
{
    /// <summary>
    /// 게임 전체에서 유지되는 플레이어의 Master 데이터.
    /// Stage에 진입할 때 이 데이터를 복사하여 PlayerDataBattle을 만들고,
    /// Stage가 끝날 때 PlayerDataUpdater를 통해 이 데이터에 변경 사항을 덮어씌웁니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlayerData", menuName = "FFF/Data/Player Data")]
    public class PlayerDataSO : ScriptableObject
    {
        private static readonly string[] DefaultInitialDeckIds =
        {
            "M1_Pi",
            "M2_Yeolkkeut",
            "M3_Pi",
            "M4_Yeolkkeut",
            "M5_Yeolkkeut",
            "M6_Yeolkkeut",
            "M7_Yeolkkeut",
            "M8_Pi",
            "M9_Yeolkkeut",
            "M10_Yeolkkeut"
        };

        [Header("=== 초기 플레이어 데이터 ===")]
        [SerializeField] private int _initialMaxHealth = 300;
        [SerializeField] private int _initialCurrentHealth = 300;
        [SerializeField] private List<string> _initialEquippedAccessoryIds = new List<string> { "ACC_REROLL_BONUS" };
        [SerializeField] private List<string> _initialHeldJokerIds = new List<string> { "JKR_REROLL_BURST" };
        [SerializeField] private List<string> _initialDeckCardIds = new List<string>(DefaultInitialDeckIds);

        [Header("=== 체력 정보 ===")]
        public int MaxHealth = 300;
        public int CurrentHealth = 300;

        [Header("=== 재화 정보 ===")]
        [SerializeField] private int _initialGold = 216;
        public int CurrentGold = 216;

        [Header("=== 장착/보유 아이템 정보 ===")]
        // 추후 이 ID를 바탕으로 팩토리에서 실제 객체를 찍어냅니다.
        public List<string> EquippedAccessoryIds = new List<string> { "ACC_REROLL_BONUS" };
        public List<string> HeldJokerIds = new List<string> { "JKR_REROLL_BURST" };

        public const int MaxHeldJokerCount = 3;

        [Header("=== 보유 덱 정보 ===")]
        public List<string> DeckCardIds = new List<string>
        {
            "M1_Pi",
            "M2_Yeolkkeut",
            "M3_Pi",
            "M4_Yeolkkeut",
            "M5_Yeolkkeut",
            "M6_Yeolkkeut",
            "M7_Yeolkkeut",
            "M8_Pi",
            "M9_Yeolkkeut",
            "M10_Yeolkkeut"
        };

        [Header("=== 스테이지 진행 정보 ===")]
        public bool HasSavedMapProgress = false;
        public int SavedMapSeed = -1;
        public List<int> SavedVisitedNodeIds = new List<int>();
        public List<int> SavedReachableNodeIds = new List<int>();

        public void AddAccessory(string accessoryId)
        {
            if (string.IsNullOrEmpty(accessoryId)) return;
            EquippedAccessoryIds.Add(accessoryId);
        }

        public void AddJoker(string jokerId)
        {
            if (string.IsNullOrEmpty(jokerId)) return;
            HeldJokerIds ??= new List<string>();
            if (HeldJokerIds.Count >= MaxHeldJokerCount)
            {
                Debug.LogWarning($"[PlayerDataSO] 조커는 최대 {MaxHeldJokerCount}장까지만 보유할 수 있습니다.");
                return;
            }

            HeldJokerIds.Add(jokerId);
        }

        public bool SpendGold(int amount)
        {
            if (amount <= 0) return true;
            if (CurrentGold < amount) return false;

            CurrentGold -= amount;
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            CurrentGold += amount;
        }

        public void AddDeckCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            DeckCardIds.Add(cardId);
        }

        public bool RemoveDeckCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || DeckCardIds == null) return false;
            return DeckCardIds.Remove(cardId);
        }

        public void ResetToInitialState()
        {
            MaxHealth = _initialMaxHealth > 0 ? _initialMaxHealth : 300;
            CurrentHealth = Mathf.Clamp(_initialCurrentHealth > 0 ? _initialCurrentHealth : MaxHealth, 0, MaxHealth);
            CurrentGold = Mathf.Max(0, _initialGold);
            EquippedAccessoryIds = CopyList(_initialEquippedAccessoryIds);
            HeldJokerIds = LimitJokerList(CopyList(_initialHeldJokerIds));
            DeckCardIds = _initialDeckCardIds != null && _initialDeckCardIds.Count > 0
                ? CopyList(_initialDeckCardIds)
                : new List<string>(DefaultInitialDeckIds);
            ClearMapProgress();
        }

        public void SaveMapProgress(int seed, IEnumerable<int> visitedNodeIds, IEnumerable<int> reachableNodeIds)
        {
            HasSavedMapProgress = true;
            SavedMapSeed = seed;
            SavedVisitedNodeIds = CopyIntList(visitedNodeIds);
            SavedReachableNodeIds = CopyIntList(reachableNodeIds);
        }

        public void ClearMapProgress()
        {
            HasSavedMapProgress = false;
            SavedMapSeed = -1;
            SavedVisitedNodeIds = new List<int>();
            SavedReachableNodeIds = new List<int>();
        }

        private static List<string> CopyList(List<string> source)
        {
            return source != null ? new List<string>(source) : new List<string>();
        }

        private static List<int> CopyIntList(IEnumerable<int> source)
        {
            return source != null ? new List<int>(source) : new List<int>();
        }

        private static List<string> LimitJokerList(List<string> source)
        {
            source ??= new List<string>();
            if (source.Count <= MaxHeldJokerCount)
                return source;

            return source.GetRange(0, MaxHeldJokerCount);
        }

        // 덱 리스트 등, 전투 외적으로 영구 저장되어야 하는 데이터는 이곳에 추가합니다.
    }
}
