using UnityEngine;
using FFF.Core;
using FFF.UI.Core;

namespace FFF.UI.Main
{
    public class MainSceneSetup : MonoBehaviour
    {
        [SerializeField] private MainUIComponent _mainUI;

        private void Start()
        {
            if (GameManager.Instance == null || _mainUI == null)
            {
                Debug.LogError("[MainSceneSetup] GameManager 또는 MainUI가 null입니다.");
                return;
            }
            GameManager.Instance.OnMainSceneReady(_mainUI);
        }

        private void OnDestroy()
        {
            GameManager.Instance?.UnregisterScreen(UIScreenNames.MAIN);
        }
    }
}
