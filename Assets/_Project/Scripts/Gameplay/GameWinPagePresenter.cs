using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using DG.Tweening;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameWinPagePresenter : UIFrameWindow,
        IDailyMetaConsumer,
        IProfileConsumer,
        IRankActivityConsumer
    {
        public override string GetTrackingScreenName() =>
            _selfName == UiName.DailyWin
                ? TrackerCatalog.Screen.DailyWin
                : TrackerCatalog.Screen.NormalWin;

        private static readonly string[] NormalTitleKeys =
        {
            "WIN_TITLE", "WIN_TITLE_1", "WIN_TITLE_2", "WIN_TITLE_3",
            "WIN_TITLE_4"
        };

        [SerializeField] private RectTransform content;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private GameObject defaultVisuals;
        [SerializeField] private RectTransform rayLight;
        [SerializeField] private RectTransform victoryCat;
        [SerializeField] private Text titleText;
        [SerializeField] private GameObject bodyRoot;
        [SerializeField] private Text bodyText;
        [SerializeField] private GameObject statisticsRoot;
        [SerializeField] private CanvasGroup statisticsGroup;
        [SerializeField] private Text timeText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text comboText;
        [SerializeField] private Text nextButtonText;
        [SerializeField] private Button nextButton;
        [SerializeField] private GameObject passPanelRoot;
        [SerializeField] private RectTransform passPanelPopup;
        [SerializeField] private CanvasGroup passPanelGroup;
        [SerializeField] private Text passTitleText;
        [SerializeField] private Text passPraiseText;
        [SerializeField] private RectTransform passPraiseRect;
        [SerializeField] private RectTransform passStatsRoot;
        [SerializeField] private RectTransform passActionsRect;
        [SerializeField] private Text passSizeKeyText;
        [SerializeField] private Text passTimeKeyText;
        [SerializeField] private Text passScoreKeyText;
        [SerializeField] private Text passComboKeyText;
        [SerializeField] private Text passSizeText;
        [SerializeField] private Text passTimeText;
        [SerializeField] private Text passScoreText;
        [SerializeField] private Text passComboText;
        [SerializeField] private GameObject passExtraRoot;
        [SerializeField] private Text passCompletionText;
        [SerializeField] private Text passMistakeText;
        [SerializeField] private Text passToolsText;
        [SerializeField] private Text passNextButtonText;
        [SerializeField] private Button passNextButton;
        [Header("Daily result presentation")]
        [SerializeField] private GameObject dailyVisuals;
        [SerializeField] private RectTransform dailyContent;
        [SerializeField] private CanvasGroup dailyContentGroup;
        [SerializeField] private RectTransform dailyRayLight;
        [SerializeField] private RectTransform dailyVictoryCat;
        [SerializeField] private Text dailyTitleText;
        [SerializeField] private Text dailyTimeText;
        [SerializeField] private Text dailyBeatText;
        [SerializeField] private Text dailyContinueText;
        [SerializeField] private Button dailyContinueButton;
        [SerializeField] private LocalizationCatalog localization;

        private readonly PassPageConfig _passPageConfig = new();
        private readonly PassTextConfig _passTextConfig = new();
        private GameplayManager _gameplayManager;
        private Sequence _openTween;
        private Tween _rayTween;
        private Tween _catTween;
        private Sequence _statisticsTween;
        private Tween _passNextReadyTween;
        private string _lastNormalTitleKey = string.Empty;
        private MainGameTransitionData _transition;
        private UiName _selfName = UiName.Win;
        private DailyMetaRuntime _dailyMetaRuntime;
        private ProfileRuntime _profileRuntime;
        private RankActivityRuntime _rankActivityRuntime;
        private bool _continuing;

        protected override void OnCreate()
        {
            if (nextButton != null) nextButton.onClick.AddListener(Continue);
            if (passNextButton != null)
                passNextButton.onClick.AddListener(Continue);
            if (dailyContinueButton != null)
                dailyContinueButton.onClick.AddListener(Continue);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            MainGameTransitionData transition = ReadTransition(parameters);
            _transition = transition;
            _gameplayManager = ReadManager(parameters);
            _continuing = false;
            if (transition == null)
            {
                Owner?.Hide(UiName.Win);
                Owner?.Hide(UiName.DailyWin);
                return;
            }
            _selfName = transition.IsDailySession
                ? UiName.DailyWin
                : UiName.Win;

            bool passPanel = RefreshText(transition);
            if (transition.IsDailySession)
            {
                if (dailyContinueButton != null)
                    dailyContinueButton.interactable = false;
                bool toastWasShown = ReadBool(
                    parameters,
                    "toast_was_shown");
                if (toastWasShown)
                    StartDailyPresentation();
                else
                    StartManagedCoroutine(StartDailyPresentationAfterDelay());
                StartManagedCoroutine(UnlockDailyInputAfterDelay());
                return;
            }
            if (passPanel)
            {
                PlayPassPanelAnimation();
                _gameplayManager?.PlayResultSound(SoundKind.PassPageSettle);
            }
            else
            {
                PlayOpenAnimation();
                _gameplayManager?.PlayResultSound(SoundKind.LevelWin);
            }
        }

        protected override IEnumerator OnHide()
        {
            KillTweens();
            _gameplayManager = null;
            _transition = null;
            _continuing = false;
            yield break;
        }

        protected override bool OnBackRequest() => true;

        protected override void OnDestroyWindow()
        {
            KillTweens();
            if (nextButton != null) nextButton.onClick.RemoveListener(Continue);
            if (passNextButton != null)
                passNextButton.onClick.RemoveListener(Continue);
            if (dailyContinueButton != null)
                dailyContinueButton.onClick.RemoveListener(Continue);
            base.OnDestroyWindow();
        }

        public void BindDailyMetaRuntime(DailyMetaRuntime runtime)
        {
            _dailyMetaRuntime = runtime;
        }

        public void BindProfileRuntime(ProfileRuntime runtime)
        {
            _profileRuntime = runtime;
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            _rankActivityRuntime = runtime;
        }

        private bool RefreshText(MainGameTransitionData transition)
        {
            if (transition.IsDailySession)
                return RefreshDailyText(transition);

            PassTextStrategySelection selection =
                PassTextStrategyContract.Select(
                    _passTextConfig.Value,
                    new PassTextStrategyInput
                    {
                        Level = transition.Level,
                        Size = transition.Size,
                        RestartCount = transition.RestartCount,
                        ReviveCount = transition.ReviveCount,
                        MistakeCount = transition.MistakeCount,
                        ElapsedSeconds = transition.ElapsedSeconds,
                        LastWinBeatPercent =
                            GameStateRuntime.Current.LastWinBeatPercent,
                        IsHard = transition.Level > 0 &&
                                 LevelData.IsHardLevel(transition.Level)
                    },
                    UnityEngine.Random.Range(0, int.MaxValue),
                    UnityEngine.Random.value);
            if (dailyVisuals != null) dailyVisuals.SetActive(false);
            GameStateRuntime.Current.SetLastWinBeatPercent(
                (float)selection.ShownPercent);

            string titleKey = !string.IsNullOrEmpty(selection.TitleKey)
                ? selection.TitleKey
                : LevelData.IsHardLevel(transition.Level)
                    ? "WIN_TITLE_HARD"
                    : PickNormalTitleKey();
            string title = Translate(titleKey, "Great!");
            string body = !string.IsNullOrEmpty(selection.BodyKey)
                ? StrategyBody(selection)
                : string.Empty;
            bool showPassPanel = _passPageConfig.IsG1() ||
                                 _passPageConfig.IsG2();
            if (defaultVisuals != null) defaultVisuals.SetActive(!showPassPanel);
            if (content != null) content.gameObject.SetActive(!showPassPanel);
            if (passPanelRoot != null) passPanelRoot.SetActive(showPassPanel);
            if (showPassPanel)
            {
                PopulatePassPanel(transition, title, body);
                return true;
            }

            SetText(titleText, title);

            bool showBody = !string.IsNullOrEmpty(selection.BodyKey);
            if (bodyRoot != null) bodyRoot.SetActive(showBody);
            if (showBody)
                SetText(bodyText, body);

            bool showStatistics = _passPageConfig.IsG4();
            if (statisticsRoot != null) statisticsRoot.SetActive(showStatistics);
            if (showStatistics)
            {
                if (statisticsGroup != null) statisticsGroup.alpha = 0f;
                SetText(timeText,
                    $"{Translate("PASS_PAGE_TIME", "Time")}  {FormatTime(0f)}");
                SetText(scoreText,
                    $"{Translate("PASS_PAGE_SCORE", "Score")}  {FormatScore(0)}");
                SetText(comboText,
                    $"{Translate("PASS_PAGE_COMBO", "Combo")}  0");
            }

            string nextLabel = transition.IsBankSession
                ? transition.NextBankLabel
                : Translate("GAME_LEVEL_TITLE", "Level %d").Replace(
                    "%d",
                    transition.CurrentLevelAfter.ToString());
            SetText(nextButtonText, nextLabel);
            if (nextButton != null)
                nextButton.interactable = !string.IsNullOrEmpty(nextLabel);
            return false;
        }

        private bool RefreshDailyText(MainGameTransitionData transition)
        {
            if (defaultVisuals != null) defaultVisuals.SetActive(false);
            if (content != null) content.gameObject.SetActive(false);
            if (passPanelRoot != null) passPanelRoot.SetActive(false);
            if (dailyVisuals != null) dailyVisuals.SetActive(true);

            SetText(
                dailyTitleText,
                Translate("DAILY_WIN_TITLE", "Challenge Cleared"));
            string time = DailyResultContract.FormatElapsedSeconds(
                transition.ElapsedSeconds);
            string percent = DailyResultContract.FormatBeatPercent(
                transition.DailyBeatPercent);
            SetText(
                dailyTimeText,
                "<color=#FFF1B9>" +
                Translate("DAILY_WIN_TIME", "Time") +
                " </color><color=#F19320><size=80><b>" + time +
                "</b></size></color>");
            string highlighted =
                "</color><color=#02BE52><size=90><b>" + percent +
                "</b></size></color><color=#FFE375>";
            string beat = Translate(
                    "DAILY_WIN_BEAT",
                    "Beat %s of players!")
                .Replace("%s", highlighted);
            SetText(dailyBeatText, "<color=#FFE375>" + beat + "</color>");
            SetText(
                dailyContinueText,
                Translate("WIN_CONTINUE", "Continue"));
            return false;
        }

        private IEnumerator StartDailyPresentationAfterDelay()
        {
            yield return new WaitForSecondsRealtime(
                DailyResultContract.AppearDelaySeconds);
            if (_transition != null && _transition.IsDailySession)
                StartDailyPresentation();
        }

        private IEnumerator UnlockDailyInputAfterDelay()
        {
            yield return new WaitForSecondsRealtime(
                DailyResultContract.InputBlockSeconds);
            if (_transition != null && _transition.IsDailySession &&
                dailyContinueButton != null)
                dailyContinueButton.interactable = true;
        }

        private void StartDailyPresentation()
        {
            PlayDailyOpenAnimation();
            _gameplayManager?.PlayResultSound(SoundKind.LevelWin);
        }

        private void PopulatePassPanel(
            MainGameTransitionData transition,
            string title,
            string praise)
        {
            SetText(passTitleText, title);
            SetText(passPraiseText, PassPraise(praise));
            if (passPraiseText != null)
                passPraiseText.gameObject.SetActive(!string.IsNullOrEmpty(praise));
            SetText(passSizeKeyText, Translate("PASS_PAGE_SIZE", "Size"));
            SetText(passTimeKeyText, Translate("PASS_PAGE_TIME", "Time"));
            SetText(passScoreKeyText, Translate("PASS_PAGE_SCORE", "Score"));
            SetText(passComboKeyText, Translate("PASS_PAGE_COMBO", "Combo"));
            SetText(passSizeText, $"{transition.Size}\u00D7{transition.Size}");
            SetText(passTimeText, FormatTime(transition.ElapsedSeconds));
            SetText(passScoreText, FormatScore(transition.FinalScore));
            SetText(passComboText, transition.MaxCombo.ToString());

            bool showExtra = _passPageConfig.IsG2();
            ApplyPassLayout(showExtra);
            if (passExtraRoot != null) passExtraRoot.SetActive(showExtra);
            if (showExtra)
            {
                SetText(passCompletionText, transition.CompletionRate + "%");
                SetText(passMistakeText, transition.MistakeCount.ToString());
                SetText(passToolsText, transition.ToolsUsed.ToString());
            }

            string nextLabel = transition.IsBankSession
                ? transition.NextBankLabel
                : Translate("GAME_LEVEL_TITLE", "Level %d").Replace(
                    "%d",
                    transition.CurrentLevelAfter.ToString());
            SetText(passNextButtonText, nextLabel);
            if (passNextButton != null)
                passNextButton.interactable = false;
        }

        private void ApplyPassLayout(bool group2)
        {
            if (passPanelPopup != null)
            {
                passPanelPopup.anchoredPosition = new Vector2(
                    0f,
                    group2 ? 366f : 372f);
                passPanelPopup.sizeDelta = new Vector2(
                    900f,
                    group2 ? 1072f : 912f);
            }
            if (passTitleText != null)
                passTitleText.rectTransform.anchoredPosition = new Vector2(
                    0f,
                    group2 ? 260f : 172f);
            if (passStatsRoot != null)
                passStatsRoot.anchoredPosition = new Vector2(
                    0f,
                    group2 ? -62f : -150f);
            if (passPraiseRect != null)
                passPraiseRect.anchoredPosition = new Vector2(
                    0f,
                    group2 ? -360f : -274f);
            if (passActionsRect != null)
                passActionsRect.anchoredPosition = new Vector2(
                    0f,
                    group2 ? -630f : -544f);
        }

        private void PlayPassPanelAnimation()
        {
            KillTweens();
            if (passPanelPopup != null)
                passPanelPopup.localScale = Vector3.one * 0.5f;
            if (passPanelGroup != null) passPanelGroup.alpha = 0f;
            _openTween = DOTween.Sequence().SetLink(gameObject);
            if (passPanelPopup != null)
                _openTween.Append(
                        passPanelPopup.DOScale(1.1f, 0.2f)
                            .SetEase(Ease.OutQuad))
                    .Append(
                        passPanelPopup.DOScale(1f, 0.133333f)
                            .SetEase(Ease.InOutQuad));
            if (passPanelGroup != null)
                _openTween.Insert(0f, passPanelGroup.DOFade(1f, 0.2f));
            _openTween.OnComplete(() => _openTween = null);
            _passNextReadyTween = DOVirtual.DelayedCall(
                    0.69804f,
                    () =>
                    {
                        if (passNextButton != null)
                            passNextButton.interactable = true;
                        _passNextReadyTween = null;
                    })
                .SetLink(gameObject);
        }

        private void PlayOpenAnimation()
        {
            KillTweens();
            if (content != null) content.localScale = Vector3.one * 0.7f;
            if (contentGroup != null) contentGroup.alpha = 0f;
            _openTween = DOTween.Sequence().SetLink(gameObject);
            if (content != null)
                _openTween.Append(content.DOScale(1.05f, 0.18f).SetEase(Ease.OutQuad))
                    .Append(content.DOScale(1f, 0.08f).SetEase(Ease.InOutQuad));
            if (contentGroup != null)
                _openTween.Insert(0f, contentGroup.DOFade(1f, 0.18f));
            _openTween.OnComplete(() => _openTween = null);

            if (rayLight != null)
                _rayTween = rayLight.DORotate(
                        new Vector3(0f, 0f, -360f),
                        12f,
                        RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1)
                    .SetLink(gameObject);
            if (victoryCat != null)
                _catTween = victoryCat.DOScale(1.035f, 0.65f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
            PlayStatisticsRoll();
        }

        private void PlayDailyOpenAnimation()
        {
            KillTweens();
            if (dailyContent != null)
                dailyContent.localScale = Vector3.one * 0.7f;
            if (dailyContentGroup != null)
                dailyContentGroup.alpha = 0f;
            _openTween = DOTween.Sequence().SetLink(gameObject);
            if (dailyContent != null)
                _openTween.Append(
                        dailyContent.DOScale(1.05f, 0.18f)
                            .SetEase(Ease.OutQuad))
                    .Append(
                        dailyContent.DOScale(1f, 0.08f)
                            .SetEase(Ease.InOutQuad));
            if (dailyContentGroup != null)
                _openTween.Insert(
                    0f,
                    dailyContentGroup.DOFade(1f, 0.18f));
            _openTween.OnComplete(() => _openTween = null);

            if (dailyRayLight != null)
                _rayTween = dailyRayLight.DORotate(
                        new Vector3(0f, 0f, -360f),
                        12f,
                        RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1)
                    .SetLink(gameObject);
            if (dailyVictoryCat != null)
                _catTween = dailyVictoryCat.DOScale(1.035f, 0.65f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
        }

        private void PlayStatisticsRoll()
        {
            if (!_passPageConfig.IsG4() || _transition == null) return;
            string timeKey = Translate("PASS_PAGE_TIME", "Time");
            string scoreKey = Translate("PASS_PAGE_SCORE", "Score");
            string comboKey = Translate("PASS_PAGE_COMBO", "Combo");
            int elapsed = Mathf.CeilToInt(_transition.ElapsedSeconds);
            int score = _transition.FinalScore;
            int combo = _transition.MaxCombo;
            _statisticsTween = DOTween.Sequence().SetLink(gameObject);
            if (statisticsGroup != null)
                _statisticsTween.Insert(
                    0.8f,
                    statisticsGroup.DOFade(1f, 0.2f));
            _statisticsTween.Insert(
                1f,
                DOVirtual.Int(0, elapsed, 0.65f, value =>
                    SetText(timeText, $"{timeKey}  {FormatTime(value)}")));
            _statisticsTween.Insert(
                1f,
                DOVirtual.Int(0, score, 0.65f, value =>
                    SetText(scoreText, $"{scoreKey}  {FormatScore(value)}")));
            _statisticsTween.Insert(
                1f,
                DOVirtual.Int(0, combo, 0.65f, value =>
                    SetText(comboText, $"{comboKey}  {value}")));
            _statisticsTween.OnComplete(() => _statisticsTween = null);
        }

        private void Continue()
        {
            if (_continuing) return;
            Tracking?.TrackButtonClick(
                _transition?.IsDailySession == true
                    ? TrackerCatalog.Button.Continue
                    : TrackerCatalog.Button.LevelPlay,
                GetTrackingScreenName());
            _continuing = true;
            SetContinueInteractable(false);
            StartManagedCoroutine(ContinueAfterMetaFlows());
        }

        private IEnumerator ContinueAfterMetaFlows()
        {
            bool main = _transition != null &&
                        !_transition.IsDailySession &&
                        !_transition.IsBankSession &&
                        _transition.Level > 0;
            RankActivityManager rank = main
                ? _rankActivityRuntime?.Manager
                : null;
            if (rank?.GetPendingReward() != null)
            {
                int uid = rank.ClaimReward(false);
                if (uid >= 0 && Owner != null)
                {
                    yield return null;
                    yield return Owner.AwaitHidden(UiName.Award);
                }
                if (!IsShowing) yield break;
            }
            rank?.MaybeOpen(false);

            if (_dailyMetaRuntime != null &&
                _dailyMetaRuntime.Streak.IsSettleReorder &&
                StreakFlowCoordinator.HasPendingFlow(_dailyMetaRuntime))
            {
                yield return StreakFlowCoordinator.RunAfterResult(
                    Owner,
                    _dailyMetaRuntime);
            }
            if (!IsShowing) yield break;

            if (rank?.IsOpenNotJoined == true && Owner != null)
            {
                var popup = Owner.Show(UiName.RankActivityOpenPopup) as
                    RankActivityOpenPopupPresenter;
                if (popup != null)
                {
                    yield return Owner.AwaitHidden(
                        UiName.RankActivityOpenPopup);
                    rank.ConfirmParticipation();
                    if (rank.PeriodCount == 1 &&
                        _profileRuntime?.Service?.IsIdentityDefault == true)
                    {
                        UIFrameWindow profile = Owner.Show(
                            UiName.Profile,
                            new Dictionary<string, object>(1)
                            {
                                ["from_rank_open_guide"] = true
                            });
                        if (profile != null)
                            yield return Owner.AwaitHidden(UiName.Profile);
                        if (!IsShowing) yield break;
                    }
                }
            }
            FinishContinue();
        }

        private void FinishContinue()
        {
            if (_gameplayManager == null || !_gameplayManager.ContinueToNextLevel())
            {
                _continuing = false;
                SetContinueInteractable(true);
                return;
            }
            Owner?.Hide(_selfName);
        }

        private void SetContinueInteractable(bool interactable)
        {
            if (nextButton != null)
                nextButton.interactable = interactable;
            if (passNextButton != null)
                passNextButton.interactable = interactable;
            if (dailyContinueButton != null)
                dailyContinueButton.interactable = interactable;
        }

        private void KillTweens()
        {
            _openTween?.Kill(false);
            _rayTween?.Kill(false);
            _catTween?.Kill(false);
            _statisticsTween?.Kill(false);
            _passNextReadyTween?.Kill(false);
            _openTween = null;
            _rayTween = null;
            _catTween = null;
            _statisticsTween = null;
            _passNextReadyTween = null;
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string translated = localization.Translate(key);
            return translated == key ? fallback : translated;
        }

        private string StrategyBody(PassTextStrategySelection selection)
        {
            string template = Translate(selection.BodyKey, selection.BodyKey);
            string percent = HighlightPercent(selection.Percent);
            string difference = HighlightPercent(selection.DifferencePercent);
            return template
                .Replace("{pct}", percent)
                .Replace("{diff}", difference)
                .Replace("{br}", "\n")
                .Replace("[center]", string.Empty)
                .Replace("[/center]", string.Empty);
        }

        private static string HighlightPercent(double value)
        {
            if (value < 0.0) return string.Empty;
            string text = value.ToString("0.0", CultureInfo.InvariantCulture) + "%";
            return $"<size=90><b><color=#02BE52>{text}</color></b></size>";
        }

        private static string PassPraise(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string result = value.Replace("#02BE52", "#F19320");
            int leading = 0;
            while (leading < result.Length && char.IsDigit(result[leading]))
                leading++;
            if (leading > 0)
                result = "<color=#F19320>" + result.Substring(0, leading) +
                         "</color>" + result.Substring(leading);
            if (!result.Contains("<color="))
                result = Regex.Replace(
                    result,
                    @"\d+(?:\.\d+)?%",
                    "<color=#F19320>$0</color>");
            return result;
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Clamp(
                Mathf.CeilToInt(seconds),
                0,
                24 * 60 * 60 - 1);
            int hours = total / 3600;
            return hours > 0
                ? $"{hours:00}:{total % 3600 / 60:00}:{total % 60:00}"
                : $"{total / 60:00}:{total % 60:00}";
        }

        private string PickNormalTitleKey()
        {
            int index = UnityEngine.Random.Range(0, NormalTitleKeys.Length);
            if (NormalTitleKeys[index] == _lastNormalTitleKey)
            {
                int offset = UnityEngine.Random.Range(
                    1,
                    NormalTitleKeys.Length);
                index = (index + offset) % NormalTitleKeys.Length;
            }
            _lastNormalTitleKey = NormalTitleKeys[index];
            return _lastNormalTitleKey;
        }

        private static string FormatScore(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
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

        private static bool ReadBool(
            IReadOnlyDictionary<string, object> parameters,
            string key)
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out object value) ||
                value == null)
                return false;
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
