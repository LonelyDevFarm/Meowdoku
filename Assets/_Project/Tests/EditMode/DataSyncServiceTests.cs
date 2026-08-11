using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Meowdoku.Core;
using Meowdoku.Core.Online;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class DataSyncServiceTests
    {
        [Test]
        public void HttpContract_PreservesSourceSchemaTimeoutAndSignature()
        {
            Assert.That(DataSyncService.SchemaVersion, Is.EqualTo("1.0.0"));
            Assert.That(DataSyncService.MaximumConflictRetries, Is.EqualTo(3));
            Assert.That(DataSyncHttpApi.TimeoutSeconds, Is.EqualTo(10));
            Assert.That(
                DataSyncHttpApi.ComputeSignature(
                    string.Concat("{", '"', "a", '"', ":1}"),
                    1700000000),
                Is.EqualTo("9844cd1cab163eb025779c4fb823d14b"));
        }

        [Test]
        public void Registry_IsIdempotent_AndNotifiesLateSavableAfterSync()
        {
            var registry = new DataSyncRegistry();
            var savable = new FakeSavable("core", new()
            {
                ["current_level"] = 10
            });
            int lateCount = 0;
            registry.SetLateHandler(_ => lateCount++);
            Assert.That(registry.Register(savable), Is.True);
            Assert.That(registry.Register(savable), Is.False);
            Assert.That(registry.All.Count, Is.EqualTo(1));
            registry.MarkSynced();
            Assert.That(registry.Register(new FakeSavable("streak")), Is.True);
            Assert.That(lateCount, Is.EqualTo(1));
        }

        [Test]
        public void BuildRoot_PreservesUnknownRootAndBlockFields()
        {
            var snapshot = new MemoryDataSyncSnapshotStore();
            snapshot.SaveRemoteRoot(new Dictionary<string, object>
            {
                ["core"] = new Dictionary<string, object>
                {
                    ["current_level"] = 5,
                    ["unknown_field"] = 99
                },
                ["unknown_root"] = new Dictionary<string, object>
                {
                    ["x"] = 1
                }
            });
            var registry = new DataSyncRegistry();
            registry.Register(new FakeSavable("core", new()
            {
                ["current_level"] = 10,
                ["tool_hint"] = 3
            }));
            DataSyncService service = CreateService(
                registry,
                new FakeDataSyncApi(),
                snapshot);

            Dictionary<string, object> root = service.BuildRoot();
            IReadOnlyDictionary<string, object> core = Block(root, "core");
            Assert.That(ValueInt(core, "current_level"), Is.EqualTo(10));
            Assert.That(ValueInt(core, "tool_hint"), Is.EqualTo(3));
            Assert.That(ValueInt(core, "unknown_field"), Is.EqualTo(99));
            Assert.That(
                ValueInt(Block(root, "unknown_root"), "x"),
                Is.EqualTo(1));
        }

        [Test]
        public void ApplyRemote_UsesFirstBasisContext_AndKeepsBaselineOnParseFail()
        {
            var snapshot = new MemoryDataSyncSnapshotStore();
            snapshot.SaveRemoteRoot(new Dictionary<string, object>
            {
                ["old"] = new Dictionary<string, object> { ["x"] = 1 }
            });
            var registry = new DataSyncRegistry();
            var core = new FakeSavable("core") { RemoteAhead = true };
            var streak = new FakeSavable("streak");
            registry.Register(core);
            registry.Register(streak);
            DataSyncService service = CreateService(
                registry,
                new FakeDataSyncApi(),
                snapshot);

            Assert.That(service.ApplyRemote("{not json"), Is.False);
            Assert.That(service.LastRemoteRoot.ContainsKey("old"), Is.True);
            string gameData = MiniJson.Serialize(new Dictionary<string, object>
            {
                ["core"] = new Dictionary<string, object>
                {
                    ["current_level"] = 20
                },
                ["streak"] = new Dictionary<string, object>
                {
                    ["current_streak"] = 4
                }
            });
            Assert.That(service.ApplyRemote(gameData), Is.True);
            Assert.That(core.MergeCount, Is.EqualTo(1));
            Assert.That(streak.MergeCount, Is.EqualTo(1));
            Assert.That(core.LastContext.RemoteAhead, Is.True);
            Assert.That(streak.LastContext.RemoteAhead, Is.True);
            Assert.That(
                ValueInt(core.LastRemote, "current_level"),
                Is.EqualTo(20));
        }

        [Test]
        public void GameStateSavable_RemoteAheadOverwritesSourceFieldsAndSignalsTools()
        {
            var data = new GameStateData
            {
                CurrentLevel = 2,
                CurrentStrategy = 1,
                ToolLocate = 5,
                ToolHint = 5,
                TutorialDone = false
            };
            var state = new GameStateService(data);
            var toolEvents = new List<string>();
            state.ToolCountChanged += (kind, count) =>
                toolEvents.Add($"{kind}:{count}");

            Assert.That(state.IsRemoteAhead(new Dictionary<string, object>
            {
                ["current_level"] = 8
            }), Is.True);
            Assert.That(state.MergeRemote(
                new Dictionary<string, object>
                {
                    ["current_level"] = 8,
                    ["current_strategy"] = 4,
                    ["tool_locate"] = 2,
                    ["tool_hint"] = 7
                },
                new DataSyncMergeContext(true)), Is.True);
            Assert.That(data.CurrentLevel, Is.EqualTo(8));
            Assert.That(data.CurrentStrategy, Is.EqualTo(4));
            Assert.That(data.TutorialDone, Is.True);
            Assert.That(toolEvents, Is.EquivalentTo(new[]
            {
                "locate:2",
                "hint:7"
            }));
            Assert.That(state.ExportRemote()["tool_hint"], Is.EqualTo(7));
        }

        [Test]
        public void FirstUpload_ThenMatchingMeta_SkipsSecondUpload()
        {
            var api = new FakeDataSyncApi();
            api.DownloadResponses.Enqueue(Response(ApiConfig.CodeNoSave));
            api.UploadResponses.Enqueue(Response(ApiConfig.CodeOk));
            var registry = new DataSyncRegistry();
            registry.Register(new FakeSavable("core", new()
            {
                ["current_level"] = 3
            }));
            DataSyncService service = CreateService(registry, api);

            DataSyncOutcome first = RunSync(service, "startup");
            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.IsFirstUpload, Is.True);
            Assert.That(first.SyncCode, Is.EqualTo(1));
            Assert.That(api.UploadSyncCodes, Is.EqualTo(new[] { 1 }));
            Assert.That(api.UploadSchemas[0], Is.EqualTo("1.0.0"));

            api.MetaResponses.Enqueue(Response(
                ApiConfig.CodeOk,
                new Dictionary<string, object> { ["sync_code"] = 1 }));
            DataSyncOutcome second = RunSync(service, "profile_save");
            Assert.That(second.Succeeded, Is.True);
            Assert.That(second.Changed, Is.False);
            Assert.That(api.UploadSyncCodes.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoteDownload_MergesSharedContext_AndSkipsUnchangedUpload()
        {
            var api = new FakeDataSyncApi();
            var core = new FakeSavable("core", new()
            {
                ["current_level"] = 5
            })
            {
                RemoteAhead = true,
                AdoptRemoteOnMerge = true
            };
            string remote = MiniJson.Serialize(new Dictionary<string, object>
            {
                ["core"] = new Dictionary<string, object>
                {
                    ["current_level"] = 10,
                    ["unknown"] = 7
                }
            });
            api.DownloadResponses.Enqueue(Response(
                ApiConfig.CodeOk,
                new Dictionary<string, object>
                {
                    ["game_data"] = remote,
                    ["sync_code"] = 4
                }));
            var registry = new DataSyncRegistry();
            registry.Register(core);
            DataSyncService service = CreateService(registry, api);

            DataSyncOutcome outcome = RunSync(service, "startup");
            Assert.That(outcome.Succeeded, Is.True);
            Assert.That(outcome.Changed, Is.True);
            Assert.That(outcome.SyncCode, Is.EqualTo(4));
            Assert.That(core.LastContext.RemoteAhead, Is.True);
            Assert.That(api.UploadSyncCodes, Is.Empty);
        }

        [Test]
        public void UploadConflict_DownloadsMergesAndRetriesAtRemoteCodePlusOne()
        {
            var api = new FakeDataSyncApi();
            api.DownloadResponses.Enqueue(Response(ApiConfig.CodeNoSave));
            api.UploadResponses.Enqueue(Response(
                ApiConfig.CodeSyncCodeTooLow));
            api.DownloadResponses.Enqueue(Response(
                ApiConfig.CodeOk,
                new Dictionary<string, object>
                {
                    ["game_data"] = MiniJson.Serialize(
                        new Dictionary<string, object>
                        {
                            ["core"] = new Dictionary<string, object>
                            {
                                ["current_level"] = 8,
                                ["unknown"] = 77
                            }
                        }),
                    ["sync_code"] = 5
                }));
            api.UploadResponses.Enqueue(Response(ApiConfig.CodeOk));
            var core = new FakeSavable("core", new()
            {
                ["current_level"] = 10
            });
            var registry = new DataSyncRegistry();
            registry.Register(core);
            DataSyncService service = CreateService(registry, api);

            DataSyncOutcome outcome = RunSync(service, "startup");
            Assert.That(outcome.Succeeded, Is.True);
            Assert.That(outcome.SyncCode, Is.EqualTo(6));
            Assert.That(api.UploadSyncCodes, Is.EqualTo(new[] { 1, 6 }));
            IReadOnlyDictionary<string, object> secondRoot = Block(
                DeserializeRoot(api.UploadGameData[1]),
                "core");
            Assert.That(ValueInt(secondRoot, "current_level"), Is.EqualTo(10));
            Assert.That(ValueInt(secondRoot, "unknown"), Is.EqualTo(77));
        }

        [Test]
        public void ExpiredDownloadToken_RefreshesOnceBeforeFirstUpload()
        {
            var auth = new FakeAuthGateway("old", "new");
            var api = new FakeDataSyncApi();
            api.DownloadResponses.Enqueue(Response(
                ApiConfig.CodeAccessTokenExpired));
            api.DownloadResponses.Enqueue(Response(ApiConfig.CodeNoSave));
            api.UploadResponses.Enqueue(Response(ApiConfig.CodeOk));
            var registry = new DataSyncRegistry();
            registry.Register(new FakeSavable("core"));
            var service = new DataSyncService(
                auth,
                api,
                registry,
                new MemoryDataSyncSnapshotStore());

            DataSyncOutcome outcome = RunSync(service, "startup");
            Assert.That(outcome.Succeeded, Is.True);
            Assert.That(auth.ForceRefreshRequests, Is.EqualTo(new[]
            {
                false,
                true
            }));
            Assert.That(api.DownloadBearers, Is.EqualTo(new[]
            {
                "old",
                "new"
            }));
            Assert.That(api.UploadBearers[0], Is.EqualTo("new"));
        }

        [Test]
        public void Repositories_RoundTripRemoteRootAndDevelopmentSwitch()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "MeowdokuDataSyncTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var snapshots = new DataSyncSnapshotRepository(directory);
                Assert.That(snapshots.SaveRemoteRoot(
                    new Dictionary<string, object>
                    {
                        ["core"] = new Dictionary<string, object>
                        {
                            ["current_level"] = 12,
                            ["unknown"] = 99
                        }
                    }), Is.True);
                Dictionary<string, object> loaded =
                    new DataSyncSnapshotRepository(directory)
                        .LoadRemoteRoot();
                Assert.That(
                    ValueInt(Block(loaded, "core"), "current_level"),
                    Is.EqualTo(12));
                Assert.That(
                    ValueInt(Block(loaded, "core"), "unknown"),
                    Is.EqualTo(99));

                var enable = new DataSyncEnableRepository(directory);
                Assert.That(enable.TryLoad(out _), Is.False);
                Assert.That(enable.Save(true), Is.True);
                Assert.That(
                    new DataSyncEnableRepository(directory)
                        .TryLoad(out bool enabled),
                    Is.True);
                Assert.That(enabled, Is.True);
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static DataSyncService CreateService(
            DataSyncRegistry registry,
            FakeDataSyncApi api,
            IDataSyncSnapshotStore snapshot = null)
        {
            return new DataSyncService(
                new FakeAuthGateway("token"),
                api,
                registry,
                snapshot ?? new MemoryDataSyncSnapshotStore());
        }

        private static DataSyncOutcome RunSync(
            DataSyncService service,
            string reason)
        {
            DataSyncOutcome outcome = default;
            Run(service.Synchronize(reason, value => outcome = value));
            return outcome;
        }

        private static void Run(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            int moves = 0;
            while (stack.Count > 0)
            {
                if (++moves > 10000)
                    Assert.Fail("Coroutine did not finish.");
                IEnumerator current = stack.Peek();
                if (!current.MoveNext())
                {
                    stack.Pop();
                    continue;
                }
                if (current.Current is IEnumerator nested)
                    stack.Push(nested);
            }
        }

        private static DataSyncApiResponse Response(
            int code,
            IReadOnlyDictionary<string, object> data = null)
        {
            return new DataSyncApiResponse(true, code, data);
        }

        private static IReadOnlyDictionary<string, object> Block(
            IReadOnlyDictionary<string, object> root,
            string key)
        {
            return root[key] as IReadOnlyDictionary<string, object> ??
                   throw new AssertionException($"Missing block {key}.");
        }

        private static int ValueInt(
            IReadOnlyDictionary<string, object> values,
            string key)
        {
            return Convert.ToInt32(values[key]);
        }

        private static Dictionary<string, object> DeserializeRoot(string raw)
        {
            return MiniJson.Deserialize(raw) as Dictionary<string, object> ??
                   throw new AssertionException("Upload root is not JSON object.");
        }

        private sealed class FakeSavable :
            IDataSyncSavable,
            IDataSyncMergeBasis
        {
            private Dictionary<string, object> _export;

            public FakeSavable(
                string id,
                Dictionary<string, object> export = null)
            {
                RemoteSaveId = id;
                _export = export ?? new Dictionary<string, object>();
            }

            public string RemoteSaveId { get; }
            public bool RemoteAhead { get; set; }
            public bool AdoptRemoteOnMerge { get; set; }
            public int MergeCount { get; private set; }
            public IReadOnlyDictionary<string, object> LastRemote { get; private set; }
            public DataSyncMergeContext LastContext { get; private set; }

            public Dictionary<string, object> ExportRemote() =>
                new(_export);

            public bool IsRemoteAhead(
                IReadOnlyDictionary<string, object> remote) => RemoteAhead;

            public bool MergeRemote(
                IReadOnlyDictionary<string, object> remote,
                DataSyncMergeContext context)
            {
                MergeCount++;
                LastRemote = remote;
                LastContext = context;
                if (AdoptRemoteOnMerge)
                {
                    _export = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, object> pair in remote)
                        if (pair.Key != "unknown")
                            _export[pair.Key] = pair.Value;
                }
                return true;
            }
        }

        private sealed class FakeAuthGateway : IDataSyncAuthGateway
        {
            private readonly Queue<string> _tokens = new();

            public FakeAuthGateway(params string[] tokens)
            {
                foreach (string token in tokens) _tokens.Enqueue(token);
            }

            public bool IsAvailable => true;
            public bool IsLoggedIn => true;
            public List<bool> ForceRefreshRequests { get; } = new();

            public IEnumerator RequestAccessToken(
                bool forceRefresh,
                Action<AuthTokenResult> completed)
            {
                ForceRefreshRequests.Add(forceRefresh);
                string token = _tokens.Count > 0 ? _tokens.Dequeue() : "token";
                completed?.Invoke(new AuthTokenResult(token, 0, string.Empty));
                yield break;
            }
        }

        private sealed class FakeDataSyncApi : IDataSyncApi
        {
            public bool IsAvailable => true;
            public Queue<DataSyncApiResponse> MetaResponses { get; } = new();
            public Queue<DataSyncApiResponse> DownloadResponses { get; } = new();
            public Queue<DataSyncApiResponse> UploadResponses { get; } = new();
            public List<string> DownloadBearers { get; } = new();
            public List<string> UploadBearers { get; } = new();
            public List<int> UploadSyncCodes { get; } = new();
            public List<string> UploadSchemas { get; } = new();
            public List<string> UploadGameData { get; } = new();

            public IEnumerator FetchMeta(
                string bearer,
                Action<DataSyncApiResponse> completed)
            {
                completed?.Invoke(MetaResponses.Dequeue());
                yield break;
            }

            public IEnumerator Download(
                string bearer,
                Action<DataSyncApiResponse> completed)
            {
                DownloadBearers.Add(bearer);
                completed?.Invoke(DownloadResponses.Dequeue());
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
                UploadBearers.Add(bearer);
                UploadSyncCodes.Add(syncCode);
                UploadSchemas.Add(schemaVersion);
                UploadGameData.Add(gameData);
                completed?.Invoke(UploadResponses.Dequeue());
                yield break;
            }
        }
    }
}
