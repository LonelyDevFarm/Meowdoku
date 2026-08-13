using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Config;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;

namespace Meowdoku.Core.Platform
{
    /// <summary>
    /// Scene-owned port of the source Helpshift, in-app review and shortcut
    /// boundaries. Native SDK calls stay behind IProductServiceProvider.
    /// Sensitive source metadata is intentionally not forwarded yet; the
    /// provider receives empty payloads until that transfer is explicitly
    /// approved for the production adapter.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProductServiceRuntime : MonoBehaviour
    {
        private const string FeedbackShortcutTitle = "SHORTCUT_FEEDBACK_TITLE";
        private const string ImportantShortcutTitle = "SHORTCUT_IMPORTANT_TITLE";

        [SerializeField] private UIManager uiManager;
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private AbConfigRuntime abConfigRuntime;
        [SerializeField] private TrackingRuntime trackingRuntime;
        [SerializeField] private MonoBehaviour providerAdapter;

        private IProductServiceProvider _provider;
        private bool _helpActive;
        private int _unreadCount;
        private int _lifetimeVersion;
        private bool _uiEventsSubscribed;

        public event Action<int> HelpUnreadCountChanged;

        public IProductServiceProvider Provider =>
            _provider ??= providerAdapter as IProductServiceProvider ??
                         OfflineProductServiceProvider.Instance;
        public bool IsOnline => Provider.IsOnline;
        public bool IsHelpActive => _helpActive;
        public int HelpUnreadCount => _unreadCount;

        private void Awake()
        {
            AttachProvider(Provider);
        }

        public void InitializeProductServices()
        {
            SubscribeUiEvents();
            GameStateRuntime.Current.EnsureInstallVersion(
                string.IsNullOrEmpty(Provider.VersionName)
                    ? Application.version
                    : Provider.VersionName);
            Provider.RegisterShortcuts(
                Translate(FeedbackShortcutTitle, "Bugs? Contact us"),
                Translate(ImportantShortcutTitle, "Important"));
            PreheatHelp();
        }

        public void BindProvider(MonoBehaviour adapter)
        {
            providerAdapter = adapter;
            AttachProvider(adapter as IProductServiceProvider ??
                           OfflineProductServiceProvider.Instance);
        }

        public bool IsRateUsEligible(int level)
        {
            RateUsPopConfig config =
                abConfigRuntime?.Platform.RateUsPop ?? new RateUsPopConfig();
            GameStateService state = GameStateRuntime.Current;
            return config.IsEligibleAtGameWin(
                       level,
                       state.SessionConsecutiveWins) &&
                   !state.HasShownRateUs &&
                   Provider.IsOnline;
        }

        public IEnumerator TryShowRateUs(int level)
        {
            if (!IsRateUsEligible(level) || uiManager == null) yield break;

            GameStateRuntime.Current.MarkRateUsShown();
            RateUsPopUiConfig uiConfig =
                abConfigRuntime?.Platform.RateUsPopUi ??
                new RateUsPopUiConfig();
            UiName route = uiConfig.IsNewUi() ? UiName.RateUsV2 : UiName.RateUs;
            UIFrameWindow window = uiManager.Show(route);
            if (!(window is IRateUsWindow rateWindow)) yield break;

            bool closed = false;
            RateUsResult result = new(0, false);
            void HandleClosed(RateUsResult value)
            {
                result = value;
                closed = true;
            }

            rateWindow.Closed += HandleClosed;
            int version = _lifetimeVersion;
            while (!closed && version == _lifetimeVersion &&
                   window != null && window.IsShowing)
                yield return null;
            rateWindow.Closed -= HandleClosed;
            if (window != null && window.IsShowing)
                uiManager.Hide(route);
            if (!closed || version != _lifetimeVersion || !result.IsSubmitted)
                yield break;

            if (result.StarCount > 4)
            {
                Provider.RequestStoreReview();
                yield break;
            }

            yield return ShowFeedbackAndWait(true);
        }

        public void OpenFeedbackFaq()
        {
            EnsureHelpInstalled();
            if (!_helpActive) return;

            GameStateRuntime.Current.SetHelpLastOpenTime(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            // The source metadata/CIF transfer is intentionally deferred.
            Provider.ShowHelpFaq(
                new Dictionary<string, object>(),
                new Dictionary<string, object>());
            Provider.RequestHelpUnreadMessageCount(true);
        }

        public void RequestHelpUnread()
        {
            if (_helpActive)
                Provider.RequestHelpUnreadMessageCount(true);
        }

        public void PreheatHelp()
        {
            if (_helpActive) return;
            long last = GameStateRuntime.Current.HelpLastOpenTime;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (last <= 0L ||
                now - last > HelpSupportConfiguration.ActiveWindowSeconds)
                return;
            EnsureHelpInstalled();
            RequestHelpUnread();
        }

        public bool TryHandleShortcut()
        {
            ProductShortcutAction action = Provider.ConsumeShortcut();
            if (action == ProductShortcutAction.None) return false;
            trackingRuntime?.Tracker.TrackRemoveAppStart();
            if (action == ProductShortcutAction.Feedback)
                StartCoroutine(OpenFeedbackFromShortcut());
            return action == ProductShortcutAction.Feedback;
        }

        public IEnumerator ShowFeedbackAndWait(bool asDialog)
        {
            if (uiManager == null) yield break;
            var parameters = new Dictionary<string, object>(1)
            {
                ["as_dlg"] = asDialog
            };
            UIFrameWindow window = uiManager.Show(UiName.Feedback, parameters);
            if (!(window is IFeedbackWindow feedback)) yield break;

            bool closed = false;
            void HandleClosed() => closed = true;
            feedback.Closed += HandleClosed;
            int version = _lifetimeVersion;
            while (!closed && version == _lifetimeVersion &&
                   window != null && window.IsShowing)
                yield return null;
            feedback.Closed -= HandleClosed;
            if (window != null && window.IsShowing)
                uiManager.Hide(UiName.Feedback);
        }

        private IEnumerator OpenFeedbackFromShortcut()
        {
            int version = _lifetimeVersion;
            while (version == _lifetimeVersion && uiManager != null &&
                   uiManager.Get(UiName.Splash)?.IsShowing == true)
                yield return null;
            if (version != _lifetimeVersion) yield break;

            yield return ShowFeedbackAndWait(false);
            if (uiManager == null || version != _lifetimeVersion) yield break;
            if (uiManager.Get(UiName.Game)?.IsShowing == true ||
                uiManager.Get(UiName.DailyGame)?.IsShowing == true)
                yield break;

            UiName route = GameStateRuntime.Current.TutorialDone
                ? UiName.Home
                : UiName.Tutorial;
            if (uiManager.Get(route)?.IsShowing != true)
                uiManager.Show(route);
        }

        private void EnsureHelpInstalled()
        {
            if (_helpActive || !Provider.IsHelpAvailable) return;
            if (Application.platform != RuntimePlatform.IPhonePlayer)
                Provider.InstallHelp(
                    HelpSupportConfiguration.AndroidAppId,
                    HelpSupportConfiguration.IosPlatformId,
                    HelpSupportConfiguration.IosApiKey,
                    HelpSupportConfiguration.Domain);
            _helpActive = true;
            Provider.RequestHelpUnreadMessageCount(true);
        }

        private void AttachProvider(IProductServiceProvider provider)
        {
            if (_provider != null)
                _provider.HelpUnreadCountChanged -= HandleUnreadCount;
            _provider = provider ?? OfflineProductServiceProvider.Instance;
            _provider.HelpUnreadCountChanged += HandleUnreadCount;
            _helpActive = false;
            _unreadCount = 0;
        }

        private void HandleUnreadCount(int count)
        {
            _unreadCount = Mathf.Max(0, count);
            HelpUnreadCountChanged?.Invoke(_unreadCount);
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string result = localization.Translate(key);
            return result == key ? fallback : result;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) RequestHelpUnread();
        }

        private void SubscribeUiEvents()
        {
            if (_uiEventsSubscribed || uiManager == null) return;
            uiManager.Events.WindowShown += HandleWindowShown;
            _uiEventsSubscribed = true;
        }

        private void HandleWindowShown(UiName name, UIFrameWindow window)
        {
            if (name == UiName.Home || name == UiName.Game ||
                name == UiName.DailyGame || name == UiName.Setting)
                RequestHelpUnread();
        }

        private void OnDestroy()
        {
            _lifetimeVersion++;
            if (_uiEventsSubscribed && uiManager != null)
                uiManager.Events.WindowShown -= HandleWindowShown;
            _uiEventsSubscribed = false;
            if (_provider != null)
                _provider.HelpUnreadCountChanged -= HandleUnreadCount;
            HelpUnreadCountChanged = null;
        }
    }
}
