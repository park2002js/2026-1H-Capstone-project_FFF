namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 제출한 2장 카드의 월(Month) 합계만큼 공격력 상수에 가산하는 연산 부품.
    /// </summary>
    public class StrengthAddCardSumEffect : IModifierEffect
    {
        public void Apply(ModifierContext context)
        {
            if (context?.ActionHandResult == null) return;
            var hand = context.ActionHandResult.Value;
            
            int sum = hand.Card1.GetMonthValue() + hand.Card2.GetMonthValue();
            context.StrengthAddConstant += sum;
        }
    }
}