using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_003_Gimmick", menuName = "FFF/Gimmick/Enemy_003")]
    public class Enemy_003_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            new BattleModifier($"{enemyId}_EvenSum_DmgMul", ModifierValueType.Damage, 
                new CardSumParityCondition(true), new DamageMultiplierEffect(2f))
        };
    }
}