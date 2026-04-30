using System.Collections.Generic;
using UnityEngine;
using FFF.Data;

namespace FFF.Battle.Data
{
    /// <summary>
    /// Stage(Battle) 내에서만 사용되는 Local 플레이어 데이터 객체입니다.
    /// Master 데이터인 PlayerDataSO를 복사하여 생성되며, 전투 중 발생하는 상태 변화를 격리합니다.
    /// </summary>
    public class PlayerDataBattle
    {
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        
        public List<string> EquippedAccessoryIds { get; private set; }
        public List<string> HeldJokerIds { get; private set; }

        /// <summary>
        /// Master 데이터인 PlayerDataSO를 기반으로 전투용 로컬 데이터를 초기화합니다.
        /// </summary>
        public PlayerDataBattle(PlayerDataSO masterData)
        {
            if (masterData == null)
            {
                Debug.LogError("[PlayerDataBattle] Master Data가 null입니다! 기본값으로 초기화합니다.");
                MaxHealth = 10000;
                CurrentHealth = 10000;
                EquippedAccessoryIds = new List<string>();
                HeldJokerIds = new List<string>();
                return;
            }

            MaxHealth = masterData.MaxHealth;
            CurrentHealth = masterData.CurrentHealth;
            
            // List는 새 인스턴스로 복사 생성하여(Shallow Copy), 
            // 전투 중 조커를 사용해 리스트 항목을 제거하더라도 SO 원본이 손상되지 않도록 합니다.
            EquippedAccessoryIds = new List<string>(masterData.EquippedAccessoryIds);
            HeldJokerIds = new List<string>(masterData.HeldJokerIds);
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            CurrentHealth -= damage;
            if (CurrentHealth < 0) CurrentHealth = 0;
            
            Debug.Log($"[PlayerDataBattle] 플레이어가 {damage}의 피해를 입었습니다! 남은 체력: {CurrentHealth}");
        }

        public void ConsumeJoker(string jokerId)
        {
            if (HeldJokerIds.Contains(jokerId))
            {
                HeldJokerIds.Remove(jokerId);
                Debug.Log($"[PlayerDataBattle] 조커({jokerId})가 제거되었습니다. 남은 조커: {HeldJokerIds.Count}개");
            }
        }

        public void ConsumeAccessory(string accessoryId)
        {
            if (HeldJokerIds.Contains(accessoryId))
            {
                HeldJokerIds.Remove(accessoryId);
                Debug.Log($"[PlayerDataBattle] 악세서리({accessoryId})가 제거되었습니다. 남은 조커: {HeldJokerIds.Count}개");
            }
        }
    }
}