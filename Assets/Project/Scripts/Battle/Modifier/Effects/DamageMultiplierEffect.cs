namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 데미지 계수를 곱산합니다.
    /// </summary>
    public class DamageMultiplierEffect : IModifierEffect
    {
        private readonly float _multiplier;

        public DamageMultiplierEffect(float multiplier)
        {
            _multiplier = multiplier;
        }

        public void Apply(ModifierContext context)
        {
            context.DamageMultiplier *= _multiplier;
        }
    }
}