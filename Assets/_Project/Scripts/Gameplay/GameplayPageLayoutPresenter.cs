using UnityEngine;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Unity adapter for the source gameplay page VBox. It owns page placement;
    /// BoardView owns only the board's intrinsic grid measurements and scale.
    /// </summary>
    public sealed class GameplayPageLayoutPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform layoutSpace;
        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform catHeartRow;
        [SerializeField] private RectTransform ruleBar;
        [SerializeField] private RectTransform board;
        [SerializeField] private RectTransform bottomTools;
        [SerializeField] private BoardView boardView;

        private void OnEnable()
        {
            if (boardView != null) boardView.LayoutChanged += ApplyLayout;
            ApplyLayout();
        }

        private void Start()
        {
            ApplyLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) ApplyLayout();
        }

        private void OnDisable()
        {
            if (boardView != null) boardView.LayoutChanged -= ApplyLayout;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) ApplyLayout();
        }

        public void ApplyLayout()
        {
            if (layoutSpace == null || layoutSpace.rect.height <= 0f)
                return;

            GetSafeInsets(out float topInset, out float bottomInset);
            ApplyLayout(layoutSpace.rect.height, topInset, bottomInset);
        }

        private void ApplyLayout(
            float viewportHeight,
            float topInset,
            float bottomInset)
        {
            if (layoutSpace == null || viewportHeight <= 0f)
                return;

            float boardHeight = boardView != null
                ? boardView.VisibleBoardPixels
                : SourceGameplayPageLayout.BoardHeight;
            SourceGameplayPageLayoutResult result = SourceGameplayPageLayout.Calculate(
                viewportHeight,
                topInset,
                bottomInset,
                boardHeight,
                boardView != null && boardView.UsesEnlargedBoard);
            if (header != null) SetCenteredPosition(header, result.HeaderCenterY);
            if (catHeartRow != null)
                SetCenteredPosition(catHeartRow, result.CatHeartCenterY);
            if (ruleBar != null) SetCenteredPosition(ruleBar, result.RuleCenterY);
            if (board != null) SetCenteredPosition(board, result.BoardCenterY);
            if (bottomTools != null)
                SetCenteredPosition(bottomTools, result.BottomToolsCenterY);
        }

        private static void SetCenteredPosition(RectTransform rect, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private void GetSafeInsets(out float topInset, out float bottomInset)
        {
            topInset = 0f;
            bottomInset = 0f;
            if (!Application.isMobilePlatform || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect safeArea = Screen.safeArea;
            float canvasUnitsPerPixel = layoutSpace.rect.width / Screen.width;
            topInset = Mathf.Max(0f, Screen.height - safeArea.yMax) * canvasUnitsPerPixel;
            bottomInset = Mathf.Max(0f, safeArea.yMin) * canvasUnitsPerPixel;
        }

#if UNITY_INCLUDE_TESTS
        internal void ApplyLayoutForTests(
            float viewportHeight,
            float topInset,
            float bottomInset)
        {
            ApplyLayout(viewportHeight, topInset, bottomInset);
        }

        internal RectTransform HeaderForTests => header;
        internal RectTransform CatHeartRowForTests => catHeartRow;
        internal RectTransform RuleBarForTests => ruleBar;
        internal RectTransform BoardForTests => board;
        internal RectTransform BottomToolsForTests => bottomTools;
#endif
    }
}
