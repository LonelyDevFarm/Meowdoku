using System;
using Meowdoku.Core;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DailyChallengeEntryPresenter : MonoBehaviour
    {
        public event Action PlayRequested;

        [SerializeField] private GameObject normalState;
        [SerializeField] private GameObject lockedState;
        [SerializeField] private GameObject doneState;
        [SerializeField] private Button clickButton;
        [SerializeField] private Text normalTitle;
        [SerializeField] private Text normalDate;
        [SerializeField] private Text normalCountdown;
        [SerializeField] private Text normalPlay;
        [SerializeField] private Text lockedTitle;
        [SerializeField] private Text lockedMessage;
        [SerializeField] private Text doneTitle;
        [SerializeField] private Text doneDate;
        [SerializeField] private Text doneTime;
        [SerializeField] private Text doneRank;
        [SerializeField] private LocalizationCatalog localization;

        private DailyEntryState _state;
        private bool _presenting;
        private double _nextRefreshAt;
        private ClockTicker _clockTicker;

        private void Awake()
        {
            if (clickButton != null)
                clickButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            UnsubscribeClock();
            if (clickButton != null)
                clickButton.onClick.RemoveListener(HandleClick);
        }

        private void Update()
        {
            if (_clockTicker != null || !_presenting ||
                Time.unscaledTimeAsDouble < _nextRefreshAt)
                return;
            RefreshNow();
        }

        public void BindClockTicker(ClockTicker ticker)
        {
            if (_clockTicker == ticker) return;
            UnsubscribeClock();
            _clockTicker = ticker;
            SubscribeClock();
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            localization = catalog;
            if (_presenting) RefreshNow();
        }

        public void Show()
        {
            _presenting = true;
            SubscribeClock();
            RefreshNow();
        }

        public void Hide()
        {
            UnsubscribeClock();
            _presenting = false;
        }

        public void RefreshNow()
        {
            DateTime now = _clockTicker != null
                ? _clockTicker.LocalNow
                : DateTime.Now;
            GameStateService state = GameStateRuntime.Current;
            string today = DailyEntryStateContract.DateKey(now);
            _state = DailyEntryStateContract.Compute(
                state.CurrentLevel,
                today,
                state.DailyCompletedDate,
                state.MaxDailyDate);

            SetActive(normalState, _state == DailyEntryState.Normal);
            SetActive(lockedState, _state == DailyEntryState.Locked);
            SetActive(doneState, _state == DailyEntryState.Done);

            string title = Translate(
                "DAILY_CHALLENGE_TITLE",
                "Daily\nChallenge");
            SetText(normalTitle, title);
            SetText(lockedTitle, title);
            SetText(doneTitle, title);

            string date = DailyEntryStateContract.TodayDateText(
                Translate(
                    DailyEntryStateContract.MonthLocalizationKey(now.Month),
                    now.ToString("MMM")),
                now.Day);
            if (_state == DailyEntryState.Normal)
            {
                SetText(normalDate, date);
                SetText(
                    normalCountdown,
                    DailyEntryStateContract.CountdownText(now));
                SetText(
                    normalPlay,
                    Translate("DAILY_CHALLENGE_PLAY", "Play"));
            }
            else if (_state == DailyEntryState.Locked)
            {
                string format = Translate(
                    "DAILY_CHALLENGE_UNLOCK_AT",
                    "Unlock at Level %d");
                SetText(
                    lockedMessage,
                    format.Replace(
                        "%d",
                        DailyEntryStateContract.UnlockLevel.ToString()));
            }
            else
            {
                SetText(doneDate, date);
                SetText(
                    doneTime,
                    DailyEntryStateContract.DoneTimeText(
                        state.DailyElapsedSeconds));
                float top = DailyEntryStateContract.DoneTopPercent(
                    state.DailyBeatPercent);
                int decimals = DailyEntryStateContract.DoneTopPercentDecimals(top);
                string percent = top.ToString(decimals == 0 ? "0" : "0.0") + "%";
                string format = Translate(
                    "HOME_DAILY_TOP_PERCENT",
                    "Top %s");
                SetText(doneRank, format.Replace("%s", percent));
            }

            _nextRefreshAt = Time.unscaledTimeAsDouble + 1.0;
        }

        private void SubscribeClock()
        {
            if (!_presenting || _clockTicker == null) return;
            _clockTicker.SecondTick -= RefreshNow;
            _clockTicker.SecondTick += RefreshNow;
        }

        private void UnsubscribeClock()
        {
            if (_clockTicker != null)
                _clockTicker.SecondTick -= RefreshNow;
        }

        private void HandleClick()
        {
            RefreshNow();
            if (_state == DailyEntryState.Normal)
                PlayRequested?.Invoke();
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value)
                target.SetActive(value);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }
    }
}
