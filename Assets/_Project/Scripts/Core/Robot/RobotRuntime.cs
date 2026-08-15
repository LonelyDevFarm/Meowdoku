using UnityEngine;

namespace Meowdoku.Core.Robot
{
    /// <summary>
    /// Scene-owned composition boundary for the source RobotService autoload.
    /// RankActivity owns pool creation/disposal; this component only owns the
    /// persistent service lifetime and exposes an explicit reset boundary.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RobotRuntime : MonoBehaviour
    {
        private RobotService _service;
        private RobotRepository _repository;

        public RobotService Service
        {
            get
            {
                if (_service != null) return _service;
                _repository = RobotRepository.CreateDefault();
                _service = new RobotService(_repository);
                return _service;
            }
        }

        public void ResetData()
        {
            Service.Reset();
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
