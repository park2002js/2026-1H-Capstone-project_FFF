using UnityEngine;

namespace FFF.Data
{
    /// <summary>
    /// 화투 카드 한 장의 데이터를 담는 ScriptableObject.
    /// 
    /// Unity Editor에서 개별 .asset 파일로 생성하여
    /// Inspector에서 카드 정보를 설정한다.
    /// 
    /// 런타임에서는 ToHwaTuCard()로 순수 데이터 클래스(HwaTuCard)로 변환하여 사용한다.
    /// 이렇게 하면 게임 로직 쪽은 SO 의존 없이 HwaTuCard만 알면 된다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHwaTuCard", menuName = "FFF/Data/HwaTu Card")]
    public class HwaTuCardSO : ScriptableObject
    {
        [Header("=== 카드 기본 정보 ===")]

        [Tooltip("카드의 월 (1~12)")]
        public CardMonth Month;

        [Tooltip("카드의 종류 (광, 띠, 열끗, 피)")]
        public CardType Type;

        [Tooltip("카드 고유 ID. 형식: M{월}_{종류} (예: M1_Gwang)")]
        public string CardId;

        [Tooltip("카드 표시 이름 (한글). 예: 1월 광 (송학)")]
        public string DisplayName;

        [Header("=== 특수 카드 ===")]

        [Tooltip("11~12월 특수 카드 여부")]
        public bool IsSpecial;

        [Tooltip("카드 종류별 추가 효과 ID (미확정 시 빈 문자열)")]
        public string EffectId = "";

        /// <summary>
        /// SO 데이터를 런타임용 HwaTuCard 인스턴스로 변환한다.
        /// </summary>
        public HwaTuCard ToHwaTuCard()
        {
            return new HwaTuCard(Month, Type, CardId, DisplayName, IsSpecial, EffectId);
        }
    }
}
