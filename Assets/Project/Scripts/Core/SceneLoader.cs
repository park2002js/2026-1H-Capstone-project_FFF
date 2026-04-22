using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FFF.Core
{
    /// <summary>
    /// Scene 전환을 담당하는 유틸리티 클래스.
    /// 아키텍처 다이어그램의 "Scene 변경 UI 클릭 신호" 흐름에서
    /// 실제 Scene 로드를 수행하는 역할.
    /// 
    /// 현재는 단순 전환이며, 추후 페이드 인/아웃 등 전환 이펙트를 여기에 추가한다.
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>
        /// Scene 이름 상수. Build Settings에 등록된 이름과 일치해야 한다.
        /// </summary>
        public static class SceneNames
        {
            public const string BOOT = "BootScene";
            public const string TITLE = "TitleScene";
            public const string MAIN = "MainScene";
            public const string MAP = "StageScene";
            public const string BATTLE = "BattleScene";
            public const string SHOP = "ShopScene";
        }

        /// <summary>
        /// Scene 전환 완료 시 호출되는 콜백.
        /// </summary>
        public static event Action<string> OnSceneLoaded;

        /// <summary>
        /// 지정한 이름의 Scene으로 전환한다 (단순 로드).
        /// 추후 전환 이펙트 추가 시 이 메서드를 확장한다.
        /// </summary>
        public static void LoadScene(string sceneName)
        {
            Debug.Log($"[SceneLoader] Scene 전환: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Scene 로드 완료 이벤트를 등록한다.
        /// BootScene 초기화 시 한 번 호출하면 된다.
        /// </summary>
        public static void Initialize()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        /// <summary>
        /// Scene 로드 완료 이벤트를 해제한다.
        /// </summary>
        public static void Cleanup()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[SceneLoader] Scene 로드 완료: {scene.name}");
            OnSceneLoaded?.Invoke(scene.name);
        }
    }
}