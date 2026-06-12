namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 지정된 조건의 반대(NOT) 상황일 때 true를 반환합니다.
    /// </summary>
    public class NotCondition : IModifierCondition
    {
        private readonly IModifierCondition _condition;
        public NotCondition(IModifierCondition condition) { _condition = condition; }
        public bool IsMet(ModifierContext context) => !_condition.IsMet(context);
    }
}