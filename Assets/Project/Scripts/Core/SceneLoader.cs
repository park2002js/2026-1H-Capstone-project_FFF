using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FFF.Core
{
    public static class SceneLoader
    {
        private static bool _isInitialized;

        public static class SceneNames
        {
            public const string BOOT = "BootScene";
            public const string TITLE = "TitleScene";
            public const string MAIN = "MainScene";
            public const string MAP = "StageScene";
            public const string BATTLE = "BattleScene";
            public const string SHOP = "ShopScene";
        }

        public static event Action<string> OnSceneLoaded;

        public static void LoadScene(string sceneName)
        {
            LoadScene(sceneName, true);
        }

        public static void LoadScene(string sceneName, bool showLoadingScreen)
        {
            Debug.Log($"[SceneLoader] Scene change: {sceneName}");

            if (!Application.isPlaying || !showLoadingScreen)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            LoadingScreenController.EnsureExists().LoadScene(sceneName);
        }

        public static void LoadSceneImmediate(string sceneName)
        {
            LoadScene(sceneName, false);
        }

        public static void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        public static void Initialize()
        {
            if (_isInitialized)
                return;

            SceneManager.sceneLoaded += HandleSceneLoaded;
            _isInitialized = true;
        }

        public static void Cleanup()
        {
            if (!_isInitialized)
                return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _isInitialized = false;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[SceneLoader] Scene loaded: {scene.name}");
            OnSceneLoaded?.Invoke(scene.name);
        }
    }
}
