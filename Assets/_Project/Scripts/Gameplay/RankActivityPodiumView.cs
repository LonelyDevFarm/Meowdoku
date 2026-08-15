using DG.Tweening;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityPodiumView : MonoBehaviour
    {
        [SerializeField] private ProfileAvatarView avatar;
        [SerializeField] private Text nameText;
        [SerializeField] private Text scoreText;
        [SerializeField] private GameObject catIcon;
        [SerializeField] private GameObject fishIcon;
        [SerializeField] private GameObject chest;
        [SerializeField] private Image chestImage;
        [SerializeField] private Sprite[] chestTiers = new Sprite[0];
        [SerializeField] private Button selfButton;
        [Header("Source Appear presentation")]
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private RectTransform avatarRoot;
        [SerializeField] private CanvasGroup avatarGroup;
        [SerializeField] private RectTransform chestRoot;
        [SerializeField] private CanvasGroup chestGroup;
        [SerializeField, Range(1, 3)] private int place = 1;

        private RankInfo _info;
        private Sequence _introSequence;
        private Vector2 _rootBasePosition;
        private Vector2 _avatarBasePosition;
        private Vector2 _chestBasePosition;
        private bool _basesCaptured;

        public event System.Action SelfRequested;

        private void Awake()
        {
            if (selfButton != null)
                selfButton.onClick.AddListener(HandleSelfRequested);
        }

        private void OnDestroy()
        {
            KillIntro();
            if (selfButton != null)
                selfButton.onClick.RemoveListener(HandleSelfRequested);
        }

        private void OnDisable()
        {
            KillIntro();
            ResetIntroVisuals();
        }

        public void Apply(RankInfo info, int group, int place)
        {
            this.place = Mathf.Clamp(place, 1, 3);
            _info = info;
            bool shown = info != null;
            gameObject.SetActive(shown);
            if (!shown) return;
            avatar?.Apply(info.PlayerInfo);
            if (nameText != null)
                nameText.text = info.PlayerInfo?.Nickname ?? string.Empty;
            if (scoreText != null) scoreText.text = info.Score.ToString();
            bool cat = RankPresentationContract.ScoreIsCat(group);
            SetActive(catIcon, cat);
            SetActive(fishIcon, !cat);
            bool showChest = RankPresentationContract.HasRewardBox(group);
            SetActive(chest, showChest);
            if (showChest && chestImage != null)
            {
                int tier = RankPresentationContract.EntryChestTier(place);
                chestImage.sprite = tier > 0 && tier <= chestTiers.Length
                    ? chestTiers[tier - 1]
                    : null;
                chestImage.enabled = chestImage.sprite != null;
            }
            if (selfButton != null)
                selfButton.gameObject.SetActive(info.IsSelf);
        }

        public void PlayIntro(float delay)
        {
            if (!gameObject.activeInHierarchy) return;
            CaptureBases();
            KillIntro();

            float stagger = Mathf.Max(0f, delay);
            float podiumSettle = 0.3f;
            float avatarStart = stagger + 0.033333335f;
            float avatarPop = avatarStart + 0.13333334f;
            float avatarBounce1 = avatarStart + 0.3f;
            float avatarBounce2 = avatarStart + 0.46666667f;
            float avatarSettle = avatarStart + 0.65f;

            transform.localScale = Vector3.one;
            ((RectTransform)transform).anchoredPosition =
                _rootBasePosition + new Vector2(0f, -200f);
            SetAlpha(contentGroup, 0f);
            if (avatarRoot != null)
            {
                avatarRoot.anchoredPosition =
                    _avatarBasePosition + new Vector2(0f, -316f);
                avatarRoot.localScale = Vector3.zero;
                avatarRoot.localEulerAngles = new Vector3(0f, 0f, -5.73f);
            }
            SetAlpha(avatarGroup, 0f);
            SetAlpha(chestGroup, 0f);

            _introSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _introSequence.Insert(
                stagger,
                ((RectTransform)transform).DOAnchorPosY(
                        _rootBasePosition.y, podiumSettle)
                    .SetEase(Ease.OutCubic));
            if (contentGroup != null)
                _introSequence.Insert(
                    stagger, contentGroup.DOFade(1f, 0.13333334f));
            if (chestGroup != null)
                _introSequence.Insert(
                    stagger + 0.06666667f,
                    chestGroup.DOFade(1f, 0.15f));
            if (avatarRoot != null)
            {
                _introSequence.Insert(
                    avatarStart,
                    avatarRoot.DOAnchorPosY(
                            _avatarBasePosition.y + 70f, 0.13333334f)
                        .SetEase(Ease.OutCubic));
                _introSequence.Insert(
                    avatarPop,
                    avatarRoot.DOAnchorPosY(
                            _avatarBasePosition.y - 8f, 0.16666666f)
                        .SetEase(Ease.InOutSine));
                _introSequence.Insert(
                    avatarBounce1,
                    avatarRoot.DOAnchorPosY(
                            _avatarBasePosition.y + 6f, 0.16666667f)
                        .SetEase(Ease.InOutSine));
                _introSequence.Insert(
                    avatarBounce2,
                    avatarRoot.DOAnchorPosY(
                            _avatarBasePosition.y, 0.18333334f)
                        .SetEase(Ease.InOutSine));
                _introSequence.Insert(
                    avatarStart,
                    avatarRoot.DOScale(1f, 0.13333334f)
                        .SetEase(Ease.OutBack));
                _introSequence.Insert(
                    avatarPop,
                    avatarRoot.DOLocalRotate(
                            new Vector3(0f, 0f, 2.86f), 0.16666666f)
                        .SetEase(Ease.InOutSine));
                _introSequence.Insert(
                    avatarBounce1,
                    avatarRoot.DOLocalRotate(
                            new Vector3(0f, 0f, -2.86f), 0.16666667f)
                        .SetEase(Ease.InOutSine));
                _introSequence.Insert(
                    avatarBounce2,
                    avatarRoot.DOLocalRotate(
                            Vector3.zero, avatarSettle - avatarBounce2)
                        .SetEase(Ease.InOutSine));
            }
            if (avatarGroup != null)
                _introSequence.Insert(
                    avatarStart, avatarGroup.DOFade(1f, 0.08333334f));

            _introSequence.OnComplete(() =>
            {
                _introSequence = null;
                ResetIntroVisuals();
            });
        }

        private void CaptureBases()
        {
            if (_basesCaptured) return;
            _rootBasePosition = ((RectTransform)transform).anchoredPosition;
            if (avatarRoot != null)
                _avatarBasePosition = avatarRoot.anchoredPosition;
            if (chestRoot != null)
                _chestBasePosition = chestRoot.anchoredPosition;
            _basesCaptured = true;
        }

        private void ResetIntroVisuals()
        {
            CaptureBases();
            ((RectTransform)transform).anchoredPosition = _rootBasePosition;
            SetAlpha(contentGroup, 1f);
            if (avatarRoot != null)
            {
                avatarRoot.anchoredPosition = _avatarBasePosition;
                avatarRoot.localScale = Vector3.one;
                avatarRoot.localEulerAngles = Vector3.zero;
            }
            if (chestRoot != null)
                chestRoot.anchoredPosition = _chestBasePosition;
            SetAlpha(avatarGroup, 1f);
            SetAlpha(chestGroup, 1f);
        }

        private void KillIntro()
        {
            if (_introSequence != null && _introSequence.IsActive())
                _introSequence.Kill(false);
            _introSequence = null;
        }

        private static void SetAlpha(CanvasGroup group, float alpha)
        {
            if (group != null) group.alpha = Mathf.Clamp01(alpha);
        }

        private void HandleSelfRequested()
        {
            if (_info?.IsSelf == true) SelfRequested?.Invoke();
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
