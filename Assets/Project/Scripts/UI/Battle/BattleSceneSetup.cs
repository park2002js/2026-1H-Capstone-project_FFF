using UnityEngine;
using FFF.UI.Core;

namespace FFF.UI.Battle
{
    /// <summary>
    /// BattleScene에 배치하는 부트스트랩 컴포넌트.
    /// Scene 로드 시 전투 UI를 UIManager에 등록하고,
    /// Scene 언로드 시 등록 해제한다.
    /// </summary>
    public class BattleSceneSetup : MonoBehaviour
    {
        [Header("이 Scene의 UI 화면")]
        [SerializeField] private BattleUIComponent _battleUI;

        private void Start()
        {
            if (UIManager.Instance != null && _battleUI != null)
            {
                UIManager.Instance.RegisterScreen(UIScreenNames.BATTLE, _battleUI);
                UIManager.Instance.ShowScreen(UIScreenNames.BATTLE);
            }
            else
            {
                Debug.LogError("[BattleSceneSetup] UIManager 또는 BattleUI가 null입니다. BootScene을 먼저 로드했는지 확인하세요.");
            }
        }

        private void OnDestroy()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UnregisterScreen(UIScreenNames.BATTLE);
            }
        }
    }
}
