namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 공격력 상수를 가산합니다.
    /// </summary>
    public class StrengthConstantEffect : IModifierEffect
    {
        private readonly int _addValue;

        public StrengthConstantEffect(int addValue)
        {
            _addValue = addValue;
        }

        public void Apply(ModifierContext context)
        {
            context.StrengthAddConstant += _addValue;
        }
    }
}