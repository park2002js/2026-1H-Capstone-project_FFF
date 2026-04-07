namespace FFF.Battle.Item.Joker
{
    /// <summary>
    /// 조커 카드의 추상 베이스 클래스.
    /// 
    /// ── 핵심 책임 ──
    /// 플레이어가 카드 선택 전에 능동적으로 사용하는 1회성 아이템.
    /// "사용해" → 알아서 효과 적용. 사용 후 소멸.
    /// 
    /// ── 설계 원칙 ──
    /// - JokerManager가 "사용해" 호출하면, 구체 조커가 알아서 효과를 적용한다.
    /// - 효과 적용 대상(DeckSystem, 데미지 계산 등)은 조커의 존재를 모른다.
    /// - 적용된 효과는 조커 종류에 따라 즉시/턴 종료 시 해제된다.
    /// </summary>
    public abstract class JokerBase
    {
        /// <summary>조커 고유 ID.</summary>
        public abstract string Id { get; }

        /// <summary>조커 표시 이름 (한글).</summary>
        public abstract string DisplayName { get; }

        /// <summary>조커 설명 텍스트.</summary>
        public abstract string Description { get; }

        /// <summary>사용 여부. true이면 이미 소멸됨.</summary>
        public bool IsUsed { get; private set; } = false;

        /// <summary>
        /// 조커를 사용한다. 1회 사용 후 소멸.
        /// JokerManager가 호출.
        /// </summary>
        /// <param name="context">효과 적용에 필요한 컨텍스트</param>
        /// <returns>사용 성공 여부</returns>
        public bool Use(JokerContext context)
        {
            if (IsUsed)
            {
                UnityEngine.Debug.LogWarning($"[JokerBase] 이미 사용된 조커: {DisplayName}");
                return false;
            }

            Activate(context);
            IsUsed = true;

            UnityEngine.Debug.Log($"[JokerBase] 조커 사용: {DisplayName} → 소멸");

            return true;
        }

        /// <summary>
        /// 구체 조커가 구현하는 효과 적용 로직.
        /// </summary>
        protected abstract void Activate(JokerContext context);
    }

    /// <summary>
    /// 조커 효과 적용에 필요한 컨텍스트.
    /// 조커는 이 컨텍스트를 통해 필요한 시스템에 접근한다.
    /// </summary>
    public class JokerContext
    {
        /// <summary>카드 시스템 (리롤 등 카드 관련 효과).</summary>
        public Card.DeckSystem DeckSystem { get; set; }

        /// <summary>조커 매니저 (데미지 배율 등 등록).</summary>
        public JokerManager JokerManager { get; set; }
    }
}
