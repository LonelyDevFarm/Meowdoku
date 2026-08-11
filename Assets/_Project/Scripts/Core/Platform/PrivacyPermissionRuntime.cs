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
    /// Scene-owned port of launcher.gd privacy/CMP/ATT/push orchestration.
    /// Native SDK work remains behind IPlatformPermissionProvider.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrivacyPermissionRuntime : MonoBehaviour,
        IAppStartupExternalServices,
        ISettingsExternalServices
    {
        private const string AttSource = "splash_scr";
        private const string StartupPushPosition = "app_start";
        private const string GuidePushPosition = "push_guide";
        private const int StartupPushAskMaximum = 2;
        private const float DailyNotificationRegistrationDelay = 0.5f;
        private const float PostAttDelaySeconds = 1f;

        [SerializeField] private UIManager uiManager;
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private AbConfigRuntime abConfigRuntime;
        [SerializeField] private TrackingRuntime trackingRuntime;
        [SerializeField] private MonoBehaviour providerAdapter;

        private IPlatformPermissionProvider _provider;
        private Coroutine _dailyNotificationRegistration;
        private Action _pendingPushCompletion;
        private bool _pushRequestPending;
        private bool _privacyAndPushCompleted;
        private int _lifetimeVersion;

        public bool IsOnline => Provider.IsOnline;
        public bool IsConsentManagementRequired =>
            Provider.IsConsentManagementRequired;
        public bool IsDataSyncAvailable => false;
        public bool IsNotificationPermissionEnabled =>
            Provider.IsNotificationPermissionEnabled;
        public IPlatformPermissionProvider Provider =>
            _provider ??= providerAdapter as IPlatformPermissionProvider ??
                         OfflinePlatformPermissionProvider.Instance;

        private void Awake()
        {
            _ = Provider;
        }

        public void ApplySystemLocale(GameStateService gameState) { }

        public void HideNativeSplash(int milliseconds)
        {
            Provider.HideNativeSplash(milliseconds);
        }

        public IEnumerator AwaitPrivacyAndPush()
        {
            if (_privacyAndPushCompleted) yield break;
            yield return AwaitPrivacyAcceptance();
            Provider.InitializeTrackingAuthorization();

            GameStateService state = GameStateRuntime.Current;
            if (Provider.IsMobile &&
                state.PushAskCount < StartupPushAskMaximum)
            {
                yield return AwaitNotificationPermission(
                    NotificationPermissionRequestType.System,
                    StartupPushPosition);
                state.IncrementPushAskCount();
                state.MarkPushGuideTriggered();
            }

            Provider.SetPushEnabled(true);
            Provider.RemoveLocalNotification(
                DailyLocalNotificationFactory.NoonId);
            Provider.RemoveLocalNotification(
                DailyLocalNotificationFactory.EveningId);
            QueueDailyNotificationRegistration();
            _privacyAndPushCompleted = true;
        }

        public IEnumerator AwaitConsentAndTracking(float maximumSeconds)
        {
            if (Provider.IsAndroid)
            {
                Provider.CheckConsentManagement(null);
                yield break;
            }

            GameStateService state = GameStateRuntime.Current;
            TrackingAuthorizationStatus status =
                Provider.GetTrackingAuthorizationStatus();
            if (state.HasShownAttGuide ||
                status != TrackingAuthorizationStatus.NotDetermined)
            {
                int version = _lifetimeVersion;
                Provider.CheckConsentManagement(() =>
                {
                    if (version != _lifetimeVersion || !isActiveAndEnabled)
                        return;
                    StartCoroutine(RunAttFlow(null));
                });
                yield break;
            }

            bool consentDone = false;
            bool attNeeded = false;
            bool attFlowDone = false;
            int lifetime = _lifetimeVersion;
            Provider.CheckConsentManagement(() =>
            {
                if (lifetime != _lifetimeVersion) return;
                consentDone = true;
                if (!Provider.CanShowTrackingAuthorization) return;
                attNeeded = true;
                StartCoroutine(RunAttFlow(() => attFlowDone = true));
            });

            float deadline = Time.realtimeSinceStartup +
                             Mathf.Max(0f, maximumSeconds);
            while (!consentDone &&
                   lifetime == _lifetimeVersion &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!consentDone || !attNeeded ||
                lifetime != _lifetimeVersion)
                yield break;
            while (!attFlowDone && lifetime == _lifetimeVersion)
                yield return null;
            if (lifetime == _lifetimeVersion)
                yield return new WaitForSecondsRealtime(PostAttDelaySeconds);
        }

        public IEnumerator AwaitRemoteDefaults(float maximumSeconds)
        {
            yield break;
        }

        public void SetupScreen()
        {
            Provider.SetupScreen();
        }

        public IEnumerator AwaitDataSync(float maximumSeconds)
        {
            yield break;
        }

        public bool TryHandleShortcut() => Provider.TryHandleShortcut();

        public void OpenFeedbackFaq() { }

        public void ShowConsentManagement()
        {
            Provider.ShowConsentManagement();
        }

        public void OpenLocalizedPrivacyUrl(string defaultUrl)
        {
            string url = Provider.GetLocalizedPrivacyUrl(defaultUrl);
            if (!string.IsNullOrWhiteSpace(url)) Application.OpenURL(url);
        }

        public void PrepareNormalGameEnd(int level)
        {
            if (level >= PushGuidePolicy.MinimumLevel)
                abConfigRuntime?.ReloadTiming(
                    AbConfigTiming.GameEndNormal20);
        }

        public bool IsPushGuideEligible(int level)
        {
            PushPermissionConfig config =
                abConfigRuntime?.Platform.PushPermission ??
                new PushPermissionConfig();
            return PushGuidePolicy.IsEligible(
                level,
                GameStateRuntime.Current,
                config);
        }

        public IEnumerator TryShowPushGuide(int level)
        {
            if (!IsPushGuideEligible(level) || uiManager == null)
                yield break;

            GameStateService state = GameStateRuntime.Current;
            int askCount = state.PushAskCount;
            int showCount = state.PushGuidePopupCount + 1;
            UIFrameWindow window = uiManager.Show(
                UiName.PrePushGuide,
                new Dictionary<string, object>(1)
                {
                    ["show_count"] = showCount
                });
            if (window is not IPrePushGuideWindow guide) yield break;

            bool closed = false;
            PushGuideCloseSource closeSource =
                PushGuideCloseSource.CloseButton;
            void HandleClosed(PushGuideCloseSource source)
            {
                closeSource = source;
                closed = true;
            }
            guide.Closed += HandleClosed;
            while (!closed && window != null && window.IsShowing)
                yield return null;
            guide.Closed -= HandleClosed;
            if (!closed) yield break;
            uiManager.Hide(UiName.PrePushGuide);

            if (closeSource == PushGuideCloseSource.AllowButton)
            {
                NotificationPermissionRequestType requestType;
                if (askCount < StartupPushAskMaximum)
                {
                    requestType =
                        NotificationPermissionRequestType.SystemAndSetting;
                    state.IncrementPushAskCount();
                }
                else
                    requestType = NotificationPermissionRequestType.Setting;
                yield return AwaitNotificationPermission(
                    requestType,
                    GuidePushPosition);
                trackingRuntime?.Tracker.TrackPushGuideResult(
                    Provider.IsNotificationPermissionEnabled,
                    showCount);
            }

            state.MarkPushGuideTriggered();
            state.MarkPushGuidePopupShown();
        }

        public IEnumerator AwaitNotificationPermission(
            NotificationPermissionRequestType type,
            string position)
        {
            while (_pushRequestPending) yield return null;
            bool completed = false;
            int version = _lifetimeVersion;
            void Complete()
            {
                if (version != _lifetimeVersion || completed) return;
                completed = true;
                _pushRequestPending = false;
                _pendingPushCompletion = null;
            }

            _pushRequestPending = true;
            _pendingPushCompletion = Complete;
            Provider.RequestNotificationPermission(
                type,
                position ?? string.Empty,
                Complete);
            while (!completed && version == _lifetimeVersion)
                yield return null;
        }

        public void BindProvider(MonoBehaviour adapter)
        {
            providerAdapter = adapter;
            _provider = adapter as IPlatformPermissionProvider ??
                        OfflinePlatformPermissionProvider.Instance;
        }

        internal void ConfigureForTests(
            UIManager manager,
            AbConfigRuntime abRuntime = null,
            LocalizationCatalog catalog = null,
            TrackingRuntime tracking = null)
        {
            uiManager = manager;
            abConfigRuntime = abRuntime;
            localization = catalog;
            trackingRuntime = tracking;
        }

        private IEnumerator AwaitPrivacyAcceptance()
        {
            if (!Provider.IsPrivacyRequired || uiManager == null) yield break;
            UIFrameWindow window = uiManager.Show(UiName.Privacy);
            if (window is not IPrivacyDialogWindow dialog) yield break;
            bool accepted = false;
            void HandleAccepted() => accepted = true;
            dialog.Accepted += HandleAccepted;
            while (!accepted && window != null && window.IsShowing)
                yield return null;
            dialog.Accepted -= HandleAccepted;
            if (!accepted) yield break;
            uiManager.Hide(UiName.Privacy);
            Provider.AgreePrivacy();
        }

        private IEnumerator RunAttFlow(Action completed)
        {
            bool canRequest = Provider.GetTrackingAuthorizationStatus() ==
                              TrackingAuthorizationStatus.NotDetermined;
            AttDialogLogicConfig config =
                abConfigRuntime?.Platform.AttDialogLogic ??
                new AttDialogLogicConfig();
            if (config.ShouldSkipCustomGuide())
            {
                if (canRequest)
                    yield return AwaitTrackingAuthorization();
                completed?.Invoke();
                yield break;
            }

            GameStateService state = GameStateRuntime.Current;
            if (!state.HasShownAttGuide)
            {
                UiName pageName = config.IsCustomGuideRestyled()
                    ? UiName.PreAttGuideV2
                    : UiName.PreAttGuide;
                UIFrameWindow window = uiManager != null
                    ? uiManager.Show(pageName)
                    : null;
                if (window is IPreAttGuideWindow guide)
                {
                    bool continued = false;
                    void HandleContinued() => continued = true;
                    guide.Continued += HandleContinued;
                    while (!continued && window != null && window.IsShowing)
                        yield return null;
                    guide.Continued -= HandleContinued;
                    if (continued) uiManager.Hide(pageName);
                }
                state.MarkAttGuideShown();
            }

            if (canRequest)
                yield return AwaitTrackingAuthorization();
            completed?.Invoke();
        }

        private IEnumerator AwaitTrackingAuthorization()
        {
            bool dismissed = false;
            int version = _lifetimeVersion;
            Provider.RequestTrackingAuthorization(
                AttSource,
                () =>
                {
                    if (version == _lifetimeVersion) dismissed = true;
                });
            while (!dismissed && version == _lifetimeVersion)
                yield return null;
        }

        private void QueueDailyNotificationRegistration()
        {
            if (_dailyNotificationRegistration != null)
                StopCoroutine(_dailyNotificationRegistration);
            _dailyNotificationRegistration =
                StartCoroutine(RegisterDailyNotifications());
        }

        private IEnumerator RegisterDailyNotifications()
        {
            while (abConfigRuntime != null &&
                   !abConfigRuntime.IsAppStartFinalized)
                yield return null;
            yield return new WaitForSecondsRealtime(
                DailyNotificationRegistrationDelay);

            PushLocalTextConfig config =
                abConfigRuntime?.Platform.PushLocalText ??
                new PushLocalTextConfig();
            IReadOnlyList<DailyLocalNotification> notifications =
                DailyLocalNotificationFactory.Build(
                    config,
                    Translate,
                    DateTime.Now);
            for (int index = 0; index < notifications.Count; index++)
                Provider.SaveDailyLocalNotification(notifications[index]);
            _dailyNotificationRegistration = null;
        }

        private string Translate(string key)
        {
            if (localization == null) return key ?? string.Empty;
            return localization.Translate(key);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused && _pushRequestPending)
                StartCoroutine(ResolvePendingPushNextFrame());
        }

        private IEnumerator ResolvePendingPushNextFrame()
        {
            yield return null;
            if (_pushRequestPending) _pendingPushCompletion?.Invoke();
        }

        private void OnDestroy()
        {
            _lifetimeVersion++;
            if (_dailyNotificationRegistration != null)
                StopCoroutine(_dailyNotificationRegistration);
            _dailyNotificationRegistration = null;
            _pushRequestPending = false;
            _pendingPushCompletion = null;
        }
    }
}
