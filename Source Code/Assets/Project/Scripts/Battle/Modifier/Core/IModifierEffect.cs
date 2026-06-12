namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 아이템 및 버프가 파이프라인을 통과하는 '값을 어떻게 바꾸는가?'를 결정하는 효과(Effect) 조립 블록입니다.
    /// </summary>
    public interface IModifierEffect
    {
        /// <summary>
        /// 전달된 Context 내부의 누적용 변수들을 조작하여 효과를 적용함.
        /// </summary>
        void Apply(ModifierContext context);
    }
}