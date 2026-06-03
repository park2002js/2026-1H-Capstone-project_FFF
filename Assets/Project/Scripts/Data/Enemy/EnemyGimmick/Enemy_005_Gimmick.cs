using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    [CreateAssetMenu(fileName = "Enemy_005_Gimmick", menuName = "FFF/Gimmick/Enemy_005")]
    public class Enemy_005_Gimmick : EnemyGimmickSO
    {
        public override List<BattleModifier> CreateGimmickModifiers(string enemyId) => new List<BattleModifier>
        {
            // 끗이 아닐 경우 데미지 배율을 0으로 만듦
            new BattleModifier($"{enemyId}_NotKkeut_DmgZero", ModifierValueType.Damage, 
                new NotCondition(new HandJokboCondition(HandCategory.Kkeut)), new DamageMultiplierEffect(0f))
        };
    }
}