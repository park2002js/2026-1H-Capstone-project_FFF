using System.Threading.Tasks;
using UnityEngine;
using FFF.Battle.Card;
using FFF.Battle.FSM;
using FFF.Core.Events;

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
            // 비동기 흐름 시작 (Fire and Forget)
            _ = RunTurnStartFlowAsync();
        }

        private async Task RunTurnStartFlowAsync()
        {
            Debug.Log("[TurnPhaseManager] 1. 턴 시작 준비 및 드로우");
            _deckSystem.OnTurnStarted();
            _deckSystem.DrawCards();

            Debug.Log("[TurnPhaseManager] 2. 유저 멀리건 대기 시작...");
            
            // TaskCompletionSource를 초기화
            // TaskCompletionSource -> 누군가 결과를 넣어줄 때까지 Task를 멈추고 기다리게 만듦 (코루틴보다 깔끔)
            _mulligan = new TaskCompletionSource<bool>();
            await _mulligan.Task;

            Debug.Log("[TurnPhaseManager] 3. 멀리건 종료. 메인 페이즈로 전환합니다.");
            _battleManager.ChangeState(TurnState.TurnProceed);
        }

        /// <summary>
        /// UI 화면에서 유저가 '리롤 완료(선택 완료)' 버튼을 누를 때 호출해야 하는 함수.
        /// (UI 버튼의 UnityEvent나 별도의 GameEvent를 통해 연결)
        /// </summary>
        public void OnPlayerMulliganFinished()
        {
            // 기다리고 있던 Task를 완료 상태로 만들어, RunTurnStartFlowAsync의 대기를 풀어줌
            _mulligan?.TrySetResult(true);
        }
    }
}