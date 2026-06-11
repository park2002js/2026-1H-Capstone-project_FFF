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
    public class Enemy_006_AI : EnemyAISO
    {
        public override List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context)
        {
            // 전체 카드 풀 무작위 정렬 연산
            List<HwaTuCard> pool = HwaTuCardDatabase.CreateAllCards().OrderBy(x => UnityEngine.Random.value).ToList();

            // 끗 또는 망통 족보에 해당하는 조합 탐색
            for (int i = 0; i < pool.Count - 1; i++)
            {
                for (int j = i + 1; j < pool.Count; j++)
                {
                    SeotdaResult result = SeotdaJudge.Judge(pool[i], pool[j]);
                    
                    if ((result.Hand >= SeotdaHand.GuKkeut && result.Hand <= SeotdaHand.IlKkeut) || result.Hand == SeotdaHand.MangTong)
                    {
                        // 조건 충족 시 해당 조합 반환
                        return new List<HwaTuCard> { pool[i], pool[j] };
                    }
                }
            }

            // 조건 충족 조합 부재 시 기본 무작위 추출 (안전 장치)
            return pool.Take(2).ToList();
        }
    }
}