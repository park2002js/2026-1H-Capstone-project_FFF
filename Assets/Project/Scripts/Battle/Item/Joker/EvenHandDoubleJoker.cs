using FFF.Data;

namespace FFF.Battle.Item.Joker
{
    /// <summary>
    /// 조커 샘플 1: 짝수 족보 패일 경우 공격력을 2배로 한다.
    /// 
    /// 짝수 족보 조건: 두 카드 월 값의 합이 짝수.
    /// (짝수 땡, 짝수 끗 모두 이 조건에 포함됨)
    /// 
    /// 사용 시 JokerManager에 조건부 데미지 배율을 등록한다.
    /// 턴 종료 시 JokerManager가 알아서 초기화.
    /// 데미지 계산 코드는 GetDamageMultiplier()만 호출하면 됨 (알빠노).
    /// </summary>
    public class EvenHandDoubleJoker : JokerBase
    {
        public override string Id => "JKR_EVEN_DOUBLE";
        public override string DisplayName => "짝수의 축복";
        public override string Description => "짝수 족보(두 카드 합이 짝수)일 경우 공격력 2배.";

        private const float DAMAGE_MULTIPLIER = 2.0f;

        protected override void Activate(JokerContext context)
        {
            // JokerManager에 조건부 배율 등록.
            // 턴 종료 시 JokerManager가 알아서 초기화한다.
            context.JokerManager.SetDamageMultiplier(DAMAGE_MULTIPLIER, IsEvenHand);
        }

        /// <summary>
        /// 족보가 짝수인지 판정한다.
        /// 두 카드 월 값의 합이 짝수이면 true.
        /// </summary>
        private static bool IsEvenHand(SeotdaResult result)
        {
            int month1 = result.Card1.GetMonthValue();
            int month2 = result.Card2.GetMonthValue();
            int sum = month1 + month2;

            return sum % 2 == 0;
        }
    }
}
