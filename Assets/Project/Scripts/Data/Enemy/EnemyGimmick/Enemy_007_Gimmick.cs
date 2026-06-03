using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_007_Gimmick", menuName = "FFF/Gimmick/Enemy_007")]
    public class Enemy_007_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            new BattleModifier($"{enemyId}_Turn_StrDown", ModifierValueType.Strength, 
                new AlwaysTrueCondition(), new DynamicTurnStrengthEffect(-1))
        };
    }
}