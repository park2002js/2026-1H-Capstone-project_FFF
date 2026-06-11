using System;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 장신구 아이템의 베이스 클래스.
    /// 이미지 로드 기본 경로를 지정함.
    /// </summary>
    public abstract class AccessoryItemBase : ItemBase
    {
        protected override string BaseFolderPath => "Assets/Project/Art/Accessories";

        // 효과 발동 로직은 하위 클래스에서 구체화 (현재는 보류)
        public override void Apply(ModifierContext context, Action onConsume = null) { }
        public override void Remove(ModifierContext context) { }
    }
}