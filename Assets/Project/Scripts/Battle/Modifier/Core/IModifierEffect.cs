namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 아이템 및 버프가 파이프라인을 통과하는 '값을 어떻게 바꾸는가?'를 결정하는 효과(Effect) 조립 블록입니다.
    /// </summary>
    public interface IModifierEffect
    {
        /// <summary>
        /// 파이프라인의 이전 단계에서 넘어온 값을 받아, 연산을 수행한 뒤 뱉어냅니다.
        /// </summary>
        /// <param name="currentValue">아이템이 개입하기 전의 현재 값</param>
        /// <returns>더하기, 곱하기, 덮어쓰기 등이 적용된 최종 연산 값</returns>
        int Apply(int currentValue);
    }
}