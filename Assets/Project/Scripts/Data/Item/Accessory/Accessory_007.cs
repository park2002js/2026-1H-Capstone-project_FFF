using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 플레이어 땡 조합 시 최종 공격력 +5 가산.
    /// 연관: ModifierValueType.Strength, HandJokboCondition, StrengthConstantEffect
    /// </summary>
    public class Accessory_007 : AccessoryItemBase
    {
        public override string Id => "Accessory_007";
        protected override string SpriteFileName => "jadering";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 땡 족보 제출 시 공격력 상승 부품 조립
            _modifier = new BattleModifier(
                id: $"{Id}_Ddaeng_StrUp",
                targetType: ModifierValueType.Strength,
                condition: new HandJokboCondition(HandCategory.Ddaeng),
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