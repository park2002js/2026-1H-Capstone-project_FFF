namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 데미지 연산 시 플레이어의 공격(승리) 여부를 판별함.
    /// </summary>
    public class IsPlayerAttackingCondition : IModifierCondition
    {
        private readonly bool _requireAttacking;

        public IsPlayerAttackingCondition(bool requireAttacking)
        {
            _requireAttacking = requireAttacking;
        }

        public bool IsMet(ModifierContext context)
        {
            if (context == null) return false;
            return context.IsPlayerAttacking == _requireAttacking;
        }
    }
}