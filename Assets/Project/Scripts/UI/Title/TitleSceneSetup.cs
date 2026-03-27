using UnityEngine;
using FFF.UI.Core;

namespace FFF.UI.Title
{
    /// <summary>
    /// TitleScene에 배치하는 부트스트랩 컴포넌트.
    /// Scene 로드 시 해당 Scene의 UI 화면을 UIManager에 동적 등록하고,
    /// Scene 언로드 시 등록 해제한다.
    /// 
    /// UIManager는 싱글턴(DontDestroyOnLoad)이므로 Scene 간 유지되지만,
    /// 각 Scene의 UI 오브젝트는 Scene과 함께 생성/파괴된다.
    /// 이 컴포넌트가 그 연결 다리 역할을 한다.
    /// </summary>
    public class TitleSceneSetup : MonoBehaviour
    {
        [Header("이 Scene의 UI 화면")]
        [SerializeField] private TitleUIComponent _titleUI;

        private void Start()
        {
            // UIManager에 타이틀 UI 등록
            if (UIManager.Instance != null && _titleUI != null)
            {
                UIManager.Instance.RegisterScreen(UIScreenNames.TITLE, _titleUI);
                UIManager.Instance.ShowScreen(UIScreenNames.TITLE);
            }
            else
            {
                Debug.LogError("[TitleSceneSetup] UIManager 또는 TitleUI가 null입니다. BootScene을 먼저 로드했는지 확인하세요.");
            }
        }

        private void OnDestroy()
        {
            // Scene 전환 시 등록 해제
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UnregisterScreen(UIScreenNames.TITLE);
            }
        }
    }
}