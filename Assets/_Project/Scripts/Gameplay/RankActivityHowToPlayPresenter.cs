using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
        [Header("Source Appear presentation")]
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private RectTransform step;
        [SerializeField] private CanvasGroup stepGroup;
        [SerializeField] private CanvasGroup clearGroup;
        [SerializeField] private RectTransform arrowToCollect;
        [SerializeField] private RectTransform catIconTransform;
        [SerializeField] private CanvasGroup catIconGroup;
        [SerializeField] private RectTransform fishIconTransform;
        [SerializeField] private CanvasGroup fishIconGroup;
        [SerializeField] private CanvasGroup collectGlowGroup;
        [SerializeField] private CanvasGroup collectTextGroup;
        [SerializeField] private RectTransform arrowToRank;
        [SerializeField] private RectTransform rankList;
        [SerializeField] private CanvasGroup rankListGroup;
        [SerializeField] private CanvasGroup topGroup;
        [SerializeField] private RectTransform arrowToReward;
        [SerializeField] private RectTransform fullChest;
        [SerializeField] private CanvasGroup fullChestGroup;
        [SerializeField] private RectTransform fullAvatar;
        [SerializeField] private CanvasGroup fullAvatarGroup;
        [SerializeField] private RectTransform frameOnlyAvatar;
        [SerializeField] private CanvasGroup frameOnlyAvatarGroup;
        [SerializeField] private Image rewardGlow;
        [SerializeField] private CanvasGroup rewardTextGroup;
        [SerializeField] private CanvasGroup continueGroup;

        private RankActivityRuntime _runtime;
        private int _group = RankActivityConfig.GroupCats;
        private Sequence _introSequence;
        private bool _basesCaptured;
        private Vector3 _arrowToCollectScale;
        private Vector3 _arrowToRankScale;
        private Vector3 _arrowToRewardScale;
        private Vector3 _arrowToCollectRotation;
        private Vector3 _arrowToRankRotation;
        private Vector3 _arrowToRewardRotation;

        internal bool IntroPlayingForTests =>
            _introSequence != null && _introSequence.IsActive();

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
            PlayIntro();
        }

        protected override IEnumerator OnHide()
        {
            StopIntro(true);
            yield break;
        }

        protected override bool OnBackRequest()
        {
            Close();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            StopIntro(false);
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

        private void PlayIntro()
        {
            StopIntro(false);
            CaptureBases();
            PrepareIntroVisuals();

            _introSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            InsertFade(titleGroup, 0f, 0.05f);
            InsertPop(step, 0f, 0.15f, 0.28333333f);
            InsertFade(stepGroup, 0f, 0.083333336f);
            InsertFade(clearGroup, 0.15f, 0.05f);
            InsertArrow(
                arrowToCollect,
                _arrowToCollectScale,
                _arrowToCollectRotation,
                0.083333336f,
                0.31666667f,
                -17.1887f);

            bool fish = _group == RankActivityConfig.GroupFish;
            RectTransform collectIcon = fish
                ? fishIconTransform
                : catIconTransform;
            CanvasGroup collectIconGroup = fish
                ? fishIconGroup
                : catIconGroup;
            InsertPop(collectIcon, 0.23333333f, 0.38333333f, 0.51666665f);
            InsertFade(collectIconGroup, 0.23333333f, 0.08333333f);
            InsertFade(collectGlowGroup, 0.26666665f, 0.2166667f);
            InsertFade(collectTextGroup, 0.38333333f, 0.05f);
            InsertArrow(
                arrowToRank,
                _arrowToRankScale,
                _arrowToRankRotation,
                0.31666666f,
                0.55f,
                17.1887f);
            InsertPop(rankList, 0.46666667f, 0.6166667f, 0.75f);
            InsertFade(rankListGroup, 0.46666667f, 0.08333328f);
            InsertFade(topGroup, 0.6166667f, 0.05f);
            InsertArrow(
                arrowToReward,
                _arrowToRewardScale,
                _arrowToRewardRotation,
                0.54999995f,
                0.7833333f,
                -17.1887f);

            if (_group == RankActivityConfig.GroupFrameOnly)
            {
                InsertPop(
                    frameOnlyAvatar, 0.7f, 0.85f, 0.9833335f);
                InsertFade(
                    frameOnlyAvatarGroup, 0.7f, 0.08333334f);
            }
            else
            {
                InsertPop(fullChest, 0.7f, 0.85f, 0.9833335f);
                InsertFade(fullChestGroup, 0.7f, 0.08333334f);
                InsertPop(fullAvatar, 0.7333334f, 0.8833335f, 1.0166669f);
                InsertFade(fullAvatarGroup, 0.7333334f, 0.08333326f);
            }
            if (rewardGlow != null)
            {
                _introSequence.Insert(
                    0.8f,
                    rewardGlow.DOFade(0.9f, 0.0833334f)
                        .SetEase(Ease.Linear));
                _introSequence.Insert(
                    0.8f,
                    rewardGlow.rectTransform.DOScale(1.12f, 0.25f)
                        .SetEase(Ease.OutCubic));
                _introSequence.Insert(
                    1.05f,
                    rewardGlow.rectTransform.DOScale(1f, 0.2f)
                        .SetEase(Ease.InOutSine));
            }
            InsertFade(rewardTextGroup, 0.88333327f, 0.05000033f);
            InsertFade(continueGroup, 0.8666667f, 0.1166666f);

            _introSequence.OnComplete(() =>
            {
                _introSequence = null;
                ResetIntroVisuals();
            });
        }

        private void PrepareIntroVisuals()
        {
            SetAlpha(titleGroup, 0f);
            SetAlpha(stepGroup, 0f);
            SetAlpha(clearGroup, 0f);
            SetAlpha(catIconGroup, 0f);
            SetAlpha(fishIconGroup, 0f);
            SetAlpha(collectGlowGroup, 0f);
            SetAlpha(collectTextGroup, 0f);
            SetAlpha(rankListGroup, 0f);
            SetAlpha(topGroup, 0f);
            SetAlpha(fullChestGroup, 0f);
            SetAlpha(fullAvatarGroup, 0f);
            SetAlpha(frameOnlyAvatarGroup, 0f);
            SetAlpha(rewardTextGroup, 0f);
            SetAlpha(continueGroup, 0f);
            SetScale(step, Vector3.zero);
            SetScale(catIconTransform, Vector3.zero);
            SetScale(fishIconTransform, Vector3.zero);
            SetScale(rankList, Vector3.zero);
            SetScale(fullChest, Vector3.zero);
            SetScale(fullAvatar, Vector3.zero);
            SetScale(frameOnlyAvatar, Vector3.zero);
            SetScale(arrowToCollect, Vector3.zero);
            SetScale(arrowToRank, Vector3.zero);
            SetScale(arrowToReward, Vector3.zero);
            if (rewardGlow != null)
            {
                SetImageAlpha(rewardGlow, 0f);
                rewardGlow.rectTransform.localScale = Vector3.one * 0.7f;
            }
        }

        private void InsertPop(
            RectTransform target,
            float start,
            float peak,
            float settle)
        {
            if (target == null || _introSequence == null) return;
            _introSequence.Insert(
                start,
                target.DOScale(1.05f, peak - start)
                    .SetEase(Ease.OutCubic));
            _introSequence.Insert(
                peak,
                target.DOScale(1f, settle - peak)
                    .SetEase(Ease.InOutSine));
        }

        private void InsertArrow(
            RectTransform target,
            Vector3 baseScale,
            Vector3 baseRotation,
            float start,
            float finish,
            float rotationOffset)
        {
            if (target == null || _introSequence == null) return;
            target.localEulerAngles = baseRotation +
                new Vector3(0f, 0f, rotationOffset);
            _introSequence.Insert(
                start,
                target.DOScale(baseScale, finish - start)
                    .SetEase(Ease.OutCubic));
            _introSequence.Insert(
                start,
                target.DOLocalRotate(baseRotation, finish - start)
                    .SetEase(Ease.OutCubic));
        }

        private void InsertFade(
            CanvasGroup target,
            float start,
            float duration)
        {
            if (target == null || _introSequence == null) return;
            _introSequence.Insert(
                start, target.DOFade(1f, duration).SetEase(Ease.Linear));
        }

        private void CaptureBases()
        {
            if (_basesCaptured) return;
            _arrowToCollectScale = ScaleOf(arrowToCollect);
            _arrowToRankScale = ScaleOf(arrowToRank);
            _arrowToRewardScale = ScaleOf(arrowToReward);
            _arrowToCollectRotation = RotationOf(arrowToCollect);
            _arrowToRankRotation = RotationOf(arrowToRank);
            _arrowToRewardRotation = RotationOf(arrowToReward);
            _basesCaptured = true;
        }

        private void StopIntro(bool reset)
        {
            if (_introSequence != null && _introSequence.IsActive())
                _introSequence.Kill(false);
            _introSequence = null;
            if (reset) ResetIntroVisuals();
        }

        private void ResetIntroVisuals()
        {
            if (!_basesCaptured) return;
            SetAlpha(titleGroup, 1f);
            SetAlpha(stepGroup, 1f);
            SetAlpha(clearGroup, 1f);
            SetAlpha(catIconGroup, 1f);
            SetAlpha(fishIconGroup, 1f);
            SetAlpha(collectGlowGroup, 1f);
            SetAlpha(collectTextGroup, 1f);
            SetAlpha(rankListGroup, 1f);
            SetAlpha(topGroup, 1f);
            SetAlpha(fullChestGroup, 1f);
            SetAlpha(fullAvatarGroup, 1f);
            SetAlpha(frameOnlyAvatarGroup, 1f);
            SetAlpha(rewardTextGroup, 1f);
            SetAlpha(continueGroup, 1f);
            SetScale(step, Vector3.one);
            SetScale(catIconTransform, Vector3.one);
            SetScale(fishIconTransform, Vector3.one);
            SetScale(rankList, Vector3.one);
            SetScale(fullChest, Vector3.one);
            SetScale(fullAvatar, Vector3.one);
            SetScale(frameOnlyAvatar, Vector3.one);
            RestoreArrow(
                arrowToCollect, _arrowToCollectScale, _arrowToCollectRotation);
            RestoreArrow(
                arrowToRank, _arrowToRankScale, _arrowToRankRotation);
            RestoreArrow(
                arrowToReward, _arrowToRewardScale, _arrowToRewardRotation);
            if (rewardGlow != null)
            {
                SetImageAlpha(rewardGlow, 0.9f);
                rewardGlow.rectTransform.localScale = Vector3.one;
            }
        }

        private static void RestoreArrow(
            RectTransform target, Vector3 scale, Vector3 rotation)
        {
            if (target == null) return;
            target.localScale = scale;
            target.localEulerAngles = rotation;
        }

        private static Vector3 ScaleOf(RectTransform target) =>
            target != null ? target.localScale : Vector3.one;

        private static Vector3 RotationOf(RectTransform target) =>
            target != null ? target.localEulerAngles : Vector3.zero;

        private static void SetScale(RectTransform target, Vector3 scale)
        {
            if (target != null) target.localScale = scale;
        }

        private static void SetAlpha(CanvasGroup group, float alpha)
        {
            if (group != null) group.alpha = Mathf.Clamp01(alpha);
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null) return;
            Color color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
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
