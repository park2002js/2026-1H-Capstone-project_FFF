using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Data;
using FFF.Battle.Modifier;
using System.Linq;
using System;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_008_AI : EnemyAISO
    {
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            bool isEvenTurn = context.CurrentTurnNumber % 2 == 0;
            var pool = HwaTuCardDatabase.CreateAllCards()
                .Where(c => c.GetMonthValue() != 0 && (c.GetMonthValue() % 2 == 0) == isEvenTurn).ToList();
            return pool.OrderBy(x => UnityEngine.Random.value).Take(2).ToList();
        }
    }
}