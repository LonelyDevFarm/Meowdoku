using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Tutorial;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TutorialPagePresenter : UIFrameWindow,
        IAbConfigRuntimeConsumer
    {
        private const float SourceMaskFadeSeconds = 0.12f;
        private const float SourceCheckSeconds = 0.95f;
        private const float SourceIqGrowSeconds = 0.4f;
        private const float SourceMessageToBoardGap = 78f;
        private const float SourceSubMessageToBoardGap = 30f;

        [Header("Board")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private RectTransform layoutSpace;
        [SerializeField] private RectTransform boardContainer;
        [SerializeField] private CanvasGroup boardInputGroup;
        [SerializeField] private GameObject cellPrefab;

        [Header("Mask and guidance")]
        [SerializeField] private GameObject maskLayer;
        [SerializeField] private CanvasGroup maskGroup;
        [SerializeField] private RectTransform maskCellLayer;
        [SerializeField] private RectTransform selectFrame;
        [SerializeField] private RectTransform handHint;
        [SerializeField] private Image handImage;

        [Header("Messages and controls")]
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject subMessagePanel;
        [SerializeField] private Text subMessageText;
        [SerializeField] private GameObject hintPanel;
        [SerializeField] private Text hintText;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text confirmText;
        [SerializeField] private LocalizationCatalog localization;

        [Header("Feedback")]
        [SerializeField] private GameObject successCheck;
        [SerializeField] private CanvasGroup successCheckGroup;
        [SerializeField] private GameObject iqBar;
        [SerializeField] private RectTransform iqFill;
        [SerializeField] private Text iqText;
        [SerializeField] private TutorialFinishEffects finishEffects;

        private readonly Dictionary<Vector2Int, CellView> _maskCells = new(12);
        private readonly Dictionary<Vector2Int, Image> _maskStateIcons = new(12);
        private readonly Vector3[] _maskIconWorldCorners = new Vector3[4];
        private RectTransform _maskStateIconLayer;
        private TutorialStateMachine _machine;
        private TutorialCompletionCommitter _completionCommitter;
        private GuideFeedbackConfig _feedbackConfig;
        private TutorialPhase _presentedPhase;
        private Tween _phaseTween;
        private Tween _feedbackTween;
        private Tween _maskTween;
        private Tween _handTween;
        private Tween _confirmTween;
        private float _feedbackDelay;
        private bool _finishEffectPlayed;
        private bool _routeCommitted;
        private AbConfigRuntime _abConfigRuntime;
        private Vector2Int _gestureStartCell = new(-1, -1);
        private CellStateType _gestureStartState;

        public string FailureReason { get; private set; } = string.Empty;
        public TutorialPhase Phase => _machine?.Phase ?? TutorialPhase.PlaceFirstCat;

        public void BindAbConfigRuntime(AbConfigRuntime runtime)
        {
            _abConfigRuntime = runtime;
        }

        protected override void OnCreate()
        {
            BindButtons();
            if (localization != null)
                localization.LocaleChanged += RefreshLocalizedPresentation;
            ResetVisuals();
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            CleanupRuntime(clearBoard: true);
            BindButtons();
            FailureReason = string.Empty;
            _routeCommitted = false;
            _finishEffectPlayed = false;
            _completionCommitter = new TutorialCompletionCommitter();

            if (boardView == null || !TutorialPuzzle.TryLoadFromBank(
                    out TutorialPuzzle puzzle))
            {
                FailureReason = boardView == null
                    ? "Tutorial BoardView reference is missing."
                    : "Source guide puzzle is missing from the special bank.";
                SetBoardInput(false);
                return;
            }

            _feedbackConfig = _abConfigRuntime?.Input.GuideFeedback ??
                              new GuideFeedbackConfig();
            TutorialDiagonalConfig diagonalConfig =
                _abConfigRuntime?.Input.TutorialDiagonal ??
                new TutorialDiagonalConfig();
            DoubleTapProtectConfig doubleTapConfig =
                _abConfigRuntime?.Input.DoubleTapProtect ??
                new DoubleTapProtectConfig();
            _machine = new TutorialStateMachine(
                puzzle,
                _feedbackConfig,
                diagonalConfig,
                doubleTapConfig);
            SubscribeMachine();
            SubscribeBoard();

            boardView.SetupBoard(
                TutorialPuzzle.SourceSize,
                puzzle.Regions,
                puzzle.ColorMap,
                regionColorConfig: _abConfigRuntime?.Board.RegionColor ??
                                   new RegionColorConfig(),
                gridUiConfig: _abConfigRuntime?.Board.GameGridUi ??
                              new GameGridUiConfig(),
                boardSizeBigConfig: _abConfigRuntime?.Board.BoardSizeBig ??
                                    new BoardSizeBigConfig(),
                visibleBoardPixelsOverride: TutorialPuzzle.SourceBoardWidth);

            InitializeIqBar();
            _presentedPhase = _machine.Phase;
            RenderPhase(_machine.Phase, true);
        }

        protected override IEnumerator OnHide()
        {
            CleanupRuntime(clearBoard: true);
            yield break;
        }

        protected override void OnDestroyWindow()
        {
            CleanupRuntime(clearBoard: true);
            if (localization != null)
                localization.LocaleChanged -= RefreshLocalizedPresentation;
            base.OnDestroyWindow();
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            if (localization == catalog) return;
            if (localization != null)
                localization.LocaleChanged -= RefreshLocalizedPresentation;
            localization = catalog;
            if (localization != null)
                localization.LocaleChanged += RefreshLocalizedPresentation;
            RefreshLocalizedPresentation();
        }

        protected override bool OnBackRequest()
        {
            return true;
        }

        private void BindButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(Confirm);
                confirmButton.onClick.AddListener(Confirm);
            }
            if (hintButton != null)
            {
                hintButton.onClick.RemoveListener(ShowHint);
                hintButton.onClick.AddListener(ShowHint);
            }
        }

        private void SubscribeMachine()
        {
            _machine.PhaseChanged += HandlePhaseChanged;
            _machine.BoardChanged += HandleBoardChanged;
            _machine.FeedbackRequested += HandleFeedbackRequested;
            _machine.PresentationChanged += HandlePresentationChanged;
        }

        private void UnsubscribeMachine()
        {
            if (_machine == null) return;
            _machine.PhaseChanged -= HandlePhaseChanged;
            _machine.BoardChanged -= HandleBoardChanged;
            _machine.FeedbackRequested -= HandleFeedbackRequested;
            _machine.PresentationChanged -= HandlePresentationChanged;
        }

        private void SubscribeBoard()
        {
            boardView.OnGesturePointerStarted += HandleGestureStarted;
            boardView.OnGesturePointerMoved += HandleGestureMoved;
            boardView.OnGestureEnded += HandleGestureEnded;
        }

        private void UnsubscribeBoard()
        {
            if (boardView == null) return;
            boardView.OnGesturePointerStarted -= HandleGestureStarted;
            boardView.OnGesturePointerMoved -= HandleGestureMoved;
            boardView.OnGestureEnded -= HandleGestureEnded;
        }

        private void HandleGestureStarted(
            Vector2 boardPosition,
            Vector2Int cell,
            int timestampMilliseconds)
        {
            _gestureStartCell = new Vector2Int(cell.y, cell.x);
            _gestureStartState = _machine != null && cell.x >= 0
                ? _machine.GetCellState(cell.y, cell.x)
                : CellStateType.EMPTY;
            if (_machine?.BeginGesture(cell.y, cell.x) != true)
                _gestureStartCell = new Vector2Int(-1, -1);
        }

        private void HandleGestureMoved(Vector2 boardPosition, int timestampMilliseconds)
        {
            if (_machine == null || boardView == null) return;
            Vector2Int cell = boardView.PointerToCell(boardPosition);
            if (cell.x >= 0 && _machine.DragOver(cell.y, cell.x))
                VibrationRuntime.Current.Play(VibrationLevel.Level2);
        }

        private void HandleGestureEnded()
        {
            if (_machine == null)
            {
                ClearGestureState();
                return;
            }
            TutorialPhase phaseBefore = _machine.Phase;
            bool accepted = _machine.EndGesture(Time.unscaledTimeAsDouble);
            PlayTapVibration(phaseBefore, accepted);
            ClearGestureState();
        }

        private void HandleBoardChanged(IReadOnlyList<BoardStateChange> changes)
        {
            for (int index = 0; index < changes.Count; index++)
            {
                BoardStateChange change = changes[index];
                int row = change.Position.x;
                int column = change.Position.y;
                boardView?.SetCellState(row, column, change.After);
                UpdateMaskStateIcon(change.Position, change.After);
            }
        }

        private void HandlePhaseChanged(TutorialPhase phase)
        {
            if (phase == TutorialPhase.Completed)
            {
                CompleteTutorial();
                return;
            }

            float delay = TransitionDelayAfter(_presentedPhase);
            BeginTransition();
            if (phase == TutorialPhase.Feedback)
            {
                _feedbackDelay = delay;
                return;
            }

            _phaseTween?.Kill(false);
            if (delay <= 0f)
            {
                RenderPhase(phase, true);
                return;
            }
            _phaseTween = DOVirtual.DelayedCall(
                    delay,
                    () => RenderPhase(phase, true),
                    true)
                .SetId(this)
                .SetLink(gameObject);
        }

        private void HandleFeedbackRequested(
            TutorialFeedbackKind kind,
            int before,
            int after)
        {
            _feedbackTween?.Kill(false);
            if (kind == TutorialFeedbackKind.Check)
                PlayCheckFeedback(_feedbackDelay);
            else if (kind == TutorialFeedbackKind.Iq)
                PlayIqFeedback(before, after, _feedbackDelay);
        }

        private void HandlePresentationChanged()
        {
            if (_machine != null && _machine.Phase == TutorialPhase.FreePlay &&
                _presentedPhase == TutorialPhase.FreePlay)
                RenderPhase(TutorialPhase.FreePlay, true);
        }

        private void Confirm()
        {
            _machine?.Confirm();
        }

        private void ShowHint()
        {
            if (_machine == null) return;
            int hintPhaseBefore = _machine.HintPhase;
            TutorialPhase phaseBefore = _machine.Phase;
            if (!_machine.PressHint()) return;
            if (hintPhaseBefore == 1 || hintPhaseBefore == 2)
                VibrationRuntime.Current.Play(VibrationLevel.Level2);
            else if (hintPhaseBefore == 3)
            {
                VibrationRuntime.Current.Play(VibrationLevel.Level3);
                if (phaseBefore == TutorialPhase.FreePlay &&
                    _machine.Phase == TutorialPhase.FinishConfirm)
                    VibrationRuntime.Current.Play(VibrationLevel.Level5);
            }
        }

        private void PlayTapVibration(TutorialPhase phaseBefore, bool accepted)
        {
            if (!accepted || _machine == null || _gestureStartCell.x < 0) return;
            CellStateType after = _machine.GetCellState(
                _gestureStartCell.x,
                _gestureStartCell.y);
            if (after == CellStateType.CAT && _gestureStartState != CellStateType.CAT)
            {
                VibrationRuntime.Current.Play(VibrationLevel.Level3);
                if (phaseBefore == TutorialPhase.FreePlay &&
                    _machine.Phase == TutorialPhase.FinishConfirm)
                    VibrationRuntime.Current.Play(VibrationLevel.Level5);
            }
            else if (after == CellStateType.MARK &&
                     _gestureStartState != CellStateType.MARK)
            {
                VibrationRuntime.Current.Play(VibrationLevel.Level2);
            }
        }

        private void ClearGestureState()
        {
            _gestureStartCell = new Vector2Int(-1, -1);
            _gestureStartState = CellStateType.EMPTY;
        }

        private void RenderPhase(TutorialPhase phase, bool animate)
        {
            if (_machine == null) return;
            _presentedPhase = phase;
            ResetStepVisuals();
            SetBoardInput(IsInteractive(phase));

            switch (phase)
            {
                case TutorialPhase.PlaceFirstCat:
                    SetMessage(_feedbackConfig.Value == GuideFeedbackConfig.ValueCurrent
                        ? LocalizedStep1()
                        : LocalizedStep1Combined(), animate);
                    ShowMaskHints();
                    ShowTapHand(new Vector2Int(0, 2));
                    break;
                case TutorialPhase.ConfirmOnePerColor:
                    SetMessage(TranslateRich(
                        TutorialText.Step2Key,
                        TutorialText.Step2), animate);
                    ShowConfirm(Translate(
                        TutorialText.GotItKey,
                        TutorialText.GotIt));
                    break;
                case TutorialPhase.MarkRowAndColumn:
                    SetMessage(TranslateRich(
                        TutorialText.StepRowColumnKey,
                        TutorialText.StepRowColumn), animate);
                    SetSubMessage(Translate(
                        TutorialText.TapExcludeKey,
                        TutorialText.TapExclude), animate);
                    ShowMaskHints();
                    break;
                case TutorialPhase.PlaceSecondCat:
                    SetMessage(TranslateRich(
                        TutorialText.PlacePinkKey,
                        TutorialText.PlacePink), animate);
                    ShowMaskHints();
                    ShowTapHand(new Vector2Int(3, 1));
                    break;
                case TutorialPhase.MarkNeighbors:
                    SetMessage(_machine.UsesDiagonalCopy
                        ? TranslateRich(
                            TutorialText.NoTouchDiagonalKey,
                            TutorialText.NoTouchDiagonal)
                        : TranslateRich(
                            TutorialText.NoTouchAdjacentKey,
                            TutorialText.NoTouchAdjacent), animate);
                    SetSubMessage(Translate(
                        TutorialText.SwipeExcludeKey,
                        TutorialText.SwipeExclude), animate);
                    ShowMaskHints();
                    ShowSwipeHand(new[]
                    {
                        new Vector2Int(3, 0),
                        new Vector2Int(2, 0),
                        new Vector2Int(2, 1)
                    });
                    break;
                case TutorialPhase.PlaceThirdCat:
                    SetMessage(TranslateRich(
                        TutorialText.PlaceBlueKey,
                        TutorialText.PlaceBlue), animate);
                    ShowMaskHints();
                    ShowTapHand(new Vector2Int(1, 0));
                    break;
                case TutorialPhase.FreePlay:
                    RenderFreePlay(animate);
                    break;
                case TutorialPhase.FinishConfirm:
                    SetMessage(TranslateRich(
                        TutorialText.FinishedKey,
                        TutorialText.Finished), animate);
                    ShowConfirm(Translate(
                        TutorialText.StartGameKey,
                        TutorialText.StartGame));
                    PlayFinishEffectOnce();
                    break;
            }
        }

        private void RenderFreePlay(bool animate)
        {
            SetHintPanel(true, animate);
            if (_machine.HintPhase == 1)
            {
                SetMessage(TranslateRich(
                    TutorialText.HintBlueRowKey,
                    TutorialText.HintBlueRow), animate);
                ShowMaskHints();
            }
            else if (_machine.HintPhase == 2)
            {
                SetMessage(TranslateRich(
                    TutorialText.HintPinkRowKey,
                    TutorialText.HintPinkRow), animate);
                ShowMaskHints();
            }
            else if (_machine.HintPhase == 3)
            {
                SetMessage(TranslateRich(
                    TutorialText.HintLastCatKey,
                    TutorialText.HintLastCat), animate);
                ShowMaskHints();
                ShowTapHand(new Vector2Int(2, 3));
            }
            else
            {
                SetMessage(TranslateRich(
                    TutorialText.LastCatKey,
                    TutorialText.LastCat), animate);
                HideMask();
            }
        }

        private void BeginTransition()
        {
            SetBoardInput(false);
            HideHand();
            HideMask();
            SetActive(subMessagePanel, false);
            SetActive(hintPanel, false);
            SetActive(confirmButton != null ? confirmButton.gameObject : null, false);
        }

        private void ShowMaskHints()
        {
            ClearMaskCells();
            if (_machine == null || maskLayer == null || maskCellLayer == null ||
                cellPrefab == null || !EnsureMaskStateIconLayer())
                return;
            EnsureBoardLayoutReady();
            AddMaskCells(_machine.MaskHintCells, true);
            AddMaskCells(_machine.MirrorCells, false);
            _maskStateIconLayer.SetAsLastSibling();
            FadeMask(true);
        }

        private void EnsureBoardLayoutReady()
        {
            Canvas.ForceUpdateCanvases();
            if (boardView != null &&
                boardView.transform is RectTransform boardRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(boardRect);
            Canvas.ForceUpdateCanvases();
        }

        private void AddMaskCells(IReadOnlyList<Vector2Int> cells, bool highlighted)
        {
            for (int index = 0; index < cells.Count; index++)
            {
                Vector2Int position = cells[index];
                if (_maskCells.ContainsKey(position) ||
                    !boardView.TryGetCellLayout(
                        maskCellLayer,
                        position.x,
                        position.y,
                        out Vector2 center,
                        out Vector2 size))
                    continue;

                GameObject instance = Instantiate(cellPrefab, maskCellLayer);
                instance.name = $"MaskCell_{position.x}_{position.y}";
                CellView view = instance.GetComponent<CellView>();
                if (view == null)
                {
                    Destroy(instance);
                    continue;
                }
                view.PrepareForUse(position.x, position.y);
                // PrepareForUse resets transform.localPosition for pooled
                // board cells. Mask mirrors are free-positioned overlays, so
                // their final layout must be applied after that reset.
                RectTransform rect = instance.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = rect.anchorMax =
                        new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = center;
                    rect.sizeDelta = size;
                    rect.localScale = Vector3.one;
                }
                if (boardView.TryGetCellRegionColor(
                        position.x, position.y, out Color color))
                    view.SetRegionColor(color);
                view.ConfigureBackgroundShape(
                    new Vector4(10f, 10f, 10f, 10f),
                    false);
                view.SetGraphicsRaycastTarget(false);
                if (highlighted) view.PlayHint();
                _maskCells.Add(position, view);
                UpdateMaskStateIcon(
                    position,
                    _machine.GetCellState(position.x, position.y));
            }
        }

        private bool EnsureMaskStateIconLayer()
        {
            if (_maskStateIconLayer == null)
            {
                var layer = new GameObject(
                    "StateIcons",
                    typeof(RectTransform));
                layer.layer = maskCellLayer.gameObject.layer;
                _maskStateIconLayer = layer.GetComponent<RectTransform>();
                _maskStateIconLayer.SetParent(maskCellLayer.parent, false);
                _maskStateIconLayer.anchorMin = Vector2.zero;
                _maskStateIconLayer.anchorMax = Vector2.one;
                _maskStateIconLayer.offsetMin = Vector2.zero;
                _maskStateIconLayer.offsetMax = Vector2.zero;
                _maskStateIconLayer.pivot = new Vector2(0.5f, 0.5f);
            }
            _maskStateIconLayer.gameObject.SetActive(true);
            _maskStateIconLayer.SetAsLastSibling();
            return true;
        }

        private void UpdateMaskStateIcon(
            Vector2Int position,
            CellStateType state)
        {
            if (_maskStateIconLayer == null ||
                !_maskCells.TryGetValue(position, out CellView cell) ||
                cell == null)
                return;

            Image source = cell.PrepareOverlayStateIcon(state);
            if (source == null || source.sprite == null)
            {
                if (_maskStateIcons.TryGetValue(position, out Image hidden) &&
                    hidden != null)
                    hidden.gameObject.SetActive(false);
                return;
            }

            if (!_maskStateIcons.TryGetValue(position, out Image icon) ||
                icon == null)
            {
                var iconObject = new GameObject(
                    $"StateIcon_{position.x}_{position.y}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                iconObject.layer = _maskStateIconLayer.gameObject.layer;
                iconObject.transform.SetParent(_maskStateIconLayer, false);
                icon = iconObject.GetComponent<Image>();
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                _maskStateIcons[position] = icon;
            }

            RectTransform rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            if (state == CellStateType.CAT &&
                boardView.TryGetCellLayout(
                    _maskStateIconLayer,
                    position.x,
                    position.y,
                    out Vector2 cellCenter,
                    out Vector2 cellSize))
            {
                Vector2 sourceSize = source.sprite.rect.size *
                                     CatSpriteAnimationView.SourceNodeScale;
                rect.anchoredPosition = cellCenter + new Vector2(
                    CatSpriteAnimationView.SourceIconOffset.x *
                    cellSize.x / 100f,
                    CatSpriteAnimationView.SourceIconOffset.y *
                    cellSize.y / 100f);
                rect.sizeDelta = new Vector2(
                    sourceSize.x * cellSize.x / 100f,
                    sourceSize.y * cellSize.y / 100f);
            }
            else
            {
                source.rectTransform.GetWorldCorners(_maskIconWorldCorners);
                Vector2 localMin = _maskStateIconLayer.InverseTransformPoint(
                    _maskIconWorldCorners[0]);
                Vector2 localMax = _maskStateIconLayer.InverseTransformPoint(
                    _maskIconWorldCorners[2]);
                rect.anchoredPosition = (localMin + localMax) * 0.5f;
                rect.sizeDelta = new Vector2(
                    Mathf.Abs(localMax.x - localMin.x),
                    Mathf.Abs(localMax.y - localMin.y));
            }
            rect.localScale = Vector3.one;
            icon.sprite = source.sprite;
            icon.material = source.material;
            icon.preserveAspect = source.preserveAspect;
            Color color = source.color;
            color.a = 1f;
            icon.color = color;
            icon.gameObject.SetActive(true);
            icon.transform.SetAsLastSibling();
            _maskStateIconLayer.SetAsLastSibling();
        }

        private void ClearMaskCells()
        {
            foreach (CellView view in _maskCells.Values)
            {
                if (view != null) Destroy(view.gameObject);
            }
            _maskCells.Clear();
            foreach (Image icon in _maskStateIcons.Values)
            {
                if (icon != null) Destroy(icon.gameObject);
            }
            _maskStateIcons.Clear();
            if (_maskStateIconLayer != null)
                _maskStateIconLayer.gameObject.SetActive(false);
        }

        private void FadeMask(bool visible)
        {
            if (maskLayer == null || maskGroup == null) return;
            _maskTween?.Kill(false);
            if (visible)
            {
                maskLayer.SetActive(true);
                maskGroup.alpha = 0f;
                _maskTween = maskGroup.DOFade(1f, SourceMaskFadeSeconds)
                    .SetUpdate(true).SetId(this).SetLink(gameObject);
            }
            else
            {
                if (!maskLayer.activeSelf) return;
                _maskTween = maskGroup.DOFade(0f, SourceMaskFadeSeconds)
                    .SetUpdate(true).SetId(this).SetLink(gameObject)
                    .OnComplete(() =>
                    {
                        if (maskLayer != null) maskLayer.SetActive(false);
                        if (maskGroup != null) maskGroup.alpha = 1f;
                    });
            }
        }

        private void HideMask()
        {
            ClearMaskCells();
            FadeMask(false);
        }

        private void ShowTapHand(Vector2Int cell)
        {
            if (!PositionHand(cell) || handHint == null) return;
            handHint.gameObject.SetActive(true);
            handHint.localScale = Vector3.one;
            _handTween?.Kill(false);
            _handTween = handHint.DOScale(0.92f, 0.3f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetId(this)
                .SetLink(gameObject);
        }

        private void ShowSwipeHand(IReadOnlyList<Vector2Int> cells)
        {
            if (handHint == null || cells == null || cells.Count == 0) return;
            var positions = new List<Vector2>(cells.Count);
            for (int index = 0; index < cells.Count; index++)
            {
                if (TryHandPosition(cells[index], out Vector2 position))
                    positions.Add(position);
            }
            if (positions.Count == 0) return;
            handHint.gameObject.SetActive(true);
            handHint.anchoredPosition = positions[0];
            CanvasGroup group = EnsureCanvasGroup(handHint.gameObject);
            group.alpha = 1f;
            _handTween?.Kill(false);
            Sequence sequence = DOTween.Sequence()
                .SetLoops(-1)
                .SetUpdate(true)
                .SetId(this)
                .SetLink(gameObject);
            sequence.AppendCallback(() =>
            {
                if (handHint != null) handHint.anchoredPosition = positions[0];
                if (group != null) group.alpha = 1f;
            });
            sequence.AppendInterval(0.15f);
            for (int index = 1; index < positions.Count; index++)
            {
                sequence.Append(handHint.DOAnchorPos(positions[index], 0.3f)
                    .SetEase(Ease.Linear));
                sequence.AppendInterval(0.1f);
            }
            sequence.AppendInterval(0.15f);
            sequence.Append(group.DOFade(0f, 0.2f));
            sequence.AppendInterval(0.35f);
            _handTween = sequence;
        }

        private bool PositionHand(Vector2Int cell)
        {
            if (!TryHandPosition(cell, out Vector2 position)) return false;
            handHint.anchoredPosition = position;
            CanvasGroup group = EnsureCanvasGroup(handHint.gameObject);
            group.alpha = 1f;
            if (handImage != null) handImage.gameObject.SetActive(true);
            return true;
        }

        private bool TryHandPosition(Vector2Int cell, out Vector2 position)
        {
            position = Vector2.zero;
            if (boardView == null || layoutSpace == null ||
                !boardView.TryGetCellLayout(
                    layoutSpace,
                    cell.x,
                    cell.y,
                    out Vector2 center,
                    out Vector2 size))
                return false;
            Vector2 topLeft = center + new Vector2(-size.x * 0.5f, size.y * 0.5f);
            const float sourceScale = 919f / 462f;
            Vector2 sourceTipOffset = new Vector2(
                111f - (-460f + 235f * sourceScale),
                -(-316f - (-460f + 19f * sourceScale)));
            position = topLeft + sourceTipOffset;
            return true;
        }

        private void HideHand()
        {
            _handTween?.Kill(false);
            _handTween = null;
            if (handHint != null)
            {
                handHint.localScale = Vector3.one;
                EnsureCanvasGroup(handHint.gameObject).alpha = 1f;
                handHint.gameObject.SetActive(false);
            }
        }

        private void SetMessage(string value, bool animate)
        {
            if (messageText != null) messageText.text = value;
            FitMessagePanel();
            ShowPanel(messagePanel, animate);
        }

        private void SetSubMessage(string value, bool animate)
        {
            if (subMessageText != null) subMessageText.text = value;
            PositionSubMessageBelowBoard();
            ShowPanel(subMessagePanel, animate);
        }

        private void SetHintPanel(bool visible, bool animate)
        {
            if (hintText != null)
                hintText.text = Translate(
                    TutorialText.HintToolKey,
                    TutorialText.HintTool);
            if (visible) ShowPanel(hintPanel, animate);
            else SetActive(hintPanel, false);
        }

        private void ShowConfirm(string value)
        {
            if (confirmButton == null) return;
            if (confirmText != null) confirmText.text = value;
            confirmButton.gameObject.SetActive(true);
            RectTransform rect = confirmButton.transform as RectTransform;
            if (rect == null) return;
            _confirmTween?.Kill(false);
            rect.localScale = Vector3.one * 0.9f;
            _confirmTween = rect.DOScale(1f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetId(this)
                .SetLink(gameObject);
        }

        private void ShowPanel(GameObject panel, bool animate)
        {
            if (panel == null) return;
            panel.SetActive(true);
            CanvasGroup group = EnsureCanvasGroup(panel);
            RectTransform rect = panel.transform as RectTransform;
            DOTween.Kill(panel, false);
            if (!animate || rect == null)
            {
                group.alpha = 1f;
                if (rect != null) rect.localScale = Vector3.one;
                return;
            }
            group.alpha = 0f;
            rect.localScale = Vector3.one * 0.6f;
            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetId(panel)
                .SetLink(panel);
            sequence.Join(group.DOFade(1f, 0.2f));
            sequence.Join(rect.DOScale(1.05f, 0.2f).SetEase(Ease.OutQuad));
            sequence.Append(rect.DOScale(1f, 0.13333334f).SetEase(Ease.InOutQuad));
        }

        private void FitMessagePanel()
        {
            RectTransform panel = messagePanel != null
                ? messagePanel.transform as RectTransform
                : null;
            if (panel == null || messageText == null) return;
            float height = Mathf.Max(190f, messageText.preferredHeight + 80f);
            panel.sizeDelta = new Vector2(930f, height);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0f);
            float bottom = TutorialPuzzle.SourceBoardWidth * 0.5f +
                           SourceMessageToBoardGap;
            if (boardView != null && layoutSpace != null &&
                boardView.TryGetBoardVerticalBounds(
                    layoutSpace, out float boardTop, out _))
                bottom = boardTop + SourceMessageToBoardGap;
            panel.anchoredPosition = new Vector2(0f, bottom);
        }

        private void PositionSubMessageBelowBoard()
        {
            RectTransform panel = subMessagePanel != null
                ? subMessagePanel.transform as RectTransform
                : null;
            if (panel == null) return;
            float top = -TutorialPuzzle.SourceBoardWidth * 0.5f -
                        SourceSubMessageToBoardGap;
            if (boardView != null && layoutSpace != null &&
                boardView.TryGetBoardVerticalBounds(
                    layoutSpace, out _, out float boardBottom))
                top = boardBottom - SourceSubMessageToBoardGap;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, top);
        }

        private void PlayCheckFeedback(float delay)
        {
            if (successCheck == null)
            {
                _machine?.CompleteFeedback();
                return;
            }
            _presentedPhase = TutorialPhase.Feedback;
            successCheck.SetActive(true);
            if (successCheckGroup == null)
                successCheckGroup = EnsureCanvasGroup(successCheck);
            successCheckGroup.alpha = 0f;
            RectTransform rect = successCheck.transform as RectTransform;
            if (rect != null) rect.localScale = Vector3.one * 0.6f;
            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true).SetId(this).SetLink(gameObject);
            sequence.AppendInterval(delay);
            sequence.Append(successCheckGroup.DOFade(1f, 0.2f));
            if (rect != null)
                sequence.Join(rect.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
            sequence.AppendInterval(Mathf.Max(0f, SourceCheckSeconds - 0.35f));
            sequence.AppendCallback(() =>
            {
                if (successCheck != null) successCheck.SetActive(false);
                _machine?.CompleteFeedback();
            });
            _feedbackTween = sequence;
        }

        private void PlayIqFeedback(int before, int after, float delay)
        {
            _presentedPhase = TutorialPhase.Feedback;
            if (iqBar != null) iqBar.SetActive(true);
            float from = Mathf.Clamp01(before / 180f);
            float to = Mathf.Clamp01(after / 180f);
            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true).SetId(this).SetLink(gameObject);
            sequence.AppendInterval(delay);
            sequence.Append(DOVirtual.Float(from, to, SourceIqGrowSeconds, value =>
            {
                SetIqFill(value);
                if (iqText != null)
                    iqText.text = $"IQ={Mathf.RoundToInt(value * 180f)}";
            }).SetEase(Ease.OutCubic));
            sequence.AppendCallback(() => finishEffects?.PlayIqBurst(
                iqBar != null ? iqBar.transform as RectTransform : null,
                to,
                after >= 180));
            sequence.AppendCallback(() => _machine?.CompleteFeedback());
            _feedbackTween = sequence;
        }

        private void InitializeIqBar()
        {
            bool visible = _feedbackConfig != null && _feedbackConfig.IsIqGuide();
            if (iqBar != null) iqBar.SetActive(visible);
            if (!visible) return;
            SetIqFill(60f / 180f);
            if (iqText != null) iqText.text = "IQ=60";
        }

        private void SetIqFill(float fraction)
        {
            if (iqFill == null) return;
            Vector2 size = iqFill.sizeDelta;
            size.x = 592f * Mathf.Clamp01(fraction);
            iqFill.sizeDelta = size;
        }

        private void PlayFinishEffectOnce()
        {
            if (_finishEffectPlayed) return;
            _finishEffectPlayed = true;
            if (_feedbackConfig != null && _feedbackConfig.IsIqGuide())
                finishEffects?.PlayFireworks();
            else
                finishEffects?.PlayDefaultConfetti();
        }

        private void CompleteTutorial()
        {
            if (_routeCommitted) return;
            _routeCommitted = true;
            _completionCommitter?.Commit(GameStateRuntime.Current);
            Owner?.Show(
                UiName.Game,
                new Dictionary<string, object> { { "level_index", 1 } });
            Owner?.Hide(UiName.Tutorial);
        }

        private void ResetStepVisuals()
        {
            _confirmTween?.Kill(false);
            _confirmTween = null;
            HideHand();
            HideMask();
            SetActive(subMessagePanel, false);
            SetActive(hintPanel, false);
            SetActive(confirmButton != null ? confirmButton.gameObject : null, false);
            SetActive(successCheck, false);
            SetActive(selectFrame != null ? selectFrame.gameObject : null, false);
        }

        private void ResetVisuals()
        {
            ResetStepVisuals();
            SetActive(messagePanel, false);
            SetActive(iqBar, false);
            finishEffects?.Clear();
            SetBoardInput(false);
        }

        private void CleanupRuntime(bool clearBoard)
        {
            UnsubscribeMachine();
            UnsubscribeBoard();
            _machine = null;
            ClearGestureState();
            _phaseTween?.Kill(false);
            _feedbackTween?.Kill(false);
            _maskTween?.Kill(false);
            _handTween?.Kill(false);
            _confirmTween?.Kill(false);
            _phaseTween = _feedbackTween = _maskTween = _handTween = _confirmTween = null;
            DOTween.Kill(this, false);
            ClearMaskCells();
            finishEffects?.Clear();
            if (clearBoard) boardView?.ClearBoard();
            ResetVisuals();
        }

        private void SetBoardInput(bool enabled)
        {
            if (boardInputGroup == null) return;
            boardInputGroup.interactable = enabled;
            boardInputGroup.blocksRaycasts = enabled;
        }

        private static bool IsInteractive(TutorialPhase phase)
        {
            return phase == TutorialPhase.PlaceFirstCat ||
                   phase == TutorialPhase.MarkRowAndColumn ||
                   phase == TutorialPhase.PlaceSecondCat ||
                   phase == TutorialPhase.MarkNeighbors ||
                   phase == TutorialPhase.PlaceThirdCat ||
                   phase == TutorialPhase.FreePlay;
        }

#if UNITY_INCLUDE_TESTS
        internal BoardView BoardForTests => boardView;
        internal CanvasGroup BoardInputGroupForTests => boardInputGroup;
        internal int MaskCellCountForTests => _maskCells.Count;
        internal int HintPhaseForTests => _machine?.HintPhase ?? 0;
        internal bool UsesDiagonalCopyForTests => _machine?.UsesDiagonalCopy == true;
        internal int GuideFeedbackValueForTests =>
            _feedbackConfig?.Value ?? GuideFeedbackConfig.ValueCurrent;

        internal bool TapCellForTests(int row, int column, double nowSeconds)
        {
            if (_machine == null) return false;
            _gestureStartCell = new Vector2Int(row, column);
            _gestureStartState = _machine.GetCellState(row, column);
            TutorialPhase phaseBefore = _machine.Phase;
            bool accepted = _machine.Tap(row, column, nowSeconds);
            PlayTapVibration(phaseBefore, accepted);
            ClearGestureState();
            return accepted;
        }
#endif

        private static float TransitionDelayAfter(TutorialPhase phase)
        {
            switch (phase)
            {
                case TutorialPhase.PlaceFirstCat:
                case TutorialPhase.MarkRowAndColumn:
                case TutorialPhase.PlaceSecondCat:
                case TutorialPhase.MarkNeighbors:
                case TutorialPhase.PlaceThirdCat:
                    return 0.4f;
                case TutorialPhase.FreePlay:
                    return 0.5f;
                default:
                    return 0f;
            }
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
        }

        private void RefreshLocalizedPresentation()
        {
            if (!IsShowing || _machine == null) return;
            RenderPhase(_machine.Phase, false);
        }

        private string LocalizedStep1()
        {
            string action = Translate(
                TutorialText.Step1HighlightKey,
                TutorialText.Step1Highlight);
            string template = Translate(
                TutorialText.Step1Key,
                TutorialText.Step1Template);
            return ConvertRich(template.Replace(
                "{breath}",
                $"<color=#d94848>{action}</color>"));
        }

        private string LocalizedStep1Combined()
        {
            return Translate(
                       TutorialText.Step1CombinedKey,
                       TutorialText.Step1Combined) +
                   "\n" +
                   LocalizedStep1();
        }

        private string TranslateRich(string key, string fallback)
        {
            return ConvertRich(Translate(key, fallback));
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) || value == key
                ? fallback
                : value;
        }

        private static string ConvertRich(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value
                .Replace("[/color]", "</color>")
                .Replace("[color=", "<color=")
                .Replace("]", ">");
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private static class TutorialText
        {
            public const string Step1HighlightKey =
                "TUTORIAL_STEP1_HIGHLIGHT";
            public const string Step1Key = "TUTORIAL_STEP1_RICH";
            public const string Step1CombinedKey =
                "TUTORIAL_STEP1_ONE_PER_COLOR";
            public const string Step2Key = "TUTORIAL_STEP2_RICH";
            public const string StepRowColumnKey = "TUTORIAL_STEP5_RICH";
            public const string TapExcludeKey = "TUTORIAL_SUB_EXCLUDE";
            public const string SwipeExcludeKey =
                "TUTORIAL_SUB_SWIPE_EXCLUDE";
            public const string PlacePinkKey =
                "TUTORIAL_STEP4_PINK_RICH";
            public const string NoTouchAdjacentKey =
                "TUTORIAL_STEP3_RICH";
            public const string NoTouchDiagonalKey =
                "TUTORIAL_STEP3_RICH_DIAGONAL";
            public const string PlaceBlueKey =
                "TUTORIAL_STEP4_BLUE_RICH";
            public const string LastCatKey = "TUTORIAL_LAST_ONE_RICH";
            public const string HintToolKey = "TUTORIAL_STEP7_HINT";
            public const string HintBlueRowKey =
                "TUTORIAL_STEP7_ROW_BLUE";
            public const string HintPinkRowKey =
                "TUTORIAL_STEP7_ROW_PINK";
            public const string HintLastCatKey =
                "TUTORIAL_STEP7_PLACE_LAST";
            public const string FinishedKey = "TUTORIAL_STEP6_RICH";
            public const string GotItKey = "TUTORIAL_GOT_IT";
            public const string StartGameKey = "TUTORIAL_START_GAME";
            public const string Step1Highlight = "Double-tap";
            public const string Step1Template =
                "{breath} to place the cat on a cell.";
            public const string Step1Combined =
                "Only one cat per color.";
            public const string Step2 =
                "Well done!\nOnly one cat per <color=#d94848>color</color>.";
            public const string StepRowColumn =
                "Nice!\nCats can't be in the <color=#d94848>same row or column</color>.";
            public const string TapExclude = "Tap empty cells to exclude them.";
            public const string SwipeExclude = "Swipe across these cells to exclude them.";
            public const string PlacePink =
                "Only the last <color=#BC537C>rose cell</color> remains\n<color=#d94848>Double-tap</color> to place a cat";
            public const string NoTouchAdjacent =
                "No cats can be <color=#d94848>adjacent</color> to each other.";
            public const string NoTouchDiagonal =
                "Cats cannot share <color=#d94848>edges or corners</color>.";
            public const string PlaceBlue =
                "Only the last <color=#3497CB>blue cell</color> remains\n<color=#d94848>Double-tap</color> to place a cat";
            public const string LastCat = "Find the <color=#d94848>last cat</color>!";
            public const string HintTool = "Tap here for a hint.";
            public const string HintBlueRow =
                "No more cats in this row!\nTap empty cells to exclude them.";
            public const string HintPinkRow = HintBlueRow;
            public const string HintLastCat =
                "<color=#d94848>Double-tap</color> to place the cat on the last cell!";
            public const string Finished = "Excellent! You've mastered the rules!";
            public const string GotIt = "Got it!";
            public const string StartGame = "Start Game";
        }
    }
}
