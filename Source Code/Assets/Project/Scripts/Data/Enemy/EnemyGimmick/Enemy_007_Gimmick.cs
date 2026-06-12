using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_007_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            // 플레이어 공격력에 턴 수 비례 감소(-2) 적용 부품 조립
            new BattleModifier(
                id: $"{enemyId}_Turn_StrDown", 
                targetType: ModifierValueType.Strength, 
                condition: new AlwaysTrueCondition(), 
                effect: new DynamicTurnStrengthEffect(-2),
                turns: BattleModifier.PERMANENT_TURN
            )
        };
    }
}