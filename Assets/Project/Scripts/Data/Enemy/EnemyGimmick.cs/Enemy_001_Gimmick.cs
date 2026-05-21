using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_001_Gimmick", menuName = "FFF/Gimmick/Enemy_001")]
    public class Enemy_001_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            new BattleModifier($"{enemyId}_Include1_StrUp", ModifierValueType.Strength, 
                new HandIncludeMonthCondition(1), new StrengthConstantEffect(5))
        };
    }
}