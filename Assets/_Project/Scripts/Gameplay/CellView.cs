using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Meowdoku.Core;

namespace Meowdoku.Gameplay
{
    // Script gắn vào Prefab của một Ô (Cell) trên bàn cờ.
    // Dùng để điều khiển hình ảnh hiển thị và lắng nghe click.
    public class CellView : MonoBehaviour
    {
        [HideInInspector] public int Row;
        [HideInInspector] public int Col;
        [Header("UI References")]
        public Image bgImage;           // Ảnh nền của ô
        public Image catIcon;           // Ảnh con mèo (Đáp án đúng)
        public Image crossIcon;         // Ảnh dấu X (Bỏ qua)
        public Image errorIcon;         // Ảnh lỗi đỏ
        public Image patternImage;      // Lớp họa tiết chìm (nếu có)
        [SerializeField] private Image hintLight;
        [SerializeField] private Image promptFrame;
        [SerializeField] private Image promptCross;
        [SerializeField] private Shader roundedBackgroundShader;
        [SerializeField] private CanvasGroup introCanvasGroup;
        [SerializeField] private CatSpriteAnimationView catSpriteAnimation;
        
        [Header("Effects")]
        public ParticleSystem appearVFX; // Hiệu ứng nổ hạt khi mèo xuất hiện

        private CellStateType _currentState = CellStateType.EMPTY;
        private Color _regionColor = Color.white;
        private Sprite _patternSprite;
        private bool _patternOn;
        private bool _patternKeepOnFilled;
        private Sequence _visualSequence;
        private Sequence _hintSequence;
        private Sequence _previewSequence;
        private Vector4 _configuredCornerRadii;
        private bool _configuredHardEdge;

        private const float HintAlphaMin = 50f / 255f;
        private const float HintHalfCycle = 0.65f;
        private const float HintFadeOut = 0.12f;

        // Khởi tạo ban đầu
        private void Awake()
        {
            if (introCanvasGroup == null)
                introCanvasGroup = GetComponent<CanvasGroup>();
            // Godot's BoardView sets every CellView to MOUSE_FILTER_IGNORE.
            // The board is the sole input surface; cell graphics are visual only.
            SetGraphicsRaycastTarget(false);
            ResetToEmpty();
        }

        public void SetGraphicsRaycastTarget(bool value)
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = value;
        }

        // Đổi màu nền của ô theo Region (khu vực)
        public void SetRegionColor(Color color)
        {
            _regionColor = color;
            if (bgImage != null)
            {
                bgImage.color = color;
            }
        }

        public Color RegionColor => _regionColor;

        public void ConfigurePattern(Sprite sprite, Color color)
        {
            _patternSprite = sprite;
            if (patternImage != null)
            {
                patternImage.sprite = sprite;
                patternImage.color = color;
                patternImage.preserveAspect = true;
                patternImage.raycastTarget = false;
            }
            RefreshPatternVisibility();
        }

        public void SetPatternMode(bool on, bool keepOnFilled)
        {
            _patternOn = on;
            _patternKeepOnFilled = keepOnFilled;
            RefreshPatternVisibility();
        }

        public void ConfigureBackgroundShape(Vector4 cornerRadii, bool hardEdge)
        {
            _configuredCornerRadii = cornerRadii;
            _configuredHardEdge = hardEdge;
            if (bgImage == null || roundedBackgroundShader == null) return;
            bgImage.material = RoundedRectMaterialCache.Get(
                roundedBackgroundShader,
                new Vector2(95f, 95f),
                cornerRadii,
                hardEdge);
        }

        public void PrepareForUse(int row, int column)
        {
            RestoreGridIntroVisual();
            Row = row;
            Col = column;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
            ResetToEmpty();
            SetGraphicsRaycastTarget(false);
        }

        public void ReleaseToPool()
        {
            RestoreGridIntroVisual();
            ResetToEmpty();
            if (appearVFX != null)
                appearVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            gameObject.SetActive(false);
        }

        // Chuyển đổi trạng thái của ô (Hiển thị mèo, dấu chéo, v.v.)
        public void ChangeState(CellStateType newState, bool playAnim = true)
        {
            if (_currentState == newState) return;
            // cell_view.gd treats LOCKED_MARK as immutable. Pool/reset code
            // writes the backing state directly and therefore remains able to
            // recycle a locked cell.
            if (_currentState == CellStateType.LOCKED_MARK) return;

            _currentState = newState;
            UpdateVisuals(playAnim);
        }

        /// <summary>
        /// Source-backed adapter for cell_view.gd demo_cat(). It intentionally
        /// bypasses idle/cry behavior and is only used by How-to-play boards.
        /// </summary>
        public void PlayDemoCat(bool animate)
        {
            ResetToEmpty();
            ChangeState(CellStateType.CAT, animate);
        }

        /// <summary>
        /// Unity rendering adapter for CrossOutAppear_2 (0.35 seconds).
        /// </summary>
        public void PlayDemoMark(bool instant = false)
        {
            ResetToEmpty();
            ChangeState(CellStateType.MARK, false);
            if (instant || crossIcon == null) return;

            crossIcon.rectTransform.localScale = Vector3.one * 0.4f;
            _visualSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);
            _visualSequence.AppendInterval(0.018f);
            _visualSequence.Append(
                crossIcon.rectTransform.DOScale(1f, 0.198f)
                    .SetEase(Ease.OutBack));
            _visualSequence.AppendInterval(0.134f);
            _visualSequence.OnComplete(() => _visualSequence = null);
        }

        /// <summary>
        /// Unity rendering adapter for ErrorAppear_2 (1.1 seconds).
        /// </summary>
        public void PlayDemoError(bool instant = false)
        {
            ResetToEmpty();
            ChangeState(CellStateType.ERROR, !instant);
        }

        /// <summary>
        /// Unity rendering adapter for DemoDisappear (0.1 seconds).
        /// </summary>
        public void PlayDemoDisappear()
        {
            _visualSequence?.Kill(false);
            _visualSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);
            JoinFadeIfVisible(_visualSequence, catIcon, 0f, 0.1f);
            JoinFadeIfVisible(_visualSequence, crossIcon, 0f, 0.1f);
            JoinFadeIfVisible(_visualSequence, errorIcon, 0f, 0.1f);
            _visualSequence.OnComplete(() =>
            {
                _visualSequence = null;
                ResetToEmpty();
            });
        }

        public void ClearDemo()
        {
            ResetToEmpty();
        }

        // Lấy trạng thái hiện tại
        public CellStateType GetState()
        {
            return _currentState;
        }

        // Xóa sạch trạng thái, đưa về ô trống
        public void ResetToEmpty()
        {
            catSpriteAnimation?.Stop();
            _visualSequence?.Kill(false);
            _visualSequence = null;
            HideHint(true);
            if (appearVFX != null)
                appearVFX.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            _currentState = CellStateType.EMPTY;
            UpdateVisuals(false);
            ResetImageTransform(catIcon);
            ResetImageTransform(crossIcon);
            ResetImageTransform(errorIcon);
            ResetImageTransform(promptCross);
            if (bgImage != null)
            {
                bgImage.color = _regionColor;
                bgImage.rectTransform.localScale = Vector3.one;
            }
        }

        // Cập nhật hiển thị dựa trên trạng thái hiện tại
        private void UpdateVisuals(bool playAnim)
        {
            catSpriteAnimation?.Stop();
            _visualSequence?.Kill(false);
            _visualSequence = null;
            // Tắt hết các icon đi trước
            if (catIcon != null) catIcon.gameObject.SetActive(false);
            if (crossIcon != null) crossIcon.gameObject.SetActive(false);
            if (errorIcon != null) errorIcon.gameObject.SetActive(false);

            switch (_currentState)
            {
                case CellStateType.CAT:
                    if (catIcon != null) catIcon.gameObject.SetActive(true);
                    if (playAnim && appearVFX != null) appearVFX.Play();
                    if (playAnim)
                    {
                        catSpriteAnimation?.PlayAppear();
                        PlaySourceCatAppear();
                    }
                    else
                    {
                        catSpriteAnimation?.ShowIdleFinal();
                    }
                    break;

                case CellStateType.MARK:
                case CellStateType.LOCKED_MARK:
                    ShowIcon(crossIcon, Color.white, Vector3.one);
                    break;

                case CellStateType.ERROR:
                    if (playAnim) PlaySourceErrorAppear();
                    else ShowIcon(errorIcon,
                        new Color(0.99215686f, 0.41568628f, 0.18039216f, 1f),
                        Vector3.one);
                    break;
            }
            RefreshPatternVisibility();
        }

        private void PlaySourceCatAppear()
        {
            if (catIcon == null) return;
            SetImageAlpha(catIcon, 0f);

            _visualSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _visualSequence.Insert(0.033333335f,
                catIcon.DOFade(1f, 0.033333335f).SetEase(Ease.Linear));
            if (bgImage != null)
            {
                RectTransform background = bgImage.rectTransform;
                background.localScale = Vector3.one;
                _visualSequence.Insert(0f,
                    background.DOScale(0.95f, 0.07216451f).SetEase(Ease.Linear));
                _visualSequence.Insert(0.07216451f,
                    background.DOScale(1.05f, 0.13045123f).SetEase(Ease.Linear));
                _visualSequence.Insert(0.8673619f,
                    background.DOScale(1f, 0.0826381f).SetEase(Ease.Linear));
            }
            _visualSequence.AppendInterval(
                Mathf.Max(0f, 1.3333334f - _visualSequence.Duration()));
            _visualSequence.OnComplete(() => _visualSequence = null);
        }

        private void RefreshPatternVisibility()
        {
            if (patternImage == null) return;
            bool show = _patternOn && _patternSprite != null &&
                        (_currentState == CellStateType.EMPTY ||
                         _patternKeepOnFilled);
            patternImage.gameObject.SetActive(show);
        }

#if UNITY_INCLUDE_TESTS
        internal bool IsPatternVisibleForTests =>
            patternImage != null && patternImage.gameObject.activeSelf;
        internal bool IsCatVisibleForTests =>
            catIcon != null && catIcon.gameObject.activeSelf;
        internal bool IsCrossVisibleForTests =>
            crossIcon != null && crossIcon.gameObject.activeSelf;
        internal bool IsErrorVisibleForTests =>
            errorIcon != null && errorIcon.gameObject.activeSelf;
        internal bool IsHintVisibleForTests =>
            hintLight != null && hintLight.gameObject.activeSelf;
        internal bool IsPromptVisibleForTests =>
            (promptFrame != null && promptFrame.gameObject.activeSelf) ||
            (promptCross != null && promptCross.gameObject.activeSelf);
        internal Vector4 CornerRadiiForTests => _configuredCornerRadii;
        internal bool HardEdgeForTests => _configuredHardEdge;
#endif

        public void PlayHint()
        {
            _hintSequence?.Kill(false);
            SetHintImage(hintLight, true, HintAlphaMin);
            SetHintImage(promptFrame, true, HintAlphaMin);
            _hintSequence = DOTween.Sequence().SetUpdate(true).SetLoops(-1)
                .SetLink(gameObject);
            _hintSequence.Append(DOVirtual.Float(
                HintAlphaMin, 1f, HintHalfCycle, SetHintAlpha));
            _hintSequence.Append(DOVirtual.Float(
                1f, HintAlphaMin, HintHalfCycle, SetHintAlpha));
        }

        public void PlayMarkPreview(float delaySeconds)
        {
            _previewSequence?.Kill(false);
            if (promptCross == null) return;
            promptCross.gameObject.SetActive(false);
            promptCross.color = Color.white;
            promptCross.rectTransform.localScale = Vector3.one * 0.4f;
            _previewSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _previewSequence.AppendInterval(Mathf.Max(0f, 0.317f + delaySeconds));
            _previewSequence.AppendCallback(() => promptCross.gameObject.SetActive(true));
            _previewSequence.Append(
                promptCross.rectTransform.DOScale(1f, 0.18f).SetEase(Ease.OutBack));
        }

        public void HideHint(bool immediate = false)
        {
            _hintSequence?.Kill(false);
            _hintSequence = null;
            _previewSequence?.Kill(false);
            _previewSequence = null;
            if (promptFrame != null) promptFrame.gameObject.SetActive(false);
            if (promptCross != null) promptCross.gameObject.SetActive(false);
            if (hintLight == null) return;
            if (immediate)
            {
                hintLight.gameObject.SetActive(false);
                return;
            }
            float from = hintLight.color.a;
            _hintSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _hintSequence.Append(DOVirtual.Float(from, 0f, HintFadeOut, value =>
            {
                Color color = hintLight.color;
                color.a = value;
                hintLight.color = color;
            }));
            _hintSequence.OnComplete(() => hintLight.gameObject.SetActive(false));
        }

        private void PlaySourceErrorAppear()
        {
            if (crossIcon == null || errorIcon == null)
            {
                ShowIcon(errorIcon,
                    new Color(0.99215686f, 0.41568628f, 0.18039216f, 1f),
                    Vector3.one);
                return;
            }
            ShowIcon(crossIcon, Color.white, Vector3.one * 0.4f);
            errorIcon.color = new Color(0.99215686f, 0.41568628f, 0.18039216f, 1f);
            errorIcon.rectTransform.localScale = Vector3.one * 0.4f;
            errorIcon.gameObject.SetActive(false);
            _visualSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _visualSequence.Insert(0.066f,
                crossIcon.rectTransform.DOScale(1f, 0.102f).SetEase(Ease.OutQuad));
            _visualSequence.Insert(0.351f, DOVirtual.Float(1f, 0f, 0.142f,
                value => SetImageAlpha(crossIcon, value)));
            _visualSequence.InsertCallback(0.599f, () =>
            {
                crossIcon.gameObject.SetActive(false);
                errorIcon.gameObject.SetActive(true);
            });
            _visualSequence.Insert(0.599f,
                errorIcon.rectTransform.DOScale(1.08f, 0.084f).SetEase(Ease.OutQuad));
            _visualSequence.Insert(0.683f,
                errorIcon.rectTransform.DOScale(1f, 0.205f).SetEase(Ease.InOutQuad));
        }

        private void SetHintAlpha(float alpha)
        {
            SetImageAlpha(hintLight, alpha);
            SetImageAlpha(promptFrame, alpha);
        }

        private static void SetHintImage(Image image, bool active, float alpha)
        {
            if (image == null) return;
            image.gameObject.SetActive(active);
            SetImageAlpha(image, alpha);
        }

        private static void ShowIcon(Image image, Color color, Vector3 scale)
        {
            if (image == null) return;
            image.gameObject.SetActive(true);
            image.color = color;
            image.rectTransform.localScale = scale;
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null) return;
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private static void ResetImageTransform(Image image)
        {
            if (image == null) return;
            image.rectTransform.localScale = Vector3.one;
            Color color = image.color;
            color.a = 1f;
            image.color = color;
        }

        private static void JoinFadeIfVisible(
            Sequence sequence,
            Image image,
            float alpha,
            float duration)
        {
            if (sequence == null || image == null || !image.gameObject.activeSelf)
                return;
            sequence.Join(image.DOFade(alpha, duration).SetEase(Ease.Linear));
        }

        internal void PrepareGridIntroVisual()
        {
            transform.DOKill(false);
            if (introCanvasGroup != null)
            {
                introCanvasGroup.DOKill(false);
                introCanvasGroup.alpha = 0f;
            }
            transform.localScale = Vector3.zero;
        }

        internal void InsertGridIntroTweens(
            Sequence sequence,
            float delay,
            float fadeDuration,
            float scaleDuration,
            AnimationCurve scaleCurve)
        {
            if (sequence == null) return;
            if (introCanvasGroup != null)
                sequence.Insert(delay, introCanvasGroup.DOFade(1f, fadeDuration)
                    .SetEase(Ease.OutSine));
            Tween scaleTween = transform.DOScale(1f, scaleDuration);
            scaleTween.SetEase(scaleCurve != null ? scaleCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f));
            sequence.Insert(delay, scaleTween);
        }

        internal void RestoreGridIntroVisual()
        {
            transform.DOKill(false);
            if (introCanvasGroup != null)
            {
                introCanvasGroup.DOKill(false);
                introCanvasGroup.alpha = 1f;
            }
            transform.localScale = Vector3.one;
        }

#if UNITY_INCLUDE_TESTS
        internal float IntroAlphaForTests => introCanvasGroup != null
            ? introCanvasGroup.alpha
            : 1f;
        internal float IntroScaleForTests => transform.localScale.x;
#endif

        private void OnDisable()
        {
            catSpriteAnimation?.Stop();
            RestoreGridIntroVisual();
            _visualSequence?.Kill(false);
            _hintSequence?.Kill(false);
            _previewSequence?.Kill(false);
            _visualSequence = null;
            _hintSequence = null;
            _previewSequence = null;
        }

    }
}
