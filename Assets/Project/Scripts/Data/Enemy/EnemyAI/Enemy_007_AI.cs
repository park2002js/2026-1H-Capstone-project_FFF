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
    public class Enemy_007_AI : EnemyAISO
    {
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            return HwaTuCardDatabase.CreateAllCards().OrderBy(x => UnityEngine.Random.value).Take(2).ToList();
        }
    }
}