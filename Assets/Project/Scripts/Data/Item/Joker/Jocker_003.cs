using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 사용 효과: 이번 턴에 한해 플레이어가 적에게 주는 데미지 2배.
    /// </summary>
    public class Jocker_003 : JokerItemBase
    {
        public override string Id => "Jocker_003";
        // public override string DisplayName => "더블 데미지 조커";
        // public override string Description => "이번 턴에 적에게 주는 데미지가 2배가 됩니다.";
        protected override string SpriteFileName => "mokjoong";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            BattleModifier modifier = new BattleModifier(
                id: $"{Id}_DoubleDamage",
                targetType: ModifierValueType.Damage,
                condition: new IsPlayerAttackingCondition(true),
                effect: new DamageMultiplierEffect(2f),
                turns: 1 
            );
            ModifierManager.Instance.AddModifier(modifier);
        }
    }
}
