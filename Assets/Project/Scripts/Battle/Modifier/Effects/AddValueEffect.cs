namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 파이프라인을 통과하는 수치에 단순히 특정 값을 더하거나 빼는(덧셈/뺄셈) 작업자 부품입니다.
    /// 
    /// - 언제 쓰이는가?
    /// 기획 요구사항 중 "무언가를 N만큼 상승/추가/감소 시켜라"라는 기믹을 구현할 때 무조건 쓰입니다.
    /// (예: 데미지 +5, 공격력 +10, 드로우 +1, 리롤 +2 등)
    /// 
    /// - 왜 쓰이는가?
    /// 데미지를 올리든, 리롤 횟수를 올리든 연산의 본질은 "덧셈(+)"으로 완전히 동일합니다.
    /// 따라서 '데미지 증가 부품', '리롤 증가 부품'을 따로 만들 필요 없이, 
    /// ItemModifier(껍데기)의 ValueType을 목적지로 설정하고 이 부품 하나만 끼워넣으면 모든 덧셈 기믹이 해결됩니다.
    /// </summary>
    public class AddValueEffect : IModifierEffect
    {
        private readonly int _amount;

        /// <summary>
        /// 덧셈 부품을 생성합니다.
        /// </summary>
        /// <param name="amount">더할 수치 (음수를 넣으면 뺄셈이 됩니다)</param>
        public AddValueEffect(int amount)
        {
            _amount = amount;
        }

        /// <summary>
        /// 이전 단계에서 넘어온 값에 자신의 수치를 더해서 다음으로 넘깁니다.
        /// </summary>
        public int Apply(int currentValue)
        {
            return currentValue + _amount;
        }
    }
}