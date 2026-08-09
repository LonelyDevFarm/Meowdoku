using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Screen-space adapter for Godot Line2D/CPUParticles2D score flight.
    /// Uses fixed serialized UI samples so it remains valid on overlay Canvas.
    /// </summary>
    public sealed class GameplayScoreFlightView : MonoBehaviour
    {
        [SerializeField] private Image point;
        [SerializeField] private Image[] trailSegments;
        [SerializeField] private Image burstGlow;
        [SerializeField] private Image[] burstStars;

        private Sequence _sequence;
        private Sequence _burstSequence;
        private Vector2[] _samples;

        public bool IsPlaying => gameObject.activeSelf;

        public void Play(Vector2 from, Vector2 to, bool life, Action arrived)
        {
            Stop();
            gameObject.SetActive(true);
            EnsureSamples(from);
            if (point != null)
            {
                point.gameObject.SetActive(true);
                point.rectTransform.anchoredPosition = from;
            }
            SetBurstActive(false);
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Append(DOVirtual.Float(0f, 1f,
                GameplayFeedbackPresentationPlan.FlyDurationSeconds,
                value => UpdateFlight(
                    GameplayScoreFlightMath.Evaluate(value, from, to, life))));
            _sequence.AppendCallback(() =>
            {
                if (point != null) point.gameObject.SetActive(false);
                PlayBurst(to);
                arrived?.Invoke();
            });
            _sequence.AppendInterval(GameplayFeedbackPresentationPlan.FlyLingerSeconds);
            _sequence.AppendCallback(HideTrail);
            _sequence.AppendInterval(1.5f - GameplayFeedbackPresentationPlan.FlyLingerSeconds);
            _sequence.OnComplete(TryFinish);
        }

        public void Stop()
        {
            if (_sequence != null && _sequence.IsActive()) _sequence.Kill(false);
            _sequence = null;
            if (_burstSequence != null && _burstSequence.IsActive())
                _burstSequence.Kill(false);
            _burstSequence = null;
            HideTrail();
            SetBurstActive(false);
            if (point != null) point.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            Stop();
        }

        private void EnsureSamples(Vector2 from)
        {
            int count = trailSegments != null ? trailSegments.Length + 1 : 1;
            if (_samples == null || _samples.Length != count) _samples = new Vector2[count];
            for (int index = 0; index < _samples.Length; index++) _samples[index] = from;
            if (trailSegments == null) return;
            for (int index = 0; index < trailSegments.Length; index++)
                if (trailSegments[index] != null) trailSegments[index].gameObject.SetActive(true);
        }

        private void UpdateFlight(Vector2 current)
        {
            if (_samples == null || _samples.Length == 0) return;
            for (int index = _samples.Length - 1; index > 0; index--)
                _samples[index] = _samples[index - 1];
            _samples[0] = current;
            if (point != null) point.rectTransform.anchoredPosition = current;
            if (trailSegments == null) return;
            for (int index = 0; index < trailSegments.Length; index++)
            {
                Image segment = trailSegments[index];
                if (segment == null) continue;
                Vector2 start = _samples[index];
                Vector2 end = _samples[index + 1];
                Vector2 delta = start - end;
                RectTransform rect = segment.rectTransform;
                rect.anchoredPosition = (start + end) * 0.5f;
                rect.sizeDelta = new Vector2(Mathf.Max(3f, delta.magnitude + 3f),
                    Mathf.Lerp(80f, 20f, index / (float)trailSegments.Length));
                rect.localEulerAngles = new Vector3(0f, 0f,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                Color color = segment.color;
                color.a = Mathf.Lerp(0.78f, 0f,
                    index / (float)trailSegments.Length);
                segment.color = color;
            }
        }

        private void PlayBurst(Vector2 position)
        {
            if (_burstSequence != null && _burstSequence.IsActive())
                _burstSequence.Kill(false);
            _burstSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            if (burstGlow != null)
            {
                burstGlow.gameObject.SetActive(true);
                burstGlow.rectTransform.anchoredPosition = position;
                burstGlow.rectTransform.localScale = Vector3.zero;
                burstGlow.color = new Color(0.9f, 0.6975f, 0.45f, 0.23529412f);
                _burstSequence.Insert(0f,
                    burstGlow.rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutQuad));
            }
            int starCount = burstStars != null ? burstStars.Length : 0;
            for (int index = 0; index < starCount; index++)
            {
                Image star = burstStars[index];
                if (star == null) continue;
                star.gameObject.SetActive(true);
                star.rectTransform.anchoredPosition = position;
                float angle = index * Mathf.PI * 2f / starCount;
                float distance = index % 2 == 0 ? 100f : 50f;
                Vector2 target = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                _burstSequence.Insert(0f, DOVirtual.Float(0f, 1f, 1.02f,
                    value => SetStar(star, position, target, value)));
            }
            _burstSequence.AppendInterval(0.48f);
            _burstSequence.OnComplete(() =>
            {
                _burstSequence = null;
                SetBurstActive(false);
                TryFinish();
            });
        }

        private void TryFinish()
        {
            bool flightActive = _sequence != null && _sequence.IsActive() &&
                                !_sequence.IsComplete();
            bool burstActive = _burstSequence != null && _burstSequence.IsActive() &&
                               !_burstSequence.IsComplete();
            if (!flightActive && !burstActive) gameObject.SetActive(false);
        }

        private static void SetStar(Image star, Vector2 from, Vector2 to, float value)
        {
            star.rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, value);
            float scale = Mathf.Sin(Mathf.Clamp01(value) * Mathf.PI) * 0.6f;
            star.rectTransform.localScale = Vector3.one * scale;
            Color color = star.color;
            color.a = value < 0.76f ? 1f : Mathf.InverseLerp(1f, 0.76f, value);
            star.color = color;
        }

        private void HideTrail()
        {
            if (trailSegments == null) return;
            for (int index = 0; index < trailSegments.Length; index++)
                if (trailSegments[index] != null) trailSegments[index].gameObject.SetActive(false);
        }

        private void SetBurstActive(bool active)
        {
            if (burstGlow != null) burstGlow.gameObject.SetActive(active);
            if (burstStars == null) return;
            for (int index = 0; index < burstStars.Length; index++)
                if (burstStars[index] != null) burstStars[index].gameObject.SetActive(active);
        }
    }
}
