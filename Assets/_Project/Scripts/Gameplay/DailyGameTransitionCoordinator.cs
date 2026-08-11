using System;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Daily;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// DailyGamePage terminal contract. Daily attempts share GameSession with
    /// the main game, but never mutate main-level retry, snapshot, DDA or
    /// progression state.
    /// </summary>
    public sealed class DailyGameTransitionCoordinator : IGameTransitionCoordinator
    {
        private readonly GameStateService _gameState;
        private bool _failSettled;
        private bool _winSettled;
        private bool _restartSettled;
        private bool _quitSettled;

        public DailyGameTransitionCoordinator(GameStateService gameState)
        {
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        }

        public bool TrySettleFail(
            GameSession session,
            GameSessionSnapshotContext context,
            out MainGameTransitionData transition)
        {
            transition = null;
            if (_failSettled || !IsDaily(context) || session == null ||
                session.State != GameSessionState.Failed)
                return false;

            _failSettled = true;
            _gameState.OnGameFinished();
            transition = Build(MainGameTransitionKind.Failed, session, context, null);
            return true;
        }

        public bool TryRevive(
            GameSession session,
            GameSessionSnapshotContext context,
            int livesToRestore,
            out MainGameTransitionData transition)
        {
            transition = null;
            if (!_failSettled || !IsDaily(context) || session == null ||
                !session.Revive(livesToRestore))
                return false;

            _failSettled = false;
            _gameState.IncrementGameTotalStat("daily", "revive_count");
            _gameState.IncrementGameTotalStat("daily", "rv_count");
            transition = Build(MainGameTransitionKind.Revived, session, context, null);
            return true;
        }

        public bool TrySettleWin(
            GameSession session,
            GameSessionSnapshotContext context,
            out MainGameTransitionData transition)
        {
            transition = null;
            if (_winSettled || !IsDaily(context) || session == null ||
                session.State != GameSessionState.Won)
                return false;

            _winSettled = true;
            _gameState.OnGameFinished();
            transition = Build(MainGameTransitionKind.Won, session, context, null);
            return true;
        }

        public bool TryRestart(
            GameSession session,
            GameSessionSnapshotContext context,
            out MainGameTransitionData transition)
        {
            transition = null;
            if (_restartSettled || !IsDaily(context) || session == null ||
                session.State != GameSessionState.Playing)
                return false;

            _restartSettled = true;
            _gameState.OnGameFinished();
            transition = Build(
                MainGameTransitionKind.Restart,
                session,
                context,
                FreshLaunch(context));
            transition.RestartCount = session.RestartCount + 1;
            return true;
        }

        public bool TryRestartAfterFail(
            GameSession session,
            GameSessionSnapshotContext context,
            out MainGameTransitionData transition)
        {
            transition = null;
            if (_restartSettled || !_failSettled || !IsDaily(context) ||
                session == null || session.State != GameSessionState.Failed)
                return false;

            _restartSettled = true;
            transition = Build(
                MainGameTransitionKind.Restart,
                session,
                context,
                FreshLaunch(context));
            transition.RestartCount = session.RestartCount + 1;
            return true;
        }

        public bool TryQuit(
            GameSession session,
            GameSessionSnapshotContext context,
            out MainGameTransitionData transition)
        {
            transition = null;
            if (_quitSettled || !IsDaily(context) || session == null ||
                session.State != GameSessionState.Playing)
                return false;

            _quitSettled = true;
            transition = Build(MainGameTransitionKind.Quit, session, context, null);
            return true;
        }

        private MainGameTransitionData Build(
            MainGameTransitionKind kind,
            GameSession session,
            GameSessionSnapshotContext context,
            Dictionary<string, object> retry)
        {
            return GameTransitionDataFactory.Build(
                kind,
                session,
                context,
                _gameState.CurrentLevel,
                retry);
        }

        private static Dictionary<string, object> FreshLaunch(
            GameSessionSnapshotContext context)
        {
            var result = context?.LaunchParameters != null
                ? new Dictionary<string, object>(context.LaunchParameters)
                : new Dictionary<string, object>();
            result["daily_mode"] = true;
            result["is_daily"] = true;
            result["daily_date"] = context?.DailyDate ?? string.Empty;
            result["daily_index"] = context?.DailyIndex ?? 0;
            result["prefill_positions"] = new List<object>();
            return result;
        }

        private static bool IsDaily(GameSessionSnapshotContext context)
        {
            return context != null &&
                   context.ResolvedMode == GameplaySessionMode.Daily;
        }
    }

    public static class DailyWinSettlement
    {
        public static bool Commit(
            GameStateService gameState,
            MainGameTransitionData transition,
            int elapsedSeconds,
            int rank,
            int size)
        {
            if (gameState == null || transition == null ||
                transition.Kind != MainGameTransitionKind.Won ||
                !transition.IsDailySession ||
                transition.DailyCompletionCommitted)
                return false;

            int elapsed = Math.Max(0, elapsedSeconds);
            transition.ElapsedSeconds = elapsed;
            transition.DailyBeatPercent = DailyStats.BeatPercent(
                elapsed,
                rank,
                size);
            if (string.IsNullOrEmpty(transition.DailyDate))
                return false;

            gameState.MarkDailyCompleted(
                transition.DailyDate,
                elapsed,
                transition.DailyBeatPercent);
            transition.DailyCompletionCommitted = true;
            return true;
        }
    }
}
