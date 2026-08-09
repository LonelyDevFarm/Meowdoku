using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core.Config;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Serialized, pooled Unity presenter for ComboFeedbackView P0: score header,
    /// bitmap score/deduction bubbles and combo encourage art.
    /// </summary>
    public sealed class GameplayFeedbackPresenter : MonoBehaviour
    {
        private const float BubbleHeight = 83f;
        private const float BubbleGap = 10f;
        private const float CatTopUnscaled = 50f;

        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private BoardView boardView;
        [SerializeField] private RectTransform feedbackArea;
        [SerializeField] private Text scoreValue;
        [SerializeField] private GameplayLifeHudPresenter lifeHudPresenter;
        [SerializeField] private SoundService soundService;
        [SerializeField] private Image encourageImage;
        [SerializeField] private Sprite[] encourageSprites = new Sprite[6];
        [SerializeField] private GameplayFeedbackBubbleView[] scoreBubbles;
        [SerializeField] private GameplayFeedbackBubbleView[] deductionBubbles;
        [SerializeField] private GameplayFeedbackBubbleView[] skillBubbles;
        [SerializeField] private GameplayMultiplierView[] multiplierViews;
        [SerializeField] private GameplayScoreFlightView[] scoreFlights;

        private int _displayedScore;
        private int _scoreCursor;
        private int _deductionCursor;
        private int _skillCursor;
        private int _multiplierCursor;
        private int _flightCursor;
        private Tween _scoreRoll;
        private Sequence _scoreBounce;
        private Sequence _encourageSequence;
        private int _generation;
        private ComboVoiceConfig _comboVoiceConfig;

        private void Awake()
        {
            _comboVoiceConfig = new ComboVoiceConfig();
        }

        private void OnEnable()
        {
            if (gameplayManager == null) return;
            gameplayManager.GameplayFeedbackBatchRequested += HandleFeedbackBatch;
        }

        private void OnDisable()
        {
            if (gameplayManager != null)
                gameplayManager.GameplayFeedbackBatchRequested -= HandleFeedbackBatch;
            ResetPresenter(0);
        }

        public void ResetPresenter(int score)
        {
            _generation++;
            _scoreRoll?.Kill(false);
            _scoreRoll = null;
            _scoreBounce?.Kill(false);
            _scoreBounce = null;
            _encourageSequence?.Kill(false);
            _encourageSequence = null;
            if (encourageImage != null) encourageImage.gameObject.SetActive(false);
            StopPool(scoreBubbles);
            StopPool(deductionBubbles);
            StopPool(skillBubbles);
            StopPool(multiplierViews);
            StopPool(scoreFlights);
            _displayedScore = Mathf.Max(0, score);
            SetScoreText(_displayedScore);
        }

        private void HandleFeedbackBatch(IReadOnlyList<GameplayFeedbackData> feedback)
        {
            GameplayFeedbackPresentationPlan plan =
                GameplayFeedbackPresentationPlan.Build(feedback);
            if (plan.CompletionDelay > 0f)
                gameplayManager.DelayWinSettlement(plan.CompletionDelay);

            float lifeOffset = plan.CorrectFlyLaunchDelay;
            int lifeIndex = 0;
            for (int index = 0; index < feedback.Count; index++)
            {
                GameplayFeedbackData item = feedback[index];
                if (item == null) continue;
                if (item.Kind == GameplayFeedbackKind.CorrectCat)
                    PresentCorrect(item);
                else if (item.Kind == GameplayFeedbackKind.WrongGuess)
                    PresentWrong(item);
                else if (item.Kind == GameplayFeedbackKind.LifeBonus)
                {
                    PresentLifeBonus(item, lifeOffset + lifeIndex *
                        GameplayFeedbackPresentationPlan.LifeSequenceGapSeconds);
                    lifeIndex++;
                }
            }
        }

        private void PresentCorrect(GameplayFeedbackData item)
        {
            if (item.ShowsComboText)
            {
                PlayEncourage(item.ComboCount);
                soundService?.PlayComboVoiceByPath(
                    _comboVoiceConfig.GetComboVoice(item.ComboCount));
            }
            Vector2 position = CellBubblePosition(item.Position);
            GameplayFeedbackBubbleView scoreBubble =
                Acquire(scoreBubbles, ref _scoreCursor);
            scoreBubble?.Play(
                item.DisplayGain,
                position,
                GameplayFeedbackPresentationPlan.BubbleDurationSeconds);

            if (item.ShowsMultiplier)
            {
                GameplayMultiplierView multiplier =
                    Acquire(multiplierViews, ref _multiplierCursor);
                multiplier?.Play(
                    item.Multiplier,
                    item.PreviousMultiplier,
                    item.UsesScrollMultiplierAnimation,
                    position);
                LayoutPair(position, scoreBubble, multiplier);
            }
            else if (item.SkillBonus > 0)
            {
                GameplayFeedbackBubbleView skill =
                    Acquire(skillBubbles, ref _skillCursor);
                skill?.Play(
                    item.SkillBonus,
                    position,
                    GameplayFeedbackPresentationPlan.BubbleDurationSeconds);
                LayoutPair(position, scoreBubble, skill);
            }

            if (!item.UsesScoreEncourage || !item.HasFlyEffect)
            {
                RollScore(item.ScoreAfter, 0f);
                return;
            }
            StartScoreFlight(
                scoreBubble != null
                    ? ((RectTransform)scoreBubble.transform).anchoredPosition
                    : position,
                item.ScoreAfter,
                Mathf.Max(0f, item.FlyDelaySeconds),
                false);
        }

        private void PresentWrong(GameplayFeedbackData item)
        {
            if (item.Deduction <= 0) return;
            Acquire(deductionBubbles, ref _deductionCursor)?.Play(
                item.Deduction,
                CellBubblePosition(item.Position),
                GameplayFeedbackPresentationPlan.BubbleDurationSeconds);
            RollScore(item.ScoreAfter, 0f);
        }

        private void PresentLifeBonus(GameplayFeedbackData item, float delay)
        {
            int generation = _generation;
            DOVirtual.DelayedCall(delay, () =>
                {
                    if (generation != _generation) return;
                    Vector2 position = Vector2.zero;
                    if (lifeHudPresenter != null)
                        lifeHudPresenter.TryGetSlotCenter(
                            feedbackArea,
                            item.LifeSlotIndex,
                            out position);
                    Acquire(scoreBubbles, ref _scoreCursor)?.Play(
                        item.DisplayGain,
                        position,
                        GameplayFeedbackPresentationPlan.BubbleDurationSeconds);
                    StartScoreFlight(position, item.ScoreAfter, 0f, true);
                }, true)
                .SetLink(gameObject);
        }

        private void PlayEncourage(int comboCount)
        {
            if (encourageImage == null || encourageSprites == null || encourageSprites.Length == 0)
                return;
            int level = Mathf.Clamp(comboCount - 2, 1, encourageSprites.Length) - 1;
            Sprite sprite = encourageSprites[level];
            if (sprite == null) return;
            if (boardView != null && feedbackArea != null &&
                boardView.TryGetBoardTopCenter(feedbackArea, out Vector2 boardTop))
            {
                encourageImage.rectTransform.anchoredPosition = new Vector2(
                    0f,
                    (feedbackArea.rect.yMax + boardTop.y) * 0.5f - 11f);
            }
            _encourageSequence?.Kill(false);
            encourageImage.sprite = sprite;
            encourageImage.SetNativeSize();
            encourageImage.gameObject.SetActive(true);
            encourageImage.color = new Color(1f, 1f, 1f, 0f);
            encourageImage.rectTransform.localScale = Vector3.one * 0.4f;
            _encourageSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _encourageSequence.Append(DOVirtual.Float(0f, 1f, 0.05f,
                value => SetImageAlpha(encourageImage, value)));
            _encourageSequence.Insert(0f,
                encourageImage.rectTransform.DOScale(1.15f, 0.2f).SetEase(Ease.OutQuad));
            _encourageSequence.Insert(0.2f,
                encourageImage.rectTransform.DOScale(1f, 0.2833333f).SetEase(Ease.InOutQuad));
            _encourageSequence.Insert(0.7f, DOVirtual.Float(1f, 0f, 0.3166667f,
                value => SetImageAlpha(encourageImage, value)).SetEase(Ease.Linear));
            _encourageSequence.OnComplete(() => encourageImage.gameObject.SetActive(false));
        }

        private void RollScore(int target, float delay)
        {
            target = Mathf.Max(0, target);
            _scoreRoll?.Kill(false);
            int from = _displayedScore;
            _displayedScore = target;
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            if (delay > 0f) sequence.AppendInterval(delay);
            sequence.Append(DOVirtual.Int(from, target,
                GameplayFeedbackPresentationPlan.ScoreRollDurationSeconds,
                SetScoreText).SetEase(Ease.Linear));
            _scoreRoll = sequence;
        }

        private void StartScoreFlight(
            Vector2 from,
            int scoreAfter,
            float delay,
            bool life)
        {
            int generation = _generation;
            DOVirtual.DelayedCall(delay, () =>
                {
                    if (generation != _generation) return;
                    GameplayScoreFlightView flight =
                        Acquire(scoreFlights, ref _flightCursor);
                    if (flight == null || !TryGetScoreTarget(out Vector2 target))
                    {
                        RollScore(scoreAfter, 0f);
                        return;
                    }
                    flight.Play(from, target, life, () =>
                    {
                        if (generation != _generation) return;
                        RollScore(scoreAfter, 0f);
                        BounceScore();
                    });
                }, true)
                .SetLink(gameObject);
        }

        private bool TryGetScoreTarget(out Vector2 target)
        {
            target = Vector2.zero;
            if (scoreValue == null || feedbackArea == null) return false;
            RectTransform scoreRect = scoreValue.rectTransform;
            Vector3 world = scoreRect.TransformPoint(scoreRect.rect.center);
            target = feedbackArea.InverseTransformPoint(world);
            return true;
        }

        private void BounceScore()
        {
            if (scoreValue == null) return;
            RectTransform rect = scoreValue.rectTransform;
            _scoreBounce?.Kill(false);
            rect.localScale = Vector3.one;
            _scoreBounce = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _scoreBounce.Append(rect.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad));
            _scoreBounce.Append(rect.DOScale(1f, 0.2f).SetEase(Ease.InOutQuad));
        }

        private void LayoutPair(
            Vector2 anchor,
            GameplayFeedbackBubbleView score,
            GameplayFeedbackBubbleView companion)
        {
            if (score == null || companion == null) return;
            LayoutPair(anchor, score, score.ContentWidth,
                companion.ContentWidth, companion.SetAnchoredPosition);
        }

        private void LayoutPair(
            Vector2 anchor,
            GameplayFeedbackBubbleView score,
            GameplayMultiplierView companion)
        {
            if (score == null || companion == null) return;
            LayoutPair(anchor, score, score.ContentWidth,
                companion.ContentWidth, companion.SetAnchoredPosition);
        }

        private void LayoutPair(
            Vector2 anchor,
            GameplayFeedbackBubbleView score,
            float scoreWidth,
            float companionWidth,
            System.Action<Vector2> setCompanionPosition)
        {
            float totalWidth = scoreWidth + BubbleGap + companionWidth;
            if (feedbackArea != null)
            {
                Rect bounds = feedbackArea.rect;
                anchor.x = Mathf.Clamp(anchor.x,
                    bounds.xMin + totalWidth * 0.5f,
                    bounds.xMax - totalWidth * 0.5f);
            }
            float left = anchor.x - totalWidth * 0.5f;
            score.SetAnchoredPosition(new Vector2(
                left + scoreWidth * 0.5f,
                anchor.y));
            setCompanionPosition(new Vector2(
                left + scoreWidth + BubbleGap + companionWidth * 0.5f,
                anchor.y));
        }

        private Vector2 CellBubblePosition(Vector2Int cell)
        {
            if (boardView == null || feedbackArea == null ||
                !boardView.TryGetCellCenter(feedbackArea, cell.x, cell.y, out Vector2 center))
                return Vector2.zero;
            float boardScale = boardView.transform.lossyScale.y /
                               Mathf.Max(0.0001f, feedbackArea.lossyScale.y);
            center.y += BubbleHeight + BubbleGap + CatTopUnscaled * boardScale;
            float halfWidth = 152.5f;
            Rect rect = feedbackArea.rect;
            center.x = Mathf.Clamp(center.x, rect.xMin + halfWidth, rect.xMax - halfWidth);
            return center;
        }

        private void SetScoreText(int value)
        {
            if (scoreValue != null) scoreValue.text = value.ToString();
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private static GameplayFeedbackBubbleView Acquire(
            GameplayFeedbackBubbleView[] pool,
            ref int cursor)
        {
            if (pool == null || pool.Length == 0) return null;
            for (int offset = 0; offset < pool.Length; offset++)
            {
                int index = (cursor + offset) % pool.Length;
                GameplayFeedbackBubbleView candidate = pool[index];
                if (candidate == null || candidate.IsPlaying) continue;
                cursor = (index + 1) % pool.Length;
                return candidate;
            }
            GameplayFeedbackBubbleView oldest = pool[cursor];
            cursor = (cursor + 1) % pool.Length;
            return oldest;
        }

        private static GameplayMultiplierView Acquire(
            GameplayMultiplierView[] pool,
            ref int cursor)
        {
            if (pool == null || pool.Length == 0) return null;
            for (int offset = 0; offset < pool.Length; offset++)
            {
                int index = (cursor + offset) % pool.Length;
                GameplayMultiplierView candidate = pool[index];
                if (candidate == null || candidate.IsPlaying) continue;
                cursor = (index + 1) % pool.Length;
                return candidate;
            }
            GameplayMultiplierView oldest = pool[cursor];
            cursor = (cursor + 1) % pool.Length;
            return oldest;
        }

        private static GameplayScoreFlightView Acquire(
            GameplayScoreFlightView[] pool,
            ref int cursor)
        {
            if (pool == null || pool.Length == 0) return null;
            for (int offset = 0; offset < pool.Length; offset++)
            {
                int index = (cursor + offset) % pool.Length;
                GameplayScoreFlightView candidate = pool[index];
                if (candidate == null || candidate.IsPlaying) continue;
                cursor = (index + 1) % pool.Length;
                return candidate;
            }
            GameplayScoreFlightView oldest = pool[cursor];
            cursor = (cursor + 1) % pool.Length;
            return oldest;
        }

        private static void StopPool(GameplayFeedbackBubbleView[] pool)
        {
            if (pool == null) return;
            for (int index = 0; index < pool.Length; index++)
            {
                if (pool[index] == null) continue;
                pool[index].Stop();
                pool[index].gameObject.SetActive(false);
            }
        }

        private static void StopPool(GameplayMultiplierView[] pool)
        {
            if (pool == null) return;
            for (int index = 0; index < pool.Length; index++)
            {
                if (pool[index] == null) continue;
                pool[index].Stop();
                pool[index].gameObject.SetActive(false);
            }
        }

        private static void StopPool(GameplayScoreFlightView[] pool)
        {
            if (pool == null) return;
            for (int index = 0; index < pool.Length; index++)
            {
                if (pool[index] == null) continue;
                pool[index].Stop();
                pool[index].gameObject.SetActive(false);
            }
        }
    }
}
