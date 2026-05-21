using FFF.Data;

namespace FFF.Battle.Modifier
{
    // TO-DO : 핸드의 족보를 여기에서 특정 카테고리로 묶는게 아닌, SeotdaHand측에서 제공하도록 변경

    public enum HandCategory { Ddaeng, Kkeut, Special }

    /// <summary>
    /// 판정된 족보가 특정 범주(땡, 끗, 특수)에 속하는지 판별합니다.
    /// </summary>
    public class HandJokboCondition : IModifierCondition
    {
        private readonly HandCategory _targetCategory;

        public HandJokboCondition(HandCategory targetCategory)
        {
            _targetCategory = targetCategory;
        }

        public bool IsMet(ModifierContext context)
        {
            if (context?.ActionHandResult == null) return false;
            SeotdaHand hand = context.ActionHandResult.Value.Hand;

            return _targetCategory switch
            {
                HandCategory.Ddaeng => hand >= SeotdaHand.IlDdaeng && hand <= SeotdaHand.SipDdaeng || hand <= SeotdaHand.IlSamGwangDdaeng,
                HandCategory.Kkeut => hand >= SeotdaHand.IlKkeut && hand <= SeotdaHand.GuKkeut || hand == SeotdaHand.MangTong,
                HandCategory.Special => hand >= SeotdaHand.Ali && hand <= SeotdaHand.JangSa,
                _ => false
            };
        }
    }
}