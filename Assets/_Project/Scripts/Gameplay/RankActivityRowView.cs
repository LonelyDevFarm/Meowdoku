using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityRowView : MonoBehaviour
    {
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

        public event System.Action SelfRequested;

        private void Awake()
        {
            if (selfButton != null)
                selfButton.onClick.AddListener(HandleSelfRequested);
        }

        private void OnDestroy()
        {
            if (selfButton != null)
                selfButton.onClick.RemoveListener(HandleSelfRequested);
        }

        public void Apply(RankInfo info, int group)
        {
            _group = group;
            _info = Clone(info);
            if (info == null)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);
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
            Apply(_info, _group);
        }

        private void HandleSelfRequested()
        {
            if (_info?.IsSelf == true) SelfRequested?.Invoke();
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
