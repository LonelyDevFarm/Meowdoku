using System;
using DG.Tweening;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StreakEntryPresenter : MonoBehaviour
    {
        public event Action OpenRequested;

        [SerializeField] private GameObject checkedState;
        [SerializeField] private GameObject uncheckedState;
        [SerializeField] private Text titleText;
        [SerializeField] private Text countText;
        [SerializeField] private Button clickButton;
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private RectTransform checkedSunVisual;
        [SerializeField] private RectTransform checkedShineVisual;
        [SerializeField] private CanvasGroup checkedGlowGroup;

        private DailyMetaRuntime _runtime;
        private bool _presenting;
        private Tween _sunTween;
        private Tween _shineTween;
        private Sequence _glowTween;
        private Vector3 _sunBaseScale = Vector3.one;
        private Vector3 _glowBaseScale = Vector3.one;

#if UNITY_INCLUDE_TESTS
        internal bool IsCheckedForTests =>
            checkedState != null && checkedState.activeSelf;
#endif

        private void Awake()
        {
            if (checkedSunVisual != null)
                _sunBaseScale = checkedSunVisual.localScale;
            if (checkedGlowGroup != null)
                _glowBaseScale = checkedGlowGroup.transform.localScale;
            if (clickButton != null)
                clickButton.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            if (_presenting) RefreshVfx();
        }

        private void OnDisable()
        {
            StopVfx();
        }

        private void OnDestroy()
        {
            StopVfx();
            Unsubscribe();
            if (clickButton != null)
                clickButton.onClick.RemoveListener(HandleClick);
        }

        public void BindDailyMetaRuntime(DailyMetaRuntime runtime)
        {
            if (_runtime == runtime) return;
            Unsubscribe();
            _runtime = runtime;
            Subscribe();
            RefreshNow();
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            localization = catalog;
            RefreshNow();
        }

        public void Show()
        {
            _presenting = true;
            Subscribe();
            RefreshNow();
        }

        public void Hide()
        {
            _presenting = false;
            StopVfx();
            Unsubscribe();
        }

        public void RefreshNow()
        {
            StreakFeature streak = _runtime != null
                ? _runtime.Streak
                : null;
            bool checkedToday = streak != null &&
                                !streak.CanCheckinToday();
            SetActive(checkedState, checkedToday);
            SetActive(uncheckedState, !checkedToday);
            if (countText != null)
                countText.text = streak != null
                    ? streak.DisplayStreak.ToString()
                    : "0";
            if (titleText != null)
                titleText.text = Translate(
                    "DAILY_STREAK_ENTRY_TITLE",
                    "Streak");
            if (clickButton != null)
                clickButton.interactable = streak != null &&
                                           streak.IsEnabled;
            RefreshVfx(checkedToday);
        }

        private void Subscribe()
        {
            if (!_presenting || _runtime == null) return;
            _runtime.Streak.StreakUpdated -= HandleStreakUpdated;
            _runtime.Streak.StreakUpdated += HandleStreakUpdated;
        }

        private void RefreshVfx()
        {
            RefreshVfx(checkedState != null && checkedState.activeSelf);
        }

        private void RefreshVfx(bool checkedToday)
        {
            if (checkedShineVisual != null)
                checkedShineVisual.gameObject.SetActive(checkedToday);
            if (checkedGlowGroup != null)
                checkedGlowGroup.gameObject.SetActive(checkedToday);
            if (!_presenting || !isActiveAndEnabled || !checkedToday)
            {
                StopVfx();
                return;
            }
            if (_shineTween != null && _shineTween.IsActive()) return;

            if (checkedSunVisual != null)
            {
                checkedSunVisual.localScale = _sunBaseScale;
                _sunTween = checkedSunVisual
                    .DOScale(_sunBaseScale * 1.06f, 1.05f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
            if (checkedShineVisual != null)
            {
                checkedShineVisual.localRotation = Quaternion.identity;
                _shineTween = checkedShineVisual
                    .DOLocalRotate(
                        new Vector3(0f, 0f, -360f),
                        18f,
                        RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
            if (checkedGlowGroup != null)
            {
                checkedGlowGroup.alpha = 0.26f;
                checkedGlowGroup.transform.localScale =
                    _glowBaseScale * 0.9f;
                _glowTween = DOTween.Sequence()
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                _glowTween.Append(
                    checkedGlowGroup.DOFade(0.58f, 1.2f)
                        .SetEase(Ease.InOutSine));
                _glowTween.Join(
                    checkedGlowGroup.transform
                        .DOScale(_glowBaseScale * 1.08f, 1.2f)
                        .SetEase(Ease.InOutSine));
                _glowTween.SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void StopVfx()
        {
            _sunTween?.Kill(false);
            _shineTween?.Kill(false);
            _glowTween?.Kill(false);
            _sunTween = null;
            _shineTween = null;
            _glowTween = null;
            if (checkedSunVisual != null)
                checkedSunVisual.localScale = _sunBaseScale;
            if (checkedShineVisual != null)
                checkedShineVisual.localRotation = Quaternion.identity;
            if (checkedGlowGroup != null)
            {
                checkedGlowGroup.alpha = 0.35f;
                checkedGlowGroup.transform.localScale = _glowBaseScale;
            }
        }

        private void Unsubscribe()
        {
            if (_runtime != null)
                _runtime.Streak.StreakUpdated -= HandleStreakUpdated;
        }

        private void HandleStreakUpdated(StreakData _)
        {
            RefreshNow();
        }

        private void HandleClick()
        {
            if (_runtime != null && _runtime.Streak.IsEnabled)
                OpenRequested?.Invoke();
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
