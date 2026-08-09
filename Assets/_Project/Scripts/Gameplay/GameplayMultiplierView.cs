using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>Bitmap multiplier port; no text-font substitution.</summary>
    public sealed class GameplayMultiplierView : MonoBehaviour
    {
        [SerializeField] private RectTransform visual;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image xMark;
        [SerializeField] private Image integerDigit;
        [SerializeField] private Image dot;
        [SerializeField] private Image decimalDigit;
        [SerializeField] private Sprite xSprite;
        [SerializeField] private Sprite dotSprite;
        [SerializeField] private Sprite[] digitSprites = new Sprite[10];
        [SerializeField] private float separation = -6f;

        private Sequence _sequence;

        public bool IsPlaying => gameObject.activeSelf;
        public float ContentWidth { get; private set; }

        public void Play(
            float multiplier,
            float previousMultiplier,
            bool scroll,
            Vector2 anchoredPosition)
        {
            Stop();
            gameObject.SetActive(true);
            ((RectTransform)transform).anchoredPosition = anchoredPosition;
            float initial = previousMultiplier > 0f && !Mathf.Approximately(
                previousMultiplier, multiplier)
                ? previousMultiplier
                : multiplier;
            ApplyDisplay(initial);
            visual.anchoredPosition = Vector2.zero;
            visual.localScale = Vector3.one * 0.4f;
            canvasGroup.alpha = 0f;

            float duration = scroll ? 1.4833333f : 0.8f;
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Append(DOVirtual.Float(0f, 1f, scroll ? 0.11666664f : 0.08333328f,
                value => canvasGroup.alpha = value));
            _sequence.Insert(0f, visual.DOScale(scroll ? 1.3f : 1.25f,
                scroll ? 0.15f : 0.18333334f).SetEase(Ease.OutQuad));
            if (!Mathf.Approximately(initial, multiplier))
                _sequence.InsertCallback(scroll ? 0.21f : 0.18f,
                    () => ApplyDisplay(multiplier));
            float shiftStart = scroll ? 0.68333334f : 0.6666667f;
            float shiftDuration = 0.1333334f;
            _sequence.Insert(shiftStart,
                DOVirtual.Float(0f, -120f, shiftDuration, SetVisualX)
                    .SetEase(Ease.InOutQuad));
            _sequence.Insert(duration - 0.3166667f,
                DOVirtual.Float(1f, 0f, 0.3166667f,
                    value => canvasGroup.alpha = value));
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
            Stop();
        }

        private void ApplyDisplay(float multiplier)
        {
            if (digitSprites == null || digitSprites.Length < 10)
            {
                ContentWidth = 0f;
                return;
            }
            int scaled = Mathf.Max(0, Mathf.RoundToInt(multiplier * 10f));
            int integer = Mathf.Clamp(scaled / 10, 0, 9);
            int decimalValue = Mathf.Clamp(scaled % 10, 0, 9);
            Configure(xMark, xSprite);
            Configure(integerDigit, digitSprites[integer]);
            Configure(dot, dotSprite);
            Configure(decimalDigit, digitSprites[decimalValue]);
            Image[] images = { xMark, integerDigit, dot, decimalDigit };
            float width = 0f;
            int visible = 0;
            for (int index = 0; index < images.Length; index++)
            {
                Image image = images[index];
                if (image == null || image.sprite == null) continue;
                if (visible > 0) width += separation;
                width += image.sprite.rect.width;
                visible++;
            }
            ContentWidth = width;
            float cursor = -width * 0.5f;
            for (int index = 0; index < images.Length; index++)
            {
                Image image = images[index];
                if (image == null || image.sprite == null) continue;
                if (cursor > -width * 0.5f) cursor += separation;
                float imageWidth = image.sprite.rect.width;
                image.rectTransform.anchoredPosition =
                    new Vector2(cursor + imageWidth * 0.5f, 0f);
                cursor += imageWidth;
            }
        }

        private static void Configure(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.gameObject.SetActive(sprite != null);
            if (sprite == null) return;
            image.rectTransform.sizeDelta = sprite.rect.size;
        }

        private void SetVisualX(float x)
        {
            Vector2 position = visual.anchoredPosition;
            position.x = x;
            visual.anchoredPosition = position;
        }
    }
}
