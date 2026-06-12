using UnityEngine;
using FFF.Data;
using FFF.Battle.Modifier;

namespace FFF.Battle.Damage
{
    public class StrengthCal
    {
        /// <summary>
        /// 플레이어가 선택한 카드의 기본 족보 공격력에 
        /// ModifierContext의 추가 공격력 상수와 계수를 적용하여 
        /// 최종 공격력을 반환합니다.
        /// </summary>
        public int CalculateExpectedStrength(HwaTuCard card1, HwaTuCard card2, ModifierContext context)
        {
            // 1. 순수 족보 판정 및 기본 공격력 산출
            SeotdaResult result = SeotdaJudge.Judge(card1, card2);
            
            if (context != null)
            {
                context.ActionHandResult = result;
                
                // 등록된 모든 공격력 관련 Modifier를 실행하여 context 내부 수치를 갱신합니다.
                ModifierManager.Instance.ProcessStrength(context);
                
                // 공식: (기본 공격력 + 추가 상수) * 계수
                int finalStrength = (int)((result.BasePower + context.StrengthAddConstant) * context.StrengthMultiplier);
                
                // 0 미만의 음수 공격력은 나오지 않도록 조정
                return Mathf.Max(0, finalStrength);
            }

            return result.BasePower;
        }
    }
}