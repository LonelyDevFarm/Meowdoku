using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Unity presenter for rule_info_bar_v0. The offline source profile keeps
    /// rule_highlight disabled; the pulse remains available for the source AB variant.
    /// </summary>
    public sealed class GameplayRuleBarPresenter : MonoBehaviour
    {
        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private Image[] highlights = new Image[3];

        private readonly Tween[] _highlightTweens = new Tween[3];
        private void Awake()
        {
            HideHighlights();
        }

        private void OnEnable()
        {
            if (gameplayManager != null)
                gameplayManager.GameplayFeedbackBatchRequested += HandleFeedback;
        }

        private void OnDisable()
        {
            if (gameplayManager != null)
                gameplayManager.GameplayFeedbackBatchRequested -= HandleFeedback;
            HideHighlights();
        }

        private void HandleFeedback(IReadOnlyList<GameplayFeedbackData> feedback)
        {
            if (gameplayManager == null ||
                !gameplayManager.ShouldHighlightRuleViolation() ||
                feedback == null)
                return;
            for (int index = 0; index < feedback.Count; index++)
            {
                GameplayFeedbackData item = feedback[index];
                if (item != null && item.Kind == GameplayFeedbackKind.WrongGuess)
                {
                    PlayViolation(item.RuleViolation);
                    return;
                }
            }
        }

        public void PlayViolation(QueendokuCore.Rule rule)
        {
            int index = (int)rule - 1;
            if (index < 0 || index >= highlights.Length || highlights[index] == null) return;
            _highlightTweens[index]?.Kill(false);
            Image image = highlights[index];
            image.gameObject.SetActive(true);
            _highlightTweens[index] = DOVirtual.Float(0f, 1f, 0.6f, progress =>
                SetAlpha(image, 0.4f + 0.6f * Mathf.Sin(progress * Mathf.PI)))
                .SetEase(Ease.Linear)
                .SetLoops(2, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    SetAlpha(image, 0f);
                    image.gameObject.SetActive(false);
                });
        }

        internal bool IsHighlightVisibleForTests(QueendokuCore.Rule rule)
        {
            int index = (int)rule - 1;
            return index >= 0 &&
                   index < highlights.Length &&
                   highlights[index] != null &&
                   highlights[index].gameObject.activeSelf;
        }

        private void HideHighlights()
        {
            for (int index = 0; index < highlights.Length; index++)
            {
                _highlightTweens[index]?.Kill(false);
                _highlightTweens[index] = null;
                if (highlights[index] == null) continue;
                SetAlpha(highlights[index], 0f);
                highlights[index].gameObject.SetActive(false);
            }
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
