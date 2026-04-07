namespace FFF.Battle.Item.Accessory
{
    /// <summary>
    /// 장신구 샘플 1: 리롤 횟수를 영구적으로 1회 증가시킨다.
    /// 
    /// 효과: DeckSystem의 보너스 리롤 횟수를 +1.
    /// 전투 시작 시 Apply → DeckSystem.AddBonusRerolls(1)
    /// 전투 종료 시 Remove → DeckSystem.AddBonusRerolls(-1)
    /// 
    /// DeckSystem은 누가 리롤 횟수를 올렸는지 알 필요 없다.
    /// </summary>
    public class RerollBonusAccessory : AccessoryBase
    {
        public override string Id => "ACC_REROLL_BONUS";
        public override string DisplayName => "행운의 주사위";
        public override string Description => "리롤 횟수가 영구적으로 1회 증가합니다.";

        private const int BONUS_REROLLS = 1;

        public override void Apply(Card.DeckSystem deckSystem)
        {
            deckSystem.AddBonusRerolls(BONUS_REROLLS);
        }

        public override void Remove(Card.DeckSystem deckSystem)
        {
            deckSystem.AddBonusRerolls(-BONUS_REROLLS);
        }
    }
}
