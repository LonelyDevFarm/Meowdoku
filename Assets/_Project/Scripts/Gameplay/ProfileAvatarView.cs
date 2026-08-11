using Meowdoku.Core.Profile;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ProfileAvatarView : MonoBehaviour
    {
        [SerializeField] private GameObject baseRoot;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private GameObject countBadge;
        [SerializeField] private Text countText;
        [SerializeField] private GameObject redDot;
        [SerializeField] private Sprite[] avatarSprites = new Sprite[0];
        [SerializeField] private Sprite[] frameSprites = new Sprite[0];

        public void Apply(PlayerInfo info)
        {
            if (info == null) return;
            SetInfo(info.AvatarId, info.Frame?.Id ?? 0);
            SetCount(info.Frame?.AcquiredCount ?? -1);
        }

        public void SetInfo(int avatarId, int frameId)
        {
            SetAvatar(avatarId);
            SetFrame(frameId);
        }

        public void SetAvatar(int avatarId)
        {
            int index = avatarId - 1;
            if (avatarImage == null) return;
            avatarImage.sprite = index >= 0 && index < avatarSprites.Length
                ? avatarSprites[index]
                : null;
            avatarImage.enabled = avatarImage.sprite != null;
        }

        public void SetFrame(int frameId)
        {
            int index = FrameIndex(frameId);
            if (frameImage != null)
            {
                frameImage.sprite = index >= 0 && index < frameSprites.Length
                    ? frameSprites[index]
                    : null;
                frameImage.enabled = frameImage.sprite != null;
            }
        }

        public void SetCount(int count)
        {
            if (countText != null) countText.text = count.ToString();
            if (countBadge != null) countBadge.SetActive(count >= 1);
        }

        public void SetBaseVisible(bool shown)
        {
            if (baseRoot != null) baseRoot.SetActive(shown);
        }

        public void SetAvatarVisible(bool shown)
        {
            if (avatarImage != null) avatarImage.gameObject.SetActive(shown);
        }

        public void SetFrameVisible(bool shown)
        {
            if (frameImage != null) frameImage.gameObject.SetActive(shown);
        }

        public void SetRedDot(bool shown)
        {
            if (redDot != null) redDot.SetActive(shown);
        }

        private static int FrameIndex(int frameId)
        {
            if (frameId >= 1 && frameId <= 8) return frameId - 1;
            return frameId == ProfileCatalog.FirstPlaceFrameId ? 8 : -1;
        }
    }
}
