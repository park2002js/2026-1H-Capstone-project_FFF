namespace FFF.Battle.Modifier
{
    public class MaxRerollsAddEffect : IModifierEffect
    {
        private readonly int _addValue;
        public MaxRerollsAddEffect(int addValue) { _addValue = addValue; }
        
        public void Apply(ModifierContext context)
        {
            if (context != null)
            {
                context.ExtraRerollCount += _addValue;
            }
        }
    }
}