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
                case "ACC_BOONE":
                    return new Acc_Boone();

                // === 조커 (Joker) 매핑 ===
                case "JKR_GAKSI":
                    return new Jkr_Gaksi();

                default:
                    Debug.LogWarning($"[ItemFactory] 등록되지 않은 아이템 ID 생성 시도: {id}");
                    return null;
            }
        }
    }
}