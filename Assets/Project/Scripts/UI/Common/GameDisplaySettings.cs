using System.Collections.Generic;
using UnityEngine;

namespace FFF.UI.Common
{
    public static class GameDisplaySettings
    {
        private const string ScreenModePrefsKey = "FFF.Settings.ScreenMode";
        private const string LegacyFullscreenPrefsKey = "FFF.Settings.Fullscreen";
        private const string ResolutionWidthPrefsKey = "FFF.Settings.ResolutionWidth";
        private const string ResolutionHeightPrefsKey = "FFF.Settings.ResolutionHeight";

        private static readonly Vector2Int[] CommonResolutions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440)
        };

        private static readonly FullScreenMode[] ScreenModes =
        {
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.FullScreenWindow,
            FullScreenMode.Windowed
        };

        private static readonly string[] ScreenModeLabels =
        {
            "전체화면",
            "전체화면 창",
            "창모드"
        };

        public static List<string> BuildResolutionLabels()
        {
            List<string> labels = new List<string>(CommonResolutions.Length);
            for (int i = 0; i < CommonResolutions.Length; i++)
                labels.Add($"{CommonResolutions[i].x} x {CommonResolutions[i].y}");

            return labels;
        }

        public static List<string> BuildScreenModeLabels()
        {
            return new List<string>(ScreenModeLabels);
        }

        public static int GetCurrentResolutionIndex()
        {
            int savedWidth = PlayerPrefs.GetInt(ResolutionWidthPrefsKey, Screen.width);
            int savedHeight = PlayerPrefs.GetInt(ResolutionHeightPrefsKey, Screen.height);
            int exactIndex = FindResolutionIndex(savedWidth, savedHeight);
            if (exactIndex >= 0)
                return exactIndex;

            exactIndex = FindResolutionIndex(Screen.width, Screen.height);
            if (exactIndex >= 0)
                return exactIndex;

            return 2;
        }

        public static int GetCurrentScreenModeIndex()
        {
            if (PlayerPrefs.HasKey(ScreenModePrefsKey))
            {
                FullScreenMode savedMode = (FullScreenMode)PlayerPrefs.GetInt(ScreenModePrefsKey);
                int savedIndex = FindScreenModeIndex(savedMode);
                if (savedIndex >= 0)
                    return savedIndex;
            }

            int currentIndex = FindScreenModeIndex(Screen.fullScreenMode);
            if (currentIndex >= 0)
                return currentIndex;

            if (PlayerPrefs.HasKey(LegacyFullscreenPrefsKey))
                return PlayerPrefs.GetInt(LegacyFullscreenPrefsKey) == 1 ? 1 : 2;

            return Screen.fullScreen ? 1 : 2;
        }

        public static void ApplySaved()
        {
            bool hasSavedResolution = PlayerPrefs.HasKey(ResolutionWidthPrefsKey) && PlayerPrefs.HasKey(ResolutionHeightPrefsKey);
            bool hasSavedMode = PlayerPrefs.HasKey(ScreenModePrefsKey) || PlayerPrefs.HasKey(LegacyFullscreenPrefsKey);
            if (!hasSavedResolution && !hasSavedMode)
                return;

            Vector2Int resolution = hasSavedResolution
                ? GetCurrentResolution()
                : new Vector2Int(Screen.width, Screen.height);
            FullScreenMode mode = hasSavedMode ? GetCurrentScreenMode() : Screen.fullScreenMode;
            Apply(resolution, mode);
        }

        public static void ApplyResolutionIndex(int index)
        {
            Vector2Int resolution = CommonResolutions[Mathf.Clamp(index, 0, CommonResolutions.Length - 1)];
            ApplyAndSave(resolution, GetCurrentScreenMode());
        }

        public static void ApplyScreenModeIndex(int index)
        {
            FullScreenMode mode = ScreenModes[Mathf.Clamp(index, 0, ScreenModes.Length - 1)];
            ApplyAndSave(GetCurrentResolution(), mode);
        }

        private static Vector2Int GetCurrentResolution()
        {
            return CommonResolutions[GetCurrentResolutionIndex()];
        }

        private static FullScreenMode GetCurrentScreenMode()
        {
            return ScreenModes[GetCurrentScreenModeIndex()];
        }

        private static void ApplyAndSave(Vector2Int resolution, FullScreenMode mode)
        {
            Apply(resolution, mode);

            PlayerPrefs.SetInt(ResolutionWidthPrefsKey, resolution.x);
            PlayerPrefs.SetInt(ResolutionHeightPrefsKey, resolution.y);
            PlayerPrefs.SetInt(ScreenModePrefsKey, (int)mode);
            PlayerPrefs.SetInt(LegacyFullscreenPrefsKey, mode == FullScreenMode.Windowed ? 0 : 1);
            PlayerPrefs.Save();
        }

        private static void Apply(Vector2Int resolution, FullScreenMode mode)
        {
            Screen.SetResolution(resolution.x, resolution.y, mode);
        }

        private static int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < CommonResolutions.Length; i++)
            {
                if (CommonResolutions[i].x == width && CommonResolutions[i].y == height)
                    return i;
            }

            return -1;
        }

        private static int FindScreenModeIndex(FullScreenMode mode)
        {
            for (int i = 0; i < ScreenModes.Length; i++)
            {
                if (ScreenModes[i] == mode)
                    return i;
            }

            return -1;
        }
    }
}
