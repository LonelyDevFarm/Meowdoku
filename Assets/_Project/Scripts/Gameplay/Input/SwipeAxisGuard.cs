using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Gameplay.Input
{
    /// <summary>
    /// Direct logic port of gameplay/core/swipe_axis_guard.gd.
    /// Vector2Int.x is column and Vector2Int.y is row, matching the source.
    /// </summary>
    public sealed class SwipeAxisGuard
    {
        public enum Axis
        {
            None = 0,
            Row = 1,
            Column = 2
        }

        private static readonly Vector2Int InvalidCell = new Vector2Int(-1, -1);

        private int _size;
        private int _slot;
        private int _padding;

        private bool _active;
        private int _threshold = 4;
        private float _tolerancePixels;

        private Axis _axis = Axis.None;
        private int _lockValue = -1;
        private Axis _runAxis = Axis.None;
        private int _runValue = -1;
        private int _runCount;
        private int _runMin;
        private int _runMax;
        private Vector2Int _lastCell = InvalidCell;

        public void Begin(
            int puzzleSize,
            int slotPixels,
            int padding,
            int cellPixels,
            Vector2Int startCell)
        {
            _size = puzzleSize;
            _slot = slotPixels;
            _padding = padding;
            _ = cellPixels;
            _axis = Axis.None;
            _lockValue = -1;
            _runAxis = Axis.None;
            _runValue = -1;
            _lastCell = startCell;
            _runCount = startCell.x >= 0 ? 1 : 0;
            _active = false;
        }

        public void Configure(bool active, int threshold, float tolerancePixels)
        {
            _active = active;
            _threshold = Mathf.Max(2, threshold);
            _tolerancePixels = tolerancePixels;
        }

        public void SetActive(bool active)
        {
            _active = active;
        }

        public void End()
        {
            _axis = Axis.None;
            _lockValue = -1;
            _runAxis = Axis.None;
            _runCount = 0;
            _lastCell = InvalidCell;
        }

        public Dictionary<string, object> GetDebugLock()
        {
            if (_axis == Axis.None)
            {
                return new Dictionary<string, object>();
            }

            return new Dictionary<string, object>
            {
                { "axis", _axis },
                { "value", _lockValue },
                { "tol", _tolerancePixels }
            };
        }

        public Vector2Int Process(float pixelX, float pixelY)
        {
            if (_axis != Axis.None)
            {
                return ProcessLocked(pixelX, pixelY);
            }

            Vector2Int nextCell = RawCell(pixelX, pixelY);
            if (nextCell.x < 0)
            {
                return nextCell;
            }

            if (nextCell != _lastCell)
            {
                AdvanceRun(nextCell);
                _lastCell = nextCell;
                if (_active && _runCount >= _threshold && _runAxis != Axis.None)
                {
                    _axis = _runAxis;
                    _lockValue = _runValue;
                }
            }

            return nextCell;
        }

        private Vector2Int RawCell(float pixelX, float pixelY)
        {
            if (_size == 0)
            {
                return InvalidCell;
            }

            int column = (int)((pixelX - _padding) / _slot);
            int row = (int)((pixelY - _padding) / _slot);
            if (row < 0 || row >= _size || column < 0 || column >= _size)
            {
                return InvalidCell;
            }

            return new Vector2Int(column, row);
        }

        private void AdvanceRun(Vector2Int nextCell)
        {
            Vector2Int last = _lastCell;
            if (last.x < 0)
            {
                _runAxis = Axis.None;
                _runValue = -1;
                _runCount = 1;
                return;
            }

            bool sameRow = nextCell.y == last.y && nextCell.x != last.x;
            bool sameColumn = nextCell.x == last.x && nextCell.y != last.y;
            Axis stepAxis = Axis.None;
            int stepValue = -1;
            if (sameRow)
            {
                stepAxis = Axis.Row;
                stepValue = nextCell.y;
            }
            else if (sameColumn)
            {
                stepAxis = Axis.Column;
                stepValue = nextCell.x;
            }

            if (stepAxis == Axis.None)
            {
                _runAxis = Axis.None;
                _runValue = -1;
                _runCount = 1;
                return;
            }

            int nextIndex = stepAxis == Axis.Row ? nextCell.x : nextCell.y;
            if (stepAxis == _runAxis && stepValue == _runValue)
            {
                _runMin = Mathf.Min(_runMin, nextIndex);
                _runMax = Mathf.Max(_runMax, nextIndex);
                _runCount = _runMax - _runMin + 1;
            }
            else
            {
                _runAxis = stepAxis;
                _runValue = stepValue;
                int lastIndex = stepAxis == Axis.Row ? last.x : last.y;
                _runMin = Mathf.Min(lastIndex, nextIndex);
                _runMax = Mathf.Max(lastIndex, nextIndex);
                _runCount = _runMax - _runMin + 1;
            }
        }

        private Vector2Int ProcessLocked(float pixelX, float pixelY)
        {
            if (!_active)
            {
                return Release(pixelX, pixelY);
            }

            if (_axis == Axis.Row)
            {
                if (Overshoot1D(pixelY, _lockValue) > _tolerancePixels)
                {
                    return Release(pixelX, pixelY);
                }

                int column = Mathf.Clamp(
                    (int)((pixelX - _padding) / _slot),
                    0,
                    _size - 1);
                return new Vector2Int(column, _lockValue);
            }

            if (Overshoot1D(pixelX, _lockValue) > _tolerancePixels)
            {
                return Release(pixelX, pixelY);
            }

            int row = Mathf.Clamp(
                (int)((pixelY - _padding) / _slot),
                0,
                _size - 1);
            return new Vector2Int(_lockValue, row);
        }

        private float Overshoot1D(float value, int index)
        {
            float low = _padding + index * _slot;
            float high = _padding + (index + 1) * _slot;
            if (value < low)
            {
                return low - value;
            }

            if (value > high)
            {
                return value - high;
            }

            return 0f;
        }

        private Vector2Int Release(float pixelX, float pixelY)
        {
            Axis previousAxis = _axis;
            int previousLock = _lockValue;
            _axis = Axis.None;
            _lockValue = -1;
            Vector2Int nextCell = RawCell(pixelX, pixelY);
            _lastCell = nextCell;

            if (nextCell.x >= 0 && previousAxis != Axis.None)
            {
                if (previousAxis == Axis.Row)
                {
                    _runAxis = Axis.Column;
                    _runValue = nextCell.x;
                    _runMin = Mathf.Min(previousLock, nextCell.y);
                    _runMax = Mathf.Max(previousLock, nextCell.y);
                }
                else
                {
                    _runAxis = Axis.Row;
                    _runValue = nextCell.y;
                    _runMin = Mathf.Min(previousLock, nextCell.x);
                    _runMax = Mathf.Max(previousLock, nextCell.x);
                }

                _runCount = _runMax - _runMin + 1;
            }
            else
            {
                _runAxis = Axis.None;
                _runValue = -1;
                _runCount = 1;
            }

            return nextCell;
        }
    }
}
