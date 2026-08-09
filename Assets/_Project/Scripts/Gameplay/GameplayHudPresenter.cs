using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>Header level and CatHeartRow progress from the source game page.</summary>
    public sealed class GameplayHudPresenter : MonoBehaviour
    {
        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private Text levelValue;
        [SerializeField] private Text catCountLabel;
        [SerializeField] private RectTransform catTarget;

        private Tween _progressTween;
        private int _lastPlaced = -1;

        private void OnEnable()
        {
            if (gameplayManager != null)
                gameplayManager.GameplayHudStateChanged += HandleHudState;
        }

        private void OnDisable()
        {
            if (gameplayManager != null)
                gameplayManager.GameplayHudStateChanged -= HandleHudState;
            _progressTween?.Kill(false);
            _progressTween = null;
            if (catTarget != null) catTarget.localScale = Vector3.one;
            _lastPlaced = -1;
        }

        private void HandleHudState(GameplayHudState state)
        {
            if (levelValue != null) levelValue.text = state.Level.ToString();
            if (catCountLabel != null)
            {
                catCountLabel.supportRichText = true;
                catCountLabel.text = $"<color=#00B31B>{state.PlacedCats}</color>" +
                                     $"<color=#935A5A>/{state.TotalCats}</color>";
            }

            if (_lastPlaced >= 0 && _lastPlaced != state.PlacedCats)
                PlayProgressPulse();
            _lastPlaced = state.PlacedCats;
        }

        private void PlayProgressPulse()
        {
            if (catTarget == null) return;
            _progressTween?.Kill(false);
            catTarget.localScale = Vector3.one;
            _progressTween = DOTween.Sequence()
                .Append(catTarget.DOScale(1.1f, 0.3f).SetEase(Ease.Linear))
                .Append(catTarget.DOScale(1f, 0.3f).SetEase(Ease.Linear))
                .SetUpdate(true)
                .SetLink(gameObject);
        }
    }
}
