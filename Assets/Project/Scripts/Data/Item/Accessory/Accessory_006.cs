using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 3턴째부터 플레이어 최종 공격력 +10 가산.
    /// 연관: ModifierValueType.Strength, MinTurnCondition, StrengthConstantEffect
    /// </summary>
    public class Accessory_006 : AccessoryItemBase
    {
        public override string Id => "Accessory_006";
        protected override string SpriteFileName => "jadering";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 최소 턴 조건 및 공격력 가산 부품 조립 (영구 지속)
            _modifier = new BattleModifier(
                id: $"{Id}_StrengthUp_FromTurn3",
                targetType: ModifierValueType.Strength,
                condition: new MinTurnCondition(3),
                effect: new StrengthConstantEffect(10),
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