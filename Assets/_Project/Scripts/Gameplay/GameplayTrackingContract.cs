using System;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Tracking;

namespace Meowdoku.Gameplay
{
    public sealed class GameplayTrackingStartData
    {
        public string Qid { get; internal set; } = string.Empty;
        public string QuestionRotation { get; internal set; } = "0";
        public string Status { get; internal set; } = TrackerCatalog.GameStatus.New;
        public string GameType { get; internal set; } = TrackerCatalog.GameType.Normal;
        public int Difficulty { get; internal set; }
        public int Level { get; internal set; }
        public int StrategyLayer { get; internal set; }
        public int Scale { get; internal set; }
        public int IsChallenge { get; internal set; }
        public string PreType { get; internal set; } = "0";
    }

    public static class GameplayTrackingContract
    {
        public static GameplayTrackingStartData BuildStart(
            GameSessionSnapshotContext context,
            string status,
            int currentLevel,
            bool isHard,
            bool isChallenge)
        {
            if (context?.Entry == null)
                throw new ArgumentNullException(nameof(context));
            LevelEntry entry = context.Entry;
            bool daily = context.ResolvedMode == GameplaySessionMode.Daily;
            return new GameplayTrackingStartData
            {
                Qid = BuildQid(context),
                QuestionRotation = TrackerService.TransformToQuestionRotation(
                    entry.BankTransform),
                Status = string.IsNullOrEmpty(status)
                    ? TrackerCatalog.GameStatus.New
                    : status,
                GameType = daily
                    ? TrackerCatalog.GameType.Daily
                    : TrackerCatalog.GameType.Normal,
                Difficulty = isHard ? 1 : 0,
                Level = daily ? currentLevel : context.Level,
                StrategyLayer = entry.Rank,
                Scale = entry.Size,
                IsChallenge = !daily && isChallenge ? 1 : 0,
                PreType = string.IsNullOrEmpty(context.PreType)
                    ? "0"
                    : context.PreType
            };
        }

        public static Dictionary<string, object> BuildEnd(
            GameplayTrackingStartData start,
            MainGameTransitionData transition,
            string result,
            TrackerService tracker,
            GameStateService state)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (transition == null)
                throw new ArgumentNullException(nameof(transition));
            if (tracker == null) throw new ArgumentNullException(nameof(tracker));
            if (state == null) throw new ArgumentNullException(nameof(state));

            string gameType = start.GameType;
            int time = Math.Max(0, (int)transition.ElapsedSeconds);
            var values = new Dictionary<string, object>
            {
                ["qid"] = start.Qid,
                ["qrotate"] = start.QuestionRotation,
                ["result"] = result ?? string.Empty,
                ["game_type"] = gameType,
                ["diffi"] = start.Difficulty,
                ["level"] = start.Level,
                ["strategy_layer"] = start.StrategyLayer,
                ["scale"] = start.Scale,
                ["hint"] = state.GetToolCount("hint"),
                ["locate"] = state.GetToolCount("locate"),
                ["hint_used"] = tracker.GetStat("hint_used"),
                ["locate_used"] = tracker.GetStat("locate_used"),
                ["hint_used_total"] = state.GetGameTotalStat(
                    gameType, "hint_used_total"),
                ["locate_used_total"] = state.GetGameTotalStat(
                    gameType, "locate_used_total"),
                ["hint_apply_used"] = tracker.GetStat("hint_apply_used"),
                ["hint_stop_used"] = tracker.GetStat("hint_stop_used"),
                ["hint_detail_used"] = tracker.GetStat("hint_detail_used"),
                ["clear_used"] = tracker.GetStat("clear_used"),
                ["clear_used_total"] = state.GetGameTotalStat(
                    gameType, "clear_used_total"),
                ["draft_used_total"] = state.GetGameTotalStat(
                    gameType, "draft_used_total"),
                ["draft_time_total"] = state.GetGameTotalStat(
                    gameType, "draft_time_total_ms") / 1000,
                ["draft_error_total"] = state.GetGameTotalStat(
                    gameType, "draft_error_total"),
                ["draft_correct_total"] = state.GetGameTotalStat(
                    gameType, "draft_correct_total"),
                ["coord_count"] = tracker.GetStat("coord_count"),
                ["step_used"] = tracker.GetStat("step_used"),
                ["step_total"] = state.GetGameTotalStat(
                    gameType, "step_total"),
                ["gamedie_count"] = tracker.GetStat("gamedie_count"),
                ["restart_count"] = tracker.GetStat("restart_count"),
                ["time"] = time,
                ["time_total"] = state.GetGameTotalStat(
                    gameType, "time_total"),
                ["cross_count"] = transition.CrossCount,
                ["correct_cross_count"] = transition.CorrectCrossCount,
                ["false_cross_count"] = transition.FalseCrossCount,
                ["hint_cross_count"] = tracker.GetStat("hint_cross_count"),
                ["invalid_sign"] = transition.ErrorCount,
                ["invalid_sign_total"] = state.GetGameTotalStat(
                    gameType, "invalid_sign_total"),
                ["fail_sign"] = transition.RemainingCats,
                ["erase_count"] = tracker.GetStat("erase_count"),
                ["revive_count"] = state.GetGameTotalStat(
                    gameType, "revive_count"),
                ["rv_count"] = state.GetGameTotalStat(
                    gameType, "rv_count"),
                ["hp_count"] = transition.Lives,
                ["se_score"] = transition.FinalScore
            };
            if (!string.IsNullOrEmpty(start.PreType))
                values["pre_type"] = start.PreType;
            if (start.GameType == TrackerCatalog.GameType.Daily)
                values["percent"] = transition.DailyBeatPercent;
            return values;
        }

        public static string BuildQid(GameSessionSnapshotContext context)
        {
            if (context?.Entry == null) return string.Empty;
            LevelEntry entry = context.Entry;
            string source = string.IsNullOrEmpty(entry.BankSource)
                ? "regular"
                : entry.BankSource;
            string tier = string.IsNullOrEmpty(entry.BankTier)
                ? entry.Tier ?? string.Empty
                : entry.BankTier;
            int strategy = QidStrategy(entry.Rank, tier);
            int index = context.BankIndex;
            return string.Concat(
                entry.Size, "_", source, "_", strategy, "_",
                index, "_", entry.BankTransform);
        }

        public static int QidStrategy(int rank, string tier)
        {
            bool hard = string.Equals(tier, "H", StringComparison.Ordinal);
            if (rank == 4 && hard) return 5;
            if (rank == 5 && hard) return 7;
            if (rank == 5) return 6;
            return rank >= 1 ? rank : 0;
        }
    }
}
