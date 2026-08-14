using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityChangePresenter : UIFrameWindow,
        IRankActivityConsumer,
        ISoundServiceConsumer
    {
        public override string GetTrackingDialogName() =>
            TrackerCatalog.Dialog.ChallengeRank;

        private const float AppearStart = 0.2f;
        private const float AppearInterval = 0.0667f;
        private const float CountStart = 0.7333f;
        private const float ScoreRollDelay = 1f;
        private const float ScoreRollDuration = 0.6333f;
        private const float LiftDuration =
            RankActivityRowCelebrationView.RiseUpDuration;
        private const float LiftHold = 0.3f;
        private const float SettleHold = 0.3f;
        private const float DropDuration =
            RankActivityRowCelebrationView.RiseDownDuration;
        private const float ArrowFadeDuration = 0.5f;
        private const float ArrowHideDelay = 0.03f;
        private const float RiseDownSwapDelay = 0.23f;
        private const float FinishHold = 0.5f;

        [SerializeField] private Text titleText;
        [SerializeField] private Text countdownText;
        [SerializeField] private GameObject encouragementRoot;
        [SerializeField] private Text encouragementText;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform rowList;
        [SerializeField] private RectTransform celebrateLayer;
        [SerializeField] private RankActivityRowView rowPrefab;
        [SerializeField] private Button maskButton;
        [SerializeField] private Button tapButton;
        [SerializeField] private Text tapText;
        [SerializeField] private LocalizationCatalog localization;

        private readonly List<RankActivityRowView> _rows = new();
        private readonly List<RankInfo> _rowInfos = new();
        private readonly List<DisplacedRow> _displaced = new();
        private RankActivityRuntime _runtime;
        private Sequence _sequence;
        private bool _animating;
        private bool _subscribed;
        private int _advance;
        private int _increment;
        private RankActivityRowView _selfRow;
        private RankInfo _selfFinal;
        private RankActivityRowView _floatingSelfRow;
        private VerticalLayoutGroup _rowLayout;
        private ContentSizeFitter _rowFitter;
        private int _selfIndex = -1;
        private int _effectiveAdvance;
        private int _group;
        private float _selfBaseY;
        private float _rowStride;
        private bool _risePrepared;
        private SoundService _soundService;

        protected override void OnCreate()
        {
            if (maskButton != null) maskButton.onClick.AddListener(Dismiss);
            if (tapButton != null) tapButton.onClick.AddListener(Dismiss);
            if (localization != null)
                localization.LocaleChanged += RefreshText;
            RefreshText();
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            RankActivityManager manager = _runtime?.Manager;
            if (manager == null)
            {
                Owner?.Hide(UiName);
                return;
            }
            _advance = ReadInt(parameters, "advance_places",
                manager.LastWinAdvance);
            _increment = ReadInt(parameters, "score_increment",
                manager.LastWinIncrement);
            Subscribe();
            BuildRows(manager.GetRankInfos(), manager.Group, true);
            ApplyEncouragement(manager.ConsumeProgressEncouragement());
            RefreshCountdown(manager.RemainingSeconds);
            PlayChange();
        }

        protected override IEnumerator OnHide()
        {
            KillSequence();
            _soundService?.Stop(SoundKind.RankScoreCount);
            RestoreFinalLayout();
            Unsubscribe();
            _animating = false;
            yield break;
        }

        protected override bool OnBackRequest()
        {
            Dismiss();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            KillSequence();
            _soundService?.Stop(SoundKind.RankScoreCount);
            RestoreFinalLayout();
            Unsubscribe();
            if (maskButton != null)
                maskButton.onClick.RemoveListener(Dismiss);
            if (tapButton != null) tapButton.onClick.RemoveListener(Dismiss);
            if (localization != null)
                localization.LocaleChanged -= RefreshText;
            base.OnDestroyWindow();
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            if (_runtime == runtime) return;
            Unsubscribe();
            _runtime = runtime;
            Subscribe();
        }

        public void BindSoundService(SoundService service)
        {
            _soundService = service;
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

        private void BuildRows(
            IReadOnlyList<RankInfo> infos,
            int group,
            bool showPreviousState)
        {
            EnsureRows(infos.Count);
            _group = group;
            _rowInfos.Clear();
            _selfRow = null;
            _selfFinal = null;
            _selfIndex = -1;
            for (int index = 0; index < _rows.Count; index++)
            {
                RankInfo info = index < infos.Count ? infos[index] : null;
                if (index < infos.Count) _rowInfos.Add(info);
                _rows[index].Apply(info, group);
                _rows[index].transform.localScale = Vector3.one;
                if (info?.IsSelf != true) continue;
                _selfRow = _rows[index];
                _selfFinal = info;
                _selfIndex = index;
                if (!showPreviousState) continue;
                int oldScore = Mathf.Max(0, info.Score - _increment);
                int oldRank = oldScore <= 0
                    ? 0
                    : Mathf.Min(
                        infos.Count,
                        Mathf.Max(info.Rank, info.Rank + _advance));
                _selfRow.SetScore(oldScore);
                _selfRow.SetRank(oldRank);
            }
            PrepareFloatingSelf(group,
                showPreviousState && (_increment > 0 || _advance > 0));
            if (scroll != null)
            {
                scroll.verticalNormalizedPosition = 1f;
                scroll.enabled = !showPreviousState;
            }
        }

        private void PlayChange()
        {
            KillSequence();
            if (tapButton != null) tapButton.gameObject.SetActive(false);
            if (encouragementRoot != null)
                encouragementRoot.SetActive(false);
            _animating = _increment > 0 || _advance > 0;
            if (!_animating)
            {
                FinishChange();
                return;
            }

            PrepareScrollAndRise();

            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);
            int visibleIndex = 0;
            for (int index = 0; index < _rows.Count; index++)
            {
                RankActivityRowView row = _rows[index];
                if (row == null || !row.gameObject.activeSelf) continue;
                if (!IsVisible(row))
                {
                    row.ShowStatic();
                    continue;
                }
                if (row == _selfRow && IsFloatingSelfActive())
                {
                    row.SetPresentationAlpha(0f);
                    _floatingSelfRow.PlayIntro(
                        RankActivityRowIntro.Appear3,
                        AppearStart + AppearInterval * visibleIndex);
                    visibleIndex++;
                    continue;
                }
                row.PlayIntro(
                    RankActivityRowIntro.Appear3,
                    AppearStart + AppearInterval * visibleIndex);
                visibleIndex++;
            }

            RankActivityRowView presentationTarget = IsFloatingSelfActive()
                ? _floatingSelfRow
                : _selfRow;
            if (presentationTarget != null)
                _sequence.InsertCallback(
                    CountStart,
                    () =>
                    {
                        presentationTarget.PlayCollection();
                        _soundService?.Play(SoundKind.RankScoreCount);
                    });

            float cursor = CountStart;
            if (_selfRow != null && _selfFinal != null && _increment > 0)
            {
                cursor += ScoreRollDelay;
                int from = Mathf.Max(0, _selfFinal.Score - _increment);
                int to = _selfFinal.Score;
                _sequence.Insert(
                    cursor,
                    DOTween.To(
                            () => from,
                            SetSelfScore,
                            to,
                            ScoreRollDuration)
                        .SetEase(Ease.OutQuad));
                cursor += ScoreRollDuration;
            }
            if (presentationTarget != null && _selfFinal != null &&
                _advance > 0)
            {
                _sequence.InsertCallback(cursor, () =>
                {
                    presentationTarget.PlayArrow(ArrowFadeDuration);
                    presentationTarget.PlayLift();
                    _soundService?.Play(SoundKind.RankRiseUp);
                });
                cursor += LiftDuration;
                _sequence.InsertCallback(
                    cursor,
                    presentationTarget.PlayRiseIdle);
                cursor += LiftHold;
                float progress = 0f;
                _sequence.Insert(cursor,
                    DOTween.To(
                            () => progress,
                            value =>
                            {
                                progress = value;
                                ApplyRise(value);
                            },
                            1f,
                            SourceRankActivityLayout.RiseDuration(_effectiveAdvance))
                        .SetEase(Ease.InOutSine));
                cursor += SourceRankActivityLayout.RiseDuration(_effectiveAdvance);
                cursor += SettleHold;
                float dropStart = cursor;
                _sequence.InsertCallback(dropStart, () =>
                {
                    presentationTarget.PlayDrop();
                    _soundService?.Play(SoundKind.RankRiseDown);
                });
                _sequence.InsertCallback(
                    dropStart + RiseDownSwapDelay,
                    FinalizeRise);
                cursor += DropDuration;
                _sequence.InsertCallback(
                    cursor + ArrowHideDelay,
                    presentationTarget.HideArrow);
                cursor += ArrowHideDelay;
            }
            else if (presentationTarget != null && _selfFinal != null)
            {
                cursor = Mathf.Max(
                    cursor,
                    CountStart +
                    RankActivityRowCelebrationView.CollectionDuration);
                _sequence.InsertCallback(cursor, () =>
                    SetSelfRank(_selfFinal.Rank));
                _sequence.InsertCallback(cursor, () =>
                {
                    presentationTarget.PlayLift();
                    _soundService?.Play(SoundKind.RankRiseUp);
                });
                cursor += LiftDuration;
                _sequence.InsertCallback(cursor, () =>
                {
                    presentationTarget.PlayDrop();
                    _soundService?.Play(SoundKind.RankRiseDown);
                });
                cursor += DropDuration;
            }
            _sequence.AppendInterval(Mathf.Max(
                0f,
                cursor + FinishHold - _sequence.Duration()));
            _sequence.OnComplete(FinishChange);
        }

        private void FinishChange()
        {
            _sequence = null;
            _animating = false;
            RestoreFinalLayout();
            if (scroll != null) scroll.enabled = true;
            if (encouragementRoot != null &&
                !string.IsNullOrEmpty(encouragementText?.text))
                encouragementRoot.SetActive(true);
            if (tapButton != null) tapButton.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (IsShowing && IsFloatingSelfActive()) SyncFloatingSelf();
        }

        private void PrepareFloatingSelf(int group, bool active)
        {
            if (!active || _selfFinal == null || celebrateLayer == null ||
                rowPrefab == null)
            {
                if (_floatingSelfRow != null)
                    _floatingSelfRow.gameObject.SetActive(false);
                return;
            }
            if (_floatingSelfRow == null)
            {
                _floatingSelfRow = Instantiate(rowPrefab, celebrateLayer);
                _floatingSelfRow.name = "FloatingSelfRow";
                _floatingSelfRow.transform.SetAsLastSibling();
            }
            _floatingSelfRow.Apply(_selfFinal, group);
            int oldScore = Mathf.Max(0, _selfFinal.Score - _increment);
            int oldRank = oldScore <= 0
                ? 0
                : Mathf.Min(_rowInfos.Count, _selfFinal.Rank + _advance);
            _floatingSelfRow.SetScore(oldScore);
            _floatingSelfRow.SetRank(oldRank);
            _floatingSelfRow.gameObject.SetActive(true);
        }

        private void PrepareScrollAndRise()
        {
            Canvas.ForceUpdateCanvases();
            if (rowList == null || _selfRow == null || scroll?.viewport == null)
                return;
            _rowLayout = rowList.GetComponent<VerticalLayoutGroup>();
            _rowFitter = rowList.GetComponent<ContentSizeFitter>();
            if (_rowLayout != null) _rowLayout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rowList);

            RectTransform selfRect = (RectTransform)_selfRow.transform;
            _rowStride = selfRect.rect.height +
                (_rowLayout != null
                    ? _rowLayout.spacing
                    : SourceRankActivityLayout.RowSpacing);
            _effectiveAdvance = Mathf.Clamp(
                _advance,
                0,
                Mathf.Max(0, _rowInfos.Count - 1 - _selfIndex));
            _displaced.Clear();
            _selfBaseY = selfRect.anchoredPosition.y;
            _risePrepared = _effectiveAdvance > 0;
            if (_rowFitter != null) _rowFitter.enabled = false;
            if (_rowLayout != null) _rowLayout.enabled = false;

            if (_risePrepared)
            {
                selfRect.anchoredPosition = new Vector2(
                    selfRect.anchoredPosition.x,
                    _selfBaseY - _effectiveAdvance * _rowStride);
                float itemProgress = 1f / _effectiveAdvance;
                float progressCursor = 0f;
                for (int index = _selfIndex + _effectiveAdvance;
                     index > _selfIndex;
                     index--)
                {
                    RankActivityRowView view = _rows[index];
                    RectTransform rect = (RectTransform)view.transform;
                    float baseY = rect.anchoredPosition.y;
                    int finalRank = _rowInfos[index].Rank;
                    view.SetRank(Mathf.Max(0, finalRank - 1));
                    rect.anchoredPosition = new Vector2(
                        rect.anchoredPosition.x,
                        baseY + _rowStride);
                    _displaced.Add(new DisplacedRow(
                        view,
                        rect,
                        baseY,
                        progressCursor,
                        Mathf.Min(1f, progressCursor + itemProgress),
                        finalRank));
                    progressCursor += itemProgress;
                }
            }
            CenterScrollOn(selfRect);
            SyncFloatingSelf();
        }

        private void ApplyRise(float progress)
        {
            if (!_risePrepared || _selfRow == null) return;
            progress = Mathf.Clamp01(progress);
            RectTransform selfRect = (RectTransform)_selfRow.transform;
            selfRect.anchoredPosition = new Vector2(
                selfRect.anchoredPosition.x,
                _selfBaseY - _effectiveAdvance * _rowStride * (1f - progress));

            int passed = 0;
            for (int index = 0; index < _displaced.Count; index++)
            {
                DisplacedRow displaced = _displaced[index];
                bool crossed = progress >= displaced.EndProgress;
                if (crossed)
                {
                    passed++;
                    displaced.Rect.anchoredPosition = new Vector2(
                        displaced.Rect.anchoredPosition.x,
                        displaced.BaseY);
                }
                else if (progress < displaced.StartProgress)
                {
                    displaced.Rect.anchoredPosition = new Vector2(
                        displaced.Rect.anchoredPosition.x,
                        displaced.BaseY + _rowStride);
                }
                else
                {
                    float local = Mathf.InverseLerp(
                        displaced.StartProgress,
                        displaced.EndProgress,
                        progress);
                    displaced.Rect.anchoredPosition = new Vector2(
                        displaced.Rect.anchoredPosition.x,
                        displaced.BaseY + _rowStride * (1f - local));
                }
                int shownRank = crossed
                    ? displaced.FinalRank
                    : Mathf.Max(0, displaced.FinalRank - 1);
                if (shownRank != displaced.ShownRank)
                {
                    displaced.ShownRank = shownRank;
                    displaced.View.SetRank(shownRank);
                }
            }

            bool wasUnranked = _selfFinal != null &&
                _selfFinal.Score - _increment <= 0;
            int rank = wasUnranked && passed == 0
                ? 0
                : Mathf.Clamp(
                    _selfFinal.Rank + _effectiveAdvance - passed,
                    _selfFinal.Rank,
                    _selfFinal.Rank + _effectiveAdvance);
            SetSelfRank(rank);
            CenterScrollOn(selfRect);
            SyncFloatingSelf();
        }

        private void FinalizeRise()
        {
            if (_selfRow == null) return;
            for (int index = 0; index < _rows.Count; index++)
            {
                if (index < _rowInfos.Count)
                    _rows[index].ApplyPreservingPresentation(
                        _rowInfos[index],
                        _group);
                _rows[index].transform.localScale = Vector3.one;
            }
            if (_floatingSelfRow != null && _selfFinal != null)
            {
                _floatingSelfRow.ApplyPreservingPresentation(
                    _selfFinal,
                    _group);
            }
            if (_rowLayout != null) _rowLayout.enabled = true;
            if (_rowFitter != null) _rowFitter.enabled = true;
            if (rowList != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowList);
            _risePrepared = false;
            SyncFloatingSelf();
        }

        private void RestoreFinalLayout()
        {
            if (_risePrepared) FinalizeRise();
            if (_selfRow != null && _selfFinal != null)
            {
                _selfRow.SetScore(_selfFinal.Score);
                _selfRow.SetRank(_selfFinal.Rank);
            }
            for (int index = 0; index < _rows.Count; index++)
            {
                if (_rows[index] == null) continue;
                _rows[index].transform.localScale = Vector3.one;
                _rows[index].ShowStatic();
            }
            if (_rowLayout != null) _rowLayout.enabled = true;
            if (_rowFitter != null) _rowFitter.enabled = true;
            if (_floatingSelfRow != null)
            {
                _floatingSelfRow.transform.localScale = Vector3.one;
                _floatingSelfRow.gameObject.SetActive(false);
            }
            _displaced.Clear();
            if (rowList != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowList);
        }

        private void SetSelfScore(int value)
        {
            _selfRow?.SetScore(value);
            if (IsFloatingSelfActive()) _floatingSelfRow.SetScore(value);
        }

        private void SetSelfRank(int value)
        {
            _selfRow?.SetRank(value);
            if (IsFloatingSelfActive()) _floatingSelfRow.SetRank(value);
        }

        private void CenterScrollOn(RectTransform row)
        {
            if (row == null || rowList == null || scroll?.viewport == null) return;
            Vector3 center = rowList.InverseTransformPoint(
                row.TransformPoint(row.rect.center));
            float centerFromTop = rowList.rect.yMax - center.y;
            float rowTopWithoutPadding = centerFromTop - row.rect.height * 0.5f -
                                         SourceRankActivityLayout.ChangeListVerticalPadding;
            float offset = SourceRankActivityLayout.CenteredScrollOffset(
                rowTopWithoutPadding,
                row.rect.height,
                scroll.viewport.rect.height);
            float maximum = Mathf.Max(
                0f,
                rowList.rect.height - scroll.viewport.rect.height);
            Vector2 position = rowList.anchoredPosition;
            position.y = Mathf.Clamp(offset, 0f, maximum);
            rowList.anchoredPosition = position;
            scroll.StopMovement();
        }

        private bool IsVisible(RankActivityRowView row)
        {
            if (row == null || rowList == null || scroll?.viewport == null)
                return false;
            RectTransform rect = (RectTransform)row.transform;
            Vector3 center = rowList.InverseTransformPoint(
                rect.TransformPoint(rect.rect.center));
            float centerFromTop = rowList.rect.yMax - center.y;
            float scrollTop = rowList.anchoredPosition.y;
            float half = rect.rect.height * 0.5f;
            return centerFromTop + half > scrollTop &&
                   centerFromTop - half < scrollTop + scroll.viewport.rect.height;
        }

        private void SyncFloatingSelf()
        {
            if (_selfRow == null || !IsFloatingSelfActive()) return;
            _floatingSelfRow.transform.position = _selfRow.transform.position;
        }

        private bool IsFloatingSelfActive()
        {
            return _floatingSelfRow != null &&
                   _floatingSelfRow.gameObject.activeSelf;
        }

        private void ApplyEncouragement(RankProgressEncouragement value)
        {
            string text = string.Empty;
            if (value.Kind == RankEncouragementKind.Reach)
            {
                int rank = Mathf.Clamp(value.Rank, 1, 3);
                text = Translate(
                    $"RANK_ENCOURAGE_REACH_{rank}",
                    ReachFallback(rank));
            }
            else if (value.Kind == RankEncouragementKind.Climb)
            {
                int variant = UnityEngine.Random.Range(1, 5);
                text = Translate(
                        $"RANK_ENCOURAGE_CLIMB_{variant}",
                        ClimbFallback(variant))
                    .Replace("%d", value.Advance.ToString());
            }
            if (encouragementText != null)
                encouragementText.text =
                    RankPresentationContract.GodotRichTextToPlainText(text);
        }

        private void Subscribe()
        {
            if (_subscribed || !IsShowing || _runtime?.Manager == null) return;
            _runtime.Manager.RankingChanged += HandleRankingChanged;
            _runtime.Manager.TimeTicked += HandleTimeTicked;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_runtime?.Manager != null)
            {
                _runtime.Manager.RankingChanged -= HandleRankingChanged;
                _runtime.Manager.TimeTicked -= HandleTimeTicked;
            }
            _subscribed = false;
        }

        private void HandleRankingChanged()
        {
            if (!_animating && _runtime?.Manager != null)
            {
                BuildRows(_runtime.Manager.GetRankInfos(),
                    _runtime.Manager.Group,
                    false);
                if (scroll != null) scroll.enabled = true;
            }
        }

        private void HandleTimeTicked(int remaining, bool _)
        {
            RefreshCountdown(remaining);
        }

        private void RefreshCountdown(int seconds)
        {
            if (countdownText != null)
                countdownText.text = RankPresentationContract.FormatHms(seconds);
        }

        private void RefreshText()
        {
            if (titleText != null)
                titleText.text = Translate("RANK_TITLE", "Leaderboard");
            if (tapText != null)
                tapText.text = Translate(
                    "RANK_TAP_CONTINUE",
                    "Tap to Continue");
        }

        private void EnsureRows(int count)
        {
            if (rowPrefab == null || rowList == null) return;
            while (_rows.Count < count)
            {
                RankActivityRowView row = Instantiate(rowPrefab, rowList);
                row.name = $"RankRow_{_rows.Count + 1:00}";
                _rows.Add(row);
            }
        }

        private void Dismiss()
        {
            if (_animating) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Continue,
                GetTrackingDialogName());
            Owner?.Hide(UiName);
        }

        private void KillSequence()
        {
            _sequence?.Kill(false);
            _sequence = null;
        }

        private sealed class DisplacedRow
        {
            public DisplacedRow(
                RankActivityRowView view,
                RectTransform rect,
                float baseY,
                float startProgress,
                float endProgress,
                int finalRank)
            {
                View = view;
                Rect = rect;
                BaseY = baseY;
                StartProgress = startProgress;
                EndProgress = endProgress;
                FinalRank = finalRank;
                ShownRank = Mathf.Max(0, finalRank - 1);
            }

            public RankActivityRowView View { get; }
            public RectTransform Rect { get; }
            public float BaseY { get; }
            public float StartProgress { get; }
            public float EndProgress { get; }
            public int FinalRank { get; }
            public int ShownRank { get; set; }
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static int ReadInt(
            IReadOnlyDictionary<string, object> parameters,
            string key,
            int fallback)
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out object value))
                return fallback;
            try { return Convert.ToInt32(value); }
            catch (Exception) { return fallback; }
        }

        private static string ReachFallback(int rank)
        {
            return rank switch
            {
                1 => "Outstanding! You placed in the 1st position.",
                2 => "Great job! You have claimed the 2nd place.",
                _ => "Congratulations! You are now in the 3rd spot."
            };
        }

        private static string ClimbFallback(int variant)
        {
            return variant switch
            {
                1 => "Impressive progress! You jumped up %d places.",
                2 => "Bravo! Your rank increased by %d spots.",
                3 => "Excellent! You climbed up %d spots fast.",
                _ => "Nice work! You rose by %d places."
            };
        }
    }
}
