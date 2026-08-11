using System;
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

        private DailyMetaRuntime _runtime;
        private bool _presenting;

#if UNITY_INCLUDE_TESTS
        internal bool IsCheckedForTests =>
            checkedState != null && checkedState.activeSelf;
#endif

        private void Awake()
        {
            if (clickButton != null)
                clickButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
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
        }

        private void Subscribe()
        {
            if (!_presenting || _runtime == null) return;
            _runtime.Streak.StreakUpdated -= HandleStreakUpdated;
            _runtime.Streak.StreakUpdated += HandleStreakUpdated;
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
