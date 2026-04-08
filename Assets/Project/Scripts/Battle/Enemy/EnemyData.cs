using UnityEngine;
using FFF.Data;
using System.Collections.Generic;

namespace FFF.Battle.Enemy
{
    public struct EnemyIntent
    {
        public HwaTuCard Card1;
        public HwaTuCard Card2;
        public int BasePower;
        public string HandName;
    }

    /// <summary>
    /// 전투 씬에 배치되어 현재 상대하는 적의 상태를 관리합니다.
    /// </summary>
    public class EnemyData : MonoBehaviour
    {
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public string EnemyName { get; private set; }

        // 이번 턴의 적 행동 의도
        public EnemyIntent CurrentIntent { get; private set; }

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

        // TurnReady 시작 시 호출되어 이번 턴의 가짜 의도를 생성합니다.
        public void GenerateMockIntent()
        {
            List<HwaTuCard> allCards = HwaTuCardDatabase.CreateAllCards();
            // 시연용이므로 대충 0번, 1번 카드를 뽑아서 족보를 만듭니다. (나중엔 EnemyBrain이 담당)
            HwaTuCard c1 = allCards[UnityEngine.Random.Range(0, allCards.Count)];
            HwaTuCard c2 = allCards[UnityEngine.Random.Range(0, allCards.Count)];
            
            SeotdaResult result = SeotdaJudge.Judge(c1, c2);

            CurrentIntent = new EnemyIntent
            {
                Card1 = c1,
                Card2 = c2,
                BasePower = result.BasePower,
                HandName = result.DisplayName
            };

            Debug.Log($"[EnemyData] 적 의도 결정됨: {c1.DisplayName} + {c2.DisplayName} = {result.DisplayName}\n(공격력: {result.BasePower})");
        }
        

        public void TakeDamage(int damage)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        }
    }
}