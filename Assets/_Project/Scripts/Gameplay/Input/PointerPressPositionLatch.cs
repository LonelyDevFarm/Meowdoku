using UnityEngine;

namespace Meowdoku.Gameplay.Input
{
    /// <summary>
    /// Preserves the pointer position at the physical press callback. Unity's
    /// UI module otherwise processes buttons after later point callbacks in
    /// the same frame and can raycast using the newer cursor position.
    /// </summary>
    public sealed class PointerPressPositionLatch
    {
        private Vector2 _latestPosition;
        private Vector2 _pressPosition;
        private int _latestDeviceId = -1;
        private int _pressDeviceId = -1;
        private bool _hasLatestPosition;
        private bool _hasPressPosition;
        private bool _pressed;

        public void RecordPosition(Vector2 position, int deviceId)
        {
            _latestPosition = position;
            _latestDeviceId = deviceId;
            _hasLatestPosition = true;
        }

        public void RecordButton(bool pressed, int deviceId)
        {
            if (pressed && !_pressed && _hasLatestPosition &&
                !_hasPressPosition &&
                (_latestDeviceId < 0 || deviceId < 0 || _latestDeviceId == deviceId))
            {
                _pressPosition = _latestPosition;
                _pressDeviceId = deviceId;
                _hasPressPosition = true;
            }
            _pressed = pressed;
        }

        public void RecordPressPosition(Vector2 position, int deviceId)
        {
            _pressPosition = position;
            _pressDeviceId = deviceId;
            _hasPressPosition = true;
        }

        public bool TryConsume(int deviceId, out Vector2 position)
        {
            position = default;
            if (!_hasPressPosition ||
                (_pressDeviceId >= 0 && deviceId >= 0 && _pressDeviceId != deviceId))
                return false;
            position = _pressPosition;
            _hasPressPosition = false;
            return true;
        }

        public void Reset()
        {
            _pressed = false;
            _hasPressPosition = false;
        }
    }
}
