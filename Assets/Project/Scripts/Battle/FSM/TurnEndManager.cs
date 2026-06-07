using System.Collections;
using UnityEngine;
using FFF.Battle.Data;
using FFF.Battle.FSM;
using FFF.Battle.Card;
using FFF.Battle.Enemy;
using FFF.Battle.Modifier;
using FFF.Battle.Damage;
using FFF.Core.Events;
using FFF.UI.Battle;
using FFF.Data;

namespace FFF.Battle.Managers
{
    public class TurnEndManager : MonoBehaviour
    {
        private const float AttackImpactDelaySeconds = 0.55f;
        private const float AttackSequenceDurationSeconds = 2f;
        private const float DrawResultDelaySeconds = 0.45f;

        private readonly struct CombatDamageResult
        {
            public readonly bool HasWinner;
            public readonly bool DamagesEnemy;
            public readonly int Damage;

            public CombatDamageResult(bool hasWinner, bool damagesEnemy, int damage)
            {
                HasWinner = hasWinner;
                DamagesEnemy = damagesEnemy;
                Damage = damage;
            }
        }

        [Header("=== 시스템 참조 ===")]
        [SerializeField] private BattleManager _battleManager;
        [SerializeField] private DeckSystem _deckSystem;
        [SerializeField] private ModifierManager _modifierManager;
        [SerializeField] private EnemyDataBattle _enemyDataBattle;
        [SerializeField] private BattleUIComponent _battleUI;

        [Header("=== 수신할 이벤트 ===")]
        [SerializeField] private GameEvent _onTurnEndEvent;

        [Header("=== 발행 이벤트 (애니메이션 트리거) ===")]
        [Tooltip("플레이어 공격 시 BattleAnimationController가 구독")]
        [SerializeField] private IntEvent _onPlayerAttack;
        [Tooltip("적 공격 시 BattleAnimationController가 구독")]
        [SerializeField] private IntEvent _onEnemyAttack;

        private CombatCalculator _combatCalculator;
        private bool _isResolvingTurnEnd;

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
            if (_isResolvingTurnEnd)
                return;

            StartCoroutine(RunTurnEndFlow());
        }

        private IEnumerator RunTurnEndFlow()
        {
            _isResolvingTurnEnd = true;
            Debug.Log("========== [TurnEnd] Resolve flow start ==========");
            _battleUI.SetTurnProceedUIVisibility(false);

            int playerStrength = GetPlayerStrength();
            int enemyStrength = _enemyDataBattle.CurrentIntent.BasePower;
            int outcome = playerStrength.CompareTo(enemyStrength);

            Debug.Log($"[TurnEnd] Player power({playerStrength}) vs Enemy power({enemyStrength})");

            if (_battleUI != null)
            {
                yield return _battleUI.PlayCombatReveal(
                    _deckSystem.SelectedCards,
                    GetPlayerHandName(),
                    playerStrength,
                    _enemyDataBattle.CurrentIntent,
                    outcome);
            }

            CombatDamageResult damageResult = CalculateCombatDamage(playerStrength, enemyStrength);

            if (damageResult.HasWinner)
            {
                RaiseAttackEventSafely(damageResult);
                yield return new WaitForSeconds(AttackImpactDelaySeconds);
                ApplyCombatDamage(damageResult);
            }
            else
            {
                Debug.Log("[TurnEnd] 🛡️ 무승부! 양측 모두 피해를 입지 않았습니다.");
            }

            PlayerDataBattle player = _battleManager.Context.PlayerData;
            _battleUI.SetPlayerHealth(player.CurrentHealth, player.MaxHealth);
            _battleUI.SetEnemyHealth(_enemyDataBattle.CurrentHealth, _enemyDataBattle.MaxHealth);

            float postDamageDelay = damageResult.HasWinner
                ? Mathf.Max(0f, AttackSequenceDurationSeconds - AttackImpactDelaySeconds)
                : DrawResultDelaySeconds;
            yield return new WaitForSeconds(postDamageDelay);

            _deckSystem.CleanupForNextTurn();
            _battleUI.SetPileCounts(_deckSystem.DrawPile.Count, _deckSystem.DiscardPile.Count);

            _modifierManager.TickModifiers();

            _battleUI.ClearHandUI(onComplete: () =>
            {
                CheckDeathAndTransition();
                _isResolvingTurnEnd = false;
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
                    _battleManager.CurrentModifierContext
                );
            }
            return 0;
        }

        private string GetPlayerHandName()
        {
            var selected = _deckSystem.SelectedCards;
            if (selected.Count != 2)
                return "선택 없음";

            return SeotdaJudge.Judge(selected[0], selected[1]).DisplayName;
        }

        /// <summary>
        /// 전투 결과를 비교하고, 승자가 계산기를 통해 산출된 최종 데미지로 패자를 타격합니다.
        /// </summary>
        private CombatDamageResult CalculateCombatDamage(int playerStrength, int enemyStrength)
        {
            if (playerStrength > enemyStrength)
            {
                // 플레이어 타격 상태 기록
                _battleManager.CurrentModifierContext.IsPlayerAttacking = true;

                int finalDamage = _combatCalculator.Damage.CalculateFinalDamage(
                    playerStrength,
                    enemyStrength,
                    _battleManager.CurrentModifierContext
                );
                return new CombatDamageResult(true, true, finalDamage);
            }

            if (enemyStrength > playerStrength)
            {
                // 적 타격(플레이어 피격) 상태 기록
                _battleManager.CurrentModifierContext.IsPlayerAttacking = false;
                
                int finalDamage = _combatCalculator.Damage.CalculateFinalDamage(
                    enemyStrength,
                    playerStrength,
                    _battleManager.CurrentModifierContext
                );
                return new CombatDamageResult(true, false, finalDamage);
            }

            return new CombatDamageResult(false, false, 0);
        }

        private void RaiseAttackEventSafely(CombatDamageResult result)
        {
            try
            {
                if (result.DamagesEnemy)
                    _onPlayerAttack?.Raise(result.Damage);
                else
                    _onEnemyAttack?.Raise(result.Damage);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TurnEnd] 공격 연출 이벤트 처리 중 오류가 발생했습니다. 피해 적용은 계속 진행합니다.\n{ex}");
            }
        }

        private void ApplyCombatDamage(CombatDamageResult result)
        {
            if (!result.HasWinner)
                return;

            if (result.DamagesEnemy)
            {
                _enemyDataBattle.TakeDamage(result.Damage);
                Debug.Log($"[TurnEnd] 💥 플레이어 승리! 적에게 {result.Damage}의 최종 피해를 입혔습니다. Enemy HP: {_enemyDataBattle.CurrentHealth}/{_enemyDataBattle.MaxHealth}");
                return;
            }

            PlayerDataBattle player = _battleManager.Context.PlayerData;
            player.TakeDamage(result.Damage);
            Debug.Log($"[TurnEnd] 🩸 적 승리! 플레이어가 {result.Damage}의 최종 피해를 입었습니다. Player HP: {player.CurrentHealth}/{player.MaxHealth}");
        }

        private void CheckDeathAndTransition()
        {
            PlayerDataBattle player = _battleManager.Context.PlayerData;

            bool isPlayerDead = player.CurrentHealth <= 0;
            bool isEnemyDead = _enemyDataBattle.CurrentHealth <= 0;

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
