using System;
using System.Collections.Generic;

namespace Meowdoku.Services
{
    // Numeric order matches SoundManager.Kind in the Godot source.
    public enum SoundKind
    {
        BoardEnter = 0,
        MarkX = 1,
        UnmarkX = 2,
        MarkCat = 3,
        MarkWrong = 4,
        UseHint = 5,
        AllCleared = 6,
        LevelWin = 7,
        LevelFail = 8,
        ButtonClick = 9,
        DialogOpen = 10,
        Clap = 11,
        BlowTrumpet = 12,
        Combo = 13,
        ComboVoice = 14,
        MarkWrongLow = 15,
        LevelFailLow = 16,
        MarkXSoft1 = 17,
        MarkXSoft2 = 18,
        RankCatCollect = 19,
        RankFishCollect1 = 20,
        RankFishCollect2 = 21,
        RankFishCollect3 = 22,
        RankScoreCount = 23,
        RankRiseUp = 24,
        RankRiseDown = 25,
        RankBoxOpen = 26,
        RankBoxAppear = 27,
        PassPageSettle = 28
    }

    public static class SoundContract
    {
        public const int KindCount = 29;
        private const string SfxRoot = "res://assets/audio/sfx/";
        private static readonly IReadOnlyList<string> DynamicPaths = BuildDynamicPaths();

        public static IReadOnlyList<string> DynamicSourcePaths => DynamicPaths;

        public static string SourcePath(SoundKind kind)
        {
            switch (kind)
            {
                case SoundKind.BoardEnter: return "res://assets/audio/sfx/board_enter_1.ogg";
                case SoundKind.MarkX: return "res://assets/audio/sfx/mark_x_2.ogg";
                case SoundKind.UnmarkX: return "res://assets/audio/sfx/unmark_x_2.ogg";
                case SoundKind.MarkCat: return "res://assets/audio/sfx/mark_cat.ogg";
                case SoundKind.MarkWrong: return "res://assets/audio/sfx/mark_wrong_1.ogg";
                case SoundKind.UseHint: return "res://assets/audio/sfx/use_hint.ogg";
                case SoundKind.AllCleared: return "res://assets/audio/sfx/all_cleared.ogg";
                case SoundKind.LevelWin: return "res://assets/audio/sfx/level_win.ogg";
                case SoundKind.LevelFail: return "res://assets/audio/sfx/level_fail.ogg";
                case SoundKind.ButtonClick: return "res://assets/audio/sfx/btn_click_2.ogg";
                case SoundKind.DialogOpen: return "res://assets/audio/sfx/dlg_open_1.ogg";
                case SoundKind.Clap: return "res://assets/audio/sfx/tile_handlike_clip.ogg";
                case SoundKind.BlowTrumpet: return "res://assets/audio/sfx/tile_handlike_genius.ogg";
                case SoundKind.Combo: return "res://assets/audio/sfx/combo_encourage.ogg";
                case SoundKind.ComboVoice: return "res://assets/audio/sfx/combo_voice.ogg";
                case SoundKind.MarkXSoft1: return "res://assets/audio/sfx/mark_x_3.ogg";
                case SoundKind.MarkXSoft2: return "res://assets/audio/sfx/mark_x_4.ogg";
                case SoundKind.RankCatCollect: return "res://assets/audio/sfx/rank_cat_collect.ogg";
                case SoundKind.RankFishCollect1: return "res://assets/audio/sfx/rank_fish_collect_1.ogg";
                case SoundKind.RankFishCollect2: return "res://assets/audio/sfx/rank_fish_collect_2.ogg";
                case SoundKind.RankFishCollect3: return "res://assets/audio/sfx/rank_fish_collect_3.ogg";
                case SoundKind.RankScoreCount: return "res://assets/audio/sfx/rank_score_count.ogg";
                case SoundKind.RankRiseUp: return "res://assets/audio/sfx/rank_rise_up.ogg";
                case SoundKind.RankRiseDown: return "res://assets/audio/sfx/rank_rise_down.ogg";
                case SoundKind.RankBoxOpen: return "res://assets/audio/sfx/rank_box_open.ogg";
                case SoundKind.RankBoxAppear: return "res://assets/audio/sfx/rank_box_appear.ogg";
                case SoundKind.PassPageSettle: return "res://assets/audio/sfx/pass_page_settle.ogg";
                default: return string.Empty;
            }
        }

        public static int Polyphony(SoundKind kind)
        {
            switch (kind)
            {
                case SoundKind.MarkX:
                case SoundKind.UnmarkX:
                case SoundKind.ButtonClick:
                case SoundKind.MarkXSoft1:
                case SoundKind.MarkXSoft2:
                    return 4;
                case SoundKind.MarkCat:
                    return 3;
                case SoundKind.MarkWrong:
                case SoundKind.Clap:
                case SoundKind.BlowTrumpet:
                case SoundKind.Combo:
                case SoundKind.ComboVoice:
                    return 2;
                default:
                    return 1;
            }
        }

        public static bool DucksBgm(SoundKind kind)
        {
            return kind == SoundKind.BoardEnter || kind == SoundKind.LevelWin;
        }

        public static bool ShouldPlayBgm()
        {
            return false;
        }

        public static bool CanPlaySfx(bool silent, bool soundOn, SoundKind kind)
        {
            return !silent && soundOn && SourcePath(kind).Length > 0;
        }

        public static bool CanPlayPeople(bool silent, bool peopleOn, string path)
        {
            return !silent && peopleOn && !string.IsNullOrEmpty(path);
        }

        public static bool CanPlayMeow(bool silent, bool soundOn, string path)
        {
            return !silent && soundOn && !string.IsNullOrEmpty(path);
        }

        private static IReadOnlyList<string> BuildDynamicPaths()
        {
            string[] comboFiles =
            {
                "combo_nice_s6.ogg", "combo_great_s6.ogg",
                "combo_perfect_s6.ogg", "combo_excellent_s6.ogg",
                "combo_amazing_s6.ogg", "combo_unbelievable_s6.ogg",
                "combo_incredible_s9.ogg", "combo_phenomenal_s9.ogg",
                "combo_spectacular_s9.ogg", "combo_legendary_s9.ogg",
                "combo_unreal_s10.ogg", "combo_insane_s10.ogg",
                "combo_epic_s10.ogg", "combo_breathtaking_s10.ogg",
                "combo_nice_meow_s11.ogg", "combo_great_kitty_s11.ogg",
                "combo_purr_fect_s11.ogg", "combo_excellent_kitten_s11.ogg",
                "combo_amazing_mew_s11.ogg", "combo_paw_some_s11.ogg",
                "combo_incredible_whiskers_s11.ogg", "combo_catnip_level_s11.ogg",
                "combo_epic_pounce_s11.ogg", "combo_legendary_furball_s11.ogg"
            };
            var paths = new List<string>(39);
            for (int index = 0; index < comboFiles.Length; index++)
                paths.Add(SfxRoot + comboFiles[index]);
            paths.Add(SfxRoot + "meow_single.ogg");
            for (int index = 1; index <= 7; index++)
                paths.Add(SfxRoot + $"meow_cresc_{index}.ogg");
            for (int index = 1; index <= 7; index++)
                paths.Add(SfxRoot + $"meow_rand_{index}.ogg");
            return paths;
        }
    }
}
