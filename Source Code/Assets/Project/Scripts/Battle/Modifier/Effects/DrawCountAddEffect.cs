namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 기본 카드 드로우 횟수를 지정된 수치만큼 증가시키는 연산 부품.
    /// 목적: 턴 시작 시 덱에서 카드를 가져올 때 참조되는 드로우 수치를 동적으로 늘림.
    /// 연관: ModifierContext.ExtraDrawCount, DeckSystem
    /// </summary>
    public class DrawCountAddEffect : IModifierEffect
    {
        private readonly int _addValue;
        public DrawCountAddEffect(int addValue) { _addValue = addValue; }
        
        public void Apply(ModifierContext context)
        {
            if (context != null)
            {
                context.ExtraDrawCount += _addValue;
            }
        }
    }
}