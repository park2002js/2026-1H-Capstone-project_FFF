namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 낸 카드 중 특정 월(Month)이 1장이라도 포함되어 있는지 판별합니다.
    /// </summary>
    public class HandIncludeMonthCondition : IModifierCondition
    {
        private readonly int _targetMonth;

        public HandIncludeMonthCondition(int targetMonth)
        {
            _targetMonth = targetMonth;
        }

        public bool IsMet(ModifierContext context)
        {
            if (context?.ActionHandResult == null) return false;
            
            var hand = context.ActionHandResult.Value;
            return hand.Card1.GetMonthValue() == _targetMonth || hand.Card2.GetMonthValue() == _targetMonth;
        }
    }
}