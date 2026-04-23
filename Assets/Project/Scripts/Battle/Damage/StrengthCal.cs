using System.Collections.Generic;
using System.Linq;
using FFF.Data;
using FFF.Battle.Modifier;

namespace FFF.Battle.Damage
{
    public class StrengthCal
    {
        /// <summary>
        /// 카드 2장과 배달통(Context)을 받아 파이프라인을 거친 '최종 예상 공격력'을 반환합니다.
        /// </summary>
        public int CalculateExpectedStrength(HwaTuCard card1, HwaTuCard card2, ModifierManager modifierManager, ModifierContext context)
        {
            // 1. 순수 족보 판정 및 기본 공격력 산출
            SeotdaResult result = SeotdaJudge.Judge(card1, card2);
            int baseStrength = result.BasePower;

            // 배달통(Context)에 방금 낸 족보 결과를 담음
            // 이렇게 해야 파이프라인의 'EvenHandCondition(짝수 조건 부품)' 등이 이 배달통을 열어보고 판정할 수 있습니다.
            if (context != null)
            {
                context.ActionHandResult = result;
            }

            // 2. 파이프라인(ModifierManager)을 통과시켜 각종 버프가 적용된 최종 공격력 획득
            if (modifierManager != null)
            {
                // ValueType.Strength(공격력) 라벨이 붙은 부품들만 알아서 연쇄 작용을 일으킵니다.
                return modifierManager.ProcessValue(ModifierValueType.Strength, baseStrength, context);
            }

            return baseStrength;
        }
    }
}