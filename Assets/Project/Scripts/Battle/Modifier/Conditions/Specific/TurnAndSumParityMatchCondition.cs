namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 현재 턴의 홀짝 여부와 낸 카드 월(Month) 합의 홀짝 여부가 일치하는지 판별합니다.
    /// (현재로써는 Enemy_013 기믹을 위한 전용 조건)
    /// </summary>
    public class TurnAndSumParityMatchCondition : IModifierCondition
    {
        public bool IsMet(ModifierContext context)
        {
            if (context?.ActionHandResult == null) return false;
            bool isTurnEven = context.CurrentTurnNumber % 2 == 0;
            int sum = context.ActionHandResult.Value.Card1.GetMonthValue() + context.ActionHandResult.Value.Card2.GetMonthValue();
            bool isSumEven = sum % 2 == 0;
            return isTurnEven == isSumEven;
        }
    }
}