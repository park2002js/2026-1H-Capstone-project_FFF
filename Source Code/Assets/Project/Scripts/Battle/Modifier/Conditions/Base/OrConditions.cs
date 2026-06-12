namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 내부에 조립된 조건 부품들 중 단 하나라도 true를 반환하면 문을 열어주는(true 반환) 논리합(OR) 게이트키퍼입니다.
    /// 
    /// - 언제 쓰이는가?
    /// "현재 턴이 3턴이거나(조건1), 플레이어의 체력이 10% 이하일 때(조건2)" 와 같이
    /// 여러 위기 상황 중 **하나라도 걸리면 발동**하는 구명조끼 같은 아이템을 만들 때 쓰입니다.
    /// 
    /// - 왜 쓰이는가?
    /// AndCondition과 마찬가지로 부품의 재사용성을 극대화하기 위함입니다. 
    /// "광땡이거나 땡일 때 발동" 처럼 기획자가 A 조건 또는 B 조건을 묶어서 요구할 때 
    /// 기존 부품들을 재활용하여 단 1초 만에 조합을 끝낼 수 있습니다.
    /// </summary>
    public class OrCondition : IModifierCondition
    {
        private readonly IModifierCondition[] _conditions;

        /// <summary>
        /// 여러 개의 조건 부품들을 하나로 묶는 OR 그룹을 생성합니다.
        /// </summary>
        /// <param name="conditions">하나라도 충족되면 통과되는 조건 부품들</param>
        public OrCondition(params IModifierCondition[] conditions)
        {
            _conditions = conditions;
        }

        public bool IsMet(ModifierContext context)
        {
            // 조립된 조건이 없다면, 만족할 건더기가 없으므로 false 반환
            if (_conditions == null || _conditions.Length == 0) return false;

            // 조립된 조건 중 하나라도 true를 뱉으면, 즉시 true
            foreach (var condition in _conditions)
            {
                if (condition.IsMet(context))
                {
                    return true;
                }
            }

            // 모든 조건을 통과하지 못했다면 false 반환
            return false;
        }
    }
}