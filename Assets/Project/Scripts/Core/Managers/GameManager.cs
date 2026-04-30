using UnityEngine;
using FFF.UI.Core;
using FFF.UI.Title;
using FFF.UI.Main;
using FFF.UI.Map;
using FFF.Map;
using FFF.UI.Battle;
using FFF.Data;
using FFF.Battle.Data;

namespace FFF.Core
{
    /// <summary>
    /// 최상위 게임 흐름 통솔자 (MVP - Presenter 최상위).
    ///
    /// 역할:
    /// - 씬 전환 결정
    /// - 각 씬의 View(UIComponent)에 델리게이트 연결
    /// - UIManager를 통해 화면 표시 명령
    ///
    /// 각 SceneSetup이 씬 준비 완료 시 OnXxxSceneReady()를 호출하면,
    /// GameManager가 View에 델리게이트를 연결하고 UIManager에 표시를 지시한다.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {

        [Header("=== Master Data ===")]
        [Tooltip("Map 및 게임 전반에서 원본으로 사용할 플레이어 데이터 SO")]
        [SerializeField] private PlayerDataSO _masterPlayerData;
        public PlayerDataSO MasterPlayerData => _masterPlayerData;


        // ================================================================
        // 씬별 준비 완료 알림 (SceneSetup → GameManager)
        // ================================================================

        public void OnTitleSceneReady(TitleUIComponent view)
        {
            view.OnExit = HandleTitleExit;
            UIManager.Instance.RegisterScreen(UIScreenNames.TITLE, view);
            UIManager.Instance.ShowScreen(UIScreenNames.TITLE);
        }

        public void OnMainSceneReady(MainUIComponent view)
        {
            view.OnNewGame  = HandleNewGame;
            view.OnContinue = HandleContinue;
            UIManager.Instance.RegisterScreen(UIScreenNames.MAIN, view);
            UIManager.Instance.ShowScreen(UIScreenNames.MAIN);
        }

        public void OnMapSceneReady(MapUIComponent view, MapData mapData)
        {
            view.SetMapData(mapData);
            view.OnNodeSelected = HandleStageSelect;
            UIManager.Instance.RegisterScreen(UIScreenNames.MAP, view);
            UIManager.Instance.ShowScreen(UIScreenNames.MAP);
        }

        public void OnBattleSceneReady(BattleUIComponent view)
        {
            UIManager.Instance.RegisterScreen(UIScreenNames.BATTLE, view);
            UIManager.Instance.ShowScreen(UIScreenNames.BATTLE);
        }

        public void UnregisterScreen(string screenName)
        {
            UIManager.Instance?.UnregisterScreen(screenName);
        }

        // ================================================================
        // 게임 흐름 결정 (GameManager 내부)
        // ================================================================

        private void HandleTitleExit()
        {
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAIN);
        }

        private void HandleNewGame()
        {
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
        }

        private void HandleContinue()
        {
            // TODO: 세이브 데이터 로드 후 MapScene으로
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
        }

        /// <summary>
        /// 특정 스테이지로 이동할 때 호출됩니다.
        /// </summary>
        private void HandleStageSelect(int nodeId)
        {
            Debug.Log($"[GameManager] 스테이지 선택: nodeId={nodeId}");
            // TODO: nodeId를 바탕으로 BattleContext 구성 후 BattleScene으로
            SceneLoader.LoadScene(SceneLoader.SceneNames.BATTLE);
        }

        /// <summary>
        /// 전투가 끝나고 Map으로 돌아갈 때 호출됩니다.
        /// </summary>
        public void HandleReturnToMap(PlayerDataBattle finalBattleData)
        {
            Debug.Log("[GameManager] 전투 종료. 맵으로 귀환하며 데이터를 동기화합니다.");

            // 1. PlayerDataUpdater 객체 생성
            PlayerDataUpdater updater = new PlayerDataUpdater();
            
            // 2. 동기화 실행 (로컬 데이터 -> SO 데이터)
            updater.SyncBattleDataToMaster(finalBattleData, _masterPlayerData);

            // 3. 동기화 완료 후 Map 씬으로 이동
            // (updater 객체는 이 메서드가 끝나면 지역 변수이므로 가비지 컬렉터에 의해 자동으로 메모리에서 제거됨)
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAP);
        }
    }
}
