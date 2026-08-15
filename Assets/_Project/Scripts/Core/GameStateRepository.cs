using System;
using System.IO;
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
        private readonly BackgroundSaveWriter _playerWriter;
        private readonly BackgroundSaveWriter _endgameWriter;

        public GameStateRepository(
            string persistentDataPath,
            bool useBackgroundEndgameWrites = false,
            bool useBackgroundPlayerWrites = false)
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
            if (useBackgroundPlayerWrites)
                _playerWriter = new BackgroundSaveWriter(_playerStore);
            if (useBackgroundEndgameWrites)
                _endgameWriter = new BackgroundSaveWriter(_endgameStore);
        }

        public static GameStateRepository CreateDefault()
        {
            // Godot's native ConfigFile encryption is not available in Unity.
            // The Unity adapter uses PBKDF2 plus a verified fsync, so runtime
            // player/endgame writes must not execute on the frame/input thread.
            return new GameStateRepository(
                Application.persistentDataPath,
                useBackgroundEndgameWrites: true,
                useBackgroundPlayerWrites: true);
        }

        public GameStateData Load()
        {
            _playerStore.MigrateLegacyIfNeeded();
            GameStateData data = GameStateData.FromDocuments(
                _playerStore.LoadConfig(),
                _endgameStore.LoadConfig());
            if (data.SeedPortfolioToolsIfNeeded())
                SavePlayer(data);
            return data;
        }

        public bool SavePlayer(GameStateData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            string serialized = MiniJson.Serialize(data.ToPlayerDocument());
            return _playerWriter != null
                ? _playerWriter.RequestSave(serialized)
                : _playerStore.SaveSerializedConfig(serialized);
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
            return _endgameWriter == null || _endgameWriter.Flush();
        }

        public bool FlushPlayerWrites()
        {
            return _playerWriter == null || _playerWriter.Flush();
        }

        public bool FlushPendingWrites()
        {
            bool playerSucceeded = FlushPlayerWrites();
            bool endgameSucceeded = FlushEndgameWrites();
            return playerSucceeded && endgameSucceeded;
        }

        private bool QueueOrApplyEndgameWrite(string serialized, bool remove)
        {
            if (_endgameWriter == null)
            {
                if (remove)
                {
                    _endgameStore.Remove();
                    return true;
                }
                return _endgameStore.SaveSerializedConfig(serialized);
            }

            return remove
                ? _endgameWriter.RequestRemove()
                : _endgameWriter.RequestSave(serialized);
        }
    }
}
