using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Config
{
    public interface IAbRuntimeProvider : IAbValueProvider, IAbDyeSink
    {
        event Action Initialized;
        event Action RemoteReady;
        event Action<string> ParamsUpdated;
        bool IsInitialized { get; }
        bool IsRemoteReady { get; }
        long FirstOpenUnixMilliseconds { get; }
    }

    public sealed class OfflineAbRuntimeProvider : IAbRuntimeProvider
    {
        public static readonly OfflineAbRuntimeProvider Instance = new();
        private OfflineAbRuntimeProvider() { }

        public event Action Initialized { add { } remove { } }
        public event Action RemoteReady { add { } remove { } }
        public event Action<string> ParamsUpdated { add { } remove { } }
        public bool IsInitialized => true;
        public bool IsRemoteReady => true;
        public long FirstOpenUnixMilliseconds => 0;
        public int GetInt(string key, int defaultValue) => defaultValue;
        public string GetString(string key, string defaultValue) => defaultValue;
        public void Dye(string key) { }
    }

    /// <summary>
    /// Provider-neutral port of abtest_manager.gd timing reloads. It does not
    /// emulate remote bucketing; a production SDK adapter remains the owner of
    /// remote value selection.
    /// </summary>
    public sealed class AbConfigService : IDisposable
    {
        private readonly IAbRuntimeProvider _provider;
        private readonly IReadOnlyList<IAbConfig> _configs;
        private bool _initialized;
        private bool _disposed;

        public AbConfigService(
            IAbRuntimeProvider provider,
            IReadOnlyList<IAbConfig> configs)
        {
            _provider = provider ?? OfflineAbRuntimeProvider.Instance;
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
        }

        public event Action ProviderReady;
        public event Action<string> ParamsUpdated;
        public bool IsRemoteReady { get; private set; }
        public bool IsAppStartFinalized { get; private set; }
        public IAbRuntimeProvider Provider => _provider;

        public void Initialize()
        {
            if (_initialized || _disposed) return;
            _initialized = true;
            _provider.Initialized += HandleInitialized;
            _provider.RemoteReady += HandleRemoteReady;
            _provider.ParamsUpdated += HandleParamsUpdated;
            if (_provider.IsInitialized) HandleInitialized();
            if (_provider.IsRemoteReady) HandleRemoteReady();
        }

        public void ReloadTiming(string timing)
        {
            if (_disposed || string.IsNullOrEmpty(timing)) return;
            for (int i = 0; i < _configs.Count; i++)
            {
                IAbConfig config = _configs[i];
                if (config != null && config.Timing == timing)
                    config.ReloadValue(_provider);
            }
        }

        public void FinalizeRemoteFallback()
        {
            if (_disposed) return;
            ProviderReady?.Invoke();
            FinalizeAppStart();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _provider.Initialized -= HandleInitialized;
            _provider.RemoteReady -= HandleRemoteReady;
            _provider.ParamsUpdated -= HandleParamsUpdated;
            ProviderReady = null;
            ParamsUpdated = null;
        }

        private void HandleInitialized()
        {
            if (_disposed) return;
            ProviderReady?.Invoke();
            FinalizeAppStart();
        }

        private void HandleRemoteReady()
        {
            if (_disposed) return;
            IsRemoteReady = true;
            ProviderReady?.Invoke();
            FinalizeAppStart();
        }

        private void HandleParamsUpdated(string updateType)
        {
            if (!_disposed) ParamsUpdated?.Invoke(updateType ?? string.Empty);
        }

        private void FinalizeAppStart()
        {
            if (IsAppStartFinalized) return;
            IsRemoteReady = true;
            IsAppStartFinalized = true;
            ReloadTiming(AbConfigTiming.AppStart);
        }
    }

    public sealed class AdConfigSet
    {
        private readonly IAbConfig[] _all;

        public AdConfigSet()
        {
            _all = new IAbConfig[]
            {
                LivingDays,
                InterUnlockLevel,
                InterUnlockSession,
                InterUnlockMemory,
                InterCooldown,
                InterExtraProtection,
                BannerUnlockSession,
                BannerUnlockLevel,
                BannerExtraProtection,
                BannerUnlockDifficulty,
                CommonRewardLogic
            };
        }

        public LivingDaysConfig LivingDays { get; } = new();
        public InterUnlockLevelConfig InterUnlockLevel { get; } = new();
        public InterUnlockSessionConfig InterUnlockSession { get; } = new();
        public InterUnlockMemoryConfig InterUnlockMemory { get; } = new();
        public InterCdLcConfig InterCooldown { get; } = new();
        public InterExtraProtectLcConfig InterExtraProtection { get; } = new();
        public BannerUnlockSessionConfig BannerUnlockSession { get; } = new();
        public BannerUnlockLevelConfig BannerUnlockLevel { get; } = new();
        public BannerExtraProtectLcConfig BannerExtraProtection { get; } = new();
        public BannerUnlockDiffLcConfig BannerUnlockDifficulty { get; } = new();
        public CommonRewardAdLogicConfig CommonRewardLogic { get; } = new();
        public IReadOnlyList<IAbConfig> All => _all;
    }
}
