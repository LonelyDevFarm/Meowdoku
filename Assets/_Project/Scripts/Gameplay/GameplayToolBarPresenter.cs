using Meowdoku.Core;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Presentation adapter for BaseGamePage BottomTools. The source ClearBtn
    /// is hidden in the default Main Game profile, so this presenter exposes
    /// only Reveal and Hint.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayToolBarPresenter : MonoBehaviour
    {
        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private ToolButtonView locateButton;
        [SerializeField] private ToolButtonView hintButton;

        public ToolButtonView LocateButtonForTests => locateButton;
        public ToolButtonView HintButtonForTests => hintButton;

        private void OnEnable()
        {
            if (locateButton != null) locateButton.Pressed += HandleLocatePressed;
            if (hintButton != null) hintButton.Pressed += HandleHintPressed;
            if (gameplayManager != null)
            {
                gameplayManager.IdleToolHintPlayRequested += HandleIdleHintPlay;
                gameplayManager.IdleToolHintStopRequested += HandleIdleHintStop;
                gameplayManager.ToolPresentationChanged += RefreshAll;
            }
            GameStateRuntime.Current.ToolCountChanged += HandleToolCountChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (locateButton != null)
            {
                locateButton.Pressed -= HandleLocatePressed;
                locateButton.StopIdlePulse();
            }
            if (hintButton != null)
            {
                hintButton.Pressed -= HandleHintPressed;
                hintButton.StopIdlePulse();
            }
            if (gameplayManager != null)
            {
                gameplayManager.IdleToolHintPlayRequested -= HandleIdleHintPlay;
                gameplayManager.IdleToolHintStopRequested -= HandleIdleHintStop;
                gameplayManager.ToolPresentationChanged -= RefreshAll;
            }
            GameStateRuntime.Current.ToolCountChanged -= HandleToolCountChanged;
        }

        private void HandleLocatePressed()
        {
            gameplayManager?.TryUseLocate(out _);
        }

        private void HandleHintPressed()
        {
            gameplayManager?.TryUseHint(
                gameplayManager.IsSpecialBankSession,
                out _);
        }

        private void HandleToolCountChanged(string kind, int _)
        {
            if (kind == "locate") Refresh(locateButton, GameToolKind.Locate);
            else if (kind == "hint") Refresh(hintButton, GameToolKind.Hint);
        }

        private bool HandleIdleHintPlay(GameToolKind kind)
        {
            return View(kind)?.PlayIdlePulse() == true;
        }

        private void HandleIdleHintStop(GameToolKind kind)
        {
            View(kind)?.StopIdlePulse();
        }

        private void RefreshAll()
        {
            Refresh(locateButton, GameToolKind.Locate);
            Refresh(hintButton, GameToolKind.Hint);
        }

        private void Refresh(ToolButtonView view, GameToolKind kind)
        {
            if (view == null) return;
            int count = GameStateRuntime.Current.GetToolCount(
                kind == GameToolKind.Locate ? "locate" : "hint");
            ToolButtonVisualState state = gameplayManager != null &&
                                          gameplayManager.IsToolFree(kind)
                ? ToolButtonVisualState.Free
                : count > 0
                    ? ToolButtonVisualState.HasTool
                    : ToolButtonVisualState.NoTool;
            view.SetState(state, count);
        }

        private ToolButtonView View(GameToolKind kind)
        {
            return kind == GameToolKind.Locate ? locateButton : hintButton;
        }
    }
}
