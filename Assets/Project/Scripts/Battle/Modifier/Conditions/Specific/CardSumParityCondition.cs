namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 낸 카드 2장의 월(Month) 합계가 짝수/홀수인지 판별합니다.
    /// </summary>
    public class CardSumParityCondition : IModifierCondition
    {
        private readonly bool _checkEven;

        /// <param name="checkEven">true일 경우 합이 짝수일 때 조건 만족, false일 경우 합이 홀수일 때 조건 만족</param>
        public CardSumParityCondition(bool checkEven)
        {
            _checkEven = checkEven;
        }

        public bool IsMet(ModifierContext context)
        {
            if (context?.ActionHandResult == null) return false;
            
            var hand = context.ActionHandResult.Value;
            int sum = hand.Card1.GetMonthValue() + hand.Card2.GetMonthValue();
            bool isSumEven = sum % 2 == 0;
            
            return isSumEven == _checkEven;
        }
    }
}