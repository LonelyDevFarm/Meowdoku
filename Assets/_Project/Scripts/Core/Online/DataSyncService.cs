using System;
using System.Collections;
using System.Collections.Generic;

namespace Meowdoku.Core.Online
{
    /// <summary>
    /// Provider-neutral port of data_sync_manager.gd. The runtime owns
    /// coroutine scheduling; this class owns ordering, merge and conflict
    /// policy so it remains deterministic under fake Auth/API providers.
    /// </summary>
    public sealed class DataSyncService
    {
        public const string SchemaVersion = "1.0.0";
        public const int MaximumConflictRetries = 3;

        private sealed class SyncContext
        {
            public string Bearer = string.Empty;
        }

        private readonly struct DownloadResult
        {
            public DownloadResult(
                bool ok,
                string reason,
                bool firstUpload,
                int syncCode)
            {
                Ok = ok;
                Reason = reason ?? string.Empty;
                FirstUpload = firstUpload;
                SyncCode = syncCode;
            }

            public bool Ok { get; }
            public string Reason { get; }
            public bool FirstUpload { get; }
            public int SyncCode { get; }
        }

        private readonly IDataSyncAuthGateway _auth;
        private readonly IDataSyncApi _api;
        private readonly IDataSyncSnapshotStore _snapshotStore;
        private Dictionary<string, object> _lastRemoteRoot;
        private int _remoteSyncCode = -1;

        public DataSyncService(
            IDataSyncAuthGateway auth,
            IDataSyncApi api,
            DataSyncRegistry registry,
            IDataSyncSnapshotStore snapshotStore = null)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _api = api ?? throw new ArgumentNullException(nameof(api));
            Registry = registry ?? throw new ArgumentNullException(
                nameof(registry));
            _snapshotStore = snapshotStore ??
                             new MemoryDataSyncSnapshotStore();
            _lastRemoteRoot = _snapshotStore.LoadRemoteRoot() ?? new();
        }

        public DataSyncRegistry Registry { get; }
        public int RemoteSyncCode => _remoteSyncCode;
        public IReadOnlyDictionary<string, object> LastRemoteRoot =>
            _lastRemoteRoot;

        public IEnumerator Synchronize(
            string reason,
            Action<DataSyncOutcome> completed)
        {
            if (!_auth.IsLoggedIn)
            {
                completed?.Invoke(DataSyncOutcome.Fail("not_logged_in"));
                yield break;
            }

            if (!_api.IsAvailable)
            {
                completed?.Invoke(DataSyncOutcome.Fail("api_unavailable"));
                yield break;
            }

            var context = new SyncContext();
            yield return FetchBearer(context, false);
            if (string.IsNullOrEmpty(context.Bearer))
            {
                completed?.Invoke(DataSyncOutcome.Fail("no_access_token"));
                yield break;
            }

            int remoteCode = 0;
            bool firstUpload = false;
            bool changed = false;

            if (_remoteSyncCode >= 0)
            {
                DataSyncApiResponse meta = null;
                yield return _api.FetchMeta(
                    context.Bearer,
                    value => meta = value);
                if (IsTokenInvalid(meta))
                {
                    yield return FetchBearer(context, true);
                    if (string.IsNullOrEmpty(context.Bearer))
                    {
                        completed?.Invoke(DataSyncOutcome.Fail(
                            "token_refresh"));
                        yield break;
                    }
                    meta = null;
                    yield return _api.FetchMeta(
                        context.Bearer,
                        value => meta = value);
                }

                if (meta == null || !meta.Ok)
                {
                    completed?.Invoke(DataSyncOutcome.Fail("meta_net"));
                    yield break;
                }

                if (meta.Code == ApiConfig.CodeNoSave)
                {
                    firstUpload = true;
                }
                else if (meta.Code == ApiConfig.CodeOk)
                {
                    int metaCode = DataSyncValues.Int(
                        meta.Data,
                        "sync_code");
                    if (metaCode == _remoteSyncCode)
                    {
                        remoteCode = _remoteSyncCode;
                    }
                    else
                    {
                        DownloadResult download = default;
                        yield return DownloadAndMerge(
                            context,
                            value => download = value);
                        if (!download.Ok)
                        {
                            completed?.Invoke(DataSyncOutcome.Fail(
                                string.IsNullOrEmpty(download.Reason)
                                    ? "download_net"
                                    : download.Reason));
                            yield break;
                        }
                        firstUpload = download.FirstUpload;
                        if (!firstUpload)
                        {
                            remoteCode = download.SyncCode;
                            changed = true;
                        }
                    }
                }
                else
                {
                    completed?.Invoke(DataSyncOutcome.Fail(
                        $"meta_code_{meta.Code}"));
                    yield break;
                }
            }
            else
            {
                DownloadResult download = default;
                yield return DownloadAndMerge(
                    context,
                    value => download = value);
                if (!download.Ok)
                {
                    completed?.Invoke(DataSyncOutcome.Fail(
                        string.IsNullOrEmpty(download.Reason)
                            ? "download_net"
                            : download.Reason));
                    yield break;
                }
                firstUpload = download.FirstUpload;
                if (!firstUpload)
                {
                    remoteCode = download.SyncCode;
                    changed = true;
                }
            }

            Dictionary<string, object> localRoot = BuildRoot();
            if (!firstUpload && string.Equals(
                    MiniJson.Serialize(localRoot),
                    MiniJson.Serialize(_lastRemoteRoot),
                    StringComparison.Ordinal))
            {
                _remoteSyncCode = remoteCode;
                SaveRemoteSnapshot();
                completed?.Invoke(new DataSyncOutcome(
                    true,
                    string.Empty,
                    changed,
                    remoteCode,
                    false));
                yield break;
            }

            int finalCode = -1;
            yield return Upload(
                context,
                remoteCode + 1,
                value => finalCode = value);
            if (finalCode < 0)
            {
                completed?.Invoke(DataSyncOutcome.Fail("upload"));
                yield break;
            }

            _remoteSyncCode = finalCode;
            SaveRemoteSnapshot();
            completed?.Invoke(new DataSyncOutcome(
                true,
                string.Empty,
                changed,
                finalCode,
                firstUpload));
        }

        public Dictionary<string, object> BuildRoot()
        {
            Dictionary<string, object> root =
                DataSyncValues.DeepClone(_lastRemoteRoot);
            IReadOnlyList<IDataSyncSavable> savables = Registry.All;
            for (int index = 0; index < savables.Count; index++)
            {
                IDataSyncSavable savable = savables[index];
                Dictionary<string, object> block = root.TryGetValue(
                        savable.RemoteSaveId,
                        out object raw) &&
                    raw is IReadOnlyDictionary<string, object> existing
                        ? DataSyncValues.DeepClone(existing)
                        : new Dictionary<string, object>();
                Dictionary<string, object> exported =
                    savable.ExportRemote() ?? new();
                foreach (KeyValuePair<string, object> pair in exported)
                    block[pair.Key] = DataSyncValues.CloneValue(pair.Value);
                root[savable.RemoteSaveId] = block;
            }
            return root;
        }

        public bool ApplyRemote(string gameData)
        {
            Dictionary<string, object> root = new();
            if (!string.IsNullOrEmpty(gameData))
            {
                object parsed;
                try { parsed = MiniJson.Deserialize(gameData); }
                catch (Exception) { return false; }
                if (parsed is not IReadOnlyDictionary<string, object> value)
                    return false;
                root = DataSyncValues.DeepClone(value);
            }

            _lastRemoteRoot = DataSyncValues.DeepClone(root);
            DataSyncMergeContext context = BuildMergeContext(root);
            IReadOnlyList<IDataSyncSavable> savables = Registry.All;
            for (int index = 0; index < savables.Count; index++)
            {
                IDataSyncSavable savable = savables[index];
                IReadOnlyDictionary<string, object> blob =
                    DataSyncValues.Dictionary(root, savable.RemoteSaveId);
                savable.MergeRemote(blob, context);
            }
            return true;
        }

        public DataSyncMergeContext BuildMergeContext(
            IReadOnlyDictionary<string, object> root)
        {
            IReadOnlyList<IDataSyncSavable> savables = Registry.All;
            for (int index = 0; index < savables.Count; index++)
            {
                IDataSyncSavable savable = savables[index];
                if (savable is not IDataSyncMergeBasis basis) continue;
                return new DataSyncMergeContext(basis.IsRemoteAhead(
                    DataSyncValues.Dictionary(root, savable.RemoteSaveId)));
            }
            return new DataSyncMergeContext(false);
        }

        private IEnumerator DownloadAndMerge(
            SyncContext context,
            Action<DownloadResult> completed)
        {
            DataSyncApiResponse response = null;
            yield return _api.Download(
                context.Bearer,
                value => response = value);
            if (IsTokenInvalid(response))
            {
                yield return FetchBearer(context, true);
                if (string.IsNullOrEmpty(context.Bearer))
                {
                    completed?.Invoke(new DownloadResult(
                        false,
                        "token_refresh",
                        false,
                        0));
                    yield break;
                }
                response = null;
                yield return _api.Download(
                    context.Bearer,
                    value => response = value);
            }

            if (response == null || !response.Ok)
            {
                completed?.Invoke(new DownloadResult(
                    false,
                    "download_net",
                    false,
                    0));
                yield break;
            }
            if (response.Code == ApiConfig.CodeNoSave)
            {
                completed?.Invoke(new DownloadResult(
                    true,
                    string.Empty,
                    true,
                    0));
                yield break;
            }
            if (response.Code == ApiConfig.CodeOk)
            {
                if (!ApplyRemote(DataSyncValues.String(
                        response.Data,
                        "game_data")))
                {
                    completed?.Invoke(new DownloadResult(
                        false,
                        "download_parse",
                        false,
                        0));
                    yield break;
                }
                int code = DataSyncValues.Int(response.Data, "sync_code");
                _remoteSyncCode = code;
                completed?.Invoke(new DownloadResult(
                    true,
                    string.Empty,
                    false,
                    code));
                yield break;
            }
            completed?.Invoke(new DownloadResult(
                false,
                $"download_code_{response.Code}",
                false,
                0));
        }

        private IEnumerator Upload(
            SyncContext context,
            int syncCode,
            Action<int> completed)
        {
            int code = syncCode;
            for (int attempt = 0;
                 attempt < MaximumConflictRetries;
                 attempt++)
            {
                Dictionary<string, object> root = BuildRoot();
                DataSyncApiResponse response = null;
                yield return _api.Upload(
                    context.Bearer,
                    MiniJson.Serialize(root),
                    SchemaVersion,
                    code,
                    string.Empty,
                    value => response = value);
                if (response == null || !response.Ok)
                {
                    completed?.Invoke(-1);
                    yield break;
                }
                if (response.Code == ApiConfig.CodeOk)
                {
                    _lastRemoteRoot = root;
                    completed?.Invoke(code);
                    yield break;
                }
                if (response.Code == ApiConfig.CodeSyncCodeTooLow)
                {
                    DataSyncApiResponse download = null;
                    yield return _api.Download(
                        context.Bearer,
                        value => download = value);
                    if (download == null || !download.Ok ||
                        download.Code != ApiConfig.CodeOk ||
                        !ApplyRemote(DataSyncValues.String(
                            download.Data,
                            "game_data")))
                    {
                        completed?.Invoke(-1);
                        yield break;
                    }
                    int remote = DataSyncValues.Int(
                        download.Data,
                        "sync_code",
                        code);
                    _remoteSyncCode = remote;
                    code = remote + 1;
                    continue;
                }
                if (IsTokenInvalid(response))
                {
                    yield return FetchBearer(context, true);
                    if (string.IsNullOrEmpty(context.Bearer))
                    {
                        completed?.Invoke(-1);
                        yield break;
                    }
                    continue;
                }
                completed?.Invoke(-1);
                yield break;
            }
            completed?.Invoke(-1);
        }

        private IEnumerator FetchBearer(
            SyncContext context,
            bool forceRefresh)
        {
            AuthTokenResult result = default;
            yield return _auth.RequestAccessToken(
                forceRefresh,
                value => result = value);
            context.Bearer = result.Code == 0
                ? result.Token ?? string.Empty
                : string.Empty;
        }

        private void SaveRemoteSnapshot()
        {
            _snapshotStore.SaveRemoteRoot(_lastRemoteRoot);
        }

        private static bool IsTokenInvalid(DataSyncApiResponse response)
        {
            return response != null && response.Code is
                ApiConfig.CodeAccessTokenInvalid or
                ApiConfig.CodeAccessTokenExpired;
        }
    }
}
