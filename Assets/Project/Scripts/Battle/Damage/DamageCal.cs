using UnityEngine;
using FFF.Data;
using FFF.Battle.Modifier;

namespace FFF.Battle.Damage
{
    public class DamageCal
    {
        /// <summary>
        /// 승자의 기본 공격력(승리 족보 점수)을 받아 파이프라인을 통과시킨 '최종 데미지'를 반환합니다.
        /// </summary>
        public int CalculateFinalDamage(int winnerStrength, int loserStrength, ModifierContext context)
        {
            if (context != null)
            {
                // 등록된 모든 데미지 관련 Modifier를 실행하여 context 내부 수치를 갱신합니다.
                ModifierManager.Instance.ProcessDamage(context);
                
                int baseDamage = Mathf.Abs(winnerStrength - loserStrength);
                
                // 공식: (공격력 차이 절댓값 + 추가 데미지 상수) * 계수
                int finalDamage = (int)((baseDamage + context.DamageAddConstant) * context.DamageMultiplier);
                return Mathf.Max(0, finalDamage);
            }

            return Mathf.Max(0, Mathf.Abs(winnerStrength - loserStrength));
        }
    }
}