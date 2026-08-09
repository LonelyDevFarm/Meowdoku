using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>Unity presentation equivalent of the source LifeSlot.</summary>
    public sealed class GameplayLifeSlotView : MonoBehaviour
    {
        [SerializeField] private Image dimImage;
        [SerializeField] private Image fullImage;

        private Sequence _sequence;
        private bool _isLost;

        public void ShowAlive()
        {
            KillSequence();
            _isLost = false;
            if (dimImage != null) dimImage.gameObject.SetActive(false);
            if (fullImage == null) return;
            fullImage.gameObject.SetActive(true);
            fullImage.color = Color.white;
            fullImage.rectTransform.anchoredPosition = Vector2.zero;
            fullImage.rectTransform.localScale = Vector3.one;
        }

        public void ShowLost(bool animate, bool silent = false)
        {
            if (!animate && _isLost) return;
            KillSequence();
            _isLost = true;
            if (dimImage != null) dimImage.gameObject.SetActive(true);
            if (fullImage == null) return;
            fullImage.gameObject.SetActive(true);
            fullImage.color = Color.white;
            fullImage.rectTransform.anchoredPosition = Vector2.zero;
            fullImage.rectTransform.localScale = Vector3.one;

            if (!animate)
            {
                fullImage.gameObject.SetActive(false);
                return;
            }

            if (silent)
            {
                _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
                _sequence.Join(DOVirtual.Float(1f, 0f, 0.3f, SetFullAlpha)
                    .SetEase(Ease.Linear));
                _sequence.Join(fullImage.rectTransform.DOScale(0.8f, 0.3f)
                    .SetEase(Ease.Linear));
                _sequence.OnComplete(() => fullImage.gameObject.SetActive(false));
                return;
            }

            // Source Appear keys: y 0 -> -25 -> 0, then Full hides at 0.25 s.
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Append(DOVirtual.Float(0f, -25f, 0.15176266f, SetFullY)
                .SetEase(Ease.InOutQuad));
            _sequence.Append(DOVirtual.Float(-25f, 0f, 0.21321014f, SetFullY)
                .SetEase(Ease.InOutQuad));
            _sequence.InsertCallback(0.25f, () => fullImage.gameObject.SetActive(false));
            _sequence.AppendInterval(0.8f - 0.3649728f);
        }

        public void PlayRevive()
        {
            KillSequence();
            _isLost = false;
            if (dimImage != null) dimImage.gameObject.SetActive(false);
            if (fullImage == null) return;
            fullImage.gameObject.SetActive(true);
            fullImage.color = new Color(1f, 1f, 1f, 0f);
            fullImage.rectTransform.anchoredPosition = Vector2.zero;
            fullImage.rectTransform.localScale = Vector3.one * 0.3f;

            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Append(fullImage.rectTransform.DOScale(1.3f, 0.08333331f)
                .SetEase(Ease.OutQuad));
            _sequence.Insert(0f, DOVirtual.Float(0f, 1f, 0.1f, SetFullAlpha)
                .SetEase(Ease.Linear));
            _sequence.Append(fullImage.rectTransform.DOScale(0.85f, 0.15f)
                .SetEase(Ease.InOutQuad));
            _sequence.Append(fullImage.rectTransform.DOScale(1f, 0.26666665f)
                .SetEase(Ease.OutQuad));
        }

        private void OnDisable()
        {
            KillSequence();
        }

        private void KillSequence()
        {
            if (_sequence != null && _sequence.IsActive()) _sequence.Kill(false);
            _sequence = null;
        }

        private void SetFullAlpha(float alpha)
        {
            if (fullImage == null) return;
            Color color = fullImage.color;
            color.a = alpha;
            fullImage.color = color;
        }

        private void SetFullY(float y)
        {
            if (fullImage == null) return;
            Vector2 position = fullImage.rectTransform.anchoredPosition;
            position.y = y;
            fullImage.rectTransform.anchoredPosition = position;
        }
    }
}
