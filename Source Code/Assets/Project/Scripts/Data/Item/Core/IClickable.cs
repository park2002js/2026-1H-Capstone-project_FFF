using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// UI를 통해 플레이어가 직접 클릭 및 상호작용 가능한 아이템(조커) 판별용 인터페이스.
    /// </summary>
    public interface IClickable
    {
        /// <summary>
        /// 아이템 사용 시도 및 처리.
        /// 내부적으로 Apply 함수를 호출하여 Modifier 시스템에 로직을 위임.
        /// </summary>
        /// <param name="context">현재 전투 컨텍스트 (효과 적용용)</param>
        /// <param name="onConsume">사용 성공 시 Manager에서 아이템을 삭제하기 위한 콜백</param>
        /// <returns>사용 성공 여부</returns>
        bool Use(ModifierContext context, Action onConsume);
    }
}