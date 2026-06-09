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
    public class Enemy_008_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            // 턴 홀짝과 카드 합 홀짝이 다를 경우 데미지 배율 0
            new BattleModifier($"{enemyId}_ParityMismatch_DmgZero", ModifierValueType.Damage, 
                new NotCondition(new TurnAndSumParityMatchCondition()), new DamageMultiplierEffect(0f))
        };
    }
}