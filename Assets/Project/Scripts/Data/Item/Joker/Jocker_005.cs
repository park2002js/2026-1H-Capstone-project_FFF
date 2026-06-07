using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 사용 효과: 이번 턴에 적에게 받는 데미지를 0으로 만듦.
    /// </summary>
    public class Jocker_005 : JokerItemBase
    {
        public override string Id => "Jocker_005";
        //public override string DisplayName => "방어 조커";
        //public override string Description => "이번 턴에 받는 데미지를 무효화합니다.";
        protected override string SpriteFileName => "somoo";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            BattleModifier modifier = new BattleModifier(
                id: $"{Id}_DamageZero",
                targetType: ModifierValueType.Damage,
                condition: new IsPlayerAttackingCondition(false),
                effect: new DamageMultiplierEffect(0f),
                turns: 1 
            );
            ModifierManager.Instance.AddModifier(modifier);
        }
    }
}
