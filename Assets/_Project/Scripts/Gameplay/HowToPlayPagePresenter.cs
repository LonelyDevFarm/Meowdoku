using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.UI;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HowToPlayPagePresenter : UIFrameWindow
    {
        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private Button tapCatcher;
        [SerializeField] private HowToPlayDemoBoardView[] boards;
        [SerializeField] private SoundService soundService;

        private Coroutine _demoCoroutine;
        private int _demoToken;

        public string FailureReason { get; private set; } = string.Empty;

        protected override void OnCreate()
        {
            if (tapCatcher != null)
                tapCatcher.onClick.AddListener(Close);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            StopDemo();
            FailureReason = string.Empty;
            if (!PrepareBoards())
            {
                FailureReason = "How-to-play demo board references are invalid.";
                return;
            }

            soundService?.SetSilent(true);
            popupAnimator?.PlayOpen();
            int token = ++_demoToken;
            _demoCoroutine = StartCoroutine(RunDemo(token));
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
            if (tapCatcher != null)
                tapCatcher.onClick.RemoveListener(Close);
            base.OnDestroyWindow();
        }

        protected override bool OnBackRequest()
        {
            Close();
            return true;
        }

        public void BindSoundService(SoundService service)
        {
            soundService = service;
        }

        private bool PrepareBoards()
        {
            if (boards == null ||
                boards.Length != HowToPlayContract.FullDemos.Count)
                return false;
            for (int index = 0; index < boards.Length; index++)
            {
                if (boards[index] == null ||
                    !boards[index].ApplyColors(
                        HowToPlayContract.FullDemos[index].Colors))
                    return false;
            }
            return true;
        }

        private IEnumerator RunDemo(int token)
        {
            ResetAll();
            for (int index = 1; index < HowToPlayContract.FullDemos.Count; index++)
                FillDemoComplete(index);

            yield return WaitFrames(
                HowToPlayContract.FullStartDelayFrames,
                token);
            if (!IsDemoActive(token)) yield break;
            yield return PlayDemo(0, token);
            if (!IsDemoActive(token)) yield break;

            int current = 0;
            while (IsDemoActive(token))
            {
                int next = (current + 1) % HowToPlayContract.FullDemos.Count;
                int gap = current == HowToPlayContract.FullDemos.Count - 1
                    ? HowToPlayContract.FullLastGapFrames
                    : HowToPlayContract.FullGapFrames;
                yield return WaitFrames(gap, token);
                if (!IsDemoActive(token)) yield break;
                yield return ClearBoard(next, token);
                if (!IsDemoActive(token)) yield break;
                yield return PlayDemo(next, token);
                current = next;
            }
        }

        private IEnumerator PlayDemo(int index, int token)
        {
            HowToPlayFullDemo demo = HowToPlayContract.FullDemos[index];
            HowToPlayDemoBoardView board = boards[index];
            for (int i = 0; i < demo.AnimatedCats.Count; i++)
                board.Cell(demo.AnimatedCats[i])?.PlayDemoCat(true);
            for (int i = 0; i < demo.StaticCats.Count; i++)
                board.Cell(demo.StaticCats[i])?.PlayDemoCat(false);
            for (int i = 0; i < demo.StaticMarks.Count; i++)
                board.Cell(demo.StaticMarks[i])?.PlayDemoMark(true);

            List<TimedEvent> events = BuildEvents(demo);
            int lastFrame = 0;
            float demoEnd = 0f;
            for (int indexInEvents = 0;
                 indexInEvents < events.Count;
                 indexInEvents++)
            {
                TimedEvent timed = events[indexInEvents];
                int frameDelta = timed.Frame - lastFrame;
                if (frameDelta > 0)
                    yield return WaitFrames(frameDelta, token);
                if (!IsDemoActive(token)) yield break;
                lastFrame = timed.Frame;
                CellView cell = board.Cell(timed.Cell);
                if (timed.Error) cell?.PlayDemoError();
                else cell?.PlayDemoMark();
                float length = timed.Error
                    ? HowToPlayContract.ErrorAnimationSeconds
                    : HowToPlayContract.CrossAnimationSeconds;
                demoEnd = Mathf.Max(
                    demoEnd,
                    HowToPlayContract.SecondsAtFrame(timed.Frame) + length);
            }

            float tail = demoEnd - HowToPlayContract.SecondsAtFrame(lastFrame);
            if (tail > 0f) yield return WaitSeconds(tail, token);
        }

        private IEnumerator ClearBoard(int index, int token)
        {
            List<HowToPlayCell> cells = ClearableCells(
                HowToPlayContract.FullDemos[index]);
            for (int i = 0; i < cells.Count; i++)
                boards[index].Cell(cells[i])?.PlayDemoDisappear();
            if (cells.Count == 0) yield break;
            yield return WaitSeconds(
                HowToPlayContract.DemoDisappearSeconds,
                token);
            if (!IsDemoActive(token)) yield break;
            for (int i = 0; i < cells.Count; i++)
                boards[index].Cell(cells[i])?.ClearDemo();
        }

        private void FillDemoComplete(int index)
        {
            HowToPlayFullDemo demo = HowToPlayContract.FullDemos[index];
            HowToPlayDemoBoardView board = boards[index];
            for (int i = 0; i < demo.AnimatedCats.Count; i++)
                board.Cell(demo.AnimatedCats[i])?.PlayDemoCat(false);
            for (int i = 0; i < demo.StaticCats.Count; i++)
                board.Cell(demo.StaticCats[i])?.PlayDemoCat(false);
            for (int i = 0; i < demo.StaticMarks.Count; i++)
                board.Cell(demo.StaticMarks[i])?.PlayDemoMark(true);
            if (demo.HasError)
                board.Cell(demo.ErrorCell)?.PlayDemoError(true);
            for (int waveIndex = 0; waveIndex < demo.Waves.Count; waveIndex++)
            {
                HowToPlayWave wave = demo.Waves[waveIndex];
                for (int cellIndex = 0; cellIndex < wave.Cells.Count; cellIndex++)
                    board.Cell(wave.Cells[cellIndex])?.PlayDemoMark(true);
            }
        }

        private void ResetAll()
        {
            if (boards == null) return;
            for (int index = 0; index < boards.Length; index++)
                boards[index]?.ResetAll();
        }

        private List<TimedEvent> BuildEvents(HowToPlayFullDemo demo)
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
                        cellIndex * HowToPlayContract.FullCrossStepFrames,
                        wave.Cells[cellIndex],
                        false));
            }
            events.Sort((a, b) => a.Frame.CompareTo(b.Frame));
            return events;
        }

        private static List<HowToPlayCell> ClearableCells(
            HowToPlayFullDemo demo)
        {
            var cells = new List<HowToPlayCell>();
            AddUnique(cells, demo.AnimatedCats);
            if (demo.HasError) AddUnique(cells, demo.ErrorCell);
            for (int waveIndex = 0; waveIndex < demo.Waves.Count; waveIndex++)
                AddUnique(cells, demo.Waves[waveIndex].Cells);

            for (int index = cells.Count - 1; index >= 0; index--)
            {
                if (Contains(demo.StaticCats, cells[index]) ||
                    Contains(demo.StaticMarks, cells[index]))
                    cells.RemoveAt(index);
            }
            return cells;
        }

        private static void AddUnique(
            List<HowToPlayCell> output,
            IReadOnlyList<HowToPlayCell> source)
        {
            for (int index = 0; index < source.Count; index++)
                AddUnique(output, source[index]);
        }

        private static void AddUnique(
            List<HowToPlayCell> output,
            HowToPlayCell value)
        {
            if (!Contains(output, value)) output.Add(value);
        }

        private static bool Contains(
            IReadOnlyList<HowToPlayCell> values,
            HowToPlayCell value)
        {
            for (int index = 0; index < values.Count; index++)
                if (values[index].Equals(value)) return true;
            return false;
        }

        private IEnumerator WaitFrames(int frames, int token) =>
            WaitSeconds(HowToPlayContract.SecondsAtFrame(frames), token);

        private IEnumerator WaitSeconds(float seconds, int token)
        {
            float end = Time.unscaledTime + Mathf.Max(0f, seconds);
            while (IsDemoActive(token) && Time.unscaledTime < end)
                yield return null;
        }

        private bool IsDemoActive(int token) =>
            token == _demoToken && IsShowing && isActiveAndEnabled;

        private void StopDemo()
        {
            _demoToken++;
            if (_demoCoroutine != null) StopCoroutine(_demoCoroutine);
            _demoCoroutine = null;
            ResetAll();
            soundService?.SetSilent(false);
        }

        private void Close()
        {
            Owner?.Hide(UiName.HowToPlay);
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
