namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 최종 피해량 상수 가산용 단일 효과 부품.
    /// 목적: 합 승리 후 산출되는 기본 데미지에 고정 수치를 더함.
    /// 연관: ModifierContext.DamageAddConstant, CombatCalculator
    /// </summary>
    public class DamageConstantEffect : IModifierEffect
    {
        private readonly int _addValue;
        public DamageConstantEffect(int addValue) { _addValue = addValue; }
        
        public void Apply(ModifierContext context)
        {
            context.DamageAddConstant += _addValue;
        }
    }
}