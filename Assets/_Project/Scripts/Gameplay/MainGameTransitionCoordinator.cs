using System;
using System.Collections.Generic;
using Meowdoku.Core;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    public enum MainGameTransitionKind
    {
        Failed,
        Revived,
        Won,
        Restart,
        Quit
    }

    public sealed class MainGameTransitionData
    {
        public MainGameTransitionKind Kind { get; internal set; }
        public int Level { get; internal set; }
        public int CurrentLevelAfter { get; internal set; }
        public int Lives { get; internal set; }
        public int RemainingCats { get; internal set; }
        public int MistakeCount { get; internal set; }
        public int ReviveCount { get; internal set; }
        public int RestartCount { get; internal set; }
        public int FinalScore { get; internal set; }
        public int MaxCombo { get; internal set; }
        public Dictionary<string, object> RetryParameters { get; internal set; } =
            new Dictionary<string, object>();
    }

    /// <summary>
    /// Builds the source-shaped retry cache without carrying board edits or the
    /// current PreCat placement into a fresh attempt.
    /// </summary>
    public static class GameRetryParameters
    {
        public static Dictionary<string, object> BuildInitial(GameSessionSnapshotContext context)
        {
            Dictionary<string, object> result = BuildFailure(context);
            result["bank_source"] = context.Entry.BankSource ?? "regular";
            return result;
        }

        public static Dictionary<string, object> BuildFailure(GameSessionSnapshotContext context)
        {
            if (context?.Entry == null) throw new ArgumentNullException(nameof(context));
            LevelEntry entry = context.Entry;
            bool identitySeed = entry.BankLk || entry.BankSp;
            var result = new Dictionary<string, object>
            {
                { "bank_mode", true },
                { "bank_size", entry.Size },
                { "bank_rank", entry.BankLk ? entry.MaxRank : entry.Rank },
                { "bank_index", context.BankIndex },
                { "prebuilt_regions", ToRows(entry.RegionMap) },
                { "prebuilt_solution", ToValues(entry.Solution) },
                { "level_seed", identitySeed ? entry.Id : entry.Seed },
                { "prefill_positions", RetryPrefills(context) },
                { "custom_color_map", ToValues(entry.ColorMap) },
                { "retry_level", context.Level },
                { "bank_source_main", entry.BankSourceMain ?? string.Empty },
                { "bank_tier", entry.BankTier ?? string.Empty }
            };

            if (!entry.FromBankBrowser) return result;
            result["from_bank_browser"] = true;
            result["bank_total"] = entry.BankTotal;
            if (entry.BankLk)
            {
                result["bank_lk"] = true;
                result["bank_lk_modified"] = entry.BankLkModified;
                return result;
            }

            AddStrategySteps(result, entry);
            result["bank_lk_style"] = entry.BankLkStyle;
            if (entry.BankSp)
            {
                result["bank_sp"] = true;
                return result;
            }

            result["bank_gc"] = entry.BankGc;
            result["bank_tier_h"] = entry.BankTierH;
            return result;
        }

        private static void AddStrategySteps(
            IDictionary<string, object> result,
            LevelEntry entry)
        {
            result["r1_steps"] = entry.R1Steps;
            result["r2_steps"] = entry.R2Steps;
            result["r3_steps"] = entry.R3Steps;
            result["r4_steps"] = entry.R4Steps;
            result["r5_steps"] = entry.R5Steps;
        }

        private static List<object> RetryPrefills(GameSessionSnapshotContext context)
        {
            var result = new List<object>();
            for (int index = 0; index < context.PrefillPositions.Count; index++)
            {
                Vector2Int position = context.PrefillPositions[index];
                if (context.PreCatPosition.x >= 0 && position == context.PreCatPosition)
                    continue;
                result.Add(new List<object> { position.x, position.y });
            }
            return result;
        }

        private static List<object> ToValues(int[] values)
        {
            var result = new List<object>();
            if (values == null) return result;
            for (int index = 0; index < values.Length; index++) result.Add(values[index]);
            return result;
        }

        private static List<object> ToRows(int[][] rows)
        {
            var result = new List<object>();
            if (rows == null) return result;
            for (int index = 0; index < rows.Length; index++) result.Add(ToValues(rows[index]));
            return result;
        }
    }

    /// <summary>
    /// P0 aggregate port of GamePage terminal callbacks and LevelOps. UI,
    /// tracker, animation and online rewards consume the emitted data outside.
    /// </summary>
    public sealed class MainGameTransitionCoordinator
    {
        private readonly GameStateService _gameState;
        private bool _failSettled;
        private bool _winSettled;
        private bool _restartSettled;
        private bool _quitSettled;

        public MainGameTransitionCoordinator(GameStateService gameState)
        {
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        }

        public bool TrySettleFail(
            GameSession session,
            GameSessionSnapshotContext context,
            out MainGameTransitionData transition)
        {
            transition = null;
            if (_failSettled || session == null || context == null ||
                session.State != GameSessionState.Failed)
                return false;

            _failSettled = true;
            _gameState.OnGameFinished();
            if (context.Level > 0) _gameState.OnLevelFailed(context.Level);
            Dictionary<string, object> retry = GameRetryParameters.BuildFailure(context);
            if (context.Level > 0) _gameState.SetRetryPuzzle(context.Level, retry);

            transition = Build(MainGameTransitionKind.Failed, session, context, retry);
            return true;
        }

        public bool TryRevive(
            GameSession session,
            GameSessionSnapshotContext context,
            int livesToRestore,
            out MainGameTransitionData transition)
        {
            transition = null;
            if (!_failSettled || session == null || context == null ||
                !session.Revive(livesToRestore))
                return false;

            _failSettled = false;
            _gameState.MarkDdaToolOrReviveUsed();
            _gameState.MarkDdaReviveUsed();
            _gameState.MarkPreCatRevived();
            _gameState.IncrementGameTotalStat("main", "revive_count");
            _gameState.IncrementGameTotalStat("main", "rv_count");
            _gameState.SetEndgameSnapshot(GameSessionSnapshot.Build(session, context));

            transition = Build(MainGameTransitionKind.Revived, session, context, null);
            return true;
        }

        public bool TrySettleWin(
            GameSession session,
            GameSessionSnapshotContext context,
            out MainGameTransitionData transition)
        {
            transition = null;
            if (_winSettled || session == null || context == null ||
                session.State != GameSessionState.Won)
                return false;

            _winSettled = true;
            _gameState.OnGameFinished();
            if (context.Level > 0) _gameState.OnLevelWon(context.Level);
            _gameState.ClearEndgameSnapshot();

            transition = Build(MainGameTransitionKind.Won, session, context, null);
            return true;
        }

        public bool TryRestart(
            GameSession session,
            GameSessionSnapshotContext context,
            out MainGameTransitionData transition)
        {
            transition = null;
            if (_restartSettled || session == null || context == null ||
                session.State != GameSessionState.Playing)
                return false;

            _restartSettled = true;
            _gameState.ClearEndgameSnapshot();
            _gameState.OnGameFinished();
            if (context.Level > 0) _gameState.OnLevelFailed(context.Level);
            Dictionary<string, object> retry = GameRetryParameters.BuildFailure(context);

            transition = Build(MainGameTransitionKind.Restart, session, context, retry);
            transition.RestartCount = session.RestartCount + 1;
            return true;
        }

        public bool TryQuit(
            GameSession session,
            GameSessionSnapshotContext context,
            out MainGameTransitionData transition)
        {
            transition = null;
            if (_quitSettled || session == null || context == null ||
                session.State != GameSessionState.Playing)
                return false;

            _quitSettled = true;
            if (context.Level > 0) _gameState.MarkCurrentLevelDirty();
            _gameState.SetEndgameSnapshot(GameSessionSnapshot.Build(session, context));
            transition = Build(MainGameTransitionKind.Quit, session, context, null);
            return true;
        }

        private MainGameTransitionData Build(
            MainGameTransitionKind kind,
            GameSession session,
            GameSessionSnapshotContext context,
            Dictionary<string, object> retry)
        {
            return new MainGameTransitionData
            {
                Kind = kind,
                Level = context.Level,
                CurrentLevelAfter = _gameState.CurrentLevel,
                Lives = session.Lives,
                RemainingCats = session.RemainingCats,
                MistakeCount = session.MistakeCount,
                ReviveCount = session.ReviveCount,
                RestartCount = session.RestartCount,
                FinalScore = session.Score.Score,
                MaxCombo = session.Score.MaxCombo,
                RetryParameters = retry ?? new Dictionary<string, object>()
            };
        }
    }
}
