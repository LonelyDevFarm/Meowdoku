using UnityEngine;

namespace Meowdoku.Core.Online
{
    [DisallowMultipleComponent]
    public sealed class AuthRuntime : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour platformAdapter;

        private AuthService _service;

        public AuthService Service
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

        public void BindPlatformAdapter(MonoBehaviour adapter)
        {
            if (platformAdapter == adapter) return;
            platformAdapter = adapter;
            RebuildService();
        }

        private void EnsureInitialized()
        {
            if (_service != null) return;
            _service = new AuthService(
                platformAdapter as IAuthProvider,
                platformAdapter as IAuthPrerequisiteProvider);
            _service.Start();
        }

        private void RebuildService()
        {
            _service?.Dispose();
            _service = null;
            if (isActiveAndEnabled) EnsureInitialized();
        }
    }
}
