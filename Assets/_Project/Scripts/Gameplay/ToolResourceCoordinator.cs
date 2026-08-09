using System;
using Meowdoku.Core;
using Meowdoku.Core.Config;

namespace Meowdoku.Gameplay
{
    public enum GameToolKind
    {
        Locate,
        Hint
    }

    public enum ToolResourceDecision
    {
        Rejected,
        NoAction,
        Available,
        Free,
        Consumed,
        RewardRequired,
        RewardCooldown
    }

    /// <summary>
    /// Resource-only port of BaseGamePage._consume_tool. Board effects remain
    /// in GameSession and external ad/award work remains behind the caller.
    /// </summary>
    public sealed class ToolResourceCoordinator
    {
        public const long RewardCooldownMilliseconds = 800;

        private readonly GameStateService _gameState;
        private readonly RewardUnlockLevelConfig _rewardUnlockConfig;
        private long _lastToolDepletedMilliseconds;

        public ToolResourceCoordinator(
            GameStateService gameState,
            RewardUnlockLevelConfig rewardUnlockConfig)
        {
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            _rewardUnlockConfig = rewardUnlockConfig ??
                                  throw new ArgumentNullException(nameof(rewardUnlockConfig));
        }

        public ToolResourceDecision Inspect(
            GameToolKind kind,
            int currentLevel,
            long nowMilliseconds)
        {
            if (!_rewardUnlockConfig.IsRewardRequiredAt(currentLevel))
                return ToolResourceDecision.Free;
            if (_gameState.GetToolCount(Key(kind)) > 0)
                return ToolResourceDecision.Available;
            return nowMilliseconds - _lastToolDepletedMilliseconds < RewardCooldownMilliseconds
                ? ToolResourceDecision.RewardCooldown
                : ToolResourceDecision.RewardRequired;
        }

        public ToolResourceDecision TryConsume(
            GameToolKind kind,
            int currentLevel,
            long nowMilliseconds)
        {
            ToolResourceDecision availability = Inspect(kind, currentLevel, nowMilliseconds);
            if (availability != ToolResourceDecision.Available) return availability;

            string key = Key(kind);
            int newCount = _gameState.GetToolCount(key) - 1;
            if (newCount == 0) _lastToolDepletedMilliseconds = nowMilliseconds;
            _gameState.SetToolCount(key, newCount);
            return ToolResourceDecision.Consumed;
        }

        public int GetCount(GameToolKind kind)
        {
            return _gameState.GetToolCount(Key(kind));
        }

        private static string Key(GameToolKind kind)
        {
            return kind == GameToolKind.Locate ? "locate" : "hint";
        }
    }

    public interface IIdleToolHintSink
    {
        bool TryPlay(GameToolKind kind);
        void Stop(GameToolKind kind);
    }

    public sealed class NullIdleToolHintSink : IIdleToolHintSink
    {
        public static readonly NullIdleToolHintSink Instance = new NullIdleToolHintSink();
        private NullIdleToolHintSink() { }
        public bool TryPlay(GameToolKind kind) => false;
        public void Stop(GameToolKind kind) { }
    }

    /// <summary>
    /// Timer/policy port of BaseGamePage's prop_highlight flow. The sink owns
    /// the actual ToolButton animation and is intentionally absent from domain.
    /// </summary>
    public sealed class IdleToolHintController
    {
        public const double IdleDelaySeconds = 20.0;
        public const double RepeatPlaySeconds = 10.0;

        private readonly GameStateService _gameState;
        private readonly PropHighlightConfig _config;
        private readonly IInclusiveRandom _random;
        private readonly IIdleToolHintSink _sink;
        private double _idleSeconds;
        private double _activePlaySeconds;
        private GameToolKind? _activeTool;

        public IdleToolHintController(
            GameStateService gameState,
            PropHighlightConfig config,
            IIdleToolHintSink sink = null,
            IInclusiveRandom random = null)
        {
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sink = sink ?? NullIdleToolHintSink.Instance;
            _random = random ?? UnityInclusiveRandom.Instance;
        }

        public bool IsPlaying => _activeTool.HasValue;
        public GameToolKind? ActiveTool => _activeTool;
        public double IdleSeconds => _idleSeconds;

        public void Tick(
            double deltaSeconds,
            bool visible,
            bool isComplete,
            bool wrongGuessPending,
            bool hintOverlayVisible)
        {
            if (deltaSeconds < 0.0) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (!CanShow(visible, isComplete, wrongGuessPending, hintOverlayVisible)) return;

            if (_activeTool.HasValue && _config.IsRepeatable())
            {
                _activePlaySeconds += deltaSeconds;
                if (_activePlaySeconds >= RepeatPlaySeconds)
                {
                    StopActive();
                    _idleSeconds = 0.0;
                    return;
                }
            }

            _idleSeconds += deltaSeconds;
            if (_idleSeconds < IdleDelaySeconds || _activeTool.HasValue) return;

            GameToolKind? selected = SelectTool();
            if (!selected.HasValue || !_sink.TryPlay(selected.Value)) return;

            _activeTool = selected;
            _activePlaySeconds = 0.0;
            _gameState.MarkPropHighlightShown();
        }

        public void Reset()
        {
            _idleSeconds = 0.0;
            StopActive();
        }

        public void ResetElapsed()
        {
            _idleSeconds = 0.0;
        }

        private bool CanShow(
            bool visible,
            bool isComplete,
            bool wrongGuessPending,
            bool hintOverlayVisible)
        {
            if (!visible || isComplete || wrongGuessPending || hintOverlayVisible)
                return false;
            if (_config.TargetProp() == "none") return false;
            if (_config.IsOncePerLifetime() && _gameState.HasPropHighlightShown)
                return false;
            if (!_config.IsRepeatable() && _gameState.HasUsedTool)
                return false;
            return true;
        }

        private GameToolKind? SelectTool()
        {
            switch (_config.TargetProp())
            {
                case "locate":
                    return GameToolKind.Locate;
                case "hint":
                    return GameToolKind.Hint;
                case "none":
                    return null;
                case "random":
                    bool hasLocate = _gameState.GetToolCount("locate") > 0;
                    bool hasHint = _gameState.GetToolCount("hint") > 0;
                    if (hasLocate == hasHint)
                        return _random.RangeInclusive(0, 1) == 0
                            ? GameToolKind.Hint
                            : GameToolKind.Locate;
                    return hasLocate ? GameToolKind.Locate : GameToolKind.Hint;
                default:
                    if (_gameState.GetToolCount("locate") > 0)
                        return GameToolKind.Locate;
                    if (_gameState.GetToolCount("hint") > 0)
                        return GameToolKind.Hint;
                    return GameToolKind.Locate;
            }
        }

        private void StopActive()
        {
            if (!_activeTool.HasValue) return;
            GameToolKind active = _activeTool.Value;
            _activeTool = null;
            _activePlaySeconds = 0.0;
            _sink.Stop(active);
        }
    }
}
