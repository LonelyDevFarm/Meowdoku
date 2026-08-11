using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// UGUI adapter for AwardPage's score-trail based frame flight. It keeps
    /// the source curve/timing while using a fixed Image trail on Overlay UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FrameAwardFlightView : MonoBehaviour
    {
        public const float TrailShowDelaySeconds = 0.2667f;
        public const float FlightStartDelaySeconds = 0.3f;
        public const float FlightDurationSeconds = 0.45f;
        public const float TrailHideDelaySeconds = 0.1f;
        public const float ArrivalHoldSeconds = 0.4f;
        public const float BurstLifetimeSeconds = 1.5f;

        private const float XCurveX1 = 0.389f;
        private const float XCurveY1 = -0.006f;
        private const float XCurveX2 = 0.933f;
        private const float XCurveY2 = 1f;
        private const float YCurveX1 = 0.544f;
        private const float YCurveY1 = -0.46f;
        private const float YCurveX2 = 1f;
        private const float YCurveY2 = 1.001f;

        [SerializeField] private Image point;
        [SerializeField] private Image[] trailSegments = Array.Empty<Image>();
        [SerializeField] private Image burstGlow;
        [SerializeField] private Image[] burstStars = Array.Empty<Image>();

        private Sequence _sequence;
        private Vector2[] _samples;

        public bool IsPlaying => _sequence != null && _sequence.IsActive();
        public static float CompletionSeconds =>
            FlightStartDelaySeconds + FlightDurationSeconds +
            ArrivalHoldSeconds;

        public void Play(
            RectTransform from,
            RectTransform to,
            Action flightStarted,
            Action arrived,
            Action completed)
        {
            StopImmediate();
            if (from == null || to == null)
            {
                completed?.Invoke();
                return;
            }

            gameObject.SetActive(true);
            Vector2 start = ToLocalCenter(from);
            Vector2 target = ToLocalCenter(to);
            ResetVisuals(start);

            float arrivalAt = FlightStartDelaySeconds + FlightDurationSeconds;
            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _sequence.InsertCallback(TrailShowDelaySeconds, ShowTrail);
            _sequence.InsertCallback(FlightStartDelaySeconds,
                () => flightStarted?.Invoke());
            _sequence.Insert(
                FlightStartDelaySeconds,
                DOVirtual.Float(
                        0f,
                        1f,
                        FlightDurationSeconds,
                        value => UpdateFlight(Evaluate(value, start, target)))
                    .SetEase(Ease.Linear));
            InsertBurst(target, arrivalAt);
            _sequence.InsertCallback(arrivalAt, () =>
            {
                if (point != null) point.gameObject.SetActive(false);
                arrived?.Invoke();
            });
            _sequence.InsertCallback(
                arrivalAt + TrailHideDelaySeconds,
                HideTrail);
            _sequence.InsertCallback(
                arrivalAt + ArrivalHoldSeconds,
                () => completed?.Invoke());
            _sequence.OnComplete(() => _sequence = null);
        }

        public void StopImmediate()
        {
            if (_sequence != null && _sequence.IsActive())
                _sequence.Kill(false);
            _sequence = null;
            HideTrail();
            SetBurstActive(false);
            if (point != null) point.gameObject.SetActive(false);
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        public static Vector2 Evaluate(float t, Vector2 from, Vector2 to)
        {
            float clamped = Mathf.Clamp01(t);
            float x = EvaluateCubicBezier(
                clamped,
                XCurveX1,
                XCurveY1,
                XCurveX2,
                XCurveY2);
            float y = EvaluateCubicBezier(
                clamped,
                YCurveX1,
                YCurveY1,
                YCurveX2,
                YCurveY2);
            return new Vector2(
                Mathf.LerpUnclamped(from.x, to.x, x),
                Mathf.LerpUnclamped(from.y, to.y, y));
        }

        private static float EvaluateCubicBezier(
            float t,
            float x1,
            float y1,
            float x2,
            float y2)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float u = SolveBezierX(t, x1, x2);
            return BezierAxis(u, y1, y2);
        }

        private static float SolveBezierX(float t, float x1, float x2)
        {
            float u = t;
            for (int index = 0; index < 8; index++)
            {
                float delta = BezierAxis(u, x1, x2) - t;
                if (Mathf.Abs(delta) < 0.0001f) return u;
                float derivative = BezierDerivative(u, x1, x2);
                if (Mathf.Abs(derivative) < 0.000001f) break;
                u -= delta / derivative;
            }

            float low = 0f;
            float high = 1f;
            u = t;
            for (int index = 0; index < 20; index++)
            {
                float value = BezierAxis(u, x1, x2);
                if (Mathf.Abs(value - t) < 0.0001f) break;
                if (value < t) low = u;
                else high = u;
                u = (low + high) * 0.5f;
            }
            return u;
        }

        private static float BezierAxis(float u, float a1, float a2)
        {
            float v = 1f - u;
            return 3f * v * v * u * a1 +
                   3f * v * u * u * a2 +
                   u * u * u;
        }

        private static float BezierDerivative(float u, float a1, float a2)
        {
            float v = 1f - u;
            return 3f * v * v * a1 +
                   6f * v * u * (a2 - a1) +
                   3f * u * u * (1f - a2);
        }

        private Vector2 ToLocalCenter(RectTransform target)
        {
            RectTransform root = transform as RectTransform;
            Vector3 world = target.TransformPoint(target.rect.center);
            return root != null
                ? (Vector2)root.InverseTransformPoint(world)
                : (Vector2)world;
        }

        private void ResetVisuals(Vector2 start)
        {
            int count = trailSegments != null
                ? trailSegments.Length + 1
                : 1;
            if (_samples == null || _samples.Length != count)
                _samples = new Vector2[count];
            for (int index = 0; index < _samples.Length; index++)
                _samples[index] = start;
            if (point != null)
            {
                point.rectTransform.anchoredPosition = start;
                point.gameObject.SetActive(false);
            }
            HideTrail();
            SetBurstActive(false);
        }

        private void ShowTrail()
        {
            if (point != null) point.gameObject.SetActive(true);
            if (trailSegments == null) return;
            for (int index = 0; index < trailSegments.Length; index++)
                if (trailSegments[index] != null)
                    trailSegments[index].gameObject.SetActive(true);
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
                Vector2 head = _samples[index];
                Vector2 tail = _samples[index + 1];
                Vector2 delta = head - tail;
                RectTransform rect = segment.rectTransform;
                rect.anchoredPosition = (head + tail) * 0.5f;
                rect.sizeDelta = new Vector2(
                    Mathf.Max(3f, delta.magnitude + 3f),
                    Mathf.Lerp(80f, 16f,
                        index / (float)Mathf.Max(1, trailSegments.Length)));
                rect.localEulerAngles = new Vector3(
                    0f,
                    0f,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                SetAlpha(segment, Mathf.Lerp(
                    0.78f,
                    0f,
                    index / (float)Mathf.Max(1, trailSegments.Length)));
            }
        }

        private void InsertBurst(Vector2 position, float at)
        {
            if (burstGlow != null)
            {
                burstGlow.gameObject.SetActive(true);
                burstGlow.rectTransform.anchoredPosition = position;
                burstGlow.rectTransform.localScale = Vector3.zero;
                SetAlpha(burstGlow, 0.23529412f);
                _sequence.Insert(
                    at,
                    burstGlow.rectTransform.DOScale(1f, 0.3f)
                        .SetEase(Ease.OutQuad));
                _sequence.Insert(
                    at + 0.14f,
                    burstGlow.DOFade(0f, 0.16f).SetEase(Ease.Linear));
            }

            int count = burstStars?.Length ?? 0;
            for (int index = 0; index < count; index++)
            {
                Image star = burstStars[index];
                if (star == null) continue;
                float angle = index * Mathf.PI * 2f / Mathf.Max(1, count);
                float distance = index % 2 == 0 ? 100f : 50f;
                Vector2 target = position +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                star.gameObject.SetActive(true);
                star.rectTransform.anchoredPosition = position;
                star.rectTransform.localScale = Vector3.zero;
                SetAlpha(star, 1f);
                _sequence.Insert(
                    at,
                    DOVirtual.Float(0f, 1f, 1.02f,
                            value => SetStar(star, position, target, value))
                        .SetEase(Ease.Linear));
            }
        }

        private static void SetStar(
            Image star,
            Vector2 from,
            Vector2 to,
            float value)
        {
            star.rectTransform.anchoredPosition =
                Vector2.LerpUnclamped(from, to, value);
            star.rectTransform.localScale = Vector3.one *
                (Mathf.Sin(Mathf.Clamp01(value) * Mathf.PI) * 0.6f);
            SetAlpha(star, value < 0.76f
                ? 1f
                : Mathf.InverseLerp(1f, 0.76f, value));
        }

        private void HideTrail()
        {
            if (trailSegments == null) return;
            for (int index = 0; index < trailSegments.Length; index++)
                if (trailSegments[index] != null)
                    trailSegments[index].gameObject.SetActive(false);
        }

        private void SetBurstActive(bool active)
        {
            if (burstGlow != null) burstGlow.gameObject.SetActive(active);
            if (burstStars == null) return;
            for (int index = 0; index < burstStars.Length; index++)
                if (burstStars[index] != null)
                    burstStars[index].gameObject.SetActive(active);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            Color color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }

        private void OnDisable()
        {
            if (_sequence != null && _sequence.IsActive())
                _sequence.Kill(false);
            _sequence = null;
        }
    }
}
