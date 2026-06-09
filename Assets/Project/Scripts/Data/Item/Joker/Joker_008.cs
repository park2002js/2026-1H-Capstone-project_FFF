using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 다음 턴부터 3턴 동안 드로우 장수 +1 증가.
    /// 연관: ModifierValueType.DrawCount, MinTurnCondition, DrawCountAddEffect
    /// </summary>
    public class Joker_008 : JokerItemBase
    {
        public override string Id => "Joker_008";
        protected override string SpriteFileName => "gaksi";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 컨텍스트에서 현재 턴을 참조하여 시작 턴 번호 산출
            int startTurn = context.CurrentTurnNumber + 1;

            // 1턴 지연 후 3턴 연속 발동을 위한 부품 조립 (생명 주기 4턴: 이번 턴 대기, 이후 3턴 발동 후 만료)
            var modifier = new BattleModifier(
                id: $"{Id}_Draw1_3Turns",
                targetType: ModifierValueType.DrawCount,
                condition: new MinTurnCondition(startTurn),
                effect: new DrawCountAddEffect(1),
                turns: 4
            );

            ModifierManager.Instance.AddModifier(modifier);
            onConsume?.Invoke();
        }
    }
}