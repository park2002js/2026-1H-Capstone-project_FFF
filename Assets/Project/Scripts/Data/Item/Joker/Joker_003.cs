using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 사용 시 이번 턴(총 1턴) 동안 플레이어 최종 데미지 3배 증폭.
    /// 연관: ModifierValueType.Damage, IsPlayerAttackingCondition, DamageMultiplierEffect
    /// </summary>
    public class Joker_003 : JokerItemBase
    {
        public override string Id => "Joker_003";
        protected override string SpriteFileName => "gaksi";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 플레이어 타격 조건 및 데미지 3배 증폭 부품 조립 (생명 주기 1턴)
            var modifier = new BattleModifier(
                id: $"{Id}_DmgTriple_1Turn",
                targetType: ModifierValueType.Damage,
                condition: new IsPlayerAttackingCondition(true),
                effect: new DamageMultiplierEffect(3f),
                turns: 1
            );

            ModifierManager.Instance.AddModifier(modifier);
            onConsume?.Invoke();
        }
    }
}