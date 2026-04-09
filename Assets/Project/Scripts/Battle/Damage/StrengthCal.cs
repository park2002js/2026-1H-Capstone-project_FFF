using System.Collections.Generic;
using System.Linq;
using FFF.Data;
using FFF.Battle.Modifier;
using FFF.Battle.Damage;

namespace FFF.Battle.Damage
{
    public class StrengthCal
    {
        /// <summary>
        /// 카드 2장과 현재 활성화된 모디파이어를 받아 최종 예상 공격력을 반환합니다.
        /// </summary>
        public int CalculateExpectedStrength(HwaTuCard card1, HwaTuCard card2, IReadOnlyList<TurnModifier> activeModifiers)
        {
            // 1. 순수 족보 공격력
            SeotdaResult result = SeotdaJudge.Judge(card1, card2);
            int finalStrength = result.BasePower;

            // 2. 공격력(Strength) 카테고리에 해당하는 모디파이어 값만 필터링하여 합산
            if (activeModifiers != null)
            {
                var strengthBuffs = activeModifiers.Where(m => m.Type == ModifierType.Strength);
                foreach (var buff in strengthBuffs)
                {
                    finalStrength += buff.Value;
                }
            }

            return finalStrength;
        }
    }
}