using UnityEngine;
using FFF.Core;
using FFF.UI.Core;

namespace FFF.UI.Battle
{
    public class BattleSceneSetup : MonoBehaviour
    {
        [SerializeField] private BattleUIComponent _battleUI;

        private void Start()
        {
            if (GameManager.Instance == null || _battleUI == null)
            {
                Debug.LogError("[BattleSceneSetup] GameManager 또는 BattleUI가 null입니다.");
                return;
            }
            GameManager.Instance.OnBattleSceneReady(_battleUI);
        }

        private void OnDestroy()
        {
            GameManager.Instance?.UnregisterScreen(UIScreenNames.BATTLE);
        }
    }
}
