using System.Collections.Generic;
using System.Linq;
using FFF.Data;
using FFF.Battle.Modifier;

namespace FFF.Battle.Damage
{
    public class DamageCal
    {
        /// <summary>
        /// 승자의 기본 공격력(승리 족보 점수)을 받아 파이프라인을 통과시킨 '최종 데미지'를 반환합니다.
        /// </summary>
        public int CalculateFinalDamage(int winnerBaseStrength, ModifierManager modifierManager, ModifierContext context)
        {
            int finalDamage = winnerBaseStrength;

            // 파이프라인(ModifierManager)을 통과시켜 데미지 증폭/감소 버프 적용
            if (modifierManager != null)
            {
                // ValueType.Damage(피해량) 라벨이 붙은 부품들만 반응합니다. (예: 방어도, 데미지 2배 조커 등)
                finalDamage = modifierManager.ProcessValue(ModifierValueType.Damage, winnerBaseStrength, context);
            }

            // 데미지는 절대 음수가 될 수 없음
            return finalDamage < 0 ? 0 : finalDamage;
        }
    }
}