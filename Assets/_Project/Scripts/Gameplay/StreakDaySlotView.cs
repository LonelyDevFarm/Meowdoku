using DG.Tweening;
using Meowdoku.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StreakDaySlotView : MonoBehaviour
    {
        public const float CheckinDurationSeconds = 2.0833333f;
        public const float RewardDurationSeconds = 0.56666666f;

        private static readonly Color CheckedColor =
            new Color32(241, 147, 32, 255);
        private static readonly Color InactiveColor =
            new Color32(147, 90, 90, 255);

        private static readonly string[] WeekdayKeys =
        {
            "WEEKDAY_SUN", "WEEKDAY_MON", "WEEKDAY_TUE",
            "WEEKDAY_WED", "WEEKDAY_THU", "WEEKDAY_FRI",
            "WEEKDAY_SAT"
        };

        [SerializeField] private Text weekdayText;
        [SerializeField] private GameObject uncheckedDot;
        [SerializeField] private GameObject checkedDot;
        [SerializeField] private GameObject chest;
        [SerializeField] private CanvasGroup uncheckedCanvas;
        [SerializeField] private CanvasGroup checkedCanvas;
        [SerializeField] private CanvasGroup chestCanvas;
        [SerializeField] private LocalizationCatalog localization;

        private Sequence _visualSequence;
        private RectTransform _uncheckedRect;
        private RectTransform _checkedRect;
        private RectTransform _chestRect;
        private Vector2 _uncheckedPosition;
        private Vector2 _checkedPosition;
        private Vector2 _chestPosition;
        private bool _visualsCached;

#if UNITY_INCLUDE_TESTS
        internal bool IsCheckedForTests =>
            checkedDot != null && checkedDot.activeSelf;
        internal bool IsChestForTests =>
            chest != null && chest.activeSelf;
        internal bool IsAnimatingForTests =>
            _visualSequence != null && _visualSequence.IsActive();
#endif

        private void Awake()
        {
            CacheVisuals();
        }

        private void OnDisable()
        {
            KillVisualSequence();
        }

        private void OnDestroy()
        {
            KillVisualSequence();
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            localization = catalog;
        }

        public void ApplyStatic(
            int weekday,
            bool isChecked,
            bool isChest)
        {
            if (_visualSequence != null && _visualSequence.IsActive() &&
                isChecked)
                return;

            KillVisualSequence();
            CacheVisuals();
            if (weekdayText != null)
            {
                string key = WeekdayKeys[
                    Mathf.Clamp(weekday, 0, WeekdayKeys.Length - 1)];
                string value = localization != null
                    ? localization.Translate(key)
                    : key;
                weekdayText.text = string.IsNullOrEmpty(value) ||
                                   value == key
                    ? key.Replace("WEEKDAY_", string.Empty)
                    : value;
                weekdayText.color = isChecked
                    ? CheckedColor
                    : InactiveColor;
            }

            SetActive(uncheckedDot, !isChecked && !isChest);
            SetActive(checkedDot, isChecked);
            SetActive(chest, !isChecked && isChest);
            ResetVisuals();
        }

        public float PlayCheckin(bool rewardChest)
        {
            KillVisualSequence();
            CacheVisuals();
            if (rewardChest)
                return PlayReward();

            SetActive(uncheckedDot, true);
            SetActive(checkedDot, true);
            SetActive(chest, false);
            if (uncheckedCanvas != null) uncheckedCanvas.alpha = 1f;
            if (checkedCanvas != null) checkedCanvas.alpha = 0f;
            if (_uncheckedRect != null)
            {
                _uncheckedRect.anchoredPosition = _uncheckedPosition;
                _uncheckedRect.localScale = Vector3.one;
            }
            if (_checkedRect != null)
            {
                _checkedRect.anchoredPosition =
                    _checkedPosition + new Vector2(0f, -130f);
                _checkedRect.localScale = Vector3.one;
            }

            _visualSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            if (checkedCanvas != null)
                _visualSequence.Insert(0f,
                    checkedCanvas.DOFade(1f, 0.1f));
            if (_checkedRect != null)
            {
                _visualSequence.Insert(0f,
                    _checkedRect.DOAnchorPos(_checkedPosition, 0.5666667f)
                        .SetEase(Ease.OutBack, 1.15f));
                Sequence squash = DOTween.Sequence()
                    .Append(_checkedRect.DOScale(
                        new Vector3(0.2f, 1f, 1f), 0.1f))
                    .Append(_checkedRect.DOScale(
                        new Vector3(1f, 1.1f, 1f), 0.1f))
                    .Append(_checkedRect.DOScale(
                        new Vector3(0.2f, 1f, 1f), 0.1f))
                    .Append(_checkedRect.DOScale(
                        new Vector3(1f, 1.1f, 1f), 0.1f))
                    .Append(_checkedRect.DOScale(Vector3.one, 0.1f));
                _visualSequence.Insert(0f, squash);
            }
            if (uncheckedCanvas != null)
                _visualSequence.Insert(0.51666665f,
                    uncheckedCanvas.DOFade(0f, 0.0166667f));
            if (weekdayText != null)
                _visualSequence.Insert(0.51666665f,
                    weekdayText.DOColor(CheckedColor, 0.18f));
            _visualSequence.InsertCallback(0.53333336f, () =>
                SetActive(uncheckedDot, false));
            _visualSequence.AppendInterval(
                Mathf.Max(0f, CheckinDurationSeconds -
                    _visualSequence.Duration()));
            _visualSequence.OnComplete(() =>
            {
                SetActive(uncheckedDot, false);
                SetActive(checkedDot, true);
                ResetVisuals();
                _visualSequence = null;
            });
            return CheckinDurationSeconds;
        }

        public void ShowUncheckedDot()
        {
            KillVisualSequence();
            SetActive(uncheckedDot, true);
            SetActive(checkedDot, false);
            SetActive(chest, false);
            ResetVisuals();
        }

        public void HideChest()
        {
            SetActive(chest, false);
        }

        private float PlayReward()
        {
            SetActive(uncheckedDot, false);
            SetActive(checkedDot, false);
            SetActive(chest, true);
            if (chestCanvas != null) chestCanvas.alpha = 1f;
            if (_chestRect != null)
            {
                _chestRect.anchoredPosition = _chestPosition;
                _chestRect.localScale = Vector3.one;
            }

            _visualSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            if (_chestRect != null)
            {
                _visualSequence.Append(
                    _chestRect.DOScale(1.2f, 0.18333331f)
                        .SetEase(Ease.OutQuad));
                _visualSequence.Append(
                    _chestRect.DOScale(0.95f, 0.2f)
                        .SetEase(Ease.InOutQuad));
                _visualSequence.Append(
                    _chestRect.DOScale(1f, 0.18333336f)
                        .SetEase(Ease.OutQuad));
            }
            else
            {
                _visualSequence.AppendInterval(RewardDurationSeconds);
            }
            _visualSequence.OnComplete(() =>
            {
                ResetVisuals();
                _visualSequence = null;
            });
            return RewardDurationSeconds;
        }

        private void CacheVisuals()
        {
            uncheckedCanvas ??= EnsureCanvasGroup(uncheckedDot);
            checkedCanvas ??= EnsureCanvasGroup(checkedDot);
            chestCanvas ??= EnsureCanvasGroup(chest);
            if (_visualsCached) return;

            _uncheckedRect = uncheckedDot != null
                ? uncheckedDot.transform as RectTransform
                : null;
            _checkedRect = checkedDot != null
                ? checkedDot.transform as RectTransform
                : null;
            _chestRect = chest != null
                ? chest.transform as RectTransform
                : null;
            if (_uncheckedRect != null)
                _uncheckedPosition = _uncheckedRect.anchoredPosition;
            if (_checkedRect != null)
                _checkedPosition = _checkedRect.anchoredPosition;
            if (_chestRect != null)
                _chestPosition = _chestRect.anchoredPosition;
            _visualsCached = true;
        }

        private void KillVisualSequence()
        {
            if (_visualSequence != null && _visualSequence.IsActive())
                _visualSequence.Kill(false);
            _visualSequence = null;
        }

        private void ResetVisuals()
        {
            if (uncheckedCanvas != null) uncheckedCanvas.alpha = 1f;
            if (checkedCanvas != null) checkedCanvas.alpha = 1f;
            if (chestCanvas != null) chestCanvas.alpha = 1f;
            ResetTransform(_uncheckedRect, _uncheckedPosition);
            ResetTransform(_checkedRect, _checkedPosition);
            ResetTransform(_chestRect, _chestPosition);
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            if (target == null) return null;
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
        }

        private static void ResetTransform(
            RectTransform target,
            Vector2 position)
        {
            if (target == null) return;
            target.anchoredPosition = position;
            target.localScale = Vector3.one;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
