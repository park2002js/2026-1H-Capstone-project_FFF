using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.FSM;

namespace FFF.Battle.Item.Accessory
{
    /// <summary>
    /// 장착된 장신구들을 관리하고, 전투 시작/종료 시 효과를 자동 적용/해제한다.
    /// 
    /// ── 핵심 책임 ──
    /// "전투 시작됐다" 신호가 오면 → 가지고 있는 장신구들한테 "알아서 적용해" 호출.
    /// BattleManager는 이 매니저가 뭘 하는지 알 필요 없다.
    /// DeckSystem도 누가 자기 값을 바꿨는지 알 필요 없다.
    /// 
    /// ── 설계 원칙 ──
    /// - BattleManager.OnBattleStart에 자기를 등록한다.
    /// - 각 장신구의 구체 효과는 AccessoryBase 구현체가 알아서 처리한다.
    /// - 이 매니저는 "가지고 있는 장신구 목록을 순회하며 Apply/Remove 호출"만 한다.
    /// </summary>
    public class AccessoryManager : MonoBehaviour
    {
        [Header("=== 참조 ===")]
        [Tooltip("카드 시스템. 장신구 효과 적용 대상.")]
        [SerializeField] private Card.DeckSystem _deckSystem;

        /// <summary>현재 장착된 장신구 목록. 전투 외부(상점/맵)에서 세팅된다.</summary>
        private readonly List<AccessoryBase> _equippedAccessories = new();

        /// <summary>현재 장착된 장신구 목록. 읽기 전용.</summary>
        public IReadOnlyList<AccessoryBase> EquippedAccessories => _equippedAccessories;

        #region === 외부 호출: 장신구 장착/해제 (전투 외부에서) ===

        /// <summary>
        /// 장신구를 장착한다. 전투 시작 전(상점/맵)에서 호출.
        /// </summary>
        public void Equip(AccessoryBase accessory)
        {
            if (accessory == null) return;

            _equippedAccessories.Add(accessory);
            Debug.Log($"[AccessoryManager] 장신구 장착: {accessory.DisplayName}");
        }

        /// <summary>
        /// 장신구를 해제한다. 전투 시작 전(상점/맵)에서 호출.
        /// </summary>
        public void Unequip(AccessoryBase accessory)
        {
            if (accessory == null) return;

            if (_equippedAccessories.Remove(accessory))
            {
                Debug.Log($"[AccessoryManager] 장신구 해제: {accessory.DisplayName}");
            }
        }

        /// <summary>
        /// 전투 시작 시 호출됨 (BattleStartManager).
        /// 장착된 모든 장신구의 효과를 DeckSystem에 적용한다.
        /// 어떤 장신구가 어떤 효과를 주는지는 알빠노. 그냥 Apply 호출.
        /// </summary>
        public void ApplyAllAccessories(Card.DeckSystem deckSystem)
        {
            foreach (var accessory in _equippedAccessories)
            {
                accessory.Apply(deckSystem);
                Debug.Log($"[AccessoryManager] 장신구 효과 적용: {accessory.DisplayName}");
            }
        }

        #endregion

        #region === 내부: 전투 이벤트 핸들러 ===

        /// <summary>
        /// 전투 종료 시 호출. 모든 장신구 효과를 되돌린다.
        /// 추후 BattleManager에 OnBattleEnd 이벤트가 추가되면 구독하거나 명시적으로 호출됨.
        /// </summary>
        public void RemoveAllAccessories()
        {
            foreach (var accessory in _equippedAccessories)
            {
                accessory.Remove(_deckSystem);
            }

            Debug.Log($"[AccessoryManager] 모든 장신구 효과 해제. 총 {_equippedAccessories.Count}개");
        }

        #endregion
    }
}
