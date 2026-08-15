using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Services
{
    /// <summary>
    /// Runtime binding added once when UIManager creates a cached window.
    /// This keeps button audio complete without scene lookups or per-page
    /// boilerplate, including buttons added by later portfolio pages.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonClickSoundEmitter : MonoBehaviour
    {
        private Button _button;
        private SoundService _soundService;
        private bool _subscribed;

        public void Bind(SoundService soundService)
        {
            _soundService = soundService;
            if (_button == null) _button = GetComponent<Button>();
            if (_button == null || _subscribed) return;
            _button.onClick.AddListener(PlayClick);
            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (_subscribed && _button != null)
                _button.onClick.RemoveListener(PlayClick);
            _subscribed = false;
            _soundService = null;
        }

        private void PlayClick()
        {
            _soundService?.Play(SoundKind.ButtonClick);
        }
    }
}
