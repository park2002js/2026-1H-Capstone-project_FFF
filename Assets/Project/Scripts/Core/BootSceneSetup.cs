using UnityEngine;
using FFF.Core;

namespace FFF.Core
{
    /// <summary>
    /// BootScene에 배치하는 게임 진입점.
    /// 
    /// 역할:
    /// 1. 싱글턴 매니저들이 Awake에서 자동 초기화되도록 보장
    /// 2. SceneLoader 초기화
    /// 3. 모든 초기화 완료 후 TitleScene으로 전환
    /// 
    /// 게임 실행 시 BootScene이 가장 먼저 로드되어야 한다.
    /// Build Settings에서 BootScene을 index 0에 배치할 것.
    /// </summary>
    public class BootSceneSetup : MonoBehaviour
    {
        private void Start()
        {
            // SceneLoader 초기화 (Scene 로드 이벤트 등록)
            SceneLoader.Initialize();

            Debug.Log("[Boot] 초기화 완료 → TitleScene 전환");

            // 타이틀 화면으로 이동
            SceneLoader.LoadScene(SceneLoader.SceneNames.TITLE);
        }
    }
}