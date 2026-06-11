using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 사용 효과: 1회에 한해 즉시 카드를 2장 드로우 함.
    /// </summary>
    public class Joker_006 : JokerItemBase
    {
        public override string Id => "Joker_006";
        protected override string SpriteFileName => "boone";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            FFF.Battle.Card.DeckSystem deck = UnityEngine.Object.FindFirstObjectByType<FFF.Battle.Card.DeckSystem>();
            if (deck != null)
            {
                // 즉발성 3장 드로우 실행
                deck.DrawCards(2);
                
                // 전투 UI에 시각적 동기화 적용
                var ui = UnityEngine.Object.FindFirstObjectByType<FFF.UI.Battle.BattleUIComponent>();
                if (ui != null)
                {
                    ui.UpdateHand(deck.Hand, _ => {}); 
                }
            }
        }
    }
}
