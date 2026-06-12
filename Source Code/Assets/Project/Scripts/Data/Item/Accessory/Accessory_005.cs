using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 첫 턴부터 3턴째까지만 플레이어 최종 데미지 +10 가산.
    /// 연관: ModifierValueType.Damage, IsPlayerAttackingCondition, DamageConstantEffect
    /// </summary>
    public class Accessory_005 : AccessoryItemBase
    {
        public override string Id => "Accessory_005";
        protected override string SpriteFileName => "jadering";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 타격 시 데미지 증가 부품 조립 (생명 주기 3턴)
            _modifier = new BattleModifier(
                id: $"{Id}_DealDmgUp_3Turns",
                targetType: ModifierValueType.Damage,
                condition: new IsPlayerAttackingCondition(true),
                effect: new DamageConstantEffect(10),
                turns: 3
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