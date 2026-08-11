using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityChangePresenter : UIFrameWindow,
        IRankActivityConsumer
    {
        public override string GetTrackingDialogName() =>
            TrackerCatalog.Dialog.ChallengeRank;

        private const float AppearStart = 0.2f;
        private const float AppearInterval = 0.0667f;
        private const float CountStart = 0.7333f;
        private const float ScoreRollDelay = 1f;
        private const float ScoreRollDuration = 0.6333f;
        private const float FinishHold = 0.5f;

        [SerializeField] private Text titleText;
        [SerializeField] private Text countdownText;
        [SerializeField] private GameObject encouragementRoot;
        [SerializeField] private Text encouragementText;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform rowList;
        [SerializeField] private RankActivityRowView rowPrefab;
        [SerializeField] private Button maskButton;
        [SerializeField] private Button tapButton;
        [SerializeField] private Text tapText;
        [SerializeField] private LocalizationCatalog localization;

        private readonly List<RankActivityRowView> _rows = new();
        private RankActivityRuntime _runtime;
        private Sequence _sequence;
        private bool _animating;
        private bool _subscribed;
        private int _advance;
        private int _increment;
        private RankActivityRowView _selfRow;
        private RankInfo _selfFinal;

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
            _selfRow = null;
            _selfFinal = null;
            for (int index = 0; index < _rows.Count; index++)
            {
                RankInfo info = index < infos.Count ? infos[index] : null;
                _rows[index].Apply(info, group);
                CanvasGroup canvas = GetCanvas(_rows[index]);
                canvas.alpha = info == null ? 0f : 1f;
                _rows[index].transform.localScale = Vector3.one;
                if (info?.IsSelf != true) continue;
                _selfRow = _rows[index];
                _selfFinal = info;
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
            if (scroll != null)
            {
                scroll.verticalNormalizedPosition = 1f;
                scroll.enabled = false;
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

            _sequence = DOTween.Sequence().SetLink(gameObject);
            int visibleIndex = 0;
            for (int index = 0; index < _rows.Count; index++)
            {
                RankActivityRowView row = _rows[index];
                if (row == null || !row.gameObject.activeSelf) continue;
                CanvasGroup canvas = GetCanvas(row);
                canvas.alpha = 0f;
                _sequence.Insert(
                    AppearStart + AppearInterval * visibleIndex,
                    canvas.DOFade(1f, 0.16f).SetEase(Ease.Linear));
                visibleIndex++;
            }

            float cursor = CountStart + ScoreRollDelay;
            if (_selfRow != null && _selfFinal != null && _increment > 0)
            {
                int from = Mathf.Max(0, _selfFinal.Score - _increment);
                int to = _selfFinal.Score;
                _sequence.Insert(
                    cursor,
                    DOTween.To(
                            () => from,
                            value => _selfRow?.SetScore(value),
                            to,
                            ScoreRollDuration)
                        .SetEase(Ease.OutQuad));
                cursor += ScoreRollDuration;
            }
            if (_selfRow != null && _selfFinal != null && _advance > 0)
            {
                _sequence.Insert(cursor,
                    _selfRow.transform.DOScale(1.08f, 0.22f)
                        .SetEase(Ease.OutQuad));
                _sequence.InsertCallback(cursor + 0.22f, () =>
                {
                    _selfRow?.SetRank(_selfFinal.Rank);
                });
                _sequence.Insert(cursor + 0.22f,
                    _selfRow.transform.DOScale(1f, 0.28f)
                        .SetEase(Ease.InOutSine));
                cursor += 0.5f;
            }
            else if (_selfRow != null && _selfFinal != null)
            {
                _sequence.InsertCallback(cursor, () =>
                    _selfRow?.SetRank(_selfFinal.Rank));
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
            if (_selfRow != null && _selfFinal != null)
            {
                _selfRow.SetScore(_selfFinal.Score);
                _selfRow.SetRank(_selfFinal.Rank);
                _selfRow.transform.localScale = Vector3.one;
            }
            if (scroll != null) scroll.enabled = true;
            if (encouragementRoot != null &&
                !string.IsNullOrEmpty(encouragementText?.text))
                encouragementRoot.SetActive(true);
            if (tapButton != null) tapButton.gameObject.SetActive(true);
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

        private static CanvasGroup GetCanvas(RankActivityRowView row)
        {
            CanvasGroup group = row.GetComponent<CanvasGroup>();
            return group != null
                ? group
                : row.gameObject.AddComponent<CanvasGroup>();
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
