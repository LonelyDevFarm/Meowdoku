using DG.Tweening;
using UnityEngine;

namespace Meowdoku.Core
{
    /// <summary>
    /// Reserves deterministic mobile capacity for gameplay/result bursts and prevents DOTween runtime resize warnings.
    /// </summary>
    internal static class TweenRuntimeConfiguration
    {
        private const int MaxTweeners = 512;
        private const int MaxSequences = 128;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Configure()
        {
            DOTween.SetTweensCapacity(MaxTweeners, MaxSequences);
        }
    }
}
