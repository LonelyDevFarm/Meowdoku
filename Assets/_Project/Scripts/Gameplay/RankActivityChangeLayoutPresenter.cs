using UnityEngine;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityChangeLayoutPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform layoutSpace;
        [SerializeField] private RectTransform encourage;
        [SerializeField] private RectTransform title;
        [SerializeField] private RectTransform countdown;
        [SerializeField] private RectTransform list;
        [SerializeField] private RectTransform tap;

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
            Apply(SourceRankActivityLayout.CalculateChange(
                layoutSpace.rect.height,
                topInset,
                bottomInset));
        }

        internal void ApplyLayoutForTests(
            float viewportHeight,
            float topInset,
            float bottomInset)
        {
            Apply(SourceRankActivityLayout.CalculateChange(
                viewportHeight,
                topInset,
                bottomInset));
        }

        private void Apply(SourceRankActivityChangeLayoutResult value)
        {
            SetTop(encourage, value.EncourageTop);
            SetTop(title, value.TitleTop);
            SetTop(countdown, value.CountdownTop);
            SetVerticalStretch(list, value.ListTop, value.ListBottomInset);
            if (tap != null)
            {
                tap.anchorMin = tap.anchorMax = new Vector2(0.5f, 0f);
                tap.pivot = new Vector2(0.5f, 0f);
                tap.anchoredPosition = new Vector2(0f, value.TapBottomInset);
            }
        }

        private static void SetTop(RectTransform rect, float top)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
        }

        private static void SetVerticalStretch(
            RectTransform rect,
            float top,
            float bottom)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, (bottom - top) * 0.5f);
            rect.sizeDelta = new Vector2(1008f, -(top + bottom));
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
