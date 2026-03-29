using System.Collections.Generic;
using UnityEngine;
using FFF.Core;
using FFF.Core.Events;

namespace FFF.UI.Core
{
    /// <summary>
    /// UI 시스템의 중심 허브. 싱글톤으로 Scene 간 유지된다.
    /// 
    /// 아키텍처 다이어그램에서의 역할:
    /// - "Smart: Observer 구독" → S.O 이벤트 채널을 구독하여 Controller 신호 수신
    /// - Controller(맵 시스템, 초기화 로직 등)로부터 비동기 신호를 받아 UI 전환 처리
    /// - UI Components에게 "UI 표시" 명령 전달
    /// - UI Components로부터 "사용자 행동 전달" 수신 후 Controller에 전파
    /// 
    /// 통신 흐름:
    /// [Controller] --Raise()--> [S.O Event] --Subscribe()--> [UIManager] --Show()/Hide()--> [UIComponent]
    /// [UIComponent] --Action--> [UIManager] --Raise()--> [S.O Event] --Subscribe()--> [Controller]
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("=== ScriptableObject 이벤트 채널 (Controller → UI) ===")]
        [Tooltip("맵 시스템에서 '맵 UI로 초기화하라' 신호")]
        [SerializeField] private GameEvent _onMapInitialize;

        [Tooltip("초기화 로직에서 '전투 UI로 초기화하라' 신호")]
        [SerializeField] private GameEvent _onBattleInitialize;

        [Tooltip("상점 시스템에서 '상점 UI로 초기화하라' 신호")]
        [SerializeField] private GameEvent _onShopInitialize;

        [Tooltip("GameManager에서 'Scene 전환 처리하라' 신호")]
        [SerializeField] private StringEvent _onSceneChange;

        [Header("=== ScriptableObject 이벤트 채널 (UI → Controller) ===")]
        [Tooltip("사용자가 '처음부터' 또는 '이어하기'를 선택한 신호")]
        [SerializeField] private StringEvent _onGameStartRequest;

        [Tooltip("사용자가 스테이지를 선택한 신호")]
        [SerializeField] private IntEvent _onStageSelectRequest;

        [Header("=== UI 화면 등록 ===")]
        [SerializeField] private List<UIScreenEntry> _screenEntries = new List<UIScreenEntry>();

        /// <summary>
        /// 등록된 UI 화면들을 이름으로 빠르게 찾기 위한 딕셔너리.
        /// </summary>
        private Dictionary<string, BaseUIComponent> _screens = new Dictionary<string, BaseUIComponent>();

        /// <summary>
        /// 현재 활성화된 UI 화면.
        /// </summary>
        private BaseUIComponent _currentScreen;

        protected override void OnInitialize()
        {
            // 등록된 UI 화면들을 딕셔너리에 캐싱
            foreach (var entry in _screenEntries)
            {
                if (entry.Screen != null && !_screens.ContainsKey(entry.ScreenName))
                {
                    _screens.Add(entry.ScreenName, entry.Screen);
                    entry.Screen.Hide(); // 초기에는 모두 숨김
                }
            }

            // Observer 구독: Controller → UI 방향 이벤트
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_onMapInitialize != null)
                _onMapInitialize.Subscribe(HandleMapInitialize);

            if (_onBattleInitialize != null)
                _onBattleInitialize.Subscribe(HandleBattleInitialize);

            if (_onShopInitialize != null)
                _onShopInitialize.Subscribe(HandleShopInitialize);

            if (_onSceneChange != null)
                _onSceneChange.Subscribe(HandleSceneChange);
        }

        private void UnsubscribeEvents()
        {
            if (_onMapInitialize != null)
                _onMapInitialize.Unsubscribe(HandleMapInitialize);

            if (_onBattleInitialize != null)
                _onBattleInitialize.Unsubscribe(HandleBattleInitialize);

            if (_onShopInitialize != null)
                _onShopInitialize.Unsubscribe(HandleShopInitialize);

            if (_onSceneChange != null)
                _onSceneChange.Unsubscribe(HandleSceneChange);
        }

        #region === UI 화면 전환 ===

        /// <summary>
        /// 지정한 이름의 UI 화면으로 전환한다.
        /// 현재 화면을 숨기고, 새 화면을 표시한다.
        /// </summary>
        public void ShowScreen(string screenName)
        {
            if (!_screens.TryGetValue(screenName, out var targetScreen))
            {
                Debug.LogWarning($"[UIManager] '{screenName}' UI 화면을 찾을 수 없습니다.");
                return;
            }

            // 현재 화면 숨기기
            if (_currentScreen != null && _currentScreen != targetScreen)
            {
                _currentScreen.Hide();
            }

            // 새 화면 표시
            targetScreen.Initialize();
            targetScreen.Show();
            _currentScreen = targetScreen;

            Debug.Log($"[UIManager] UI 화면 전환: {screenName}");
        }

        /// <summary>
        /// 현재 활성화된 모든 UI 화면을 숨긴다.
        /// </summary>
        public void HideAllScreens()
        {
            foreach (var screen in _screens.Values)
            {
                screen.Hide();
            }
            _currentScreen = null;
        }

        /// <summary>
        /// 런타임에 UI 화면을 등록한다.
        /// Scene 로드 후 해당 Scene의 UI를 동적으로 등록할 때 사용.
        /// </summary>
        public void RegisterScreen(string screenName, BaseUIComponent screen)
        {
            if (!_screens.ContainsKey(screenName))
            {
                _screens.Add(screenName, screen);
                screen.Hide();
                Debug.Log($"[UIManager] UI 화면 등록: {screenName}");
            }
            else
            {
                // 같은 이름으로 재등록 시 교체
                _screens[screenName] = screen;
                Debug.Log($"[UIManager] UI 화면 교체 등록: {screenName}");
            }
        }

        /// <summary>
        /// 등록된 UI 화면을 제거한다.
        /// Scene 언로드 시 해당 Scene의 UI를 정리할 때 사용.
        /// </summary>
        public void UnregisterScreen(string screenName)
        {
            if (_screens.ContainsKey(screenName))
            {
                if (_currentScreen == _screens[screenName])
                {
                    _currentScreen = null;
                }
                _screens.Remove(screenName);
                Debug.Log($"[UIManager] UI 화면 등록 해제: {screenName}");
            }
        }

        #endregion

        #region === 사용자 행동 전달 (UI → Controller) ===

        /// <summary>
        /// 사용자가 '처음부터' 또는 '이어하기'를 선택했을 때 호출.
        /// UIComponent(Dumb)에서 이 메서드를 호출하여 Controller에 전달.
        /// </summary>
        public void RequestGameStart(string startType)
        {
            Debug.Log($"[UIManager] 게임 시작 요청: {startType}");
            _onGameStartRequest?.Raise(startType);
        }

        /// <summary>
        /// 사용자가 맵에서 스테이지를 선택했을 때 호출.
        /// </summary>
        public void RequestStageSelect(int stageIndex)
        {
            Debug.Log($"[UIManager] 스테이지 선택 요청: {stageIndex}");
            _onStageSelectRequest?.Raise(stageIndex);
        }

        #endregion

        #region === 이벤트 핸들러 (Controller → UI) ===

        /// <summary>
        /// 맵 시스템으로부터 "맵 UI로 초기화하라" 신호 수신.
        /// 다이어그램: 맵 시스템 → UIManager "맵 UI로 초기화 신호 비동기 호출"
        /// </summary>
        private void HandleMapInitialize()
        {
            Debug.Log("[UIManager] 맵 UI 초기화 신호 수신");
            ShowScreen(UIScreenNames.MAP);
        }

        /// <summary>
        /// 초기화 로직으로부터 "전투 UI로 초기화하라" 신호 수신.
        /// 다이어그램: 초기화 로직 → UIManager "전투 UI로 초기화하라는 신호 비동기로 호출"
        /// </summary>
        private void HandleBattleInitialize()
        {
            Debug.Log("[UIManager] 전투 UI 초기화 신호 수신");
            ShowScreen(UIScreenNames.BATTLE);
        }

        /// <summary>
        /// 상점 시스템으로부터 "상점 UI로 초기화하라" 신호 수신.
        /// </summary>
        private void HandleShopInitialize()
        {
            Debug.Log("[UIManager] 상점 UI 초기화 신호 수신");
            ShowScreen(UIScreenNames.SHOP);
        }

        /// <summary>
        /// GameManager로부터 "Scene 전환" 신호 수신.
        /// 다이어그램: Game Manager → "Scene 변경 UI 클릭 신호"
        /// </summary>
        private void HandleSceneChange(string sceneName)
        {
            Debug.Log($"[UIManager] Scene 전환 신호 수신: {sceneName}");
            HideAllScreens();
        }

        #endregion

        protected override void OnDestroy()
        {
            UnsubscribeEvents();
            base.OnDestroy();
        }
    }

    /// <summary>
    /// UI 화면 이름 상수. 문자열 오타 방지용.
    /// </summary>
    public static class UIScreenNames
    {
        public const string TITLE = "Title";
        public const string MAIN = "Main";
        public const string MAP = "Map";
        public const string BATTLE = "Battle";
        public const string SHOP = "Shop";
        public const string GAME_OVER = "GameOver";
        public const string VICTORY = "Victory";
        public const string SETTINGS = "Settings";
    }

    /// <summary>
    /// Inspector에서 UI 화면을 이름과 함께 등록하기 위한 구조체.
    /// </summary>
    [System.Serializable]
    public class UIScreenEntry
    {
        [Tooltip("UI 화면 식별 이름 (UIScreenNames 상수 사용)")]
        public string ScreenName;

        [Tooltip("해당 UI 화면의 BaseUIComponent")]
        public BaseUIComponent Screen;
    }
}