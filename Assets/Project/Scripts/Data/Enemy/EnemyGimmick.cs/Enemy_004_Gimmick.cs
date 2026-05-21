using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_004_Gimmick", menuName = "FFF/Gimmick/Enemy_004")]
    public class Enemy_004_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            new BattleModifier($"{enemyId}_OddSum_DmgMul", ModifierValueType.Damage, 
                new CardSumParityCondition(false), new DamageMultiplierEffect(2f))
        };
    }

}