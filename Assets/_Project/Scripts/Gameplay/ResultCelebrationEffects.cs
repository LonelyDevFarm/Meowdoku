using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ResultCelebrationEffects : MonoBehaviour
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
        [SerializeField] private Sprite lineSprite;
        [SerializeField] private Sprite[] ribbonSprites;
        [SerializeField] private Sprite starSprite;
        [SerializeField] private Sprite glowSprite;

        private readonly List<GameObject> _active = new(128);
        private readonly Stack<GameObject> _pool = new(128);
        private bool _isRunning;
        private int _playCount;

        public void Play()
        {
            Clear();
            if (effectRoot == null || lineSprite == null || starSprite == null ||
                glowSprite == null || ribbonSprites == null ||
                ribbonSprites.Length == 0)
                return;

            _isRunning = true;
            _playCount++;
            DOVirtual.DelayedCall(0.39431918f, PlayOneShot, true)
                .SetId(this).SetLink(gameObject);
            DOVirtual.DelayedCall(0.5346975f, SpawnRibbonBatch, true)
                .SetId(this).SetLink(gameObject);
        }

        public void Clear()
        {
            DOTween.Kill(this, false);
            _isRunning = false;
            for (int index = _active.Count - 1; index >= 0; index--)
                Recycle(_active[index]);
            _active.Clear();
        }

        private void OnDisable() => Clear();

        private void PlayOneShot()
        {
            if (!_isRunning) return;
            PlaySide(new Vector2(-190f, -1780f), new Vector2(-160f, -1930f), 1f);
            PlaySide(new Vector2(1270f, -1780f), new Vector2(1210f, -1930f), -1f);
        }

        private void PlaySide(Vector2 origin, Vector2 lineOrigin, float inward)
        {
            for (int i = 0; i < 9; i++)
            {
                Vector2 direction = new(inward * Random.Range(0.45f, 1.05f),
                    Random.Range(0.65f, 1.2f));
                direction.Normalize();
                GameObject item = Acquire("ResultLine", lineSprite);
                RectTransform rect = Configure(item, lineOrigin, new Vector2(110f, 24f));
                rect.localRotation = Quaternion.Euler(0f, 0f,
                    Vector2.SignedAngle(Vector2.up, direction));
                Tint(item, 1f);
                float duration = Random.Range(0.22f, 0.34f);
                rect.DOAnchorPos(lineOrigin + direction * Random.Range(360f, 610f), duration)
                    .SetEase(Ease.OutCubic).SetUpdate(true).SetId(this);
                item.GetComponent<Image>().DOFade(0f, duration)
                    .SetUpdate(true).SetId(this).OnComplete(() => Release(item));
            }

            for (int i = 0; i < 9; i++) SpawnBurst(origin, inward, starSprite, false);
            for (int i = 0; i < 7; i++) SpawnBurst(origin, inward, glowSprite, true);
            for (int i = 0; i < 8; i++) SpawnBurstRibbon(origin, inward);
        }

        private void SpawnBurst(Vector2 origin, float inward, Sprite sprite, bool glow)
        {
            GameObject item = Acquire(glow ? "ResultGlow" : "ResultStar", sprite);
            float size = glow ? Random.Range(48f, 92f) : Random.Range(28f, 58f);
            RectTransform rect = Configure(item, origin + Random.insideUnitCircle * 50f,
                Vector2.one * size);
            Tint(item, glow ? 0.55f : 0.95f);
            Vector2 direction = new(inward * Random.Range(0.45f, 1.15f),
                Random.Range(0.55f, 1.2f));
            direction.Normalize();
            float duration = Random.Range(0.42f, 0.68f);
            rect.DOAnchorPos(rect.anchoredPosition + direction * Random.Range(250f, 470f), duration)
                .SetEase(Ease.OutCubic).SetUpdate(true).SetId(this);
            rect.DOScale(glow ? 1.7f : 0.25f, duration)
                .SetUpdate(true).SetId(this);
            item.GetComponent<Image>().DOFade(0f, duration)
                .SetUpdate(true).SetId(this).OnComplete(() => Release(item));
        }

        private void SpawnBurstRibbon(Vector2 origin, float inward)
        {
            GameObject item = Acquire("ResultBurstRibbon",
                ribbonSprites[Random.Range(0, ribbonSprites.Length)]);
            RectTransform rect = Configure(item, origin + Random.insideUnitCircle * 45f,
                new Vector2(Random.Range(26f, 48f), Random.Range(42f, 76f)));
            Tint(item, 1f);
            Vector2 apex = rect.anchoredPosition +
                           new Vector2(inward * Random.Range(230f, 480f), Random.Range(250f, 510f));
            Vector2 end = apex + new Vector2(inward * Random.Range(80f, 260f), -Random.Range(800f, 1250f));
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetId(this);
            sequence.Append(rect.DOAnchorPos(apex, 0.4f).SetEase(Ease.OutQuad));
            sequence.Append(rect.DOAnchorPos(end, Random.Range(1.25f, 1.7f)).SetEase(Ease.InQuad));
            sequence.Join(rect.DORotate(new Vector3(0f, 0f, Random.Range(-720f, 720f)),
                1.5f, RotateMode.FastBeyond360));
            sequence.Join(item.GetComponent<Image>().DOFade(0f, 0.5f).SetDelay(0.85f));
            sequence.OnComplete(() => Release(item));
        }

        private void SpawnRibbonBatch()
        {
            if (!_isRunning) return;
            for (int i = 0; i < 5; i++) SpawnFallingRibbon();
            DOVirtual.DelayedCall(0.45f, SpawnRibbonBatch, true)
                .SetId(this).SetLink(gameObject);
        }

        private void SpawnFallingRibbon()
        {
            GameObject item = Acquire("ResultRibbon",
                ribbonSprites[Random.Range(0, ribbonSprites.Length)]);
            float duration = Random.Range(2.5f, 3.2f);
            Vector2 start = new(Random.Range(1f, 1080f), Random.Range(-1048f, -248f));
            RectTransform rect = Configure(item, start,
                new Vector2(Random.Range(25f, 48f), Random.Range(42f, 78f)));
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            Tint(item, 1f);
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetId(this);
            sequence.Append(rect.DOAnchorPos(start + new Vector2(Random.Range(-130f, 130f),
                -Random.Range(200f, 315f) * duration), duration).SetEase(Ease.Linear));
            sequence.Join(rect.DORotate(new Vector3(0f, 0f, Random.Range(-900f, 900f)),
                duration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
            sequence.Join(item.GetComponent<Image>().DOFade(0f, 0.55f)
                .SetDelay(duration - 0.55f));
            sequence.OnComplete(() => Release(item));
        }

        private GameObject Acquire(string itemName, Sprite sprite)
        {
            GameObject item = _pool.Count > 0 ? _pool.Pop() :
                new GameObject(itemName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.name = itemName;
            item.layer = effectRoot.gameObject.layer;
            item.transform.SetParent(effectRoot, false);
            item.transform.localScale = Vector3.one;
            item.transform.localRotation = Quaternion.identity;
            Image image = item.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            item.SetActive(true);
            _active.Add(item);
            return item;
        }

        private void Release(GameObject item)
        {
            if (item == null || !_active.Remove(item)) return;
            Recycle(item);
        }

        private void Recycle(GameObject item)
        {
            if (item == null) return;
            item.SetActive(false);
            if (effectRoot != null) item.transform.SetParent(effectRoot, false);
            _pool.Push(item);
        }

        private static RectTransform Configure(GameObject item, Vector2 position, Vector2 size)
        {
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Tint(GameObject item, float alpha)
        {
            Color color = SourceColors[Random.Range(0, SourceColors.Length)];
            color.a = alpha;
            item.GetComponent<Image>().color = color;
        }

#if UNITY_INCLUDE_TESTS
        internal bool IsRunningForTests => _isRunning;
        internal int ActiveCountForTests => _active.Count;
        internal int PlayCountForTests => _playCount;
#endif
    }
}
