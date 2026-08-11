using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ProfileSelectionCell : MonoBehaviour,
        IPointerDownHandler
    {
        private const float ScrollTapTolerance = 6f;

        [SerializeField] private ProfileAvatarView avatarView;
        [SerializeField] private GameObject check;
        [SerializeField] private RectTransform lockVisual;
        [SerializeField] private Button clickButton;

        private ScrollRect _scroll;
        private Sequence _lockTween;
        private float _pressScrollY;
        private int _id;
        private bool _locked;

        public event Action<ProfileSelectionCell, int> Pressed;
        public event Action<ProfileSelectionCell, int> LockedPressed;

        public int Id => _id;

        private void Awake()
        {
            if (clickButton != null) clickButton.onClick.AddListener(HandleClick);
        }

        public void SetupAvatar(int id, ScrollRect scroll)
        {
            _id = id;
            _scroll = scroll;
            avatarView?.SetFrameVisible(false);
            avatarView?.SetAvatarVisible(true);
            avatarView?.SetBaseVisible(true);
            avatarView?.SetAvatar(id);
        }

        public void SetupFrame(int id, int count, ScrollRect scroll)
        {
            _id = id;
            _scroll = scroll;
            avatarView?.SetAvatarVisible(false);
            avatarView?.SetBaseVisible(false);
            avatarView?.SetFrameVisible(true);
            avatarView?.SetFrame(id);
            avatarView?.SetCount(count);
        }

        public void SetSelected(bool selected)
        {
            if (check != null) check.SetActive(selected);
        }

        public void SetLocked(bool locked)
        {
            _locked = locked;
            if (lockVisual != null) lockVisual.gameObject.SetActive(locked);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressScrollY = ScrollY();
        }

        private void HandleClick()
        {
            if (Mathf.Abs(ScrollY() - _pressScrollY) > ScrollTapTolerance)
                return;
            if (_locked)
            {
                PlayLockShake();
                LockedPressed?.Invoke(this, _id);
            }
            else
            {
                Pressed?.Invoke(this, _id);
            }
        }

        private float ScrollY()
        {
            return _scroll != null && _scroll.content != null
                ? _scroll.content.anchoredPosition.y
                : 0f;
        }

        private void PlayLockShake()
        {
            if (lockVisual == null) return;
            _lockTween?.Kill(false);
            lockVisual.localRotation = Quaternion.identity;
            lockVisual.localScale = Vector3.one;
            _lockTween = DOTween.Sequence().SetLink(gameObject);
            _lockTween.Insert(0f,
                lockVisual.DOScale(1.12f, 0.06666667f).SetEase(Ease.Linear));
            _lockTween.Insert(0f,
                lockVisual.DORotate(
                    new Vector3(0f, 0f, -11.459156f),
                    0.1f).SetEase(Ease.Linear));
            _lockTween.Append(lockVisual.DORotate(
                    new Vector3(0f, 0f, 11.459156f),
                    0.1f)
                .SetEase(Ease.Linear));
            _lockTween.Append(lockVisual.DORotate(
                    new Vector3(0f, 0f, -5.729578f),
                    0.1f)
                .SetEase(Ease.Linear));
            _lockTween.Append(lockVisual.DORotate(Vector3.zero, 0.1f)
                .SetEase(Ease.Linear));
            _lockTween.Insert(0.3f,
                lockVisual.DOScale(1f, 0.1f).SetEase(Ease.Linear));
            _lockTween.OnComplete(() => _lockTween = null);
        }

        private void OnDestroy()
        {
            if (clickButton != null)
                clickButton.onClick.RemoveListener(HandleClick);
            _lockTween?.Kill(false);
        }
    }
}
