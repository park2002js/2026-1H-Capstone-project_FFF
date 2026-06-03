using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 사용 효과: 이번 턴에 한해 공격력 상수 +50 증가.
    /// </summary>
    public class Jocker_001 : JokerItemBase
    {
        public override string Id => "Jocker_001"; // 실제 이름과 ID는 다름
        protected override string SpriteFileName => "gaksi";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            // 공격력 +50 상수 연산 효과 부품 생성 (기존 StrengthConstantEffect 활용)
            IModifierEffect strengthEffect = new StrengthConstantEffect(50);

            // 1턴(이번 턴)만 유지되는 단발성 BattleModifier 조립
            BattleModifier modifier = new BattleModifier(
                id: $"{Id}_StrengthBoost",
                targetType: ModifierValueType.Strength,
                condition: new AlwaysTrueCondition(),
                effect: strengthEffect,
                turns: 1 
            );

            // ModifierManager 파이프라인에 효과 등록
            ModifierManager.Instance.AddModifier(modifier);
        }
    }

    public sealed class RerollBurstJokerItem : JokerItemBase
    {
        public const string JokerId = "JKR_REROLL_BURST";

        public override string Id => JokerId;
        protected override string SpriteFileName => "boone";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            ModifierManager.Instance.AddModifier(new BattleModifier(
                $"{Id}_MaxRerolls",
                ModifierValueType.MaxRerolls,
                new AlwaysTrueCondition(),
                new ExtraRerollCountEffect(4),
                turns: 1));
        }
    }

    public sealed class HighCardJokerItem : JokerItemBase
    {
        public const string JokerId = "JKR_HIGH_CARD";

        public override string Id => JokerId;
        protected override string SpriteFileName => "yangban";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            ModifierManager.Instance.AddModifier(new BattleModifier(
                $"{Id}_Strength",
                ModifierValueType.Strength,
                new HandIncludesCardTypeCondition(CardType.Gwang),
                new StrengthConstantEffect(60),
                turns: 1));
        }
    }

    public sealed class DoublePipJokerItem : JokerItemBase
    {
        public const string JokerId = "JKR_DOUBLE_PIP";

        public override string Id => JokerId;
        protected override string SpriteFileName => "mokjoong";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            ModifierManager.Instance.AddModifier(new BattleModifier(
                $"{Id}_Strength",
                ModifierValueType.Strength,
                new HandCardTypeCountCondition(CardType.Pi, 2),
                new StrengthConstantEffect(45),
                turns: 1));
        }
    }

    public sealed class LuckyCharmJokerItem : JokerItemBase
    {
        public const string JokerId = "JKR_LUCKY_CHARM";

        public override string Id => JokerId;
        protected override string SpriteFileName => "gaksi";

        public override void Apply(ModifierContext context, Action onConsume = null)
        {
            ModifierManager.Instance.AddModifier(new BattleModifier(
                $"{Id}_Strength",
                ModifierValueType.Strength,
                new AlwaysTrueCondition(),
                new StrengthConstantEffect(25),
                turns: 1));
        }
    }

    internal sealed class HandIncludesCardTypeCondition : IModifierCondition
    {
        private readonly CardType _cardType;

        public HandIncludesCardTypeCondition(CardType cardType)
        {
            _cardType = cardType;
        }

        public bool IsMet(ModifierContext context)
        {
            if (context?.ActionHandResult == null)
                return false;

            SeotdaResult result = context.ActionHandResult.Value;
            return result.Card1.Type == _cardType || result.Card2.Type == _cardType;
        }
    }

    internal sealed class HandCardTypeCountCondition : IModifierCondition
    {
        private readonly CardType _cardType;
        private readonly int _requiredCount;

        public HandCardTypeCountCondition(CardType cardType, int requiredCount)
        {
            _cardType = cardType;
            _requiredCount = Math.Max(1, requiredCount);
        }

        public bool IsMet(ModifierContext context)
        {
            if (context?.ActionHandResult == null)
                return false;

            SeotdaResult result = context.ActionHandResult.Value;
            int count = 0;
            if (result.Card1.Type == _cardType)
                count++;
            if (result.Card2.Type == _cardType)
                count++;

            return count >= _requiredCount;
        }
    }
}
