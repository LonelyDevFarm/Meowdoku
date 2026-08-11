using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core.Config
{
    public interface IAbConfigRuntimeConsumer
    {
        void BindAbConfigRuntime(AbConfigRuntime runtime);
    }

    [DisallowMultipleComponent]
    public sealed class AbConfigRuntime : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour providerAdapter;

        private readonly AdConfigSet _adConfigs = new();
        private readonly SettingsConfigSet _settingsConfigs = new();
        private readonly HomeConfigSet _homeConfigs = new();
        private readonly PlatformConfigSet _platformConfigs = new();
        private AbConfigService _service;
        private GameStateService _gameState;

        public event Action<string> ParamsUpdated;
        public AdConfigSet Ads => _adConfigs;
        public SettingsConfigSet Settings => _settingsConfigs;
        public HomeConfigSet Home => _homeConfigs;
        public PlatformConfigSet Platform => _platformConfigs;
        public IAbValueProvider ValueProvider
        {
            get
            {
                Initialize(_gameState ?? GameStateRuntime.Current);
                return _service?.Provider ?? OfflineAbRuntimeProvider.Instance;
            }
        }
        public bool IsRemoteReady => _service?.IsRemoteReady == true;
        public bool IsAppStartFinalized =>
            _service?.IsAppStartFinalized == true;
        public long FirstOpenUnixMilliseconds =>
            (_gameState ?? GameStateRuntime.Current).Data.FirstOpenTimeMs;

        private void Awake()
        {
            Initialize(GameStateRuntime.Current);
        }

        public void Initialize(GameStateService gameState)
        {
            _gameState = gameState ?? GameStateRuntime.Current;
            if (_service != null) return;
            IAbRuntimeProvider provider =
                providerAdapter as IAbRuntimeProvider ??
                OfflineAbRuntimeProvider.Instance;
            _service = new AbConfigService(provider, BuildConfigCatalog());
            _service.ProviderReady += EnsureFirstOpenTime;
            _service.ParamsUpdated += HandleParamsUpdated;
            if (provider.IsInitialized || provider.IsRemoteReady)
                EnsureFirstOpenTime();
            _service.Initialize();
        }

        public IEnumerator AwaitRemoteReady(float maximumSeconds = 2f)
        {
            Initialize(_gameState ?? GameStateRuntime.Current);
            float deadline = Time.realtimeSinceStartup +
                             Mathf.Max(0f, maximumSeconds);
            while (!IsRemoteReady && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!IsRemoteReady)
            {
                EnsureFirstOpenTime();
                _service.FinalizeRemoteFallback();
            }
        }

        public void ReloadTiming(string timing)
        {
            Initialize(_gameState ?? GameStateRuntime.Current);
            if (!IsAppStartFinalized)
            {
                EnsureFirstOpenTime();
                _service.FinalizeRemoteFallback();
            }
            _service.ReloadTiming(timing);
        }

        public LivingDaysSegment CurrentLivingDaysSegment()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int bias = (int)TimeZoneInfo.Local
                .GetUtcOffset(DateTimeOffset.FromUnixTimeMilliseconds(now))
                .TotalMinutes;
            return _adConfigs.LivingDays.Resolve(
                FirstOpenUnixMilliseconds,
                now,
                bias);
        }

        public void BindProvider(MonoBehaviour adapter)
        {
            if (providerAdapter == adapter) return;
            providerAdapter = adapter;
            RebuildService();
        }

        private void EnsureFirstOpenTime()
        {
            GameStateService state = _gameState ?? GameStateRuntime.Current;
            long sdkValue = _service?.Provider.FirstOpenUnixMilliseconds ?? 0;
            state.EnsureFirstOpenTime(sdkValue);
        }

        private void RebuildService()
        {
            if (_service != null)
            {
                _service.ProviderReady -= EnsureFirstOpenTime;
                _service.ParamsUpdated -= HandleParamsUpdated;
                _service.Dispose();
                _service = null;
            }
            if (isActiveAndEnabled)
                Initialize(_gameState ?? GameStateRuntime.Current);
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.ProviderReady -= EnsureFirstOpenTime;
                _service.ParamsUpdated -= HandleParamsUpdated;
                _service.Dispose();
                _service = null;
            }
            ParamsUpdated = null;
        }

        private void HandleParamsUpdated(string updateType)
        {
            ParamsUpdated?.Invoke(updateType ?? string.Empty);
        }

        private IReadOnlyList<IAbConfig> BuildConfigCatalog()
        {
            var configs = new List<IAbConfig>(
                _adConfigs.All.Count +
                _settingsConfigs.All.Count +
                _homeConfigs.All.Count +
                _platformConfigs.All.Count);
            configs.AddRange(_adConfigs.All);
            configs.AddRange(_settingsConfigs.All);
            configs.AddRange(_homeConfigs.All);
            configs.AddRange(_platformConfigs.All);
            return configs;
        }
    }
}
