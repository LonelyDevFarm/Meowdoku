using System;
using System.Collections.Generic;
using Meowdoku.Core.Platform;
using UnityEngine;

namespace Meowdoku.Tests.PlayMode
{
    [DisallowMultipleComponent]
    public sealed class PlayModePlatformPermissionProvider : MonoBehaviour,
        IPlatformPermissionProvider
    {
        public bool IsAndroidValue;
        public bool IsIosValue;
        public bool IsMobileValue;
        public bool IsOnlineValue = true;
        public bool IsPrivacyRequiredValue;
        public bool IsConsentManagementRequiredValue;
        public bool CanShowTrackingAuthorizationValue;
        public bool IsNotificationPermissionEnabledValue;
        public TrackingAuthorizationStatus TrackingStatus =
            TrackingAuthorizationStatus.NotDetermined;

        public int AgreePrivacyCount { get; private set; }
        public int InitializeTrackingCount { get; private set; }
        public int ConsentCheckCount { get; private set; }
        public int ConsentShowCount { get; private set; }
        public int TrackingRequestCount { get; private set; }
        public int NotificationRequestCount { get; private set; }
        public int HideSplashMilliseconds { get; private set; }
        public int SetupScreenCount { get; private set; }
        public bool PushEnabled { get; private set; }
        public string TrackingSource { get; private set; } = string.Empty;
        public string NotificationPosition { get; private set; } = string.Empty;
        public NotificationPermissionRequestType NotificationRequestType {
            get;
            private set;
        }
        public List<string> RemovedNotificationIds { get; } = new();
        public List<DailyLocalNotification> SavedNotifications { get; } = new();

        public bool IsAndroid => IsAndroidValue;
        public bool IsIos => IsIosValue;
        public bool IsMobile => IsMobileValue;
        public bool IsOnline => IsOnlineValue;
        public bool IsPrivacyRequired => IsPrivacyRequiredValue;
        public bool IsConsentManagementRequired =>
            IsConsentManagementRequiredValue;
        public bool CanShowTrackingAuthorization =>
            CanShowTrackingAuthorizationValue;
        public bool IsNotificationPermissionEnabled =>
            IsNotificationPermissionEnabledValue;

        public void AgreePrivacy() => AgreePrivacyCount++;

        public void InitializeTrackingAuthorization() =>
            InitializeTrackingCount++;

        public TrackingAuthorizationStatus GetTrackingAuthorizationStatus() =>
            TrackingStatus;

        public void CheckConsentManagement(Action completed)
        {
            ConsentCheckCount++;
            completed?.Invoke();
        }

        public void ShowConsentManagement() => ConsentShowCount++;

        public void RequestTrackingAuthorization(
            string source,
            Action dismissed)
        {
            TrackingRequestCount++;
            TrackingSource = source ?? string.Empty;
            TrackingStatus = TrackingAuthorizationStatus.Authorized;
            dismissed?.Invoke();
        }

        public void RequestNotificationPermission(
            NotificationPermissionRequestType type,
            string position,
            Action completed)
        {
            NotificationRequestCount++;
            NotificationRequestType = type;
            NotificationPosition = position ?? string.Empty;
            completed?.Invoke();
        }

        public string GetLocalizedPrivacyUrl(string defaultUrl) =>
            defaultUrl ?? string.Empty;

        public void SetPushEnabled(bool enabled) => PushEnabled = enabled;

        public void RemoveLocalNotification(string id) =>
            RemovedNotificationIds.Add(id ?? string.Empty);

        public void SaveDailyLocalNotification(
            DailyLocalNotification notification)
        {
            if (notification != null) SavedNotifications.Add(notification);
        }

        public void HideNativeSplash(int milliseconds) =>
            HideSplashMilliseconds = milliseconds;

        public void SetupScreen() => SetupScreenCount++;

        public bool TryHandleShortcut() => false;
    }
}
