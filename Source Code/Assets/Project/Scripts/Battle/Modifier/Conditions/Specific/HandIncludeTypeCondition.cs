using FFF.Data;

namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 제출한 카드 중 특정 종류(광, 띠 등)가 포함되어 있는지 판별하는 조건 부품.
    /// </summary>
    public class HandIncludeTypeCondition : IModifierCondition
    {
        private readonly CardType _targetType;

        public HandIncludeTypeCondition(CardType targetType)
        {
            _targetType = targetType;
        }

        public bool IsMet(ModifierContext context)
        {
            if (context?.ActionHandResult == null) return false;
            var hand = context.ActionHandResult.Value;
            return hand.Card1.Type == _targetType || hand.Card2.Type == _targetType;
        }
    }
}