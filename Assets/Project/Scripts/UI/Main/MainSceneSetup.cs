using UnityEngine;
using FFF.UI.Core;

namespace FFF.UI.Main
{
    /// <summary>
    /// MainScene에 배치하는 부트스트랩 컴포넌트.
    /// Scene 로드 시 메인 화면 UI를 UIManager에 동적 등록한다.
    /// </summary>
    public class MainSceneSetup : MonoBehaviour
    {
        [Header("이 Scene의 UI 화면")]
        [SerializeField] private MainUIComponent _mainUI;

        private void Start()
        {
            if (UIManager.Instance != null && _mainUI != null)
            {
                UIManager.Instance.RegisterScreen(UIScreenNames.MAIN, _mainUI);
                UIManager.Instance.ShowScreen(UIScreenNames.MAIN);
            }
            else
            {
                Debug.LogError("[MainSceneSetup] UIManager 또는 MainUI가 null입니다. BootScene을 먼저 로드했는지 확인하세요.");
            }
        }

        private void OnDestroy()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UnregisterScreen(UIScreenNames.MAIN);
            }
        }
    }
}