namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 현재 턴의 짝수/홀수 여부를 판별합니다.
    /// </summary>
    public class TurnParityCondition : IModifierCondition
    {
        private readonly bool _checkEven;

        /// <param name="checkEven">true일 경우 짝수 턴에 조건 만족, false일 경우 홀수 턴에 조건 만족</param>
        public TurnParityCondition(bool checkEven)
        {
            _checkEven = checkEven;
        }

        public bool IsMet(ModifierContext context)
        {
            if (context == null) return false;
            bool isCurrentTurnEven = context.CurrentTurnNumber % 2 == 0;
            return isCurrentTurnEven == _checkEven;
        }
    }
}