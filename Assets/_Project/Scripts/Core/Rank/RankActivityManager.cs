using System;
using System.Collections.Generic;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Online;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Robot;

namespace Meowdoku.Core.Rank
{
    public interface IRankActivityEnvironment
    {
        bool LeaderboardEnabled { get; }
        int LeaderboardGroup { get; }
        int CurrentLevel { get; }
    }

    public sealed class DisabledRankActivityEnvironment :
        IRankActivityEnvironment
    {
        public static readonly DisabledRankActivityEnvironment Instance = new();
        private DisabledRankActivityEnvironment() { }
        public bool LeaderboardEnabled => false;
        public int LeaderboardGroup => 0;
        public int CurrentLevel => 0;
    }

    /// <summary>
    /// Pure source-shaped RankActivity state machine. UI, trackers and the
    /// one-second ticker subscribe at the runtime boundary instead of being
    /// hidden global dependencies.
    /// </summary>
    public sealed class RankActivityManager : IDataSyncSavable
    {
        public const long ZeroScoreTieUnix = 253402300799L;
        public const string RewardReason = "challenge_get_dlg";
        public const string BonusRewardReason = "challenge_reward_get_dlg";

        private readonly IRankActivityStore _store;
        private readonly RobotService _robots;
        private readonly ProfileService _profile;
        private readonly AwardManager _awards;
        private readonly IRobotTimeProvider _time;
        private readonly IRobotRandomFactory _randomFactory;
        private readonly IRankActivityEnvironment _environment;

        private RankActivityData _data;
        private bool _inLevel;
        private bool _newSession = true;
        private RankSettlementResult _lastSettleResult;
        private int _lastWinAdvance;
        private bool _lastWinScored;
        private int _lastWinIncrement;
        private bool _rewardPresenting;
        private bool _rewardAtHome = true;

        public RankActivityManager(
            IRankActivityStore store,
            RobotService robots,
            ProfileService profile,
            AwardManager awards,
            IRankActivityEnvironment environment = null,
            IRobotTimeProvider time = null,
            IRobotRandomFactory randomFactory = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _robots = robots ?? throw new ArgumentNullException(nameof(robots));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _awards = awards ?? throw new ArgumentNullException(nameof(awards));
            _environment = environment ??
                           DisabledRankActivityEnvironment.Instance;
            _time = time ?? new SystemRobotTimeProvider();
            _randomFactory = randomFactory ?? new SystemRobotRandomFactory();
            _data = _store.Load() ?? new RankActivityData();
            ReconcileExpiry();
            ReconcileInterruptedReward();
        }

        public event Action<RankActivityState> StateChanged;
        public event Action RankingChanged;
        public event Action<int, bool> TimeTicked;
        public event Action<RankSettlementResult> SettleReady;
        public event Action<int> PeriodOpened;

        public RankActivityState State => _data.State;
        public bool IsRunning =>
            _data.State == RankActivityState.OpenNotJoined ||
            _data.State == RankActivityState.OpenJoined;
        public bool IsJoined => _data.Joined;
        public bool IsOpenNotJoined =>
            _data.State == RankActivityState.OpenNotJoined;
        public int LastWinAdvance => _lastWinAdvance;
        public bool DidLastWinScore => _lastWinScored;
        public int LastWinIncrement => _lastWinIncrement;
        public int CollectTotal => _data.CollectTotal;
        public int PeriodCount => _data.PeriodCount;
        public int Group => _data.Group;
        public int RemainingSeconds => RankActivityPeriod.RemainingSeconds(
            _time.UnixNow,
            _data.EndUnix);

#if UNITY_INCLUDE_TESTS
        internal int LevelCacheForTests => _data.LevelCache;
        internal bool IsLevelCacheActiveForTests => _data.LevelCacheActive;
        internal bool IsInLevelForTests => _inLevel;
#endif

        public List<RobotRankEntry> GetRanking()
        {
            return string.IsNullOrEmpty(_data.RobotKey)
                ? new List<RobotRankEntry>()
                : _robots.GetRanking(
                    _data.RobotKey,
                    _data.CollectTotal,
                    PlayerUnix(),
                    dropZeroRobots: true);
        }

        public List<RankInfo> GetRankInfos()
        {
            return string.IsNullOrEmpty(_data.RobotKey)
                ? new List<RankInfo>()
                : _robots.GetRankList(
                    _data.RobotKey,
                    _profile.GetPlayerInfo(),
                    _data.CollectTotal,
                    PlayerUnix(),
                    dropZeroRobots: true);
        }

        public int GetPlayerRank()
        {
            return string.IsNullOrEmpty(_data.RobotKey)
                ? -1
                : _robots.GetPlayerRank(
                    _data.RobotKey,
                    _data.CollectTotal,
                    PlayerUnix(),
                    dropZeroRobots: true);
        }

        public int GetRankForScore(int score)
        {
            return string.IsNullOrEmpty(_data.RobotKey)
                ? -1
                : _robots.GetPlayerRank(
                    _data.RobotKey,
                    score,
                    PlayerUnix(),
                    dropZeroRobots: true);
        }

        public RankProgressEncouragement ConsumeProgressEncouragement()
        {
            int rank = GetPlayerRank();
            if (rank >= 1 && rank <= 3 &&
                (_data.BestEncouragedRank == 0 ||
                 rank < _data.BestEncouragedRank))
            {
                _data.BestEncouragedRank = rank;
                Save();
                return new RankProgressEncouragement(
                    RankEncouragementKind.Reach,
                    rank);
            }
            return _lastWinAdvance >= 3
                ? new RankProgressEncouragement(
                    RankEncouragementKind.Climb,
                    advance: _lastWinAdvance)
                : new RankProgressEncouragement(RankEncouragementKind.None);
        }

        public RankSettlementResult GetPendingReward()
        {
            if (_data.State != RankActivityState.Settling ||
                !_data.Settled ||
                _data.RewardClaimed ||
                !RankActivityConfig.HasReward(
                    _data.Group,
                    _data.FinalRank))
                return null;
            return (_lastSettleResult ?? BuildSettlementResult(
                _data.FinalRank,
                true)).Clone();
        }

        public bool HasHomeEntry =>
            IsRunning ||
            (_data.State == RankActivityState.Settling &&
             GetPendingReward() != null);

        public bool MaybeOpen(bool atHome)
        {
            bool newSession = _newSession;
            _newSession = false;
            if (_data.State != RankActivityState.NotOpened ||
                !_environment.LeaderboardEnabled)
                return false;
            if (!RankActivityPeriod.ShouldOpen(
                    _data.PeriodCount,
                    _environment.CurrentLevel,
                    _data.PreviousAwarded,
                    _data.WinsSinceClose,
                    atHome,
                    newSession,
                    RankActivityConfig.UnlockLevel,
                    RankActivityConfig.ReopenWins))
                return false;
            OpenPeriod(_environment.LeaderboardGroup);
            return true;
        }

        public void NotifyNewSession() => _newSession = true;

        public void ConfirmParticipation()
        {
            if (_data.State != RankActivityState.OpenNotJoined) return;
            _data.Joined = true;
            SetState(RankActivityState.OpenJoined);
        }

        public void NotifyLevelStart() => _inLevel = true;

        public void SetLevelCollect(int count)
        {
            if (_data.State == RankActivityState.NotOpened) return;
            _data.LevelCache = Math.Max(0, count);
            _data.LevelCacheActive = true;
            Save();
        }

        public void NotifyLevelRestart()
        {
            _data.LevelCache = 0;
            _data.LevelCacheActive =
                _data.State != RankActivityState.NotOpened;
            Save();
        }

        public void NotifyLevelWin()
        {
            _inLevel = false;
            RankActivityState state = _data.State;
            bool wasSettled = _data.Settled;
            int rankBefore = GetPlayerRank();
            int totalBefore = _data.CollectTotal;
            bool settlingPending =
                state == RankActivityState.Settling && !wasSettled;

            if (_data.Joined &&
                (state == RankActivityState.OpenJoined || settlingPending))
                CommitLevelCache();
            else
                ClearLevelCache();

            if (settlingPending)
            {
                CaptureLastWin(rankBefore, totalBefore);
                RankingChanged?.Invoke();
                Settle(true);
            }
            else if (state == RankActivityState.Settling)
            {
                _lastWinAdvance = 0;
                _lastWinScored = false;
                _lastWinIncrement = 0;
            }
            else if (state == RankActivityState.NotOpened)
            {
                _data.WinsSinceClose++;
                Save();
                MaybeOpen(false);
            }
            else
            {
                Save();
                CaptureLastWin(rankBefore, totalBefore);
                RankingChanged?.Invoke();
            }
        }

        public void NotifyLevelExit()
        {
            _inLevel = false;
            ClearLevelCache();
            if (_data.State == RankActivityState.Settling)
                Settle();
            else
                Save();
        }

        public bool OnHomeShown()
        {
            if (!_environment.LeaderboardEnabled)
            {
                if (HasActiveData()) ResetData();
                return false;
            }
            if (_data.State == RankActivityState.Settling &&
                _data.Settled && GetPendingReward() == null)
                NotifySettlementDone();
            MaybeRunSettle();
            return MaybeOpen(true);
        }

        public int ClaimReward(bool atHome = true)
        {
            if (_rewardPresenting ||
                _data.State != RankActivityState.Settling ||
                !_data.Settled ||
                _data.RewardClaimed)
                return -1;
            _rewardAtHome = atHome;
            List<AwardItem> items = RankActivityConfig.RewardItems(
                _data.Group,
                _data.FinalRank,
                CreateRandom());
            if (items.Count == 0)
            {
                _data.RewardClaimed = true;
                FoldToNotOpened();
                return -1;
            }

            _rewardPresenting = true;
            int uid = _awards.Dispatch(
                items,
                AwardDisplayType.RankGift,
                RewardReason,
                BonusRewardReason);
            if (uid < 0)
            {
                _rewardPresenting = false;
                return -1;
            }
            _data.RewardClaimed = true;
            Save();

            var top3 = new List<object>();
            List<RankInfo> infos = GetRankInfos();
            for (int index = 0; index < Math.Min(3, infos.Count); index++)
                if (infos[index]?.PlayerInfo != null)
                    top3.Add(infos[index].PlayerInfo);
            int place = _data.FinalRank;
            int winCount = PlaceWinCountAfterClaim(place);
            _awards.ShowAward(uid, new Dictionary<string, object>
            {
                ["top3_infos"] = top3,
                ["place"] = place,
                ["win_count"] = winCount,
                ["group"] = _data.Group
            });
            _awards.ContinueWhenAwardEnd(uid, OnRewardAwardEnd);
            return uid;
        }

        public void NotifySettlementDone()
        {
            if (_data.State != RankActivityState.Settling || !_data.Settled ||
                RankActivityConfig.HasReward(
                    _data.Group,
                    _data.FinalRank))
                return;
            _data.RewardClaimed = true;
            FoldToNotOpened();
        }

        public void Tick()
        {
            if (!IsRunning) return;
            long now = _time.UnixNow;
            if (RankActivityPeriod.IsExpired(now, _data.EndUnix))
            {
                SetState(RankActivityState.Settling);
                TimeTicked?.Invoke(0, true);
                MaybeRunSettle();
            }
            else
            {
                TimeTicked?.Invoke(
                    RankActivityPeriod.RemainingSeconds(
                        now,
                        _data.EndUnix),
                    false);
            }
        }

        public void ResetData()
        {
            if (!string.IsNullOrEmpty(_data.RobotKey))
                _robots.DiscardPool(_data.RobotKey);
            _data = new RankActivityData();
            _lastSettleResult = null;
            Save();
            StateChanged?.Invoke(_data.State);
        }

        public string RemoteSaveId => "rank";

        public Dictionary<string, object> ExportRemote() => new()
        {
            ["period_count"] = _data.PeriodCount
        };

        public bool MergeRemote(
            IReadOnlyDictionary<string, object> remote,
            bool remoteAhead)
        {
            if (!remoteAhead || remote == null) return false;
            _data.PeriodCount = RankValue.Int(remote, "period_count");
            Save();
            return true;
        }

        public bool MergeRemote(
            IReadOnlyDictionary<string, object> remote,
            DataSyncMergeContext context)
        {
            return MergeRemote(remote, context.RemoteAhead);
        }

        private void OpenPeriod(int group)
        {
            RobotConfig config = RankActivityConfig.BuildRobotConfig(group);
            long now = _time.UnixNow;
            long end = RankActivityPeriod.ComputeEnd(
                now,
                RankActivityConfig.PeriodDurationSeconds);
            string key = _robots.CreatePool(
                config,
                _data.PreviousScore,
                _data.PreviousRank,
                _profile.GetAvatarIds(),
                _profile.GetFrameIds(),
                ProfileCatalog.FirstPlaceFrameId,
                RobotNicknameCatalog.Names,
                _data.PeriodCount + 1,
                end);

            _data.Group = group;
            _data.StartUnix = now;
            _data.EndUnix = end;
            _data.RobotKey = key;
            _data.Joined = false;
            _data.CollectTotal = 0;
            _data.PlayerScoreUnix = 0;
            _data.Settled = false;
            _data.FinalRank = 0;
            _data.RewardClaimed = false;
            _data.WinsSinceClose = 0;
            _data.LevelCache = 0;
            _data.LevelCacheActive = false;
            _data.BestEncouragedRank = 0;
            _data.PeriodCount++;
            _lastSettleResult = null;
            SetState(RankActivityState.OpenNotJoined);
            PeriodOpened?.Invoke(_data.PeriodCount);
        }

        private void Settle(bool deferFold = false)
        {
            if (_data.Settled) return;
            int rank = 0;
            bool awarded = false;
            if (_data.Joined && !string.IsNullOrEmpty(_data.RobotKey))
            {
                rank = _robots.GetPlayerRank(
                    _data.RobotKey,
                    _data.CollectTotal,
                    PlayerUnix(),
                    _data.EndUnix);
                awarded = RankActivityConfig.HasReward(_data.Group, rank);
            }
            _data.FinalRank = rank;
            _data.Settled = true;
            _data.PreviousScore = _data.CollectTotal;
            _data.PreviousRank = rank;
            _data.PreviousAwarded = awarded;
            Save();

            _lastSettleResult = BuildSettlementResult(rank, awarded);
            SettleReady?.Invoke(_lastSettleResult.Clone());
            if (!awarded)
            {
                if (!deferFold) FoldToNotOpened();
            }
            else
            {
                StateChanged?.Invoke(_data.State);
            }
        }

        private void MaybeRunSettle()
        {
            if (_data.State == RankActivityState.Settling &&
                !_data.Settled && !_inLevel)
                Settle();
        }

        private void FoldToNotOpened()
        {
            if (!string.IsNullOrEmpty(_data.RobotKey))
            {
                _robots.DiscardPool(_data.RobotKey);
                _data.RobotKey = string.Empty;
            }
            _data.WinsSinceClose = 0;
            _data.LevelCache = 0;
            _data.LevelCacheActive = false;
            SetState(RankActivityState.NotOpened);
        }

        private void CommitLevelCache()
        {
            if (!_data.LevelCacheActive) return;
            int committed = _data.LevelCache;
            _data.CollectTotal += committed;
            _data.LevelCache = 0;
            _data.LevelCacheActive = false;
            if (committed > 0) _data.PlayerScoreUnix = _time.UnixNow;
            if (!string.IsNullOrEmpty(_data.RobotKey))
                _robots.OnPlayerScore(
                    _data.RobotKey,
                    _data.CollectTotal,
                    PlayerUnix());
        }

        private void ClearLevelCache()
        {
            _data.LevelCache = 0;
            _data.LevelCacheActive = false;
        }

        private void CaptureLastWin(int rankBefore, int totalBefore)
        {
            _lastWinAdvance = Math.Max(0, rankBefore - GetPlayerRank());
            _lastWinScored = _data.CollectTotal > totalBefore;
            _lastWinIncrement = Math.Max(
                0,
                _data.CollectTotal - totalBefore);
        }

        private int PlaceWinCountAfterClaim(int place)
        {
            switch (place)
            {
                case 1:
                    return Math.Max(
                        0,
                        _profile.GetFrameCount(
                            ProfileCatalog.FirstPlaceFrameId)) + 1;
                case 2:
                    _data.Place2Wins++;
                    Save();
                    return _data.Place2Wins;
                case 3:
                    _data.Place3Wins++;
                    Save();
                    return _data.Place3Wins;
                default:
                    return 0;
            }
        }

        private void OnRewardAwardEnd(int uid)
        {
            _rewardPresenting = false;
            if (_data.State != RankActivityState.Settling) return;
            FoldToNotOpened();
            MaybeOpen(_rewardAtHome);
        }

        private void ReconcileExpiry()
        {
            if ((_data.State == RankActivityState.OpenNotJoined ||
                 _data.State == RankActivityState.OpenJoined) &&
                RankActivityPeriod.IsExpired(
                    _time.UnixNow,
                    _data.EndUnix))
            {
                _data.State = RankActivityState.Settling;
                Save();
            }
            MaybeRunSettle();
        }

        private void ReconcileInterruptedReward()
        {
            if (_data.State == RankActivityState.Settling &&
                _data.Settled && _data.RewardClaimed)
                FoldToNotOpened();
        }

        private void SetState(RankActivityState state)
        {
            _data.State = state;
            Save();
            StateChanged?.Invoke(state);
        }

        private bool HasActiveData() =>
            _data.PeriodCount > 0 ||
            _data.State != RankActivityState.NotOpened ||
            !string.IsNullOrEmpty(_data.RobotKey);

        private long PlayerUnix()
        {
            if (_data.CollectTotal <= 0) return ZeroScoreTieUnix;
            return _data.PlayerScoreUnix > 0
                ? _data.PlayerScoreUnix
                : _data.StartUnix;
        }

        private RankSettlementResult BuildSettlementResult(
            int rank,
            bool awarded)
        {
            return new RankSettlementResult
            {
                Rank = rank,
                Awarded = awarded,
                IsFirst = RankActivityConfig.IsFirstPlace(rank),
                CollectTotal = _data.CollectTotal,
                Group = _data.Group
            };
        }

        private IRobotRandom CreateRandom()
        {
            return _randomFactory.Create() ??
                   throw new InvalidOperationException(
                       "Rank random factory returned null.");
        }

        private void Save() => _store.Save(_data);
    }
}
