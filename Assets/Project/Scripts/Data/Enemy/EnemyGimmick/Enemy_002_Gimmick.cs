using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_002_Gimmick", menuName = "FFF/Gimmick/Enemy_002")]
    public class Enemy_002_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            new BattleModifier($"{enemyId}_Include7_DmgMul", ModifierValueType.Damage, 
                new HandIncludeMonthCondition(7), new DamageMultiplierEffect(7f))
        };
    }
}