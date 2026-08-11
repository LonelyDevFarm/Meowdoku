using System.Collections;
using System.Text.RegularExpressions;
using Meowdoku.Core.Config;
using Meowdoku.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayWinToastPresenter : MonoBehaviour
    {
        private static readonly Regex NumberPattern =
            new(@"\d+%?", RegexOptions.Compiled);

        [SerializeField] private RectTransform content;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private Image tierIcon;
        [SerializeField] private Text messageText;
        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private Sprite perfectIcon;
        [SerializeField] private Sprite p5Icon;
        [SerializeField] private Sprite p10Icon;
        [SerializeField] private Sprite p20Icon;

        private readonly WinToastConfig _config = new();
        private Coroutine _hideRoutine;
        private int _generation;

        public bool TryShow(MainGameTransitionData transition)
        {
            HideImmediate();
            if (transition == null || !_config.IsEnabled()) return false;
            int tier = WinToastTierContract.DetermineTier(
                transition.Size,
                transition.StepsUsed);
            if (!_config.CoversTier(tier)) return false;

            string key = WinToastTierContract.MessageKey(
                tier,
                Random.Range(0, int.MaxValue));
            if (string.IsNullOrEmpty(key)) return false;
            string message = Translate(key)
                .Replace("{N}", transition.StepsUsed.ToString())
                .Replace("{CATS}", transition.Size.ToString());
            Color highlight = HighlightColor(tier);
            if (messageText != null)
            {
                messageText.supportRichText = true;
                messageText.text = HighlightNumbers(message, highlight);
            }
            if (tierIcon != null)
            {
                tierIcon.sprite = Icon(tier);
                tierIcon.preserveAspect = true;
            }

            gameObject.SetActive(true);
            int generation = ++_generation;
            popupAnimator?.PlayOpen();
            _hideRoutine = StartCoroutine(HideAfterDelay(generation));
            return true;
        }

        public void HideImmediate()
        {
            _generation++;
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }
            popupAnimator?.Stop();
            if (content != null) content.localScale = Vector3.one * 0.7f;
            if (contentGroup != null) contentGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private IEnumerator HideAfterDelay(int generation)
        {
            yield return new WaitForSecondsRealtime(1.3f);
            if (generation != _generation) yield break;
            if (popupAnimator != null)
                yield return popupAnimator.PlayClose();
            if (generation != _generation) yield break;
            _hideRoutine = null;
            gameObject.SetActive(false);
        }

        private string Translate(string key)
        {
            if (localization == null) return key;
            string value = localization.Translate(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }

        private Sprite Icon(int tier)
        {
            return tier switch
            {
                WinToastTierContract.TierPerfect => perfectIcon,
                WinToastTierContract.TierP5 => p5Icon,
                WinToastTierContract.TierP10 => p10Icon,
                WinToastTierContract.TierP20 => p20Icon,
                _ => null
            };
        }

        private static Color HighlightColor(int tier)
        {
            return tier switch
            {
                WinToastTierContract.TierPerfect =>
                    new Color(1f, 0.878431f, 0.082353f, 1f),
                WinToastTierContract.TierP5 =>
                    new Color(1f, 0.513725f, 0.984314f, 1f),
                WinToastTierContract.TierP10 =>
                    new Color(0.45098f, 0.788235f, 1f, 1f),
                _ => new Color(0.360784f, 0.854902f, 0.572549f, 1f)
            };
        }

        private static string HighlightNumbers(string value, Color color)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string hex = "#" + ColorUtility.ToHtmlStringRGB(color);
            return NumberPattern.Replace(
                value,
                match => $"<color={hex}>{match.Value}</color>");
        }

        private void OnDisable()
        {
            _generation++;
            popupAnimator?.Stop();
            _hideRoutine = null;
        }
    }
}
