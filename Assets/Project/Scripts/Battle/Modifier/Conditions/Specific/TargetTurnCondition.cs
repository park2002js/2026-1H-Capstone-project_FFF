namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 현재 진행 중인 턴이 '목표로 설정한 특정 턴'과 일치할 때만 문을 열어주는 게이트키퍼 부품입니다.
    /// 
    /// - 언제 쓰이는가?
    /// "전투 시작 후 3턴 뒤에 발동", "사용 후 2턴 뒤에 발동" 등 
    /// 시간차를 두고 지연(Delayed) 발동하는 모든 기믹을 만들 때 쓰입니다.
    /// 
    /// - 왜 쓰이는가?
    /// 매 턴 파이프라인이 돌 때마다 버프가 발동하는 것을 막기 위해서입니다.
    /// 파이프라인이 매 턴 이 부품을 찌르지만, 이 부품은 ModifierContext의 CurrentTurnNumber를 
    /// 확인하며, 목표 턴이 오기 전까지는 계속해서 false를 반환합니다.
    /// </summary>
    public class TargetTurnCondition : IModifierCondition
    {
        private readonly int _targetTurn;

        /// <summary>
        /// 시한폭탄 조건 부품을 생성합니다.
        /// </summary>
        /// <param name="targetTurn">효과가 발동해야 할 정확한 목표 턴 번호</param>
        public TargetTurnCondition(int targetTurn)
        {
            _targetTurn = targetTurn;
        }

        /// <summary>
        /// 배달통에 들어있는 현재 턴 번호가 목표 턴 번호와 일치하는지 판별합니다.
        /// </summary>
        public bool IsMet(ModifierContext context)
        {
            // 배달통이 없거나 턴 정보가 없으면 안전하게 문을 닫습니다.
            if (context == null) return false;

            // 지금이 목표한 그 턴이라면 문을 활짝 엽니다!
            return context.CurrentTurnNumber == _targetTurn;
        }
    }
}