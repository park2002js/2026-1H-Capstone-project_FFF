using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 플레이어 카드 조합 숫자 합 비례 공격력 가산.
    /// 연관: ModifierValueType.Strength, AlwaysTrueCondition, StrengthAddCardSumEffect
    /// </summary>
    public class Accessory_002 : AccessoryItemBase
    {
        public override string Id => "Accessory_002"; 
        protected override string SpriteFileName => "jadering"; // 임시 이미지 파일명 적용

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 제출 카드 합계 비례 공격력 가산 연산 효과 부품 조립
            _modifier = new BattleModifier(
                id: $"{Id}_CardSumStrengthBonus",
                targetType: ModifierValueType.Strength,
                condition: new AlwaysTrueCondition(),
                effect: new StrengthAddCardSumEffect(),
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