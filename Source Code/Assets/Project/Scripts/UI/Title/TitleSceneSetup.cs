using UnityEngine;
using FFF.Core;
using FFF.UI.Core;

namespace FFF.UI.Title
{
    public class TitleSceneSetup : MonoBehaviour
    {
        [SerializeField] private TitleUIComponent _titleUI;

        private void Start()
        {
            if (GameManager.Instance == null || _titleUI == null)
            {
                Debug.LogError("[TitleSceneSetup] GameManager 또는 TitleUI가 null입니다.");
                return;
            }
            GameManager.Instance.OnTitleSceneReady(_titleUI);
        }

        private void OnDestroy()
        {
            GameManager.Instance?.UnregisterScreen(UIScreenNames.TITLE);
        }
    }
}
