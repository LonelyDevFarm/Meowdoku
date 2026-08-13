using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SplashPagePresenter : UIFrameWindow,
        IStartupSplashWindow
    {
        public override string GetTrackingScreenName() =>
            TrackerCatalog.Screen.Splash;

        private const float DefaultMinimumSeconds = 3f;
        private const float FinishDurationSeconds = 0.1f;
        private const int SloganCount = 67;
        private const float ProgressLeft = -450f;
        private const float ProgressWidth = 900f;

        [SerializeField] private RectTransform progressFill;
        [SerializeField] private RectTransform catFace;
        [SerializeField] private Text quoteLabel;
        [SerializeField] private Text authorLabel;
        [SerializeField] private LocalizationCatalog localization;

        private float _progress;
        private float _elapsed;
        private float _minimumSeconds = DefaultMinimumSeconds;
        private bool _running;
        private bool _finishing;

#if UNITY_INCLUDE_TESTS
        internal float MinimumSecondsForTests => _minimumSeconds;
        internal float ProgressForTests => _progress;
        internal bool IsRunningForTests => _running;
#endif

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _minimumSeconds = ReadFloat(
                parameters,
                "min_wait_seconds",
                DefaultMinimumSeconds);
            _progress = 0f;
            _elapsed = 0f;
            _running = true;
            _finishing = false;
            ApplyQuote();
            ApplyProgress(0f);
            enabled = true;
        }

        protected override IEnumerator OnHide()
        {
            _running = false;
            _finishing = false;
            enabled = false;
            yield break;
        }

        public IEnumerator ForceCompleteAndWait()
        {
            if (!_running) yield break;
            _finishing = true;
            float from = _progress;
            float elapsed = 0f;
            while (elapsed < FinishDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                ApplyProgress(Mathf.Lerp(
                    from,
                    1f,
                    Mathf.Clamp01(elapsed / FinishDurationSeconds)));
                yield return null;
            }
            ApplyProgress(1f);
            _running = false;
            _finishing = false;
        }

        private void Update()
        {
            if (!_running || _finishing) return;
            float delta = Time.unscaledDeltaTime;
            _elapsed += delta;
            if (_progress >= 1f) return;
            float denominator = Mathf.Max(0.001f, _minimumSeconds);
            ApplyProgress(_progress +
                (1f - _progress) * (delta / denominator));
        }

        private void ApplyQuote()
        {
            GameStateService state = GameStateRuntime.Current;
            bool firstToday = state.MarkSplashShownToday();
            int index = firstToday ? 0 : UnityEngine.Random.Range(1, SloganCount);
            if (quoteLabel != null)
            {
                string key = $"splash_slogan_{index}";
                quoteLabel.text = localization != null
                    ? localization.Translate(key)
                    : key;
            }
            if (authorLabel != null)
            {
                string key = $"splash_slogan_author_{index}";
                string author = localization != null
                    ? localization.Translate(key)
                    : key;
                authorLabel.text = "- " + author;
            }
        }

        private void ApplyProgress(float value)
        {
            _progress = Mathf.Clamp01(value);
            if (progressFill != null)
            {
                Vector2 size = progressFill.sizeDelta;
                size.x = ProgressWidth * _progress;
                progressFill.sizeDelta = size;
                Vector2 position = progressFill.anchoredPosition;
                position.x = ProgressLeft;
                progressFill.anchoredPosition = position;
            }
            if (catFace != null)
            {
                Vector2 position = catFace.anchoredPosition;
                position.x = ProgressLeft + ProgressWidth * _progress;
                catFace.anchoredPosition = position;
            }
        }

        private static float ReadFloat(
            IReadOnlyDictionary<string, object> parameters,
            string key,
            float fallback)
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out object value) ||
                value == null)
                return fallback;
            try
            {
                return Convert.ToSingle(value);
            }
            catch (Exception)
            {
                return fallback;
            }
        }
    }
}
