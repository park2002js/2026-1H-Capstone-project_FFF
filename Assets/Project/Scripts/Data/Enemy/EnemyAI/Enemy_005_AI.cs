using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;
using System.Linq;
using System;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_005_AI : EnemyAISO
    {
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            // 끗을 확정적으로 만들기 위해 서로 다른 월의 일반 카드 2장 반환 (특수조합 배제)
            var pool = HwaTuCardDatabase.CreateAllCards().Where(c => !c.IsSpecial).ToList();
            var card1 = pool[UnityEngine.Random.Range(0, pool.Count)];
            // 광땡이나 알리, 독사 등의 특수 조합을 회피하는 복잡한 로직 대신, 임시로 무작위 카드 반환 후 땡 회피
            var card2 = pool.FirstOrDefault(c => c.GetMonthValue() != card1.GetMonthValue());
            return new List<HwaTuCard> { card1, card2 ?? pool[0] };
        }
    }
}