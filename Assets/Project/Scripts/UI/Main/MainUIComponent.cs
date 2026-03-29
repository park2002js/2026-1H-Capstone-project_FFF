using UnityEngine;
using UnityEngine.UI;
using FFF.UI.Core;

namespace FFF.UI.Main
{
    /// <summary>
    /// 백로그 10번/1번: 메인 화면 UI.
    /// 
    /// 기능:
    /// - '처음부터' 버튼: 새 게임 시작 → 맵 화면으로 전환
    /// - '이어하기' 버튼: 저장된 게임 이어서 → 맵 화면으로 전환
    /// - '설정' 버튼: 설정 팝업 표시
    /// - '게임 종료' 버튼: 게임 종료
    /// 
    /// Dumb 원칙:
    /// - 버튼 클릭 이벤트만 UIManager에게 전달
    /// - 게임 로직(시드 생성, 세이브 로드 등)은 관여하지 않음
    /// </summary>
    public class MainUIComponent : BaseUIComponent
    {
        [Header("=== 메인 화면 버튼 ===")]
        [SerializeField] private Button _newGameButton;      // 처음부터
        [SerializeField] private Button _continueButton;     // 이어하기
        [SerializeField] private Button _settingsButton;     // 설정
        [SerializeField] private Button _quitButton;         // 게임 종료

        protected override void OnInitialize()
        {
            // 버튼 리스너 등록
            if (_newGameButton != null)
                _newGameButton.onClick.AddListener(OnNewGameClicked);

            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinueClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);

            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitClicked);

            // 이어하기 버튼 활성화 여부 판단
            // 추후 세이브 데이터 존재 여부에 따라 비활성화 처리
            UpdateContinueButtonState();
        }

        protected override void OnShow()
        {
            UpdateContinueButtonState();
        }

        protected override void OnHide()
        {
            // 리스너 중복 등록 방지를 위해 해제
            if (_newGameButton != null)
                _newGameButton.onClick.RemoveListener(OnNewGameClicked);

            if (_continueButton != null)
                _continueButton.onClick.RemoveListener(OnContinueClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);

            if (_quitButton != null)
                _quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        /// <summary>
        /// '처음부터' 버튼 클릭.
        /// 백로그 1번: "'처음부터' 혹은 '이어하기' 선택 시 저장된 게임 시나리오에 맞는 맵 현황 띄움"
        /// 백로그 1번: "'처음부터'로 시작하면 플레이어가 선택할 여러 스테이지들이 길로 연결되어 있는 지도를 생성한다."
        /// </summary>
        private void OnNewGameClicked()
        {
            Debug.Log("[MainUI] '처음부터' 클릭 → UIManager에 전달");
            UIManager.Instance.RequestGameStart("new");
        }

        /// <summary>
        /// '이어하기' 버튼 클릭.
        /// 백로그 1번: "'이어하기'로 시작하면 플레이어가 이전에 플레이했던 데이터를 기반으로 지도를 불러온다."
        /// </summary>
        private void OnContinueClicked()
        {
            Debug.Log("[MainUI] '이어하기' 클릭 → UIManager에 전달");
            UIManager.Instance.RequestGameStart("continue");
        }

        /// <summary>
        /// '설정' 버튼 클릭.
        /// 백로그 1번: "'설정'을 눌렀을 때, 설정에 맞는 기능들이 등장한다."
        /// </summary>
        private void OnSettingsClicked()
        {
            Debug.Log("[MainUI] '설정' 클릭 → 설정 팝업 표시 요청");
            // 추후 UIManager를 통해 Settings 팝업 표시
            // UIManager.Instance.ShowScreen(UIScreenNames.SETTINGS);
        }

        /// <summary>
        /// '게임 종료' 버튼 클릭.
        /// </summary>
        private void OnQuitClicked()
        {
            Debug.Log("[MainUI] '게임 종료' 클릭");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 이어하기 버튼의 활성화 상태를 업데이트한다.
        /// 세이브 데이터가 없으면 비활성화(회색 처리).
        /// </summary>
        private void UpdateContinueButtonState()
        {
            if (_continueButton == null) return;

            // 추후 세이브 시스템 연동 시 실제 판단 로직으로 교체
            bool hasSaveData = false; // TODO: 세이브 시스템에서 확인

            _continueButton.interactable = hasSaveData;
        }
    }
}