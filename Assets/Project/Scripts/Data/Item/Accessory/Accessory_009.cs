using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 플레이어 카드 중 광 카드가 하나라도 포함 시 최종 공격력 +5 가산.
    /// 연관: ModifierValueType.Strength, HandIncludeTypeCondition, StrengthConstantEffect
    /// </summary>
    public class Accessory_009 : AccessoryItemBase
    {
        public override string Id => "Accessory_009";
        protected override string SpriteFileName => "jadering";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 광 카드 포함 조건 판별 부품 조립
            _modifier = new BattleModifier(
                id: $"{Id}_GwangInclude_StrUp",
                targetType: ModifierValueType.Strength,
                condition: new HandIncludeTypeCondition(CardType.Gwang),
                effect: new StrengthConstantEffect(5),
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