using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 3턴째부터 플레이어가 받는 데미지 -10 감소.
    /// 연관: ModifierValueType.Damage, MinTurnCondition, IsPlayerAttackingCondition, DamageConstantEffect
    /// </summary>
    public class Accessory_004 : AccessoryItemBase
    {
        public override string Id => "Accessory_004";
        protected override string SpriteFileName => "jadering";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 최소 턴 조건 및 피격 조건 결합 부품 조립 (영구 지속)
            _modifier = new BattleModifier(
                id: $"{Id}_RecvDmgDown_FromTurn3",
                targetType: ModifierValueType.Damage,
                condition: new AndCondition(
                    new MinTurnCondition(3),
                    new IsPlayerAttackingCondition(false)
                ),
                effect: new DamageConstantEffect(-10),
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