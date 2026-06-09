using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_004_Gimmick : EnemyGimmickSO
    {
        // 6월이 포함되면 기믹 발동
        private readonly int _targetMonth = 6;
        
        // 가산할 공격력 수치 지정.
        private readonly int _strengthBonus = 5;

        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            // 타겟 월 포함 여부 판별 부품과 공격력 가산 연산 부품 조립.
            new BattleModifier(
                id: $"{enemyId}_Include{_targetMonth}_StrUp", 
                targetType: ModifierValueType.Strength, 
                condition: new HandIncludeMonthCondition(_targetMonth), 
                effect: new StrengthConstantEffect(_strengthBonus),
                turns: BattleModifier.PERMANENT_TURN
            )
        };
    }
}