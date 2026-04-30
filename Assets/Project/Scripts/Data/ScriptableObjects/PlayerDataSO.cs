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
        [Header("=== 체력 정보 ===")]
        public int MaxHealth = 100;
        public int CurrentHealth = 100;
        
        [Header("=== 장착/보유 아이템 정보 ===")]
        // 추후 이 ID를 바탕으로 팩토리에서 실제 객체를 찍어냅니다.
        public List<string> EquippedAccessoryIds = new List<string> { "ACC_REROLL_BONUS" };
        public List<string> HeldJokerIds = new List<string> { "JKR_REROLL_BURST" };

        // 덱 리스트 등, 전투 외적으로 영구 저장되어야 하는 데이터는 이곳에 추가합니다.
    }
}