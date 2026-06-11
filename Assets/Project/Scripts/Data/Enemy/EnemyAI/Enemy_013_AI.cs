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
    public class Enemy_013_AI : EnemyAISO
    {
        // 턴 홀짝에 맞춰 카드 월(Month) 홀짝 일치 조합 추출.
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            bool isEvenTurn = context.CurrentTurnNumber % 2 == 0;
            var pool = HwaTuCardDatabase.CreateAllCards()
                .Where(c => c.GetMonthValue() != 0 && (c.GetMonthValue() % 2 == 0) == isEvenTurn).ToList();
            
            return pool.OrderBy(x => UnityEngine.Random.value).Take(2).ToList();
        }
    }
}