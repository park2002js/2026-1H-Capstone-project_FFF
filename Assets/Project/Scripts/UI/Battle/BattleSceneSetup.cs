using UnityEngine;
using FFF.Core;
using FFF.UI.Core;
using FFF.Battle.FSM;

namespace FFF.UI.Battle
{
    public class BattleSceneSetup : MonoBehaviour
    {
        [SerializeField] private BattleUIComponent _battleUI;
        [SerializeField] private bool _startBattleOnReady = true;

        private void Start()
        {
            if (GameManager.Instance == null || _battleUI == null)
            {
                Debug.LogError("[BattleSceneSetup] GameManager 또는 BattleUI가 null입니다.");
                return;
            }

            GameManager.Instance.OnBattleSceneReady(_battleUI);

            if (_startBattleOnReady)
            {
                if (BattleManager.Instance == null)
                {
                    Debug.LogError("[BattleSceneSetup] BattleManager가 null이라 전투를 시작할 수 없습니다.");
                    return;
                }

                BattleManager.Instance.StartBattle();
            }
        }

        private void OnDestroy()
        {
            GameManager.Instance?.UnregisterScreen(UIScreenNames.BATTLE);
        }
    }
}
