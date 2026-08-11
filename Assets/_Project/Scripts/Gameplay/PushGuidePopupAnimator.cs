using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PushGuidePopupAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform popup;
        [SerializeField] private CanvasGroup popupGroup;
        [SerializeField] private RectTransform catAccent;

        private Sequence _sequence;
        private Vector2 _shownPosition;

        public void PlayOpen()
        {
            Stop();
            if (popup == null || popupGroup == null) return;
            _shownPosition = popup.anchoredPosition;
            popup.anchoredPosition = _shownPosition + new Vector2(0f, -126f);
            popupGroup.alpha = 0f;
            if (catAccent != null) catAccent.localScale = Vector3.zero;
            _sequence = DOTween.Sequence().SetLink(gameObject);
            _sequence.Append(popup.DOAnchorPosY(
                    _shownPosition.y + 50f,
                    0.1f)
                .SetEase(Ease.OutQuad));
            _sequence.Append(popup.DOAnchorPosY(
                    _shownPosition.y,
                    0.2f)
                .SetEase(Ease.InOutQuad));
            _sequence.Insert(0.0666667f, popupGroup.DOFade(1f, 0.1f));
            if (catAccent != null)
            {
                _sequence.Insert(0.0333333f,
                    catAccent.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad));
                _sequence.Insert(0.1333333f,
                    catAccent.DOScale(0.8f, 0.1f).SetEase(Ease.InOutQuad));
                _sequence.Insert(0.2333333f,
                    catAccent.DOScale(1.1f, 0.1f).SetEase(Ease.OutQuad));
                _sequence.Insert(0.3333333f,
                    catAccent.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
            }
            _sequence.AppendInterval(0.1666667f);
            _sequence.OnComplete(() => _sequence = null);
        }

        public IEnumerator PlayClose()
        {
            Stop();
            if (popup == null || popupGroup == null) yield break;
            bool completed = false;
            _shownPosition = popup.anchoredPosition;
            _sequence = DOTween.Sequence().SetLink(gameObject);
            _sequence.Append(popup.DOAnchorPosY(
                    _shownPosition.y - 106f,
                    0.3333333f)
                .SetEase(Ease.InQuad));
            _sequence.Insert(0.2f, popupGroup.DOFade(0f, 0.1333333f));
            _sequence.OnComplete(() => completed = true);
            while (!completed && _sequence != null && _sequence.IsActive())
                yield return null;
            Stop();
        }

        public void Stop()
        {
            _sequence?.Kill(false);
            _sequence = null;
        }

        private void OnDisable() => Stop();
        private void OnDestroy() => Stop();
    }
}
