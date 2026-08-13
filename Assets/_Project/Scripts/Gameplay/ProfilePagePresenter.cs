using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ProfilePagePresenter : UIFrameWindow,
        IProfileConsumer,
        IRankActivityConsumer
    {
        public override string GetTrackingDialogName() =>
            TrackerCatalog.Dialog.Avatar;

        private const float LockTipAutoCloseSeconds = 3f;
        private const float TipArrowCenterX = 390f;
        private const float LockTipFixedY = -365f;
        private const float AvatarScrollBottomExtend = 20f;
        private const float FrameScrollTopDown = 10f;
        private const float FrameScrollBottomExtend = 10f;
        private const float TipTextWidthWithGo = 500f;
        private const float TipTextWidthWithoutGo = 700f;

        [Header("Popup")]
        [SerializeField] private RectTransform content;
        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private Button actionCloseButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text confirmText;

        [Header("Identity")]
        [SerializeField] private ProfileAvatarView headerAvatar;
        [SerializeField] private InputField nicknameInput;
        [SerializeField] private Button nicknameClickButton;
        [SerializeField] private Button renameButton;

        [Header("Tabs")]
        [SerializeField] private Button avatarTabButton;
        [SerializeField] private Button frameTabButton;
        [SerializeField] private GameObject avatarActive;
        [SerializeField] private GameObject frameActive;
        [SerializeField] private GameObject frameRedDot;
        [SerializeField] private RoundedImageView gridBackground;
        [SerializeField] private Text avatarTabText;
        [SerializeField] private Text frameTabText;

        [Header("Scrollable grids")]
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform avatarGrid;
        [SerializeField] private GameObject leaderboardDivider;
        [SerializeField] private Text leaderboardTitle;
        [SerializeField] private RectTransform leaderboardGrid;
        [SerializeField] private GameObject classicDivider;
        [SerializeField] private Text classicTitle;
        [SerializeField] private RectTransform classicGrid;
        [SerializeField] private ProfileSelectionCell cellPrefab;

        [Header("Locked frame tooltip")]
        [SerializeField] private GameObject lockTipRoot;
        [SerializeField] private RectTransform lockTipBubble;
        [SerializeField] private CanvasGroup lockTipGroup;
        [SerializeField] private Text lockTipText;
        [SerializeField] private Button lockTipDismissButton;
        [SerializeField] private Button lockTipGoButton;
        [SerializeField] private GameObject lockTipGoRoot;
        [SerializeField] private Text lockTipGoText;

        [Header("Localization")]
        [SerializeField] private LocalizationCatalog localization;

        private readonly List<ProfileSelectionCell> _cells = new();
        private ProfileRuntime _runtime;
        private RankActivityRuntime _rankActivityRuntime;
        private Sequence _lockTipTween;
        private int _lockTipGeneration;
        private int _pendingAvatarId;
        private int _pendingFrameId;
        private bool _showingFrames;
        private bool _serviceSubscribed;
        private bool _fromRankOpenGuide;
        private Vector2 _scrollOriginalPosition;
        private Vector2 _scrollOriginalSize;

#if UNITY_INCLUDE_TESTS
        internal int PendingAvatarIdForTests => _pendingAvatarId;
        internal int PendingFrameIdForTests => _pendingFrameId;
        internal bool ShowingFramesForTests => _showingFrames;
        internal bool FrameRedDotVisibleForTests =>
            frameRedDot != null && frameRedDot.activeSelf;
        internal bool LockTipVisibleForTests =>
            lockTipRoot != null && lockTipRoot.activeSelf;
        internal bool LockTipGoVisibleForTests =>
            lockTipGoRoot != null && lockTipGoRoot.activeSelf;
        internal Vector2 ScrollViewportPositionForTests =>
            scroll?.viewport != null
                ? scroll.viewport.anchoredPosition
                : Vector2.zero;
        internal Vector2 ScrollViewportSizeForTests =>
            scroll?.viewport != null
                ? scroll.viewport.sizeDelta
                : Vector2.zero;
        internal int CellCountForTests => _cells.Count;
#endif

        protected override void OnCreate()
        {
            Add(actionCloseButton, Close);
            Add(confirmButton, Confirm);
            Add(renameButton, BeginRename);
            Add(nicknameClickButton, BeginRename);
            Add(avatarTabButton, ShowAvatarTab);
            Add(frameTabButton, ShowFrameTab);
            Add(lockTipDismissButton, CloseLockTip);
            Add(lockTipGoButton, OpenRankFromTip);
            if (nicknameInput != null)
            {
                nicknameInput.onEndEdit.AddListener(EndRename);
                nicknameInput.onValueChanged.AddListener(ConstrainNickname);
            }
            if (localization != null)
                localization.LocaleChanged += RefreshText;
            if (scroll?.viewport != null)
            {
                _scrollOriginalPosition = scroll.viewport.anchoredPosition;
                _scrollOriginalSize = scroll.viewport.sizeDelta;
            }
            RefreshText();
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            popupAnimator?.Stop();
            ProfileService service = _runtime?.Service;
            if (service == null) return;

            _pendingAvatarId = service.AvatarId;
            _pendingFrameId = service.FrameId;
            _fromRankOpenGuide = ReadBool(
                parameters,
                "from_rank_open_guide");
            if (nicknameInput != null)
            {
                nicknameInput.text = service.Nickname;
                nicknameInput.readOnly = true;
            }
            if (nicknameClickButton != null)
                nicknameClickButton.gameObject.SetActive(true);
            RefreshHeader();
            SubscribeService();
            CloseLockTipImmediate();
            ShowAvatarTab();
            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
            RefreshText();
            popupAnimator?.PlayOpen();
        }

        protected override IEnumerator PlayCloseAnimation()
        {
            if (popupAnimator != null)
                yield return popupAnimator.PlayClose();
        }

        protected override IEnumerator OnHide()
        {
            popupAnimator?.Stop();
            CloseLockTipImmediate();
            UnsubscribeService();
            ClearCells();
            _fromRankOpenGuide = false;
            yield break;
        }

        protected override void OnDestroyWindow()
        {
            Remove(actionCloseButton, Close);
            Remove(confirmButton, Confirm);
            Remove(renameButton, BeginRename);
            Remove(nicknameClickButton, BeginRename);
            Remove(avatarTabButton, ShowAvatarTab);
            Remove(frameTabButton, ShowFrameTab);
            Remove(lockTipDismissButton, CloseLockTip);
            Remove(lockTipGoButton, OpenRankFromTip);
            if (nicknameInput != null)
            {
                nicknameInput.onEndEdit.RemoveListener(EndRename);
                nicknameInput.onValueChanged.RemoveListener(ConstrainNickname);
            }
            if (localization != null)
                localization.LocaleChanged -= RefreshText;
            UnsubscribeService();
            ClearCells();
            base.OnDestroyWindow();
        }

        public void BindProfileRuntime(ProfileRuntime runtime)
        {
            if (_runtime == runtime) return;
            UnsubscribeService();
            _runtime = runtime;
            if (IsShowing) SubscribeService();
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            _rankActivityRuntime = runtime;
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            if (localization == catalog) return;
            if (localization != null)
                localization.LocaleChanged -= RefreshText;
            localization = catalog;
            if (localization != null)
                localization.LocaleChanged += RefreshText;
            RefreshText();
        }

        private void ShowAvatarTab()
        {
            _showingFrames = false;
            ApplyTabVisuals();
            BuildAvatarCells();
        }

        private void ShowFrameTab()
        {
            _showingFrames = true;
            _runtime?.Service.ClearFrameRedDot();
            ApplyTabVisuals();
            BuildFrameCells();
            StartManagedCoroutine(CenterSelectedFrameNextFrame());
        }

        private void ApplyTabVisuals()
        {
            if (avatarActive != null) avatarActive.SetActive(!_showingFrames);
            if (frameActive != null) frameActive.SetActive(_showingFrames);
            if (avatarGrid != null) avatarGrid.gameObject.SetActive(!_showingFrames);
            if (leaderboardDivider != null)
                leaderboardDivider.SetActive(_showingFrames);
            if (leaderboardGrid != null)
                leaderboardGrid.gameObject.SetActive(_showingFrames);
            if (classicDivider != null) classicDivider.SetActive(_showingFrames);
            if (classicGrid != null)
                classicGrid.gameObject.SetActive(_showingFrames);
            gridBackground?.SetCornerRadii(_showingFrames
                ? new Vector4(30f, 0f, 30f, 30f)
                : new Vector4(0f, 30f, 30f, 30f));
            ApplyScrollLayout();
            RefreshFrameRedDot();
        }

        private void ApplyScrollLayout()
        {
            RectTransform viewport = scroll?.viewport;
            if (viewport == null) return;

            if (_showingFrames)
            {
                viewport.anchoredPosition = _scrollOriginalPosition +
                    new Vector2(0f, -FrameScrollTopDown);
                viewport.sizeDelta = _scrollOriginalSize + new Vector2(
                    0f,
                    FrameScrollBottomExtend - FrameScrollTopDown);
            }
            else
            {
                viewport.anchoredPosition = _scrollOriginalPosition;
                viewport.sizeDelta = _scrollOriginalSize +
                    new Vector2(0f, AvatarScrollBottomExtend);
            }
        }

        private void BuildAvatarCells()
        {
            ClearCells();
            ProfileService service = _runtime?.Service;
            if (service == null || avatarGrid == null || cellPrefab == null)
                return;
            int[] ids = service.ListAvatarIds();
            for (int index = 0; index < ids.Length; index++)
            {
                ProfileSelectionCell cell = CreateCell(avatarGrid);
                cell.SetupAvatar(ids[index], scroll);
                cell.SetLocked(false);
                cell.SetSelected(ids[index] == _pendingAvatarId);
            }
            RebuildScroll();
        }

        private void BuildFrameCells()
        {
            ClearCells();
            ProfileService service = _runtime?.Service;
            if (service == null || cellPrefab == null) return;
            IReadOnlyList<ProfileFrameGroup> groups = service.GetFrameGroups();
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                ProfileFrameGroup group = groups[groupIndex];
                RectTransform grid = string.Equals(
                    group.Group,
                    "leaderboard",
                    StringComparison.Ordinal)
                    ? leaderboardGrid
                    : classicGrid;
                if (grid == null) continue;
                for (int index = 0; index < group.Ids.Count; index++)
                {
                    int id = group.Ids[index];
                    ProfileSelectionCell cell = CreateCell(grid);
                    cell.SetupFrame(id, service.GetFrameCount(id), scroll);
                    cell.SetLocked(!service.IsFrameUnlocked(id));
                    cell.SetSelected(id == _pendingFrameId);
                }
            }
            RebuildScroll();
        }

        private ProfileSelectionCell CreateCell(RectTransform parent)
        {
            ProfileSelectionCell cell = Instantiate(cellPrefab, parent);
            cell.name = $"Cell_{_cells.Count}";
            cell.gameObject.SetActive(true);
            cell.Pressed += HandleCellPressed;
            cell.LockedPressed += HandleLockedPressed;
            _cells.Add(cell);
            BindDynamicButtonGuards(cell.transform);
            return cell;
        }

        private void HandleCellPressed(ProfileSelectionCell selected, int id)
        {
            if (_showingFrames) _pendingFrameId = id;
            else _pendingAvatarId = id;
            for (int index = 0; index < _cells.Count; index++)
                _cells[index].SetSelected(_cells[index] == selected);
            RefreshHeader();
            EnsureVisible(selected.transform as RectTransform);
        }

        private void HandleLockedPressed(ProfileSelectionCell cell, int _)
        {
            EnsureVisible(cell.transform as RectTransform);
            ShowLockTip(cell.transform as RectTransform);
        }

        private void BeginRename()
        {
            if (nicknameInput == null) return;
            nicknameInput.readOnly = false;
            if (nicknameClickButton != null)
                nicknameClickButton.gameObject.SetActive(false);
            nicknameInput.ActivateInputField();
            nicknameInput.Select();
            nicknameInput.selectionAnchorPosition = 0;
            nicknameInput.selectionFocusPosition = nicknameInput.text.Length;
        }

        private void EndRename(string _)
        {
            if (nicknameInput == null) return;
            nicknameInput.readOnly = true;
            nicknameInput.DeactivateInputField();
            if (nicknameClickButton != null)
                nicknameClickButton.gameObject.SetActive(true);
        }

        private void ConstrainNickname(string value)
        {
            if (nicknameInput == null) return;
            string truncated = ProfileNickname.TruncateCodePoints(value, 12);
            if (!string.Equals(value, truncated, StringComparison.Ordinal))
                nicknameInput.SetTextWithoutNotify(truncated);
        }

        private void Confirm()
        {
            ProfileService service = _runtime?.Service;
            if (service == null) return;
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Save,
                GetTrackingDialogName());
            service.SetNickname(nicknameInput != null
                ? nicknameInput.text
                : service.Nickname);
            service.SetAvatarId(_pendingAvatarId);
            service.SetFrameId(_pendingFrameId);
            service.NotifyProfileSaved();
            if (nicknameInput != null) nicknameInput.text = service.Nickname;
            RefreshHeader();
            Owner?.Hide(UiName.Profile);
        }

        private void Close()
        {
            Tracking?.TrackButtonClick(
                TrackerCatalog.Button.Close,
                GetTrackingDialogName());
            Owner?.Hide(UiName.Profile);
        }

        private void ShowLockTip(RectTransform cell)
        {
            if (lockTipRoot == null || lockTipBubble == null || content == null)
                return;
            CloseLockTipImmediate();
            Vector3 worldCenter = cell.TransformPoint(cell.rect.center);
            Vector3 localCenter = content.InverseTransformPoint(worldCenter);
            float x = Mathf.Clamp(
                localCenter.x - TipArrowCenterX,
                0f,
                Mathf.Max(0f, content.rect.width - lockTipBubble.rect.width));
            lockTipBubble.anchoredPosition = new Vector2(x, LockTipFixedY);
            ApplyLockTipLayout();
            lockTipRoot.SetActive(true);
            lockTipBubble.localScale = Vector3.one * 0.5f;
            if (lockTipGroup != null) lockTipGroup.alpha = 0f;
            _lockTipTween = DOTween.Sequence().SetLink(gameObject);
            _lockTipTween.Append(lockTipBubble.DOScale(1.05f, 0.06666667f)
                .SetEase(Ease.Linear));
            _lockTipTween.Append(lockTipBubble.DOScale(0.97f, 0.13333333f)
                .SetEase(Ease.Linear));
            _lockTipTween.Append(lockTipBubble.DOScale(1f, 0.13333334f)
                .SetEase(Ease.Linear));
            if (lockTipGroup != null)
                _lockTipTween.Insert(0f,
                    lockTipGroup.DOFade(1f, 0.03333334f)
                        .SetEase(Ease.Linear));
            int generation = ++_lockTipGeneration;
            StartManagedCoroutine(AutoCloseLockTip(generation));
        }

        private IEnumerator AutoCloseLockTip(int generation)
        {
            yield return new WaitForSeconds(LockTipAutoCloseSeconds);
            if (generation == _lockTipGeneration) CloseLockTip();
        }

        private void CloseLockTip()
        {
            if (lockTipRoot == null || !lockTipRoot.activeSelf) return;
            _lockTipGeneration++;
            _lockTipTween?.Kill(false);
            _lockTipTween = DOTween.Sequence().SetLink(gameObject);
            _lockTipTween.Append(lockTipBubble.DOScale(1.05f, 0.1492852f)
                .SetEase(Ease.Linear));
            _lockTipTween.Append(lockTipBubble.DOScale(0.8f, 0.1673815f)
                .SetEase(Ease.Linear));
            if (lockTipGroup != null)
                _lockTipTween.Insert(0.0833333f,
                    lockTipGroup.DOFade(0f, 0.1f).SetEase(Ease.Linear));
            _lockTipTween.OnComplete(() =>
            {
                if (lockTipRoot != null) lockTipRoot.SetActive(false);
                _lockTipTween = null;
            });
        }

        private void CloseLockTipImmediate()
        {
            _lockTipGeneration++;
            _lockTipTween?.Kill(false);
            _lockTipTween = null;
            if (lockTipRoot != null) lockTipRoot.SetActive(false);
        }

        private void OpenRankFromTip()
        {
            RankActivityManager manager = _rankActivityRuntime?.Manager;
            if (manager == null || !manager.IsRunning ||
                _fromRankOpenGuide || Owner == null)
                return;
            StartManagedCoroutine(OpenRankHomeEntry(manager));
        }

        private void ApplyLockTipLayout()
        {
            bool rankOn = _rankActivityRuntime?.Manager?.IsRunning == true &&
                          !_fromRankOpenGuide;
            if (lockTipGoRoot != null) lockTipGoRoot.SetActive(rankOn);
            if (lockTipText == null) return;
            RectTransform rect = lockTipText.rectTransform;
            Vector2 size = rect.sizeDelta;
            size.x = rankOn
                ? TipTextWidthWithGo
                : TipTextWidthWithoutGo;
            rect.sizeDelta = size;
            lockTipText.alignment = rankOn
                ? TextAnchor.MiddleLeft
                : TextAnchor.MiddleCenter;
        }

        private IEnumerator OpenRankHomeEntry(RankActivityManager manager)
        {
            CloseLockTipImmediate();
            Owner.Hide(UiName.Profile);

            if (manager.IsOpenNotJoined)
            {
                RequestHomeRankPopup();
                yield break;
            }

            UIFrameWindow page = Owner.Show(UiName.RankActivityPage);
            if (page == null) yield break;
            yield return Owner.AwaitHidden(UiName.RankActivityPage);
            yield return null;
            yield return null;
            RequestHomeRankPopup();
        }

        private void RequestHomeRankPopup()
        {
            if (Owner?.Get(UiName.Home) is HomePagePresenter home)
                home.RequestRankOpenPopup();
        }

        private void RefreshHeader()
        {
            headerAvatar?.SetInfo(_pendingAvatarId, _pendingFrameId);
            headerAvatar?.SetCount(_runtime?.Service.GetFrameCount(
                _pendingFrameId) ?? -1);
        }

        private void RefreshFrameRedDot()
        {
            if (frameRedDot != null)
                frameRedDot.SetActive(
                    !_showingFrames &&
                    (_runtime?.Service.HasFrameRedDot ?? false));
        }

        private void RefreshText()
        {
            SetText(titleText, "PROFILE_TITLE", "Profile");
            SetText(avatarTabText, "PROFILE_TAB_AVATAR", "Avatar");
            SetText(frameTabText, "PROFILE_TAB_FRAME", "Frame");
            SetText(
                leaderboardTitle,
                "PROFILE_FRAME_GROUP_LEADERBOARD",
                "Leaderboard");
            SetText(
                classicTitle,
                "PROFILE_FRAME_GROUP_CLASSIC",
                "Classic");
            SetText(
                lockTipText,
                "PROFILE_FIRST_PLACE_FRAME_HINT",
                "Get first place in the challenge to get this frame!");
            SetText(lockTipGoText, "PROFILE_GO", "GO");
            SetText(confirmText, "PROFILE_CONFIRM", "Confirm");
        }

        private void SetText(Text target, string key, string fallback)
        {
            if (target == null) return;
            string value = localization != null
                ? localization.Translate(key)
                : key;
            target.text = value == key ? fallback : value;
        }

        private void SubscribeService()
        {
            if (_serviceSubscribed || _runtime == null) return;
            _runtime.Service.AvatarFrameChanged += HandleProfileChanged;
            _serviceSubscribed = true;
        }

        private void UnsubscribeService()
        {
            if (!_serviceSubscribed || _runtime == null) return;
            _runtime.Service.AvatarFrameChanged -= HandleProfileChanged;
            _serviceSubscribed = false;
        }

        private void HandleProfileChanged()
        {
            RefreshFrameRedDot();
        }

        private void ClearCells()
        {
            for (int index = 0; index < _cells.Count; index++)
            {
                ProfileSelectionCell cell = _cells[index];
                if (cell == null) continue;
                cell.Pressed -= HandleCellPressed;
                cell.LockedPressed -= HandleLockedPressed;
                Destroy(cell.gameObject);
            }
            _cells.Clear();
        }

        private void RebuildScroll()
        {
            Canvas.ForceUpdateCanvases();
            if (scroll?.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        }

        private IEnumerator CenterSelectedFrameNextFrame()
        {
            yield return null;
            for (int index = 0; index < _cells.Count; index++)
            {
                if (_cells[index].Id != _pendingFrameId) continue;
                EnsureVisible(_cells[index].transform as RectTransform, true);
                yield break;
            }
        }

        private void EnsureVisible(RectTransform target, bool center = false)
        {
            if (target == null || scroll == null || scroll.content == null ||
                scroll.viewport == null)
                return;
            Canvas.ForceUpdateCanvases();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                scroll.viewport,
                target);
            Rect view = scroll.viewport.rect;
            float delta = 0f;
            if (center)
                delta = bounds.center.y - view.center.y;
            else if (bounds.max.y > view.yMax)
                delta = bounds.max.y - view.yMax + 12f;
            else if (bounds.min.y < view.yMin)
                delta = bounds.min.y - view.yMin - 12f;
            if (Mathf.Abs(delta) < 0.01f) return;
            Vector2 position = scroll.content.anchoredPosition;
            position.y += delta;
            scroll.content.anchoredPosition = position;
        }

        private static void Add(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void Remove(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }

        private static bool ReadBool(
            IReadOnlyDictionary<string, object> parameters,
            string key)
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out object value) ||
                value == null)
                return false;
            if (value is bool flag) return flag;
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
