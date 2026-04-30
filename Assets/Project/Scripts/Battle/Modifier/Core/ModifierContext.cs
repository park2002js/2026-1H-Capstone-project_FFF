using FFF.Data;

namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 파이프라인을 통과할 때마다 갱신되어 Condition 부품들에게 전달되는 '종합 상황 배달통'입니다.
    /// 
    /// ── 언제 쓰이는가? ──
    /// ModifierManager가 ProcessValue(가로채기)나 SyncValues(캐싱 동기화)를 실행할 때,
    /// 이 객체를 생성하거나 갱신하여 Condition의 IsMet() 함수에 던져줍니다.
    /// 
    /// ── 왜 쓰이는가? ──
    /// Condition 부품들이 "내 체력이 30% 이하인가?", "지금 짝수 족보를 냈는가?"를 판별하려면
    /// 현재 상황에 대한 데이터가 필요합니다. 이 데이터를 파라미터 하나(Context)로 묶어서
    /// 전달하기 위해 사용합니다.
    /// 
    /// ── 언제, 어떻게 수정(확장)하는가? ──
    /// 새로운 기획이 추가되어 기존의 배달통 데이터만으로 판별이 불가능할 때 수정합니다.
    /// [예시]
    /// Q. 기획자: "상점에서 돈이 100원 이상일 때 발동하는 아이템 만들어주세요."
    /// A. 이 파일에 `public int CurrentMoney { get; set; }` 를 추가하고,
    ///    전투 매니저에서 Context를 생성할 때 현재 소지금을 넣어주면 됩니다!
    /// </summary>
    public class ModifierContext
    {
        // === 시간에 대한 정보 ===
        /// <summary> 전투 시작부터 카운트된 현재 진행 중인 턴 번호 </summary>
        public int CurrentTurnNumber { get; set; } 
        
        // === 전투 개체 정보 ===
        public PlayerData Player { get; set; }
        public Battle.Enemy.EnemyData Enemy { get; set; }
        
        // === 액션 결과 정보 ===
        /// <summary> 방금 제출한 카드의 족보 (공격 연산 시에만 존재, 평소엔 null) </summary>
        public SeotdaResult? ActionHandResult { get; set; }
    }
}