using Meowdoku.Core.Tracking;
using Meowdoku.Core.Config;
using UnityEngine;

namespace Meowdoku.Core.Ads
{
    public interface IAdServiceConsumer
    {
        void BindAdService(AdService service);
    }

    [DisallowMultipleComponent]
    public sealed class AdRuntime : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour providerAdapter;
        [SerializeField] private TrackingRuntime trackingRuntime;
        [SerializeField] private AbConfigRuntime abConfigRuntime;

        private AdService _service;

        public AdService Service
        {
            get
            {
                EnsureInitialized();
                return _service;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            _service?.Tick();
        }

        private void OnDestroy()
        {
            _service?.Dispose();
            _service = null;
        }

        public void BindProvider(MonoBehaviour adapter)
        {
            if (providerAdapter == adapter) return;
            providerAdapter = adapter;
            RebuildService();
        }

        public void BindTrackingRuntime(TrackingRuntime runtime)
        {
            if (trackingRuntime == runtime) return;
            trackingRuntime = runtime;
            RebuildService();
        }

        public void BindAbConfigRuntime(AbConfigRuntime runtime)
        {
            if (abConfigRuntime == runtime) return;
            abConfigRuntime = runtime;
            RebuildService();
        }

        private void EnsureInitialized()
        {
            if (_service != null) return;
            _service = new AdService(
                GameStateRuntime.Current,
                trackingRuntime != null ? trackingRuntime.Tracker : null,
                providerAdapter as IAdProvider,
                sessionActiveSeconds: trackingRuntime != null
                    ? () => trackingRuntime.Session.SessionActiveSeconds
                    : null,
                rewardRestoreConfig:
                    abConfigRuntime != null
                        ? abConfigRuntime.Ads.CommonRewardLogic
                        : null);
        }

        private void RebuildService()
        {
            _service?.Dispose();
            _service = null;
            if (isActiveAndEnabled) EnsureInitialized();
        }
    }
}
