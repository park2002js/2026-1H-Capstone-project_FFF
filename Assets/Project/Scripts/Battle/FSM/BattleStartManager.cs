using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Card;
using FFF.Battle.Enemy;
using FFF.Battle.Item.Joker;
using FFF.Battle.Item.Accessory;
using FFF.UI.Battle;
using FFF.Core.Events;
using FFF.Data;

namespace FFF.Battle.Managers
{
    /// <summary>
    /// 전투 시작 전, 1회성 초기화를 담당하는 지휘자.
    /// 
    /// ── 핵심 책임 ──
    /// BattleManager의 OnBattleStart 이벤트를 구독하여,
    /// 덱 세팅, 장신구(영구 버프) 적용 등 전투에 필요한 모든 초기 준비를 마친다.
    /// </summary>
    public class BattleStartManager : MonoBehaviour
    {
        [Header("=== 시스템 참조 ===")]
        [SerializeField] private DeckSystem _deckSystem;
        [SerializeField] private AccessoryManager _accessoryManager;
        [SerializeField] private JokerManager _jokerManager;
        [SerializeField] private EnemyData _enemyData;
        [SerializeField] private BattleUIComponent _battleUI;

        [Header("=== 수신할 이벤트 ===")]
        [Tooltip("BattleManager가 방송하는 BattleStart 이벤트")]
        [SerializeField] private GameEvent _onBattleStartEvent;

        private void OnEnable()
        {
            if (_onBattleStartEvent != null)
                _onBattleStartEvent.Subscribe(HandleBattleStart);
        }

        private void OnDisable()
        {
            if (_onBattleStartEvent != null)
                _onBattleStartEvent.Unsubscribe(HandleBattleStart);
        }

        /// <summary>
        /// BattleManager가 StartBattle()을 호출할 때 1회 실행됨.
        /// (이후 BattleManager가 알아서 TurnReady 상태로 넘김)
        /// </summary>
        private void HandleBattleStart()
        {
            Debug.Log("[BattleStartManager] 전투 초기화 시작...");
            // 1. 플레이어 데이터 로드
            PlayerData player = PlayerData.Instance;

            // 2. 덱 시스템 초기화 (시연용으로 전체 카드 로드)
            // 현재: Resources 폴더의 모든 카드(20장)를 로드.
            // 미래: GameManager나 SaveData에서 '현재 플레이어가 보유한 덱'을 가져오도록 수정.
            List<HwaTuCard> playerDeck = HwaTuCardDatabase.CreateAllCards();

            // 3. DeckSystem 초기화 (시드값을 고정하고 싶다면 두 번째 인자로 전달)
            _deckSystem.Initialize(playerDeck);

            // 4. 아이템 세팅
            // 실제 구현 시에는 player.EquippedAccessoryIds 를 순회하며 팩토리에서 찍어냅니다.
            _accessoryManager.Equip(new RerollBonusAccessory());
            _jokerManager.AddJoker(new RerollBurstJoker());

            // 장신구(영구 버프) 일괄 적용
            // 전투가 시작될 때 플레이어가 장착 중인 장신구의 모디파이어를 DeckSystem에 등록합니다.
            if (_accessoryManager != null)
            {
                _accessoryManager.ApplyAllAccessories(_deckSystem);
            }

            _battleUI.Show();

            // 5. UI 초기화
            _battleUI.SetPlayerHealth(player.CurrentHealth, player.MaxHealth);
            _battleUI.SetEnemyHealth(_enemyData.CurrentHealth, _enemyData.MaxHealth);
            _battleUI.SetupItemIcons(player.EquippedAccessoryIds, player.HeldJokerIds);

            Debug.Log("[BattleStartManager] 전투 초기화 완료. 턴 시작 준비 끝!");
        }
    }
}