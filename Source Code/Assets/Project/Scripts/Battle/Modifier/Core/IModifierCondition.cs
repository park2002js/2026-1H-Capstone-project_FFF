namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 아이템 및 버프가 '언제 발동하는가?'를 결정하는 조건(Condition) 조립 블록입니다.
    /// </summary>
    public interface IModifierCondition
    {
        /// <summary>
        /// 주어진 상황(Context)에서 이 효과가 발동될 수 있는지 판별합니다.
        /// </summary>
        /// <param name="context">
        /// 조건 판별에 필요한 문맥 정보. 
        /// (예: 실시간 데미지 계산 시 SeotdaResult가 넘어오고, 조건이 필요 없는 캐싱 스탯은 null이 넘어옵니다)
        /// </param>
        /// <returns>조건이 충족되면 true, 아니면 false를 반환합니다.</returns>
        bool IsMet(ModifierContext context = null);
    }
}