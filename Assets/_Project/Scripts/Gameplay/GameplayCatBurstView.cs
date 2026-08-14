using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    public sealed class GameplayCatBurstView : MonoBehaviour
    {
        public const float EmissionDelaySeconds = 0.1164f;
        public const float GlowLifetimeSeconds = 0.5f;
        public const float StarLifetimeSeconds = 1.02f;
        public const int StarCount = 24;

        private static readonly Color Green =
            new Color(0.3246f, 1f, 0.7656f, 1f);
        private static readonly Color Yellow =
            new Color(1f, 0.69997f, 0.24132f, 1f);
        private static readonly Color Purple =
            new Color(0.78438f, 0.46931f, 1f, 1f);

        [SerializeField] private Image glow;
        [SerializeField] private Image[] stars;

        private Sequence _sequence;
        private Vector2 _center;

        public bool IsPlaying => gameObject.activeSelf;

        internal bool IsEmittingForTests
        {
            get
            {
                if (glow != null && glow.gameObject.activeSelf) return true;
                if (stars == null) return false;
                for (int index = 0; index < stars.Length; index++)
                    if (stars[index] != null && stars[index].gameObject.activeSelf)
                        return true;
                return false;
            }
        }

        public void Play(Vector2 center)
        {
            Stop();
            _center = center;
            gameObject.SetActive(true);
            SetVisualsActive(false);

            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.InsertCallback(EmissionDelaySeconds, BeginEmission);
            _sequence.Insert(EmissionDelaySeconds, DOVirtual.Float(
                0f, 1f, GlowLifetimeSeconds, UpdateGlow).SetEase(Ease.Linear));
            _sequence.Insert(EmissionDelaySeconds, DOVirtual.Float(
                0f, 1f, StarLifetimeSeconds, UpdateStars).SetEase(Ease.Linear));
            _sequence.OnComplete(() =>
            {
                _sequence = null;
                SetVisualsActive(false);
                gameObject.SetActive(false);
            });
        }

        public void Stop()
        {
            if (_sequence != null && _sequence.IsActive()) _sequence.Kill(false);
            _sequence = null;
            SetVisualsActive(false);
        }

        private void OnDisable()
        {
            Stop();
        }

        private void BeginEmission()
        {
            if (glow != null)
            {
                glow.gameObject.SetActive(true);
                glow.rectTransform.anchoredPosition = _center;
            }
            if (stars == null) return;
            for (int index = 0; index < stars.Length; index++)
            {
                Image star = stars[index];
                if (star == null) continue;
                star.gameObject.SetActive(true);
                star.rectTransform.anchoredPosition = _center;
            }
            UpdateGlow(0f);
            UpdateStars(0f);
        }

        private void UpdateGlow(float value)
        {
            if (glow == null) return;
            float scale = value < 0.36f
                ? Mathf.Lerp(0f, 1.72f, value / 0.36f)
                : Mathf.Lerp(1.72f, 2f, (value - 0.36f) / 0.64f);
            float alpha = value < 0.2f
                ? Mathf.Lerp(0f, 0.265f, value / 0.2f)
                : Mathf.Lerp(0.265f, 0f, (value - 0.2f) / 0.8f);
            glow.rectTransform.localScale = Vector3.one * scale;
            glow.color = new Color(1f, 1f, 1f, alpha);
        }

        private void UpdateStars(float value)
        {
            if (stars == null) return;
            float travel = Mathf.Clamp01(value * 4f);
            float alpha = EvaluateStarAlpha(value);
            for (int index = 0; index < stars.Length; index++)
            {
                Image star = stars[index];
                if (star == null) continue;
                int emitterIndex = index % 12;
                int emitter = index / 12;
                float angle = (emitterIndex * 30f + emitter * 15f) * Mathf.Deg2Rad;
                float radius = 130f + ((emitterIndex * 17 + emitter * 11) % 31);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                star.rectTransform.anchoredPosition = _center + direction * radius * travel;
                float maximumScale = 0.15f +
                    ((index * 7) % StarCount) / (float)(StarCount - 1) * 0.0525f;
                float normalizedScale = value <= 0.16751269f
                    ? Mathf.InverseLerp(0f, 0.16751269f, value)
                    : value <= 0.8747885f
                        ? 1f - Mathf.InverseLerp(0.16751269f, 0.8747885f, value)
                        : 0f;
                float scale = maximumScale * normalizedScale;
                star.rectTransform.localScale = Vector3.one * scale;
                Color color = index % 3 == 0 ? Green : index % 3 == 1 ? Yellow : Purple;
                color.a = alpha;
                star.color = color;
            }
        }

        private static float EvaluateStarAlpha(float value)
        {
            if (value <= 0.4713f) return 1f;
            if (value <= 0.7049f)
                return Mathf.Lerp(1f, 0.41085f,
                    Mathf.InverseLerp(0.4713f, 0.7049f, value));
            return Mathf.Lerp(0.41085f, 0f,
                Mathf.InverseLerp(0.7049f, 1f, value));
        }

        private void SetVisualsActive(bool active)
        {
            if (glow != null)
            {
                glow.gameObject.SetActive(active);
                if (!active)
                {
                    glow.rectTransform.localScale = Vector3.zero;
                    glow.color = new Color(1f, 1f, 1f, 0f);
                }
            }
            if (stars == null) return;
            for (int index = 0; index < stars.Length; index++)
            {
                Image star = stars[index];
                if (star == null) continue;
                star.gameObject.SetActive(active);
                if (!active) star.rectTransform.localScale = Vector3.zero;
            }
        }
    }
}
