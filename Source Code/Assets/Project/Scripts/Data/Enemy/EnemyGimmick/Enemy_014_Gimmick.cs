using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_014_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            // 플레이어 카드에 포함된 1월 카드 수량 비례 공격력(+10) 증가 부품 조립.
            new BattleModifier(
                id: $"{enemyId}_MatchMonth1_StrBonus", 
                targetType: ModifierValueType.Strength, 
                condition: new AlwaysTrueCondition(), 
                effect: new StrengthAddMatchMonthCountEffect(1, 10),
                turns: BattleModifier.PERMANENT_TURN
            )
        };
    }
}