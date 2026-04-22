namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 파이프라인에서 가로채어 수정할 값의 종류를 정의합니다.
    /// 기존 ModifierType을 대체하며, 역할이 훨씬 명확해집니다.
    /// </summary>
    public enum ModifierValueType
    {
        None = 0,

        // === 캐싱 대상 (Dirty Bit가 켜졌을 때만 연산하여 DeckSystem에 Push) ===
        MaxRerolls,    // 리롤 횟수 증감
        DrawCount,     // 드로우 장수 증감

        // === 실시간 연산 대상 (전투 중 ProcessValue를 통해 매번 파이프라인 통과) ===
        Strength,      // 공격력 (기본 족보 점수에 합산/곱산)
        Damage         // 최종 피해량 (방어, 증폭 등)
    }
}