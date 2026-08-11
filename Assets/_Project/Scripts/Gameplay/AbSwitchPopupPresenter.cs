using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AbSwitchPopupPresenter : UIFrameWindow
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text actionText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button actionCloseButton;
        [SerializeField] private Button feedbackButton;
        [SerializeField] private GameObject toolGroup;
        [SerializeField] private GameObject locateReward;
        [SerializeField] private Text locateCountText;
        [SerializeField] private GameObject hintReward;
        [SerializeField] private Text hintCountText;
        [SerializeField] private LocalizationCatalog localization;

        protected override void OnCreate()
        {
            Add(actionButton, Close);
            Add(actionCloseButton, Close);
            // Support/FAQ belongs to the external services boundary. Keep the
            // source control visible when requested but non-interactive until
            // that boundary exists.
            if (feedbackButton != null)
                feedbackButton.interactable = false;
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            SetText(titleText, TranslateParameter(
                parameters,
                "title",
                "DAILY_STREAK_MAJOR_UPDATE",
                "Major Update"));
            SetText(bodyText, TranslateParameter(
                parameters,
                "body",
                "DAILY_STREAK_SWITCH3_DESC",
                "Good news! Daily Streak has been upgraded."));
            SetText(actionText, TranslateParameter(
                parameters,
                "btn_text",
                "DAILY_STREAK_GET_IT",
                "Get it"));
            SetText(feedbackText, Translate(
                "FEEDBACK_TITLE",
                "Feedback"));

            bool feedback = ReadString(parameters, "feedback") == "1";
            SetActive(feedbackButton, feedback);
            ApplyRewards(parameters);
        }

        protected override IEnumerator OnHide()
        {
            yield break;
        }

        protected override bool OnBackRequest()
        {
            Close();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            Remove(actionButton, Close);
            Remove(actionCloseButton, Close);
            base.OnDestroyWindow();
        }

        private void ApplyRewards(
            IReadOnlyDictionary<string, object> parameters)
        {
            IReadOnlyDictionary<string, object> rewards = null;
            if (parameters != null &&
                parameters.TryGetValue("reward", out object raw))
                rewards = raw as IReadOnlyDictionary<string, object>;
            int locate = ReadCount(rewards, "locate");
            int hint = ReadCount(rewards, "hint");
            SetActive(toolGroup, locate > 0 || hint > 0);
            SetActive(locateReward, locate > 0);
            SetActive(hintReward, hint > 0);
            SetText(locateCountText, "x" + locate);
            SetText(hintCountText, "x" + hint);
        }

        private void Close()
        {
            Owner?.Hide(UiName.AbSwitchPopup);
        }

        private string TranslateParameter(
            IReadOnlyDictionary<string, object> parameters,
            string name,
            string defaultKey,
            string fallback)
        {
            string key = ReadString(parameters, name);
            if (string.IsNullOrEmpty(key)) key = defaultKey;
            return Translate(key, fallback);
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static string ReadString(
            IReadOnlyDictionary<string, object> parameters,
            string key)
        {
            return parameters != null &&
                   parameters.TryGetValue(key, out object value) &&
                   value != null
                ? Convert.ToString(value) ?? string.Empty
                : string.Empty;
        }

        private static int ReadCount(
            IReadOnlyDictionary<string, object> rewards,
            string key)
        {
            if (rewards == null ||
                !rewards.TryGetValue(key, out object value))
                return 0;
            try
            {
                return Math.Max(0, Convert.ToInt32(value));
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
