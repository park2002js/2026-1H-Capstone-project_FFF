using UnityEngine;

namespace FFF.Data
{
    public enum ItemType { Accessory, Joker }

    /// <summary>
    /// 아이템의 정적 데이터(이름, 아이콘, 가격 등) 보관용 ScriptableObject 데이터 컨테이너.
    /// 에디터 할당 및 상점, UI 표기용으로 사용. 실제 로직은 배제.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "FFF/Data/Item Data")]
    public class ItemDataSO : ScriptableObject
    {
        [Tooltip("아이템 고유 ID (ItemFactory와 매핑)")]
        public string Id;
        
        [Tooltip("아이템 분류 (장신구 / 조커)")]
        public ItemType Type;
        
        [Tooltip("인게임 UI 표기 이름")]
        public string DisplayName;
        
        [Tooltip("아이템 효과 설명")]
        [TextArea] public string Description;
        
        [Tooltip("상점 및 전투 UI 표기 아이콘")]
        public Sprite Icon;
        
        [Tooltip("상점 거래 가격")]
        public int Price;
    }
}