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

            switch (id.ToUpperInvariant())
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
                case "ACCESSORY_006":
                    return new Accessory_006();
                case "ACCESSORY_007":
                    return new Accessory_007();
                case "ACCESSORY_008":
                    return new Accessory_008();
                case "ACCESSORY_009":
                    return new Accessory_009();
                case "ACCESSORY_010":
                    return new Accessory_010();
                case "ACCESSORY_011":
                    return new Accessory_011();
                case "ACCESSORY_012":
                    return new Accessory_012();
                case "ACCESSORY_013":
                    return new Accessory_013();
                case "ACCESSORY_014":
                    return new Accessory_014();
                case "ACCESSORY_015":
                    return new Accessory_015();
                case "ACCESSORY_016":
                    return new Accessory_016();
                case "ACCESSORY_017":
                    return new Accessory_017();
                case "ACCESSORY_018":
                    return new Accessory_018();
                case "ACCESSORY_019":
                    return new Accessory_019();
                case "ACCESSORY_020":
                    return new Accessory_020();

                // === 조커 (Joker) 매핑 ===
                case "JOKER_001":
                    return new Joker_001();
                case "JOKER_002":
                    return new Joker_002();
                case "JOKER_003":
                    return new Joker_003();
                case "JOKER_004":
                    return new Joker_004();
                case "JOKER_005":
                    return new Joker_005();
                case "JOKER_006":
                    return new Joker_006();
                case "JOKER_007":
                    return new Joker_007();
                case "JOKER_008":
                    return new Joker_008();

                default:
                    Debug.LogWarning($"[ItemFactory] 등록되지 않은 아이템 ID 생성 시도: {id}");
                    return null;
            }
        }
    }
}
