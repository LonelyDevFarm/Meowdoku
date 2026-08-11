using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Online;

namespace Meowdoku.Core.Profile
{
    /// <summary>
    /// Source-shaped owner of local identity and avatar-frame inventory.
    /// Storage and randomness are injected so runtime composition and tests do
    /// not depend on a global singleton.
    /// </summary>
    public sealed class ProfileService : IFrameAwardSink, IDataSyncSavable
    {
        private const int MaximumNicknameCodePoints = 12;
        private const string NickBase64Prefix = "b64:";

        private readonly IProfileDataStore _store;
        private readonly IProfileRandom _random;
        private ProfileData _data;

        public ProfileService(
            IProfileDataStore store,
            IProfileRandom random = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _random = random ?? new SystemProfileRandom();
            _data = _store.Load() ?? new ProfileData();
            EnsureInitialized();
        }

        public event Action AvatarFrameChanged;
        public event Action ProfileSaved;

        public string Nickname => _data.Nickname;
        public int AvatarId => _data.AvatarId;
        public int FrameId => _data.FrameId;
        public bool HasFrameRedDot => _data.FrameRedDot;
        public bool IsIdentityDefault => !_data.IdentityCustomized;

        public void SetNickname(string newName)
        {
            string value = (newName ?? string.Empty).Trim();
            if (value.Length == 0) return;
            value = ProfileNickname.TruncateCodePoints(
                value,
                MaximumNicknameCodePoints);
            if (string.Equals(value, _data.Nickname, StringComparison.Ordinal))
                return;
            _data.Nickname = value;
            _data.IdentityCustomized = true;
            SaveAndNotify();
        }

        public void SetAvatarId(int id)
        {
            if (!ProfileCatalog.IsValidAvatar(id) || id == _data.AvatarId)
                return;
            _data.AvatarId = id;
            _data.IdentityCustomized = true;
            SaveAndNotify();
        }

        public void SetFrameId(int id)
        {
            if (!ProfileCatalog.IsValidFrame(id) ||
                !IsFrameUnlocked(id) ||
                id == _data.FrameId)
                return;
            _data.FrameId = id;
            _data.IdentityCustomized = true;
            SaveAndNotify();
        }

        public bool GrantFrame(int frameId, int count = 1)
        {
            if (!ProfileCatalog.IsValidFrame(frameId) || count <= 0)
                return false;
            if (!_data.OwnedFrames.TryGetValue(
                    frameId,
                    out AvatarFrame frame))
            {
                frame = new AvatarFrame(
                    frameId,
                    ProfileCatalog.DefaultFrameCount(frameId));
                _data.OwnedFrames[frameId] = frame;
            }

            int before = frame.AcquiredCount;
            frame.AcquiredCount = Math.Max(frame.AcquiredCount, 0) + count;
            if (before < 1 && frame.AcquiredCount >= 1)
                _data.FrameRedDot = true;
            SaveAndNotify();
            return true;
        }

        public void RevokeFrameForCheat(int frameId)
        {
            if (!_data.OwnedFrames.Remove(frameId)) return;
            if (_data.FrameId == frameId)
                _data.FrameId = ProfileCatalog.ClassicFrameIds[0];
            SaveAndNotify();
        }

        public bool IsFrameUnlocked(int frameId) =>
            _data.OwnedFrames.ContainsKey(frameId);

        public int GetFrameCount(int frameId)
        {
            return _data.OwnedFrames.TryGetValue(
                frameId,
                out AvatarFrame frame)
                ? frame.AcquiredCount
                : ProfileCatalog.DefaultFrameCount(frameId);
        }

        public ProfileFrameInfo GetFrameInfo(int frameId) =>
            new(IsFrameUnlocked(frameId), GetFrameCount(frameId));

        public void ClearFrameRedDot()
        {
            if (!_data.FrameRedDot) return;
            _data.FrameRedDot = false;
            SaveAndNotify();
        }

        public void NotifyProfileSaved() => ProfileSaved?.Invoke();

        public PlayerInfo GetPlayerInfo()
        {
            return new PlayerInfo
            {
                Nickname = _data.Nickname,
                AvatarId = _data.AvatarId,
                Frame = new AvatarFrame(
                    _data.FrameId,
                    GetFrameCount(_data.FrameId)),
                PlayerId = PlayerInfo.LocalPlayerId,
                IsRobot = false
            };
        }

        public int[] GetAvatarIds() => ProfileCatalog.CopyAvatarIds();
        public int[] ListAvatarIds() => ProfileCatalog.CopyAvatarIds();
        public int[] GetFrameIds() => ProfileCatalog.CopyClassicFrameIds();

        public IReadOnlyList<ProfileFrameGroup> GetFrameGroups()
        {
            return new[]
            {
                new ProfileFrameGroup(
                    "leaderboard",
                    ProfileCatalog.CopyLeaderboardFrameIds()),
                new ProfileFrameGroup(
                    "classic",
                    ProfileCatalog.CopyClassicFrameIds())
            };
        }

        public void Reset()
        {
            _data = new ProfileData();
            EnsureInitialized();
        }

        public string RemoteSaveId => "profile";

        public Dictionary<string, object> ExportRemote()
        {
            var frames = new List<object>();
            foreach (KeyValuePair<int, AvatarFrame> pair in _data.OwnedFrames)
                if (pair.Key >= ProfileCatalog.AcquireThreshold &&
                    pair.Value != null)
                    frames.Add(pair.Value.ToDictionary());
            return new Dictionary<string, object>
            {
                ["nickname"] = NickBase64Prefix + Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(_data.Nickname ?? string.Empty)),
                ["avatar_id"] = _data.AvatarId,
                ["frame_id"] = _data.FrameId,
                ["identity_customized"] = _data.IdentityCustomized,
                ["owned_frames"] = frames
            };
        }

        public bool MergeRemote(
            IReadOnlyDictionary<string, object> remote,
            bool remoteAhead)
        {
            if (!remoteAhead || remote == null) return false;
            OverwriteFromRemote(remote);
            _store.Save(_data);
            AvatarFrameChanged?.Invoke();
            return true;
        }

        public bool MergeRemote(
            IReadOnlyDictionary<string, object> remote,
            DataSyncMergeContext context)
        {
            return MergeRemote(remote, context.RemoteAhead);
        }

        private void EnsureInitialized()
        {
            bool dirty = false;
            IReadOnlyList<int> classics = ProfileCatalog.ClassicFrameIds;
            for (int index = 0; index < classics.Count; index++)
            {
                int id = classics[index];
                if (_data.OwnedFrames.ContainsKey(id)) continue;
                _data.OwnedFrames[id] = new AvatarFrame(
                    id,
                    ProfileCatalog.DefaultFrameCount(id));
                dirty = true;
            }

            if (!ProfileCatalog.IsValidAvatar(_data.AvatarId))
            {
                IReadOnlyList<int> avatars = ProfileCatalog.AvatarIds;
                _data.AvatarId = avatars[_random.NextInclusive(
                    0,
                    avatars.Count - 1)];
                dirty = true;
            }
            if (!IsFrameUnlocked(_data.FrameId))
            {
                _data.FrameId = classics[_random.NextInclusive(
                    0,
                    classics.Count - 1)];
                dirty = true;
            }
            if (string.IsNullOrWhiteSpace(_data.Nickname))
            {
                _data.Nickname = ProfileNickname.RandomDefault(_random);
                dirty = true;
            }
            if (!_data.Initialized)
            {
                _data.Initialized = true;
                dirty = true;
            }
            if (dirty) _store.Save(_data);
        }

        private void OverwriteFromRemote(
            IReadOnlyDictionary<string, object> remote)
        {
            if (remote.TryGetValue("nickname", out object nickname))
                _data.Nickname = DecodeRemoteNickname(
                    Convert.ToString(nickname) ?? string.Empty);
            int avatarId = ProfileData.ReadInt(
                remote,
                "avatar_id",
                _data.AvatarId);
            if (ProfileCatalog.IsValidAvatar(avatarId))
                _data.AvatarId = avatarId;
            _data.IdentityCustomized = ProfileData.ReadBool(
                remote,
                "identity_customized",
                _data.IdentityCustomized);

            var remove = new List<int>();
            foreach (int id in _data.OwnedFrames.Keys)
                if (id >= ProfileCatalog.AcquireThreshold)
                    remove.Add(id);
            for (int index = 0; index < remove.Count; index++)
                _data.OwnedFrames.Remove(remove[index]);

            if (remote.TryGetValue("owned_frames", out object frames) &&
                frames is IList list)
            {
                for (int index = 0; index < list.Count; index++)
                {
                    if (list[index] is not
                        IReadOnlyDictionary<string, object> frameDictionary)
                        continue;
                    AvatarFrame frame = AvatarFrame.FromDictionary(
                        frameDictionary);
                    if (frame.Id >= ProfileCatalog.AcquireThreshold)
                        _data.OwnedFrames[frame.Id] = frame;
                }
            }
            _data.FrameId = ProfileData.ReadInt(
                remote,
                "frame_id",
                _data.FrameId);
        }

        private void SaveAndNotify()
        {
            _store.Save(_data);
            AvatarFrameChanged?.Invoke();
        }

        private static string DecodeRemoteNickname(string raw)
        {
            if (raw == null ||
                !raw.StartsWith(NickBase64Prefix, StringComparison.Ordinal))
                return raw ?? string.Empty;
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(
                    raw.Substring(NickBase64Prefix.Length)));
            }
            catch (FormatException)
            {
                return raw;
            }
        }

    }
}
