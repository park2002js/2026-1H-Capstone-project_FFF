using System.Collections.Generic;
using UnityEngine;
using FFF.Core;

namespace FFF.UI.Core
{
    /// <summary>
    /// 화면 등록/표시/숨기기만 담당하는 순수 UI 스위처.
    ///
    /// MVP에서의 역할:
    /// - GameManager(Presenter)의 명령을 받아 화면을 전환한다.
    /// - 스스로 어떤 화면을 보여줄지 결정하지 않는다.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private readonly Dictionary<string, BaseUIComponent> _screens = new Dictionary<string, BaseUIComponent>();
        private BaseUIComponent _currentScreen;

        public void RegisterScreen(string screenName, BaseUIComponent screen)
        {
            if (!_screens.ContainsKey(screenName))
                _screens.Add(screenName, screen);
            else
                _screens[screenName] = screen;

            screen.Hide();
            Debug.Log($"[UIManager] 화면 등록: {screenName}");
        }

        public void UnregisterScreen(string screenName)
        {
            if (!_screens.TryGetValue(screenName, out var screen)) return;

            if (_currentScreen == screen) _currentScreen = null;
            _screens.Remove(screenName);
            Debug.Log($"[UIManager] 화면 등록 해제: {screenName}");
        }

        public void ShowScreen(string screenName)
        {
            if (!_screens.TryGetValue(screenName, out var target))
            {
                Debug.LogWarning($"[UIManager] '{screenName}' 화면을 찾을 수 없습니다.");
                return;
            }

            if (_currentScreen != null && _currentScreen != target)
                _currentScreen.Hide();

            target.Initialize();
            target.Show();
            _currentScreen = target;
            Debug.Log($"[UIManager] 화면 전환: {screenName}");
        }

        public void HideAllScreens()
        {
            foreach (var screen in _screens.Values)
                screen.Hide();

            _currentScreen = null;
        }
    }

    public static class UIScreenNames
    {
        public const string TITLE  = "Title";
        public const string MAIN   = "Main";
        public const string MAP    = "Map";
        public const string BATTLE = "Battle";
        public const string SHOP   = "Shop";
    }
}
