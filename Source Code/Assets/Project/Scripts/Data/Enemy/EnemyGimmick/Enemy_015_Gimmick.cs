using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_015_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            // 땡 족보 제출 시 데미지 +10 가산 부품 조립.
            new BattleModifier(
                id: $"{enemyId}_Ddaeng_DmgUp", 
                targetType: ModifierValueType.Damage, 
                condition: new HandJokboCondition(HandCategory.Ddaeng), 
                effect: new DamageConstantEffect(10),
                turns: BattleModifier.PERMANENT_TURN
            ),
            
            // 땡 족보 미제출 시 데미지 -10 가산(감소) 부품 조립.
            new BattleModifier(
                id: $"{enemyId}_NotDdaeng_DmgDown", 
                targetType: ModifierValueType.Damage, 
                condition: new NotCondition(new HandJokboCondition(HandCategory.Ddaeng)), 
                effect: new DamageConstantEffect(-10),
                turns: BattleModifier.PERMANENT_TURN
            )
        };
    }
}