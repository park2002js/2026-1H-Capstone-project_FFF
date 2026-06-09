namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 제출한 카드 중 특정 월(Month) 카드의 개수에 비례하여 공격력을 가산하는 연산 부품.
    /// </summary>
    public class StrengthAddMatchMonthCountEffect : IModifierEffect
    {
        private readonly int _targetMonth;
        private readonly int _addValuePerMatch;

        public StrengthAddMatchMonthCountEffect(int targetMonth, int addValuePerMatch)
        {
            _targetMonth = targetMonth;
            _addValuePerMatch = addValuePerMatch;
        }

        public void Apply(ModifierContext context)
        {
            if (context?.ActionHandResult == null) return;
            var hand = context.ActionHandResult.Value;
            int matchCount = 0;
            
            if (hand.Card1.GetMonthValue() == _targetMonth) matchCount++;
            if (hand.Card2.GetMonthValue() == _targetMonth) matchCount++;

            context.StrengthAddConstant += (matchCount * _addValuePerMatch);
        }
    }
}