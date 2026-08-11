using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HowToPlayPagedPagePresenter : UIFrameWindow
    {
        public event Action Closed;

        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private HowToPlayDemoBoardView[] boards;
        [SerializeField] private RectTransform[] boardRects;
        [SerializeField] private Text caption;
        [SerializeField] private Button backButton;
        [SerializeField] private Button mainButton;
        [SerializeField] private LocalizedText mainLabel;
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private SoundService soundService;

        private readonly List<float> _boardRestX = new(3);
        private Coroutine _demoCoroutine;
        private Tween _slideTween;
        private int _demoToken;
        private int _page;
        private bool _closedRaised;

        public int PageIndex => _page;
        public string FailureReason { get; private set; } = string.Empty;

        protected override void OnCreate()
        {
            if (backButton != null)
                backButton.onClick.AddListener(Previous);
            if (mainButton != null)
                mainButton.onClick.AddListener(NextOrClose);
            if (localization != null)
                localization.LocaleChanged += RefreshText;
            CaptureBoardRestPositions();
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            StopDemo();
            _closedRaised = false;
            FailureReason = string.Empty;
            if (!PrepareBoards())
            {
                FailureReason = "Paged How-to-play board references are invalid.";
                return;
            }

            soundService?.SetSilent(true);
            popupAnimator?.PlayOpen();
            GoToPage(0, false);
        }

        protected override IEnumerator PlayCloseAnimation()
        {
            StopDemo();
            if (popupAnimator != null)
                yield return popupAnimator.PlayClose();
        }

        protected override IEnumerator OnHide()
        {
            StopDemo();
            popupAnimator?.Stop();
            yield break;
        }

        protected override void OnDestroyWindow()
        {
            StopDemo();
            popupAnimator?.Stop();
            if (backButton != null)
                backButton.onClick.RemoveListener(Previous);
            if (mainButton != null)
                mainButton.onClick.RemoveListener(NextOrClose);
            if (localization != null)
                localization.LocaleChanged -= RefreshText;
            Closed = null;
            base.OnDestroyWindow();
        }

        protected override bool OnBackRequest()
        {
            Close();
            return true;
        }

        protected override void OnCloseButtonPressed()
        {
            RaiseClosed();
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            if (localization == catalog) return;
            if (localization != null)
                localization.LocaleChanged -= RefreshText;
            localization = catalog;
            if (localization != null)
                localization.LocaleChanged += RefreshText;
            mainLabel?.Bind(catalog);
            RefreshText();
        }

        public void BindSoundService(SoundService service)
        {
            soundService = service;
        }

        private bool PrepareBoards()
        {
            if (boards == null || boardRects == null ||
                boards.Length != HowToPlayContract.PagedDemos.Count ||
                boardRects.Length != boards.Length)
                return false;
            for (int index = 0; index < boards.Length; index++)
            {
                if (boards[index] == null || boardRects[index] == null ||
                    !boards[index].ApplyColors(
                        HowToPlayContract.PagedDemos[index].Colors))
                    return false;
            }
            CaptureBoardRestPositions();
            return true;
        }

        private void CaptureBoardRestPositions()
        {
            _boardRestX.Clear();
            if (boardRects == null) return;
            for (int index = 0; index < boardRects.Length; index++)
                _boardRestX.Add(
                    boardRects[index] != null
                        ? boardRects[index].anchoredPosition.x
                        : 0f);
        }

        private void GoToPage(int index, bool slide = true)
        {
            int previous = _page;
            _page = Mathf.Clamp(
                index,
                0,
                HowToPlayContract.PagedDemos.Count - 1);
            for (int pageIndex = 0; pageIndex < boards.Length; pageIndex++)
                boards[pageIndex].gameObject.SetActive(pageIndex == _page);
            RefreshText();
            RefreshButtons();
            if (slide) AnimateSwitch(_page >= previous ? 1 : -1);
            else ClearSlide();

            _demoToken++;
            if (_demoCoroutine != null) StopCoroutine(_demoCoroutine);
            int token = _demoToken;
            _demoCoroutine = StartCoroutine(RunDemo(token, _page));
        }

        private void AnimateSwitch(int direction)
        {
            _slideTween?.Kill(false);
            if (_page < 0 || _page >= boardRects.Length ||
                _page >= _boardRestX.Count)
                return;
            RectTransform board = boardRects[_page];
            float rest = _boardRestX[_page];
            Vector2 position = board.anchoredPosition;
            position.x = rest + direction * HowToPlayContract.PagedSlideDistance;
            board.anchoredPosition = position;
            _slideTween = board.DOAnchorPosX(
                    rest,
                    HowToPlayContract.PagedSlideSeconds)
                .SetEase(Ease.OutQuart)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => _slideTween = null);
        }

        private void ClearSlide()
        {
            _slideTween?.Kill(false);
            _slideTween = null;
            if (_page < 0 || _page >= boardRects.Length ||
                _page >= _boardRestX.Count)
                return;
            Vector2 position = boardRects[_page].anchoredPosition;
            position.x = _boardRestX[_page];
            boardRects[_page].anchoredPosition = position;
        }

        private void RefreshButtons()
        {
            bool first = _page == 0;
            bool last = _page == HowToPlayContract.PagedDemos.Count - 1;
            if (backButton != null) backButton.gameObject.SetActive(!first);
            if (mainLabel != null)
                mainLabel.SetKey(last
                    ? "HOW_TO_PLAY_GOT_IT"
                    : "HOW_TO_PLAY_NEXT");
            if (mainButton == null) return;
            RectTransform rect = (RectTransform)mainButton.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(
                first ? 260f : 365f,
                -1675f);
            rect.sizeDelta = new Vector2(560f, 160f);
        }

        private void RefreshText()
        {
            if (caption == null || _page < 0 ||
                _page >= HowToPlayContract.PagedDemos.Count)
                return;
            HowToPlayPagedDemo demo = HowToPlayContract.PagedDemos[_page];
            string value = localization != null
                ? localization.Translate(demo.CaptionKey)
                : demo.CaptionKey;
            string keyword = HowToPlayContract.HighlightKeyword(
                demo.CaptionKey,
                localization != null ? localization.Locale : "en");
            if (!string.IsNullOrEmpty(keyword) && value.Contains(keyword))
                value = value.Replace(
                    keyword,
                    $"<color={HowToPlayContract.HighlightColor}>{keyword}</color>");
            caption.text = value;
        }

        private IEnumerator RunDemo(int token, int page)
        {
            HowToPlayPagedDemo demo = HowToPlayContract.PagedDemos[page];
            HowToPlayDemoBoardView board = boards[page];
            List<TimedEvent> events = BuildEvents(demo);
            while (IsDemoActive(token, page))
            {
                board.ResetAll();
                yield return WaitFrames(
                    HowToPlayContract.PagedStartDelayFrames,
                    token,
                    page);
                if (!IsDemoActive(token, page)) yield break;
                board.Cell(demo.Cat)?.PlayDemoCat(true);

                int lastFrame = 0;
                for (int eventIndex = 0;
                     eventIndex < events.Count;
                     eventIndex++)
                {
                    TimedEvent timed = events[eventIndex];
                    int delta = timed.Frame - lastFrame;
                    if (delta > 0) yield return WaitFrames(delta, token, page);
                    if (!IsDemoActive(token, page)) yield break;
                    lastFrame = timed.Frame;
                    CellView cell = board.Cell(timed.Cell);
                    if (timed.Error) cell?.PlayDemoError();
                    else cell?.PlayDemoMark();
                }
                yield return WaitSeconds(
                    HowToPlayContract.PagedHoldAfterSeconds,
                    token,
                    page);
            }
        }

        private static List<TimedEvent> BuildEvents(HowToPlayPagedDemo demo)
        {
            var events = new List<TimedEvent>();
            if (demo.HasError)
                events.Add(new TimedEvent(
                    demo.ErrorFrame,
                    demo.ErrorCell,
                    true));
            for (int waveIndex = 0; waveIndex < demo.Waves.Count; waveIndex++)
            {
                HowToPlayWave wave = demo.Waves[waveIndex];
                for (int cellIndex = 0; cellIndex < wave.Cells.Count; cellIndex++)
                    events.Add(new TimedEvent(
                        wave.StartFrame +
                        cellIndex * HowToPlayContract.PagedCrossStepFrames,
                        wave.Cells[cellIndex],
                        false));
            }
            events.Sort((a, b) => a.Frame.CompareTo(b.Frame));
            return events;
        }

        private IEnumerator WaitFrames(int frames, int token, int page) =>
            WaitSeconds(HowToPlayContract.SecondsAtFrame(frames), token, page);

        private IEnumerator WaitSeconds(
            float seconds,
            int token,
            int page)
        {
            float end = Time.unscaledTime + Mathf.Max(0f, seconds);
            while (IsDemoActive(token, page) && Time.unscaledTime < end)
                yield return null;
        }

        private bool IsDemoActive(int token, int page) =>
            token == _demoToken && page == _page && IsShowing &&
            isActiveAndEnabled;

        private void StopDemo()
        {
            _demoToken++;
            if (_demoCoroutine != null) StopCoroutine(_demoCoroutine);
            _demoCoroutine = null;
            _slideTween?.Kill(false);
            _slideTween = null;
            if (boards != null)
            {
                for (int index = 0; index < boards.Length; index++)
                    boards[index]?.ResetAll();
            }
            soundService?.SetSilent(false);
        }

        private void Previous()
        {
            if (_page > 0) GoToPage(_page - 1);
        }

        private void NextOrClose()
        {
            if (_page >= HowToPlayContract.PagedDemos.Count - 1) Close();
            else GoToPage(_page + 1);
        }

        private void Close()
        {
            RaiseClosed();
            Owner?.Hide(UiName.HowToPlayPaged);
        }

        private void RaiseClosed()
        {
            if (_closedRaised) return;
            _closedRaised = true;
            Closed?.Invoke();
        }

        private readonly struct TimedEvent
        {
            public TimedEvent(int frame, HowToPlayCell cell, bool error)
            {
                Frame = frame;
                Cell = cell;
                Error = error;
            }

            public int Frame { get; }
            public HowToPlayCell Cell { get; }
            public bool Error { get; }
        }
    }
}
