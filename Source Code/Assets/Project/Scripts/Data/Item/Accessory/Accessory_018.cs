using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 매 턴 기본 드로우 횟수 +1 증가. (017과 동일하나 이름이 다름)
    /// 연관: ModifierValueType.DrawCount, AlwaysTrueCondition, DrawCountAddEffect
    /// </summary>
    public class Accessory_018 : AccessoryItemBase
    {
        public override string Id => "Accessory_018";
        protected override string SpriteFileName => "jadering";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 상시 드로우 횟수 증가 부품 조립 (영구 지속)
            _modifier = new BattleModifier(
                id: $"{Id}_DrawCountUp",
                targetType: ModifierValueType.DrawCount,
                condition: new AlwaysTrueCondition(),
                effect: new DrawCountAddEffect(1),
                turns: BattleModifier.PERMANENT_TURN
            );

            ModifierManager.Instance.AddModifier(_modifier);
        }

        public override void Remove(ModifierContext context)
        {
            if (_modifier != null)
            {
                ModifierManager.Instance.RemoveModifier(_modifier);
                _modifier = null;
            }
        }
    }
}