using System.Collections.Generic;
using UnityEngine;

namespace FFF.Data
{
    /// <summary>
    /// 게임 전체에서 유지되는 플레이어의 원본 데이터 (싱글톤)
    /// 시연용 더미 데이터를 기본으로 가집니다.
    /// </summary>
    public class PlayerData : MonoBehaviour
    {
        public static PlayerData Instance { get; private set; }

        [Header("=== 시연용 더미 데이터 ===")]
        public int MaxHealth = 100;
        public int CurrentHealth = 100;
        
        // 추후 이 ID를 바탕으로 팩토리에서 실제 객체를 찍어냅니다.
        public List<string> EquippedAccessoryIds = new List<string> { "ACC_REROLL_BONUS" };
        public List<string> HeldJokerIds = new List<string> { "JKR_REROLL_BURST" };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // PlayerData 클래스 내부에 추가
        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            CurrentHealth -= damage;
            if (CurrentHealth < 0) CurrentHealth = 0;
            
            UnityEngine.Debug.Log($"[PlayerData] 플레이어가 {damage}의 피해를 입었습니다! 남은 체력: {CurrentHealth}");
        }
    }
}