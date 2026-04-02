using System.Collections.Generic;

namespace FFF.Data
{
    /// <summary>
    /// 실제 섯다에서 사용하는 20장 카드 데이터를 생성하고 관리한다.
    /// 
    /// 구성 (실제 화투 기준, 1~10월 각 2장 = 20장):
    ///   1월: 광(송학) + 피          → 광땡 가능
    ///   2월: 열끗(꾀꼬리) + 피
    ///   3월: 광(벚꽃) + 피          → 광땡 가능
    ///   4월: 열끗(흑싸리) + 피
    ///   5월: 열끗(난초) + 피
    ///   6월: 열끗(모란) + 피
    ///   7월: 열끗(홍싸리) + 피
    ///   8월: 광(공산) + 피          → 광땡 가능
    ///   9월: 열끗(국화) + 피
    ///  10월: 열끗(단풍) + 피
    /// 
    /// 광은 1, 3, 8월에만 각 1장씩 존재한다 (총 3장).
    /// 나머지 월의 첫 번째 카드는 열끗이다.
    /// 각 월의 두 번째 카드는 피이다.
    /// 
    /// 11~12월은 섯다에서 사용하지 않으므로 제외.
    /// 추후 덱빌딩으로 특수 카드를 추가하려면 별도 메서드로 확장한다.
    /// </summary>
    public static class HwaTuCardDatabase
    {
        /// <summary>
        /// 섯다 기본 20장 카드 목록을 생성하여 반환한다.
        /// </summary>
        public static List<HwaTuCard> CreateAllCards()
        {
            var cards = new List<HwaTuCard>
            {
                // === 1월 - 송학: 광 + 피 ===
                new HwaTuCard(CardMonth.January, CardType.Gwang, "M1_Gwang", "1월 광 (송학)"),
                new HwaTuCard(CardMonth.January, CardType.Pi, "M1_Pi", "1월 피 (송학)"),

                // === 2월 - 매화: 열끗(꾀꼬리) + 피 ===
                new HwaTuCard(CardMonth.February, CardType.Yeolkkeut, "M2_Yeolkkeut", "2월 열끗 (매화)"),
                new HwaTuCard(CardMonth.February, CardType.Pi, "M2_Pi", "2월 피 (매화)"),

                // === 3월 - 벚꽃: 광 + 피 ===
                new HwaTuCard(CardMonth.March, CardType.Gwang, "M3_Gwang", "3월 광 (벚꽃)"),
                new HwaTuCard(CardMonth.March, CardType.Pi, "M3_Pi", "3월 피 (벚꽃)"),

                // === 4월 - 흑싸리: 열끗 + 피 ===
                new HwaTuCard(CardMonth.April, CardType.Yeolkkeut, "M4_Yeolkkeut", "4월 열끗 (흑싸리)"),
                new HwaTuCard(CardMonth.April, CardType.Pi, "M4_Pi", "4월 피 (흑싸리)"),

                // === 5월 - 난초: 열끗 + 피 ===
                new HwaTuCard(CardMonth.May, CardType.Yeolkkeut, "M5_Yeolkkeut", "5월 열끗 (난초)"),
                new HwaTuCard(CardMonth.May, CardType.Pi, "M5_Pi", "5월 피 (난초)"),

                // === 6월 - 모란: 열끗 + 피 ===
                new HwaTuCard(CardMonth.June, CardType.Yeolkkeut, "M6_Yeolkkeut", "6월 열끗 (모란)"),
                new HwaTuCard(CardMonth.June, CardType.Pi, "M6_Pi", "6월 피 (모란)"),

                // === 7월 - 홍싸리: 열끗 + 피 ===
                new HwaTuCard(CardMonth.July, CardType.Yeolkkeut, "M7_Yeolkkeut", "7월 열끗 (홍싸리)"),
                new HwaTuCard(CardMonth.July, CardType.Pi, "M7_Pi", "7월 피 (홍싸리)"),

                // === 8월 - 공산: 광 + 피 ===
                new HwaTuCard(CardMonth.August, CardType.Gwang, "M8_Gwang", "8월 광 (공산)"),
                new HwaTuCard(CardMonth.August, CardType.Pi, "M8_Pi", "8월 피 (공산)"),

                // === 9월 - 국화: 열끗 + 피 ===
                new HwaTuCard(CardMonth.September, CardType.Yeolkkeut, "M9_Yeolkkeut", "9월 열끗 (국화)"),
                new HwaTuCard(CardMonth.September, CardType.Pi, "M9_Pi", "9월 피 (국화)"),

                // === 10월 - 단풍: 열끗 + 피 ===
                new HwaTuCard(CardMonth.October, CardType.Yeolkkeut, "M10_Yeolkkeut", "10월 열끗 (단풍)"),
                new HwaTuCard(CardMonth.October, CardType.Pi, "M10_Pi", "10월 피 (단풍)")
            };

            return cards;
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
        /// 광 카드만 필터링한다 (1, 3, 8월 광 = 3장).
        /// </summary>
        public static List<HwaTuCard> GetGwangCards(List<HwaTuCard> cards)
        {
            return cards.FindAll(c => c.Type == CardType.Gwang);
        }

        /// <summary>
        /// 특정 종류의 카드만 필터링한다.
        /// </summary>
        public static List<HwaTuCard> GetCardsByType(List<HwaTuCard> cards, CardType type)
        {
            return cards.FindAll(c => c.Type == type);
        }
    }
}