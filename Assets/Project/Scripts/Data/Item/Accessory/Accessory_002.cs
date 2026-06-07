using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 악세서리 002 아이템 클래스.
    /// 부여 효과: 끗 조합 시 공격력 +5.
    /// 연관: ModifierValueType.Strength, HandJokboCondition, StrengthConstantEffect
    /// </summary>
    public class Accessory_002 : AccessoryItemBase
    {
        public override string Id => "Accessory_002"; 
        protected override string SpriteFileName => "gat"; // 임시 이미지 파일명

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 끗 족보 조건과 공격력 +5 상수 연산 효과를 결합하여 모디파이어 생성
            _modifier = new BattleModifier(
                id: $"{Id}_KkeutStrengthBonus",
                targetType: ModifierValueType.Strength,
                condition: new HandJokboCondition(HandCategory.Kkeut),
                effect: new StrengthConstantEffect(5),
                turns: BattleModifier.PERMANENT_TURN
            );

            // 파이프라인에 영구 효과 등록
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