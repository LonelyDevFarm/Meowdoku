using System;
using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankGiftView : MonoBehaviour
    {
        public const float AppearWithBoxDuration = 3.45f;
        public const float AppearWithoutBoxDuration = 3.3666666f;
        public const float OpenNotifyDelay = 0.8834f;
        public const float OpenDuration = 2f;

        [Header("Content")]
        [SerializeField] private Image backdrop;
        [SerializeField] private Text winText;
        [SerializeField] private CanvasGroup winGroup;
        [SerializeField] private GameObject chestRoot;
        [SerializeField] private Image chestImage;
        [SerializeField] private CanvasGroup chestGroup;
        [SerializeField] private RectTransform chestVisual;
        [SerializeField] private Image chestGlow;
        [SerializeField] private Sprite[] chestTiers = Array.Empty<Sprite>();
        [SerializeField] private RectTransform podiumVisual;
        [SerializeField] private CanvasGroup podiumGroup;
        [SerializeField] private RectTransform[] podiumSeats =
            Array.Empty<RectTransform>();
        [SerializeField] private CanvasGroup[] podiumSeatGroups =
            Array.Empty<CanvasGroup>();
        [SerializeField] private ProfileAvatarView[] podiumAvatars =
            Array.Empty<ProfileAvatarView>();
        [SerializeField] private RectTransform[] podiumAvatarVisuals =
            Array.Empty<RectTransform>();
        [SerializeField] private CanvasGroup[] podiumAvatarGroups =
            Array.Empty<CanvasGroup>();
        [SerializeField] private Button collectButton;
        [SerializeField] private Text collectText;
        [SerializeField] private CanvasGroup collectGroup;
        [SerializeField] private RectTransform[] burstRoots =
            Array.Empty<RectTransform>();
        [SerializeField] private Image[] burstGlows = Array.Empty<Image>();
        [SerializeField] private Image[] burstStars = Array.Empty<Image>();
        [SerializeField] private LocalizationCatalog localization;

        public event Action CollectRequested;

        public bool HasBox { get; private set; }

        private Sequence _appearSequence;
        private Sequence _openSequence;
        private Sequence _chestIdle;
        private Vector2 _podiumWithBoxPosition;
        private Vector2 _chestBasePosition;
        private Vector2[] _seatBasePositions;
        private Vector2[] _avatarBasePositions;
        private bool _positionsCaptured;
        private bool _appeared;
        private bool _opening;
        private bool _externallyInteractable;

        private void Awake()
        {
            if (collectButton != null)
                collectButton.onClick.AddListener(HandleCollect);
            CapturePositions();
            ResetImmediate();
        }

        private void OnDisable()
        {
            KillTweens();
            ResetBackdrop();
        }

        private void OnDestroy()
        {
            KillTweens();
            if (collectButton != null)
                collectButton.onClick.RemoveListener(HandleCollect);
        }

        public void Apply(AwardPresentationRequest request)
        {
            int place = ReadInt(request?.DisplayParameters, "place", 1);
            int winCount = ReadInt(
                request?.DisplayParameters,
                "win_count",
                0);
            int group = ReadInt(
                request?.DisplayParameters,
                "group",
                0);
            HasBox = group > 0
                ? RankPresentationContract.HasRewardBox(group)
                : HasToolItem(request?.Items);
            if (chestRoot != null) chestRoot.SetActive(HasBox);
            if (chestImage != null)
            {
                int tier = 4 - Mathf.Clamp(place, 1, 3);
                chestImage.sprite = tier >= 1 && tier <= chestTiers.Length
                    ? chestTiers[tier - 1]
                    : null;
                chestImage.enabled = chestImage.sprite != null;
            }

            string win = Translate(
                    $"RANK_GIFT_WIN_TIMES_{Mathf.Clamp(place, 1, 3)}",
                    WinFallback(place))
                .Replace("%d", Mathf.Max(0, winCount).ToString())
                .Replace("%s", string.Empty);
            if (winText != null)
                winText.text =
                    RankPresentationContract.GodotRichTextToPlainText(win);
            if (collectText != null)
                collectText.text = HasBox
                    ? Translate(
                        "AD_REWARD_RESTORED_COLLECT",
                        "Collect")
                    : Translate("RANK_GIFT_OK", "OK");
            ApplyPodium(request?.DisplayParameters);
            PlayAppear();
        }

        public void SetInteractable(bool interactable)
        {
            _externallyInteractable = interactable;
            RefreshInteractable();
        }

        public void StopImmediate()
        {
            KillTweens();
            ResetImmediate();
        }

        private void ApplyPodium(
            IReadOnlyDictionary<string, object> parameters)
        {
            IReadOnlyList<object> top3 = ReadList(parameters, "top3_infos");
            for (int index = 0; index < podiumAvatars.Length; index++)
            {
                ProfileAvatarView avatar = podiumAvatars[index];
                if (avatar == null) continue;
                PlayerInfo info = top3 != null && index < top3.Count
                    ? top3[index] as PlayerInfo
                    : null;
                avatar.gameObject.SetActive(info != null);
                if (info != null) avatar.Apply(info);
            }
        }

        private void HandleCollect()
        {
            if (collectButton == null || !collectButton.interactable ||
                !_appeared || _opening)
                return;
            _opening = true;
            RefreshInteractable();
            if (!HasBox)
            {
                CollectRequested?.Invoke();
                gameObject.SetActive(false);
                return;
            }
            PlayOpen();
        }

        private void PlayAppear()
        {
            CapturePositions();
            KillTweens();
            ResetVisualsForAppear();
            _appeared = false;
            _opening = false;
            RefreshInteractable();

            float duration = HasBox
                ? AppearWithBoxDuration
                : AppearWithoutBoxDuration;
            _appearSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            if (backdrop != null)
                _appearSequence.Insert(0f,
                    backdrop.DOFade(0.85f, 0.26666668f)
                        .SetEase(Ease.Linear));
            if (winGroup != null)
                _appearSequence.Insert(0f,
                    winGroup.DOFade(1f, 0.26666668f)
                        .SetEase(Ease.Linear));
            if (podiumVisual != null)
                _appearSequence.Insert(0f,
                    podiumVisual.DOScale(1f, 0.5f)
                        .SetEase(Ease.OutCubic));

            PlaySeatAppear(2, 0f);
            PlaySeatAppear(1, 0.06666667f);
            PlaySeatAppear(0, 0.13333334f);
            PlayGoldAvatarAppear();
            float sideAvatarStart = HasBox ? 2.2666667f : 1.5f;
            PlayAvatarFade(1, sideAvatarStart,
                HasBox ? 0.15f : 0.1166667f);
            PlayAvatarFade(2, sideAvatarStart,
                HasBox ? 0.15f : 0.1166667f);
            PlayCelebrationBursts(_appearSequence);

            if (HasBox) PlayChestAppear();
            float collectStart = HasBox ? 2.9999998f : 1.8666667f;
            if (collectGroup != null)
                _appearSequence.Insert(collectStart,
                    collectGroup.DOFade(
                            1f,
                            HasBox ? 0.1999998f : 0.1999999f)
                        .SetEase(Ease.Linear));
            if (_appearSequence.Duration() < duration)
                _appearSequence.AppendInterval(
                    duration - _appearSequence.Duration());
            _appearSequence.OnComplete(() =>
            {
                _appearSequence = null;
                _appeared = true;
                RefreshInteractable();
                if (HasBox) StartChestIdle();
            });
        }

        private void PlayOpen()
        {
            KillAppear();
            KillChestIdle();
            _openSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            if (collectGroup != null)
                _openSequence.Insert(0f,
                    collectGroup.DOFade(0f, 0.16666698f)
                        .SetEase(Ease.Linear));
            if (winGroup != null)
                _openSequence.Insert(0f,
                    winGroup.DOFade(0f, 0.16666698f)
                        .SetEase(Ease.Linear));
            if (podiumGroup != null)
                _openSequence.Insert(0f,
                    podiumGroup.DOFade(0f, 0.2666669f)
                        .SetEase(Ease.Linear));
            if (chestGlow != null)
                _openSequence.Insert(0f,
                    chestGlow.DOFade(0f, 0.26666665f)
                        .SetEase(Ease.Linear));
            if (chestVisual != null)
            {
                _openSequence.Insert(0.06666756f,
                    chestVisual.DOPunchScale(
                            new Vector3(0.18f, -0.12f, 0f),
                            0.45f,
                            4,
                            0.5f)
                        .SetEase(Ease.OutQuad));
                _openSequence.Insert(
                    1.0166678f,
                    chestVisual.DOAnchorPos(
                            _chestBasePosition + new Vector2(0f, -200f),
                            0.4166666f)
                        .SetEase(Ease.InQuad));
            }
            _openSequence.InsertCallback(0.9f, PlayOpenBurst);
            if (chestGroup != null)
                _openSequence.Insert(1.3166676f,
                    chestGroup.DOFade(0f, 0.25f).SetEase(Ease.Linear));
            _openSequence.InsertCallback(OpenNotifyDelay,
                () => CollectRequested?.Invoke());
            _openSequence.AppendInterval(Mathf.Max(
                0f,
                OpenDuration - _openSequence.Duration()));
            _openSequence.OnComplete(() =>
            {
                _openSequence = null;
                gameObject.SetActive(false);
            });
        }

        private void PlaySeatAppear(int index, float start)
        {
            if (index < 0 || index >= podiumSeats.Length) return;
            RectTransform seat = podiumSeats[index];
            CanvasGroup group = index < podiumSeatGroups.Length
                ? podiumSeatGroups[index]
                : null;
            if (seat == null) return;
            if (group != null)
                _appearSequence.Insert(start,
                    group.DOFade(1f, 0.1f).SetEase(Ease.Linear));
            if (index == 1)
                _appearSequence.Insert(0f,
                    seat.DOAnchorPosX(_seatBasePositions[index].x, 0.5f)
                        .SetEase(Ease.OutCubic));
            else if (index == 2)
                _appearSequence.Insert(0f,
                    seat.DOAnchorPosX(_seatBasePositions[index].x, 0.5f)
                        .SetEase(Ease.OutCubic));
            else
                _appearSequence.Insert(0f,
                    seat.DOScale(1f, 0.5f).SetEase(Ease.OutCubic));
        }

        private void PlayGoldAvatarAppear()
        {
            if (podiumAvatarVisuals.Length == 0 ||
                podiumAvatarVisuals[0] == null)
                return;
            RectTransform avatar = podiumAvatarVisuals[0];
            CanvasGroup group = podiumAvatarGroups.Length > 0
                ? podiumAvatarGroups[0]
                : null;
            if (group != null)
                _appearSequence.Insert(0.5f,
                    group.DOFade(1f, HasBox ? 0.1333333f : 0.25f)
                        .SetEase(Ease.Linear));
            _appearSequence.Insert(0.5f,
                avatar.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            Vector2 basePosition = _avatarBasePositions[0];
            avatar.anchoredPosition = basePosition +
                                      new Vector2(0f, HasBox ? -208.5f : -252.5f);
            Sequence bounce = DOTween.Sequence();
            bounce.Append(avatar.DOAnchorPosY(
                    basePosition.y + 12.5f,
                    0.25f)
                .SetEase(Ease.OutQuad));
            bounce.Append(avatar.DOAnchorPosY(
                    basePosition.y + 10f,
                    HasBox ? 0.0833332f : 0.1666666f)
                .SetEase(Ease.InOutSine));
            bounce.Append(avatar.DOAnchorPosY(
                    basePosition.y + 15f,
                    HasBox ? 0.0833333f : 0.1833333f)
                .SetEase(Ease.InOutSine));
            bounce.Append(avatar.DOAnchorPosY(
                    basePosition.y,
                    0.1f)
                .SetEase(Ease.OutQuad));
            _appearSequence.Insert(0.5f, bounce);
        }

        private void PlayAvatarFade(int index, float start, float duration)
        {
            if (index < 0 || index >= podiumAvatarGroups.Length) return;
            CanvasGroup group = podiumAvatarGroups[index];
            if (group != null)
                _appearSequence.Insert(start,
                    group.DOFade(1f, duration).SetEase(Ease.Linear));
        }

        private void PlayChestAppear()
        {
            if (chestRoot != null) chestRoot.SetActive(false);
            _appearSequence.InsertCallback(2.483333f, () =>
            {
                if (chestRoot != null) chestRoot.SetActive(true);
            });
            if (chestGroup != null)
                _appearSequence.Insert(2.4999998f,
                    chestGroup.DOFade(1f, 0.25f).SetEase(Ease.Linear));
            if (chestVisual == null) return;
            chestVisual.localScale = new Vector3(0.375f, 0.3f, 1f);
            chestVisual.anchoredPosition =
                _chestBasePosition + new Vector2(0f, -251f);
            _appearSequence.Insert(2.4999998f,
                chestVisual.DOAnchorPos(
                        _chestBasePosition + new Vector2(0f, 86.83f),
                        0.2f)
                    .SetEase(Ease.OutQuad));
            _appearSequence.Insert(2.6999998f,
                chestVisual.DOAnchorPos(
                        _chestBasePosition + new Vector2(0f, -14.715f),
                        0.25f)
                    .SetEase(Ease.InOutSine));
            _appearSequence.Insert(2.9499998f,
                chestVisual.DOAnchorPos(_chestBasePosition, 0.1333335f)
                    .SetEase(Ease.OutQuad));
            _appearSequence.Insert(2.4999998f,
                chestVisual.DOScaleX(0.75f, 0.3333335f)
                    .SetEase(Ease.OutCubic));
            _appearSequence.Insert(2.4999998f,
                chestVisual.DOScaleY(0.75f, 0.45f)
                    .SetEase(Ease.OutCubic));
        }

        private void PlayCelebrationBursts(Sequence sequence)
        {
            float[] starts = { 0.6166667f, 0.9833334f, 1.2166667f, 1.35f };
            for (int index = 0; index < burstGlows.Length; index++)
            {
                Image glow = burstGlows[index];
                if (glow == null) continue;
                float start = starts[Mathf.Min(index, starts.Length - 1)];
                glow.gameObject.SetActive(true);
                SetAlpha(glow, 0f);
                glow.rectTransform.localScale = Vector3.zero;
                sequence.Insert(start,
                    glow.DOFade(0.75f, 0.08f).SetEase(Ease.OutQuad));
                sequence.Insert(start,
                    glow.rectTransform.DOScale(1.2f, 0.45f)
                        .SetEase(Ease.OutCubic));
                sequence.Insert(start + 0.12f,
                    glow.DOFade(0f, 0.33f).SetEase(Ease.Linear));
            }
            int perBurst = burstRoots.Length > 0
                ? Mathf.Max(1, burstStars.Length / burstRoots.Length)
                : burstStars.Length;
            for (int index = 0; index < burstStars.Length; index++)
            {
                Image star = burstStars[index];
                if (star == null) continue;
                int groupIndex = Mathf.Min(
                    starts.Length - 1,
                    index / Mathf.Max(1, perBurst));
                int localIndex = index % Mathf.Max(1, perBurst);
                float start = starts[groupIndex] + localIndex * 0.008f;
                float angle = localIndex * Mathf.PI * 2f /
                              Mathf.Max(1, perBurst);
                Vector2 target = new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)) * (90f + 18f * (localIndex % 2));
                star.gameObject.SetActive(true);
                star.rectTransform.anchoredPosition = Vector2.zero;
                star.rectTransform.localScale = Vector3.zero;
                SetAlpha(star, 0f);
                sequence.Insert(start,
                    star.DOFade(1f, 0.05f).SetEase(Ease.Linear));
                sequence.Insert(start,
                    star.rectTransform.DOAnchorPos(target, 0.6f)
                        .SetEase(Ease.OutCubic));
                sequence.Insert(start,
                    star.rectTransform.DOScale(0.6f, 0.22f)
                        .SetEase(Ease.OutBack));
                sequence.Insert(start + 0.3f,
                    star.DOFade(0f, 0.3f).SetEase(Ease.Linear));
            }
        }

        private void PlayOpenBurst()
        {
            for (int index = 0; index < burstGlows.Length; index++)
            {
                Image glow = burstGlows[index];
                if (glow == null) continue;
                glow.gameObject.SetActive(index == 0);
                if (index == 0)
                {
                    glow.rectTransform.localScale = Vector3.zero;
                    SetAlpha(glow, 0.8f);
                    glow.rectTransform.DOScale(1.5f, 0.5f)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true)
                        .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                    glow.DOFade(0f, 0.5f)
                        .SetEase(Ease.Linear)
                        .SetUpdate(true)
                        .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                }
            }
            int count = Mathf.Min(12, burstStars.Length);
            for (int index = 0; index < count; index++)
            {
                Image star = burstStars[index];
                if (star == null) continue;
                float angle = index * Mathf.PI * 2f / Mathf.Max(1, count);
                Vector2 target = new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)) * (150f + 20f * (index % 2));
                star.gameObject.SetActive(true);
                star.rectTransform.anchoredPosition = Vector2.zero;
                star.rectTransform.localScale = Vector3.zero;
                SetAlpha(star, 1f);
                star.rectTransform.DOAnchorPos(target, 0.6f)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                star.rectTransform.DOScale(0.65f, 0.22f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                star.DOFade(0f, 0.3f)
                    .SetDelay(0.3f)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        private void StartChestIdle()
        {
            KillChestIdle();
            if (chestVisual == null) return;
            _chestIdle = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _chestIdle.Append(
                chestVisual.DOScale(new Vector3(0.78f, 0.72f, 1f), 0.45f)
                    .SetEase(Ease.InOutSine));
            _chestIdle.Append(
                chestVisual.DOScale(new Vector3(0.75f, 0.75f, 1f), 0.45f)
                    .SetEase(Ease.InOutSine));
            _chestIdle.SetLoops(-1, LoopType.Restart);
            if (chestGlow != null)
                chestGlow.rectTransform.DORotate(
                        new Vector3(0f, 0f, 360f),
                        6f,
                        RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void CapturePositions()
        {
            if (_positionsCaptured) return;
            _podiumWithBoxPosition = podiumVisual != null
                ? podiumVisual.anchoredPosition
                : Vector2.zero;
            _chestBasePosition = chestVisual != null
                ? chestVisual.anchoredPosition
                : Vector2.zero;
            _seatBasePositions = Capture(podiumSeats);
            _avatarBasePositions = Capture(podiumAvatarVisuals);
            _positionsCaptured = true;
        }

        private static Vector2[] Capture(RectTransform[] values)
        {
            var result = new Vector2[values?.Length ?? 0];
            for (int index = 0; index < result.Length; index++)
                if (values[index] != null)
                    result[index] = values[index].anchoredPosition;
            return result;
        }

        private void ResetVisualsForAppear()
        {
            if (backdrop != null) SetAlpha(backdrop, 0f);
            if (winGroup != null) winGroup.alpha = 0f;
            if (collectGroup != null) collectGroup.alpha = 0f;
            if (podiumGroup != null) podiumGroup.alpha = 1f;
            if (podiumVisual != null)
            {
                podiumVisual.anchoredPosition = _podiumWithBoxPosition +
                    new Vector2(0f, HasBox ? 0f : -300f);
                podiumVisual.localScale = Vector3.one * 2f;
            }
            for (int index = 0; index < podiumSeats.Length; index++)
            {
                RectTransform seat = podiumSeats[index];
                if (seat == null) continue;
                seat.anchoredPosition = _seatBasePositions[index];
                seat.localScale = index == 0 ? Vector3.one * 2f : Vector3.one;
                if (index == 1)
                    seat.anchoredPosition += new Vector2(-310f, 0f);
                else if (index == 2)
                    seat.anchoredPosition += new Vector2(320f, 0f);
            }
            for (int index = 0; index < podiumSeatGroups.Length; index++)
                if (podiumSeatGroups[index] != null)
                    podiumSeatGroups[index].alpha = 0f;
            for (int index = 0; index < podiumAvatarVisuals.Length; index++)
            {
                RectTransform avatar = podiumAvatarVisuals[index];
                if (avatar == null) continue;
                avatar.anchoredPosition = _avatarBasePositions[index];
                avatar.localScale = index == 0
                    ? Vector3.one * (HasBox ? 0f : 0.5f)
                    : Vector3.one;
            }
            for (int index = 0; index < podiumAvatarGroups.Length; index++)
                if (podiumAvatarGroups[index] != null)
                    podiumAvatarGroups[index].alpha = 0f;
            if (chestRoot != null) chestRoot.SetActive(HasBox);
            if (chestGroup != null) chestGroup.alpha = 0f;
            if (chestVisual != null)
            {
                chestVisual.anchoredPosition = _chestBasePosition;
                chestVisual.localScale = Vector3.one * 0.75f;
            }
            if (chestGlow != null)
            {
                chestGlow.rectTransform.localRotation = Quaternion.identity;
                SetAlpha(chestGlow, 1f);
            }
            ResetBursts();
        }

        private void ResetImmediate()
        {
            CapturePositions();
            _appeared = false;
            _opening = false;
            _externallyInteractable = false;
            ResetBackdrop();
            RefreshInteractable();
            ResetBursts();
        }

        private void ResetBackdrop()
        {
            if (backdrop != null) SetAlpha(backdrop, 0.72f);
        }

        private void ResetBursts()
        {
            for (int index = 0; index < burstGlows.Length; index++)
                ResetImage(burstGlows[index]);
            for (int index = 0; index < burstStars.Length; index++)
                ResetImage(burstStars[index]);
        }

        private static void ResetImage(Image image)
        {
            if (image == null) return;
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.rectTransform.localScale = Vector3.zero;
            SetAlpha(image, 0f);
            image.gameObject.SetActive(false);
        }

        private void RefreshInteractable()
        {
            if (collectButton != null)
                collectButton.interactable = _externallyInteractable &&
                                             _appeared &&
                                             !_opening;
        }

        private void KillTweens()
        {
            KillAppear();
            if (_openSequence != null && _openSequence.IsActive())
                _openSequence.Kill(false);
            _openSequence = null;
            KillChestIdle();
            if (chestGlow != null) chestGlow.rectTransform.DOKill(false);
            for (int index = 0; index < burstGlows.Length; index++)
            {
                if (burstGlows[index] == null) continue;
                burstGlows[index].DOKill(false);
                burstGlows[index].rectTransform.DOKill(false);
            }
            for (int index = 0; index < burstStars.Length; index++)
            {
                if (burstStars[index] == null) continue;
                burstStars[index].DOKill(false);
                burstStars[index].rectTransform.DOKill(false);
            }
        }

        private void KillAppear()
        {
            if (_appearSequence != null && _appearSequence.IsActive())
                _appearSequence.Kill(false);
            _appearSequence = null;
        }

        private void KillChestIdle()
        {
            if (_chestIdle != null && _chestIdle.IsActive())
                _chestIdle.Kill(false);
            _chestIdle = null;
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            Color color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static bool HasToolItem(IReadOnlyList<AwardItem> items)
        {
            if (items == null) return false;
            for (int index = 0; index < items.Count; index++)
                if (items[index]?.Category == AwardCategory.Tool)
                    return true;
            return false;
        }

        private static IReadOnlyList<object> ReadList(
            IReadOnlyDictionary<string, object> values,
            string key)
        {
            return values != null &&
                   values.TryGetValue(key, out object value)
                ? value as IReadOnlyList<object>
                : null;
        }

        private static int ReadInt(
            IReadOnlyDictionary<string, object> values,
            string key,
            int fallback)
        {
            if (values == null ||
                !values.TryGetValue(key, out object value))
                return fallback;
            try { return Convert.ToInt32(value); }
            catch (Exception) { return fallback; }
        }

        private static string WinFallback(int place)
        {
            return place switch
            {
                1 => "You've won 1st place %d times!",
                2 => "You've won 2nd place %d times!",
                _ => "You've won 3rd place %d times!"
            };
        }
    }
}
