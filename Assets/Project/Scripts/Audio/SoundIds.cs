namespace FFF.Audio
{
    public static class SoundIds
    {
        public const string UiClick = "ui_click";

        public const string BgmTitle = "bgm_title";
        public const string BgmMain = "bgm_main";
        public const string BgmMap = "bgm_map";
        public const string BgmBattle = "bgm_battle";

        public const string SfxBattleStart = "sfx_battle_start";
        public const string SfxTurnReady = "sfx_turn_ready";
        public const string SfxTurnEnd = "sfx_turn_end";
        public const string SfxBattleEnd = "sfx_battle_end";

        public static string GetConventionalSceneBgmId(string sceneName)
        {
            switch (sceneName)
            {
                case "TitleScene":
                    return BgmTitle;
                case "MainScene":
                    return BgmMain;
                case "StageScene":
                    return BgmMap;
                case "BattleScene":
                    return BgmBattle;
                default:
                    return null;
            }
        }
    }
}
