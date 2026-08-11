using System;
using System.Collections;
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
        private AbConfigService _service;
        private GameStateService _gameState;

        public event Action<string> ParamsUpdated;
        public AdConfigSet Ads => _adConfigs;
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
            _service = new AbConfigService(provider, _adConfigs.All);
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
    }
}
