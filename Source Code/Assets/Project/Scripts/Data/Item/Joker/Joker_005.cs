using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 사용 시 이번 턴 포함 3턴 동안 플레이어 최종 공격력 +10 가산.
    /// 연관: ModifierValueType.Strength, AlwaysTrueCondition, StrengthConstantEffect
    /// </summary>
    public class Joker_005 : JokerItemBase
    {
        public override string Id => "Joker_005";
        protected override string SpriteFileName => "gaksi";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 상시 조건 및 공격력 10 가산 부품 조립 (생명 주기 3턴)
            var modifier = new BattleModifier(
                id: $"{Id}_StrUp10_3Turns",
                targetType: ModifierValueType.Strength,
                condition: new AlwaysTrueCondition(),
                effect: new StrengthConstantEffect(10),
                turns: 3
            );

            ModifierManager.Instance.AddModifier(modifier);
            onConsume?.Invoke();
        }
    }
}