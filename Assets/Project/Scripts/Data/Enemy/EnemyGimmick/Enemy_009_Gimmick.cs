using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FFF.Battle.Data;
using FFF.Battle.Modifier;
using System;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_009_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            // 합계 10 이하 시 데미지 2배 증폭
            new BattleModifier($"{enemyId}_SumUnder10_DmgDouble", ModifierValueType.Damage, 
                new CardSumCondition(false, 10), new DamageMultiplierEffect(2f)),
                
            // 합계 11 이상(10 초과) 시 데미지 절반 감소
            new BattleModifier($"{enemyId}_SumOver10_DmgHalf", ModifierValueType.Damage, 
                new CardSumCondition(true, 11), new DamageMultiplierEffect(0.5f))
        };
    }
}