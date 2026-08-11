using DG.Tweening;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    public enum RankActivityRowIntro
    {
        Appear1 = 1,
        Appear2 = 2,
        Appear3 = 3
    }

    [DisallowMultipleComponent]
    public sealed class RankActivityRowView : MonoBehaviour
    {
        public const float Appear1Duration = 0.36666667f;
        public const float Appear2Duration = 0.42151412f;
        public const float Appear3Duration = 0.3f;
        public const float IntroFadeDuration = 0.06666667f;
        public const float PopFadeDuration = 0.16666667f;
        public const float ShadowFadeDuration = 0.15f;

        private const float Appear1StartX = 1100f;
        private const float Appear2StartY = -200f;
        private const float Appear2PeakY = 20f;
        private const float Appear2SettleY = -5f;
        private const float Appear2PeakTime = 0.13333334f;
        private const float Appear2SettleTime = 0.3f;
        private const float PopPeakTime = 0.16666667f;
        private const float ShadowNormalCenterY = -84f;
        private const float ShadowFlippedCenterY = -93.55f;

        [Header("Source presentation")]
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private Image selfShadow;
        [SerializeField] private RankActivityRowCelebrationView celebration;
        [SerializeField] private Image background;
        [SerializeField] private Sprite normalBackground;
        [SerializeField] private Sprite selfBackground;
        [SerializeField] private Image bigMedal;
        [SerializeField] private Sprite[] bigMedals = new Sprite[0];
        [SerializeField] private Image badge;
        [SerializeField] private Sprite[] badges = new Sprite[0];
        [SerializeField] private Text badgeText;
        [SerializeField] private Text rankText;
        [SerializeField] private ProfileAvatarView avatar;
        [SerializeField] private Text nameText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Image scoreBackground;
        [SerializeField] private Sprite normalScoreBackground;
        [SerializeField] private Sprite selfScoreBackground;
        [SerializeField] private GameObject catIcon;
        [SerializeField] private GameObject fishIcon;
        [SerializeField] private GameObject chest;
        [SerializeField] private Image chestImage;
        [SerializeField] private Sprite[] chestTiers = new Sprite[0];
        [SerializeField] private Button selfButton;

        private RankInfo _info;
        private int _group;
        private Sequence _introSequence;
        private Sequence _riseSequence;
        private Tween _shadowTween;
        private Vector2 _visualBasePosition;
        private bool _visualBaseCaptured;
        private bool _shadowShown;
        private bool _shadowFlipped;

        public event System.Action SelfRequested;

        public CanvasGroup PresentationGroup => contentGroup;
        public RankActivityRowCelebrationView Celebration => celebration;
        public bool IsSelfShadowShown => _shadowShown;
        public bool IsSelfShadowFlipped => _shadowFlipped;

        private void Awake()
        {
            if (selfButton != null)
                selfButton.onClick.AddListener(HandleSelfRequested);
        }

        private void OnDestroy()
        {
            KillPresentationTweens();
            if (selfButton != null)
                selfButton.onClick.RemoveListener(HandleSelfRequested);
        }

        private void OnDisable()
        {
            KillPresentationTweens();
            HideSelfShadowImmediate();
            ResetVisualImmediate();
        }

        public void Apply(RankInfo info, int group)
        {
            ApplyInternal(info, group, true);
        }

        public void ApplyPreservingPresentation(RankInfo info, int group)
        {
            ApplyInternal(info, group, false);
        }

        private void ApplyInternal(
            RankInfo info,
            int group,
            bool resetPresentation)
        {
            _group = group;
            _info = Clone(info);
            if (resetPresentation)
            {
                HideSelfShadowImmediate();
                ShowStatic();
            }
            if (info == null)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
            Render(info, group);
        }

        private void Render(RankInfo info, int group)
        {
            bool self = info.IsSelf;
            bool top3 = info.Rank >= 1 && info.Rank <= 3;
            bool scoreIsCat = RankPresentationContract.ScoreIsCat(group);
            bool hasChest = RankPresentationContract.HasRewardBox(group);
            if (background != null)
                background.sprite = self ? selfBackground : normalBackground;
            if (scoreBackground != null)
                scoreBackground.sprite = self
                    ? selfScoreBackground
                    : normalScoreBackground;
            Color color = self
                ? new Color(0.774f, 0.362f, 0.237f, 1f)
                : new Color(0.577f, 0.352f, 0.352f, 1f);
            SetText(nameText, info.PlayerInfo?.Nickname ?? string.Empty, color);
            SetText(scoreText, info.Score.ToString(), color);
            SetActive(catIcon, scoreIsCat);
            SetActive(fishIcon, !scoreIsCat);
            celebration?.SetCollectionIsCat(scoreIsCat);
            avatar?.Apply(info.PlayerInfo);

            SetActive(bigMedal != null ? bigMedal.gameObject : null, top3);
            SetActive(badge != null ? badge.gameObject : null, top3);
            SetActive(rankText != null ? rankText.gameObject : null, !top3);
            if (top3)
            {
                int index = info.Rank - 1;
                SetSprite(bigMedal, bigMedals, index);
                SetSprite(badge, badges, index);
                SetText(badgeText, info.Rank.ToString(), Color.white);
            }
            else
            {
                string rank = self && info.Score <= 0
                    ? "-"
                    : info.Rank >= 1 ? info.Rank.ToString() : "-";
                SetText(rankText, rank, color);
            }

            bool showChest = top3 && hasChest;
            SetActive(chest, showChest);
            if (showChest && chestImage != null)
            {
                int tier = RankPresentationContract.EntryChestTier(info.Rank);
                SetSprite(chestImage, chestTiers, tier - 1);
            }
            if (selfButton != null)
                selfButton.gameObject.SetActive(self);
        }

        public void PlayIntro(
            RankActivityRowIntro intro,
            float delay = 0f)
        {
            if (!gameObject.activeInHierarchy || visualRoot == null ||
                contentGroup == null)
                return;

            CaptureVisualBase();
            KillIntro();
            KillShadowTween();
            delay = Mathf.Max(0f, delay);
            visualRoot.anchoredPosition = _visualBasePosition;
            visualRoot.localScale = Vector3.one;
            SetPresentationAlpha(0f);
            SetShadowAlpha(0f);

            _introSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            switch (intro)
            {
                case RankActivityRowIntro.Appear2:
                    visualRoot.anchoredPosition = _visualBasePosition +
                        new Vector2(0f, Appear2StartY);
                    _introSequence.Insert(
                        delay,
                        visualRoot.DOAnchorPosY(
                                _visualBasePosition.y + Appear2PeakY,
                                Appear2PeakTime)
                            .SetEase(Ease.OutCubic));
                    _introSequence.Insert(
                        delay + Appear2PeakTime,
                        visualRoot.DOAnchorPosY(
                                _visualBasePosition.y + Appear2SettleY,
                                Appear2SettleTime - Appear2PeakTime)
                            .SetEase(Ease.InOutSine));
                    _introSequence.Insert(
                        delay + Appear2SettleTime,
                        visualRoot.DOAnchorPosY(
                                _visualBasePosition.y,
                                Appear2Duration - Appear2SettleTime)
                            .SetEase(Ease.InOutSine));
                    InsertFade(delay, IntroFadeDuration);
                    break;

                case RankActivityRowIntro.Appear3:
                    visualRoot.localScale = Vector3.one * 0.6f;
                    _introSequence.Insert(
                        delay,
                        visualRoot.DOScale(1.05f, PopPeakTime)
                            .SetEase(Ease.OutCubic));
                    _introSequence.Insert(
                        delay + PopPeakTime,
                        visualRoot.DOScale(
                                1f,
                                Appear3Duration - PopPeakTime)
                            .SetEase(Ease.InOutSine));
                    InsertFade(delay, PopFadeDuration);
                    break;

                default:
                    visualRoot.anchoredPosition = _visualBasePosition +
                        new Vector2(Appear1StartX, 0f);
                    _introSequence.Insert(
                        delay,
                        visualRoot.DOAnchorPosX(
                                _visualBasePosition.x,
                                Appear1Duration)
                            .SetEase(Ease.OutCubic));
                    InsertFade(delay, IntroFadeDuration);
                    break;
            }

            _introSequence.OnComplete(() =>
            {
                _introSequence = null;
                ResetVisualImmediate();
            });
        }

        public void ShowStatic()
        {
            KillIntro();
            KillRise();
            celebration?.Stop();
            ResetVisualImmediate();
        }

        public void PlayCollection()
        {
            celebration?.PlayCollection();
        }

        public void PlayArrow(float fadeDuration)
        {
            celebration?.PlayArrow(fadeDuration);
        }

        public void PlayLift()
        {
            if (!gameObject.activeInHierarchy || visualRoot == null) return;
            CaptureVisualBase();
            KillIntro();
            KillRise();
            visualRoot.localScale = Vector3.one;
            _riseSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _riseSequence.Append(
                visualRoot.DOScale(
                        1.05f,
                        RankActivityRowCelebrationView.RiseUpDuration)
                    .SetEase(Ease.OutCubic));
            _riseSequence.OnComplete(() =>
            {
                _riseSequence = null;
                if (visualRoot != null)
                    visualRoot.localScale = Vector3.one * 1.05f;
            });
        }

        public void PlayRiseIdle()
        {
            KillRise();
            if (visualRoot != null)
                visualRoot.localScale = Vector3.one * 1.05f;
            celebration?.PlayRiseIdle();
        }

        public void PlayDrop()
        {
            celebration?.BeginRiseDown();
            if (!gameObject.activeInHierarchy || visualRoot == null) return;
            KillRise();
            _riseSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _riseSequence.Insert(
                0f,
                visualRoot.DOScale(1.08f, 0.13333334f)
                    .SetEase(Ease.OutCubic));
            _riseSequence.Insert(
                0.13333334f,
                visualRoot.DOScale(0.95f, 0.1f)
                    .SetEase(Ease.InCubic));
            _riseSequence.Insert(
                0.23333335f,
                visualRoot.DOScale(1f, 0.1f)
                    .SetEase(Ease.OutCubic));
            _riseSequence.OnComplete(() =>
            {
                _riseSequence = null;
                if (visualRoot != null)
                    visualRoot.localScale = Vector3.one;
            });
        }

        public void HideArrow()
        {
            celebration?.HideArrow();
        }

        public void SetPresentationAlpha(float alpha)
        {
            if (contentGroup != null)
                contentGroup.alpha = Mathf.Clamp01(alpha);
        }

        public void SetSelfShadow(bool shown, bool flipVertical = false)
        {
            shown &= _info?.IsSelf == true;
            if (shown)
                ConfigureShadow(flipVertical);
            if (selfShadow == null || shown == _shadowShown) return;

            _shadowShown = shown;
            KillShadowTween();
            if (shown)
            {
                selfShadow.gameObject.SetActive(true);
                _shadowTween = selfShadow.DOFade(1f, ShadowFadeDuration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                    .OnComplete(() => _shadowTween = null);
                return;
            }

            _shadowTween = selfShadow.DOFade(0f, ShadowFadeDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    _shadowTween = null;
                    if (!_shadowShown && selfShadow != null)
                        selfShadow.gameObject.SetActive(false);
                });
        }

        public void SetScore(int score)
        {
            if (_info == null) return;
            _info.Score = Mathf.Max(0, score);
            if (scoreText != null) scoreText.text = _info.Score.ToString();
        }

        public void SetRank(int rank)
        {
            if (_info == null) return;
            _info.Rank = rank;
            ApplyInternal(_info, _group, false);
        }

        private void HandleSelfRequested()
        {
            if (_info?.IsSelf == true) SelfRequested?.Invoke();
        }

        private void InsertFade(float delay, float duration)
        {
            _introSequence.Insert(
                delay,
                contentGroup.DOFade(1f, duration).SetEase(Ease.Linear));
            if (selfShadow != null && _shadowShown)
            {
                selfShadow.gameObject.SetActive(true);
                _introSequence.Insert(
                    delay,
                    selfShadow.DOFade(1f, duration).SetEase(Ease.Linear));
            }
        }

        private void CaptureVisualBase()
        {
            if (_visualBaseCaptured || visualRoot == null) return;
            _visualBasePosition = visualRoot.anchoredPosition;
            _visualBaseCaptured = true;
        }

        private void ResetVisualImmediate()
        {
            if (visualRoot != null)
            {
                CaptureVisualBase();
                visualRoot.anchoredPosition = _visualBasePosition;
                visualRoot.localScale = Vector3.one;
            }
            SetPresentationAlpha(1f);
            SetShadowAlpha(_shadowShown ? 1f : 0f);
        }

        private void ConfigureShadow(bool flipVertical)
        {
            if (selfShadow == null) return;
            _shadowFlipped = flipVertical;
            RectTransform rect = selfShadow.rectTransform;
            Vector3 scale = rect.localScale;
            scale.x = Mathf.Abs(scale.x) <= Mathf.Epsilon ? 1f :
                Mathf.Abs(scale.x);
            scale.y = flipVertical ? -1f : 1f;
            scale.z = 1f;
            rect.localScale = scale;
            Vector2 position = rect.anchoredPosition;
            position.y = flipVertical
                ? ShadowFlippedCenterY
                : ShadowNormalCenterY;
            rect.anchoredPosition = position;
        }

        private void HideSelfShadowImmediate()
        {
            KillShadowTween();
            _shadowShown = false;
            _shadowFlipped = false;
            if (selfShadow == null) return;
            SetShadowAlpha(0f);
            selfShadow.gameObject.SetActive(false);
        }

        private void SetShadowAlpha(float alpha)
        {
            if (selfShadow == null) return;
            Color color = selfShadow.color;
            color.a = Mathf.Clamp01(alpha);
            selfShadow.color = color;
        }

        private void KillPresentationTweens()
        {
            KillIntro();
            KillRise();
            KillShadowTween();
            celebration?.Stop();
        }

        private void KillIntro()
        {
            if (_introSequence != null && _introSequence.IsActive())
                _introSequence.Kill(false);
            _introSequence = null;
        }

        private void KillRise()
        {
            if (_riseSequence != null && _riseSequence.IsActive())
                _riseSequence.Kill(false);
            _riseSequence = null;
        }

        private void KillShadowTween()
        {
            if (_shadowTween != null && _shadowTween.IsActive())
                _shadowTween.Kill(false);
            _shadowTween = null;
        }

        private static void SetText(Text target, string value, Color color)
        {
            if (target == null) return;
            target.text = value ?? string.Empty;
            target.color = color;
        }

        private static void SetSprite(
            Image image,
            Sprite[] sprites,
            int index)
        {
            if (image == null) return;
            image.sprite = sprites != null && index >= 0 && index < sprites.Length
                ? sprites[index]
                : null;
            image.enabled = image.sprite != null;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private static RankInfo Clone(RankInfo source)
        {
            return source == null ? null : new RankInfo
            {
                PlayerInfo = source.PlayerInfo,
                Rank = source.Rank,
                Score = source.Score,
                AwardId = source.AwardId
            };
        }
    }
}
