using FFF.Battle.Modifier;

namespace FFF.Battle.Item.Joker
{
    /// <summary>
    /// 조커 샘플 2: 리롤 횟수를 한 턴만 4번 증가시킨다.
    /// 
    /// 사용 시 DeckSystem.AddTempRerolls(4) 호출.
    /// 이번 턴의 남은 리롤 횟수에 즉시 +4가 반영된다.
    /// 다음 턴 드로우 시 리롤 횟수는 원래 기본값으로 리셋되므로
    /// 별도 해제 로직이 필요 없다.
    /// </summary>
    public class RerollBurstJoker : JokerBase
    {
        public override string Id => "JKR_REROLL_BURST";
        public override string DisplayName => "운명의 주사위";
        public override string Description => "이번 턴에 한해 리롤 횟수가 4번 증가합니다.";

        private const int BONUS_REROLLS = 4;

        protected override void Activate(JokerContext context)
        {
            // Turn Modifier -> DeckSystem을 통해 현재 턴의 남은 리롤 횟수를 직접 증가.
            // 다음 턴 시작시 DeckSystem 내부에서 자동으로 턴수가 차감된다.
            var burstModifier = new TurnModifier(ModifierType.MaxRerolls, BONUS_REROLLS, 1);
            
            context.DeckSystem.AddModifier(burstModifier);
        }
    }
}
