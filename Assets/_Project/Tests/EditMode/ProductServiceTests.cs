using System;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Core.Platform;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class ProductServiceTests
    {
        [Test]
        public void RateUsPolicy_MatchesSourceLevelAndStreakGates()
        {
            var config = new RateUsPopConfig();
            Assert.That(config.IsEligibleAtGameWin(7, 99), Is.False);
            Assert.That(config.IsEligibleAtGameWin(8, 0), Is.True);

            config.SetDebugOverride(RateUsPopConfig.ValueWinStreak5);
            Assert.That(config.IsEligibleAtGameWin(14, 5), Is.False);
            Assert.That(config.IsEligibleAtGameWin(15, 4), Is.False);
            Assert.That(config.IsEligibleAtGameWin(15, 5), Is.True);
        }

        [Test]
        public void GameState_ProductFieldsRoundTripThroughPlayerDocument()
        {
            var state = new GameStateService(new GameStateData
            {
                HasShownRateUs = true,
                HelpLastOpenTime = 1_728_000_123L,
                InstallVersion = "0.9.7"
            });

            Dictionary<string, object> document =
                state.Data.ToPlayerDocument();
            GameStateService restored = new GameStateService(
                GameStateData.FromDocuments(document, null));

            Assert.That(restored.HasShownRateUs, Is.True);
            Assert.That(restored.HelpLastOpenTime, Is.EqualTo(1_728_000_123L));
            Assert.That(restored.InstallVersion, Is.EqualTo("0.9.7"));
        }

        [Test]
        public void HelpSupport_UsesSourceProviderBoundaryWithoutNativeRequirement()
        {
            IProductServiceProvider provider =
                OfflineProductServiceProvider.Instance;
            int unread = 0;
            provider.HelpUnreadCountChanged += value => unread = value;

            provider.InstallHelp(
                HelpSupportConfiguration.AndroidAppId,
                HelpSupportConfiguration.IosPlatformId,
                HelpSupportConfiguration.IosApiKey,
                HelpSupportConfiguration.Domain);
            provider.ShowHelpFaq(
                new Dictionary<string, object>(),
                new Dictionary<string, object>());
            provider.RequestHelpUnreadMessageCount(true);
            provider.RequestStoreReview();

            Assert.That(provider.IsOnline, Is.False);
            Assert.That(provider.IsHelpAvailable, Is.False);
            Assert.That(provider.ConsumeShortcut(), Is.EqualTo(
                ProductShortcutAction.None));
            Assert.That(unread, Is.Zero);
            Assert.That(HelpSupportConfiguration.ActiveWindowSeconds,
                Is.EqualTo(172800L));
        }
    }
}
