using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 사용 시 이번 턴(총 1턴) 동안 플레이어 최종 공격력 +20 가산.
    /// 연관: ModifierValueType.Strength, AlwaysTrueCondition, StrengthConstantEffect
    /// </summary>
    public class Joker_004 : JokerItemBase
    {
        public override string Id => "Joker_004";
        protected override string SpriteFileName => "gaksi";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 상시 조건 및 공격력 20 가산 부품 조립 (생명 주기 1턴)
            var modifier = new BattleModifier(
                id: $"{Id}_StrUp20_1Turn",
                targetType: ModifierValueType.Strength,
                condition: new AlwaysTrueCondition(),
                effect: new StrengthConstantEffect(20),
                turns: 1
            );

            ModifierManager.Instance.AddModifier(modifier);
            onConsume?.Invoke();
        }
    }
}