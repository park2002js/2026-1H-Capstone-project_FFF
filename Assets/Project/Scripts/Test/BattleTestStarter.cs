using UnityEngine;
using FFF.Battle.FSM;

namespace FFF.Test
{
    public class BattleTestStarter : MonoBehaviour
    {
        public void OnClickStartBattle()
        {
            Debug.Log("[Test] 테스트 버튼 클릭 -> 전투 시작 요청");
            
            // BattleManager를 통해 전투 시작 파이프라인 가동
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.StartBattle();
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("BattleManager Instance가 존재하지 않습니다!");
            }
        }
    }
}