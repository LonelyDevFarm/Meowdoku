using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TutorialFinishEffects : MonoBehaviour
    {
        private static readonly Color[] SourceColors =
        {
            new Color32(0xFF, 0x52, 0x52, 0xFF),
            new Color32(0x44, 0x8A, 0xFF, 0xFF),
            new Color32(0x69, 0xF0, 0xAE, 0xFF),
            new Color32(0xFF, 0xD7, 0x40, 0xFF),
            new Color32(0xFF, 0x40, 0x81, 0xFF),
            new Color32(0x40, 0xC4, 0xFF, 0xFF)
        };

        [SerializeField] private RectTransform effectRoot;
        [Header("Source-backed fireworks")]
        [SerializeField] private Sprite lineSprite;
        [SerializeField] private Sprite[] ribbonSprites;
        [SerializeField] private Sprite starSprite;
        [SerializeField] private Sprite glowSprite;

        private readonly List<GameObject> _active = new(96);
        private readonly Stack<GameObject> _pool = new(96);

        public void PlayDefaultConfetti()
        {
            Clear();
            if (effectRoot == null) return;
            for (int index = 0; index < 30; index++)
            {
                GameObject piece = Acquire(
                    $"Confetti_{index:00}", effectRoot, null);
                RectTransform rect = piece.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(
                    Random.Range(6f, 14f),
                    Random.Range(10f, 22f));
                rect.anchoredPosition = new Vector2(
                    Random.Range(40f, 1040f),
                    Random.Range(40f, 150f));
                rect.localRotation = Quaternion.Euler(
                    0f, 0f, Random.Range(0f, 360f));
                Image image = piece.GetComponent<Image>();
                image.color = SourceColors[Random.Range(0, SourceColors.Length)];

                rect.DOAnchorPosY(-1980f, Random.Range(2f, 3.5f))
                    .SetDelay(Random.Range(0f, 0.6f))
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .SetId(this)
                    .OnComplete(() => Release(piece));
            }
        }

        public void PlayFireworks()
        {
            Clear();
            if (effectRoot == null || lineSprite == null ||
                starSprite == null || glowSprite == null ||
                ribbonSprites == null || ribbonSprites.Length == 0)
            {
                PlayDefaultConfetti();
                return;
            }

            // The Godot source fires symmetric bursts from just outside the
            // 1080-wide tutorial root at y=467.
            PlayFireworkSide(new Vector2(-40f, -467f), 1f);
            PlayFireworkSide(new Vector2(1120f, -467f), -1f);
        }

        public void PlayIqBurst(
            RectTransform iqBar,
            float fillFraction,
            bool completed)
        {
            if (iqBar == null || starSprite == null || glowSprite == null)
                return;

            float x = Mathf.Lerp(-296f, 296f, Mathf.Clamp01(fillFraction));
            Vector2 origin = new(x, 0f);
            int starCount = completed ? 10 : 6;
            int glowCount = completed ? 12 : 8;
            for (int index = 0; index < starCount; index++)
                PlayIqParticle(iqBar, origin, starSprite, false, index);
            for (int index = 0; index < glowCount; index++)
                PlayIqParticle(iqBar, origin, glowSprite, true, index);
        }

        public void Clear()
        {
            DOTween.Kill(this, false);
            for (int index = _active.Count - 1; index >= 0; index--)
            {
                GameObject piece = _active[index];
                if (piece == null) continue;
                piece.SetActive(false);
                piece.transform.SetParent(effectRoot, false);
                _pool.Push(piece);
            }
            _active.Clear();
        }

        private void OnDisable()
        {
            Clear();
        }

        private void Release(GameObject piece)
        {
            if (piece == null || !_active.Remove(piece)) return;
            piece.SetActive(false);
            piece.transform.SetParent(effectRoot, false);
            _pool.Push(piece);
        }

        private GameObject Acquire(
            string itemName,
            RectTransform parent,
            Sprite sprite)
        {
            GameObject piece;
            if (_pool.Count > 0)
            {
                piece = _pool.Pop();
                piece.name = itemName;
                piece.SetActive(true);
            }
            else
            {
                piece = new GameObject(
                    itemName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
            }

            piece.layer = parent.gameObject.layer;
            RectTransform rect = piece.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            Image image = piece.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = sprite != null;
            image.raycastTarget = false;
            _active.Add(piece);
            return piece;
        }

        private void PlayFireworkSide(Vector2 origin, float directionX)
        {
            for (int index = 0; index < 8; index++)
            {
                Vector2 direction = new Vector2(
                    directionX * Random.Range(0.75f, 1.3f),
                    Random.Range(0.65f, 1.15f)).normalized;
                GameObject piece = Acquire(
                    $"FireworkLine_{index:00}", effectRoot, lineSprite);
                RectTransform rect = ConfigureTopLeftParticle(
                    piece, origin, new Vector2(90f, 22f));
                rect.localRotation = Quaternion.Euler(
                    0f, 0f, Vector2.SignedAngle(Vector2.up, direction));
                Image image = piece.GetComponent<Image>();
                image.color = SourceColors[Random.Range(0, SourceColors.Length)];
                float distance = Random.Range(360f, 620f);
                rect.DOAnchorPos(origin + direction * distance, 0.28f)
                    .SetEase(Ease.OutCubic).SetUpdate(true).SetId(this);
                image.DOFade(0f, 0.28f).SetEase(Ease.InQuad)
                    .SetUpdate(true).SetId(this)
                    .OnComplete(() => Release(piece));
            }

            for (int index = 0; index < 12; index++)
            {
                Sprite ribbon = ribbonSprites[
                    Random.Range(0, ribbonSprites.Length)];
                GameObject piece = Acquire(
                    $"FireworkRibbon_{index:00}", effectRoot, ribbon);
                RectTransform rect = ConfigureTopLeftParticle(
                    piece,
                    origin + Random.insideUnitCircle * 45f,
                    new Vector2(Random.Range(30f, 56f), Random.Range(45f, 82f)));
                Image image = piece.GetComponent<Image>();
                image.color = SourceColors[Random.Range(0, SourceColors.Length)];
                Vector2 apex = rect.anchoredPosition + new Vector2(
                    directionX * Random.Range(220f, 520f),
                    Random.Range(260f, 580f));
                Vector2 end = apex + new Vector2(
                    directionX * Random.Range(120f, 360f),
                    -Random.Range(1150f, 1700f));
                Sequence flight = DOTween.Sequence()
                    .SetUpdate(true).SetId(this);
                flight.Append(rect.DOAnchorPos(apex, 0.42f)
                    .SetEase(Ease.OutQuad));
                flight.Append(rect.DOAnchorPos(end, Random.Range(1.35f, 1.85f))
                    .SetEase(Ease.InQuad));
                flight.Join(rect.DORotate(
                    new Vector3(0f, 0f, Random.Range(-720f, 720f)),
                    Random.Range(1.35f, 1.85f),
                    RotateMode.FastBeyond360));
                flight.Join(image.DOFade(0f, 0.55f)
                    .SetDelay(Random.Range(0.65f, 1.05f)));
                flight.OnComplete(() => Release(piece));
            }

            for (int index = 0; index < 10; index++)
                PlayBurstParticle(origin, directionX, starSprite, false, index);
            for (int index = 0; index < 8; index++)
                PlayBurstParticle(origin, directionX, glowSprite, true, index);
        }

        private void PlayBurstParticle(
            Vector2 origin,
            float directionX,
            Sprite sprite,
            bool glow,
            int index)
        {
            GameObject piece = Acquire(
                glow ? $"FireworkGlow_{index:00}" : $"FireworkStar_{index:00}",
                effectRoot,
                sprite);
            float size = glow ? Random.Range(42f, 78f) : Random.Range(24f, 52f);
            RectTransform rect = ConfigureTopLeftParticle(
                piece,
                origin + Random.insideUnitCircle * 55f,
                Vector2.one * size);
            Image image = piece.GetComponent<Image>();
            Color color = SourceColors[Random.Range(0, SourceColors.Length)];
            color.a = glow ? 0.55f : 0.9f;
            image.color = color;
            Vector2 direction = new Vector2(
                directionX * Random.Range(0.55f, 1.2f),
                Random.Range(0.5f, 1.25f)).normalized;
            float duration = Random.Range(0.48f, 0.72f);
            rect.DOAnchorPos(
                    rect.anchoredPosition + direction * Random.Range(240f, 430f),
                    duration)
                .SetEase(Ease.OutCubic).SetUpdate(true).SetId(this);
            rect.DOScale(glow ? 1.65f : 0.25f, duration)
                .SetEase(Ease.OutQuad).SetUpdate(true).SetId(this);
            image.DOFade(0f, duration).SetEase(Ease.InQuad)
                .SetUpdate(true).SetId(this)
                .OnComplete(() => Release(piece));
        }

        private void PlayIqParticle(
            RectTransform iqBar,
            Vector2 origin,
            Sprite sprite,
            bool glow,
            int index)
        {
            GameObject piece = Acquire(
                glow ? $"IqGlow_{index:00}" : $"IqStar_{index:00}",
                iqBar,
                sprite);
            RectTransform rect = piece.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = origin + Random.insideUnitCircle * 18f;
            float size = glow ? Random.Range(26f, 52f) : Random.Range(18f, 34f);
            rect.sizeDelta = Vector2.one * size;
            Image image = piece.GetComponent<Image>();
            Color color = SourceColors[Random.Range(0, SourceColors.Length)];
            color.a = glow ? 0.4f : 0.9f;
            image.color = color;
            float duration = Random.Range(0.35f, 0.58f);
            rect.DOAnchorPos(
                    rect.anchoredPosition + Random.insideUnitCircle *
                    Random.Range(45f, 105f),
                    duration)
                .SetEase(Ease.OutCubic).SetUpdate(true).SetId(this);
            rect.DOScale(glow ? 1.8f : 0.2f, duration)
                .SetUpdate(true).SetId(this);
            image.DOFade(0f, duration).SetUpdate(true).SetId(this)
                .OnComplete(() => Release(piece));
        }

        private static RectTransform ConfigureTopLeftParticle(
            GameObject piece,
            Vector2 position,
            Vector2 size)
        {
            RectTransform rect = piece.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
