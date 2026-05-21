using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;
using System.Linq;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_007_AI", menuName = "FFF/AI/Enemy_007")]
    public class Enemy_007_AI : EnemyAISO
    {
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            return HwaTuCardDatabase.CreateAllCards().OrderBy(x => Random.value).Take(2).ToList();
        }
    }
}