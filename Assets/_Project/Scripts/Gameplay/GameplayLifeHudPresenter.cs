using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    /// <summary>Owns the three source-ordered life slots and their presentation.</summary>
    public sealed class GameplayLifeHudPresenter : MonoBehaviour
    {
        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private GameplayLifeSlotView[] slots = new GameplayLifeSlotView[3];

        private int _generation;

        private void OnEnable()
        {
            if (gameplayManager != null)
                gameplayManager.GameplayFeedbackBatchRequested += HandleFeedbackBatch;
        }

        private void OnDisable()
        {
            if (gameplayManager != null)
                gameplayManager.GameplayFeedbackBatchRequested -= HandleFeedbackBatch;
            _generation++;
        }

        public void ResetLives(int lives)
        {
            _generation++;
            for (int index = 0; index < slots.Length; index++)
            {
                GameplayLifeSlotView slot = slots[index];
                if (slot == null) continue;
                if (index < lives) slot.ShowAlive();
                else slot.ShowLost(false);
            }
        }

        public void PlayRevive(int lives, bool animateAll)
        {
            ResetLives(lives);
            if (!animateAll) return;
            int count = Mathf.Min(lives, slots.Length);
            for (int index = 0; index < count; index++)
                slots[index]?.PlayRevive();
        }

        public bool TryGetSlotCenter(
            RectTransform targetSpace,
            int index,
            out Vector2 anchoredPosition)
        {
            anchoredPosition = Vector2.zero;
            if (targetSpace == null || index < 0 || index >= slots.Length || slots[index] == null)
                return false;
            RectTransform slotRect = slots[index].transform as RectTransform;
            if (slotRect == null) return false;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(
                null,
                slotRect.TransformPoint(slotRect.rect.center));
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetSpace,
                screen,
                null,
                out anchoredPosition);
        }

        private void HandleFeedbackBatch(IReadOnlyList<GameplayFeedbackData> feedback)
        {
            if (feedback == null) return;
            GameplayFeedbackPresentationPlan plan =
                GameplayFeedbackPresentationPlan.Build(feedback);
            int generation = _generation;
            for (int index = 0; index < feedback.Count; index++)
            {
                GameplayFeedbackData item = feedback[index];
                if (item == null) continue;
                if (item.Kind == GameplayFeedbackKind.WrongGuess)
                {
                    int lostIndex = item.LivesBefore - 1;
                    if (lostIndex >= 0 && lostIndex < slots.Length)
                        slots[lostIndex]?.ShowLost(true);
                    continue;
                }
                if (item.Kind != GameplayFeedbackKind.LifeBonus) continue;
                float delay = plan.CorrectFlyLaunchDelay + item.LifeSlotIndex *
                    GameplayFeedbackPresentationPlan.LifeSequenceGapSeconds;
                int slotIndex = item.LifeSlotIndex;
                DOVirtual.DelayedCall(delay, () =>
                    {
                        if (generation != _generation || slotIndex < 0 || slotIndex >= slots.Length)
                            return;
                        slots[slotIndex]?.ShowLost(true, true);
                    }, true)
                    .SetLink(gameObject);
            }
        }
    }
}
