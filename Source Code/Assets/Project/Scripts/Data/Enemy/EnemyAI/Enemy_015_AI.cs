using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_015_AI : EnemyAISO
    {
        // 2턴 주기 땡 조합 추출, 그 외 무작위 조합 추출.
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            List<HwaTuCard> pool = HwaTuCardDatabase.CreateAllCards();
            
            if (context.CurrentTurnNumber % 2 == 0)
            {
                var ddaengGroups = pool.GroupBy(c => c.Month).Where(g => g.Count() >= 2).ToList();
                if (ddaengGroups.Count > 0)
                {
                    var selectedGroup = ddaengGroups[UnityEngine.Random.Range(0, ddaengGroups.Count)].ToList();
                    return selectedGroup.OrderBy(x => UnityEngine.Random.value).Take(2).ToList();
                }
            }
            
            return pool.OrderBy(x => UnityEngine.Random.value).Take(2).ToList();
        }
    }
}