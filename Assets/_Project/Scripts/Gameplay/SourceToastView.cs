using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// UGUI adapter for scripts/module/ui/common/toast.gd. A new message
    /// replaces the current one, matching the source's single static toast.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SourceToastView : MonoBehaviour
    {
        public const float MaximumWidth = 870f;
        public const float SourceTopY = 750f;
        public const float FloatDistance = 50f;
        public const float FadeInSeconds = 0.15f;
        public const float HoldSeconds = 1.2f;
        public const float FadeOutSeconds = 0.2f;
        public const float MoveSeconds = 1.55f;

        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Text label;

        private Sequence _sequence;
        private Tween _move;

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        public void Show(string message)
        {
            if (panel == null || panelGroup == null || label == null) return;
            KillTweens();
            gameObject.SetActive(true);
            label.text = message ?? string.Empty;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

            float width = Mathf.Min(
                MaximumWidth,
                Mathf.Max(120f, label.preferredWidth + 120f));
            float height = Mathf.Max(80f, label.preferredHeight + 40f);
            panel.sizeDelta = new Vector2(width, height);
            panel.anchoredPosition = new Vector2(0f, -SourceTopY);
            panelGroup.alpha = 0f;

            _sequence = DOTween.Sequence().SetLink(gameObject);
            _sequence.Append(panelGroup.DOFade(1f, FadeInSeconds)
                .SetEase(Ease.Linear));
            _sequence.AppendInterval(HoldSeconds);
            _sequence.Append(panelGroup.DOFade(0f, FadeOutSeconds)
                .SetEase(Ease.InQuad));
            _sequence.OnComplete(() =>
            {
                _sequence = null;
                gameObject.SetActive(false);
            });
            _move = panel.DOAnchorPosY(
                    -SourceTopY + FloatDistance,
                    MoveSeconds)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        private void KillTweens()
        {
            _sequence?.Kill(false);
            _move?.Kill(false);
            _sequence = null;
            _move = null;
        }
    }
}
