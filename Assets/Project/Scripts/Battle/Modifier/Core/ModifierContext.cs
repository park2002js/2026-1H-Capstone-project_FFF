using FFF.Data;
using FFF.Battle.Data;
using FFF.Battle.Enemy;

// TO-DO List : ModifierValueType과 역할을 명확히 구분할 필요가 존재함

namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 전투 중 Modifier가 참조하고 개입할 상태 및 수치 컨테이너.
    /// 정확히는 Modifier Manager가 외부 호출에 의해 효과 계산을 할 때, 이 클래스 객체를 만든 뒤 속성에 접근해서 변경사항을 누적
    /// 이 수치들은 이후 그 수치에 대해 전문적으로 다룰 곳에서 
    /// </summary>
    public class ModifierContext
    {
        // === 시간에 대한 정보 ===
        /// <summary> 전투 시작부터 카운트된 현재 진행 중인 턴 번호 </summary>
        public int CurrentTurnNumber { get; set; }
        
        // === 전투 개체 정보 ===
        public PlayerDataBattle Player { get; set; }
        //public EnemyDataBattle Enemy { get; set; } //적의 경우 적 내부에서 처리할 것이므로 

        // === 액션 결과 정보 ===
        /// <summary> 방금 제출한 카드의 족보 (공격 연산 시에만 존재, 평소엔 null) </summary>
        public SeotdaResult? ActionHandResult { get; set; }

        // === 효과 누적용 휘발성 데이터 ===
        public int StrengthAddConstant { get; set; }
        public float StrengthMultiplier { get; set; }
        public int DamageAddConstant { get; set; }
        public float DamageMultiplier { get; set; }
        public int ExtraDrawCount { get; set; }
        public int ExtraRerollCount { get; set; }

        public ModifierContext()
        {
            Reset();
        }

        /// <summary>
        /// 새로운 파이프라인 연산 전, 누적된 수치를 초기화함.
        /// </summary>
        public void Reset()
        {
            StrengthAddConstant = 0;
            StrengthMultiplier = 1.0f;
            DamageAddConstant = 0;
            DamageMultiplier = 1.0f;
            ExtraDrawCount = 0;
            ExtraRerollCount = 0;
        }
    }

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
}