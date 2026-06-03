using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_006_Gimmick", menuName = "FFF/Gimmick/Enemy_006")]
    public class Enemy_006_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            new BattleModifier($"{enemyId}_Ddaeng_DmgMul", ModifierValueType.Damage, 
                new HandJokboCondition(HandCategory.Ddaeng), new DamageMultiplierEffect(2f))
        };
    }
}