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
        [SerializeField] private RectTransform animatedContent;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private Image entryGlow;

        private readonly Tween[] _highlightTweens = new Tween[3];
        private Sequence _entrySequence;

        private void Awake()
        {
            HideHighlights();
            ResetEntryVisual();
        }

        private void OnEnable()
        {
            if (gameplayManager != null)
            {
                gameplayManager.GameplayFeedbackBatchRequested += HandleFeedback;
                gameplayManager.SessionLoadPreparing += HandleSessionLoadPreparing;
            }
        }

        private void OnDisable()
        {
            if (gameplayManager != null)
            {
                gameplayManager.GameplayFeedbackBatchRequested -= HandleFeedback;
                gameplayManager.SessionLoadPreparing -= HandleSessionLoadPreparing;
            }
            HideHighlights();
            StopEntryAnimation();
        }

        private void HandleSessionLoadPreparing(
            GameplaySessionMode mode,
            int level)
        {
            PlayEntry();
        }

        private void PlayEntry()
        {
            StopEntryAnimation();
            if (animatedContent == null || contentGroup == null || entryGlow == null)
                return;

            contentGroup.alpha = 0f;
            SetAlpha(entryGlow, 0f);
            animatedContent.localScale = Vector3.one;

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);
            _entrySequence = sequence;
            sequence.Insert(0.35959503f,
                contentGroup.DOFade(1f, 0.57867134f - 0.35959503f)
                    .SetEase(Ease.Linear));
            sequence.Insert(0.7516941f,
                entryGlow.DOFade(1f, 1.0141883f - 0.7516941f)
                    .SetEase(Ease.Linear));
            sequence.Insert(1.0141883f,
                entryGlow.DOFade(0.47058824f, 1.28392f - 1.0141883f)
                    .SetEase(Ease.Linear));
            sequence.Insert(1.28392f,
                entryGlow.DOFade(1f, 1.5511862f - 1.28392f)
                    .SetEase(Ease.Linear));
            sequence.Insert(1.5511862f,
                entryGlow.DOFade(0f, 1.816747f - 1.5511862f)
                    .SetEase(Ease.Linear));
            sequence.Insert(0.7537534f,
                animatedContent.DOScale(1.05f, 1.0148019f - 0.7537534f)
                    .SetEase(Ease.Linear));
            sequence.Insert(1.0148019f,
                animatedContent.DOScale(1f, 1.283933f - 1.0148019f)
                    .SetEase(Ease.Linear));
            sequence.Insert(1.283933f,
                animatedContent.DOScale(1.05f, 1.5501174f - 1.283933f)
                    .SetEase(Ease.Linear));
            sequence.Insert(1.5501174f,
                animatedContent.DOScale(1f, 1.8180395f - 1.5501174f)
                    .SetEase(Ease.Linear));
            sequence.OnComplete(() =>
            {
                if (_entrySequence != sequence) return;
                _entrySequence = null;
                ResetEntryVisual();
            });
        }

        private void StopEntryAnimation()
        {
            _entrySequence?.Kill(false);
            _entrySequence = null;
            ResetEntryVisual();
        }

        private void ResetEntryVisual()
        {
            if (contentGroup != null) contentGroup.alpha = 1f;
            if (entryGlow != null) SetAlpha(entryGlow, 0f);
            if (animatedContent != null) animatedContent.localScale = Vector3.one;
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

#if UNITY_INCLUDE_TESTS
        internal bool EntryAnimationActiveForTests =>
            _entrySequence != null && _entrySequence.IsActive();
        internal float EntryGlowAlphaForTests =>
            entryGlow != null ? entryGlow.color.a : 0f;
        internal float EntryScaleForTests =>
            animatedContent != null ? animatedContent.localScale.x : 1f;
#endif

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
