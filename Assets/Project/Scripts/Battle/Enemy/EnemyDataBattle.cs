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
    /// 전투 씬에 배치되어 현재 상대하는 적의 상태(런타임 데이터)를 관리
    /// PlayerDataBattle처럼 마스터 데이터(SO)를 전달 받아서 초기화
    /// </summary>
    public class EnemyDataBattle : MonoBehaviour
    {
        public string EnemyId { get; private set; }
        public string EnemyName { get; private set; }
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        
        // 이번 턴의 적 행동 의도
        public EnemyIntent CurrentIntent { get; private set; }

        /// <summary>
        /// 전투 시작 시 BattleStartManager가 SO 데이터를 넘기며 호출합니다.
        /// 인자로 넘어온 SO 데이터를 바탕으로 현재 전투의 적 정보를 초기화합니다.
        /// </summary>
        public void Initialize(EnemyDataSO enemyData)
        {
            // Enemy Data 누락으로 인해 게임 에러 방지를 위한 임시 데이터 
            if (enemyData == null)
            {
                Debug.LogWarning("[EnemyDataBattle] EnemyDataSO가 null입니다! 임시 데이터로 세팅합니다.");
                EnemyId = "Mock_01";
                EnemyName = "시연용 허수아비";
                MaxHealth = 1;
                CurrentHealth = 1;
                return;
            }

            EnemyId = enemyData.EnemyId;
            EnemyName = enemyData.EnemyName;
            MaxHealth = enemyData.MaxHealth;
            CurrentHealth = enemyData.MaxHealth;

            Debug.Log($"[EnemyDataBattle] 적 세팅 완료: {EnemyName} (HP: {CurrentHealth}/{MaxHealth})");
        }

        // TurnReady 시작 시 호출되어 로직에 따라 이번턴 행동을 결정 합니다.
        public void GenerateIntent()
        {
            List<HwaTuCard> allCards = HwaTuCardDatabase.CreateAllCards();
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

            Debug.Log($"[EnemyDataBattle] 적 의도 결정됨: {result.DisplayName} (공격력: {result.BasePower})");
        }
        

        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            CurrentHealth -= damage;
            if (CurrentHealth < 0) CurrentHealth = 0;
            
            Debug.Log($"[EnemyData] 적이 {damage}의 피해를 입었습니다! 남은 체력: {CurrentHealth}");
        }
    }
}