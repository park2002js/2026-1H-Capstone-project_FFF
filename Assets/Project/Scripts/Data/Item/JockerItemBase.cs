using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 조커 아이템의 베이스 클래스.
    /// 이미지 로드 기본 경로 지정 및 클릭 상호작용 인터페이스 구현.
    /// </summary>
    public abstract class JokerItemBase : ItemBase, IClickable
    {
        // 사용자가 요청한 철자 유지
        protected override string BaseFolderPath => "Assets/Project/Art/Jocker";

        public bool IsUsed { get; protected set; } = false;

        public bool Use(ModifierContext context, Action onConsume)
        {
            if (IsUsed) return false;

            Apply(context, onConsume);
            IsUsed = true;
            
            // 사용 완료 후 삭제 콜백 실행
            onConsume?.Invoke();
            
            return true;
        }

        // 효과 발동 로직은 하위 클래스에서 구체화 (현재는 보류)
        public override void Apply(ModifierContext context, Action onConsume = null) { }
        public override void Remove(ModifierContext context) { }
    }
}