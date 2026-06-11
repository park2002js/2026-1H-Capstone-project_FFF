using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_013_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            // 턴 홀짝과 카드 합 홀짝 불일치 시 데미지 0 처리 부품 조립.
            new BattleModifier(
                id: $"{enemyId}_ParityMismatch_DmgZero", 
                targetType: ModifierValueType.Damage, 
                condition: new NotCondition(new TurnAndSumParityMatchCondition()), 
                effect: new DamageMultiplierEffect(0f),
                turns: BattleModifier.PERMANENT_TURN
            )
        };
    }
}