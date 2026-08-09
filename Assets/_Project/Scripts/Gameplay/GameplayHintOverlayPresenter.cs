using System;
using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Unity presenter for hint_overlay.tscn and BaseGamePage._build_hint_highlights.
    /// Highlight cells are temporary source-style Cell instances, not board mutations.
    /// </summary>
    public sealed class GameplayHintOverlayPresenter : MonoBehaviour
    {
        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private BoardView boardView;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private RectTransform layoutSpace;
        [SerializeField] private RectTransform highlightLayer;
        [SerializeField] private RectTransform banner;
        [SerializeField] private RectTransform buttonGroup;
        [SerializeField] private Image dimImage;
        [SerializeField] private Text strategyLabel;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Button detailButton;

        private readonly List<CellView> _temporaryCells = new List<CellView>(24);
        private GameplayHintPresentationData _presentation;
        private Tween _dimTween;

        private void Awake()
        {
            BindButtons();
            if (visualRoot != null) visualRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (gameplayManager == null) return;
            gameplayManager.HintPresentationRequested += Show;
            gameplayManager.HintPresentationClosed += Close;
        }

        private void OnDisable()
        {
            if (gameplayManager != null)
            {
                gameplayManager.HintPresentationRequested -= Show;
                gameplayManager.HintPresentationClosed -= Close;
            }
            _dimTween?.Kill(false);
            _dimTween = null;
            ClearTemporaryCells();
        }

        public void Show(GameplayHintPresentationData presentation)
        {
            if (presentation == null) return;
            _presentation = presentation;
            if (visualRoot != null && !visualRoot.activeSelf) visualRoot.SetActive(true);
            BindButtons();
            if (strategyLabel != null)
            {
                strategyLabel.text = presentation.StrategyLabel;
                strategyLabel.gameObject.SetActive(false); // source: debug-only
            }
            if (descriptionLabel != null)
                descriptionLabel.text = ResolveEnglishSourceText(presentation.DescriptionKey);
            SetButtonVisible(applyButton, true);
            SetButtonVisible(detailButton, presentation.HasChainDetail);
            SetButtonVisible(dismissButton, false);
            AlignToBoard();
            BuildTemporaryCells(presentation);
            PlayDimIn();
        }

        public void Close()
        {
            _presentation = null;
            _dimTween?.Kill(false);
            _dimTween = null;
            ClearTemporaryCells();
            if (visualRoot != null) visualRoot.SetActive(false);
        }

        private void BindButtons()
        {
            if (applyButton != null)
            {
                applyButton.onClick.RemoveListener(Apply);
                applyButton.onClick.AddListener(Apply);
            }
            if (dismissButton != null)
            {
                dismissButton.onClick.RemoveListener(Dismiss);
                dismissButton.onClick.AddListener(Dismiss);
            }
            if (detailButton != null)
            {
                detailButton.onClick.RemoveListener(ShowDetail);
                detailButton.onClick.AddListener(ShowDetail);
            }
        }

        private void Apply()
        {
            gameplayManager?.ApplyRequestedHint();
        }

        private void Dismiss()
        {
            gameplayManager?.CancelRequestedHint();
        }

        private void ShowDetail()
        {
            if (_presentation == null || !_presentation.HasChainDetail) return;
            SetButtonVisible(detailButton, false);
            SetButtonVisible(dismissButton, true);
        }

        private void BuildTemporaryCells(GameplayHintPresentationData presentation)
        {
            ClearTemporaryCells();
            if (boardView == null || cellPrefab == null || highlightLayer == null) return;
            var cells = new Dictionary<Vector2Int, CellView>();
            IReadOnlyList<Vector2Int> highlights = presentation.HighlightCells;
            for (int index = 0; index < highlights.Count; index++)
                EnsureTemporaryCell(highlights[index], cells)?.PlayHint();
            IReadOnlyList<HintPreviewCell> previews = presentation.MarkPreviews;
            for (int index = 0; index < previews.Count; index++)
            {
                HintPreviewCell preview = previews[index];
                EnsureTemporaryCell(preview.Position, cells)?.PlayMarkPreview(
                    preview.DelaySeconds);
            }
        }

        private CellView EnsureTemporaryCell(
            Vector2Int position,
            Dictionary<Vector2Int, CellView> cells)
        {
            if (cells.TryGetValue(position, out CellView existing)) return existing;
            if (!boardView.TryGetCellLayout(
                    highlightLayer, position.x, position.y,
                    out Vector2 center, out Vector2 size))
                return null;
            GameObject instance = Instantiate(cellPrefab, highlightLayer);
            instance.name = $"HintCell_{position.x}_{position.y}";
            RectTransform rect = instance.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = center;
                rect.sizeDelta = size;
                rect.localScale = Vector3.one;
            }
            CellView view = instance.GetComponent<CellView>();
            if (view == null)
            {
                Destroy(instance);
                return null;
            }
            view.Row = position.x;
            view.Col = position.y;
            if (boardView.TryGetCellRegionColor(position.x, position.y, out Color color))
                view.SetRegionColor(color);
            CellStateType state = boardView.GetCellState(position.x, position.y);
            if (state == CellStateType.ERROR) state = CellStateType.MARK;
            view.ChangeState(state, false);
            view.SetGraphicsRaycastTarget(false);
            cells.Add(position, view);
            _temporaryCells.Add(view);
            return view;
        }

        private void AlignToBoard()
        {
            if (boardView == null || layoutSpace == null ||
                !boardView.TryGetBoardVerticalBounds(layoutSpace, out float top, out float bottom))
                return;
            if (banner != null)
                banner.anchoredPosition = new Vector2(
                    banner.anchoredPosition.x,
                    top + 15f + banner.rect.height * banner.pivot.y);
            if (buttonGroup != null)
                buttonGroup.anchoredPosition = new Vector2(
                    buttonGroup.anchoredPosition.x,
                    bottom - 15f - buttonGroup.rect.height * (1f - buttonGroup.pivot.y));
        }

        private void PlayDimIn()
        {
            if (dimImage == null) return;
            _dimTween?.Kill(false);
            SetImageAlpha(dimImage, 0f);
            _dimTween = DOVirtual.Float(0f, 0.749f, 0.3f,
                    value => SetImageAlpha(dimImage, value))
                .SetUpdate(true)
                .SetLink(visualRoot != null ? visualRoot : gameObject);
        }

        private void ClearTemporaryCells()
        {
            for (int index = _temporaryCells.Count - 1; index >= 0; index--)
            {
                CellView view = _temporaryCells[index];
                if (view != null) Destroy(view.gameObject);
            }
            _temporaryCells.Clear();
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null) button.gameObject.SetActive(visible);
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        internal static string ResolveEnglishSourceText(string key)
        {
            switch (key)
            {
                case "HINT_WRONG_MARK":
                    return "You've incorrectly marked this cell! Tap to remove the X mark.";
                case "HINT_R1_MARK":
                    return "This cat's row, column and neighbors can't have other cats — exclude them";
                case "HINT_REGION_CONSTRAINT":
                    return "Region Constraint: Exclude related cells";
                case "HINT_SET_LOCKING":
                    return "Set Locking: Exclude related cells";
                case "HINT_LARGE_SET_LOCKING":
                    return "Large Set Locking: Exclude related cells";
                case "HINT_CONTRADICTION":
                    return "Placing here causes contradiction - exclude";
                case "HINT_INTERSECTION":
                    return "Row and column share a region - intersection must have cat";
                case "HINT_ONLY_ONE_CELL":
                    return "Only one cell left for a cat";
                default:
                    return key ?? string.Empty;
            }
        }
    }
}
