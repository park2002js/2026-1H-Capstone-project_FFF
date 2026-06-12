namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 내부에 조립된 **모든 조건 부품들이 true를 반환할 때만** 문을 열어주는(true 반환) 논리곱(AND) 게이트키퍼입니다.
    /// 
    /// - 언제 쓰이는가?
    /// "플레이어의 체력이 30% 이하이면서(조건1), 동시에 짝수 족보를 냈을 때(조건2)" 와 같이
    /// 여러 개의 기믹이 모두 충족되어야 발동하는 까다로운 아이템을 만들 때 쓰입니다.
    /// 
    /// - 왜 쓰이는가?
    /// 만약 이 부품이 없다면, "체력 30% 이하 + 짝수 족보"를 확인하는 새로운 단일 클래스를 계속해서 찍어내야 합니다. (클래스 폭발 현상)
    /// 하지만 AndCondition이 있으면, 기존에 만들어둔 [체력 조건 부품]과 [짝수 조건 부품]을 
    /// 그냥 이 상자에 한꺼번에 던져 넣는 것만으로 새로운 복합 조건이 완성됩니다.
    /// </summary>
    public class AndCondition : IModifierCondition
    {
        private readonly IModifierCondition[] _conditions;

        /// <summary>
        /// 여러 개의 조건 부품들을 하나로 묶는 AND 그룹을 생성합니다.
        /// (params 키워드를 사용하여 몇 개든 콤마(,)로 연결해 넣을 수 있습니다)
        /// </summary>
        /// <param name="conditions">모두 충족되어야 하는 조건 부품들</param>
        public AndCondition(params IModifierCondition[] conditions)
        {
            _conditions = conditions;
        }

        public bool IsMet(ModifierContext context)
        {
            // 조립된 조건이 아예 없다면, 막을 이유가 없으므로 true 반환
            if (_conditions == null || _conditions.Length == 0) return true;

            // 조립된 조건 중 단 하나라도 false를 뱉으면, 즉시 false
            foreach (var condition in _conditions)
            {
                if (!condition.IsMet(context))
                {
                    return false;
                }
            }

            // 모든 조건을 무사히 통과했다면 true 반환
            return true;
        }
    }
}