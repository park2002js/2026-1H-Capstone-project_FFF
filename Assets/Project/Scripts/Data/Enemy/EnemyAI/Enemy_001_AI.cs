using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;
using System.Linq;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_001_AI", menuName = "FFF/AI/Enemy_001")]
    public class Enemy_001_AI : EnemyAISO
    {
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            var pool = HwaTuCardDatabase.CreateAllCards();
            if (Random.value < 0.9f)
            {
                var card1 = pool.FirstOrDefault(c => c.GetMonthValue() == 1);
                if (card1 != null) return new List<HwaTuCard> { card1, pool[Random.Range(0, pool.Count)] };
            }
            return pool.OrderBy(x => Random.value).Take(2).ToList();
        }
    }
}