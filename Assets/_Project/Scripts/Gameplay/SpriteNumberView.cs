using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Unity equivalent of the source TextureRect digit rows. It keeps the
    /// original bitmap glyphs instead of replacing them with a font.
    /// </summary>
    public sealed class SpriteNumberView : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private Image sign;
        [SerializeField] private Image[] digits = new Image[4];
        [SerializeField] private Sprite signSprite;
        [SerializeField] private Sprite[] digitSprites = new Sprite[10];
        [SerializeField] private float separation = -18f;

        public float ContentWidth { get; private set; }

        public void SetValue(int value)
        {
            int absolute = Mathf.Abs(value);
            int count = DigitCount(absolute);
            int divisor = Pow10(count - 1);
            float width = 0f;
            int visibleCount = 0;

            ConfigureImage(sign, signSprite, true, ref width, ref visibleCount);
            for (int index = 0; index < digits.Length; index++)
            {
                bool visible = index < count;
                int digit = visible ? absolute / divisor % 10 : 0;
                Sprite sprite = visible && digitSprites != null && digit < digitSprites.Length
                    ? digitSprites[digit]
                    : null;
                ConfigureImage(digits[index], sprite, visible, ref width, ref visibleCount);
                if (visible && divisor > 1) divisor /= 10;
            }

            ContentWidth = Mathf.Max(0f, width);
            LayoutVisibleImages(ContentWidth);
            if (content != null)
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ContentWidth);
        }

        private void ConfigureImage(
            Image image,
            Sprite sprite,
            bool visible,
            ref float width,
            ref int visibleCount)
        {
            if (image == null) return;
            visible &= sprite != null;
            image.gameObject.SetActive(visible);
            if (!visible) return;
            image.sprite = sprite;
            RectTransform rect = image.rectTransform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sprite.rect.width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sprite.rect.height);
            if (visibleCount > 0) width += separation;
            width += sprite.rect.width;
            visibleCount++;
        }

        private void LayoutVisibleImages(float totalWidth)
        {
            float cursor = -totalWidth * 0.5f;
            bool placed = false;
            Place(sign, ref cursor, ref placed);
            for (int index = 0; index < digits.Length; index++)
                Place(digits[index], ref cursor, ref placed);
        }

        private void Place(Image image, ref float cursor, ref bool placed)
        {
            if (image == null || !image.gameObject.activeSelf || image.sprite == null) return;
            if (placed) cursor += separation;
            float width = image.sprite.rect.width;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(cursor + width * 0.5f, 0f);
            cursor += width;
            placed = true;
        }

        private static int DigitCount(int value)
        {
            if (value < 10) return 1;
            if (value < 100) return 2;
            if (value < 1000) return 3;
            return 4;
        }

        private static int Pow10(int exponent)
        {
            int value = 1;
            for (int index = 0; index < exponent; index++) value *= 10;
            return value;
        }
    }
}
