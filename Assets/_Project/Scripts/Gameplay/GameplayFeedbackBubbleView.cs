using DG.Tweening;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    public sealed class GameplayFeedbackBubbleView : MonoBehaviour
    {
        [SerializeField] private RectTransform visual;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private SpriteNumberView number;

        private Sequence _sequence;

        public bool IsPlaying => gameObject.activeSelf;
        public float ContentWidth => number != null ? number.ContentWidth : 0f;

        public void Play(int value, Vector2 anchoredPosition, float duration)
        {
            Stop();
            gameObject.SetActive(true);
            RectTransform root = (RectTransform)transform;
            root.anchoredPosition = anchoredPosition;
            number.SetValue(value);
            visual.localScale = Vector3.one * 0.4f;
            canvasGroup.alpha = 0f;

            float fadeOutStart = Mathf.Max(0.05f, duration - 0.3166667f);
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Append(DOVirtual.Float(0f, 1f, 0.05f,
                value => canvasGroup.alpha = value).SetEase(Ease.Linear));
            _sequence.Insert(0f, visual.DOScale(1.15f, 0.2f).SetEase(Ease.OutQuad));
            _sequence.Insert(0.2f, visual.DOScale(0.98f, 0.2f).SetEase(Ease.InOutQuad));
            _sequence.Insert(0.4f, visual.DOScale(1f, 0.0833333f).SetEase(Ease.OutQuad));
            _sequence.Insert(fadeOutStart, DOVirtual.Float(1f, 0f,
                duration - fadeOutStart,
                value => canvasGroup.alpha = value).SetEase(Ease.Linear));
            _sequence.OnComplete(() => gameObject.SetActive(false));
        }

        public void SetAnchoredPosition(Vector2 anchoredPosition)
        {
            ((RectTransform)transform).anchoredPosition = anchoredPosition;
        }

        public void Stop()
        {
            if (_sequence != null && _sequence.IsActive()) _sequence.Kill(false);
            _sequence = null;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void OnDisable()
        {
            if (_sequence != null && _sequence.IsActive()) _sequence.Kill(false);
            _sequence = null;
        }
    }
}
