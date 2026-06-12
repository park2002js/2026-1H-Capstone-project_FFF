namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 현재 턴 수에 비례하여 공격력 상수를 가감합니다. (Enemy_007 기믹 전용)
    /// </summary>
    public class DynamicTurnStrengthEffect : IModifierEffect
    {
        private readonly int _multiplierPerTurn; // 예: 턴당 -1 감소면 -1 할당

        public DynamicTurnStrengthEffect(int multiplierPerTurn)
        {
            _multiplierPerTurn = multiplierPerTurn;
        }

        public void Apply(ModifierContext context)
        {
            context.StrengthAddConstant += (context.CurrentTurnNumber * _multiplierPerTurn);
        }
    }
}