using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;
using System;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_004_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            new BattleModifier($"{enemyId}_OddSum_DmgMul", ModifierValueType.Damage, 
                new CardSumParityCondition(false), new DamageMultiplierEffect(2f))
        };
    }

}