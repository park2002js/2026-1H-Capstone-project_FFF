namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 낸 카드 2장의 월(Month) 합이 목표값 이상인지 이하인지 판별합니다.
    /// </summary>
    public class CardSumCondition : IModifierCondition
    {
        private readonly bool _isAtLeast;
        private readonly int _threshold;

        /// <param name="isAtLeast">true일 경우 합이 '이상'일때 조건 만족, false일 경우 합이 '이하'일 때 조건 만족</param>
        /// <param name="threshold"> 이상-이하 기준이 되는 값
        public CardSumCondition(bool isAtLeast, int threshold)
        {
            _isAtLeast = isAtLeast;
            _threshold = threshold;
        }

        public bool IsMet(ModifierContext context)
        {
            if (context?.ActionHandResult == null) return false;
            
            var hand = context.ActionHandResult.Value;
            int sum = hand.Card1.GetMonthValue() + hand.Card2.GetMonthValue();
            bool isSumAtLeastThreshold = sum >= _threshold;
            
            return isSumAtLeastThreshold == _isAtLeast;
        }
    }
}