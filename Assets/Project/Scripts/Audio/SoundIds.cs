namespace FFF.Audio
{
    public static class SoundIds
    {
        public const string UiClick = "ui_click";
        public const string UiConfirm = "ui_confirm";
        public const string UiCancel = "ui_cancel";
        public const string UiError = "ui_error";

        public const string BgmTitle = "bgm_title";
        public const string BgmMain = "bgm_main";
        public const string BgmMap = "bgm_map";
        public const string BgmBattle = "bgm_battle";

        public const string SfxSceneTransition = "sfx_scene_transition";

        public const string SfxBattleStart = "sfx_battle_start";
        public const string SfxTurnReady = "sfx_turn_ready";
        public const string SfxTurnProceed = "sfx_turn_proceed";
        public const string SfxTurnEnd = "sfx_turn_end";
        public const string SfxBattleEnd = "sfx_battle_end";

        public const string SfxCardDraw = "sfx_card_draw";
        public const string SfxCardDeal = "sfx_card_deal";
        public const string SfxCardFlip = "sfx_card_flip";
        public const string SfxCardSelect = "sfx_card_select";
        public const string SfxCardDeselect = "sfx_card_deselect";
        public const string SfxCardReroll = "sfx_card_reroll";
        public const string SfxCardDiscard = "sfx_card_discard";
        public const string SfxDeckShuffle = "sfx_deck_shuffle";
        public const string SfxDiscardRecycle = "sfx_discard_recycle";

        public const string SfxPlayerAttack = "sfx_player_attack";
        public const string SfxEnemyAttack = "sfx_enemy_attack";
        public const string SfxHitLight = "sfx_hit_light";
        public const string SfxHitHeavy = "sfx_hit_heavy";
        public const string SfxCritical = "sfx_critical";
        public const string SfxBlock = "sfx_block";
        public const string SfxMiss = "sfx_miss";
        public const string SfxPlayerDamage = "sfx_player_damage";
        public const string SfxEnemyDamage = "sfx_enemy_damage";
        public const string SfxEnemyDefeat = "sfx_enemy_defeat";
        public const string SfxPlayerDefeat = "sfx_player_defeat";

        public const string SfxHandReveal = "sfx_hand_reveal";
        public const string SfxHandStrong = "sfx_hand_strong";
        public const string SfxHandWeak = "sfx_hand_weak";
        public const string SfxRoundWin = "sfx_round_win";
        public const string SfxRoundLose = "sfx_round_lose";
        public const string SfxBattleVictory = "sfx_battle_victory";
        public const string SfxBattleDefeat = "sfx_battle_defeat";

        public const string SfxMapNodeSelect = "sfx_map_node_select";
        public const string SfxMapNodeLocked = "sfx_map_node_locked";
        public const string SfxMapPathReveal = "sfx_map_path_reveal";
        public const string SfxMapEnterBattle = "sfx_map_enter_battle";
        public const string SfxMapEnterShop = "sfx_map_enter_shop";
        public const string SfxMapEnterBoss = "sfx_map_enter_boss";

        public const string SfxRewardOpen = "sfx_reward_open";
        public const string SfxRewardSelect = "sfx_reward_select";
        public const string SfxRewardClaim = "sfx_reward_claim";
        public const string SfxJokerActivate = "sfx_joker_activate";
        public const string SfxAccessoryActivate = "sfx_accessory_activate";
        public const string SfxItemEquip = "sfx_item_equip";
        public const string SfxGoldGain = "sfx_gold_gain";
        public const string SfxGoldSpend = "sfx_gold_spend";

        public const string SfxShopOpen = "sfx_shop_open";
        public const string SfxShopBuy = "sfx_shop_buy";
        public const string SfxShopCannotBuy = "sfx_shop_cannot_buy";
        public const string SfxShopCardRemove = "sfx_shop_card_remove";
        public const string SfxShopLeave = "sfx_shop_leave";

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
