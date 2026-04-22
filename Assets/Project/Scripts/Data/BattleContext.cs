namespace FFF.Data
{
    /// <summary>
    /// 단일 전투 세션의 모든 휘발성 데이터를 저장하는 공용 바구니 (Blackboard).
    /// 상태 매니저들은 서로를 모른 채 이 객체에만 데이터를 쓰고 읽습니다.
    /// </summary>
    public class BattleContext
    {
        // MVP: 누가 이겼는지만 저장
        public bool IsPlayerWinner { get; set; }
        
        // 💡 훗날 여기에 턴 수, 사용 조커, 획득 점수 등을 자유롭게 추가할 수 있습니다.
    }
}