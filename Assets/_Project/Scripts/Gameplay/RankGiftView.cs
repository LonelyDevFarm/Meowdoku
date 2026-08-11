using System;
using System.Collections.Generic;
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
        [SerializeField] private Text winText;
        [SerializeField] private GameObject chestRoot;
        [SerializeField] private Image chestImage;
        [SerializeField] private Sprite[] chestTiers = Array.Empty<Sprite>();
        [SerializeField] private ProfileAvatarView[] podiumAvatars =
            Array.Empty<ProfileAvatarView>();
        [SerializeField] private Button collectButton;
        [SerializeField] private Text collectText;
        [SerializeField] private LocalizationCatalog localization;

        public event Action CollectRequested;

        public bool HasBox { get; private set; }

        private void Awake()
        {
            if (collectButton != null)
                collectButton.onClick.AddListener(HandleCollect);
        }

        private void OnDestroy()
        {
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
            HasBox = HasToolItem(request?.Items);
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
        }

        public void SetInteractable(bool interactable)
        {
            if (collectButton != null)
                collectButton.interactable = interactable;
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
            if (collectButton != null && collectButton.interactable)
                CollectRequested?.Invoke();
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
