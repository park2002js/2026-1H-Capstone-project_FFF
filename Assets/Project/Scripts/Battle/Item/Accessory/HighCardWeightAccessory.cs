namespace FFF.Battle.Item.Accessory
{
    /// <summary>
    /// 장신구 샘플 2: 5 이상의 카드가 나올 확률을 영구적으로 10% 증가시킨다.
    /// 
    /// 효과: DeckSystem에 가중치 드로우 함수를 등록한다.
    /// 5~10월 카드의 가중치를 1.1로, 1~4월 카드의 가중치를 1.0으로 설정.
    /// 
    /// CardDrawHandler → CardPile.MoveDrawToHandWeighted()에서 이 가중치를 사용하여
    /// 높은 월 카드가 더 자주 뽑히도록 한다.
    /// DeckSystem은 가중치가 왜 걸려있는지 알 필요 없다.
    /// </summary>
    public class HighCardWeightAccessory : AccessoryBase
    {
        public override string Id => "ACC_HIGH_CARD_WEIGHT";
        public override string DisplayName => "고급 화투패";
        public override string Description => "5 이상의 카드가 나올 확률이 영구적으로 10% 증가합니다.";

        /// <summary>
        /// 5 이상 월 카드에 적용할 추가 가중치.
        /// 기본 가중치 1.0 대비 1.1 = 10% 확률 증가.
        /// </summary>
        private const float HIGH_CARD_WEIGHT = 1.1f;
        private const float NORMAL_WEIGHT = 1.0f;
        private const int HIGH_CARD_THRESHOLD = 5;

        public override void Apply(Card.DeckSystem deckSystem)
        {
            deckSystem.SetDrawWeightFunc(GetCardWeight);
        }

        public override void Remove(Card.DeckSystem deckSystem)
        {
            deckSystem.SetDrawWeightFunc(null);
        }

        /// <summary>
        /// 카드별 드로우 가중치를 반환한다.
        /// 5월 이상 카드는 1.1, 그 외는 1.0.
        /// </summary>
        private static float GetCardWeight(Data.HwaTuCard card)
        {
            int monthValue = card.GetMonthValue();

            if (monthValue >= HIGH_CARD_THRESHOLD)
            {
                return HIGH_CARD_WEIGHT;
            }

            return NORMAL_WEIGHT;
        }
    }
}
