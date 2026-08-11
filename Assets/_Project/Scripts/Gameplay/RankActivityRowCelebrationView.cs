using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityRowCelebrationView : MonoBehaviour
    {
        public const float CollectionDuration = 1.6666701f;
        public const float CollectionInterval = 0.1f;
        public const float CollectionArrivalTime = 1.0333333f;
        public const float RiseUpDuration = 0.23333333f;
        public const float RiseDownDuration = 0.33333334f;
        public const float RiseDownBurstTime = 0.23333335f;
        public const float ArrowDropFadeDuration = 0.06666667f;

        [Header("Collection")]
        [SerializeField] private RectTransform collectionTarget;
        [SerializeField] private Image[] collectionItems = new Image[0];
        [SerializeField] private Image[] collectionGlows = new Image[0];
        [SerializeField] private Image[] collectionStars = new Image[0];
        [SerializeField] private Sprite fishSprite;
        [SerializeField] private Sprite catSprite;

        [Header("Rise")]
        [SerializeField] private CanvasGroup arrowGroup;
        [SerializeField] private Image[] arrowItems = new Image[0];
        [SerializeField] private Image riseGlow;
        [SerializeField] private Image[] riseStars = new Image[0];

        private Sequence _collectionSequence;
        private Sequence _arrowLoop;
        private Tween _arrowFade;
        private Sequence _riseBurstSequence;
        private Vector2[] _collectionBasePositions;
        private Vector2[] _arrowBasePositions;
        private Vector2[] _riseStarBasePositions;
        private bool _positionsCaptured;

        public bool CollectionPlaying =>
            _collectionSequence != null && _collectionSequence.IsActive();
        public bool ArrowVisible =>
            arrowGroup != null && arrowGroup.alpha > 0.001f;

        private void Awake()
        {
            CapturePositions();
            ResetImmediate();
        }

        private void OnDisable()
        {
            KillAll();
            ResetImmediate();
        }

        private void OnDestroy()
        {
            KillAll();
        }

        public void SetCollectionIsCat(bool isCat)
        {
            Sprite sprite = isCat ? catSprite : fishSprite;
            for (int index = 0; index < collectionItems.Length; index++)
            {
                if (collectionItems[index] != null)
                    collectionItems[index].sprite = sprite;
            }
        }

        public void PlayCollection()
        {
            CapturePositions();
            KillCollection();
            ResetCollection();
            if (collectionTarget == null || collectionItems.Length == 0)
                return;

            _collectionSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            Vector2 target = collectionTarget.anchoredPosition;
            for (int index = 0; index < collectionItems.Length; index++)
            {
                Image item = collectionItems[index];
                if (item == null) continue;
                float start = CollectionInterval * index;
                RectTransform rect = item.rectTransform;
                item.gameObject.SetActive(true);
                rect.anchoredPosition = _collectionBasePositions[index];
                rect.localScale = Vector3.one * 0.3f;
                SetAlpha(item, 0f);

                _collectionSequence.Insert(
                    start,
                    item.DOFade(1f, 0.06666667f).SetEase(Ease.Linear));
                _collectionSequence.Insert(
                    start,
                    rect.DOScale(0.8f, 0.18666667f)
                        .SetEase(Ease.OutCubic));
                _collectionSequence.Insert(
                    start + 0.18666667f,
                    rect.DOScale(0.6f, 0.14f)
                        .SetEase(Ease.InOutSine));
                _collectionSequence.Insert(
                    start + 0.6066667f,
                    rect.DOScale(0.67f, 0.4266666f)
                        .SetEase(Ease.InQuad));
                _collectionSequence.Insert(
                    start + 0.7933333f,
                    rect.DOAnchorPos(target, 0.24f)
                        .SetEase(Ease.InCubic));
                _collectionSequence.Insert(
                    start + 1f,
                    item.DOFade(0f, 0.03333334f).SetEase(Ease.Linear));
                InsertCollectionBurst(index, start + CollectionArrivalTime);
            }
            if (_collectionSequence.Duration() < CollectionDuration)
                _collectionSequence.AppendInterval(
                    CollectionDuration - _collectionSequence.Duration());
            _collectionSequence.OnComplete(() =>
            {
                _collectionSequence = null;
                ResetCollection();
            });
        }

        public void PlayArrow(float fadeDuration)
        {
            StartArrowLoop();
            KillArrowFade();
            if (arrowGroup == null) return;
            arrowGroup.alpha = 1f;
            _arrowFade = arrowGroup.DOFade(
                    0f,
                    Mathf.Max(0.01f, fadeDuration))
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => _arrowFade = null);
        }

        public void PlayRiseIdle()
        {
            StartArrowLoop();
            KillArrowFade();
            if (arrowGroup != null) arrowGroup.alpha = 1f;
        }

        public void BeginRiseDown()
        {
            FadeArrow(ArrowDropFadeDuration);
            KillRiseBurst();
            _riseBurstSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            InsertRiseBurst(RiseDownBurstTime);
            _riseBurstSequence.OnComplete(() =>
            {
                _riseBurstSequence = null;
                ResetRiseBurst();
            });
        }

        public void HideArrow()
        {
            KillArrowFade();
            if (_arrowLoop != null && _arrowLoop.IsActive())
                _arrowLoop.Kill(false);
            _arrowLoop = null;
            ResetArrow();
        }

        public void ResetImmediate()
        {
            CapturePositions();
            ResetCollection();
            ResetArrow();
            ResetRiseBurst();
        }

        public void Stop()
        {
            KillAll();
            ResetImmediate();
        }

        private void InsertCollectionBurst(int index, float at)
        {
            if (index < collectionGlows.Length &&
                collectionGlows[index] != null)
            {
                Image glow = collectionGlows[index];
                glow.gameObject.SetActive(true);
                glow.rectTransform.localScale = Vector3.one * 0.2f;
                SetAlpha(glow, 0f);
                _collectionSequence.Insert(
                    at,
                    glow.DOFade(0.7f, 0.08f).SetEase(Ease.OutQuad));
                _collectionSequence.Insert(
                    at,
                    glow.rectTransform.DOScale(1.25f, 0.3f)
                        .SetEase(Ease.OutCubic));
                _collectionSequence.Insert(
                    at + 0.08f,
                    glow.DOFade(0f, 0.22f).SetEase(Ease.Linear));
            }
            if (index < collectionStars.Length &&
                collectionStars[index] != null)
            {
                Image star = collectionStars[index];
                star.gameObject.SetActive(true);
                star.rectTransform.localScale = Vector3.one * 0.2f;
                SetAlpha(star, 0f);
                _collectionSequence.Insert(
                    at,
                    star.DOFade(1f, 0.05f).SetEase(Ease.Linear));
                _collectionSequence.Insert(
                    at,
                    star.rectTransform.DOScale(1f, 0.28f)
                        .SetEase(Ease.OutBack));
                _collectionSequence.Insert(
                    at + 0.12f,
                    star.DOFade(0f, 0.2f).SetEase(Ease.Linear));
            }
        }

        private void StartArrowLoop()
        {
            CapturePositions();
            if (_arrowLoop != null && _arrowLoop.IsActive()) return;
            if (arrowGroup == null || arrowItems.Length == 0) return;
            ResetArrowItems();
            arrowGroup.gameObject.SetActive(true);
            _arrowLoop = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            for (int index = 0; index < arrowItems.Length; index++)
            {
                Image arrow = arrowItems[index];
                if (arrow == null) continue;
                float start = 0.25f * index;
                RectTransform rect = arrow.rectTransform;
                Vector2 from = _arrowBasePositions[index];
                Vector2 to = from + new Vector2(0f, 330f);
                _arrowLoop.Insert(
                    start,
                    arrow.DOFade(1f, 0.12f).SetEase(Ease.Linear));
                _arrowLoop.Insert(
                    start,
                    rect.DOAnchorPos(to, 1.1f).SetEase(Ease.Linear));
                _arrowLoop.Insert(
                    start + 0.75f,
                    arrow.DOFade(0f, 0.35f).SetEase(Ease.Linear));
            }
            _arrowLoop.SetLoops(-1, LoopType.Restart);
        }

        private void FadeArrow(float duration)
        {
            KillArrowFade();
            if (arrowGroup == null) return;
            _arrowFade = arrowGroup.DOFade(0f, duration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => _arrowFade = null);
        }

        private void InsertRiseBurst(float at)
        {
            CapturePositions();
            if (riseGlow != null)
            {
                riseGlow.gameObject.SetActive(true);
                riseGlow.rectTransform.localScale = Vector3.one * 0.5f;
                SetAlpha(riseGlow, 0f);
                _riseBurstSequence.Insert(
                    at,
                    riseGlow.DOFade(0.8f, 0.08f).SetEase(Ease.OutQuad));
                _riseBurstSequence.Insert(
                    at,
                    riseGlow.rectTransform.DOScale(2.2f, 0.5f)
                        .SetEase(Ease.OutCubic));
                _riseBurstSequence.Insert(
                    at + 0.08f,
                    riseGlow.DOFade(0f, 0.42f).SetEase(Ease.Linear));
            }
            Vector2 center = riseGlow != null
                ? riseGlow.rectTransform.anchoredPosition
                : Vector2.zero;
            for (int index = 0; index < riseStars.Length; index++)
            {
                Image star = riseStars[index];
                if (star == null) continue;
                RectTransform rect = star.rectTransform;
                Vector2 from = _riseStarBasePositions[index];
                Vector2 direction = from - center;
                if (direction.sqrMagnitude < 0.001f) direction = Vector2.up;
                Vector2 to = from + direction.normalized * 150f;
                star.gameObject.SetActive(true);
                rect.anchoredPosition = from;
                rect.localScale = Vector3.one * (index % 2 == 0 ? 0.1f : 0.2f);
                SetAlpha(star, 0f);
                float start = at + 0.012f * index;
                _riseBurstSequence.Insert(
                    start,
                    star.DOFade(1f, 0.05f).SetEase(Ease.Linear));
                _riseBurstSequence.Insert(
                    start,
                    rect.DOAnchorPos(to, 0.6f).SetEase(Ease.OutCubic));
                _riseBurstSequence.Insert(
                    start,
                    rect.DOScale(0.35f, 0.22f).SetEase(Ease.OutBack));
                _riseBurstSequence.Insert(
                    start + 0.3f,
                    star.DOFade(0f, 0.3f).SetEase(Ease.Linear));
            }
        }

        private void CapturePositions()
        {
            if (_positionsCaptured) return;
            _collectionBasePositions = Capture(collectionItems);
            _arrowBasePositions = Capture(arrowItems);
            _riseStarBasePositions = Capture(riseStars);
            _positionsCaptured = true;
        }

        private static Vector2[] Capture(Image[] images)
        {
            var result = new Vector2[images?.Length ?? 0];
            for (int index = 0; index < result.Length; index++)
                if (images[index] != null)
                    result[index] = images[index].rectTransform.anchoredPosition;
            return result;
        }

        private void ResetCollection()
        {
            for (int index = 0; index < collectionItems.Length; index++)
                ResetImage(collectionItems[index],
                    _collectionBasePositions,
                    index,
                    0.3f);
            for (int index = 0; index < collectionGlows.Length; index++)
                ResetImage(collectionGlows[index], null, index, 0.2f);
            for (int index = 0; index < collectionStars.Length; index++)
                ResetImage(collectionStars[index], null, index, 0.2f);
        }

        private void ResetArrow()
        {
            if (arrowGroup != null)
            {
                arrowGroup.alpha = 0f;
                arrowGroup.gameObject.SetActive(false);
            }
            ResetArrowItems();
        }

        private void ResetArrowItems()
        {
            for (int index = 0; index < arrowItems.Length; index++)
                ResetImage(arrowItems[index], _arrowBasePositions, index, 1f, false);
        }

        private void ResetRiseBurst()
        {
            ResetImage(riseGlow, null, 0, 0.5f);
            for (int index = 0; index < riseStars.Length; index++)
                ResetImage(riseStars[index], _riseStarBasePositions, index, 0.2f);
        }

        private static void ResetImage(
            Image image,
            Vector2[] positions,
            int index,
            float scale,
            bool deactivate = true)
        {
            if (image == null) return;
            if (positions != null && index >= 0 && index < positions.Length)
                image.rectTransform.anchoredPosition = positions[index];
            image.rectTransform.localScale = Vector3.one * scale;
            SetAlpha(image, 0f);
            if (deactivate) image.gameObject.SetActive(false);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            Color color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }

        private void KillAll()
        {
            KillCollection();
            HideArrow();
            KillRiseBurst();
        }

        private void KillCollection()
        {
            if (_collectionSequence != null && _collectionSequence.IsActive())
                _collectionSequence.Kill(false);
            _collectionSequence = null;
        }

        private void KillArrowFade()
        {
            if (_arrowFade != null && _arrowFade.IsActive())
                _arrowFade.Kill(false);
            _arrowFade = null;
        }

        private void KillRiseBurst()
        {
            if (_riseBurstSequence != null && _riseBurstSequence.IsActive())
                _riseBurstSequence.Kill(false);
            _riseBurstSequence = null;
        }
    }
}
