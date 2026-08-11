using System;
using System.Collections.Generic;
using System.Linq;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Core.Platform;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class PlatformPermissionTests
    {
        [Test]
        public void PushGuide_ThreeDayGroupUsesAllSourceEligibilityGates()
        {
            var state = new GameStateService(
                new GameStateData
                {
                    TodayDate = "2026-08-12",
                    RecentWinCountsByDay = new Dictionary<string, object>
                    {
                        ["2026-08-10"] = 7,
                        ["2026-08-11"] = 6,
                        ["2026-08-12"] = 7
                    }
                },
                dateProvider: new FixedDate("2026-08-12"));
            var config = new PushPermissionConfig();
            config.SetDebugOverride(
                PushPermissionConfig.ValueThreeDayProgress);

            Assert.That(
                PushGuidePolicy.IsEligible(19, state, config),
                Is.False);
            Assert.That(
                PushGuidePolicy.IsEligible(20, state, config),
                Is.True);

            state.MarkPushGuideTriggered();

            Assert.That(
                PushGuidePolicy.IsEligible(20, state, config),
                Is.False);
        }

        [Test]
        public void PushGuide_SessionGroupRequiresFiveConsecutiveWins()
        {
            var state = new GameStateService(new GameStateData
            {
                CurrentLevel = 20
            });
            var config = new PushPermissionConfig();
            config.SetDebugOverride(
                PushPermissionConfig.ValueSessionStreak);

            for (int index = 0; index < 4; index++)
                state.OnLevelWon(state.CurrentLevel);
            Assert.That(
                PushGuidePolicy.IsEligible(24, state, config),
                Is.False);

            state.OnLevelWon(state.CurrentLevel);

            Assert.That(
                PushGuidePolicy.IsEligible(25, state, config),
                Is.True);
        }

        [Test]
        public void PushGuide_PopupCapIsFiveRegardlessOfTriggeredCount()
        {
            var data = new GameStateData
            {
                PushGuidePopupCount = 5,
                PushGuideShownCount = 99,
                TodayDate = "2026-08-12",
                RecentWinCountsByDay = new Dictionary<string, object>
                {
                    ["2026-08-12"] = 20
                }
            };
            var state = new GameStateService(
                data,
                dateProvider: new FixedDate("2026-08-12"));
            var config = new PushPermissionConfig();
            config.SetDebugOverride(
                PushPermissionConfig.ValueThreeDayProgress);

            Assert.That(
                PushGuidePolicy.IsEligible(20, state, config),
                Is.False);
        }

        [Test]
        public void LocalNotifications_LegacyUsesFourNoonAndFourEveningTexts()
        {
            IReadOnlyList<DailyLocalNotification> notifications =
                DailyLocalNotificationFactory.Build(
                    new PushLocalTextConfig(),
                    key => "T:" + key,
                    new DateTime(2026, 8, 12, 12, 0, 0));

            Assert.That(notifications.Select(item => item.Id),
                Is.EqualTo(new[] { "daily_noon", "daily_evening" }));
            Assert.That(notifications[0].Contents.Count, Is.EqualTo(4));
            Assert.That(notifications[1].Contents.Count, Is.EqualTo(4));
            Assert.That(notifications[0].Contents[0].Title,
                Is.EqualTo("T:PUSH_TITLE"));
            Assert.That(notifications[0].Contents[0].Body,
                Is.EqualTo("T:PUSH_CONTENT_1"));
            Assert.That(notifications[1].Contents[3].Body,
                Is.EqualTo("T:PUSH_CONTENT_8"));
            Assert.That(notifications[0].AdvanceOneDay, Is.True);
            Assert.That(notifications[1].AdvanceOneDay, Is.False);
            Assert.That(notifications[0].RepeatIntervalMilliseconds,
                Is.EqualTo(86_400_000L));
            Assert.That(notifications[0].IsInfiniteRepeat, Is.True);
        }

        [Test]
        public void LocalNotifications_NewPoolTwoShufflesFiveFromEachHundred()
        {
            var config = new PushLocalTextConfig();
            config.SetDebugOverride(PushLocalTextConfig.ValueNewPool2);

            IReadOnlyList<DailyLocalNotification> notifications =
                DailyLocalNotificationFactory.Build(
                    config,
                    key => key,
                    new DateTime(2026, 8, 12, 9, 30, 0),
                    new Random(7));

            Assert.That(notifications[0].Contents.Count, Is.EqualTo(5));
            Assert.That(notifications[1].Contents.Count, Is.EqualTo(5));
            Assert.That(notifications[0].Contents.Select(item => item.Title),
                Is.Unique);
            Assert.That(notifications[0].Contents.All(item =>
                item.Title.StartsWith("PUSH2_NOON_TITLE_")), Is.True);
            Assert.That(notifications[1].Contents.All(item =>
                item.Body.StartsWith("PUSH2_EVE_BODY_")), Is.True);
        }

        [Test]
        public void OfflineProvider_CompletesEveryAwaitableBoundaryImmediately()
        {
            IPlatformPermissionProvider provider =
                OfflinePlatformPermissionProvider.Instance;
            int callbacks = 0;

            provider.CheckConsentManagement(() => callbacks++);
            provider.RequestTrackingAuthorization("splash_scr", () => callbacks++);
            provider.RequestNotificationPermission(
                NotificationPermissionRequestType.System,
                "app_start",
                () => callbacks++);

            Assert.That(callbacks, Is.EqualTo(3));
            Assert.That(provider.IsMobile, Is.False);
            Assert.That(provider.IsPrivacyRequired, Is.False);
        }

        private sealed class FixedDate : ICurrentDateProvider
        {
            public FixedDate(string date) { CurrentDate = date; }
            public string CurrentDate { get; }
        }
    }
}
