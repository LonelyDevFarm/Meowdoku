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

        public RobotService Service =>
            _service ??= new RobotService(RobotRepository.CreateDefault());

        public void ResetData()
        {
            Service.Reset();
        }
    }
}
