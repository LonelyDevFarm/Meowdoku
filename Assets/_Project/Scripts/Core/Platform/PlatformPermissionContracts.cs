using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Platform
{
    public enum TrackingAuthorizationStatus
    {
        NotDetermined = 0,
        Authorized = 1,
        SystemDenied = 2,
        AppDenied = 3
    }

    public enum NotificationPermissionRequestType
    {
        System = 1,
        SystemAndSetting = 2,
        Setting = 3
    }

    public enum PushGuideCloseSource
    {
        CloseButton = 0,
        AllowButton = 1
    }

    public interface IPrivacyDialogWindow
    {
        event Action Accepted;
    }

    public interface IPreAttGuideWindow
    {
        event Action Continued;
    }

    public interface IPrePushGuideWindow
    {
        event Action<PushGuideCloseSource> Closed;
    }

    public interface IPlatformPermissionRuntimeConsumer
    {
        void BindPlatformPermissionRuntime(PrivacyPermissionRuntime runtime);
    }

    public sealed class LocalNotificationContent
    {
        public LocalNotificationContent(string title, string body)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
        }

        public string Title { get; }
        public string Body { get; }
    }

    public sealed class DailyLocalNotification
    {
        public DailyLocalNotification(
            string id,
            DateTime localRegistrationTime,
            int localHour,
            int localMinute,
            IReadOnlyList<LocalNotificationContent> contents,
            int disturbType = 1)
        {
            Id = id ?? string.Empty;
            LocalRegistrationTime = localRegistrationTime;
            LocalHour = localHour;
            LocalMinute = localMinute;
            Contents = contents ?? Array.Empty<LocalNotificationContent>();
            DisturbType = disturbType;
        }

        public string Id { get; }
        public DateTime LocalRegistrationTime { get; }
        public int LocalHour { get; }
        public int LocalMinute { get; }
        public IReadOnlyList<LocalNotificationContent> Contents { get; }
        public int DisturbType { get; }
        public bool AdvanceOneDay =>
            LocalRegistrationTime.Hour > LocalHour ||
            (LocalRegistrationTime.Hour == LocalHour &&
             LocalRegistrationTime.Minute >= LocalMinute);
        public long RepeatIntervalMilliseconds => 86_400_000L;
        public bool IsInfiniteRepeat => true;
    }

    /// <summary>
    /// SDK-neutral equivalent of the privacy/CMP/ATT/push portion of
    /// UniKitManager. A native adapter owns platform dialogs and local
    /// notification persistence; game flow owns ordering and save state.
    /// </summary>
    public interface IPlatformPermissionProvider
    {
        bool IsAndroid { get; }
        bool IsIos { get; }
        bool IsMobile { get; }
        bool IsOnline { get; }
        bool IsPrivacyRequired { get; }
        bool IsConsentManagementRequired { get; }
        bool CanShowTrackingAuthorization { get; }
        bool IsNotificationPermissionEnabled { get; }

        void AgreePrivacy();
        void InitializeTrackingAuthorization();
        TrackingAuthorizationStatus GetTrackingAuthorizationStatus();
        void CheckConsentManagement(Action completed);
        void ShowConsentManagement();
        void RequestTrackingAuthorization(string source, Action dismissed);
        void RequestNotificationPermission(
            NotificationPermissionRequestType type,
            string position,
            Action completed);
        string GetLocalizedPrivacyUrl(string defaultUrl);
        void SetPushEnabled(bool enabled);
        void RemoveLocalNotification(string id);
        void SaveDailyLocalNotification(DailyLocalNotification notification);
        void HideNativeSplash(int milliseconds);
        void SetupScreen();
        bool TryHandleShortcut();
    }

    public sealed class OfflinePlatformPermissionProvider :
        IPlatformPermissionProvider
    {
        public static readonly OfflinePlatformPermissionProvider Instance = new();
        private OfflinePlatformPermissionProvider() { }

        public bool IsAndroid => false;
        public bool IsIos => false;
        public bool IsMobile => false;
        public bool IsOnline => false;
        public bool IsPrivacyRequired => false;
        public bool IsConsentManagementRequired => false;
        public bool CanShowTrackingAuthorization => false;
        public bool IsNotificationPermissionEnabled => false;
        public void AgreePrivacy() { }
        public void InitializeTrackingAuthorization() { }
        public TrackingAuthorizationStatus GetTrackingAuthorizationStatus() =>
            TrackingAuthorizationStatus.NotDetermined;
        public void CheckConsentManagement(Action completed) => completed?.Invoke();
        public void ShowConsentManagement() { }
        public void RequestTrackingAuthorization(string source, Action dismissed) =>
            dismissed?.Invoke();
        public void RequestNotificationPermission(
            NotificationPermissionRequestType type,
            string position,
            Action completed) => completed?.Invoke();
        public string GetLocalizedPrivacyUrl(string defaultUrl) =>
            defaultUrl ?? string.Empty;
        public void SetPushEnabled(bool enabled) { }
        public void RemoveLocalNotification(string id) { }
        public void SaveDailyLocalNotification(DailyLocalNotification notification) { }
        public void HideNativeSplash(int milliseconds) { }
        public void SetupScreen() { }
        public bool TryHandleShortcut() => false;
    }
}
