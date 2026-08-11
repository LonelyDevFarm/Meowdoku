using System.Collections.Generic;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Rank;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityHowToPlayPresenter : UIFrameWindow,
        IRankActivityConsumer
    {
        [SerializeField] private GameObject catIcon;
        [SerializeField] private GameObject fishIcon;
        [SerializeField] private GameObject fullReward;
        [SerializeField] private GameObject frameOnlyReward;
        [SerializeField] private Text titleText;
        [SerializeField] private Text clearText;
        [SerializeField] private Text collectText;
        [SerializeField] private Text topText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Text continueText;
        [SerializeField] private Button dismissButton;
        [SerializeField] private LocalizationCatalog localization;

        private RankActivityRuntime _runtime;
        private int _group = RankActivityConfig.GroupCats;

        protected override void OnCreate()
        {
            if (dismissButton != null)
                dismissButton.onClick.AddListener(Close);
            if (localization != null)
                localization.LocaleChanged += RefreshText;
            RefreshText();
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _group = ResolveGroup(parameters);
            ApplyGroup();
            RefreshText();
        }

        protected override bool OnBackRequest()
        {
            Close();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            if (dismissButton != null)
                dismissButton.onClick.RemoveListener(Close);
            if (localization != null)
                localization.LocaleChanged -= RefreshText;
            base.OnDestroyWindow();
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            _runtime = runtime;
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            if (localization == catalog) return;
            if (localization != null)
                localization.LocaleChanged -= RefreshText;
            localization = catalog;
            if (localization != null)
                localization.LocaleChanged += RefreshText;
            RefreshText();
        }

        private int ResolveGroup(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters != null &&
                parameters.TryGetValue("group", out object value))
            {
                try
                {
                    int group = System.Convert.ToInt32(value);
                    if (group >= RankActivityConfig.GroupCats &&
                        group <= RankActivityConfig.GroupFrameOnly)
                        return group;
                }
                catch (System.Exception) { }
            }
            int runtimeGroup = _runtime?.Manager?.Group ?? 0;
            return runtimeGroup >= RankActivityConfig.GroupCats
                ? runtimeGroup
                : RankActivityConfig.GroupCats;
        }

        private void ApplyGroup()
        {
            bool fish = _group == RankActivityConfig.GroupFish;
            bool frameOnly = _group == RankActivityConfig.GroupFrameOnly;
            SetActive(catIcon, !fish);
            SetActive(fishIcon, fish);
            SetActive(fullReward, !frameOnly);
            SetActive(frameOnlyReward, frameOnly);
        }

        private void RefreshText()
        {
            SetText(titleText, "RANK_TITLE", "Leaderboard");
            SetText(clearText, "RANK_HTP_STEP_CLEAR", "Clear main levels");
            SetText(
                collectText,
                _group == RankActivityConfig.GroupFish
                    ? "RANK_HTP_COLLECT_FISH"
                    : "RANK_HTP_FIND_CATS",
                _group == RankActivityConfig.GroupFish
                    ? "Collect fishes to increase your rank"
                    : "Find cats to increase your rank");
            SetText(topText, "RANK_HTP_STEP_TOP", "Top the Leaderboard");
            SetText(
                rewardText,
                _group == RankActivityConfig.GroupFrameOnly
                    ? "RANK_HTP_WIN_FRAMES"
                    : "RANK_HTP_WIN_FRAMES_REWARDS",
                _group == RankActivityConfig.GroupFrameOnly
                    ? "Win exclusive frames"
                    : "Win exclusive frames and rewards");
            SetText(continueText, "RANK_TAP_CONTINUE", "Tap to Continue");
        }

        private void SetText(Text target, string key, string fallback)
        {
            if (target == null) return;
            string value = localization != null
                ? localization.Translate(key)
                : fallback;
            target.text = string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private void Close()
        {
            Owner?.Hide(UiName);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
