using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 사용 효과: 이번 턴에 한해 공격력 상수 +50 증가.
    /// </summary>
    public class Jocker_001 : JokerItemBase
    {
        public override string Id => "Jocker_001"; // 실제 이름과 ID는 다름
        protected override string SpriteFileName => "gaksi";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 공격력 +50 상수 연산 효과 부품 생성 (기존 StrengthConstantEffect 활용)
            IModifierEffect strengthEffect = new StrengthConstantEffect(50);

            // 1턴(이번 턴)만 유지되는 단발성 BattleModifier 조립
            BattleModifier modifier = new BattleModifier(
                id: $"{Id}_StrengthBoost",
                targetType: ModifierValueType.Strength,
                condition: new AlwaysTrueCondition(),
                effect: strengthEffect,
                turns: 1 
            );

            // ModifierManager 파이프라인에 효과 등록
            ModifierManager.Instance.AddModifier(modifier);
        }
    }
}