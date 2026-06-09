using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 두 번째 턴에 한정하여 리롤 횟수 +2 증가.
    /// 연관: ModifierValueType.MaxRerolls, TargetTurnCondition, MaxRerollsAddEffect
    /// </summary>
    public class Accessory_020 : AccessoryItemBase
    {
        public override string Id => "Accessory_020";
        protected override string SpriteFileName => "jadering";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 2번째 턴 일치 판별 및 생명 주기를 2턴으로 제한(이후 자동 파기)하여 부품 조립
            _modifier = new BattleModifier(
                id: $"{Id}_SecondTurn_RerollUp",
                targetType: ModifierValueType.MaxRerolls,
                condition: new TargetTurnCondition(2),
                effect: new MaxRerollsAddEffect(2),
                turns: 2
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