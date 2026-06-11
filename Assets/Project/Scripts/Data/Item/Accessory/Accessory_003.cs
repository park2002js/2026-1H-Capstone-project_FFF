using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 첫 턴부터 3턴째까지만 플레이어가 받는 데미지 -5 감소.
    /// 연관: ModifierValueType.Damage, IsPlayerAttackingCondition, DamageConstantEffect
    /// </summary>
    public class Accessory_003 : AccessoryItemBase
    {
        public override string Id => "Accessory_003";
        protected override string SpriteFileName => "jadering"; // 임시 이미지 파일명 적용

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 피격 시 데미지 감소 부품 조립 (생명 주기 3턴)
            _modifier = new BattleModifier(
                id: $"{Id}_RecvDmgDown_3Turns",
                targetType: ModifierValueType.Damage,
                condition: new IsPlayerAttackingCondition(false),
                effect: new DamageConstantEffect(-5),
                turns: 3
            );

            // 파이프라인에 효과 등록
            ModifierManager.Instance.AddModifier(_modifier);
        }

        public override void Remove(ModifierContext context)
        {
            // 보관된 Modifier 파이프라인 제거
            if (_modifier != null)
            {
                ModifierManager.Instance.RemoveModifier(_modifier);
                _modifier = null;
            }
        }
    }
}