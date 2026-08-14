using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using Meowdoku.Core;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    public interface IRewardedReviveService
    {
        bool IsRewardAvailable { get; }
        IEnumerator RequestReward(System.Action<bool> completed);
    }

    [DisallowMultipleComponent]
    public sealed class GameFailPagePresenter : UIFrameWindow,
        IAdServiceConsumer,
        IAbConfigRuntimeConsumer
    {
        public override string GetTrackingScreenName() =>
            _selfName == UiName.DailyFail
                ? TrackerCatalog.Screen.DailyFail
                : TrackerCatalog.Screen.NormalFail;

        private const float InputBlockSeconds = 1.5f;
        private const float HideSeconds = 0.1f;
        private const float RemainingTravel = 60f;

        [SerializeField] private CanvasGroup pageGroup;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private RectTransform content;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private RectTransform failCat;
        [SerializeField] private RectTransform title;
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private RectTransform remaining;
        [SerializeField] private CanvasGroup remainingGroup;
        [SerializeField] private Text remainingText;
        [SerializeField] private GameObject encourageRoot;
        [SerializeField] private CanvasGroup encourageGroup;
        [SerializeField] private Text encourageText;
        [SerializeField] private GameObject reviveRoot;
        [SerializeField] private CanvasGroup reviveGroup;
        [SerializeField] private Text reviveText;
        [SerializeField] private Text reviveSubtitleText;
        [SerializeField] private Button reviveButton;
        [SerializeField] private CanvasGroup restartGroup;
        [SerializeField] private Text restartText;
        [SerializeField] private Button restartButton;
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private MonoBehaviour rewardedReviveAdapter;

        private ReviveLifeConfig _reviveLifeConfig = new();
        private ReviveFreeLogicConfig _freeLogicConfig = new();
        private RewardUnlockLevelConfig _rewardUnlockConfig = new();
        private FailTextConfig _failTextConfig = new();
        private GameplayManager _gameplayManager;
        private AdService _adService;
        private MainGameTransitionData _transition;
        private bool _freeRevive;
        private int _rewardLevel;
        private UiName _selfName = UiName.Fail;
        private Sequence _openTween;
        private Tween _buttonReadyTween;
        private Tween _hideTween;
        private Vector2 _remainingRestAnchoredPosition;
        private static readonly string[] EncourageHigh =
            { "FAIL_ENC_HIGH_1", "FAIL_ENC_HIGH_2", "FAIL_ENC_HIGH_3", "FAIL_ENC_HIGH_4" };
        private static readonly string[] EncourageMid =
            { "FAIL_ENC_MID_1", "FAIL_ENC_MID_2", "FAIL_ENC_MID_3", "FAIL_ENC_MID_4" };
        private static readonly string[] EncourageLow =
            { "FAIL_ENC_LOW_1", "FAIL_ENC_LOW_2", "FAIL_ENC_LOW_3", "FAIL_ENC_LOW_4" };

        private IRewardedReviveService RewardedRevive =>
            rewardedReviveAdapter as IRewardedReviveService;

        protected override void OnCreate()
        {
            if (remaining != null)
                _remainingRestAnchoredPosition = remaining.anchoredPosition;
            if (reviveButton != null) reviveButton.onClick.AddListener(Revive);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
        }

        public void BindAbConfigRuntime(AbConfigRuntime runtime)
        {
            _reviveLifeConfig = runtime?.Result.ReviveLife ??
                                new ReviveLifeConfig();
            _freeLogicConfig = runtime?.Result.ReviveFreeLogic ??
                               new ReviveFreeLogicConfig();
            _rewardUnlockConfig = runtime?.Gameplay.RewardUnlockLevel ??
                                  new RewardUnlockLevelConfig();
            _failTextConfig = runtime?.Result.FailText ?? new FailTextConfig();
        }

#if UNITY_INCLUDE_TESTS
        internal int ReviveLifeValueForTests => _reviveLifeConfig.Value;
        internal int ReviveFreeLogicValueForTests => _freeLogicConfig.Value;
        internal int RewardUnlockValueForTests => _rewardUnlockConfig.Value;
        internal int FailTextValueForTests => _failTextConfig.Value;
        internal bool IntroActiveForTests => _openTween != null &&
            _openTween.IsActive();
        internal bool ButtonsReadyForTests => _buttonReadyTween == null;
        internal float PageAlphaForTests => pageGroup != null
            ? pageGroup.alpha
            : 0f;
#endif

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            if (pageGroup != null) pageGroup.alpha = 1f;
            _transition = ReadTransition(parameters);
            _gameplayManager = ReadManager(parameters);
            if (_transition == null)
            {
                Owner?.Hide(UiName.Fail);
                Owner?.Hide(UiName.DailyFail);
                return;
            }

            GameStateService state = GameStateRuntime.Current;
            _selfName = _transition.IsDailySession
                ? UiName.DailyFail
                : UiName.Fail;
            _rewardLevel = _transition.IsDailySession
                ? state.CurrentLevel
                : _transition.Level;
            _freeRevive = _freeLogicConfig.ShouldFreeRevive(
                _rewardLevel,
                state.HasUsedReviveFree);
            bool rewardRequired = _rewardUnlockConfig.IsRewardRequiredAt(
                _rewardLevel);
            bool canReward = RewardedRevive != null
                ? RewardedRevive.IsRewardAvailable
                : _adService != null && _adService.IsValid(
                    TrackerCatalog.Placement.Reward,
                    RewardPosition());
            bool showRevive = _freeRevive || !rewardRequired || canReward;
            bool revivePromote = _failTextConfig.ShouldShowRevivePromote() &&
                showRevive;

            bool daily = _transition.IsDailySession;
            SetText(
                titleText,
                daily
                    ? Translate("DAILY_FAIL_TITLE", "So close")
                    : Translate("FAIL_TITLE_FISH", "Out of fish"));
            SetText(remainingText,
                Translate("FAIL_REMAINING_CAT", "Remaining: ") +
                "<b><color=#D94848>" + _transition.RemainingCats +
                "</color></b>");
            SetText(restartText, Translate("FAIL_RESTART", "Restart"));
            SetText(reviveText,
                Translate(
                    _reviveLifeConfig.IsAlternateButtonText()
                        ? "FAIL_REVIVE_3FISH"
                        : "FAIL_REVIVE",
                    _reviveLifeConfig.IsAlternateButtonText()
                        ? "Get 3 Fishes"
                        : "Revive"));
            bool showSubtitle = _reviveLifeConfig.IsTwoLineButton();
            if (reviveSubtitleText != null)
            {
                reviveSubtitleText.gameObject.SetActive(showSubtitle);
                if (showSubtitle)
                    reviveSubtitleText.text = Translate(
                        "FAIL_REVIVE_SUBTITLE_3FISH",
                        "Use it to Get 3 Fishes");
            }
            if (reviveRoot != null) reviveRoot.SetActive(showRevive);

            bool showEncourage = _failTextConfig.ShouldShowEncourage();
            if (encourageRoot != null) encourageRoot.SetActive(showEncourage);
            if (showEncourage)
                SetText(encourageText, ResolveEncourageText(
                    state,
                    showRevive));

            SetButtons(false);
            Owner?.BlockInputBriefly(
                transform as RectTransform,
                InputBlockSeconds);
            _gameplayManager?.SetResultBgmPaused(true);
            _gameplayManager?.PlayResultSound(SoundKind.LevelFail);
            PlayOpenAnimation(revivePromote);
        }

        protected override IEnumerator OnHide()
        {
            KillOpenAndButtonTweens();
            _hideTween?.Kill(false);
            _hideTween = null;
            if (pageGroup != null)
            {
                _hideTween = pageGroup.DOFade(0f, HideSeconds)
                    .SetUpdate(true)
                    .SetLink(gameObject);
                yield return _hideTween.WaitForCompletion();
                _hideTween = null;
            }
            _gameplayManager?.SetResultBgmPaused(false);
            _gameplayManager = null;
            _transition = null;
            yield break;
        }

        protected override bool OnBackRequest() => true;

        protected override void OnDestroyWindow()
        {
            KillOpenAndButtonTweens();
            _hideTween?.Kill(false);
            _hideTween = null;
            if (reviveButton != null) reviveButton.onClick.RemoveListener(Revive);
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            base.OnDestroyWindow();
        }

        private void Revive()
        {
            if (_gameplayManager == null || _transition == null) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Revive,
                GetTrackingScreenName());
            SetButtons(false);
            if (_freeRevive || !_rewardUnlockConfig.IsRewardRequiredAt(_rewardLevel))
            {
                FinishRevive();
                return;
            }

            IRewardedReviveService reward = RewardedRevive;
            if (reward != null)
            {
                if (!reward.IsRewardAvailable)
                {
                    SetButtons(true);
                    return;
                }
                StartManagedCoroutine(reward.RequestReward(success =>
                {
                    if (success) FinishRevive();
                    else SetButtons(true);
                }));
                return;
            }

            if (_adService == null || !_adService.TryShowReward(
                    RewardPosition(),
                    success =>
                    {
                        if (success) FinishRevive();
                        else SetButtons(true);
                    }))
            {
                SetButtons(true);
            }
        }

        public void BindAdService(AdService service)
        {
            _adService = service;
        }

        private string RewardPosition() =>
            _selfName == UiName.DailyFail
                ? TrackerCatalog.AdPosition.DailyGameFail
                : TrackerCatalog.AdPosition.NormalGameFail;

        private void FinishRevive()
        {
            if (_gameplayManager == null ||
                !_gameplayManager.ReviveFromFail(
                    _reviveLifeConfig.LivesToRestore()))
            {
                SetButtons(true);
                return;
            }
            if (_freeRevive && _freeLogicConfig.ShouldConsume())
                GameStateRuntime.Current.MarkReviveFreeUsed();
            Owner?.Hide(_selfName);
        }

        private void Restart()
        {
            if (_gameplayManager == null) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Restart,
                GetTrackingScreenName());
            SetButtons(false);
            if (_gameplayManager.RestartLevel()) Owner?.Hide(_selfName);
            else SetButtons(true);
        }

        private void PlayOpenAnimation(bool revivePromote)
        {
            KillOpenAndButtonTweens();
            _hideTween?.Kill(false);
            _hideTween = null;
            if (pageGroup != null) pageGroup.alpha = 1f;
            if (overlayGroup != null) overlayGroup.alpha = 0f;
            if (content != null) content.localScale = Vector3.one;
            if (contentGroup != null) contentGroup.alpha = 1f;
            if (failCat != null) failCat.localScale = Vector3.zero;
            if (title != null) title.localScale = Vector3.one * 1.8f;
            if (titleGroup != null) titleGroup.alpha = 0f;
            if (remaining != null)
                remaining.anchoredPosition = _remainingRestAnchoredPosition +
                    Vector2.down * RemainingTravel;
            if (remainingGroup != null) remainingGroup.alpha = 0f;
            if (encourageGroup != null) encourageGroup.alpha = 0f;
            if (reviveGroup != null) reviveGroup.alpha = 0f;
            if (restartGroup != null) restartGroup.alpha = 0f;

            _openTween = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);
            if (overlayGroup != null)
                _openTween.Insert(0f, overlayGroup.DOFade(
                    0.8509804f, 0.06666667f).SetEase(Ease.Linear));
            if (failCat != null)
            {
                _openTween.Insert(0f, failCat.DOScale(
                    0.6f, 0.016666668f).SetEase(Ease.Linear));
                _openTween.Insert(0.016666668f, failCat.DOScale(
                    1.1f, 0.25f).SetEase(Ease.OutQuad));
                _openTween.Insert(0.26666668f, failCat.DOScale(
                    0.9236364f, 0.16666666f).SetEase(Ease.InOutQuad));
            }
            if (titleGroup != null)
                _openTween.Insert(0.33333334f, titleGroup.DOFade(
                    1f, 0.14999998f).SetEase(Ease.Linear));
            if (title != null)
            {
                _openTween.Insert(0.33333334f, title.DOScale(
                    0.9f, 0.24999996f).SetEase(Ease.OutQuad));
                _openTween.Insert(0.5833333f, title.DOScale(
                    1f, 0.31666666f).SetEase(Ease.OutQuad));
            }
            if (remainingGroup != null)
                _openTween.Insert(0.6333333f, remainingGroup.DOFade(
                    1f, 0.2666667f).SetEase(Ease.Linear));
            if (remaining != null)
                _openTween.Insert(0.6333334f, remaining.DOAnchorPos(
                    _remainingRestAnchoredPosition, 0.35f)
                    .SetEase(Ease.OutQuad));
            if (encourageGroup != null &&
                encourageGroup.gameObject.activeInHierarchy)
                _openTween.Insert(
                    revivePromote ? 0.8166f : 0.9833f,
                    encourageGroup.DOFade(1f, 0.35f).SetEase(Ease.Linear));
            if (reviveGroup != null &&
                reviveGroup.gameObject.activeInHierarchy)
                _openTween.Insert(0.8166666f, reviveGroup.DOFade(
                    1f, 0.35f).SetEase(Ease.Linear));
            if (restartGroup != null &&
                restartGroup.gameObject.activeInHierarchy)
                _openTween.Insert(0.98333335f, restartGroup.DOFade(
                    1f, 0.35000005f).SetEase(Ease.Linear));
            _openTween.OnComplete(() => _openTween = null);

            _buttonReadyTween = DOVirtual.DelayedCall(
                    InputBlockSeconds,
                    () =>
                    {
                        _buttonReadyTween = null;
                        SetButtons(true);
                    },
                    true)
                .SetLink(gameObject);
        }

        private void KillOpenAndButtonTweens()
        {
            _openTween?.Kill(false);
            _openTween = null;
            _buttonReadyTween?.Kill(false);
            _buttonReadyTween = null;
        }

        private void SetButtons(bool interactable)
        {
            if (restartButton != null) restartButton.interactable = interactable;
            if (reviveButton != null)
                reviveButton.interactable = interactable &&
                    reviveRoot != null && reviveRoot.activeSelf;
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string translated = localization.Translate(key);
            return translated == key ? fallback : translated;
        }

        private string ResolveEncourageText(
            GameStateService state,
            bool reviveVisible)
        {
            if (_failTextConfig.ShouldShowRevivePromote() && reviveVisible)
            {
                float percent = state.GetFailTextRevivePercent(_transition.Level);
                if (percent < 0f)
                {
                    percent = PickRevivePromotePercent(
                        _transition.Level,
                        _transition.Size);
                    state.SetFailTextRevivePercent(
                        _transition.Level,
                        percent);
                }
                string format = Translate(
                    "FAIL_REVIVE_PROMOTE",
                    "%s of players used Revive to pass this level!");
                return ConvertGodotBbCode(format.Replace(
                    "%s",
                    percent.ToString("0.0", CultureInfo.InvariantCulture) +
                    "%"));
            }

            float foundRatio = _transition.Size > 0
                ? (_transition.Size - _transition.RemainingCats) /
                  (float)_transition.Size
                : 0f;
            string[] keys = foundRatio > 0.8f
                ? EncourageHigh
                : foundRatio >= 0.2f
                    ? EncourageMid
                    : EncourageLow;
            string key = keys[UnityEngine.Random.Range(0, keys.Length)];
            return Translate(key, key);
        }

        private static float PickRevivePromotePercent(int level, int size)
        {
            float low;
            float high;
            if (LevelData.IsHardLevel(level))
            {
                low = 72f;
                high = 87f;
            }
            else if (size <= 5)
            {
                low = 25f;
                high = 45f;
            }
            else if (size <= 7)
            {
                low = 35f;
                high = 55f;
            }
            else if (size <= 9)
            {
                low = 52f;
                high = 67f;
            }
            else
            {
                low = 62f;
                high = 77f;
            }

            for (int attempt = 0; attempt < 20; attempt++)
            {
                float value = Mathf.Round(
                    UnityEngine.Random.Range(low, high) * 10f) / 10f;
                if (value <= low || value >= high) continue;
                if (Mathf.RoundToInt(value * 10f) % 10 == 0) continue;
                return value;
            }
            return low + 0.1f;
        }

        private static string ConvertGodotBbCode(string value)
        {
            return (value ?? string.Empty)
                .Replace("[center]", string.Empty)
                .Replace("[/center]", string.Empty)
                .Replace("[font_size=70]", "<size=70>")
                .Replace("[/font_size]", "</size>")
                .Replace("[b]", "<b>")
                .Replace("[/b]", "</b>")
                .Replace("[color=#FFCC00]", "<color=#FFCC00>")
                .Replace("[/color]", "</color>");
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }

        private static MainGameTransitionData ReadTransition(
            IReadOnlyDictionary<string, object> parameters)
        {
            return parameters != null &&
                   parameters.TryGetValue("transition", out object value)
                ? value as MainGameTransitionData
                : null;
        }

        private static GameplayManager ReadManager(
            IReadOnlyDictionary<string, object> parameters)
        {
            return parameters != null &&
                   parameters.TryGetValue("gameplay_manager", out object value)
                ? value as GameplayManager
                : null;
        }
    }
}
