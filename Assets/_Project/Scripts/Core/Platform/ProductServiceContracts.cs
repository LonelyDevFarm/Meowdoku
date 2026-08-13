using System;
using System.Collections.Generic;
using Meowdoku.Core.UI;

namespace Meowdoku.Core.Platform
{
    public enum ProductShortcutAction
    {
        None = 0,
        Feedback = 1,
        Important = 2
    }

    public readonly struct ProductIdentitySnapshot
    {
        public ProductIdentitySnapshot(
            string uuid,
            string luid,
            string abDyeingTag,
            string country,
            string mediaSource,
            string flowDomain)
        {
            Uuid = uuid ?? string.Empty;
            Luid = luid ?? string.Empty;
            AbDyeingTag = abDyeingTag ?? string.Empty;
            Country = country ?? string.Empty;
            MediaSource = mediaSource ?? string.Empty;
            FlowDomain = flowDomain ?? string.Empty;
        }

        public string Uuid { get; }
        public string Luid { get; }
        public string AbDyeingTag { get; }
        public string Country { get; }
        public string MediaSource { get; }
        public string FlowDomain { get; }
    }

    public interface IProductServiceProvider
    {
        bool IsOnline { get; }
        bool IsHelpAvailable { get; }
        string VersionName { get; }
        ProductIdentitySnapshot Identity { get; }
        event Action<int> HelpUnreadCountChanged;

        void InstallHelp(
            string androidAppId,
            string iosPlatformId,
            string iosApiKey,
            string domain);
        void ShowHelpFaq(
            IReadOnlyDictionary<string, object> metadata,
            IReadOnlyDictionary<string, object> customIssueFields);
        void RequestHelpUnreadMessageCount(bool fromCache);
        void RequestStoreReview();
        void RegisterShortcuts(string feedbackTitle, string importantTitle);
        ProductShortcutAction ConsumeShortcut();
    }

    public sealed class OfflineProductServiceProvider : IProductServiceProvider
    {
        public static readonly OfflineProductServiceProvider Instance = new();
        private OfflineProductServiceProvider() { }

        public bool IsOnline => false;
        public bool IsHelpAvailable => false;
        public string VersionName => string.Empty;
        public ProductIdentitySnapshot Identity => new();
        public event Action<int> HelpUnreadCountChanged
        {
            add { }
            remove { }
        }

        public void InstallHelp(
            string androidAppId,
            string iosPlatformId,
            string iosApiKey,
            string domain) { }

        public void ShowHelpFaq(
            IReadOnlyDictionary<string, object> metadata,
            IReadOnlyDictionary<string, object> customIssueFields) { }

        public void RequestHelpUnreadMessageCount(bool fromCache) { }
        public void RequestStoreReview() { }
        public void RegisterShortcuts(string feedbackTitle, string importantTitle) { }
        public ProductShortcutAction ConsumeShortcut() => ProductShortcutAction.None;
    }

    public sealed class HelpSupportConfiguration
    {
        public const string AndroidAppId =
            "arsenal-support_platform_20260610074440920-419e5d01b34cf98";
        public const string IosPlatformId =
            "arsenal-support_platform_20260610074440901-cc1ce66e7ed9026";
        public const string IosApiKey =
            "f6e712714ec70365ca39e75ec59799f2";
        public const string Domain = "arsenal-support.helpshift.com";
        public const string DotId = "helpshift_unread";
        public const long ActiveWindowSeconds = 2L * 86400L;
    }

    public readonly struct RateUsResult
    {
        public RateUsResult(int starCount, bool isSubmitted)
        {
            StarCount = starCount;
            IsSubmitted = isSubmitted;
        }

        public int StarCount { get; }
        public bool IsSubmitted { get; }
    }

    public interface IRateUsWindow
    {
        event Action<RateUsResult> Closed;
    }

    public interface IFeedbackWindow
    {
        event Action Closed;
        bool IsSubmitted { get; }
    }

    public interface IProductServiceRuntimeConsumer
    {
        void BindProductServiceRuntime(ProductServiceRuntime runtime);
    }
}
