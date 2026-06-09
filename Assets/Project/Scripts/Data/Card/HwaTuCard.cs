using System;

namespace FFF.Data
{
    /// <summary>
    /// 화투 카드의 월(1~12월).
    /// </summary>
    public enum CardMonth
    {
        January = 1,    // 1월 - 송학 (솔)
        February = 2,   // 2월 - 매화 (매조)
        March = 3,      // 3월 - 벚꽃 (사쿠라)
        April = 4,      // 4월 - 흑싸리
        May = 5,        // 5월 - 난초
        June = 6,       // 6월 - 모란
        July = 7,       // 7월 - 홍싸리
        August = 8,     // 8월 - 공산 (달)
        September = 9,  // 9월 - 국화
        October = 10,   // 10월 - 단풍
        November = 11,  // 11월 - 오동 (특수)
        December = 12   // 12월 - 비 (특수)
    }

    /// <summary>
    /// 화투 카드의 종류.
    /// 각 월마다 최대 4종류가 존재한다.
    /// </summary>
    public enum CardType
    {
        Gwang,    // 광 (光) - 가장 높은 등급
        Tti,      // 띠 (홍단, 청단, 초단 등)
        Yeolkkeut, // 열끗 (동물/사물)
        Pi         // 피 - 가장 낮은 등급
    }

    /// <summary>
    /// 화투 카드 한 장의 데이터.
    /// 
    /// 섯다 족보 판정 시 "월(Month)" 값만 사용한다.
    /// 카드 종류(Type)는 추후 카드별 추가 효과에 활용된다.
    /// 
    /// 11~12월 카드는 IsSpecial = true로, 족보 판정 대신 특수 효과를 가진다.
    /// </summary>
    [Serializable]
    public class HwaTuCard
    {
        /// <summary>
        /// 카드의 월 (1~12).
        /// </summary>
        public CardMonth Month;

        /// <summary>
        /// 카드의 종류 (광, 띠, 열끗, 피).
        /// </summary>
        public CardType Type;

        /// <summary>
        /// 카드 고유 ID. 48장 각각을 구분한다.
        /// 형식: "M{월}_{종류}" (예: "M1_Gwang", "M3_Pi")
        /// </summary>
        public string CardId;

        /// <summary>
        /// 카드 표시 이름 (한글).
        /// 예: "1월 광 (송학)", "8월 광 (공산)"
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// 11~12월 특수 카드 여부.
        /// true이면 족보 판정에 사용되지 않고 별도 효과를 가진다.
        /// </summary>
        public bool IsSpecial;

        /// <summary>
        /// 카드 종류별 추가 효과 ID (미확정, 추후 기획 확정 시 사용).
        /// 빈 문자열이면 추가 효과 없음.
        /// </summary>
        public string EffectId;

        /// <summary>
        /// 섯다 족보 판정에 사용되는 월 숫자를 반환한다.
        /// 특수 카드(11~12월)는 0을 반환한다.
        /// </summary>
        public int GetMonthValue()
        {
            if (IsSpecial) return 0;
            return (int)Month;
        }

        public HwaTuCard(CardMonth month, CardType type, string cardId, string displayName, bool isSpecial = false, string effectId = "")
        {
            Month = month;
            Type = type;
            CardId = cardId;
            DisplayName = displayName;
            IsSpecial = isSpecial;
            EffectId = effectId;
        }

        /// <summary>
        /// JSON 역직렬화용 기본 생성자.
        /// </summary>
        public HwaTuCard() { }

        public override string ToString()
        {
            return $"[{CardId}] {DisplayName}";
        }
    }
}
