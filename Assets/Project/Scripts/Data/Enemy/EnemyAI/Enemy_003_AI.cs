using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;
using System.Linq;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_003_AI", menuName = "FFF/AI/Enemy_003")]
    public class Enemy_003_AI : EnemyAISO
    {
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            var pool = HwaTuCardDatabase.CreateAllCards().Where(c => c.GetMonthValue() % 2 == 0 && c.GetMonthValue() != 0).ToList();
            return pool.OrderBy(x => Random.value).Take(2).ToList();
        }
    }
}