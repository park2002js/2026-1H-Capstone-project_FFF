using UnityEngine;

namespace FFF.Battle.Enemy
{
    /// <summary>
    /// 전투 씬에 배치되어 현재 상대하는 적의 상태를 관리합니다.
    /// </summary>
    public class EnemyData : MonoBehaviour
    {
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public string EnemyName { get; private set; }

        /// <summary>
        /// 전투 시작 시 BattleStartManager가 호출하여 초기화합니다.
        /// (시연을 위해 하드코딩된 데이터를 넣습니다)
        /// </summary>
        public void InitializeMockData()
        {
            EnemyName = "시연용 허수아비";
            MaxHealth = 150;
            CurrentHealth = 150;
            Debug.Log($"[EnemyData] 적 세팅 완료: {EnemyName} (HP: {CurrentHealth}/{MaxHealth})");
        }

        public void TakeDamage(int damage)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        }
    }
}