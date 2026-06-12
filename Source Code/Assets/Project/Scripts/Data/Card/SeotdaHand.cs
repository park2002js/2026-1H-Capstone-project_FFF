namespace FFF.Data
{
    /// <summary>
    /// 섯다 족보 종류.
    /// 위에서부터 강한 순서대로 정렬되어 있다.
    /// 
    /// 섯다 규칙 기준:
    /// - 땡 계열: 같은 월 2장
    /// - 특수 조합: 특정 월 조합 (알리, 독사, 구삥, 장삥, 세륙, 장사)
    /// - 끗 계열: 두 카드 월의 합 끝자리 (0~9)
    /// - 망통: 끗수 0 (가장 약함)
    /// </summary>
    public enum SeotdaHand
    {
        // === 최강 ===
        SamPalGwangDdaeng,  // 38광땡: 3월 광 + 8월 광 (최강)
        IlPalGwangDdaeng,   // 18광땡: 1월 광 + 8월 광
        IlSamGwangDdaeng,   // 13광땡: 1월 광 + 3월 광

        // === 땡 (같은 월 2장, 10땡 > 9땡 > ... > 1땡) ===
        SipDdaeng,    // 10땡: 10월 + 10월
        GuDdaeng,     // 9땡: 9월 + 9월
        PalDdaeng,    // 8땡: 8월 + 8월
        ChilDdaeng,   // 7땡: 7월 + 7월
        YukDdaeng,    // 6땡: 6월 + 6월
        OhDdaeng,     // 5땡: 5월 + 5월
        SaDdaeng,     // 4땡: 4월 + 4월
        SamDdaeng,    // 3땡: 3월 + 3월
        YiDdaeng,     // 2땡: 2월 + 2월
        IlDdaeng,     // 1땡: 1월 + 1월

        // === 특수 조합 ===
        Ali,       // 알리: 1월 + 2월
        Doksa,     // 독사: 1월 + 4월
        GuBbing,   // 구삥: 1월 + 9월
        JangBbing, // 장삥: 1월 + 10월
        SeRyuk,    // 세륙: 4월 + 6월
        JangSa,    // 장사: 4월 + 10월

        // === 끗 (두 월의 합 끝자리, 9끗 > 8끗 > ... > 1끗) ===
        GuKkeut,   // 9끗
        PalKkeut,  // 8끗
        ChilKkeut, // 7끗
        YukKkeut,  // 6끗
        OhKkeut,   // 5끗
        SaKkeut,   // 4끗
        SamKkeut,  // 3끗
        YiKkeut,   // 2끗
        IlKkeut,   // 1끗

        // === 최약 ===
        MangTong   // 망통: 끗수 0 (가장 약함)
    }

    /// <summary>
    /// 섯다 족보의 기본 공격력 테이블.
    /// 족보가 강할수록 높은 공격력을 가진다.
    /// 
    /// 이 수치는 게임 밸런스에 따라 조정 가능하다.
    /// 추후 JSON이나 ScriptableObject로 외부화할 수 있다.
    /// </summary>
    public static class SeotdaHandPower
    {
        /// <summary>
        /// 족보에 해당하는 기본 공격력을 반환한다.
        /// </summary>
        public static int GetBasePower(SeotdaHand hand)
        {
            switch (hand)
            {
                // 광땡 (최강)
                case SeotdaHand.SamPalGwangDdaeng:  return 300;
                case SeotdaHand.IlPalGwangDdaeng:   return 280;
                case SeotdaHand.IlSamGwangDdaeng:   return 260;

                // 땡 (10땡~1땡)
                case SeotdaHand.SipDdaeng:  return 200;
                case SeotdaHand.GuDdaeng:   return 190;
                case SeotdaHand.PalDdaeng:  return 180;
                case SeotdaHand.ChilDdaeng: return 170;
                case SeotdaHand.YukDdaeng:  return 160;
                case SeotdaHand.OhDdaeng:   return 150;
                case SeotdaHand.SaDdaeng:   return 140;
                case SeotdaHand.SamDdaeng:  return 130;
                case SeotdaHand.YiDdaeng:   return 120;
                case SeotdaHand.IlDdaeng:   return 110;

                // 특수 조합
                case SeotdaHand.Ali:       return 100;
                case SeotdaHand.Doksa:     return 95;
                case SeotdaHand.GuBbing:   return 90;
                case SeotdaHand.JangBbing: return 85;
                case SeotdaHand.SeRyuk:    return 80;
                case SeotdaHand.JangSa:    return 75;

                // 끗 (9끗~1끗)
                case SeotdaHand.GuKkeut:   return 60;
                case SeotdaHand.PalKkeut:  return 55;
                case SeotdaHand.ChilKkeut: return 50;
                case SeotdaHand.YukKkeut:  return 45;
                case SeotdaHand.OhKkeut:   return 40;
                case SeotdaHand.SaKkeut:   return 35;
                case SeotdaHand.SamKkeut:  return 30;
                case SeotdaHand.YiKkeut:   return 25;
                case SeotdaHand.IlKkeut:   return 20;

                // 망통 (최약)
                case SeotdaHand.MangTong:  return 10;

                default: return 0;
            }
        }

        /// <summary>
        /// 족보의 한글 이름을 반환한다.
        /// </summary>
        public static string GetDisplayName(SeotdaHand hand)
        {
            switch (hand)
            {
                case SeotdaHand.SamPalGwangDdaeng:  return "38광땡";
                case SeotdaHand.IlPalGwangDdaeng:   return "18광땡";
                case SeotdaHand.IlSamGwangDdaeng:   return "13광땡";

                case SeotdaHand.SipDdaeng:  return "장땡 (10땡)";
                case SeotdaHand.GuDdaeng:   return "9땡";
                case SeotdaHand.PalDdaeng:  return "8땡";
                case SeotdaHand.ChilDdaeng: return "7땡";
                case SeotdaHand.YukDdaeng:  return "6땡";
                case SeotdaHand.OhDdaeng:   return "5땡";
                case SeotdaHand.SaDdaeng:   return "4땡";
                case SeotdaHand.SamDdaeng:  return "3땡";
                case SeotdaHand.YiDdaeng:   return "2땡";
                case SeotdaHand.IlDdaeng:   return "1땡";

                case SeotdaHand.Ali:       return "알리 (1-2)";
                case SeotdaHand.Doksa:     return "독사 (1-4)";
                case SeotdaHand.GuBbing:   return "구삥 (1-9)";
                case SeotdaHand.JangBbing: return "장삥 (1-10)";
                case SeotdaHand.SeRyuk:    return "세륙 (4-6)";
                case SeotdaHand.JangSa:    return "장사 (4-10)";

                case SeotdaHand.GuKkeut:   return "9끗";
                case SeotdaHand.PalKkeut:  return "8끗";
                case SeotdaHand.ChilKkeut: return "7끗";
                case SeotdaHand.YukKkeut:  return "6끗";
                case SeotdaHand.OhKkeut:   return "5끗";
                case SeotdaHand.SaKkeut:   return "4끗";
                case SeotdaHand.SamKkeut:  return "3끗";
                case SeotdaHand.YiKkeut:   return "2끗";
                case SeotdaHand.IlKkeut:   return "1끗";

                case SeotdaHand.MangTong:  return "망통 (0끗)";

                default: return "알 수 없음";
            }
        }
    }
}
