using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Meowdoku.Core;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Profile;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class ProfileServiceTests
    {
        [Test]
        public void EmptyProfile_InitializesSourceCatalogAndIdentityOnce()
        {
            var store = new MemoryStore(new ProfileData());
            var service = new ProfileService(store, new FixedRandom(0));

            Assert.That(service.Nickname, Is.EqualTo("AAAAAA"));
            Assert.That(service.AvatarId, Is.EqualTo(1));
            Assert.That(service.FrameId, Is.EqualTo(1));
            Assert.That(service.IsIdentityDefault, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
            foreach (int id in ProfileCatalog.ClassicFrameIds)
            {
                Assert.That(service.IsFrameUnlocked(id), Is.True);
                Assert.That(service.GetFrameCount(id), Is.EqualTo(-1));
            }
        }

        [Test]
        public void IdentityMutations_ValidateTrimAndTwelveCodePoints()
        {
            var service = new ProfileService(
                InitializedStore(),
                new FixedRandom(0));
            int changes = 0;
            service.AvatarFrameChanged += () => changes++;

            service.SetNickname("  12345678901😀Z  ");
            service.SetAvatarId(8);
            service.SetAvatarId(99);
            service.SetFrameId(100);

            Assert.That(service.Nickname, Is.EqualTo("12345678901😀"));
            Assert.That(service.AvatarId, Is.EqualTo(8));
            Assert.That(service.FrameId, Is.EqualTo(1));
            Assert.That(service.IsIdentityDefault, Is.False);
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void GrantFrame_UnlocksCountsAndRaisesRedDotOnlyOnFirstCopy()
        {
            var service = new ProfileService(
                InitializedStore(),
                new FixedRandom(0));

            Assert.That(service.GrantFrame(100, 2), Is.True);
            Assert.That(service.GetFrameCount(100), Is.EqualTo(2));
            Assert.That(service.HasFrameRedDot, Is.True);
            service.ClearFrameRedDot();
            Assert.That(service.HasFrameRedDot, Is.False);

            Assert.That(service.GrantFrame(100), Is.True);
            Assert.That(service.GetFrameCount(100), Is.EqualTo(3));
            Assert.That(service.HasFrameRedDot, Is.False);
            service.SetFrameId(100);
            Assert.That(service.FrameId, Is.EqualTo(100));
        }

        [Test]
        public void RemoteExportAndAheadMerge_KeepOnlyAcquiredFrames()
        {
            var local = new ProfileService(
                InitializedStore(),
                new FixedRandom(0));
            local.SetNickname("Mèo Việt");
            local.GrantFrame(100, 2);
            Dictionary<string, object> exported = local.ExportRemote();

            Assert.That(exported["nickname"], Is.EqualTo(
                "b64:" + Convert.ToBase64String(
                    Encoding.UTF8.GetBytes("Mèo Việt"))));
            Assert.That((IList)exported["owned_frames"],
                Has.Count.EqualTo(1));

            var remote = new ProfileService(
                InitializedStore(),
                new FixedRandom(0));
            Assert.That(remote.MergeRemote(exported, false), Is.False);
            Assert.That(remote.MergeRemote(exported, true), Is.True);
            Assert.That(remote.Nickname, Is.EqualTo("Mèo Việt"));
            Assert.That(remote.GetFrameCount(100), Is.EqualTo(2));
            Assert.That(remote.IsFrameUnlocked(1), Is.True);
        }

        [Test]
        public void PlayerInfo_RoundTripsSourceKeys()
        {
            var value = new PlayerInfo
            {
                Nickname = "CAT123",
                AvatarId = 4,
                Frame = new AvatarFrame(100, 3),
                PlayerId = PlayerInfo.LocalPlayerId,
                LevelIndex = 17,
                IsRobot = false
            };

            PlayerInfo restored = PlayerInfo.FromDictionary(
                value.ToDictionary());

            Assert.That(restored.Nickname, Is.EqualTo("CAT123"));
            Assert.That(restored.Frame.Id, Is.EqualTo(100));
            Assert.That(restored.Frame.AcquiredCount, Is.EqualTo(3));
            Assert.That(restored.IsSelf, Is.True);
        }

        [Test]
        public void AwardManager_FrameBoundaryPersistsIntoProfileInventory()
        {
            var profile = new ProfileService(
                InitializedStore(),
                new FixedRandom(0));
            var awards = new AwardManager(
                new GameStateService(new GameStateData()),
                profile);

            int uid = awards.Dispatch(
                new[] { AwardItem.Frame(100, 2) },
                AwardDisplayType.Direct,
                "rank_reward");

            Assert.That(uid, Is.GreaterThan(0));
            Assert.That(profile.GetFrameCount(100), Is.EqualTo(2));
            Assert.That(profile.HasFrameRedDot, Is.True);
        }

        private static MemoryStore InitializedStore()
        {
            var data = new ProfileData
            {
                Nickname = "ABC123",
                AvatarId = 1,
                FrameId = 1,
                Initialized = true
            };
            foreach (int id in ProfileCatalog.ClassicFrameIds)
                data.OwnedFrames[id] = new AvatarFrame(id, -1);
            return new MemoryStore(data);
        }

        private sealed class FixedRandom : IProfileRandom
        {
            private readonly int _value;
            public FixedRandom(int value) { _value = value; }
            public int NextInclusive(int minimum, int maximum) =>
                Math.Clamp(_value, minimum, maximum);
        }

        private sealed class MemoryStore : IProfileDataStore
        {
            private ProfileData _data;
            public MemoryStore(ProfileData data) { _data = data; }
            public int SaveCount { get; private set; }
            public ProfileData Load() => _data;
            public bool Save(ProfileData data)
            {
                _data = data;
                SaveCount++;
                return true;
            }
            public void Reset() { _data = new ProfileData(); }
        }
    }
}
