using System.Collections.Generic;
using System.Linq;
using FFF.Data;
using FFF.Battle.Modifier;
using FFF.Battle.Damage;
namespace FFF.Battle.Damage
{
    public class DamageCal
    {
        
        /// <summary>
        /// 승자(공격자)의 기본 공격력과 양측의 모디파이어(버프/디버프)를 기반으로 
        /// 최종적으로 상대에게 꽂힐 '단일 데미지 정수'를 계산하여 반환합니다.
        /// </summary>
        public int CalculateFinalDamage(int winnerBaseStrength, IReadOnlyList<TurnModifier> attackerModifiers, IReadOnlyList<TurnModifier> defenderModifiers = null)
        {
            int finalDamage = winnerBaseStrength;

            // 1. 공격자의 '데미지(Damage)' 카테고리 모디파이어 합산 (예: 추가 데미지 버프)
            if (attackerModifiers != null)
            {
                var damageBuffs = attackerModifiers.Where(m => m.Type == ModifierType.Strength);
                foreach (var buff in damageBuffs)
                {
                    finalDamage += buff.Value;
                }
            }

            // 2. 방어자의 모디파이어 합산 (예: 받는 피해 감소 방어막 등 - 향후 확장을 위한 방어적 설계)
            if (defenderModifiers != null)
            {
                // TODO: 방어 관련 모디파이어 로직 추가 시 여기에 구현
            }

            // 데미지는 음수가 될 수 없음
            return finalDamage < 0 ? 0 : finalDamage;
        }
    }
}