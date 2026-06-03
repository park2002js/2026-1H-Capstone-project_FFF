using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 장착 시 기본 리롤 횟수 +1 증가.
    /// </summary>
    public class Accessory_001 : AccessoryItemBase
    {
        public override string Id => "Accessory_001"; // 실제 이름과 ID는 다름
        protected override string SpriteFileName => "jadering";

        // 등록된 Modifier 참조 보관용 (해제 목적)
        private BattleModifier _modifier;

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 리롤 횟수 증가 연산 효과 부품 생성
            IModifierEffect rerollEffect = new MaxRerollsAddEffect(1);

            // 영구 적용 BattleModifier 조립
            _modifier = new BattleModifier(
                id: $"{Id}_RerollBonus",
                targetType: ModifierValueType.MaxRerolls,
                condition: new AlwaysTrueCondition(),
                effect: rerollEffect,
                turns: BattleModifier.PERMANENT_TURN
            );

            // ModifierManager 파이프라인에 효과 등록
            ModifierManager.Instance.AddModifier(_modifier);
        }

        public override void Remove(ModifierContext context)
        {
            // 보관된 Modifier가 존재할 경우 파이프라인에서 제거
            if (_modifier != null)
            {
                ModifierManager.Instance.RemoveModifier(_modifier);
                _modifier = null;
            }
        }
    }
    /// <summary>
    /// 최대 리롤 횟수 가산용 단일 효과 클래스.
    /// 기존 구현체 부재 대비 전용 부품으로 선언.
    /// </summary>
    public class MaxRerollsAddEffect : IModifierEffect
    {
        private readonly int _addValue;
        public MaxRerollsAddEffect(int addValue) { _addValue = addValue; }
        
        public void Apply(ModifierContext context)
        {
            context.ExtraRerollCount += _addValue;
        }
    }
}