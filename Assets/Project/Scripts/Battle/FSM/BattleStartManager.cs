using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Data;
using FFF.Battle.FSM;
using FFF.Battle.Card;
using FFF.Battle.Enemy;
using FFF.Battle.Item.Joker;
using FFF.Battle.Item.Accessory;
using FFF.Core;
using FFF.UI.Battle;
using FFF.UI.Animation;
using FFF.Core.Events;
using FFF.Data;
using FFF.Audio;
using FFF.Battle.Damage;

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
        [SerializeField] private EnemyVisualSelector _enemyVisualSelector;

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
                ApplyEnemyVisual();

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
                _battleUI.SetJokerClickHandler(HandleJokerClicked);
                _jokerManager?.SetJokersFromIds(player.HeldJokerIds);

                // 4. 적 데이터 연동 및 초기화
                // BattleContext에서 타겟 EnemyId 로드
                string targetEnemyId = BattleManager.Instance.Context.TargetEnemyId;
                // 데이터베이스에서 해당 ID를 가진 SO 파일을 로드
                EnemyDataSO enemySO = EnemyDatabase.FindById(targetEnemyId);
                // 해당 SO로 배틀 전용 객체를 초기화
                _enemyDataBattle.Initialize(enemySO);

                // 5. UI 초기화
                _battleUI.SetPlayerHealth(player.CurrentHealth, player.MaxHealth);
                _battleUI.SetPlayerGold(player.CurrentGold);
                _battleUI.SetDeckCards(player.DeckCardIds);
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

        private void ApplyEnemyVisual()
        {
            if (_enemyVisualSelector == null)
                _enemyVisualSelector = FindFirstObjectByType<EnemyVisualSelector>();

            if (_enemyVisualSelector == null)
            {
                Debug.LogWarning("[BattleStartManager] EnemyVisualSelector를 찾을 수 없어 기본 적 외형을 유지합니다.");
                return;
            }

            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
                return;

            string visualId = gameManager.SelectEnemyVisualId(_enemyVisualSelector.GetRegisteredIds());
            if (string.IsNullOrEmpty(visualId))
                return;

            if (!_enemyVisualSelector.TrySelect(visualId))
                Debug.LogWarning($"[BattleStartManager] 적 외형 적용 실패: {visualId}");
        }

        private void HandleJokerClicked(int jokerIndex, string jokerId)
        {
            BattleManager battleManager = BattleManager.Instance;
            if (battleManager == null || battleManager.Context?.PlayerData == null)
            {
                SoundManager.PlayUiSound(SoundIds.UiError);
                return;
            }

            if (!CanUseJokerInCurrentPhase(battleManager.CurrentPhase))
            {
                SoundManager.PlayUiSound(SoundIds.UiError);
                Debug.LogWarning($"[BattleStartManager] 현재 페이즈에서는 조커를 사용할 수 없습니다: {battleManager.CurrentPhase}");
                return;
            }

            PlayerDataBattle player = battleManager.Context.PlayerData;
            if (player.HeldJokerIds == null || jokerIndex < 0 || jokerIndex >= player.HeldJokerIds.Count)
            {
                SoundManager.PlayUiSound(SoundIds.UiError);
                Debug.LogWarning($"[BattleStartManager] 잘못된 조커 클릭 인덱스: {jokerIndex}");
                return;
            }

            if (!string.IsNullOrEmpty(jokerId) && player.HeldJokerIds[jokerIndex] != jokerId)
            {
                int correctedIndex = player.HeldJokerIds.IndexOf(jokerId);
                if (correctedIndex < 0)
                {
                    SoundManager.PlayUiSound(SoundIds.UiError);
                    Debug.LogWarning($"[BattleStartManager] 보유 중이 아닌 조커입니다: {jokerId}");
                    return;
                }

                jokerIndex = correctedIndex;
            }

            if (_jokerManager == null || !_jokerManager.UseJoker(jokerIndex))
            {
                SoundManager.PlayUiSound(SoundIds.UiError);
                return;
            }

            player.ConsumeJoker(player.HeldJokerIds[jokerIndex]);
            SoundManager.PlaySfxSound(SoundIds.SfxJokerActivate);

            _battleUI.SetupItemIcons(player.EquippedAccessoryIds, player.HeldJokerIds);
            _battleUI.UpdateRerollState(_deckSystem.RerollsRemaining, _deckSystem.SelectedCards.Count);
            RefreshExpectedStrengthIfNeeded(battleManager);
        }

        private static bool CanUseJokerInCurrentPhase(TurnState phase)
        {
            return phase == TurnState.TurnReady || phase == TurnState.TurnProceed;
        }

        private void RefreshExpectedStrengthIfNeeded(BattleManager battleManager)
        {
            if (battleManager.CurrentPhase != TurnState.TurnProceed || _deckSystem.SelectedCards.Count != 2)
                return;

            var calculator = new CombatCalculator();
            int expectedPower = calculator.Strength.CalculateExpectedStrength(
                _deckSystem.SelectedCards[0],
                _deckSystem.SelectedCards[1],
                battleManager.CurrentModifierContext);

            _battleUI.SetExpectedStrengthText(expectedPower.ToString());
        }
    }
}
