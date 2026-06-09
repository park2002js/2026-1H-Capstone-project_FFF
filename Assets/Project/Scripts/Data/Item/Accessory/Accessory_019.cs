using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 첫 턴에 한정하여 리롤 횟수 +1 증가.
    /// 연관: ModifierValueType.MaxRerolls, AlwaysTrueCondition, MaxRerollsAddEffect
    /// </summary>
    public class Accessory_019 : AccessoryItemBase
    {
        public override string Id => "Accessory_019";
        protected override string SpriteFileName => "jadering";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 상시 판별이나 생명 주기를 1턴으로 제한하여 첫 턴만 적용되도록 부품 조립
            _modifier = new BattleModifier(
                id: $"{Id}_FirstTurn_RerollUp",
                targetType: ModifierValueType.MaxRerolls,
                condition: new AlwaysTrueCondition(),
                effect: new MaxRerollsAddEffect(1),
                turns: 1 
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