using UnityEngine;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityPageLayoutPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform layoutSpace;
        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform podium;
        [SerializeField] private RectTransform list;
        [SerializeField] private RectTransform cta;

        private void OnEnable() => ApplyLayout();

        private void Start() => ApplyLayout();

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) ApplyLayout();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) ApplyLayout();
        }

        public void ApplyLayout()
        {
            if (layoutSpace == null || layoutSpace.rect.height <= 0f) return;
            GetSafeInsets(out float topInset, out float bottomInset);
            Apply(SourceRankActivityLayout.CalculatePage(
                layoutSpace.rect.height,
                topInset,
                bottomInset));
        }

        internal void ApplyLayoutForTests(
            float viewportHeight,
            float topInset,
            float bottomInset)
        {
            Apply(SourceRankActivityLayout.CalculatePage(
                viewportHeight,
                topInset,
                bottomInset));
        }

        private void Apply(SourceRankActivityPageLayoutResult value)
        {
            SetTop(
                header,
                value.HeaderTop,
                SourceRankActivityLayout.PageHeaderHeight,
                SourceRankActivityLayout.PageWidth);
            SetTop(
                podium,
                value.PodiumTop,
                SourceRankActivityLayout.PagePodiumHeight,
                SourceRankActivityLayout.PageWidth);
            SetVerticalStretch(list, value.ListTop, value.ListBottomInset,
                SourceRankActivityLayout.PageListWidth);
            if (cta != null)
            {
                cta.anchorMin = cta.anchorMax = new Vector2(0.5f, 0f);
                cta.pivot = new Vector2(0.5f, 0f);
                cta.anchoredPosition = new Vector2(0f, value.CtaBottomInset);
                cta.sizeDelta = new Vector2(
                    SourceRankActivityLayout.PageCtaWidth,
                    SourceRankActivityLayout.PageCtaHeight);
            }
        }

        private static void SetTop(
            RectTransform rect,
            float top,
            float height,
            float width)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            // Header is authored as a horizontal stretch RectTransform, whose
            // sizeDelta.x is zero. Once its anchors are collapsed to center,
            // retaining that zero width shifts all source-positioned children
            // to the right of screen center. Restore the 1080-wide source
            // coordinate space explicitly.
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetVerticalStretch(
            RectTransform rect,
            float top,
            float bottom,
            float width)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, (bottom - top) * 0.5f);
            rect.sizeDelta = new Vector2(width, -(top + bottom));
        }

        private void GetSafeInsets(out float topInset, out float bottomInset)
        {
            topInset = 0f;
            bottomInset = 0f;
            if (!Application.isMobilePlatform || Screen.width <= 0 ||
                Screen.height <= 0 || layoutSpace.rect.width <= 0f)
                return;
            Rect safeArea = Screen.safeArea;
            float unitsPerPixel = layoutSpace.rect.width / Screen.width;
            topInset = Mathf.Max(0f, Screen.height - safeArea.yMax) * unitsPerPixel;
            bottomInset = Mathf.Max(0f, safeArea.yMin) * unitsPerPixel;
        }
    }
}
