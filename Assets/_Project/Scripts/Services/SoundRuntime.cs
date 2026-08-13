using Meowdoku.Core.UI;
using UnityEngine;

namespace Meowdoku.Services
{
    public interface ISoundServiceConsumer
    {
        void BindSoundService(SoundService service);
    }

    /// <summary>
    /// Scene-owned binding boundary for the source SoundManager autoload.
    /// UI prefabs cannot serialize a scene reference, so each cached window
    /// receives the one App-scoped service when UIManager creates it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SoundService))]
    public sealed class SoundRuntime : MonoBehaviour
    {
        [SerializeField] private UIManager uiManager;
        [SerializeField] private SoundService soundService;

        private UIManager _subscribedManager;

        public SoundService Service => soundService;

        private void Awake()
        {
            if (soundService == null) soundService = GetComponent<SoundService>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void BindUIManager(UIManager manager)
        {
            if (uiManager == manager) return;
            Unsubscribe();
            uiManager = manager;
            if (isActiveAndEnabled) Subscribe();
        }

        private void Subscribe()
        {
            if (uiManager == null || _subscribedManager == uiManager) return;
            Unsubscribe();
            _subscribedManager = uiManager;
            _subscribedManager.Events.WindowCreated += HandleWindowCreated;
        }

        private void Unsubscribe()
        {
            if (_subscribedManager != null)
                _subscribedManager.Events.WindowCreated -= HandleWindowCreated;
            _subscribedManager = null;
        }

        private void HandleWindowCreated(UiName _, UIFrameWindow window)
        {
            if (window is ISoundServiceConsumer consumer)
                consumer.BindSoundService(soundService);
        }
    }
}
