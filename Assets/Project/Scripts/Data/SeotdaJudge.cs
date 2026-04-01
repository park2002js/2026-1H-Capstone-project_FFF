using UnityEngine;

namespace FFF.Data
{
    /// <summary>
    /// 족보 판정 결과를 담는 구조체.
    /// </summary>
    public struct SeotdaResult
    {
        /// <summary>
        /// 판정된 족보.
        /// </summary>
        public SeotdaHand Hand;

        /// <summary>
        /// 족보 기본 공격력.
        /// </summary>
        public int BasePower;

        /// <summary>
        /// 족보 한글 이름.
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// 판정에 사용된 카드 2장.
        /// </summary>
        public HwaTuCard Card1;
        public HwaTuCard Card2;

        /// <summary>
        /// 유효한 족보인지 여부.
        /// 특수 카드(11~12월)만으로 구성되면 false.
        /// </summary>
        public bool IsValid;

        public override string ToString()
        {
            if (!IsValid) return "[무효] 족보 판정 불가";
            return $"{DisplayName} (공격력: {BasePower}) [{Card1} + {Card2}]";
        }
    }

    /// <summary>
    /// 섯다 족보 판정 시스템.
    /// 
    /// 카드 2장을 받아 실제 섯다 규칙에 따라 족보를 판정하고 공격력을 계산한다.
    /// 
    /// 판정 우선순위:
    /// 1. 광땡 (1,3,8월 광 카드 2장 조합)
    /// 2. 땡 (같은 월 2장)
    /// 3. 특수 조합 (알리, 독사, 구삥, 장삥, 세륙, 장사)
    /// 4. 끗 (두 월의 합 끝자리)
    /// 5. 망통 (끗수 0)
    /// </summary>
    public static class SeotdaJudge
    {
        /// <summary>
        /// 카드 2장으로 족보를 판정한다.
        /// </summary>
        /// <param name="card1">첫 번째 카드</param>
        /// <param name="card2">두 번째 카드</param>
        /// <returns>족보 판정 결과</returns>
        public static SeotdaResult Judge(HwaTuCard card1, HwaTuCard card2)
        {
            var result = new SeotdaResult
            {
                Card1 = card1,
                Card2 = card2,
                IsValid = false
            };

            // 두 카드 모두 특수 카드(11~12월)이면 족보 판정 불가
            if (card1.IsSpecial && card2.IsSpecial)
            {
                Debug.LogWarning("[SeotdaJudge] 두 카드 모두 특수 카드라 족보 판정 불가");
                return result;
            }

            // 특수 카드가 하나라도 섞여있으면 특수 카드의 월을 0으로 처리
            int month1 = card1.GetMonthValue();
            int month2 = card2.GetMonthValue();

            // 특수 카드 1장 + 일반 카드 1장인 경우
            // 특수 카드의 월 값은 0이므로, 끗수 = 일반 카드의 월 끝자리로 계산
            if (card1.IsSpecial || card2.IsSpecial)
            {
                int normalMonth = card1.IsSpecial ? month2 : month1;
                int kkeut = normalMonth % 10;
                result.Hand = GetKkeutHand(kkeut);
                result.BasePower = SeotdaHandPower.GetBasePower(result.Hand);
                result.DisplayName = SeotdaHandPower.GetDisplayName(result.Hand);
                result.IsValid = true;
                return result;
            }

            // === 1. 광땡 판정 ===
            SeotdaHand? gwangDdaeng = CheckGwangDdaeng(card1, card2, month1, month2);
            if (gwangDdaeng.HasValue)
            {
                result.Hand = gwangDdaeng.Value;
                result.BasePower = SeotdaHandPower.GetBasePower(result.Hand);
                result.DisplayName = SeotdaHandPower.GetDisplayName(result.Hand);
                result.IsValid = true;
                return result;
            }

            // === 2. 땡 판정 (같은 월 2장) ===
            if (month1 == month2)
            {
                result.Hand = GetDdaengHand(month1);
                result.BasePower = SeotdaHandPower.GetBasePower(result.Hand);
                result.DisplayName = SeotdaHandPower.GetDisplayName(result.Hand);
                result.IsValid = true;
                return result;
            }

            // === 3. 특수 조합 판정 ===
            SeotdaHand? specialHand = CheckSpecialHand(month1, month2);
            if (specialHand.HasValue)
            {
                result.Hand = specialHand.Value;
                result.BasePower = SeotdaHandPower.GetBasePower(result.Hand);
                result.DisplayName = SeotdaHandPower.GetDisplayName(result.Hand);
                result.IsValid = true;
                return result;
            }

            // === 4. 끗 판정 ===
            int kkeutValue = (month1 + month2) % 10;
            result.Hand = GetKkeutHand(kkeutValue);
            result.BasePower = SeotdaHandPower.GetBasePower(result.Hand);
            result.DisplayName = SeotdaHandPower.GetDisplayName(result.Hand);
            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// 광땡 판정.
        /// 1월 광, 3월 광, 8월 광 중 2장이 모두 "광" 타입이어야 광땡이다.
        /// </summary>
        private static SeotdaHand? CheckGwangDdaeng(HwaTuCard card1, HwaTuCard card2, int month1, int month2)
        {
            // 둘 다 광 타입이어야 함
            if (card1.Type != CardType.Gwang || card2.Type != CardType.Gwang)
                return null;

            int small = Mathf.Min(month1, month2);
            int big = Mathf.Max(month1, month2);

            // 3-8 광땡 (최강)
            if (small == 3 && big == 8)
                return SeotdaHand.SamPalGwangDdaeng;

            // 1-8 광땡
            if (small == 1 && big == 8)
                return SeotdaHand.IlPalGwangDdaeng;

            // 1-3 광땡
            if (small == 1 && big == 3)
                return SeotdaHand.IlSamGwangDdaeng;

            return null;
        }

        /// <summary>
        /// 같은 월 2장 → 땡 판정.
        /// </summary>
        private static SeotdaHand GetDdaengHand(int month)
        {
            switch (month)
            {
                case 10: return SeotdaHand.SipDdaeng;
                case 9:  return SeotdaHand.GuDdaeng;
                case 8:  return SeotdaHand.PalDdaeng;
                case 7:  return SeotdaHand.ChilDdaeng;
                case 6:  return SeotdaHand.YukDdaeng;
                case 5:  return SeotdaHand.OhDdaeng;
                case 4:  return SeotdaHand.SaDdaeng;
                case 3:  return SeotdaHand.SamDdaeng;
                case 2:  return SeotdaHand.YiDdaeng;
                case 1:  return SeotdaHand.IlDdaeng;
                default:
                    Debug.LogError($"[SeotdaJudge] 잘못된 월 값으로 땡 판정: {month}");
                    return SeotdaHand.MangTong;
            }
        }

        /// <summary>
        /// 특수 조합 판정 (알리, 독사, 구삥, 장삥, 세륙, 장사).
        /// </summary>
        private static SeotdaHand? CheckSpecialHand(int month1, int month2)
        {
            int small = Mathf.Min(month1, month2);
            int big = Mathf.Max(month1, month2);

            // 알리: 1-2
            if (small == 1 && big == 2) return SeotdaHand.Ali;

            // 독사: 1-4
            if (small == 1 && big == 4) return SeotdaHand.Doksa;

            // 구삥: 1-9
            if (small == 1 && big == 9) return SeotdaHand.GuBbing;

            // 장삥: 1-10
            if (small == 1 && big == 10) return SeotdaHand.JangBbing;

            // 세륙: 4-6
            if (small == 4 && big == 6) return SeotdaHand.SeRyuk;

            // 장사: 4-10
            if (small == 4 && big == 10) return SeotdaHand.JangSa;

            return null;
        }

        /// <summary>
        /// 끗수(0~9)에 해당하는 족보를 반환한다.
        /// </summary>
        private static SeotdaHand GetKkeutHand(int kkeutValue)
        {
            switch (kkeutValue)
            {
                case 9: return SeotdaHand.GuKkeut;
                case 8: return SeotdaHand.PalKkeut;
                case 7: return SeotdaHand.ChilKkeut;
                case 6: return SeotdaHand.YukKkeut;
                case 5: return SeotdaHand.OhKkeut;
                case 4: return SeotdaHand.SaKkeut;
                case 3: return SeotdaHand.SamKkeut;
                case 2: return SeotdaHand.YiKkeut;
                case 1: return SeotdaHand.IlKkeut;
                case 0: return SeotdaHand.MangTong;
                default:
                    Debug.LogError($"[SeotdaJudge] 잘못된 끗 값: {kkeutValue}");
                    return SeotdaHand.MangTong;
            }
        }

        /// <summary>
        /// 두 족보 결과를 비교하여 승자를 반환한다.
        /// </summary>
        /// <returns>양수면 result1 승리, 음수면 result2 승리, 0이면 무승부</returns>
        public static int CompareHands(SeotdaResult result1, SeotdaResult result2)
        {
            // SeotdaHand enum은 강한 순서대로 정의되어 있으므로
            // 값이 작을수록 강한 족보
            if (result1.Hand < result2.Hand) return 1;   // result1 승리
            if (result1.Hand > result2.Hand) return -1;  // result2 승리
            return 0;  // 무승부 (같은 족보)
        }
    }
}
