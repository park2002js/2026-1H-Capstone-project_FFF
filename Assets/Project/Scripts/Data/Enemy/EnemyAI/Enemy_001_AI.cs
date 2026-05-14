using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;
using System.Linq;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_001_AI", menuName = "FFF/AI/Enemy_001_Pattern")]
    public class Enemy_001_AI : EnemyAISO
    {
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            // 1. 전체 카드 풀 생성
            List<HwaTuCard> allCards = HwaTuCardDatabase.CreateAllCards();
            List<HwaTuCard> pickedCards = new List<HwaTuCard>();

            // 2. 현재 턴 확인 (1, 2, 3턴 주기로 계산)
            // BattleManager의 CurrentTurnNumber가 1부터 시작한다고 가정
            int turnInCycle = (context.CurrentTurnNumber - 1) % 3 + 1;

            Debug.Log($"[{self.EnemyName} AI] 현재 패턴 주기: {turnInCycle}/3 (전체 {context.CurrentTurnNumber}턴)");

            if (turnInCycle == 3)
            {
                // [3턴: 무조건 땡 나오게 하기]
                pickedCards = GetRandomDdaeng(allCards);
            }
            else
            {
                // [1, 2턴: 땡이 아닌 무작위 조합]
                pickedCards = GetRandomNonDdaeng(allCards);
            }

            return pickedCards;
        }

        /// <summary>
        /// 무작위로 '땡' 조합(월이 같은 카드 2장)을 찾아 반환합니다.
        /// </summary>
        private List<HwaTuCard> GetRandomDdaeng(List<HwaTuCard> pool)
        {
            // 같은 월(Month)을 가진 카드끼리 그룹화
            var ddaengGroups = pool.GroupBy(c => c.Month)
                                   .Where(g => g.Count() >= 2)
                                   .ToList();

            if (ddaengGroups.Count == 0) return pool.Take(2).ToList(); // 예외 처리

            // 랜덤하게 한 그룹(월) 선택
            var selectedGroup = ddaengGroups[Random.Range(0, ddaengGroups.Count)].ToList();
            
            // 그 그룹에서 카드 2장 선택
            return selectedGroup.OrderBy(x => Random.value).Take(2).ToList();
        }

        /// <summary>
        /// '땡'이 아닌(월이 다른) 무작위 카드 2장을 반환합니다.
        /// </summary>
        private List<HwaTuCard> GetRandomNonDdaeng(List<HwaTuCard> pool)
        {
            List<HwaTuCard> result = new List<HwaTuCard>();
            
            // 첫 번째 카드 랜덤 선택
            int firstIdx = Random.Range(0, pool.Count);
            result.Add(pool[firstIdx]);

            // 두 번째 카드는 첫 번째 카드와 월이 다른 것 중에서 선택
            var filteredPool = pool.Where(c => c.Month != result[0].Month).ToList();
            
            if (filteredPool.Count > 0)
            {
                result.Add(filteredPool[Random.Range(0, filteredPool.Count)]);
            }
            else
            {
                // 만약 월이 다른 카드가 없다면(데이터 오류 상황 등) 그냥 아무거나 선택
                result.Add(pool[(firstIdx + 1) % pool.Count]);
            }

            return result;
        }
    }
}