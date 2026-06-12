using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public class Enemy_010_Gimmick : EnemyGimmickSO
    {
        // 짝수 합 만족시 데미지 2배
        private readonly float _evenMultiplier = 2f;
        
        // 홀수 합 만족시 데미지 0.5배
        private readonly float _oddMultiplier = 0.5f;

        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            // 짝수 합 조건 판별 부품 및 데미지 배율 연산 부품 조립
            new BattleModifier(
                id: $"{enemyId}_EvenSum_DmgMul", 
                targetType: ModifierValueType.Damage, 
                condition: new CardSumParityCondition(true), 
                effect: new DamageMultiplierEffect(_evenMultiplier),
                turns: BattleModifier.PERMANENT_TURN
            ),
            
            // 홀수 합 조건 판별 부품 및 데미지 배율 연산 부품 조립
            new BattleModifier(
                id: $"{enemyId}_OddSum_DmgMul", 
                targetType: ModifierValueType.Damage, 
                condition: new CardSumParityCondition(false), 
                effect: new DamageMultiplierEffect(_oddMultiplier),
                turns: BattleModifier.PERMANENT_TURN
            )
        };
    }
}