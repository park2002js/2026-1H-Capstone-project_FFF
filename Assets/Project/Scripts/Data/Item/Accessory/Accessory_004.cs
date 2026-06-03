using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 악세서리 004 아이템 클래스.
    /// 부여 효과: 땡 조합 시 공격력 +50.
    /// 연관: ModifierValueType.Strength, HandJokboCondition, StrengthConstantEffect
    /// </summary>
    public class Accessory_004 : AccessoryItemBase
    {
        public override string Id => "Accessory_004";
        protected override string SpriteFileName => "accessory_004";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 땡 족보 조건과 공격력 +50 상수 연산 효과를 결합하여 모디파이어 생성
            _modifier = new BattleModifier(
                id: $"{Id}_DdaengStrengthBonus",
                targetType: ModifierValueType.Strength,
                condition: new HandJokboCondition(HandCategory.Ddaeng),
                effect: new StrengthConstantEffect(50),
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