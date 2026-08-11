using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core.Online
{
    internal sealed class AuthEmptyDictionary :
        Dictionary<string, object>
    {
        public static readonly AuthEmptyDictionary Instance = new();
        private AuthEmptyDictionary() { }
    }

    public static class AuthErrorCode
    {
        public const int NotSupported = -1;
        public const int NotInitialized = 1000;
        public const int Network = 1001;
        public const int Parameter = 1002;
        public const int Provider = 2000;
        public const int TokenInvalid = 2001;
        public const int UserCanceled = 2002;
        public const int InProgress = 2003;
        public const int Internal = 2004;
        public const int Firebase = 3000;
        public const int UserNotLoggedIn = 3001;
        public const int UserTokenExpired = 3002;
        public const int InvalidUser = 3003;
        public const int Server = 4000;
        public const int ServerEmptyData = 4001;
        public const int ServerParseFailed = 4002;
        public const int ServerAccessTokenInvalid = 4003;
        public const int ServerAccessTokenExpired = 4004;
        public const int ServerRefreshTokenInvalid = 4005;
        public const int ServerRefreshTokenExpired = 4006;
        public const int ServerAccountNotFound = 4007;
        public const int ServerAccountDeleted = 4008;
        public const int ServerSignInvalid = 4009;
        public const int ServerTimestampExpired = 4010;
        public const int FirebaseTokenInvalid = 4011;
        public const int PluginUnavailable = -100;
        public const int TokenTimeout = -101;
    }

    public sealed class AuthLoginResult
    {
        public AuthLoginResult(
            bool isNewUser,
            IReadOnlyDictionary<string, object> userProfile)
        {
            IsNewUser = isNewUser;
            UserProfile = userProfile ?? AuthEmptyDictionary.Instance;
        }

        public bool IsNewUser { get; }
        public IReadOnlyDictionary<string, object> UserProfile { get; }
    }

    public readonly struct AuthTokenResult
    {
        public AuthTokenResult(string token, int code, string message)
        {
            Token = token ?? string.Empty;
            Code = code;
            Message = message ?? string.Empty;
        }

        public string Token { get; }
        public int Code { get; }
        public string Message { get; }
        public bool Succeeded => Code == 0 && !string.IsNullOrEmpty(Token);
    }

    public interface IAuthProvider
    {
        bool IsAvailable { get; }
        event Action<string> LoginResult;
        event Action<string> LoginError;
        event Action LoginExpired;
        event Action<string> AccessTokenResult;
        int Initialize(string configurationJson);
        void LoginAsGuest();
        void RequestAccessToken(bool forceRefresh);
        string GetDeviceId();
        bool IsLoggedIn();
        bool IsGuestLogin();
        string GetUserProfileJson();
    }

    public interface IAuthPrerequisiteProvider
    {
        bool IsAnalyticsInitialized { get; }
        string LocalUserId { get; }
        event Action AnalyticsInitialized;
        event Action<string> LocalUserIdReady;
    }

    public interface IAuthMonotonicClock
    {
        long Milliseconds { get; }
    }

    public sealed class NullAuthProvider : IAuthProvider
    {
        public static readonly NullAuthProvider Instance = new();
        private NullAuthProvider() { }
        public bool IsAvailable => false;
        public event Action<string> LoginResult { add { } remove { } }
        public event Action<string> LoginError { add { } remove { } }
        public event Action LoginExpired { add { } remove { } }
        public event Action<string> AccessTokenResult { add { } remove { } }
        public int Initialize(string configurationJson) =>
            AuthErrorCode.PluginUnavailable;
        public void LoginAsGuest() { }
        public void RequestAccessToken(bool forceRefresh) { }
        public string GetDeviceId() => string.Empty;
        public bool IsLoggedIn() => false;
        public bool IsGuestLogin() => false;
        public string GetUserProfileJson() => string.Empty;
    }

    public sealed class NullAuthPrerequisiteProvider :
        IAuthPrerequisiteProvider
    {
        public static readonly NullAuthPrerequisiteProvider Instance = new();
        private NullAuthPrerequisiteProvider() { }
        public bool IsAnalyticsInitialized => false;
        public string LocalUserId => string.Empty;
        public event Action AnalyticsInitialized { add { } remove { } }
        public event Action<string> LocalUserIdReady { add { } remove { } }
    }

    internal sealed class UnityAuthMonotonicClock : IAuthMonotonicClock
    {
        public static readonly UnityAuthMonotonicClock Instance = new();
        private UnityAuthMonotonicClock() { }
        public long Milliseconds => (long)(
            Time.realtimeSinceStartupAsDouble * 1000.0);
    }

    /// <summary>
    /// Provider-neutral port of auth_manager.gd. Native JSON remains at the
    /// provider boundary and all timeout/relogin policy stays deterministic.
    /// </summary>
    public sealed class AuthService : IDisposable
    {
        public const int AccessTokenTimeoutMilliseconds = 12_000;
        public const int ReloginMinimumIntervalMilliseconds = 60_000;
        public const int ReloginMaximumConsecutive = 5;

        private sealed class PendingTokenRequest
        {
            public long Deadline;
            public Action<AuthTokenResult> Completed;
        }

        private readonly IAuthProvider _provider;
        private readonly IAuthPrerequisiteProvider _prerequisites;
        private readonly IAuthMonotonicClock _clock;
        private readonly bool _production;
        private readonly bool _showLog;
        private readonly List<PendingTokenRequest> _pendingTokens = new();
        private bool _analyticsReady;
        private string _localUserId;
        private bool _authStarted;
        private bool _initialized;
        private bool _disposed;
        private int _reloginCount;
        private long _lastReloginMilliseconds;

        public AuthService(
            IAuthProvider provider = null,
            IAuthPrerequisiteProvider prerequisites = null,
            IAuthMonotonicClock clock = null,
            bool? production = null,
            bool? showLog = null)
        {
            _provider = provider ?? NullAuthProvider.Instance;
            _prerequisites = prerequisites ??
                             NullAuthPrerequisiteProvider.Instance;
            _clock = clock ?? UnityAuthMonotonicClock.Instance;
            _production = production ?? ApiConfig.IsProductionBuild;
            _showLog = showLog ?? Debug.isDebugBuild;
            _analyticsReady = _prerequisites.IsAnalyticsInitialized;
            _localUserId = _prerequisites.LocalUserId ?? string.Empty;

            _provider.LoginResult += HandleLoginResult;
            _provider.LoginError += HandleLoginError;
            _provider.LoginExpired += HandleLoginExpired;
            _provider.AccessTokenResult += HandleAccessTokenResult;
            _prerequisites.AnalyticsInitialized +=
                HandleAnalyticsInitialized;
            _prerequisites.LocalUserIdReady += HandleLocalUserIdReady;
        }

        public event Action<AuthLoginResult> LoginSucceeded;
        public event Action<int, string> LoginFailed;
        public event Action LoginExpired;
        public event Action<AuthTokenResult> AccessTokenReady;

        public bool IsAvailable => !_disposed && _provider.IsAvailable;
        public bool IsInitialized => !_disposed && _initialized;
        public bool IsLoggedIn => IsAvailable && _provider.IsLoggedIn();
        public bool IsGuestLogin => IsAvailable && _provider.IsGuestLogin();
        public bool HasLoginAttemptCompleted { get; private set; }
        public string DeviceId => IsAvailable
            ? _provider.GetDeviceId() ?? string.Empty
            : string.Empty;
        public int PendingTokenRequestCount => _pendingTokens.Count;

        public void Start()
        {
            if (!_disposed) MaybeStartAuth();
        }

        public void Tick()
        {
            if (_disposed || _pendingTokens.Count == 0) return;
            long now = _clock.Milliseconds;
            for (int index = _pendingTokens.Count - 1; index >= 0; index--)
            {
                PendingTokenRequest request = _pendingTokens[index];
                if (now < request.Deadline) continue;
                _pendingTokens.RemoveAt(index);
                request.Completed?.Invoke(new AuthTokenResult(
                    string.Empty,
                    AuthErrorCode.TokenTimeout,
                    "token timeout"));
            }
        }

        public bool RequestAccessToken(
            bool forceRefresh,
            Action<AuthTokenResult> completed)
        {
            if (_disposed || completed == null) return false;
            if (!IsAvailable)
            {
                AuthTokenResult result = new(
                    string.Empty,
                    AuthErrorCode.PluginUnavailable,
                    "AuthPlugin unavailable");
                AccessTokenReady?.Invoke(result);
                completed(result);
                return true;
            }

            _pendingTokens.Add(new PendingTokenRequest
            {
                Deadline = _clock.Milliseconds +
                           AccessTokenTimeoutMilliseconds,
                Completed = completed
            });
            _provider.RequestAccessToken(forceRefresh);
            return true;
        }

        public IReadOnlyDictionary<string, object> GetUserProfile()
        {
            return IsAvailable
                ? ParseDictionary(_provider.GetUserProfileJson())
                : AuthEmptyDictionary.Instance;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _provider.LoginResult -= HandleLoginResult;
            _provider.LoginError -= HandleLoginError;
            _provider.LoginExpired -= HandleLoginExpired;
            _provider.AccessTokenResult -= HandleAccessTokenResult;
            _prerequisites.AnalyticsInitialized -=
                HandleAnalyticsInitialized;
            _prerequisites.LocalUserIdReady -= HandleLocalUserIdReady;
            _pendingTokens.Clear();
            LoginSucceeded = null;
            LoginFailed = null;
            LoginExpired = null;
            AccessTokenReady = null;
        }

        private void HandleAnalyticsInitialized()
        {
            _analyticsReady = true;
            MaybeStartAuth();
        }

        private void HandleLocalUserIdReady(string localUserId)
        {
            _localUserId = localUserId ?? string.Empty;
            MaybeStartAuth();
        }

        private void MaybeStartAuth()
        {
            if (_disposed || _authStarted || !_analyticsReady ||
                string.IsNullOrEmpty(_localUserId))
                return;
            _authStarted = true;
            IReadOnlyDictionary<string, object> configuration =
                ApiConfig.BuildAuthInitialization(
                    _localUserId,
                    _production,
                    _showLog);
            int code = Initialize(MiniJson.Serialize(configuration));
            if (code != 0 && code != AuthErrorCode.PluginUnavailable)
                return;
            LoginAsGuest();
        }

        private int Initialize(string configurationJson)
        {
            if (_initialized) return 0;
            if (!IsAvailable) return AuthErrorCode.PluginUnavailable;
            int code = _provider.Initialize(configurationJson);
            _initialized = code == 0;
            return code;
        }

        private void LoginAsGuest()
        {
            HasLoginAttemptCompleted = false;
            if (!IsAvailable)
            {
                HasLoginAttemptCompleted = true;
                LoginFailed?.Invoke(
                    AuthErrorCode.PluginUnavailable,
                    "AuthPlugin unavailable");
                return;
            }
            _provider.LoginAsGuest();
        }

        private void HandleLoginResult(string raw)
        {
            HasLoginAttemptCompleted = true;
            IReadOnlyDictionary<string, object> value = ParseDictionary(raw);
            bool isNew = ReadBool(value, "is_new_user");
            IReadOnlyDictionary<string, object> profile =
                value.TryGetValue("user_profile", out object rawProfile) &&
                rawProfile is IReadOnlyDictionary<string, object> dictionary
                    ? dictionary
                    : AuthEmptyDictionary.Instance;
            _reloginCount = 0;
            LoginSucceeded?.Invoke(new AuthLoginResult(isNew, profile));
        }

        private void HandleLoginError(string raw)
        {
            HasLoginAttemptCompleted = true;
            IReadOnlyDictionary<string, object> value = ParseDictionary(raw);
            LoginFailed?.Invoke(
                ReadInt(value, "code", AuthErrorCode.TokenInvalid),
                ReadString(value, "msg"));
        }

        private void HandleLoginExpired()
        {
            LoginExpired?.Invoke();
            long now = _clock.Milliseconds;
            if (_lastReloginMilliseconds > 0 &&
                now - _lastReloginMilliseconds <
                ReloginMinimumIntervalMilliseconds)
                return;
            if (_reloginCount >= ReloginMaximumConsecutive) return;
            _reloginCount++;
            _lastReloginMilliseconds = now;
            LoginAsGuest();
        }

        private void HandleAccessTokenResult(string raw)
        {
            IReadOnlyDictionary<string, object> value = ParseDictionary(raw);
            var result = new AuthTokenResult(
                ReadString(value, "token"),
                ReadInt(value, "code"),
                ReadString(value, "msg"));
            AccessTokenReady?.Invoke(result);
            if (_pendingTokens.Count == 0) return;
            PendingTokenRequest[] pending = _pendingTokens.ToArray();
            _pendingTokens.Clear();
            for (int index = 0; index < pending.Length; index++)
                pending[index].Completed?.Invoke(result);
        }

        private static IReadOnlyDictionary<string, object> ParseDictionary(
            string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return AuthEmptyDictionary.Instance;
            try
            {
                return MiniJson.Deserialize(raw) is
                    IReadOnlyDictionary<string, object> dictionary
                    ? dictionary
                    : AuthEmptyDictionary.Instance;
            }
            catch (Exception)
            {
                return AuthEmptyDictionary.Instance;
            }
        }

        private static string ReadString(
            IReadOnlyDictionary<string, object> values,
            string key)
        {
            return values != null && values.TryGetValue(key, out object raw) &&
                   raw != null
                ? Convert.ToString(raw) ?? string.Empty
                : string.Empty;
        }

        private static int ReadInt(
            IReadOnlyDictionary<string, object> values,
            string key,
            int fallback = 0)
        {
            if (values == null || !values.TryGetValue(key, out object raw) ||
                raw == null)
                return fallback;
            try { return Convert.ToInt32(raw); }
            catch (Exception) { return fallback; }
        }

        private static bool ReadBool(
            IReadOnlyDictionary<string, object> values,
            string key)
        {
            if (values == null || !values.TryGetValue(key, out object raw) ||
                raw == null)
                return false;
            try { return Convert.ToBoolean(raw); }
            catch (Exception) { return false; }
        }

    }
}
