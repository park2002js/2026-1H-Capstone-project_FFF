namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 지정된 턴(최소 턴) 이상일 때부터 영구적으로 발동을 허용하는 조건 부품.
    /// (예: 3턴째부터 발동)
    /// </summary>
    public class MinTurnCondition : IModifierCondition
    {
        private readonly int _minTurn;

        public MinTurnCondition(int minTurn)
        {
            _minTurn = minTurn;
        }

        public bool IsMet(ModifierContext context)
        {
            if (context == null) return false;
            return context.CurrentTurnNumber >= _minTurn;
        }
    }
}