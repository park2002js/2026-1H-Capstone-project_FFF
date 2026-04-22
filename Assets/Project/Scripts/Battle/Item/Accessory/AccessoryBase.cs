using FFF.Battle.Modifier;

namespace FFF.Battle.Item.Accessory
{
    /// <summary>
    /// 장신구의 추상 베이스 클래스.
    /// 
    /// ── 핵심 책임 ──
    /// 전투 시작 시 영구적 효과를 적용하고, 전투 종료 시 되돌린다.
    /// 어떤 장신구인지는 외부(AccessoryManager 포함)에서 알 필요 없다.
    /// "적용해" → 알아서 적용. "해제해" → 알아서 해제.
    /// 
    /// ── 설계 원칙 ──
    /// - DeckSystem 등 대상 시스템의 public setter를 통해 효과를 적용한다.
    /// - 대상 시스템은 누가 자기 값을 바꿨는지 알 필요 없다 (알빠노).
    /// - 구체 장신구는 이 클래스를 상속하여 Apply/Remove만 구현한다.
    /// </summary>
    public abstract class AccessoryBase
    {
        /// <summary>장신구 고유 ID.</summary>
        public abstract string Id { get; }

        /// <summary>장신구 표시 이름 (한글).</summary>
        public abstract string DisplayName { get; }

        /// <summary>장신구 설명 텍스트.</summary>
        public abstract string Description { get; }

        /// <summary>
        /// 전투 시작 시 효과를 적용한다.
        /// AccessoryManager가 호출. 대상 시스템의 속성을 직접 변경한다.
        /// </summary>
        /// <param name="deckSystem">카드 시스템 (드로우/리롤 관련 효과 적용 대상)</param>
        public abstract void Apply(ModifierManager modifierManager);

        /// <summary>
        /// 전투 종료 시 효과를 되돌린다.
        /// </summary>
        /// <param name="deckSystem">카드 시스템</param>
        public abstract void Remove(ModifierManager modifierManager);
    }
}
