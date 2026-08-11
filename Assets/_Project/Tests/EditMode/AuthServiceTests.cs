using System;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Online;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class AuthServiceTests
    {
        [Test]
        public void ApiConfig_PreservesSourceEndpointsCodesAndAuthPayload()
        {
            Assert.That(ApiConfig.AppId,
                Is.EqualTo("com.oakever.meowdoku"));
            Assert.That(ApiConfig.BaseUrl(false),
                Is.EqualTo(ApiConfig.DevelopmentBaseUrl));
            Assert.That(ApiConfig.BaseUrl(true),
                Is.EqualTo(ApiConfig.ProductionBaseUrl));
            Assert.That(ApiConfig.Platform(ApiRuntimePlatform.Android),
                Is.EqualTo("android"));
            Assert.That(ApiConfig.Platform(ApiRuntimePlatform.Ios),
                Is.EqualTo("ios"));
            Assert.That(ApiConfig.SyncGameDataPath,
                Is.EqualTo("/sync/v1/gamedata"));
            Assert.That(ApiConfig.CodeNoSave, Is.EqualTo(2001));
            Assert.That(ApiConfig.CodeSyncCodeTooLow, Is.EqualTo(2003));

            IReadOnlyDictionary<string, object> payload =
                ApiConfig.BuildAuthInitialization(
                    "luid-7",
                    production: false,
                    showLog: true);
            Assert.That(payload.Keys, Is.EquivalentTo(new[]
            {
                "base_url",
                "secret",
                "luid",
                "show_log",
                "is_keychain_sync_enabled"
            }));
            Assert.That(payload["luid"], Is.EqualTo("luid-7"));
            Assert.That(payload["show_log"], Is.True);
            Assert.That(payload["is_keychain_sync_enabled"], Is.False);
        }

        [Test]
        public void Bootstrap_WaitsForAnalyticsAndLuidThenStartsGuestOnce()
        {
            var provider = new FakeAuthProvider();
            var prerequisites = new FakePrerequisites();
            var service = new AuthService(
                provider,
                prerequisites,
                new MutableClock(1_000),
                production: false,
                showLog: true);

            service.Start();
            prerequisites.EmitAnalyticsInitialized();
            Assert.That(provider.InitializeCount, Is.Zero);
            prerequisites.EmitLocalUserId("local-user");

            Assert.That(provider.InitializeCount, Is.EqualTo(1));
            Assert.That(provider.LoginCount, Is.EqualTo(1));
            Assert.That(service.IsInitialized, Is.True);
            var config = (IReadOnlyDictionary<string, object>)
                MiniJson.Deserialize(provider.LastConfiguration);
            Assert.That(config["base_url"],
                Is.EqualTo(ApiConfig.DevelopmentBaseUrl));
            Assert.That(config["luid"], Is.EqualTo("local-user"));
            Assert.That(config["show_log"], Is.True);

            prerequisites.EmitAnalyticsInitialized();
            prerequisites.EmitLocalUserId("another-user");
            Assert.That(provider.InitializeCount, Is.EqualTo(1));
            Assert.That(provider.LoginCount, Is.EqualTo(1));
            service.Dispose();
        }

        [Test]
        public void TokenRequest_UsesProviderResultAndExactTwelveSecondTimeout()
        {
            var provider = new FakeAuthProvider();
            var prerequisites = new FakePrerequisites(true, "luid");
            var clock = new MutableClock(2_000);
            var service = new AuthService(
                provider,
                prerequisites,
                clock,
                production: false,
                showLog: false);
            service.Start();

            AuthTokenResult first = default;
            Assert.That(service.RequestAccessToken(
                false,
                value => first = value), Is.True);
            Assert.That(provider.TokenRequestCount, Is.EqualTo(1));
            provider.EmitToken(
                MiniJson.Serialize(new Dictionary<string, object>
                {
                    ["token"] = "abc",
                    ["code"] = 0,
                    ["msg"] = string.Empty
                }));
            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.Token, Is.EqualTo("abc"));
            Assert.That(service.PendingTokenRequestCount, Is.Zero);

            AuthTokenResult timeout = default;
            service.RequestAccessToken(true, value => timeout = value);
            Assert.That(provider.LastForceRefresh, Is.True);
            clock.Milliseconds +=
                AuthService.AccessTokenTimeoutMilliseconds - 1;
            service.Tick();
            Assert.That(service.PendingTokenRequestCount, Is.EqualTo(1));
            clock.Milliseconds++;
            service.Tick();
            Assert.That(timeout.Code,
                Is.EqualTo(AuthErrorCode.TokenTimeout));
            Assert.That(timeout.Token, Is.Empty);
            Assert.That(service.PendingTokenRequestCount, Is.Zero);
            service.Dispose();
        }

        [Test]
        public void LoginExpiry_DebouncesCapsAndSuccessResetsConsecutiveCount()
        {
            var provider = new FakeAuthProvider();
            var prerequisites = new FakePrerequisites(true, "luid");
            var clock = new MutableClock(1_000);
            var service = new AuthService(
                provider,
                prerequisites,
                clock,
                production: false,
                showLog: false);
            service.Start();
            Assert.That(provider.LoginCount, Is.EqualTo(1));

            provider.EmitLoginExpired();
            Assert.That(provider.LoginCount, Is.EqualTo(2));
            clock.Milliseconds +=
                AuthService.ReloginMinimumIntervalMilliseconds - 1;
            provider.EmitLoginExpired();
            Assert.That(provider.LoginCount, Is.EqualTo(2));

            for (int attempt = 2;
                 attempt <= AuthService.ReloginMaximumConsecutive;
                 attempt++)
            {
                clock.Milliseconds +=
                    AuthService.ReloginMinimumIntervalMilliseconds;
                provider.EmitLoginExpired();
            }
            Assert.That(provider.LoginCount,
                Is.EqualTo(1 + AuthService.ReloginMaximumConsecutive));
            clock.Milliseconds +=
                AuthService.ReloginMinimumIntervalMilliseconds;
            provider.EmitLoginExpired();
            Assert.That(provider.LoginCount,
                Is.EqualTo(1 + AuthService.ReloginMaximumConsecutive));

            provider.EmitLoginResult(
                MiniJson.Serialize(new Dictionary<string, object>
                {
                    ["is_new_user"] = false,
                    ["user_profile"] = new Dictionary<string, object>
                    {
                        ["user_id"] = "7"
                    }
                }));
            clock.Milliseconds +=
                AuthService.ReloginMinimumIntervalMilliseconds;
            provider.EmitLoginExpired();
            Assert.That(provider.LoginCount,
                Is.EqualTo(2 + AuthService.ReloginMaximumConsecutive));
            service.Dispose();
        }

        [Test]
        public void MissingProvider_DegradesToSourceErrorWithoutRuntimeLog()
        {
            var prerequisites = new FakePrerequisites(true, "luid");
            var service = new AuthService(
                NullAuthProvider.Instance,
                prerequisites,
                new MutableClock(1_000),
                production: false,
                showLog: false);
            int failureCode = 0;
            service.LoginFailed += (code, _) => failureCode = code;

            service.Start();

            Assert.That(service.IsAvailable, Is.False);
            Assert.That(service.IsInitialized, Is.False);
            Assert.That(service.HasLoginAttemptCompleted, Is.True);
            Assert.That(failureCode,
                Is.EqualTo(AuthErrorCode.PluginUnavailable));
            AuthTokenResult token = default;
            service.RequestAccessToken(false, value => token = value);
            Assert.That(token.Code,
                Is.EqualTo(AuthErrorCode.PluginUnavailable));
            service.Dispose();
        }

        private sealed class MutableClock : IAuthMonotonicClock
        {
            public MutableClock(long milliseconds)
            {
                Milliseconds = milliseconds;
            }

            public long Milliseconds { get; set; }
        }

        private sealed class FakePrerequisites : IAuthPrerequisiteProvider
        {
            public FakePrerequisites(
                bool analyticsInitialized = false,
                string localUserId = "")
            {
                IsAnalyticsInitialized = analyticsInitialized;
                LocalUserId = localUserId;
            }

            public bool IsAnalyticsInitialized { get; private set; }
            public string LocalUserId { get; private set; }
            public event Action AnalyticsInitialized;
            public event Action<string> LocalUserIdReady;

            public void EmitAnalyticsInitialized()
            {
                IsAnalyticsInitialized = true;
                AnalyticsInitialized?.Invoke();
            }

            public void EmitLocalUserId(string value)
            {
                LocalUserId = value;
                LocalUserIdReady?.Invoke(value);
            }
        }

        private sealed class FakeAuthProvider : IAuthProvider
        {
            public bool IsAvailable => true;
            public int InitializeCount { get; private set; }
            public int LoginCount { get; private set; }
            public int TokenRequestCount { get; private set; }
            public bool LastForceRefresh { get; private set; }
            public string LastConfiguration { get; private set; }
            public event Action<string> LoginResult;
            public event Action<string> LoginError;
            public event Action LoginExpired;
            public event Action<string> AccessTokenResult;

            public int Initialize(string configurationJson)
            {
                InitializeCount++;
                LastConfiguration = configurationJson;
                return 0;
            }

            public void LoginAsGuest() => LoginCount++;
            public void RequestAccessToken(bool forceRefresh)
            {
                TokenRequestCount++;
                LastForceRefresh = forceRefresh;
            }
            public string GetDeviceId() => "device";
            public bool IsLoggedIn() => true;
            public bool IsGuestLogin() => true;
            public string GetUserProfileJson() => "{}";
            public void EmitLoginResult(string raw) => LoginResult?.Invoke(raw);
            public void EmitLoginExpired() => LoginExpired?.Invoke();
            public void EmitToken(string raw) => AccessTokenResult?.Invoke(raw);
        }
    }
}
