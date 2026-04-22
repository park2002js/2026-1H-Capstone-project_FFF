using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환용
using FFF.Battle.FSM;
using FFF.UI.Battle;
using FFF.Core.Events;

namespace FFF.Battle.FSM
{
    public class BattleEndManager : MonoBehaviour
    {
        [Header("=== 시스템 참조 ===")]
        [SerializeField] private BattleManager _battleManager;
        [SerializeField] private BattleUIComponent _battleUI;

        [Header("=== 수신할 이벤트 ===")]
        [SerializeField] private GameEvent _onBattleEndEvent;

        private void OnEnable()
        {
            if (_onBattleEndEvent != null) _onBattleEndEvent.Subscribe(HandleBattleEndEnter);
        }

        private void OnDisable()
        {
            if (_onBattleEndEvent != null) _onBattleEndEvent.Unsubscribe(HandleBattleEndEnter);
        }

        private void HandleBattleEndEnter()
        {
            Debug.Log("========== [BattleEnd] 전투 종료 및 결과 표시 ==========");

            // 1. 기존 전투 UI 싹 지우기 (옵션)
            _battleUI.ClearHandUI();
            _battleUI.SetTurnProceedUIVisibility(false);
            _battleUI.SetTurnReadyUIVisibility(false);

            // 2. 바구니(Context) 열어보기 (핵심 로직)
            bool isPlayerWin = _battleManager.Context.IsPlayerWinner;

            // 3. 결과에 따라 분기하여 UI 텍스트 결정
            string message = isPlayerWin ? "Stage Clear\n<size=50>플레이어 승</size>" : "Game Over\n<size=50>플레이어 패배</size>";
            
            // 4. UI 띄우기
            _battleUI.ShowBattleResult(message);
        }

        #region === 버튼 클릭 콜백 ===

        public void OnRestartButtonClicked()
        {
            Debug.Log("[BattleEnd] 전투를 다시 시작합니다.");
            // 현재 활성화된 씬(BattleScene)을 다시 로드하여 모든 것을 완전 초기화합니다.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnTitleButtonClicked()
        {
            Debug.Log("[BattleEnd] 타이틀로 돌아갑니다.");
            // 씬 이름은 실제 프로젝트의 Title 씬 이름("TitleScene" 등)으로 맞춰주세요.
            SceneManager.LoadScene("TitleScene"); 
        }

        #endregion
    }
}