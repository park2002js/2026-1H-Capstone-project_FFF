using FFF.Data;

namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 특정 세부 족보(알리, 독사, 장사 등)와 정확히 일치하는지 판별하는 조건 부품.
    /// </summary>
    public class SpecificHandCondition : IModifierCondition
    {
        private readonly SeotdaHand _targetHand;

        public SpecificHandCondition(SeotdaHand targetHand)
        {
            _targetHand = targetHand;
        }

        public bool IsMet(ModifierContext context)
        {
            if (context?.ActionHandResult == null) return false;
            return context.ActionHandResult.Value.Hand == _targetHand;
        }
    }
}