using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;
using System.Linq;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_002_AI", menuName = "FFF/AI/Enemy_002")]
    public class Enemy_002_AI : EnemyAISO
    {
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            var pool = HwaTuCardDatabase.CreateAllCards();
            if (Random.value < 0.9f)
            {
                var card7 = pool.FirstOrDefault(c => c.GetMonthValue() == 7);
                if (card7 != null) return new List<HwaTuCard> { card7, pool[Random.Range(0, pool.Count)] };
            }
            return pool.OrderBy(x => Random.value).Take(2).ToList();
        }
    }
}