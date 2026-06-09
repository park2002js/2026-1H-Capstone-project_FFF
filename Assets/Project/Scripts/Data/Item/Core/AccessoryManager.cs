using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Modifier;

// ItemFactoy 및 ItemBase 기반 코드
namespace FFF.Data
{
    /// <summary>
    /// 장신구 객체 생성 및 전투 시작/종료 시의 영구 버프 일괄 적용/해제 관리자.
    /// </summary>
    public class AccessoryManager : MonoBehaviour
    {
        [Header("=== 참조 ===")]
        [Tooltip("장신구 효과 적용 및 해제를 위한 참조")]
        [SerializeField] private ModifierManager _modifierManager;

        /// <summary>현재 장착된 장신구 C# 로직 객체 목록</summary>
        private readonly List<ItemBase> _equippedAccessories = new();
        public IReadOnlyList<ItemBase> EquippedAccessories => _equippedAccessories;

        /// <summary>
        /// 전투 시작 시 ID 리스트를 기반으로 장신구 객체 생성.
        /// </summary>
        public void Initialize(List<string> accessoryIds)
        {
            _equippedAccessories.Clear();
            foreach (var id in accessoryIds)
            {
                var item = ItemFactory.CreateItem(id);
                if (item != null)
                {
                    _equippedAccessories.Add(item);
                }
            }
            Debug.Log($"[AccessoryManager] 장신구 {accessoryIds.Count}개 로드 및 생성 완료.");
        }

        /// <summary>
        /// 전투 시작 시 모든 장신구 효과를 Modifier 파이프라인에 일괄 등록.
        /// </summary>
        public void ApplyAllAccessories(ModifierContext context)
        {
            foreach (var accessory in _equippedAccessories)
            {
                accessory.Apply(context);
                Debug.Log($"[AccessoryManager] 장신구 효과 적용: {accessory.Id}");
            }
        }

        /// <summary>
        /// 전투 종료 시 모든 장신구 효과를 Modifier 파이프라인에서 일괄 제거.
        /// </summary>
        public void RemoveAllAccessories(ModifierContext context)
        {
            foreach (var accessory in _equippedAccessories)
            {
                accessory.Remove(context);
            }
            Debug.Log($"[AccessoryManager] 모든 장신구 효과 해제.");
        }
    }
}