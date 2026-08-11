using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StreakRevivePagePresenter : UIFrameWindow,
        IDailyMetaConsumer,
        IAdServiceConsumer
    {
        [SerializeField] private Text infoText;
        [SerializeField] private Text fromStreakText;
        [SerializeField] private Text toStreakText;
        [SerializeField] private Text restoreText;
        [SerializeField] private Text giveUpText;
        [SerializeField] private Button restoreButton;
        [SerializeField] private Button giveUpButton;
        [SerializeField] private Button actionCloseButton;
        [SerializeField] private LocalizationCatalog localization;

        private DailyMetaRuntime _runtime;
        private bool _isResume;
        private bool _settled;
        private AdService _adService;
        private int _requestGeneration;

        protected override void OnCreate()
        {
            Add(restoreButton, HandleRestore);
            Add(giveUpButton, HandleGiveUp);
            Add(actionCloseButton, HandleGiveUp);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _requestGeneration++;
            _settled = false;
            _isResume = UiName == UiName.StreakResume;
            int from = ReadInt(parameters, "from_streak");
            int to = ReadInt(parameters, "to_streak");
            int days = ReadInt(parameters, "info_days");
            SetText(fromStreakText, from.ToString());
            SetText(toStreakText, to.ToString());

            string key = _isResume
                ? "STREAK_REVIVE_RESUME_INFO"
                : "STREAK_REVIVE_BACKFILL_INFO";
            string fallback =
                "%s days of Daily Streak interrupted!";
            SetText(infoText, Translate(key, fallback)
                .Replace("%s", days.ToString()));
            SetText(restoreText, Translate(
                "STREAK_REVIVE_RESTORE",
                "Restore"));
            SetText(giveUpText, Translate(
                "STREAK_REVIVE_GIVE_UP",
                "Give up"));

            // Source requires a rewarded-ad success before restoring. R16 has
            // no adapter yet, so the action stays unavailable rather than
            // granting a fabricated reward.
            if (restoreButton != null)
                restoreButton.interactable = false;
            if (giveUpButton != null)
                giveUpButton.interactable = true;
        }

        protected override IEnumerator OnHide()
        {
            _requestGeneration++;
            if (!_settled && _runtime != null &&
                _runtime.Streak.HasPendingReviveDecision)
                _runtime.Streak.GiveUpRevive();
            yield break;
        }

        protected override bool OnBackRequest()
        {
            HandleGiveUp();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            Remove(restoreButton, HandleRestore);
            Remove(giveUpButton, HandleGiveUp);
            Remove(actionCloseButton, HandleGiveUp);
            base.OnDestroyWindow();
        }

        public void BindDailyMetaRuntime(DailyMetaRuntime runtime)
        {
            _runtime = runtime;
        }

        public void BindAdService(AdService service)
        {
            _adService = service;
        }

        private void HandleRestore()
        {
            if (_settled || _runtime == null || _adService == null) return;
            SetButtons(false);
            int generation = _requestGeneration;
            if (_adService.TryShowReward(
                    TrackerCatalog.AdPosition.StreakReviveReward,
                    rewarded => HandleRestoreCompleted(
                        generation,
                        rewarded)))
                return;
            SetButtons(true);
        }

        private void HandleRestoreCompleted(int generation, bool rewarded)
        {
            if (generation != _requestGeneration || !IsShowing) return;
            if (!rewarded)
            {
                SetButtons(true);
                return;
            }
            if (_runtime == null ||
                !_runtime.Streak.HasPendingReviveDecision)
            {
                SetButtons(true);
                return;
            }
            _runtime.Streak.ReviveStreak();
            _settled = true;
            Owner?.Hide(UiName);
        }

        private void SetButtons(bool interactable)
        {
            if (restoreButton != null) restoreButton.interactable = interactable;
            if (giveUpButton != null) giveUpButton.interactable = interactable;
            if (actionCloseButton != null)
                actionCloseButton.interactable = interactable;
        }

        private void HandleGiveUp()
        {
            if (_settled) return;
            _settled = true;
            if (_runtime != null &&
                _runtime.Streak.HasPendingReviveDecision)
                _runtime.Streak.GiveUpRevive();
            Owner?.Hide(UiName);
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static int ReadInt(
            IReadOnlyDictionary<string, object> parameters,
            string key)
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out object value))
                return 0;
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return 0;
            }
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
    }
}
