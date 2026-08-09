using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Gameplay;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class ToolResourceCoordinatorTests
    {
        [Test]
        public void ConsumeLastTool_StartsSharedEightHundredMillisecondRewardCooldown()
        {
            var data = new GameStateData { ToolLocate = 1, ToolHint = 0 };
            var state = new GameStateService(data);
            var coordinator = new ToolResourceCoordinator(
                state,
                new RewardUnlockLevelConfig());

            ToolResourceDecision consumed = coordinator.TryConsume(
                GameToolKind.Locate,
                1,
                1000);

            Assert.That(consumed, Is.EqualTo(ToolResourceDecision.Consumed));
            Assert.That(state.GetToolCount("locate"), Is.Zero);
            Assert.That(state.HasUsedTool, Is.True);
            Assert.That(
                coordinator.Inspect(GameToolKind.Hint, 1, 1799),
                Is.EqualTo(ToolResourceDecision.RewardCooldown));
            Assert.That(
                coordinator.Inspect(GameToolKind.Hint, 1, 1800),
                Is.EqualTo(ToolResourceDecision.RewardRequired));
        }

        [Test]
        public void FreeZone_DoesNotConsumeOrMarkToolUsed()
        {
            var data = new GameStateData { ToolLocate = 0 };
            var state = new GameStateService(data);
            var unlock = new RewardUnlockLevelConfig();
            unlock.SetDebugOverride(5);
            var coordinator = new ToolResourceCoordinator(state, unlock);

            ToolResourceDecision decision = coordinator.TryConsume(
                GameToolKind.Locate,
                4,
                1000);

            Assert.That(decision, Is.EqualTo(ToolResourceDecision.Free));
            Assert.That(state.GetToolCount("locate"), Is.Zero);
            Assert.That(state.HasUsedTool, Is.False);
        }

        [Test]
        public void DefaultIdleHint_PlaysHintAfterTwentySecondsAndPersistsOnlyAfterPlayback()
        {
            var state = new GameStateService(new GameStateData());
            var sink = new RecordingSink();
            var controller = new IdleToolHintController(
                state,
                new PropHighlightConfig(),
                sink);

            controller.Tick(19.999, true, false, false, false);
            Assert.That(sink.PlayCount, Is.Zero);
            Assert.That(state.HasPropHighlightShown, Is.False);

            controller.Tick(0.001, true, false, false, false);

            Assert.That(sink.PlayCount, Is.EqualTo(1));
            Assert.That(sink.LastPlayed, Is.EqualTo(GameToolKind.Hint));
            Assert.That(controller.IsPlaying, Is.True);
            Assert.That(state.HasPropHighlightShown, Is.True);
        }

        [Test]
        public void IdleHint_MissingAnimationDoesNotMarkLifetimeFlag()
        {
            var state = new GameStateService(new GameStateData());
            var sink = new RecordingSink { CanPlay = false };
            var controller = new IdleToolHintController(
                state,
                new PropHighlightConfig(),
                sink);

            controller.Tick(20.0, true, false, false, false);
            controller.Tick(1.0, true, false, false, false);

            Assert.That(sink.PlayCount, Is.EqualTo(2));
            Assert.That(controller.IsPlaying, Is.False);
            Assert.That(state.HasPropHighlightShown, Is.False);
        }

        [Test]
        public void NonRepeatableControl_IsBlockedAfterAnyToolWasConsumed()
        {
            var data = new GameStateData { ToolLocate = 1, ToolHint = 1 };
            var state = new GameStateService(data);
            state.SetToolCount("locate", 0);
            var config = new PropHighlightConfig();
            config.SetDebugOverride(PropHighlightConfig.ValueControl);
            var sink = new RecordingSink();
            var controller = new IdleToolHintController(state, config, sink);

            controller.Tick(30.0, true, false, false, false);

            Assert.That(sink.PlayCount, Is.Zero);
        }

        [Test]
        public void RepeatableIdleHint_UsesTwentyTenTwentyCadence()
        {
            var data = new GameStateData { ToolLocate = 1, ToolHint = 1 };
            var state = new GameStateService(data);
            var config = new PropHighlightConfig();
            config.SetDebugOverride(PropHighlightConfig.ValueControlRepeatable);
            var sink = new RecordingSink();
            var controller = new IdleToolHintController(
                state,
                config,
                sink,
                new FixedInclusiveRandom(0));

            controller.Tick(20.0, true, false, false, false);
            Assert.That(sink.PlayCount, Is.EqualTo(1));
            Assert.That(sink.LastPlayed, Is.EqualTo(GameToolKind.Hint));

            controller.Tick(10.0, true, false, false, false);
            Assert.That(sink.StopCount, Is.EqualTo(1));
            Assert.That(controller.IsPlaying, Is.False);

            controller.Tick(19.999, true, false, false, false);
            Assert.That(sink.PlayCount, Is.EqualTo(1));
            controller.Tick(0.001, true, false, false, false);

            Assert.That(sink.PlayCount, Is.EqualTo(2));
            Assert.That(controller.IsPlaying, Is.True);
        }

        [Test]
        public void IdleHint_GuardsDoNotAdvanceTimer()
        {
            var state = new GameStateService(new GameStateData());
            var sink = new RecordingSink();
            var controller = new IdleToolHintController(
                state,
                new PropHighlightConfig(),
                sink);

            controller.Tick(10.0, false, false, false, false);
            controller.Tick(10.0, true, true, false, false);
            controller.Tick(10.0, true, false, true, false);
            controller.Tick(10.0, true, false, false, true);

            Assert.That(controller.IdleSeconds, Is.Zero);
            Assert.That(sink.PlayCount, Is.Zero);
        }

        private sealed class RecordingSink : IIdleToolHintSink
        {
            public bool CanPlay { get; set; } = true;
            public int PlayCount { get; private set; }
            public int StopCount { get; private set; }
            public GameToolKind LastPlayed { get; private set; }

            public bool TryPlay(GameToolKind kind)
            {
                PlayCount++;
                LastPlayed = kind;
                return CanPlay;
            }

            public void Stop(GameToolKind kind)
            {
                StopCount++;
            }
        }

        private sealed class FixedInclusiveRandom : IInclusiveRandom
        {
            private readonly int _value;
            public FixedInclusiveRandom(int value) { _value = value; }
            public int RangeInclusive(int minimum, int maximum) => _value;
        }
    }
}
