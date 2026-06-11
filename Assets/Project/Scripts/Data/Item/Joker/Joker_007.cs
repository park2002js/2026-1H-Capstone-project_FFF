using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 다음 턴에 한해 드로우 장수 +3 증가.
    /// 연관: ModifierValueType.DrawCount, TargetTurnCondition, DrawCountAddEffect
    /// </summary>
    public class Joker_007 : JokerItemBase
    {
        public override string Id => "Joker_007";
        protected override string SpriteFileName => "gaksi";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 컨텍스트에서 현재 턴을 참조하여 다음 턴 번호 산출
            int nextTurn = context.CurrentTurnNumber + 1;

            // 1턴 지연 후 단발성 발동을 위한 부품 조립 (생명 주기 2턴: 이번 턴 대기, 다음 턴 발동 후 만료)
            var modifier = new BattleModifier(
                id: $"{Id}_Draw3_NextTurn",
                targetType: ModifierValueType.DrawCount,
                condition: new TargetTurnCondition(nextTurn),
                effect: new DrawCountAddEffect(3),
                turns: 2
            );

            ModifierManager.Instance.AddModifier(modifier);
            onConsume?.Invoke();
        }
    }
}