using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using FFF.Battle.Card;
using FFF.Battle.FSM;
using FFF.Core.Events;
using FFF.Battle.Enemy;
using FFF.UI.Battle;
using FFF.Data;
using FFF.Audio;

namespace FFF.Battle.Managers
{
    /// <summary>
    /// 턴의 일련의 흐름을 비동기(Async)로 지휘하는 중간 관리자.
    /// 
    /// ── 핵심 책임 ──
    /// BattleManager의 OnTurnReady 이벤트를 구독하여,
    /// [턴 횟수 초기화 → 드로우 → 유저 리롤 대기 → 다음 페이즈 전환] 의
    /// 순서가 보장된 흐름을 제어한다.
    /// </summary>
    public class TurnReadyManager : MonoBehaviour
    {
        [Header("=== 시스템 참조 ===")]
        [SerializeField] private BattleManager _battleManager;
        [SerializeField] private DeckSystem _deckSystem;
        [SerializeField] private EnemyData _enemyData;
        [SerializeField] private BattleUIComponent _battleUI;

        [Header("=== 수신할 이벤트 ===")]
        [Tooltip("BattleManager가 방송하는 TurnReady 이벤트")]
        [SerializeField] private GameEvent _onTurnReadyEvent;

        // UI 대기를 위한 비동기 신호기
        private TaskCompletionSource<bool> _mulligan;

        private void OnEnable()
        {
            if (_onTurnReadyEvent != null)
                _onTurnReadyEvent.Subscribe(HandleTurnReady);
        }

        private void OnDisable()
        {
            if (_onTurnReadyEvent != null)
                _onTurnReadyEvent.Unsubscribe(HandleTurnReady);
        }

        /// <summary>
        /// BattleManager가 TurnReady 상태가 되었을 때 호출됨.
        /// </summary>
        private void HandleTurnReady()
        {
            // 턴 번호 증가 : BattleManager에 있는 배달통 갱신
            _battleManager.CurrentModifierContext.CurrentTurnNumber++;
            
            int currentTurn = _battleManager.CurrentModifierContext.CurrentTurnNumber;
            Debug.Log($"=== {currentTurn} 턴 시작 ===");

            // 비동기 흐름 시작 (Fire and Forget)
            _ = RunTurnStartFlowAsync();
        }

        private async Task RunTurnStartFlowAsync()
        {
            try {
                // 멀리건 UI 활성화
                _battleUI.SetTurnReadyUIVisibility(true);

                Debug.Log("[TurnReadyManager] 0. 적 의도 파악 및 표시");
                _enemyData.GenerateMockIntent();
                _battleUI.ShowEnemyIntent(_enemyData.CurrentIntent);

                Debug.Log("[TurnPhaseManager] 1. 턴 시작 준비 및 드로우");
                _deckSystem.OnTurnStarted();
                _deckSystem.DrawCards();

                _deckSystem.SetMaxSelectionLimit(5); // 멀리건 페이즈 이므로

                // UI에 카드 띄우기 (클릭 콜백 연결)
                _battleUI.UpdateHand(_deckSystem.Hand, OnCardClickedInUI);
                _battleUI.UpdateRerollState(_deckSystem.RerollsRemaining, _deckSystem.SelectedCards.Count);

                Debug.Log("[TurnPhaseManager] 2. 유저 멀리건 대기 시작...");
                
                // TaskCompletionSource를 초기화
                // TaskCompletionSource -> 누군가 결과를 넣어줄 때까지 Task를 멈추고 기다리게 만듦 (코루틴보다 깔끔)
                _mulligan = new TaskCompletionSource<bool>();
                await _mulligan.Task;

                // 남아있는 선택 카드들을 모두 선택 해제(Hand로 복귀)
                _deckSystem.DeselectAllCards();
                // UI를 새로고침 -> 선택된 캬드들의 강조효과 제거 (현재의 크기가 커짐 피드백에 한해서만)
                _battleUI.UpdateHand(_deckSystem.Hand, OnCardClickedInUI);

                // 멀리건 전용 UI 숨기기
                _battleUI.SetTurnReadyUIVisibility(false);

                _deckSystem.SetMaxSelectionLimit(2);

                Debug.Log("[TurnPhaseManager] 3. 멀리건 종료. 메인 페이즈로 전환합니다.");
                _battleManager.ChangeState(TurnState.TurnProceed);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TurnReadyManager] 비동기 흐름 중 치명적 에러 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // UI에서 특정 카드를 클릭했을 때 실행됨
        private void OnCardClickedInUI(CardUIComponent cardUI)
        {
            var card = cardUI.CardData;

            // 이미 선택된 카드면 해제, 아니면 선택
            if (_deckSystem.SelectedCards.Contains(card))
            {
                _deckSystem.DeselectCard(card);
                cardUI.SetSelected(false); // 크기 1.0으로 원복
            }
            else
            {
                bool isSuccess = _deckSystem.SelectCard(card);
                if (isSuccess)
                {
                    cardUI.SetSelected(true);  // 크기 1.1로 뻥튀기
                }
                else
                {
                    // 제한(2장)에 걸려 false가 반환되었다면 아무런 시각적 변화도 주지 않습니다.
                    Debug.LogWarning("[UI 방어] 최대 선택 개수를 초과하여 카드를 선택할 수 없습니다.");
                }
            }

            // 리롤 버튼 상태 갱신
            _battleUI.UpdateRerollState(_deckSystem.RerollsRemaining, _deckSystem.SelectedCards.Count);
        }

        // UI의 '리롤' 버튼에서 직접 호출하도록 연결할 public 함수
        public void OnRerollButtonClicked()
        {
            var selected = _deckSystem.SelectedCards.ToList();
            if (selected.Count == 0) return;
            SoundManager.PlayDefaultUiClick();

            Debug.Log($"[TurnReadyManager] 리롤 진행 ({selected.Count}장)");
            var redrawn = _deckSystem.Reroll(selected);

            if (redrawn.Count > 0)
            {
                // 리롤에 성공했으니 UI 다시 그리기 (선택 상태는 자동으로 초기화됨)
                _battleUI.UpdateHand(_deckSystem.Hand, OnCardClickedInUI);
                _battleUI.UpdateRerollState(_deckSystem.RerollsRemaining, 0);

                // 요구사항: 리롤 기회가 1일 때 리롤하면, 그 이후 자동으로 TurnProceed로 넘어간다.
                if (_deckSystem.RerollsRemaining <= 0)
                {
                    Debug.Log("[TurnReadyManager] 리롤 기회 소진! 자동으로 메인 페이즈로 넘어갑니다.");
                    CompletePlayerMulligan();
                }
            }
        }


        /// <summary>
        /// UI 화면에서 유저가 '리롤 완료(선택 완료)' 버튼을 누를 때 호출해야 하는 함수.
        /// (UI 버튼의 UnityEvent나 별도의 GameEvent를 통해 연결)
        /// </summary>
        public void OnPlayerMulliganFinished()
        {
            SoundManager.PlayDefaultUiClick();

            // 기다리고 있던 Task를 완료 상태로 만들어, RunTurnStartFlowAsync의 대기를 풀어줌
            CompletePlayerMulligan();
        }

        private void CompletePlayerMulligan()
        {
            if (_mulligan != null && !_mulligan.Task.IsCompleted)
            {
                Debug.Log("[TurnReadyManager] Player finished mulligan.");
                _mulligan.TrySetResult(true);
            }
        }
    }
}
