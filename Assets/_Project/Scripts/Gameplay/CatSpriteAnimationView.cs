using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    public sealed class CatSpriteAnimationView : MonoBehaviour
    {
        // cell.tscn keeps CatIcon at (49, 51) in a 100 px cell and applies a
        // node scale of 0.5. Unity Image normally stretches every atlas frame
        // into one fixed RectTransform, capping the large appear frames inside
        // the cell so the source overscale reads as a smaller effect.
        private const float SourceScale = 0.5f;
        private static readonly Vector2 SourceAnchoredPosition =
            new Vector2(-1f, -1f);

        public static float SourceNodeScale => SourceScale;
        public static Vector2 SourceIconOffset => SourceAnchoredPosition;

        private enum PlaybackState
        {
            Stopped,
            Appear,
            Cry,
            Frustrated,
            Idle,
            Waiting
        }

        [SerializeField] private Image target;
        [SerializeField] private CatSpriteAnimationCatalog catalog;

        private PlaybackState _state;
        private int _frameIndex;
        private float _elapsed;

        public void PlayAppear()
        {
            Stop();
            if (!CanPlay()) return;

            Sprite[] frames = catalog.Appear;
            if (frames.Length == 0)
            {
                BeginIdle();
                return;
            }

            _state = PlaybackState.Appear;
            SetFrame(frames, 0);
        }

        public void PlayCryLoop()
        {
            if (_state == PlaybackState.Cry) return;
            Stop();
            if (!CanPlay()) return;

            Sprite[] frames = catalog.Cry;
            if (frames.Length == 0)
            {
                ShowIdleFinal();
                return;
            }

            _state = PlaybackState.Cry;
            SetFrame(frames, 0);
        }

        public void PlayFrustratedOnce()
        {
            if (_state == PlaybackState.Cry) return;
            Stop();
            if (!CanPlay()) return;

            Sprite[] frames = catalog.Frustrated;
            if (frames.Length == 0)
            {
                ShowIdleFinal();
                return;
            }

            _state = PlaybackState.Frustrated;
            SetFrame(frames, 0);
        }

        public void ReviveToIdle()
        {
            Stop();
            if (target == null || catalog == null) return;
            Sprite[] frames = catalog.Idle;
            if (frames.Length == 0) return;
            SetFrame(frames, frames.Length - 1);
            _state = PlaybackState.Waiting;
        }

        public void ShowIdleFinal()
        {
            Stop();
            if (target == null || catalog == null) return;
            Sprite[] frames = catalog.Idle;
            if (frames.Length == 0) return;
            SetFrame(frames, frames.Length - 1);
        }

        public void Stop()
        {
            _state = PlaybackState.Stopped;
            _frameIndex = 0;
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_state == PlaybackState.Stopped || !CanPlay()) return;

            _elapsed += Time.deltaTime;
            if (_state == PlaybackState.Waiting)
            {
                if (_elapsed >= catalog.IdleInterval) BeginIdle();
                return;
            }

            float frameDuration = 1f / catalog.Fps;
            while (_elapsed >= frameDuration &&
                   _state != PlaybackState.Stopped &&
                   _state != PlaybackState.Waiting)
            {
                _elapsed -= frameDuration;
                AdvanceFrame();
            }
        }

        private bool CanPlay()
        {
            return target != null && catalog != null && catalog.Fps > 0f;
        }

        private void AdvanceFrame()
        {
            Sprite[] frames = CurrentFrames();
            if (frames.Length == 0)
            {
                Stop();
                return;
            }
            int next = _frameIndex + 1;
            if (next < frames.Length)
            {
                SetFrame(frames, next);
                return;
            }

            if (_state == PlaybackState.Appear)
            {
                BeginIdle();
                return;
            }

            if (_state == PlaybackState.Cry)
            {
                _elapsed = 0f;
                SetFrame(frames, 0);
                return;
            }

            if (_state == PlaybackState.Frustrated)
            {
                BeginIdle();
                return;
            }

            _state = PlaybackState.Waiting;
            _elapsed = 0f;
        }

        private Sprite[] CurrentFrames()
        {
            switch (_state)
            {
                case PlaybackState.Appear:
                    return catalog.Appear;
                case PlaybackState.Cry:
                    return catalog.Cry;
                case PlaybackState.Frustrated:
                    return catalog.Frustrated;
                default:
                    return catalog.Idle;
            }
        }

        private void BeginIdle()
        {
            Sprite[] frames = catalog.Idle;
            if (frames.Length == 0)
            {
                Stop();
                return;
            }

            _state = PlaybackState.Idle;
            _elapsed = 0f;
            SetFrame(frames, 0);
        }

        private void SetFrame(Sprite[] frames, int index)
        {
            _frameIndex = index;
            Sprite frame = frames[index];
            target.sprite = frame;
            ApplySourceFrameLayout(frame);
        }

        private void ApplySourceFrameLayout(Sprite frame)
        {
            if (target == null || frame == null) return;
            RectTransform rect = target.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = SourceAnchoredPosition;
            rect.sizeDelta = frame.rect.size;
            rect.localScale = Vector3.one * SourceScale;
        }

        private void OnDisable()
        {
            Stop();
        }

#if UNITY_INCLUDE_TESTS
        internal bool IsPlayingForTests => _state != PlaybackState.Stopped;
        internal int FrameIndexForTests => _frameIndex;
        internal const float SourceScaleForTests = SourceScale;
        internal static Vector2 SourceAnchoredPositionForTests =>
            SourceAnchoredPosition;
#endif
    }
}
