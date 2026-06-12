using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 부여 효과: 사용 시 이번 턴과 다음 턴(총 2턴) 동안 플레이어 최종 데미지 2배 증폭.
    /// 연관: ModifierValueType.Damage, IsPlayerAttackingCondition, DamageMultiplierEffect
    /// </summary>
    public class Joker_002 : JokerItemBase
    {
        public override string Id => "Joker_002";
        protected override string SpriteFileName => "gaksi"; // 임시 이미지 파일명 지정

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 플레이어 타격 조건 및 데미지 2배 증폭 부품 조립 (생명 주기 2턴)
            var modifier = new BattleModifier(
                id: $"{Id}_DmgDouble_2Turns",
                targetType: ModifierValueType.Damage,
                condition: new IsPlayerAttackingCondition(true),
                effect: new DamageMultiplierEffect(2f),
                turns: 2
            );

            // 파이프라인에 효과 등록
            ModifierManager.Instance.AddModifier(modifier);
            
            // 조커 사용에 따른 소모 콜백 호출
            onConsume?.Invoke();
        }
    }
}