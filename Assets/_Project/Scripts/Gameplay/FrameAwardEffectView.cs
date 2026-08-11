using System;
using DG.Tweening;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Unity presentation of AwardPage's source FrameAddEffect. The view owns
    /// its tween lifecycle; inventory persistence remains in AwardManager.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FrameAwardEffectView : MonoBehaviour
    {
        public const float AppearDurationSeconds = 0.56666666f;
        public const float HoldNewFrameSeconds = 0.6334f;
        public const float HoldExistingFrameSeconds = 0.8f;
        public const float DisappearDurationSeconds = 0.33333334f;

        private const float SourceSettledScale = 2.1621623f;

        [SerializeField] private CanvasGroup rayGroup;
        [SerializeField] private RectTransform rayVisual;
        [SerializeField] private CanvasGroup avatarGroup;
        [SerializeField] private RectTransform avatarVisual;
        [SerializeField] private ProfileAvatarView avatar;
        [SerializeField] private FrameAwardFlightView flight;

        private Sequence _sequence;
        private Action _completed;

        public bool IsPlaying =>
            (_sequence != null && _sequence.IsActive()) ||
            (flight != null && flight.IsPlaying);
        public int DisplayedFrameId { get; private set; }
        public int DisplayedCount { get; private set; }

        public static float TotalDuration(bool alreadyOwned)
        {
            return AppearDurationSeconds +
                   (alreadyOwned
                       ? HoldExistingFrameSeconds
                       : HoldNewFrameSeconds) +
                   DisappearDurationSeconds;
        }

        public void Play(
            int avatarId,
            int frameId,
            int beforeCount,
            int awardCount,
            RectTransform profileTarget,
            Action flightStarted,
            Action flightArrived,
            Action completed)
        {
            StopImmediate();
            DisplayedFrameId = frameId;
            int safeBefore = Mathf.Max(0, beforeCount);
            int after = safeBefore + Mathf.Max(1, awardCount);
            DisplayedCount = safeBefore > 0 ? safeBefore : after;
            _completed = completed;

            gameObject.SetActive(true);
            avatar?.SetInfo(avatarId, frameId);
            avatar?.SetBaseVisible(true);
            avatar?.SetRedDot(false);
            SetCount(DisplayedCount);

            if (rayGroup != null) rayGroup.alpha = 0f;
            if (rayVisual != null) rayVisual.localScale = Vector3.one * 0.5f;
            if (avatarGroup != null) avatarGroup.alpha = 0f;
            if (avatarVisual != null)
            {
                avatarVisual.localScale = Vector3.one * 0.6f;
                avatarVisual.localRotation = Quaternion.identity;
            }

            float hold = safeBefore > 0
                ? HoldExistingFrameSeconds
                : HoldNewFrameSeconds;
            float disappearAt = AppearDurationSeconds + hold;
            bool flyToProfile = safeBefore <= 0 &&
                                profileTarget != null &&
                                flight != null;
            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);

            if (avatarGroup != null)
                _sequence.Insert(0f, avatarGroup.DOFade(1f, 0.16666667f));
            if (avatarVisual != null)
            {
                _sequence.Insert(0f,
                    avatarVisual.DOScale(2.5f, 0.13333334f)
                        .SetEase(Ease.OutQuad));
                _sequence.Insert(0.13333334f,
                    avatarVisual.DOScale(SourceSettledScale, 0.2f)
                        .SetEase(Ease.InOutQuad));
                _sequence.Insert(0f,
                    avatarVisual.DORotate(
                        new Vector3(0f, 0f, 10f),
                        0.13333367f));
                _sequence.Insert(0.13333367f,
                    avatarVisual.DORotate(
                        new Vector3(0f, 0f, -5f),
                        0.13333273f));
                _sequence.Insert(0.2666664f,
                    avatarVisual.DORotate(
                        new Vector3(0f, 0f, 3f),
                        0.1500001f));
                _sequence.Insert(0.4166665f,
                    avatarVisual.DORotate(Vector3.zero, 0.15000016f));
            }
            if (rayGroup != null)
                _sequence.Insert(0.10000038f,
                    rayGroup.DOFade(1f, 0.1166663f));
            if (rayVisual != null)
                _sequence.Insert(0.10000038f,
                    rayVisual.DOScale(1f, 0.1166663f));

            if (safeBefore > 0)
                _sequence.Insert(AppearDurationSeconds,
                    DOVirtual.Int(safeBefore, after, 0.4f, SetCount)
                        .SetEase(Ease.OutQuad));

            if (rayGroup != null)
                _sequence.Insert(disappearAt + 0.06666732f,
                    rayGroup.DOFade(0f, 0.11666584f));
            if (avatarVisual != null)
            {
                _sequence.Insert(disappearAt,
                    avatarVisual.DOScale(2.5f, 0.13333333f)
                        .SetEase(Ease.OutQuad));
                _sequence.Insert(disappearAt + 0.13333333f,
                    avatarVisual.DOScale(0.3f, 0.2f)
                        .SetEase(Ease.InQuad));
            }
            if (avatarGroup != null)
                _sequence.Insert(disappearAt + 0.20000005f,
                    avatarGroup.DOFade(0f, 0.13333332f));

            if (flyToProfile)
            {
                _sequence.InsertCallback(disappearAt, () => flight.Play(
                    avatarVisual,
                    profileTarget,
                    flightStarted,
                    flightArrived,
                    Finish));
                _sequence.OnComplete(() => _sequence = null);
            }
            else
            {
                _sequence.InsertCallback(
                    disappearAt + DisappearDurationSeconds,
                    Finish);
            }
        }

        public void StopImmediate()
        {
            _sequence?.Kill(false);
            _sequence = null;
            flight?.StopImmediate();
            _completed = null;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void SetCount(int count)
        {
            DisplayedCount = count;
            avatar?.SetCount(count);
        }

        private void Finish()
        {
            Action callback = _completed;
            _completed = null;
            _sequence = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            _sequence?.Kill(false);
            _sequence = null;
            flight?.StopImmediate();
            _completed = null;
        }
    }
}
