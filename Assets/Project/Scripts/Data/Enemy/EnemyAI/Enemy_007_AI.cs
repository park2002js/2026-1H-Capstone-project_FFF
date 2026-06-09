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
            // 전체 카드 풀 생성 및 무작위 정렬 후 2장 추출 반환
            return HwaTuCardDatabase.CreateAllCards().OrderBy(x => UnityEngine.Random.value).Take(2).ToList();
        }
    }
}