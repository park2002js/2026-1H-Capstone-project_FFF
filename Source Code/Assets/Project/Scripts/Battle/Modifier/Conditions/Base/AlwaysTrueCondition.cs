using FFF.Battle.Modifier;

namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 조건 판정을 무시하고 무조건 문을 열어주는(true 반환) 깡통 게이트키퍼 부품입니다.
    /// 
    /// - 언제 쓰이는가?
    /// 1. "전투 내내 영구적으로 리롤 횟수 +1" (장신구)
    /// 2. "사용 즉시 이번 턴에만 드로우 장수 +1" (일반 조커)
    /// 위와 같이 특별한 발동 조건(체력, 족보 등) 없이 무조건 효과가 적용되어야 할 때 쓰입니다.
    /// 
    /// - 왜 쓰이는가?
    /// 시간적 기믹(이번 턴에만, 영구적으로 등)은 Condition이 아니라 
    /// ItemModifier 껍데기의 수명(TurnsRemaining)이 통제합니다.
    /// 따라서 조건 자체가 필요 없는 아이템들을 위해 파이프라인의 흐름을 방해하지 않는 '프리패스' 부품이 필요합니다.
    /// </summary>
    public class AlwaysTrueCondition : IModifierCondition
    {
        /// <summary>
        /// 배달통(Context)의 내용을 아예 쳐다보지도 않고 무조건 true를 뱉어냅니다.
        /// </summary>
        public bool IsMet(ModifierContext context)
        {
            return true;
        }
    }
}