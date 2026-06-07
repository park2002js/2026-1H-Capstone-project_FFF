using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 악세서리 005 아이템 클래스.
    /// 부여 효과: 턴 시작 시 드로우 장수 +1 증가.
    /// 연관: ModifierValueType.DrawCount, AlwaysTrueCondition, DrawCountAddEffect
    /// </summary>
    public class Accessory_005 : AccessoryItemBase
    {
        public override string Id => "Accessory_005";
        protected override string SpriteFileName => "norigae";

        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 드로우 증가 파이프라인에 +1 상수를 무조건 더하는 모디파이어 생성
            _modifier = new BattleModifier(
                id: $"{Id}_DrawCountBonus",
                targetType: ModifierValueType.DrawCount,
                condition: new AlwaysTrueCondition(),
                effect: new DrawCountAddEffect(1),
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
    /// 기본 드로우 장수 가산용 단일 효과 부품.
    /// 목적: 턴 시작 시 덱에서 카드를 가져올 때 참조되는 드로우 수치를 동적으로 늘림.
    /// 연관: ModifierContext.ExtraDrawCount, DeckSystem
    /// </summary>
    public class DrawCountAddEffect : IModifierEffect
    {
        private readonly int _addValue;
        public DrawCountAddEffect(int addValue) { _addValue = addValue; }
        
        public void Apply(ModifierContext context)
        {
            context.ExtraDrawCount += _addValue;
        }
    }
}