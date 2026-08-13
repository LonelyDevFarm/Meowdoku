using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// UGUI port of common/confirm_dialog.gd. The caller supplies optional
    /// localization keys and callbacks; hiding begins before the callback,
    /// matching the source's button ordering.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConfirmDialogPresenter : UIFrameWindow
    {
        private const string DefaultTitleKey = "DIALOG_QUIT_TITLE";
        private const string DefaultContentKey = "DIALOG_QUIT_MSG";
        private const string DefaultButtonKey = "DIALOG_QUIT_BTN";

        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private Text titleText;
        [SerializeField] private Text contentText;
        [SerializeField] private Text actionText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button confirmCloseButton;
        [SerializeField] private LocalizationCatalog localization;

        private Action _onConfirm;
        private Action _onClose;
        private bool _closing;

        protected override bool UsesDefaultCloseButton => false;

#if UNITY_INCLUDE_TESTS
        internal string TitleForTests => titleText != null
            ? titleText.text
            : string.Empty;
        internal string ContentForTests => contentText != null
            ? contentText.text
            : string.Empty;
        internal string ActionForTests => actionText != null
            ? actionText.text
            : string.Empty;
#endif

        protected override void OnCreate()
        {
            if (actionButton != null)
                actionButton.onClick.AddListener(Confirm);
            if (confirmCloseButton != null)
                confirmCloseButton.onClick.AddListener(Close);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _closing = false;
            _onConfirm = ReadAction(parameters, "on_confirm");
            _onClose = ReadAction(parameters, "on_close");
            SetText(titleText, ResolveText(
                parameters,
                "title",
                DefaultTitleKey,
                "Quit"));
            SetText(contentText, ResolveText(
                parameters,
                "content",
                DefaultContentKey,
                "Are you sure you want to quit?"));
            SetText(actionText, ResolveText(
                parameters,
                "btn_text",
                DefaultButtonKey,
                "Quit"));
            popupAnimator?.PlayOpen();
        }

        protected override IEnumerator PlayCloseAnimation()
        {
            if (popupAnimator != null)
                yield return popupAnimator.PlayClose();
        }

        protected override IEnumerator OnHide()
        {
            popupAnimator?.Stop();
            _closing = false;
            _onConfirm = null;
            _onClose = null;
            yield break;
        }

        protected override void OnDestroyWindow()
        {
            if (actionButton != null)
                actionButton.onClick.RemoveListener(Confirm);
            if (confirmCloseButton != null)
                confirmCloseButton.onClick.RemoveListener(Close);
            _onConfirm = null;
            _onClose = null;
            base.OnDestroyWindow();
        }

        private void Confirm()
        {
            if (_closing) return;
            _closing = true;
            Action callback = _onConfirm;
            Owner?.Hide(UiName);
            callback?.Invoke();
        }

        private void Close()
        {
            if (_closing) return;
            _closing = true;
            Action callback = _onClose;
            Owner?.Hide(UiName);
            callback?.Invoke();
        }

        private string ResolveText(
            IReadOnlyDictionary<string, object> parameters,
            string parameterName,
            string defaultKey,
            string fallback)
        {
            string token = ReadString(parameters, parameterName, defaultKey);
            if (localization == null)
                return token == defaultKey ? fallback : token;
            string translated = localization.Translate(token);
            return string.IsNullOrEmpty(translated) ||
                   translated == defaultKey
                ? fallback
                : translated;
        }

        private static Action ReadAction(
            IReadOnlyDictionary<string, object> parameters,
            string key)
        {
            return parameters != null &&
                   parameters.TryGetValue(key, out object value)
                ? value as Action
                : null;
        }

        private static string ReadString(
            IReadOnlyDictionary<string, object> parameters,
            string key,
            string fallback)
        {
            return parameters != null &&
                   parameters.TryGetValue(key, out object value) &&
                   value is string text &&
                   !string.IsNullOrEmpty(text)
                ? text
                : fallback;
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }
    }
}
