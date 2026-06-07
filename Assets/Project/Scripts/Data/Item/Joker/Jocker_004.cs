using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 사용 효과: 3턴 동안 공격력 +10 증가.
    /// </summary>
    public class Jocker_004 : JokerItemBase
    {
        public override string Id => "Jocker_004";
        // public override string DisplayName => "지속 공격력 조커";
        // public override string Description => "3턴 동안 공격력이 10 증가합니다.";
        protected override string SpriteFileName => "yangban";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            BattleModifier modifier = new BattleModifier(
                id: $"{Id}_StrengthBoostDuration",
                targetType: ModifierValueType.Strength,
                condition: new AlwaysTrueCondition(),
                effect: new StrengthConstantEffect(10),
                turns: 3 
            );
            ModifierManager.Instance.AddModifier(modifier);
        }
    }
}
