using UnityEngine;
using UnityEngine.EventSystems;

namespace Meowdoku.Core.UI
{
    /// <summary>
    /// Keeps the press that opened a window owned by the old window until the
    /// release frame ends. This is the UGUI equivalent of Godot's held-button
    /// guard and prevents a release from reaching newly opened UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIButtonPressGuard : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler
    {
        private UIManager _manager;
        private bool _held;

        internal void Bind(UIManager manager)
        {
            _manager = manager;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_manager == null || _held) return;
            _held = true;
            _manager.NotifyButtonHeld(GetInstanceID());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Release()
        {
            if (!_held) return;
            _held = false;
            if (_manager != null)
                _manager.NotifyButtonReleased(GetInstanceID());
        }
    }
}
