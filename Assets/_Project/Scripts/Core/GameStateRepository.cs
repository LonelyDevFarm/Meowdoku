using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Meowdoku.Core
{
    public interface IGameStatePlayerStore
    {
        bool SavePlayer(GameStateData data);
    }

    public interface IGameStateEndgameStore
    {
        bool SaveEndgame(GameStateData data);
        bool RequestSaveEndgame(GameStateData data);
        void ClearEndgame();
    }

    /// <summary>
    /// Owns the player dual-slot store and the separate endgame store used by
    /// game_state.gd. Runtime mutation/signals remain the responsibility of the
    /// future GameState service.
    /// </summary>
    public sealed class GameStateRepository : IGameStatePlayerStore, IGameStateEndgameStore
    {
        private const string SavePassword = "qd_x9K3mPv7RtN2sLwH8jFcZyA5eBkM1n";

        private readonly SaveStore _playerStore;
        private readonly SaveStore _endgameStore;
        private readonly bool _useBackgroundEndgameWrites;
        private readonly object _endgameWriteGate = new object();
        private string _pendingEndgameJson;
        private bool _pendingEndgameRemove;
        private bool _hasPendingEndgameWrite;
        private bool _endgameWorkerRunning;
        private bool _lastEndgameWriteSucceeded = true;

        public GameStateRepository(
            string persistentDataPath,
            bool useBackgroundEndgameWrites = false)
        {
            if (string.IsNullOrEmpty(persistentDataPath))
                throw new ArgumentException("Persistent data path cannot be empty.", nameof(persistentDataPath));

            string saveDirectory = Path.Combine(persistentDataPath, "save_store");
            _playerStore = new SaveStore(
                SavePassword,
                saveDirectory,
                true,
                Path.Combine(saveDirectory, "save_a.cfg"),
                Path.Combine(saveDirectory, "save_b.cfg"),
                Path.Combine(saveDirectory, "flag.txt"),
                Path.Combine(persistentDataPath, "save.cfg"));
            _endgameStore = new SaveStore(
                SavePassword,
                saveDirectory,
                false,
                Path.Combine(saveDirectory, "endgame.cfg"));
            _useBackgroundEndgameWrites = useBackgroundEndgameWrites;
        }

        public static GameStateRepository CreateDefault()
        {
            // Godot's native ConfigFile encryption is not available in Unity.
            // The Unity adapter uses PBKDF2 plus a verified fsync, so runtime
            // endgame writes must not execute on the frame/input thread.
            return new GameStateRepository(
                Application.persistentDataPath,
                useBackgroundEndgameWrites: true);
        }

        public GameStateData Load()
        {
            _playerStore.MigrateLegacyIfNeeded();
            return GameStateData.FromDocuments(
                _playerStore.LoadConfig(),
                _endgameStore.LoadConfig());
        }

        public bool SavePlayer(GameStateData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return _playerStore.SaveConfig(data.ToPlayerDocument());
        }

        public bool SaveEndgame(GameStateData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.IsEndgameStoreEmpty())
            {
                return QueueOrApplyEndgameWrite(null, true);
            }

            // Serialize on the caller before queueing. The resulting string is
            // immutable, so gameplay can keep mutating GameStateData while the
            // encrypted file is written on a worker thread.
            string serialized = MiniJson.Serialize(data.ToEndgameDocument());
            return QueueOrApplyEndgameWrite(serialized, false);
        }

        public bool RequestSaveEndgame(GameStateData data)
        {
            return SaveEndgame(data);
        }

        public void ClearEndgame()
        {
            QueueOrApplyEndgameWrite(null, true);
        }

        /// <summary>
        /// Waits for the latest queued runtime endgame state to reach disk.
        /// Call only at lifecycle durability boundaries, never in gameplay.
        /// </summary>
        public bool FlushEndgameWrites()
        {
            if (!_useBackgroundEndgameWrites) return true;

            lock (_endgameWriteGate)
            {
                while (_endgameWorkerRunning || _hasPendingEndgameWrite)
                    Monitor.Wait(_endgameWriteGate);
                return _lastEndgameWriteSucceeded;
            }
        }

        private bool QueueOrApplyEndgameWrite(string serialized, bool remove)
        {
            if (!_useBackgroundEndgameWrites)
            {
                if (remove)
                {
                    _endgameStore.Remove();
                    return true;
                }
                return _endgameStore.SaveSerializedConfig(serialized);
            }

            lock (_endgameWriteGate)
            {
                // Latest state wins, matching the source's coalesced snapshot
                // persistence while guaranteeing writes remain ordered.
                _pendingEndgameJson = serialized;
                _pendingEndgameRemove = remove;
                _hasPendingEndgameWrite = true;
                if (_endgameWorkerRunning) return true;

                _endgameWorkerRunning = true;
                if (ThreadPool.QueueUserWorkItem(ProcessEndgameWrites)) return true;

                _endgameWorkerRunning = false;
                _hasPendingEndgameWrite = false;
                _lastEndgameWriteSucceeded = false;
                Monitor.PulseAll(_endgameWriteGate);
                return false;
            }
        }

        private void ProcessEndgameWrites(object state)
        {
            while (true)
            {
                string serialized;
                bool remove;
                lock (_endgameWriteGate)
                {
                    if (!_hasPendingEndgameWrite)
                    {
                        _endgameWorkerRunning = false;
                        Monitor.PulseAll(_endgameWriteGate);
                        return;
                    }

                    serialized = _pendingEndgameJson;
                    remove = _pendingEndgameRemove;
                    _hasPendingEndgameWrite = false;
                }

                bool succeeded;
                try
                {
                    if (remove)
                    {
                        _endgameStore.Remove();
                        succeeded = true;
                    }
                    else
                    {
                        succeeded = _endgameStore.SaveSerializedConfig(serialized);
                    }
                }
                catch (Exception)
                {
                    succeeded = false;
                }

                lock (_endgameWriteGate)
                    _lastEndgameWriteSucceeded = succeeded;
            }
        }
    }
}
