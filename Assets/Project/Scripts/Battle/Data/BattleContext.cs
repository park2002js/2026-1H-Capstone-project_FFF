namespace FFF.Battle.Data
{
    /// <summary>
    /// 단일 전투 세션의 모든 휘발성 데이터를 저장하는 공용 바구니 (Blackboard).
    /// 상태 매니저들은 서로를 모른 채 이 객체에만 데이터를 쓰고 읽습니다.
    /// </summary>
    public class BattleContext
    {
        public bool IsPlayerWinner { get; set; }
        
        // 추가: 이번 스테이지 전용 로컬 플레이어 데이터
        public PlayerDataBattle PlayerData { get; set; } 
    }
}