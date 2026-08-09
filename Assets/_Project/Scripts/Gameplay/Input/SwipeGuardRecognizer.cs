using System;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using UnityEngine;

namespace Meowdoku.Gameplay.Input
{
    /// <summary>
    /// Unity event adapter for game/input/swipe_guard_recognizer.gd.
    /// Input positions use board-local pixels; resolved cells use x=column, y=row.
    /// </summary>
    public sealed class SwipeGuardRecognizer
    {
        private readonly BoardGestureRecognizer _inner;
        private readonly SwipeProtectConfig _config;
        private readonly Func<Vector2, Vector2Int> _resolveRawCell;
        private readonly SwipeAxisGuard _guard = new SwipeAxisGuard();
        private readonly SwipeVelocityGate _gate = new SwipeVelocityGate();

        private int _puzzleSize;
        private int _slotPixels;
        private int _paddingPixels;
        private int _cellPixels;
        private bool _dynamic;

        public SwipeGuardRecognizer(
            BoardGestureRecognizer inner,
            SwipeProtectConfig config,
            Func<Vector2, Vector2Int> resolveRawCell)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _resolveRawCell = resolveRawCell ?? throw new ArgumentNullException(nameof(resolveRawCell));
        }

        public void ConfigureBoard(
            int puzzleSize,
            int slotPixels,
            int paddingPixels,
            int cellPixels)
        {
            _puzzleSize = puzzleSize;
            _slotPixels = Math.Max(1, slotPixels);
            _paddingPixels = Math.Max(0, paddingPixels);
            _cellPixels = Math.Max(1, cellPixels);
        }

        public List<CellAction> OnDragStart(Vector2 position, int nowMilliseconds)
        {
            return OnDragStart(position, _resolveRawCell(position), nowMilliseconds);
        }

        public List<CellAction> OnDragStart(
            Vector2 position,
            Vector2Int authoritativeStart,
            int nowMilliseconds)
        {
            Vector2Int start = authoritativeStart;
            _guard.Begin(
                _puzzleSize,
                _slotPixels,
                _paddingPixels,
                _cellPixels,
                start);
            _gate.Reset();

            List<CellAction> actions = _inner.OnDragStart(
                start.y,
                start.x,
                nowMilliseconds / 1000f);
            ConfigureGuard();
            if (_dynamic)
                _gate.AddSample(position.x, position.y, nowMilliseconds);
            return actions;
        }

        public List<CellAction> OnDragOver(Vector2 position, int nowMilliseconds)
        {
            bool wasPending = _inner.TargetPending;
            Vector2Int cell = GuardEnabledForLevel()
                ? _guard.Process(position.x, position.y)
                : _resolveRawCell(position);
            List<CellAction> actions = _inner.OnDragOver(cell.y, cell.x);

            if (wasPending && !_inner.TargetPending)
                _guard.SetActive(GuardActiveNow());

            if (_dynamic)
            {
                _gate.AddSample(position.x, position.y, nowMilliseconds);
                _guard.SetActive(GuardActiveNow() && _gate.IsFast());
            }
            return actions;
        }

        public void OnDragEnd()
        {
            _inner.OnDragEnd();
            _guard.End();
            _gate.Reset();
        }

        public void OnDragTick(Vector2 position, int nowMilliseconds)
        {
            if (!_dynamic) return;
            _gate.AddSample(position.x, position.y, nowMilliseconds);
            _guard.SetActive(GuardActiveNow() && _gate.IsFast());
            _guard.Process(position.x, position.y);
        }

        public void Reset()
        {
            _inner.Reset();
            _guard.End();
            _gate.Reset();
        }

        private bool GuardEnabledForLevel()
        {
            return _config.IsEnabled() && _puzzleSize >= _config.MinSize();
        }

        private bool GuardActiveNow()
        {
            return GuardEnabledForLevel() &&
                   !_inner.TargetPending &&
                   _inner.TargetState != CellStateType.EMPTY;
        }

        private void ConfigureGuard()
        {
            int threshold = _config.ThresholdFor(_puzzleSize);
            float tolerancePixels = (float)(_config.TolerancePercent() * _cellPixels);
            _guard.Configure(GuardActiveNow(), threshold, tolerancePixels);

            _dynamic = _config.IsDynamicIntent() &&
                       GuardEnabledForLevel() &&
                       (_inner.TargetPending || _inner.TargetState != CellStateType.EMPTY);
            if (_dynamic)
            {
                _gate.Configure(
                    SwipeProtectConfig.DynamicWindowMilliseconds,
                    SwipeProtectConfig.DynamicVelocityThresholdPixelsPerMillisecond);
            }
        }
    }
}
