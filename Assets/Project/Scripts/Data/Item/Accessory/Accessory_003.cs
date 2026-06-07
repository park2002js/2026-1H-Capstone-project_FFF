using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 악세서리 003 아이템 클래스.
    /// 부여 효과: 피해 계산 시 데미지 +5 상수 추가.
    /// 연관: ModifierValueType.Damage, AlwaysTrueCondition, DamageConstantEffect
    /// </summary>
    public class Accessory_003 : AccessoryItemBase
    {
        public override string Id => "Accessory_003";
        protected override string SpriteFileName => "silverknife";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 데미지 파이프라인에 +5 상수를 무조건 더하는 모디파이어 생성
            _modifier = new BattleModifier(
                id: $"{Id}_DamageBonus",
                targetType: ModifierValueType.Damage,
                condition: new AlwaysTrueCondition(),
                effect: new DamageConstantEffect(5),
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

namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 최종 피해량 상수 가산용 단일 효과 부품.
    /// 목적: 합 승리 후 산출되는 기본 데미지에 고정 수치를 더함.
    /// 연관: ModifierContext.DamageAddConstant, CombatCalculator
    /// </summary>
    public class DamageConstantEffect : IModifierEffect
    {
        private readonly int _addValue;
        public DamageConstantEffect(int addValue) { _addValue = addValue; }
        
        public void Apply(ModifierContext context)
        {
            context.DamageAddConstant += _addValue;
        }
    }
}