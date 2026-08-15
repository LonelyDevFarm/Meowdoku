using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    public enum StreakDisplayState
    {
        Main = 0,
        Lit = 1,
        Settle = 2
    }

    [DisallowMultipleComponent]
    public sealed class StreakPagePresenter : UIFrameWindow,
        IDailyMetaConsumer
    {
        public override string GetTrackingScreenName() => _state switch
        {
            StreakDisplayState.Main => TrackerCatalog.Screen.Streak,
            StreakDisplayState.Settle => TrackerCatalog.Screen.GameStreak,
            _ => string.Empty
        };

        public const string StateParameter = "state";
        public const float SettleSlotDelaySeconds = 20f / 60f;
        public const float LitSlotDelaySeconds = 62f / 60f;
        public const float AddAfterCheckinSeconds = 0.9f;

        [SerializeField] private Text titleText;
        [SerializeField] private Text streakText;
        [SerializeField] private Text streakTextNew;
        [SerializeField] private Text currentText;
        [SerializeField] private Text bestText;
        [SerializeField] private Text tapSunText;
        [SerializeField] private Text continueText;
        [SerializeField] private Text goToPlayText;
        [SerializeField] private GameObject sunRoot;
        [SerializeField] private Button sunButton;
        [SerializeField] private Button tapSurface;
        [SerializeField] private Button backButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button goToPlayButton;
        [SerializeField] private StreakDaySlotView[] slots =
            Array.Empty<StreakDaySlotView>();
        [SerializeField] private LocalizationCatalog localization;

        private DailyMetaRuntime _runtime;
        private StreakDisplayState _state;
        private int _flowGeneration;
        private bool _litReady;
        private bool _settleRevealComplete;
        private bool _numberRollPending;
        private bool _numberRollScheduled;
        private int _numberRollFrom;
        private int _numberRollTo;
        private Sequence _numberSequence;
        private Vector2 _numberPosition;
        private Vector2 _numberNewPosition;

#if UNITY_INCLUDE_TESTS
        internal StreakDisplayState StateForTests => _state;
        internal bool SettleRevealCompleteForTests =>
            _settleRevealComplete;
        internal bool SunVisibleForTests =>
            sunRoot != null && sunRoot.activeSelf;
        internal bool NumberRollPendingForTests => _numberRollPending;
#endif

        protected override void OnCreate()
        {
            EnsureNumberRollText();
            ResetNumberRoll(false);
            Add(sunButton, HandleSun);
            Add(tapSurface, HandleSun);
            Add(backButton, HandleBack);
            Add(continueButton, HandleContinue);
            Add(goToPlayButton, HandleGoToPlay);
            if (localization != null)
                localization.LocaleChanged += Refresh;
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _flowGeneration++;
            _state = ReadState(parameters);
            _litReady = false;
            _settleRevealComplete = false;
            PrepareNumberRoll();
            Subscribe();
            Refresh();

            if (_state == StreakDisplayState.Lit)
                StartManagedCoroutine(EnableLitInputNextFrame(
                    _flowGeneration));
            else if (_state == StreakDisplayState.Settle)
                StartManagedCoroutine(RunSettle(
                    _flowGeneration,
                    SettleSlotDelaySeconds));
        }

        protected override IEnumerator OnHide()
        {
            _flowGeneration++;
            _litReady = false;
            _settleRevealComplete = false;
            ResetNumberRoll(true);
            Unsubscribe();
            yield break;
        }

        protected override bool OnBackRequest()
        {
            if (_state != StreakDisplayState.Main) return true;
            HandleBack();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            _flowGeneration++;
            ResetNumberRoll(true);
            Unsubscribe();
            Remove(sunButton, HandleSun);
            Remove(tapSurface, HandleSun);
            Remove(backButton, HandleBack);
            Remove(continueButton, HandleContinue);
            Remove(goToPlayButton, HandleGoToPlay);
            if (localization != null)
                localization.LocaleChanged -= Refresh;
            base.OnDestroyWindow();
        }

        public void BindDailyMetaRuntime(DailyMetaRuntime runtime)
        {
            if (_runtime == runtime) return;
            Unsubscribe();
            _runtime = runtime;
            Subscribe();
            Refresh();
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            if (localization == catalog) return;
            if (localization != null)
                localization.LocaleChanged -= Refresh;
            localization = catalog;
            if (localization != null)
                localization.LocaleChanged += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            StreakFeature streak = _runtime != null
                ? _runtime.Streak
                : null;
            if (streakText != null)
            {
                int value = streak != null ? streak.DisplayStreak : 0;
                streakText.text = (_numberRollPending
                        ? _numberRollFrom
                        : value)
                    .ToString();
            }
            SetText(titleText, Translate(
                "DAILY_STREAK_TITLE",
                "Daily Streak"));
            SetText(currentText, Translate(
                "DAILY_STREAK_CURRENT",
                "Current Streak"));
            SetText(tapSunText, Translate(
                "DAILY_STREAK_TAP_SUN",
                "Tap the sun, spark your streak!"));
            SetText(continueText, Translate(
                "WIN_CONTINUE",
                "Continue"));
            SetText(goToPlayText, Translate(
                "DAILY_STREAK_GO_TO_PLAY",
                "Go to Play"));

            int best = streak != null ? streak.Data.BestStreak : 0;
            string bestFormat = Translate(
                "DAILY_STREAK_BEST_FORMAT",
                "Best Streak: %d");
            SetText(bestText, bestFormat.Replace("%d", best.ToString()));

            bool main = _state == StreakDisplayState.Main;
            bool settle = _state == StreakDisplayState.Settle;
            SetActive(backButton, main);
            SetActive(continueButton, settle);
            // Source keeps SunImg visible in every display state and only
            // hides/disables SunBtn outside LIT. Disabling the whole root
            // removed the main visual from the portfolio Streak page.
            SetActive(sunRoot, true);
            SetActive(goToPlayButton, main &&
                streak != null && streak.HasPlayEntry);
            if (continueButton != null && !settle)
                continueButton.interactable = false;
            if (sunButton != null)
            {
                sunButton.enabled = _state == StreakDisplayState.Lit;
                sunButton.interactable =
                    _state == StreakDisplayState.Lit && _litReady;
            }
            if (tapSurface != null)
                tapSurface.interactable = _litReady;

            RefreshSlots(streak);
        }

        private IEnumerator EnableLitInputNextFrame(int generation)
        {
            yield return null;
            if (!IsCurrent(generation) ||
                _state != StreakDisplayState.Lit)
                yield break;
            _litReady = true;
            if (sunButton != null) sunButton.interactable = true;
            if (tapSurface != null) tapSurface.interactable = true;
        }

        private IEnumerator RunSettle(int generation, float slotDelay)
        {
            if (continueButton != null)
                continueButton.interactable = false;
            if (slotDelay > 0f)
                yield return new WaitForSecondsRealtime(slotDelay);
            if (!IsCurrent(generation)) yield break;

            StreakFeature streak = _runtime != null
                ? _runtime.Streak
                : null;
            if (streak == null)
            {
                if (continueButton != null)
                    continueButton.interactable = true;
                yield break;
            }

            _settleRevealComplete = true;
            int revealIndex = NewestCheckedIndex(streak);
            StreakDaySlotView revealSlot =
                revealIndex >= 0 && revealIndex < slots.Length
                    ? slots[revealIndex]
                    : null;
            int pendingUid = streak.PendingShowUid;
            if (pendingUid > 0)
            {
                float rewardDuration = revealSlot != null
                    ? revealSlot.PlayCheckin(true)
                    : StreakDaySlotView.RewardDurationSeconds;
                if (rewardDuration > 0f)
                    yield return new WaitForSecondsRealtime(rewardDuration);
                if (!IsCurrent(generation)) yield break;
                streak.ClaimReward();
                streak.ConsumePendingShow();
                revealSlot?.HideChest();
                revealSlot?.ShowUncheckedDot();
                if (Owner != null)
                    yield return Owner.AwaitHidden(UiName.Award);
                if (!IsCurrent(generation)) yield break;
                revealSlot?.PlayCheckin(false);
                ScheduleNumberRoll(generation);
            }
            else
            {
                if (revealSlot != null)
                    revealSlot.PlayCheckin(false);
                else
                    RefreshSlots(streak);
                ScheduleNumberRoll(generation);
                streak.ConsumePendingShow();
            }

            if (!IsCurrent(generation)) yield break;
            if (continueButton != null)
                continueButton.interactable = true;
        }

        private void RefreshSlots(StreakFeature streak)
        {
            if (slots == null || slots.Length == 0 || streak == null)
                return;
            IReadOnlyList<StreakWeekSlot> week = streak.GetWeekSlots();
            int hiddenCheckinIndex = -1;
            if (_state == StreakDisplayState.Settle &&
                !_settleRevealComplete &&
                streak.ReviveAnimation.Kind ==
                StreakReviveAnimationKind.None)
            {
                for (int index = 0; index < week.Count; index++)
                    if (week[index].IsChecked)
                        hiddenCheckinIndex = index;
            }
            for (int index = 0;
                 index < slots.Length && index < week.Count;
                 index++)
            {
                StreakDaySlotView view = slots[index];
                if (view == null) continue;
                view.BindLocalization(localization);
                bool chest = index == StreakFeature.CycleLength - 1 &&
                             streak.HasReward;
                view.ApplyStatic(
                    week[index].Weekday,
                    week[index].IsChecked &&
                    index != hiddenCheckinIndex,
                    chest);
            }
        }

        private void HandleSun()
        {
            if (!_litReady || _state != StreakDisplayState.Lit) return;
            _litReady = false;
            _settleRevealComplete = false;
            _state = StreakDisplayState.Settle;
            Refresh();
            StartManagedCoroutine(RunSettle(
                _flowGeneration,
                LitSlotDelaySeconds));
            Tracking?.TrackScreenShown(TrackerCatalog.Screen.GameStreak);
        }

        private void HandleBack()
        {
            if (_state == StreakDisplayState.Main)
            {
                Tracking?.TrackButtonClick(
                    TrackerCatalog.Button.Back,
                    TrackerCatalog.Screen.Streak);
                Owner?.Hide(UiName.Streak);
            }
        }

        private void HandleContinue()
        {
            if (_state == StreakDisplayState.Settle &&
                continueButton != null &&
                continueButton.interactable)
            {
                Tracking?.TrackButtonClick(
                    TrackerCatalog.Button.Continue,
                    TrackerCatalog.Screen.GameStreak);
                Owner?.Hide(UiName.Streak);
            }
        }

        private void HandleGoToPlay()
        {
            if (_runtime == null || !_runtime.Streak.HasPlayEntry ||
                Owner == null)
                return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.GoToPlay,
                TrackerCatalog.Screen.Streak);
            Owner.Show(
                UiName.Game,
                new Dictionary<string, object>
                {
                    ["level_index"] =
                        Meowdoku.Core.GameStateRuntime.Current.CurrentLevel
                });
            Owner.Hide(UiName.Home);
            Owner.Hide(UiName.Streak);
        }

        private void Subscribe()
        {
            if (_runtime == null || !IsShowing) return;
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
            Refresh();
        }

        private void PrepareNumberRoll()
        {
            ResetNumberRoll(false);
            StreakFeature streak = _runtime != null
                ? _runtime.Streak
                : null;
            int finalValue = streak != null ? streak.DisplayStreak : 0;
            bool normalSettle = streak != null &&
                streak.ReviveAnimation.Kind == StreakReviveAnimationKind.None;
            _numberRollPending = _state == StreakDisplayState.Settle &&
                                 normalSettle &&
                                 finalValue >= 2;
            _numberRollFrom = _numberRollPending
                ? finalValue - 1
                : finalValue;
            _numberRollTo = finalValue;
            if (streakText != null)
                streakText.text = _numberRollFrom.ToString();
        }

        private void ScheduleNumberRoll(int generation)
        {
            if (!_numberRollPending || _numberRollScheduled) return;
            _numberRollScheduled = true;
            StartManagedCoroutine(RunNumberRollAfterDelay(generation));
        }

        private IEnumerator RunNumberRollAfterDelay(int generation)
        {
            yield return new WaitForSecondsRealtime(AddAfterCheckinSeconds);
            if (!IsCurrent(generation) || !_numberRollPending) yield break;
            PlayNumberRoll();
        }

        private void PlayNumberRoll()
        {
            EnsureNumberRollText();
            KillNumberSequence();
            if (streakText == null || streakTextNew == null ||
                _numberRollFrom <= 0)
            {
                CompleteNumberRoll();
                return;
            }

            RectTransform oldRect = streakText.rectTransform;
            RectTransform newRect = streakTextNew.rectTransform;
            oldRect.anchoredPosition = _numberPosition;
            newRect.anchoredPosition = _numberNewPosition;
            streakText.text = _numberRollFrom.ToString();
            streakTextNew.text = _numberRollTo.ToString();
            streakTextNew.gameObject.SetActive(true);

            _numberSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _numberSequence.Join(oldRect.DOAnchorPosY(
                    _numberPosition.y + 220f, 0.4f)
                .SetEase(Ease.InOutCubic));
            _numberSequence.Join(newRect.DOAnchorPosY(
                    _numberPosition.y, 0.4f)
                .SetEase(Ease.InOutCubic));
            _numberSequence.AppendInterval(0.1f);
            _numberSequence.OnComplete(CompleteNumberRoll);
        }

        private void CompleteNumberRoll()
        {
            _numberSequence = null;
            _numberRollPending = false;
            _numberRollScheduled = false;
            if (streakText != null)
            {
                streakText.text = _numberRollTo.ToString();
                streakText.rectTransform.anchoredPosition = _numberPosition;
            }
            if (streakTextNew != null)
            {
                streakTextNew.rectTransform.anchoredPosition =
                    _numberNewPosition;
                streakTextNew.gameObject.SetActive(false);
            }
        }

        private void EnsureNumberRollText()
        {
            if (streakText == null) return;
            if (streakTextNew == null)
            {
                GameObject clone = Instantiate(
                    streakText.gameObject,
                    streakText.transform.parent);
                clone.name = "StreakNumberNext";
                streakTextNew = clone.GetComponent<Text>();
            }

            _numberPosition = streakText.rectTransform.anchoredPosition;
            _numberNewPosition = _numberPosition + new Vector2(0f, -220f);
            streakTextNew.rectTransform.anchoredPosition =
                _numberNewPosition;
            streakTextNew.gameObject.SetActive(false);
        }

        private void ResetNumberRoll(bool showFinal)
        {
            KillNumberSequence();
            if (showFinal && streakText != null)
                streakText.text = _numberRollTo.ToString();
            if (streakText != null)
                streakText.rectTransform.anchoredPosition = _numberPosition;
            if (streakTextNew != null)
            {
                streakTextNew.rectTransform.anchoredPosition =
                    _numberNewPosition;
                streakTextNew.gameObject.SetActive(false);
            }
            _numberRollPending = false;
            _numberRollScheduled = false;
        }

        private void KillNumberSequence()
        {
            if (_numberSequence != null && _numberSequence.IsActive())
                _numberSequence.Kill(false);
            _numberSequence = null;
        }

        private static int NewestCheckedIndex(StreakFeature streak)
        {
            if (streak == null) return -1;
            IReadOnlyList<StreakWeekSlot> week = streak.GetWeekSlots();
            int result = -1;
            for (int index = 0; index < week.Count; index++)
                if (week[index].IsChecked)
                    result = index;
            return result;
        }

        private bool IsCurrent(int generation)
        {
            return generation == _flowGeneration && IsShowing;
        }

        private static StreakDisplayState ReadState(
            IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null ||
                !parameters.TryGetValue(StateParameter, out object value))
                return StreakDisplayState.Main;
            try
            {
                int raw = Convert.ToInt32(value);
                return Enum.IsDefined(typeof(StreakDisplayState), raw)
                    ? (StreakDisplayState)raw
                    : StreakDisplayState.Main;
            }
            catch (Exception)
            {
                return StreakDisplayState.Main;
            }
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static void Add(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void Remove(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }

        private static void SetActive(Component target, bool active)
        {
            if (target != null &&
                target.gameObject.activeSelf != active)
                target.gameObject.SetActive(active);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
