namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Log-free port of hint_mutex.gd. GameSession uses it as the Unity-side
    /// equivalent of the Godot hint overlay input guard.
    /// </summary>
    public sealed class HintMutex
    {
        private string _activeId = string.Empty;

        public bool TryAcquire(string hintId)
        {
            if (!string.IsNullOrEmpty(_activeId) || string.IsNullOrEmpty(hintId)) return false;
            _activeId = hintId;
            return true;
        }

        public void Release(string hintId)
        {
            if (_activeId == hintId) _activeId = string.Empty;
        }

        public bool IsActive => !string.IsNullOrEmpty(_activeId);
        public string ActiveId => _activeId;
    }
}
