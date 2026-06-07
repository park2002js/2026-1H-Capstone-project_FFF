using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 사용 효과: 1회에 한해 즉시 카드를 3장 드로우 함.
    /// </summary>
    public class Jocker_002 : JokerItemBase
    {
        public override string Id => "Jocker_002";
        // public override string DisplayName => "드로우 조커";
        // public override string Description => "사용 즉시 카드를 3장 뽑습니다.";
        protected override string SpriteFileName => "boone";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            FFF.Battle.Card.DeckSystem deck = UnityEngine.Object.FindFirstObjectByType<FFF.Battle.Card.DeckSystem>();
            if (deck != null)
            {
                // 즉발성 3장 드로우 실행
                deck.DrawCards(3);
                
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
