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
    public class Enemy_014_AI : EnemyAISO
    {
        // 무조건 1월 카드를 포함하는 무작위 조합 추출.
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            var pool = HwaTuCardDatabase.CreateAllCards();
            var card1 = pool.FirstOrDefault(c => c.GetMonthValue() == 1);
            
            if (card1 != null) 
            {
                var remainingPool = pool.Where(c => c != card1).ToList();
                return new List<HwaTuCard> { card1, remainingPool[UnityEngine.Random.Range(0, remainingPool.Count)] };
            }
            
            return pool.OrderBy(x => UnityEngine.Random.value).Take(2).ToList();
        }
    }
}