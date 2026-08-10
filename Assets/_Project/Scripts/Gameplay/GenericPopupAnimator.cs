using System.Collections;
using DG.Tweening;
using Meowdoku.Core.UI;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Shared UGUI adapter for assets/animation/GenericPopup.res.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GenericPopupAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private CanvasGroup contentGroup;

        private Sequence _tween;

        public void PlayOpen()
        {
            Stop();
            if (content == null || contentGroup == null) return;
            content.localScale = Vector3.one * 0.7f;
            contentGroup.alpha = 0f;
            _tween = DOTween.Sequence().SetLink(gameObject);
            _tween.Append(content.DOScale(
                    1.05f,
                    SettingsPageContract.PopupOpenOvershootSeconds)
                .SetEase(Ease.Linear));
            _tween.Append(content.DOScale(
                    1f,
                    SettingsPageContract.PopupMarkerSeconds -
                    SettingsPageContract.PopupOpenOvershootSeconds)
                .SetEase(Ease.Linear));
            _tween.Insert(0f,
                contentGroup.DOFade(
                        1f,
                        SettingsPageContract.PopupOpenFadeSeconds)
                    .SetEase(Ease.Linear));
            _tween.OnComplete(() => _tween = null);
        }

        public IEnumerator PlayClose()
        {
            Stop();
            if (content == null || contentGroup == null) yield break;
            bool completed = false;
            float closeLength = SettingsPageContract.PopupLengthSeconds -
                                SettingsPageContract.PopupMarkerSeconds;
            float finalScaleSeconds = Mathf.Max(
                0f,
                closeLength - SettingsPageContract.PopupCloseOvershootSeconds);
            float fadeSeconds = Mathf.Max(
                0f,
                closeLength - SettingsPageContract.PopupCloseFadeStartSeconds);

            content.localScale = Vector3.one;
            contentGroup.alpha = 1f;
            _tween = DOTween.Sequence().SetLink(gameObject);
            _tween.Append(content.DOScale(
                    1.05f,
                    SettingsPageContract.PopupCloseOvershootSeconds)
                .SetEase(Ease.Linear));
            _tween.Append(content.DOScale(0.8f, finalScaleSeconds)
                .SetEase(Ease.Linear));
            _tween.Insert(
                SettingsPageContract.PopupCloseFadeStartSeconds,
                contentGroup.DOFade(0f, fadeSeconds).SetEase(Ease.Linear));
            _tween.OnComplete(() => completed = true);
            while (!completed && _tween != null && _tween.IsActive())
                yield return null;
            Stop();
        }

        public void Stop()
        {
            _tween?.Kill(false);
            _tween = null;
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
