using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 플레이어 땡 조합으로 공격 시 최종 데미지 +10 가산.
    /// 연관: ModifierValueType.Damage, AndCondition, HandJokboCondition, IsPlayerAttackingCondition, DamageConstantEffect
    /// </summary>
    public class Accessory_008 : AccessoryItemBase
    {
        public override string Id => "Accessory_008";
        protected override string SpriteFileName => "jadering";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 땡 족보 및 플레이어 타격 조건 결합 부품 조립
            _modifier = new BattleModifier(
                id: $"{Id}_DdaengAttack_DmgUp",
                targetType: ModifierValueType.Damage,
                condition: new AndCondition(
                    new HandJokboCondition(HandCategory.Ddaeng),
                    new IsPlayerAttackingCondition(true)
                ),
                effect: new DamageConstantEffect(10),
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