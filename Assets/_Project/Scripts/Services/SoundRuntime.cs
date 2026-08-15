using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

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
            _subscribedManager.Events.WindowShown += HandleWindowShown;
        }

        private void Unsubscribe()
        {
            if (_subscribedManager != null)
            {
                _subscribedManager.Events.WindowCreated -= HandleWindowCreated;
                _subscribedManager.Events.WindowShown -= HandleWindowShown;
            }
            _subscribedManager = null;
        }

        private void HandleWindowCreated(UiName _, UIFrameWindow window)
        {
            if (window is ISoundServiceConsumer consumer)
                consumer.BindSoundService(soundService);

            Button[] buttons = window.GetComponentsInChildren<Button>(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                Button button = buttons[index];
                if (button == null) continue;
                ButtonClickSoundEmitter emitter =
                    button.GetComponent<ButtonClickSoundEmitter>();
                if (emitter == null)
                    emitter = button.gameObject.AddComponent<
                        ButtonClickSoundEmitter>();
                emitter.Bind(soundService);
            }
        }

        private void HandleWindowShown(UiName _, UIFrameWindow window)
        {
            if (window != null && window.PlayOpenSound)
                soundService?.Play(SoundKind.DialogOpen);
        }
    }
}
