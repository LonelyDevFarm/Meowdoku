using System;
using System.Collections.Generic;
using Meowdoku.Core;
using UnityEngine;

namespace Meowdoku.Gameplay.Input
{
    public sealed class BoardGestureRecognizer
    {
        public const float DefaultDoubleTapSeconds = 0.35f;

        private static readonly Vector2Int InvalidCell = new Vector2Int(-1, -1);
        private readonly BoardStrokeContext _stroke = new BoardStrokeContext();
        private readonly BoardInputScheme _scheme;
        private readonly Func<int, int, float> _windowSecondsProvider;
        private Vector2Int _lastTapCell = InvalidCell;
        private float _lastTapExpiresAt;

        public BoardGestureRecognizer(
            BoardInputScheme scheme,
            Func<int, int, float> windowSecondsProvider = null)
        {
            _scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
            _windowSecondsProvider = windowSecondsProvider;
        }

        public bool TargetPending => _stroke.TargetPending;
        public CellStateType TargetState => _stroke.TargetState;

        public List<CellAction> OnDragStart(int row, int column, float now)
        {
            // BaseGamePage rejects a gesture before it reaches the recognizer
            // when its authoritative start cell is ERROR.
            if (_scheme.IsTerminalError(row, column))
            {
                _stroke.Reset();
                return new List<CellAction>();
            }
            var cell = new Vector2Int(row, column);
            if (_lastTapCell == cell && now <= _lastTapExpiresAt)
            {
                CloseDoubleTapWindow();
                _stroke.Reset();
                return _scheme.DoubleTap.OnDoubleTap(row, column);
            }

            _stroke.Reset();
            _stroke.StartCell = cell;
            _stroke.LastCell = cell;
            List<CellAction> actions = _scheme.Tap.OnTap(row, column, _stroke);
            if (actions.Count > 0)
            {
                _lastTapCell = cell;
                float window = _windowSecondsProvider != null
                    ? _windowSecondsProvider(row, column)
                    : DefaultDoubleTapSeconds;
                _lastTapExpiresAt = now + window;
            }
            return actions;
        }

        public List<CellAction> OnDragOver(int row, int column)
        {
            var result = new List<CellAction>();
            if (!_stroke.IsActive || row < 0 || column < 0) return result;

            var cell = new Vector2Int(row, column);
            if (cell == _stroke.LastCell) return result;

            Vector2Int last = _stroke.LastCell;
            int deltaRow = row - last.x;
            int deltaColumn = column - last.y;
            int steps = Math.Max(Math.Abs(deltaRow), Math.Abs(deltaColumn));
            for (int i = 1; i < steps; i++)
            {
                int middleRow = last.x + Mathf.RoundToInt((float)deltaRow * i / steps);
                int middleColumn = last.y + Mathf.RoundToInt((float)deltaColumn * i / steps);
                CellAction middle = _scheme.Swipe.OnPaint(middleRow, middleColumn, _stroke, false);
                if (middle != null) result.Add(middle);
            }

            _stroke.LastCell = cell;
            _stroke.HadMove = true;
            CellAction current = _scheme.Swipe.OnPaint(row, column, _stroke, true);
            if (current != null) result.Add(current);
            return result;
        }

        public void OnDragEnd() { _stroke.Reset(); }

        public void Reset()
        {
            CloseDoubleTapWindow();
            _stroke.Reset();
        }

        private void CloseDoubleTapWindow()
        {
            _lastTapCell = InvalidCell;
            _lastTapExpiresAt = 0f;
        }
    }
}
