using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_012_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            // 플레이어가 타격자이면서 끗 족보를 사용했을 경우 데미지 배율 0 처리 부품 조립
            new BattleModifier(
                id: $"{enemyId}_KkeutAttack_DmgZero", 
                targetType: ModifierValueType.Damage, 
                condition: new AndCondition(
                    new IsPlayerAttackingCondition(true),
                    new HandJokboCondition(HandCategory.Kkeut)
                ), 
                effect: new DamageMultiplierEffect(0f),
                turns: BattleModifier.PERMANENT_TURN
            )
        };
    }
}