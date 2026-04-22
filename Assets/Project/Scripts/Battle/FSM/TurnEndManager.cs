using UnityEngine;
using FFF.Battle.FSM;
using FFF.Battle.Card;
using FFF.UI.Battle;
using FFF.Battle.Damage;
using FFF.Data;
using FFF.Battle.Enemy;
using FFF.Core.Events;
using FFF.Battle.Modifier;

namespace FFF.Battle.Managers
{
    public class TurnEndManager : MonoBehaviour
    {
        [Header("=== 시스템 참조 ===")]
        [SerializeField] private BattleManager _battleManager;
        [SerializeField] private DeckSystem _deckSystem;
        [SerializeField] private ModifierManager _modifierManager;
        [SerializeField] private EnemyData _enemyData;
        [SerializeField] private BattleUIComponent _battleUI;

        [Header("=== 수신할 이벤트 ===")]
        [SerializeField] private GameEvent _onTurnEndEvent;

        private CombatCalculator _combatCalculator;

        private void Awake()
        {
            // 순수 C# 계산기 인스턴스 생성
            _combatCalculator = new CombatCalculator();
        }

        private void OnEnable()
        {
            if (_onTurnEndEvent != null) _onTurnEndEvent.Subscribe(HandleTurnEndEnter);
        }

        private void OnDisable()
        {
            if (_onTurnEndEvent != null) _onTurnEndEvent.Unsubscribe(HandleTurnEndEnter);
        }

        private void HandleTurnEndEnter()
        {
            Debug.Log("========== [TurnEnd] 결산 페이즈 진입 ==========");
            _battleUI.SetTurnProceedUIVisibility(false);

            // 1. 양측의 기본 공격력 산출
            int playerStrength = GetPlayerStrength();
            int enemyStrength = _enemyData.CurrentIntent.BasePower;

            Debug.Log($"[TurnEnd] ⚔️ 플레이어 공격력({playerStrength}) vs 적 공격력({enemyStrength})");

            // 2. 승자 판정 및 데미지 적용 (핵심 로직)
            ApplyCombatResult(playerStrength, enemyStrength);

            // 3. UI 체력 갱신
            _battleUI.SetPlayerHealth(PlayerData.Instance.CurrentHealth, PlayerData.Instance.MaxHealth);
            _battleUI.SetEnemyHealth(_enemyData.CurrentHealth, _enemyData.MaxHealth);

            // 4. 턴 정리
            _deckSystem.CleanupForNextTurn(); // 카드 무덤으로 보내기

            // 5. 턴 종료에 따른 Modifier들의 수명 업데이트
            _modifierManager.TickModifiers(); // 버프 수명 차감 및 파기

            // 6. 카드 폐기 연출 → 완료 후 전환 판정
            //    ClearHandUI가 콜백을 받으므로, 연출이 끝나야 다음 턴으로 넘어간다.
            _battleUI.ClearHandUI(onComplete: () =>
            {
                CheckDeathAndTransition();
            });
        }

        private int GetPlayerStrength()
        {
            var selected = _deckSystem.SelectedCards;
            if (selected.Count == 2)
            {
                // 매니저와 배달통을 넘김
                return _combatCalculator.Strength.CalculateExpectedStrength(
                    selected[0], 
                    selected[1], 
                    _modifierManager, 
                    _battleManager.CurrentModifierContext
                );
            }
            return 0;
        }

        /// <summary>
        /// 전투 결과를 비교하고, 승자가 계산기를 통해 산출된 최종 데미지로 패자를 타격합니다.
        /// </summary>
        private void ApplyCombatResult(int playerStrength, int enemyStrength)
        {
            if (playerStrength > enemyStrength)
            {
                // 플레이어 승리: 플레이어의 데미지 파이프라인 가동
                int finalDamage = _combatCalculator.Damage.CalculateFinalDamage(
                    playerStrength, 
                    _modifierManager, 
                    _battleManager.CurrentModifierContext
                );
                _enemyData.TakeDamage(finalDamage);
                Debug.Log($"[TurnEnd] 💥 플레이어 승리! 적에게 {finalDamage}의 최종 피해를 입혔습니다.");
            }
            else if (enemyStrength > playerStrength)
            {
                // 적 승리: 적이 플레이어를 때릴 때도 플레이어의 파이프라인(방어력 등) 가동
                int finalDamage = _combatCalculator.Damage.CalculateFinalDamage(
                    enemyStrength, 
                    _modifierManager, 
                    _battleManager.CurrentModifierContext
                );
                PlayerData.Instance.TakeDamage(finalDamage);
                Debug.Log($"[TurnEnd] 🩸 적 승리! 플레이어가 {finalDamage}의 최종 피해를 입었습니다.");
            }
            else
            {
                Debug.Log("[TurnEnd] 🛡️ 무승부! 양측 모두 피해를 입지 않았습니다.");
            }
        }

        private void CheckDeathAndTransition()
        {
            bool isPlayerDead = PlayerData.Instance.CurrentHealth <= 0;
            bool isEnemyDead = _enemyData.CurrentHealth <= 0;

            if (isPlayerDead || isEnemyDead)
            {
                _battleManager.Context.IsPlayerWinner = isEnemyDead && !isPlayerDead;
                Debug.Log($"[TurnEnd] 누군가 쓰러졌습니다! (PlayerDead:{isPlayerDead}, EnemyDead:{isEnemyDead}) -> BattleEnd로 전환.");
                _battleManager.EndBattle();
            }
            else
            {
                Debug.Log("[TurnEnd] 양측 모두 생존. 다음 턴(TurnReady)으로 넘어갑니다.");
                _battleManager.ChangeState(TurnState.TurnReady);
            }
        }
    }
}
