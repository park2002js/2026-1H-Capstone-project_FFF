using UnityEngine;
using System.Collections;
using FFF.Core;
using FFF.UI.Core;
using FFF.Battle.FSM;

namespace FFF.UI.Battle
{
    public class BattleSceneSetup : MonoBehaviour
    {
        [SerializeField] private BattleUIComponent _battleUI;
        [SerializeField] private bool _startBattleOnReady = true;

        /// <summary>
        /// 다른 모든 스크립트의 Awake와 Start가 끝나기를 기다리기 위해 코루틴 사용
        /// 코루틴으로 1프레임을 대기하여 위 목적을 이룬다.
        /// </summary>
        private IEnumerator Start()
        {
            if (GameManager.Instance == null || _battleUI == null)
            {
                Debug.LogError("[BattleSceneSetup] GameManager 또는 BattleUI가 null입니다.");
                yield break; // return 대신 yield break 사용
            }
            
            // 1. 기존처럼 GameManager에게 씬 로드 완료를 알리고 UI를 세팅
            GameManager.Instance.OnBattleSceneReady(_battleUI);

            // 2. 핵심: 딱 한 프레임을 대기
            // 이렇게 하면 씬에 있는 모든 스크립트들의 Awake()와 Start()가 완전히 끝났음이 100% 보장됨
            yield return null; 

            // 3. 모든 준비가 끝났으므로 BattleManager의 StartBattle 호출
            if (_startBattleOnReady && BattleManager.Instance != null)
            {
                BattleManager.Instance.StartBattle();
            }
            else
            {
                Debug.LogError("[BattleSceneSetup] BattleManager Instance를 찾을 수 없습니다.");
            }
        }

        private void OnDestroy()
        {
            GameManager.Instance?.UnregisterScreen(UIScreenNames.BATTLE);
        }
    }
}
