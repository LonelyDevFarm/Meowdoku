using System.Collections.Generic;

namespace Meowdoku.Core.Online
{
    public enum ApiRuntimePlatform
    {
        Android = 0,
        Ios = 1
    }

    /// <summary>
    /// Source contract from core/net/api_config.gd. Transport and native SDK
    /// adapters consume this catalog; gameplay never owns endpoints or codes.
    /// </summary>
    public static class ApiConfig
    {
        public const string AppId = "com.oakever.meowdoku";
        public const string DevelopmentBaseUrl =
            "https://meowdoku-api-dev.dailyinnovation.biz";
        public const string ProductionBaseUrl =
            "https://meowdoku-api.dailyinnovation.biz";
        public const string SignSecret =
            "htqcuibn9AFeG8LGZqmSvWzaATJB7r";

        public const string SyncGameDataPath = "/sync/v1/gamedata";
        public const string SyncGameDataMetaPath =
            "/sync/v1/gamedata/meta";
        public const string AccountInfoPath = "/account/v1/info";

        public const int CodeOk = 0;
        public const int CodeAccountDeleted = 1003;
        public const int CodeAccessTokenInvalid = 1004;
        public const int CodeAccessTokenExpired = 1005;
        public const int CodeSignInvalid = 1006;
        public const int CodeTimestampExpired = 1007;
        public const int CodeRefreshTokenInvalid = 1008;
        public const int CodeRefreshTokenExpired = 1009;
        public const int CodeNoSave = 2001;
        public const int CodeSchemaTooLow = 2002;
        public const int CodeSyncCodeTooLow = 2003;

        public static bool IsProductionBuild
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return false;
#else
                return true;
#endif
            }
        }

        public static string BaseUrl(bool production) => production
            ? ProductionBaseUrl
            : DevelopmentBaseUrl;

        public static string Platform(ApiRuntimePlatform platform) =>
            platform == ApiRuntimePlatform.Ios ? "ios" : "android";

        public static string CurrentPlatform
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return Platform(ApiRuntimePlatform.Ios);
#else
                return Platform(ApiRuntimePlatform.Android);
#endif
            }
        }

        public static IReadOnlyDictionary<string, object>
            BuildAuthInitialization(
                string localUserId,
                bool production,
                bool showLog)
        {
            return new Dictionary<string, object>
            {
                ["base_url"] = BaseUrl(production),
                ["secret"] = SignSecret,
                ["luid"] = localUserId ?? string.Empty,
                ["show_log"] = showLog,
                ["is_keychain_sync_enabled"] = false
            };
        }
    }
}
