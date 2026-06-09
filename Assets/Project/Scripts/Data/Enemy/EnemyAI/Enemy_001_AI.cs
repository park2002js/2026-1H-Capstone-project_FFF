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
    public class Enemy_001_AI : EnemyAISO
    {
        // 9월이 포함된 카드 조합을 냄 
        private readonly int _targetMonth = 9;

        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            List<HwaTuCard> pool = HwaTuCardDatabase.CreateAllCards();

            // 타겟 월을 가진 카드 중 1장 무작위 추출.
            var targetCards = pool.Where(c => c.GetMonthValue() == _targetMonth).ToList();
            HwaTuCard card1 = targetCards.Count > 0 
                ? targetCards[UnityEngine.Random.Range(0, targetCards.Count)] 
                : pool[UnityEngine.Random.Range(0, pool.Count)];

            // "한 장만 포함" 조건을 만족하기 위해, 타겟 월이 아닌 카드 중 1장 무작위 추출.
            var otherCards = pool.Where(c => c.GetMonthValue() != _targetMonth).ToList();
            HwaTuCard card2 = otherCards.Count > 0 
                ? otherCards[UnityEngine.Random.Range(0, otherCards.Count)] 
                : pool[UnityEngine.Random.Range(0, pool.Count)];

            return new List<HwaTuCard> { card1, card2 };
        }
    }
}