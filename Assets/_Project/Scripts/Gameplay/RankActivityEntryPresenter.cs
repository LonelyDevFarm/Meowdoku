using System;
using DG.Tweening;
using Meowdoku.Core.Rank;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RankActivityEntryPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private GameObject pendingRewardState;
        [SerializeField] private GameObject activeState;
        [SerializeField] private GameObject rankMedal;
        [SerializeField] private Text rankText;
        [SerializeField] private Text countdownText;
        [SerializeField] private GameObject[] chestTiers = new GameObject[0];
        [SerializeField] private GameObject frameOnlyChest;
        [SerializeField] private Button clickButton;
        [SerializeField] private RectTransform shineVisual;
        [SerializeField] private CanvasGroup glowGroup;
        [SerializeField] private RectTransform pendingChestVisual;
        [SerializeField] private RectTransform activeArtVisual;
        [SerializeField] private CanvasGroup[] starGroups =
            new CanvasGroup[0];

        private RankActivityRuntime _runtime;
        private bool _presenting;
        private Tween _shineTween;
        private Sequence _glowTween;
        private Sequence _contentTween;
        private Tween[] _starTweens = new Tween[0];
        private Vector3 _glowBaseScale = Vector3.one;
        private Vector3 _pendingBaseScale = Vector3.one;
        private Vector3 _activeBaseScale = Vector3.one;
        private Vector2 _pendingBasePosition;
        private Vector2 _activeBasePosition;
        private bool _animatedPending;

        public event Action OpenRequested;

        private void Awake()
        {
            if (glowGroup != null)
                _glowBaseScale = glowGroup.transform.localScale;
            CacheBase(pendingChestVisual, out _pendingBaseScale,
                out _pendingBasePosition);
            CacheBase(activeArtVisual, out _activeBaseScale,
                out _activeBasePosition);
            if (clickButton != null)
                clickButton.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            if (_presenting) RefreshVfx();
        }

        private void OnDisable()
        {
            StopVfx();
        }

        private void OnDestroy()
        {
            StopVfx();
            Unsubscribe();
            if (clickButton != null)
                clickButton.onClick.RemoveListener(HandleClick);
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            if (_runtime == runtime) return;
            Unsubscribe();
            _runtime = runtime;
            Subscribe();
            RefreshNow();
        }

        public void Show()
        {
            _presenting = true;
            Subscribe();
            RefreshNow();
        }

        public void Hide()
        {
            _presenting = false;
            StopVfx();
            Unsubscribe();
        }

        public void RefreshNow()
        {
            RankActivityManager manager = _runtime?.Manager;
            bool shown = manager != null && manager.HasHomeEntry;
            SetActive(contentRoot != null ? contentRoot : gameObject, shown);
            if (!shown) return;

            RankSettlementResult pending = manager.GetPendingReward();
            bool hasPending = pending != null;
            SetActive(pendingRewardState, hasPending);
            SetActive(activeState, !hasPending);
            if (hasPending)
            {
                bool frameOnly = manager.Group ==
                                 RankActivityConfig.GroupFrameOnly;
                SetActive(frameOnlyChest, frameOnly);
                int tier = RankPresentationContract.EntryChestTier(
                    pending.Rank);
                for (int index = 0; index < chestTiers.Length; index++)
                    SetActive(
                        chestTiers[index],
                        !frameOnly && index == tier - 1);
            }
            else
            {
                bool hasRank = RankPresentationContract.ShowsPlayerRank(
                    manager.IsJoined,
                    manager.CollectTotal);
                SetActive(rankMedal, hasRank);
                if (rankText != null && hasRank)
                    rankText.text = manager.GetPlayerRank().ToString();
                if (countdownText != null)
                    countdownText.text = RankPresentationContract.FormatHms(
                        manager.RemainingSeconds);
            }
            RefreshVfx(hasPending);
        }

        private void Subscribe()
        {
            if (!_presenting || _runtime?.Manager == null) return;
            RankActivityManager manager = _runtime.Manager;
            manager.StateChanged -= HandleStateChanged;
            manager.StateChanged += HandleStateChanged;
            manager.RankingChanged -= HandleRankingChanged;
            manager.RankingChanged += HandleRankingChanged;
            manager.TimeTicked -= HandleTimeTicked;
            manager.TimeTicked += HandleTimeTicked;
        }

        private void RefreshVfx()
        {
            RankActivityManager manager = _runtime?.Manager;
            RefreshVfx(manager != null &&
                       manager.GetPendingReward() != null);
        }

        private void RefreshVfx(bool hasPending)
        {
            if (!_presenting || !isActiveAndEnabled)
            {
                StopVfx();
                return;
            }
            if (_shineTween != null && _shineTween.IsActive() &&
                _animatedPending == hasPending)
                return;

            StopVfx();
            _animatedPending = hasPending;
            if (shineVisual != null)
            {
                shineVisual.localRotation = Quaternion.identity;
                _shineTween = shineVisual
                    .DOLocalRotate(
                        new Vector3(0f, 0f, -360f),
                        16f,
                        RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
            if (glowGroup != null)
            {
                glowGroup.alpha = 0.18f;
                glowGroup.transform.localScale = _glowBaseScale * 0.88f;
                _glowTween = DOTween.Sequence()
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                _glowTween.Append(
                    glowGroup.DOFade(0.48f, 1.35f)
                        .SetEase(Ease.InOutSine));
                _glowTween.Join(
                    glowGroup.transform
                        .DOScale(_glowBaseScale * 1.1f, 1.35f)
                        .SetEase(Ease.InOutSine));
                _glowTween.SetLoops(-1, LoopType.Yoyo);
            }
            PlayContentVfx(hasPending);
            PlayStarVfx();
        }

        private void Unsubscribe()
        {
            if (_runtime?.Manager == null) return;
            RankActivityManager manager = _runtime.Manager;
            manager.StateChanged -= HandleStateChanged;
            manager.RankingChanged -= HandleRankingChanged;
            manager.TimeTicked -= HandleTimeTicked;
        }

        private void PlayContentVfx(bool hasPending)
        {
            RectTransform content = hasPending
                ? pendingChestVisual
                : activeArtVisual;
            Vector3 baseScale = hasPending
                ? _pendingBaseScale
                : _activeBaseScale;
            Vector2 basePosition = hasPending
                ? _pendingBasePosition
                : _activeBasePosition;
            if (content == null) return;

            content.localScale = baseScale;
            content.anchoredPosition = basePosition;
            _contentTween = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _contentTween.Append(
                content.DOAnchorPos(
                        basePosition + Vector2.up * 5f,
                        0.95f)
                    .SetEase(Ease.InOutSine));
            _contentTween.Join(
                content.DOScale(baseScale * 1.035f, 0.95f)
                    .SetEase(Ease.InOutSine));
            _contentTween.SetLoops(-1, LoopType.Yoyo);
        }

        private void PlayStarVfx()
        {
            _starTweens = new Tween[starGroups?.Length ?? 0];
            for (int index = 0; index < _starTweens.Length; index++)
            {
                CanvasGroup star = starGroups[index];
                if (star == null) continue;
                star.alpha = 0.18f;
                star.transform.localScale = Vector3.one * 0.72f;
                Sequence twinkle = DOTween.Sequence()
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                twinkle.AppendInterval(index * 0.24f);
                twinkle.Append(star.DOFade(0.95f, 0.42f));
                twinkle.Join(
                    star.transform.DOScale(1.12f, 0.42f)
                        .SetEase(Ease.OutQuad));
                twinkle.Append(star.DOFade(0.12f, 0.55f));
                twinkle.Join(
                    star.transform.DOScale(0.72f, 0.55f)
                        .SetEase(Ease.InQuad));
                twinkle.AppendInterval(
                    Mathf.Max(0.2f, 0.75f - index * 0.12f));
                twinkle.SetLoops(-1, LoopType.Restart);
                _starTweens[index] = twinkle;
            }
        }

        private void HandleStateChanged(RankActivityState _) => RefreshNow();
        private void HandleRankingChanged() => RefreshNow();
        private void HandleTimeTicked(int remaining, bool _)
        {
            if (activeState != null && activeState.activeInHierarchy &&
                countdownText != null)
                countdownText.text = RankPresentationContract.FormatHms(
                    remaining);
        }

        private void HandleClick()
        {
            if (_runtime?.Manager != null && _runtime.Manager.HasHomeEntry)
                OpenRequested?.Invoke();
        }

        private void StopVfx()
        {
            _shineTween?.Kill(false);
            _glowTween?.Kill(false);
            _contentTween?.Kill(false);
            _shineTween = null;
            _glowTween = null;
            _contentTween = null;
            for (int index = 0; index < _starTweens.Length; index++)
                _starTweens[index]?.Kill(false);
            _starTweens = new Tween[0];

            if (shineVisual != null)
                shineVisual.localRotation = Quaternion.identity;
            if (glowGroup != null)
            {
                glowGroup.alpha = 0.26f;
                glowGroup.transform.localScale = _glowBaseScale;
            }
            ResetContent(
                pendingChestVisual,
                _pendingBaseScale,
                _pendingBasePosition);
            ResetContent(
                activeArtVisual,
                _activeBaseScale,
                _activeBasePosition);
        }

        private static void CacheBase(
            RectTransform target,
            out Vector3 scale,
            out Vector2 position)
        {
            scale = target != null ? target.localScale : Vector3.one;
            position = target != null
                ? target.anchoredPosition
                : Vector2.zero;
        }

        private static void ResetContent(
            RectTransform target,
            Vector3 scale,
            Vector2 position)
        {
            if (target == null) return;
            target.localScale = scale;
            target.anchoredPosition = position;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
