using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Platform;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Port of rate_us_page.gd and its V2 presentation. Star selection is
    /// deterministic and the close result is consumed by ProductServiceRuntime.
    /// </summary>
    [DisallowMultipleComponent]
    public class RateUsPagePresenter : UIFrameWindow, IRateUsWindow
    {
        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private Text titleText;
        [SerializeField] private Text questionText;
        [SerializeField] private Image[] stars;
        [SerializeField] private Button[] starButtons;
        [SerializeField] private Sprite litStar;
        [SerializeField] private Sprite dimStar;
        [SerializeField] private Button rateButton;
        [SerializeField] private Button rateCloseButton;
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private bool restyled;
        [SerializeField] private float restyledAutoSelectDelaySeconds = 0.3f;

        private bool _closing;
        private bool _closeAlreadyPlayed;
        private int _selectedStars = 5;
        private UnityEngine.Events.UnityAction[] _starActions;

        public event Action<RateUsResult> Closed;

        public override string GetTrackingDialogName() => TrackerCatalog.Dialog.Rate;

        public override IReadOnlyDictionary<string, object>
            GetTrackingDialogExtra()
        {
            return new Dictionary<string, object>
            {
                ["dlg_star_ui"] = restyled ? 1 : 0
            };
        }

        protected override bool UsesDefaultCloseButton => false;

        protected override void OnCreate()
        {
            if (rateButton != null) rateButton.onClick.AddListener(Submit);
            if (rateCloseButton != null) rateCloseButton.onClick.AddListener(Close);
            Button[] buttons = starButtons ?? Array.Empty<Button>();
            _starActions = new UnityEngine.Events.UnityAction[buttons.Length];
            for (int index = 0; index < buttons.Length; index++)
            {
                int captured = index + 1;
                if (buttons[index] == null) continue;
                UnityEngine.Events.UnityAction action =
                    () => SelectStarsAt(captured);
                _starActions[index] = action;
                buttons[index].onClick.AddListener(action);
            }

            RateUsStarPointerView[] pointerViews =
                GetComponentsInChildren<RateUsStarPointerView>(true);
            foreach (RateUsStarPointerView pointerView in pointerViews)
                pointerView.Bind(SelectStarsAt);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _closing = false;
            _closeAlreadyPlayed = false;
            _selectedStars = restyled ? 0 : 5;
            SetText(titleText, Translate("RATE_US_TITLE", "Rate us"));
            SetText(questionText, Translate(
                "RATE_US_QUESTION", "How do you like Meowdoku?"));
            ApplyStars();
            popupAnimator?.PlayOpen();
            if (restyled)
                StartManagedCoroutine(AutoSelectRestyledStars());
        }

        protected override IEnumerator PlayCloseAnimation()
        {
            if (_closeAlreadyPlayed)
            {
                _closeAlreadyPlayed = false;
                yield break;
            }
            if (popupAnimator != null) yield return popupAnimator.PlayClose();
        }

        protected override IEnumerator OnHide()
        {
            popupAnimator?.Stop();
            _closing = false;
            yield break;
        }

        protected override bool OnBackRequest()
        {
            Close();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            if (rateButton != null) rateButton.onClick.RemoveListener(Submit);
            if (rateCloseButton != null)
                rateCloseButton.onClick.RemoveListener(Close);
            Button[] buttons = starButtons ?? Array.Empty<Button>();
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index] != null && _starActions != null &&
                    index < _starActions.Length && _starActions[index] != null)
                    buttons[index].onClick.RemoveListener(_starActions[index]);
            }
            _starActions = null;
            Closed = null;
            base.OnDestroyWindow();
        }

        private IEnumerator AutoSelectRestyledStars()
        {
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0f, restyledAutoSelectDelaySeconds));
            if (!_closing && IsShowing) SelectStarsAt(5);
        }

        private void SelectStarsAt(int count)
        {
            if (_closing) return;
            _selectedStars = Mathf.Clamp(count, 1, 5);
            ApplyStars();
        }

        private void Submit()
        {
            if (_closing || _selectedStars <= 0) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.RateUs,
                GetTrackingDialogName(),
                GetTrackingDialogExtra());
            StartManagedCoroutine(CloseAfterAnimation(
                new RateUsResult(_selectedStars, true)));
        }

        private void Close()
        {
            if (_closing) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Close,
                GetTrackingDialogName(),
                GetTrackingDialogExtra());
            StartManagedCoroutine(CloseAfterAnimation(new RateUsResult(0, false)));
        }

        private IEnumerator CloseAfterAnimation(RateUsResult result)
        {
            if (_closing) yield break;
            _closing = true;
            if (popupAnimator != null) yield return popupAnimator.PlayClose();
            _closeAlreadyPlayed = true;
            Closed?.Invoke(result);
            Owner?.Hide(UiName);
        }

        private void ApplyStars()
        {
            if (stars == null) return;
            for (int index = 0; index < stars.Length; index++)
            {
                Image image = stars[index];
                if (image == null) continue;
                image.sprite = index < _selectedStars ? litStar : dimStar;
                image.color = image.sprite == null
                    ? (index < _selectedStars ? Color.white : new Color(1f, 1f, 1f, 0.35f))
                    : Color.white;
            }
            if (rateButton != null) rateButton.interactable = _selectedStars > 0;
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string translated = localization.Translate(key);
            return translated == key || string.IsNullOrEmpty(translated)
                ? fallback
                : translated;
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }
    }

    /// <summary>Pointer/drag adapter for the five source star hit areas.</summary>
    [DisallowMultipleComponent]
    public sealed class RateUsStarPointerView : MonoBehaviour,
        IPointerDownHandler, IDragHandler
    {
        [SerializeField] private int starIndex = 1;
        private Action<int> _select;

        public void Bind(Action<int> select) => _select = select;

        public void OnPointerDown(PointerEventData eventData) => _select?.Invoke(starIndex);

        public void OnDrag(PointerEventData eventData)
        {
            if (_select == null) return;
            RectTransform parent = transform.parent as RectTransform;
            if (parent == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, eventData.position, eventData.pressEventCamera, out local))
                return;
            float width = parent.rect.width / 5f;
            int selected = Mathf.FloorToInt((local.x - parent.rect.xMin) / width) + 1;
            _select(Mathf.Clamp(selected, 1, 5));
        }
    }
}
