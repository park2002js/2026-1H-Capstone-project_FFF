using System;
using FFF.Battle.Modifier;
using FFF.Data; // SeotdaHand 열거형 사용을 위해 필요

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 플레이어 구삥(1-9) 족보 조합 시 최종 공격력 +5 가산.
    /// 연관: ModifierValueType.Strength, SpecificHandCondition, StrengthConstantEffect
    /// </summary>
    public class Accessory_014 : AccessoryItemBase
    {
        public override string Id => "Accessory_014";
        protected override string SpriteFileName => "jadering"; // 임시 이미지 파일명 적용

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 구삥(1-9) 족보 판별 부품 조립
            _modifier = new BattleModifier(
                id: $"{Id}_SpecificHand_StrUp",
                targetType: ModifierValueType.Strength,
                condition: new SpecificHandCondition(SeotdaHand.GuBbing), 
                effect: new StrengthConstantEffect(5),
                turns: BattleModifier.PERMANENT_TURN
            );

            // 파이프라인에 영구 효과 등록
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