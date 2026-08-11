using System;
using System.Collections;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using UnityEngine;

namespace Meowdoku.Core.Online
{
    public interface IDataSyncConsumer
    {
        void BindDataSyncRuntime(DataSyncRuntime runtime);
    }

    [DisallowMultipleComponent]
    public sealed class DataSyncRuntime : MonoBehaviour
    {
        [SerializeField] private AuthRuntime authRuntime;
        [SerializeField] private MonoBehaviour apiAdapter;
        [SerializeField] private DailyMetaRuntime dailyMetaRuntime;
        [SerializeField] private ProfileRuntime profileRuntime;
        [SerializeField] private RankActivityRuntime rankActivityRuntime;

        private DataSyncRegistry _registry;
        private DataSyncService _service;
        private IDataSyncEnableStore _enableStore;
        private GameStateService _gameState;
        private ProfileService _profile;
        private StreakFeature _streak;
        private RankActivityManager _rank;
        private Coroutine _syncRoutine;
        private bool _subscribed;
        private bool _syncEnabled;
        private bool _syncing;
        private bool _syncRequested;
        private bool _startupResolved;

        public event Action<bool> DataSyncCompleted;
        public event Action<string> DataSyncFailed;

        public DataSyncService Service
        {
            get
            {
                EnsureInitialized();
                return _service;
            }
        }

        public bool IsSyncEnabled
        {
            get
            {
                EnsureInitialized();
                return _syncEnabled;
            }
        }

        public bool IsStartupAvailable =>
            authRuntime != null && authRuntime.Service.IsAvailable;
        public bool IsStartupResolved => _startupResolved;
        public bool IsSyncing => _syncing;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            Subscribe();
            if (_syncEnabled && authRuntime != null &&
                authRuntime.Service.IsLoggedIn)
                RequestSync("startup");
            else if (authRuntime != null &&
                    authRuntime.Service.HasLoginAttemptCompleted)
                _startupResolved = true;
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (_syncRoutine != null)
            {
                StopCoroutine(_syncRoutine);
                _syncRoutine = null;
            }
            if (apiAdapter is IDataSyncCancelableApi cancelable)
                cancelable.CancelAll();
            _syncing = false;
            _syncRequested = false;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _registry?.Clear();
            DataSyncCompleted = null;
            DataSyncFailed = null;
        }

        public void BindDependencies(
            AuthRuntime auth,
            MonoBehaviour api,
            DailyMetaRuntime daily,
            ProfileRuntime profile,
            RankActivityRuntime rank)
        {
            if (_service != null)
                throw new InvalidOperationException(
                    "DataSyncRuntime dependencies are immutable after initialization.");
            authRuntime = auth;
            apiAdapter = api;
            dailyMetaRuntime = daily;
            profileRuntime = profile;
            rankActivityRuntime = rank;
        }

        public void SetSyncEnabled(bool enabled)
        {
            EnsureInitialized();
            _syncEnabled = enabled;
            _enableStore?.Save(enabled);
            if (!enabled)
            {
                _startupResolved = true;
                return;
            }
            _startupResolved = false;
            if (authRuntime != null && authRuntime.Service.IsLoggedIn)
                RequestSync("sync_enabled");
        }

        public void RequestSync(string reason)
        {
            EnsureInitialized();
            if (!_syncEnabled || !isActiveAndEnabled) return;
            if (_syncing)
            {
                _syncRequested = true;
                return;
            }
            _syncRoutine = StartCoroutine(RunSync(
                reason ?? string.Empty));
        }

        public IEnumerator AwaitStartup(float maximumSeconds)
        {
            EnsureInitialized();
            if (!_syncEnabled || !IsStartupAvailable) yield break;
            float deadline = Time.realtimeSinceStartup +
                             Mathf.Max(0f, maximumSeconds);
            while (!_startupResolved &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        private IEnumerator RunSync(string reason)
        {
            _syncing = true;
            DataSyncOutcome outcome = default;
            yield return _service.Synchronize(
                reason,
                value => outcome = value);
            _syncing = false;
            _syncRoutine = null;
            _startupResolved = true;
            _registry.MarkSynced();
            if (outcome.Succeeded)
                DataSyncCompleted?.Invoke(outcome.Changed);
            else
                DataSyncFailed?.Invoke(outcome.Reason);

            if (_syncRequested)
            {
                _syncRequested = false;
                RequestSync("coalesced");
            }
        }

        private void EnsureInitialized()
        {
            if (_service != null) return;
            _registry = new DataSyncRegistry();
            _registry.SetLateHandler(HandleLateSavable);
            _gameState = GameStateRuntime.Current;
            _profile = profileRuntime != null
                ? profileRuntime.Service
                : null;
            _streak = dailyMetaRuntime != null
                ? dailyMetaRuntime.Streak
                : null;
            _rank = rankActivityRuntime != null
                ? rankActivityRuntime.Manager
                : null;
            _registry.Register(_gameState);
            if (_profile != null) _registry.Register(_profile);
            if (_streak != null) _registry.Register(_streak);
            if (_rank != null) _registry.Register(_rank);

            IDataSyncAuthGateway auth = authRuntime != null
                ? new AuthDataSyncGateway(authRuntime.Service)
                : new UnavailableAuthGateway();
            IDataSyncApi api = apiAdapter as IDataSyncApi ??
                                NullDataSyncApi.Instance;
            _service = new DataSyncService(
                auth,
                api,
                _registry,
                DataSyncSnapshotRepository.CreateDefault());
            _enableStore = DataSyncEnableRepository.CreateDefault();
            _syncEnabled = ResolveSyncEnabled(_enableStore);
            _startupResolved = !_syncEnabled;
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            if (authRuntime != null)
            {
                authRuntime.Service.LoginSucceeded += HandleLoginSucceeded;
                authRuntime.Service.LoginFailed += HandleLoginFailed;
            }
            if (_gameState != null)
                _gameState.LevelSettled += HandleLevelSettled;
            if (_profile != null)
                _profile.ProfileSaved += HandleProfileSaved;
            if (_streak != null)
                _streak.RemoteSyncRequested += HandleRemoteSyncRequested;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (authRuntime != null)
            {
                authRuntime.Service.LoginSucceeded -= HandleLoginSucceeded;
                authRuntime.Service.LoginFailed -= HandleLoginFailed;
            }
            if (_gameState != null)
                _gameState.LevelSettled -= HandleLevelSettled;
            if (_profile != null)
                _profile.ProfileSaved -= HandleProfileSaved;
            if (_streak != null)
                _streak.RemoteSyncRequested -= HandleRemoteSyncRequested;
            _subscribed = false;
        }

        private void HandleLoginSucceeded(AuthLoginResult _)
        {
            RequestSync("startup");
        }

        private void HandleLoginFailed(int code, string message)
        {
            _startupResolved = true;
        }

        private void HandleLevelSettled(bool won)
        {
            RequestSync(won ? "level_won" : "level_failed");
        }

        private void HandleProfileSaved()
        {
            RequestSync("profile_save");
        }

        private void HandleRemoteSyncRequested(string reason)
        {
            RequestSync(reason);
        }

        private void HandleLateSavable(IDataSyncSavable savable)
        {
            RequestSync($"late_savable_{savable.RemoteSaveId}");
        }

        private static bool ResolveSyncEnabled(IDataSyncEnableStore store)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return store != null && store.TryLoad(out bool enabled) && enabled;
#else
            return true;
#endif
        }

        private sealed class UnavailableAuthGateway : IDataSyncAuthGateway
        {
            public bool IsAvailable => false;
            public bool IsLoggedIn => false;
            public IEnumerator RequestAccessToken(
                bool forceRefresh,
                Action<AuthTokenResult> completed)
            {
                completed?.Invoke(new AuthTokenResult(
                    string.Empty,
                    AuthErrorCode.PluginUnavailable,
                    "AuthPlugin unavailable"));
                yield break;
            }
        }
    }
}
