using System.Collections.Generic;

namespace FFF.Data
{
    /// <summary>
    /// 화투 48장 전체 카드 데이터를 생성하고 관리한다.
    /// 
    /// 구성:
    /// - 1~10월: 각 월 4장 (광, 띠, 열끗, 피) = 40장 → 섯다 족보 판정용
    /// - 11~12월: 각 월 4장 = 8장 → 특수 카드
    /// - 합계: 48장
    /// 
    /// 참고: 실제 화투에서 모든 월에 광이 있는 건 아니지만 (1,3,8월만 광),
    /// 게임 밸런스를 위해 각 월 4장씩 균등 배분한다.
    /// 실제 화투 구성으로 변경이 필요하면 이 클래스만 수정하면 된다.
    /// </summary>
    public static class HwaTuCardDatabase
    {
        /// <summary>
        /// 화투 48장 전체 카드 목록을 생성하여 반환한다.
        /// </summary>
        public static List<HwaTuCard> CreateAllCards()
        {
            var cards = new List<HwaTuCard>();

            // === 1~10월: 족보 판정용 일반 카드 (40장) ===
            cards.AddRange(CreateMonthCards(CardMonth.January, "송학"));
            cards.AddRange(CreateMonthCards(CardMonth.February, "매화"));
            cards.AddRange(CreateMonthCards(CardMonth.March, "벚꽃"));
            cards.AddRange(CreateMonthCards(CardMonth.April, "흑싸리"));
            cards.AddRange(CreateMonthCards(CardMonth.May, "난초"));
            cards.AddRange(CreateMonthCards(CardMonth.June, "모란"));
            cards.AddRange(CreateMonthCards(CardMonth.July, "홍싸리"));
            cards.AddRange(CreateMonthCards(CardMonth.August, "공산"));
            cards.AddRange(CreateMonthCards(CardMonth.September, "국화"));
            cards.AddRange(CreateMonthCards(CardMonth.October, "단풍"));

            // === 11~12월: 특수 카드 (8장) ===
            cards.AddRange(CreateSpecialMonthCards(CardMonth.November, "오동"));
            cards.AddRange(CreateSpecialMonthCards(CardMonth.December, "비"));

            return cards;
        }

        /// <summary>
        /// 1~10월의 한 달치 카드 4장을 생성한다.
        /// </summary>
        private static List<HwaTuCard> CreateMonthCards(CardMonth month, string monthName)
        {
            int m = (int)month;
            return new List<HwaTuCard>
            {
                new HwaTuCard(month, CardType.Gwang, $"M{m}_Gwang",
                    $"{m}월 광 ({monthName})"),
                new HwaTuCard(month, CardType.Tti, $"M{m}_Tti",
                    $"{m}월 띠 ({monthName})"),
                new HwaTuCard(month, CardType.Yeolkkeut, $"M{m}_Yeolkkeut",
                    $"{m}월 열끗 ({monthName})"),
                new HwaTuCard(month, CardType.Pi, $"M{m}_Pi",
                    $"{m}월 피 ({monthName})")
            };
        }

        /// <summary>
        /// 11~12월의 특수 카드 4장을 생성한다.
        /// IsSpecial = true로 설정되어 족보 판정에서 제외된다.
        /// </summary>
        private static List<HwaTuCard> CreateSpecialMonthCards(CardMonth month, string monthName)
        {
            int m = (int)month;
            return new List<HwaTuCard>
            {
                new HwaTuCard(month, CardType.Gwang, $"M{m}_Gwang",
                    $"{m}월 광 ({monthName})", isSpecial: true),
                new HwaTuCard(month, CardType.Tti, $"M{m}_Tti",
                    $"{m}월 띠 ({monthName})", isSpecial: true),
                new HwaTuCard(month, CardType.Yeolkkeut, $"M{m}_Yeolkkeut",
                    $"{m}월 열끗 ({monthName})", isSpecial: true),
                new HwaTuCard(month, CardType.Pi, $"M{m}_Pi",
                    $"{m}월 피 ({monthName})", isSpecial: true)
            };
        }

        /// <summary>
        /// CardId로 카드를 찾는다.
        /// </summary>
        public static HwaTuCard FindCardById(List<HwaTuCard> cards, string cardId)
        {
            return cards.Find(c => c.CardId == cardId);
        }

        /// <summary>
        /// 특정 월의 카드들만 필터링한다.
        /// </summary>
        public static List<HwaTuCard> GetCardsByMonth(List<HwaTuCard> cards, CardMonth month)
        {
            return cards.FindAll(c => c.Month == month);
        }

        /// <summary>
        /// 족보 판정 가능한 카드(1~10월)만 필터링한다.
        /// </summary>
        public static List<HwaTuCard> GetNormalCards(List<HwaTuCard> cards)
        {
            return cards.FindAll(c => !c.IsSpecial);
        }

        /// <summary>
        /// 특수 카드(11~12월)만 필터링한다.
        /// </summary>
        public static List<HwaTuCard> GetSpecialCards(List<HwaTuCard> cards)
        {
            return cards.FindAll(c => c.IsSpecial);
        }
    }
}
