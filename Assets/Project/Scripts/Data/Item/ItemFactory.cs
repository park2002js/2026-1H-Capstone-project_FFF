using UnityEngine;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 전달받은 ID 문자열을 기반으로 순수 C# 로직 객체(ItemBase)를 생성하는 팩토리.
    /// 외부(Manager)는 구체적인 클래스 이름 없이 ID만으로 객체 생성.
    /// </summary>
    public static class ItemFactory
    {
        public static ItemBase CreateItem(string id)
        {
            // TODO: 실제 기믹 스크립트 작성 후 switch 문에 매핑 추가 필요
            if (string.IsNullOrEmpty(id)) return null;

            switch (id.ToUpper())
            {
                // === 장신구 (Accessory) 매핑 ===
                case "ACCESSORY_001":
                    return new Accessory_001();
                case "ACCESSORY_002":
                    return new Accessory_002();
                case "ACCESSORY_003":
                    return new Accessory_003();
                case "ACCESSORY_004":
                    return new Accessory_004();
                case "ACCESSORY_005":
                    return new Accessory_005();

                // === 조커 (Joker) 매핑 ===
                case "JOCKER_001":
                    return new Jocker_001();

                default:
                    Debug.LogWarning($"[ItemFactory] 등록되지 않은 아이템 ID 생성 시도: {id}");
                    return null;
            }
        }
    }
}