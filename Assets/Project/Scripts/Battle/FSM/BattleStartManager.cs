using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Data;
using FFF.Battle.FSM;
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
        [SerializeField] private EnemyDataBattle _enemyDataBattle;
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
            try{
                Debug.Log("[BattleStartManager] 전투 초기화 시작...");

                // 1. 플레이어 데이터 로드
                // BattleManager의 Context에 있는 로컬 데이터를 가져옴
                PlayerDataBattle player = BattleManager.Instance.Context.PlayerData;

                // 2. 플레이어가 보유한 덱 ID 목록을 카드 SO 원본에서 복사해 전투용 덱으로 만든다.
                // 같은 CardId가 여러 번 들어있으면 같은 카드가 여러 장 생성된다.
                List<HwaTuCard> playerDeck = HwaTuCardDatabase.CreateCardsFromIds(player.DeckCardIds);
                if (playerDeck.Count == 0)
                {
                    Debug.LogWarning("[BattleStartManager] 플레이어 덱이 비어 있어 기본 1~10월 초기 덱을 사용합니다.");
                    playerDeck = HwaTuCardDatabase.CreateDefaultInitialDeck();
                }

                // 3. DeckSystem 초기화 (시드값을 고정하고 싶다면 두 번째 인자로 전달)
                _deckSystem.Initialize(playerDeck);
                _battleUI.Show();

                // 4. 적 데이터 연동 및 초기화
                // BattleContext에서 타겟 EnemyId 로드
                string targetEnemyId = BattleManager.Instance.Context.TargetEnemyId;
                // 데이터베이스에서 해당 ID를 가진 SO 파일을 로드
                EnemyDataSO enemySO = EnemyDatabase.FindById(targetEnemyId);
                // 해당 SO로 배틀 전용 객체를 초기화
                _enemyDataBattle.Initialize(enemySO);

                // 5. UI 초기화
                _battleUI.SetPlayerHealth(player.CurrentHealth, player.MaxHealth);
                _battleUI.SetEnemyHealth(_enemyDataBattle.CurrentHealth, _enemyDataBattle.MaxHealth);
                _battleUI.SetupItemIcons(player.EquippedAccessoryIds, player.HeldJokerIds);
                _battleUI.SetPileCounts(_deckSystem.DrawPile.Count, _deckSystem.DiscardPile.Count);

                Debug.Log("[BattleStartManager] 전투 초기화 완료. 턴 시작 준비 끝!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BattleStartManager] 초기화 중 치명적 에러 발생 (여기서 중단됨!): {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
