using Meowdoku.Core.Daily;
using UnityEngine;

namespace Meowdoku.Core.Profile
{
    public interface IProfileConsumer
    {
        void BindProfileRuntime(ProfileRuntime runtime);
    }

    /// <summary>
    /// Scene-owned composition boundary for the source ProfileService autoload.
    /// The service stays lazy so cold-start award recovery can safely request
    /// it regardless of MonoBehaviour Awake ordering.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProfileRuntime : MonoBehaviour, IFrameAwardSink
    {
        private ProfileService _service;
        private ProfileRepository _repository;

        public ProfileService Service
        {
            get
            {
                if (_service != null) return _service;
                _repository = ProfileRepository.CreateDefault();
                _service = new ProfileService(_repository);
                return _service;
            }
        }

        public bool GrantFrame(int frameId, int count)
        {
            return Service.GrantFrame(frameId, count);
        }

        internal void ConfigureForTests(ProfileService service)
        {
            FlushPendingWrites();
            _repository = null;
            _service = service;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushPendingWrites();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) FlushPendingWrites();
        }

        private void OnDestroy()
        {
            FlushPendingWrites();
        }

        private void FlushPendingWrites()
        {
            _repository?.FlushPendingWrites();
        }
    }
}
