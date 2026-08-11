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

        public ProfileService Service =>
            _service ??= new ProfileService(ProfileRepository.CreateDefault());

        public bool GrantFrame(int frameId, int count)
        {
            return Service.GrantFrame(frameId, count);
        }
    }
}
