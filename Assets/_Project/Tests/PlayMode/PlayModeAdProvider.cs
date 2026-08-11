using System;
using Meowdoku.Core.Ads;
using UnityEngine;

namespace Meowdoku.Tests.PlayMode
{
    [DisallowMultipleComponent]
    public sealed class PlayModeAdProvider : MonoBehaviour, IAdProvider
    {
        private int _nextShowId;

        public bool IsAvailable => true;
        public int ShowCount { get; private set; }
        public string LastPlacementId { get; private set; } = string.Empty;
        public string LastPosition { get; private set; } = string.Empty;
        public string LastShowId { get; private set; } = string.Empty;

        public event Action<string> AdShown;
        public event Action<string> AdClosed;
        public event Action<string> AdRewarded;
        public event Action<string, string> AdError;
        public event Action<AdImpression> AdImpression;

        public string CreateShowId() =>
            "playmode-show-" + (++_nextShowId);

        public bool IsReady(
            string placementId,
            string position,
            string showId) =>
            !string.IsNullOrEmpty(placementId) &&
            !string.IsNullOrEmpty(showId);

        public bool IsValid(string placementId, string position) =>
            !string.IsNullOrEmpty(placementId);

        public void Show(
            string placementId,
            string position,
            string showId)
        {
            ShowCount++;
            LastPlacementId = placementId ?? string.Empty;
            LastPosition = position ?? string.Empty;
            LastShowId = showId ?? string.Empty;
        }

        public void ShowBanner(
            string placementId,
            string position,
            bool anchorBottom,
            int offsetBase,
            int heightBase)
        {
        }

        public void Destroy(string placementId)
        {
        }

        public void EmitShown() => AdShown?.Invoke(LastPlacementId);
        public void EmitClosed() => AdClosed?.Invoke(LastPlacementId);
        public void EmitRewarded() => AdRewarded?.Invoke(LastPlacementId);
        public void EmitError(string message) =>
            AdError?.Invoke(LastPlacementId, message ?? string.Empty);
        public void EmitImpression() =>
            AdImpression?.Invoke(new AdImpression(
                LastPlacementId,
                LastPosition));
    }
}
