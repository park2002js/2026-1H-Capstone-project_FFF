using FFF.Data;
using FFF.Map;

namespace FFF.Battle.Data
{
    /// <summary>
    /// 전투 씬 진입 시 GameManager가 BattleManager로 전달하는 초기화 데이터 묶음 구조체.
    /// Step 2의 정보 전달 객체화 요구사항 충족 목적.
    /// </summary>
    public struct BattleEntryData
    {
        // === 적 정보 ===
        /// <summary> Table에서 추출하여 조우할 적의 고유 ID </summary>
        public string TargetEnemyId;

        /// <summary> 전투가 시작된 맵 방 타입. 승리 고정 보상 계산 등에 사용한다. </summary>
        public RoomType RoomType;
        
        /// <summary> 난이도(층수)에 비례하여 적에게 부여될 추가 체력 수치 </summary>
        public int EnemyBonusHealth;

        /// <summary> 난이도(층수)에 비례하여 적에게 부여될 추가 공격력 수치 </summary>
        public int EnemyBonusStrength { get; set; }

        // === 플레이어 정보 ===
        /// <summary> 전투 씬에서 로컬 복제용으로 사용할 플레이어 마스터 데이터 원본 </summary>
        public PlayerDataSO PlayerMasterData;
    }
}
