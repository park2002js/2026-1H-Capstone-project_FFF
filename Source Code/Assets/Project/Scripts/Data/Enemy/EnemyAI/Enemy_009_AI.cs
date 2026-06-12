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
    public class Enemy_009_AI : EnemyAISO
    {
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            // 전체 카드 풀 무작위 정렬
            var pool = HwaTuCardDatabase.CreateAllCards().OrderBy(x => UnityEngine.Random.value).ToList();

            // 두 카드의 월 숫자 합이 10 이하인 조합 탐색
            for (int i = 0; i < pool.Count - 1; i++)
            {
                for (int j = i + 1; j < pool.Count; j++)
                {
                    if (pool[i].GetMonthValue() + pool[j].GetMonthValue() <= 10)
                    {
                        // 조건 충족 조합 발견 시 반환
                        return new List<HwaTuCard> { pool[i], pool[j] };
                    }
                }
            }

            // 조건 충족 조합 부재 시 기본 무작위 반환 (안전 장치)
            return pool.Take(2).ToList();
        }
    }
}