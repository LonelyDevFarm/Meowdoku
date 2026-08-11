using System;
using System.Collections;
using System.Collections.Generic;

namespace Meowdoku.Core.Online
{
    public readonly struct DataSyncMergeContext
    {
        public DataSyncMergeContext(bool remoteAhead)
        {
            RemoteAhead = remoteAhead;
        }

        public bool RemoteAhead { get; }
    }

    public interface IDataSyncSavable
    {
        string RemoteSaveId { get; }
        Dictionary<string, object> ExportRemote();
        bool MergeRemote(
            IReadOnlyDictionary<string, object> remote,
            DataSyncMergeContext context);
    }

    public interface IDataSyncMergeBasis
    {
        bool IsRemoteAhead(
            IReadOnlyDictionary<string, object> remote);
    }

    public sealed class DataSyncRegistry
    {
        private readonly List<IDataSyncSavable> _savables = new();
        private bool _syncedOnce;
        private Action<IDataSyncSavable> _lateHandler;

        public IReadOnlyList<IDataSyncSavable> All => _savables;
        public bool HasSyncedOnce => _syncedOnce;

        public bool Register(IDataSyncSavable savable)
        {
            if (savable == null)
                throw new ArgumentNullException(nameof(savable));
            if (_savables.Contains(savable)) return false;
            _savables.Add(savable);
            if (_syncedOnce) _lateHandler?.Invoke(savable);
            return true;
        }

        public void SetLateHandler(Action<IDataSyncSavable> handler)
        {
            _lateHandler = handler;
        }

        public void MarkSynced() => _syncedOnce = true;

        public void Clear()
        {
            _savables.Clear();
            _syncedOnce = false;
            _lateHandler = null;
        }
    }

    public sealed class DataSyncApiResponse
    {
        public DataSyncApiResponse(
            bool ok,
            int code,
            IReadOnlyDictionary<string, object> data = null,
            int httpStatus = 0,
            string message = "")
        {
            Ok = ok;
            Code = code;
            Data = data ?? AuthEmptyDictionary.Instance;
            HttpStatus = httpStatus;
            Message = message ?? string.Empty;
        }

        public bool Ok { get; }
        public int Code { get; }
        public IReadOnlyDictionary<string, object> Data { get; }
        public int HttpStatus { get; }
        public string Message { get; }

        public static DataSyncApiResponse NetworkFailure(string reason) =>
            new(false, -1, message: reason);
    }

    public interface IDataSyncApi
    {
        bool IsAvailable { get; }
        IEnumerator FetchMeta(
            string bearer,
            Action<DataSyncApiResponse> completed);
        IEnumerator Download(
            string bearer,
            Action<DataSyncApiResponse> completed);
        IEnumerator Upload(
            string bearer,
            string gameData,
            string schemaVersion,
            int syncCode,
            string extraInfo,
            Action<DataSyncApiResponse> completed);
    }

    public interface IDataSyncCancelableApi
    {
        void CancelAll();
    }

    public sealed class NullDataSyncApi : IDataSyncApi
    {
        public static readonly NullDataSyncApi Instance = new();
        private NullDataSyncApi() { }
        public bool IsAvailable => false;

        public IEnumerator FetchMeta(
            string bearer,
            Action<DataSyncApiResponse> completed)
        {
            completed?.Invoke(
                DataSyncApiResponse.NetworkFailure("api unavailable"));
            yield break;
        }

        public IEnumerator Download(
            string bearer,
            Action<DataSyncApiResponse> completed)
        {
            completed?.Invoke(
                DataSyncApiResponse.NetworkFailure("api unavailable"));
            yield break;
        }

        public IEnumerator Upload(
            string bearer,
            string gameData,
            string schemaVersion,
            int syncCode,
            string extraInfo,
            Action<DataSyncApiResponse> completed)
        {
            completed?.Invoke(
                DataSyncApiResponse.NetworkFailure("api unavailable"));
            yield break;
        }
    }

    public interface IDataSyncAuthGateway
    {
        bool IsAvailable { get; }
        bool IsLoggedIn { get; }
        IEnumerator RequestAccessToken(
            bool forceRefresh,
            Action<AuthTokenResult> completed);
    }

    public sealed class AuthDataSyncGateway : IDataSyncAuthGateway
    {
        private readonly AuthService _auth;

        public AuthDataSyncGateway(AuthService auth)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        }

        public bool IsAvailable => _auth.IsAvailable;
        public bool IsLoggedIn => _auth.IsLoggedIn;

        public IEnumerator RequestAccessToken(
            bool forceRefresh,
            Action<AuthTokenResult> completed)
        {
            bool done = false;
            AuthTokenResult result = default;
            if (!_auth.RequestAccessToken(forceRefresh, value =>
                {
                    result = value;
                    done = true;
                }))
            {
                completed?.Invoke(new AuthTokenResult(
                    string.Empty,
                    AuthErrorCode.PluginUnavailable,
                    "AuthPlugin unavailable"));
                yield break;
            }

            while (!done) yield return null;
            completed?.Invoke(result);
        }
    }

    public interface IDataSyncSnapshotStore
    {
        Dictionary<string, object> LoadRemoteRoot();
        bool SaveRemoteRoot(IReadOnlyDictionary<string, object> root);
    }

    public interface IDataSyncEnableStore
    {
        bool TryLoad(out bool enabled);
        bool Save(bool enabled);
    }

    public sealed class MemoryDataSyncSnapshotStore :
        IDataSyncSnapshotStore
    {
        private Dictionary<string, object> _root = new();

        public Dictionary<string, object> LoadRemoteRoot() =>
            DataSyncValues.DeepClone(_root);

        public bool SaveRemoteRoot(
            IReadOnlyDictionary<string, object> root)
        {
            _root = DataSyncValues.DeepClone(root);
            return true;
        }
    }

    public readonly struct DataSyncOutcome
    {
        public DataSyncOutcome(
            bool succeeded,
            string reason,
            bool changed,
            int syncCode,
            bool isFirstUpload)
        {
            Succeeded = succeeded;
            Reason = reason ?? string.Empty;
            Changed = changed;
            SyncCode = syncCode;
            IsFirstUpload = isFirstUpload;
        }

        public bool Succeeded { get; }
        public string Reason { get; }
        public bool Changed { get; }
        public int SyncCode { get; }
        public bool IsFirstUpload { get; }

        public static DataSyncOutcome Fail(string reason) =>
            new(false, reason, false, 0, false);
    }

    internal static class DataSyncValues
    {
        public static Dictionary<string, object> DeepClone(
            IReadOnlyDictionary<string, object> source)
        {
            var clone = new Dictionary<string, object>();
            if (source == null) return clone;
            foreach (KeyValuePair<string, object> pair in source)
                clone[pair.Key] = DeepCloneValue(pair.Value);
            return clone;
        }

        public static IReadOnlyDictionary<string, object> Dictionary(
            IReadOnlyDictionary<string, object> source,
            string key)
        {
            return source != null && source.TryGetValue(key, out object raw) &&
                   raw is IReadOnlyDictionary<string, object> dictionary
                ? dictionary
                : AuthEmptyDictionary.Instance;
        }

        public static int Int(
            IReadOnlyDictionary<string, object> source,
            string key,
            int fallback = 0)
        {
            if (source == null || !source.TryGetValue(key, out object raw) ||
                raw == null)
                return fallback;
            try { return Convert.ToInt32(raw); }
            catch (Exception) { return fallback; }
        }

        public static string String(
            IReadOnlyDictionary<string, object> source,
            string key)
        {
            return source != null && source.TryGetValue(key, out object raw) &&
                   raw != null
                ? Convert.ToString(raw) ?? string.Empty
                : string.Empty;
        }

        public static object CloneValue(object value) =>
            DeepCloneValue(value);

        private static object DeepCloneValue(object value)
        {
            if (value is IReadOnlyDictionary<string, object> dictionary)
                return DeepClone(dictionary);
            if (value is IList list)
            {
                var clone = new List<object>(list.Count);
                for (int index = 0; index < list.Count; index++)
                    clone.Add(DeepCloneValue(list[index]));
                return clone;
            }
            return value;
        }
    }
}
