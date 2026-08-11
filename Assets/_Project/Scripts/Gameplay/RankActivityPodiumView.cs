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

        private RankInfo _info;

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

        public void Apply(RankInfo info, int group, int place)
        {
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
