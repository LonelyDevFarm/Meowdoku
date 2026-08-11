using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Meowdoku.Core.Online
{
    public interface IDataSyncPlatformMetadata
    {
        bool IsOnline { get; }
        string VersionName { get; }
        string Country { get; }
        string LearningsId { get; }
        string LocalUserId { get; }
    }

    internal sealed class NullDataSyncPlatformMetadata :
        IDataSyncPlatformMetadata
    {
        public static readonly NullDataSyncPlatformMetadata Instance = new();
        private NullDataSyncPlatformMetadata() { }
        public bool IsOnline => false;
        public string VersionName => Application.version;
        public string Country => string.Empty;
        public string LearningsId => string.Empty;
        public string LocalUserId => string.Empty;
    }

    [DisallowMultipleComponent]
    public sealed class DataSyncHttpApi : MonoBehaviour,
        IDataSyncApi,
        IDataSyncCancelableApi
    {
        public const int TimeoutSeconds = 10;
        public const int ClockSkewRetryThresholdSeconds = 3;

        private static long _serverTimeOffsetSeconds;

        [SerializeField] private MonoBehaviour platformMetadataAdapter;
        [SerializeField] private AuthRuntime authRuntime;

        private readonly HashSet<UnityWebRequest> _activeRequests = new();

        private IDataSyncPlatformMetadata Metadata =>
            platformMetadataAdapter as IDataSyncPlatformMetadata ??
            NullDataSyncPlatformMetadata.Instance;

        public bool IsAvailable => isActiveAndEnabled;

        public void BindPlatformMetadataAdapter(MonoBehaviour adapter)
        {
            platformMetadataAdapter = adapter;
        }

        public void BindAuthRuntime(AuthRuntime runtime)
        {
            authRuntime = runtime;
        }

        public IEnumerator FetchMeta(
            string bearer,
            Action<DataSyncApiResponse> completed)
        {
            yield return RequestJson(
                UnityWebRequest.kHttpVerbGET,
                ApiConfig.SyncGameDataMetaPath,
                string.Empty,
                bearer,
                completed);
        }

        public IEnumerator Download(
            string bearer,
            Action<DataSyncApiResponse> completed)
        {
            yield return RequestJson(
                UnityWebRequest.kHttpVerbGET,
                ApiConfig.SyncGameDataPath,
                string.Empty,
                bearer,
                completed);
        }

        public IEnumerator Upload(
            string bearer,
            string gameData,
            string schemaVersion,
            int syncCode,
            string extraInfo,
            Action<DataSyncApiResponse> completed)
        {
            var body = new Dictionary<string, object>
            {
                ["game_data"] = gameData ?? string.Empty,
                ["schema_version"] = schemaVersion ?? string.Empty,
                ["sync_code"] = syncCode
            };
            if (!string.IsNullOrEmpty(extraInfo))
                body["extra_info"] = extraInfo;
            yield return RequestJson(
                UnityWebRequest.kHttpVerbPOST,
                ApiConfig.SyncGameDataPath,
                MiniJson.Serialize(body),
                bearer,
                completed);
        }

        private IEnumerator RequestJson(
            string method,
            string path,
            string rawBody,
            string bearer,
            Action<DataSyncApiResponse> completed)
        {
            if (!Metadata.IsOnline)
            {
                completed?.Invoke(
                    DataSyncApiResponse.NetworkFailure("offline"));
                yield break;
            }

            for (int attempt = 0; attempt < 2; attempt++)
            {
                long usedOffset = _serverTimeOffsetSeconds;
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() +
                                 usedOffset;
                string signature = ComputeSignature(rawBody, timestamp);
                string url = $"{ApiConfig.BaseUrl(ApiConfig.IsProductionBuild)}" +
                             $"{path}?timestamp={timestamp}&sign={signature}";
                using UnityWebRequest request = CreateRequest(
                    method,
                    url,
                    rawBody,
                    bearer);
                _activeRequests.Add(request);
                yield return request.SendWebRequest();
                _activeRequests.Remove(request);

                CalibrateServerTime(request.GetResponseHeaders());
                if (request.result is UnityWebRequest.Result.ConnectionError or
                    UnityWebRequest.Result.DataProcessingError)
                {
                    completed?.Invoke(DataSyncApiResponse.NetworkFailure(
                        request.error ?? "transport"));
                    yield break;
                }

                DataSyncApiResponse response = ParseResponse(
                    request.downloadHandler?.text,
                    (int)request.responseCode);
                if (attempt == 0 &&
                    response.Code is ApiConfig.CodeSignInvalid or
                        ApiConfig.CodeTimestampExpired &&
                    Math.Abs(_serverTimeOffsetSeconds - usedOffset) >=
                    ClockSkewRetryThresholdSeconds)
                    continue;

                completed?.Invoke(response);
                yield break;
            }
        }

        private UnityWebRequest CreateRequest(
            string method,
            string url,
            string rawBody,
            string bearer)
        {
            var request = new UnityWebRequest(url, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = TimeoutSeconds
            };
            if (!string.Equals(
                    method,
                    UnityWebRequest.kHttpVerbGET,
                    StringComparison.Ordinal) &&
                !string.IsNullOrEmpty(rawBody))
            {
                request.uploadHandler = new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(rawBody));
            }

            IDataSyncPlatformMetadata metadata = Metadata;
            string version = string.IsNullOrEmpty(metadata.VersionName)
                ? Application.version
                : metadata.VersionName;
            string platform = ApiConfig.CurrentPlatform;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("app", ApiConfig.AppId);
            request.SetRequestHeader("app-version", version);
            request.SetRequestHeader(
                "country",
                metadata.Country ?? string.Empty);
            request.SetRequestHeader("platform", platform);
            request.SetRequestHeader(
                "user-agent",
                $"{platform} {ApiConfig.AppId}/{version}");
            request.SetRequestHeader(
                "learnings-id",
                metadata.LearningsId ?? string.Empty);
            request.SetRequestHeader(
                "device-id",
                authRuntime != null
                    ? authRuntime.Service.DeviceId
                    : string.Empty);
            request.SetRequestHeader(
                "luid",
                metadata.LocalUserId ?? string.Empty);
            if (!string.IsNullOrEmpty(bearer))
                request.SetRequestHeader("Authorization", bearer);
            return request;
        }

        public static string ComputeSignature(
            string rawBody,
            long timestamp)
        {
            string source = (rawBody ?? string.Empty) +
                            timestamp.ToString(CultureInfo.InvariantCulture) +
                            ApiConfig.SignSecret;
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(source));
            var builder = new StringBuilder(hash.Length * 2);
            for (int index = 0; index < hash.Length; index++)
                builder.Append(hash[index].ToString("x2"));
            return builder.ToString();
        }

        private static DataSyncApiResponse ParseResponse(
            string raw,
            int httpStatus)
        {
            IReadOnlyDictionary<string, object> envelope =
                AuthEmptyDictionary.Instance;
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    if (MiniJson.Deserialize(raw) is
                        IReadOnlyDictionary<string, object> parsed)
                        envelope = parsed;
                }
                catch (Exception)
                {
                    envelope = AuthEmptyDictionary.Instance;
                }
            }
            IReadOnlyDictionary<string, object> status =
                DataSyncValues.Dictionary(envelope, "status");
            return new DataSyncApiResponse(
                true,
                DataSyncValues.Int(status, "code", -1),
                DataSyncValues.Dictionary(envelope, "data"),
                httpStatus,
                DataSyncValues.String(status, "message"));
        }

        private static void CalibrateServerTime(
            IReadOnlyDictionary<string, string> headers)
        {
            if (headers == null) return;
            foreach (KeyValuePair<string, string> pair in headers)
            {
                if (!string.Equals(
                        pair.Key,
                        "date",
                        StringComparison.OrdinalIgnoreCase))
                    continue;
                if (DateTimeOffset.TryParseExact(
                        pair.Value,
                        "r",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal |
                        DateTimeStyles.AdjustToUniversal,
                        out DateTimeOffset serverTime))
                {
                    _serverTimeOffsetSeconds =
                        serverTime.ToUnixTimeSeconds() -
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
                return;
            }
        }

        public void CancelAll()
        {
            UnityWebRequest[] requests = new UnityWebRequest[
                _activeRequests.Count];
            _activeRequests.CopyTo(requests);
            _activeRequests.Clear();
            for (int index = 0; index < requests.Length; index++)
                requests[index]?.Abort();
        }

        private void OnDisable() => CancelAll();
        private void OnDestroy() => CancelAll();

#if UNITY_INCLUDE_TESTS
        internal static void ResetClockOffsetForTests()
        {
            _serverTimeOffsetSeconds = 0;
        }
#endif
    }
}
