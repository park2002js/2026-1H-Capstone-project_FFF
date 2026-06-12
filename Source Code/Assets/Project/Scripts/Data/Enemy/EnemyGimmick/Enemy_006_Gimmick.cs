using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_006_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            // 플레이어의 끗 족보 제출 시 공격력 상수 +5 가산 부품 조립
            new BattleModifier(
                id: $"{enemyId}_Kkeut_StrUp", 
                targetType: ModifierValueType.Strength, 
                condition: new HandJokboCondition(HandCategory.Kkeut), 
                effect: new StrengthConstantEffect(5),
                turns: BattleModifier.PERMANENT_TURN
            )
        };
    }
}