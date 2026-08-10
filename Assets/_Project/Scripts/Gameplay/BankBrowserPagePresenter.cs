using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BankBrowserPagePresenter : UIFrameWindow
    {
        [Header("Header")]
        [SerializeField] private Button homeBackButton;

        [Header("Source panels")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private GameObject regularSizePanel;
        [SerializeField] private GameObject tierPanel;
        [SerializeField] private GameObject levelListPanel;
        [SerializeField] private GameObject lkPanel;
        [SerializeField] private GameObject variantSizePanel;

        [Header("Root cards")]
        [SerializeField] private BankRootCardView regularCard;
        [SerializeField] private BankRootCardView lkCard;
        [SerializeField] private BankRootCardView lkModifiedCard;
        [SerializeField] private BankRootCardView lkStyleCard;
        [SerializeField] private BankRootCardView gcCard;
        [SerializeField] private BankRootCardView specialCard;

        [Header("Regular size panel")]
        [SerializeField] private Button regularSizeBackButton;
        [SerializeField] private ScrollRect regularSizeScroll;
        [SerializeField] private RectTransform regularSizeContent;
        [SerializeField] private BankSizeCardView regularSizeTemplate;

        [Header("Variant size panel")]
        [SerializeField] private Button variantSizeBackButton;
        [SerializeField] private Text variantSizeTitle;
        [SerializeField] private ScrollRect variantSizeScroll;
        [SerializeField] private RectTransform variantSizeContent;
        [SerializeField] private BankSizeCardView variantSizeTemplate;

        [Header("Tier panel")]
        [SerializeField] private Button tierBackButton;
        [SerializeField] private Text tierTitle;
        [SerializeField] private ScrollRect tierScroll;
        [SerializeField] private RectTransform tierContent;
        [SerializeField] private BankTierCardView tierTemplate;

        [Header("SP list panel")]
        [SerializeField] private Button levelListBackButton;
        [SerializeField] private Text levelListTitle;
        [SerializeField] private ScrollRect levelListScroll;
        [SerializeField] private RectTransform levelListContent;
        [SerializeField] private BankLevelRowView specialRowTemplate;

        [Header("LK panel")]
        [SerializeField] private Button lkBackButton;
        [SerializeField] private Text lkTitle;
        [SerializeField] private Text lkInfoLabel;
        [SerializeField] private Button lkMinusButton;
        [SerializeField] private Button lkPlusButton;
        [SerializeField] private Button lkGoButton;
        [SerializeField] private Text lkNumberLabel;
        [SerializeField] private ScrollRect lkScroll;
        [SerializeField] private RectTransform lkContent;
        [SerializeField] private BankLevelRowView lkRowTemplate;

        private readonly List<BankSizeCardView> _regularSizeRows = new();
        private readonly List<BankSizeCardView> _variantSizeRows = new();
        private readonly List<BankTierCardView> _tierRows = new();
        private readonly List<BankLevelRowView> _specialRows = new();
        private readonly List<BankLevelRowView> _lkRows = new();

        private BankBrowserState _state;
        private IReadOnlyList<LevelEntry> _lkLevels = Array.Empty<LevelEntry>();
        private int _lkNumber = 1;

        private static readonly Color TextColor = Hex("333333");
        private static readonly Color Gray = Hex("888888");
        private static readonly Color Regular = Hex("2E8C47");
        private static readonly Color Lk = new(0.04f, 0.4f, 0.76f, 1f);
        private static readonly Color LkModified = new(0.12f, 0.58f, 0.45f, 1f);
        private static readonly Color LkStyle = new(0.38f, 0.18f, 0.72f, 1f);
        private static readonly Color Gc = new(0.08f, 0.6f, 0.45f, 1f);
        private static readonly Color Sp = Hex("E65100");

        protected override void OnCreate()
        {
            Add(homeBackButton, ExitToHome);
            Add(regularSizeBackButton, PanelBack);
            Add(variantSizeBackButton, PanelBack);
            Add(tierBackButton, PanelBack);
            Add(levelListBackButton, PanelBack);
            Add(lkBackButton, PanelBack);
            Add(lkMinusButton, DecreaseLkNumber);
            Add(lkPlusButton, IncreaseLkNumber);
            Add(lkGoButton, LaunchSelectedLk);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            _state = BankBrowserContract.ResolveInitial(parameters);
            RenderState();
        }

        protected override IEnumerator OnHide()
        {
            _lkLevels = Array.Empty<LevelEntry>();
            yield break;
        }

        protected override bool OnBackRequest()
        {
            if (_state.Panel == BankBrowserPanel.Root) ExitToHome();
            else PanelBack();
            return true;
        }

        protected override void OnDestroyWindow()
        {
            Remove(homeBackButton, ExitToHome);
            Remove(regularSizeBackButton, PanelBack);
            Remove(variantSizeBackButton, PanelBack);
            Remove(tierBackButton, PanelBack);
            Remove(levelListBackButton, PanelBack);
            Remove(lkBackButton, PanelBack);
            Remove(lkMinusButton, DecreaseLkNumber);
            Remove(lkPlusButton, IncreaseLkNumber);
            Remove(lkGoButton, LaunchSelectedLk);
            base.OnDestroyWindow();
        }

        private void RenderState()
        {
            SetPanel(rootPanel, _state.Panel == BankBrowserPanel.Root);
            SetPanel(regularSizePanel,
                _state.Panel == BankBrowserPanel.RegularSize);
            SetPanel(tierPanel, _state.Panel == BankBrowserPanel.Tier);
            SetPanel(levelListPanel,
                _state.Panel == BankBrowserPanel.LevelList);
            SetPanel(lkPanel, _state.Panel == BankBrowserPanel.LkList);
            SetPanel(variantSizePanel,
                _state.Panel == BankBrowserPanel.VariantSize);

            switch (_state.Panel)
            {
                case BankBrowserPanel.Root:
                    BuildRootCards();
                    break;
                case BankBrowserPanel.RegularSize:
                    BuildSizeRows(
                        BankPoolKind.Regular,
                        regularSizeTemplate,
                        regularSizeContent,
                        _regularSizeRows);
                    ScrollTop(regularSizeScroll);
                    break;
                case BankBrowserPanel.VariantSize:
                    BuildVariantSizes();
                    break;
                case BankBrowserPanel.Tier:
                    BuildTierRows();
                    break;
                case BankBrowserPanel.LkList:
                    BuildLkPanel();
                    break;
                case BankBrowserPanel.LevelList:
                    BuildSpecialRows();
                    break;
            }
        }

        private void BuildRootCards()
        {
            SizedPoolSummary regular = Summarize(BankPoolKind.Regular);
            ConfigureRoot(
                regularCard,
                regular.Total > 0,
                "常规题库",
                regular.SizeRange,
                regular.Total,
                string.Empty,
                Regular,
                Color.white,
                BankPoolKind.Regular);

            int lkCount = BankData.GetLkLevels().Count;
            ConfigureRoot(
                lkCard,
                lkCount > 0,
                "LK 题库",
                "LinkedIn Queens 存档",
                lkCount,
                string.Empty,
                Lk,
                Color.white,
                BankPoolKind.Lk);

            int modifiedCount = BankData.GetLkModifiedLevels().Count;
            ConfigureRoot(
                lkModifiedCard,
                modifiedCount > 0,
                "LK 改题库",
                "旋转/镜像变换版",
                modifiedCount,
                string.Empty,
                LkModified,
                Color.white,
                BankPoolKind.LkModified);

            SizedPoolSummary lkStyle = Summarize(BankPoolKind.LkStyle);
            ConfigureRoot(
                lkStyleCard,
                lkStyle.Total > 0,
                "LK 优化题库",
                lkStyle.SizeRange + "  LinkedIn 优化版",
                lkStyle.Total,
                lkStyle.Ranks,
                LkStyle,
                Color.white,
                BankPoolKind.LkStyle);

            SizedPoolSummary gc = Summarize(BankPoolKind.Gc);
            ConfigureRoot(
                gcCard,
                gc.Total > 0,
                "GC 题库",
                gc.SizeRange + "  紧凑型区域布局",
                gc.Total,
                gc.Ranks,
                Gc,
                Color.white,
                BankPoolKind.Gc);

            int spCount = BankData.GetSpecialLevels().Count;
            ConfigureRoot(
                specialCard,
                spCount > 0,
                "SP 特殊图案题库",
                "数字图案",
                spCount,
                string.Empty,
                Sp,
                Hex("FFF3E0"),
                BankPoolKind.Special);
        }

        private void ConfigureRoot(
            BankRootCardView card,
            bool visible,
            string title,
            string subtitle,
            int count,
            string metadata,
            Color accent,
            Color background,
            BankPoolKind pool)
        {
            if (card == null) return;
            card.gameObject.SetActive(visible);
            if (!visible) return;
            card.Configure(
                title,
                subtitle,
                $"{count} 关",
                metadata,
                accent,
                background,
                () =>
                {
                    _state = BankBrowserContract.OpenRootPool(pool);
                    RenderState();
                });
        }

        private void BuildVariantSizes()
        {
            if (variantSizeTitle != null)
            {
                variantSizeTitle.text = _state.Pool == BankPoolKind.Gc
                    ? "GC 题库"
                    : "LK 优化题库";
                variantSizeTitle.color = _state.Pool == BankPoolKind.Gc
                    ? Gc
                    : LkStyle;
            }
            BuildSizeRows(
                _state.Pool,
                variantSizeTemplate,
                variantSizeContent,
                _variantSizeRows);
            ScrollTop(variantSizeScroll);
        }

        private void BuildSizeRows(
            BankPoolKind pool,
            BankSizeCardView template,
            RectTransform content,
            List<BankSizeCardView> rows)
        {
            IReadOnlyList<int> sizes = BankBrowserContract.GetSizes(pool);
            int used = 0;
            foreach (int size in sizes)
            {
                int total = 0;
                var ranks = new List<int>(5);
                for (int rank = 1; rank <= 5; rank++)
                {
                    int count = BankBrowserContract.GetLevels(
                        pool, size, rank).Count;
                    if (count <= 0) continue;
                    ranks.Add(rank);
                    total += count;
                }
                if (total <= 0) continue;
                BankSizeCardView row = Acquire(rows, template, content, used++);
                int capturedSize = size;
                row.Configure(
                    size,
                    BankBrowserContract.SizeTierLabel(size),
                    total,
                    RankList(ranks),
                    SizeColor(size),
                    () =>
                    {
                        _state = BankBrowserContract.OpenSize(
                            pool, capturedSize);
                        RenderState();
                    });
            }
            HideUnused(rows, used);
        }

        private void BuildTierRows()
        {
            string prefix = _state.Pool switch
            {
                BankPoolKind.LkStyle => "LK优化 ",
                BankPoolKind.Gc => "GC ",
                _ => string.Empty
            };
            if (tierTitle != null)
                tierTitle.text = $"{prefix}{_state.Size}×{_state.Size} 题库";

            IReadOnlyList<BankTierBucket> buckets =
                BankBrowserContract.GetTierBuckets(
                    _state.Pool, _state.Size);
            int used = 0;
            foreach (BankTierBucket bucket in buckets)
            {
                BankTierCardView row = Acquire(
                    _tierRows, tierTemplate, tierContent, used++);
                Color badge = RankColor(bucket.Rank, bucket.IsHardTier);
                Color background = RankBackground(bucket.Rank,
                    bucket.IsHardTier);
                BankTierBucket captured = bucket;
                row.Configure(
                    bucket,
                    background,
                    badge,
                    RankGoColor(bucket.Rank, bucket.IsHardTier),
                    index => Launch(
                        _state.Pool,
                        index,
                        _state.Size,
                        captured.Rank,
                        captured.Tier));
            }
            HideUnused(_tierRows, used);
            ScrollTop(tierScroll);
        }

        private void BuildLkPanel()
        {
            bool modified = _state.Pool == BankPoolKind.LkModified;
            _lkLevels = BankBrowserContract.GetLevels(_state.Pool);
            _lkNumber = _lkLevels.Count > 0 ? 1 : 0;
            if (lkTitle != null)
            {
                lkTitle.text = modified ? "LK 改题库" : "LK 题库";
                lkTitle.color = modified ? LkModified : Lk;
            }
            if (lkInfoLabel != null)
            {
                lkInfoLabel.text = modified
                    ? $"共 {_lkLevels.Count} 关  ·  LK 改题库"
                    : $"共 {_lkLevels.Count} 关  ·  按日期排序";
            }
            RefreshLkSelector();

            int used = 0;
            for (int index = 0; index < _lkLevels.Count; index++)
            {
                LevelEntry entry = _lkLevels[index];
                BankLevelRowView row = Acquire(
                    _lkRows, lkRowTemplate, lkContent, used++);
                int captured = index;
                row.ConfigureLk(
                    entry,
                    index,
                    modified ? LkModified : Lk,
                    RankColor(entry.MaxRank, false),
                    () => Launch(
                        _state.Pool,
                        captured,
                        entry.Size,
                        entry.MaxRank,
                        string.Empty));
            }
            HideUnused(_lkRows, used);
            ScrollTop(lkScroll);
        }

        private void BuildSpecialRows()
        {
            IReadOnlyList<LevelEntry> levels =
                BankBrowserContract.GetLevels(BankPoolKind.Special);
            if (levelListTitle != null)
                levelListTitle.text = "SP 特殊图案题库";
            int used = 0;
            for (int index = 0; index < levels.Count; index++)
            {
                LevelEntry entry = levels[index];
                BankLevelRowView row = Acquire(
                    _specialRows,
                    specialRowTemplate,
                    levelListContent,
                    used++);
                int captured = index;
                row.ConfigureSpecial(
                    entry,
                    index,
                    RankColor(entry.Rank, false),
                    () => Launch(
                        BankPoolKind.Special,
                        captured,
                        entry.Size,
                        entry.Rank,
                        string.Empty));
            }
            HideUnused(_specialRows, used);
            ScrollTop(levelListScroll);
        }

        private void Launch(
            BankPoolKind pool,
            int index,
            int size,
            int rank,
            string tier)
        {
            if (Owner == null || !BankBrowserContract.TryCreateLaunch(
                    pool,
                    index,
                    size,
                    rank,
                    tier,
                    out BankLaunchRequest request))
                return;
            Owner.Show(UiName.Game, request.Parameters);
        }

        private void DecreaseLkNumber()
        {
            if (_lkLevels.Count <= 0) return;
            _lkNumber = Mathf.Max(1, _lkNumber - 1);
            RefreshLkSelector();
        }

        private void IncreaseLkNumber()
        {
            if (_lkLevels.Count <= 0) return;
            _lkNumber = Mathf.Min(_lkLevels.Count, _lkNumber + 1);
            RefreshLkSelector();
        }

        private void LaunchSelectedLk()
        {
            if (_lkNumber <= 0 || _lkNumber > _lkLevels.Count) return;
            LevelEntry entry = _lkLevels[_lkNumber - 1];
            Launch(
                _state.Pool,
                _lkNumber - 1,
                entry.Size,
                entry.MaxRank,
                string.Empty);
        }

        private void RefreshLkSelector()
        {
            if (lkNumberLabel != null)
                lkNumberLabel.text = _lkNumber.ToString();
            if (lkMinusButton != null)
                lkMinusButton.interactable = _lkNumber > 1;
            if (lkPlusButton != null)
                lkPlusButton.interactable =
                    _lkNumber > 0 && _lkNumber < _lkLevels.Count;
            if (lkGoButton != null)
                lkGoButton.interactable = _lkNumber > 0;
        }

        private void PanelBack()
        {
            _state = BankBrowserContract.PanelBack(_state);
            RenderState();
        }

        private void ExitToHome()
        {
            if (Owner == null) return;
            Owner.Show(UiName.Home);
            Owner.Hide(UiName.Bank);
        }

        private T Acquire<T>(
            List<T> pool,
            T template,
            Transform parent,
            int index) where T : Component
        {
            T result;
            if (index < pool.Count) result = pool[index];
            else
            {
                result = Instantiate(template, parent);
                result.name = template.name.Replace("Template", string.Empty) +
                              (index + 1);
                pool.Add(result);
            }
            result.transform.SetAsLastSibling();
            result.gameObject.SetActive(true);
            BindDynamicButtonGuards(result.transform);
            return result;
        }

        private static void HideUnused<T>(List<T> pool, int used)
            where T : Component
        {
            for (int index = used; index < pool.Count; index++)
                if (pool[index] != null)
                    pool[index].gameObject.SetActive(false);
        }

        private static void SetPanel(GameObject panel, bool visible)
        {
            if (panel != null) panel.SetActive(visible);
        }

        private static void ScrollTop(ScrollRect scroll)
        {
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private static void Add(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void Remove(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }

        private static SizedPoolSummary Summarize(BankPoolKind pool)
        {
            IReadOnlyList<int> sizes = BankBrowserContract.GetSizes(pool);
            int total = 0;
            var ranks = new List<int>(5);
            foreach (int size in sizes)
            {
                for (int rank = 1; rank <= 5; rank++)
                {
                    int count = BankBrowserContract.GetLevels(
                        pool, size, rank).Count;
                    if (count <= 0) continue;
                    total += count;
                    if (!ranks.Contains(rank)) ranks.Add(rank);
                }
            }
            string range = sizes.Count > 0
                ? $"{sizes[0]}×{sizes[0]} ~ " +
                  $"{sizes[sizes.Count - 1]}×{sizes[sizes.Count - 1]}"
                : string.Empty;
            return new SizedPoolSummary(total, range, RankList(ranks));
        }

        private static string RankList(IReadOnlyList<int> ranks)
        {
            if (ranks == null || ranks.Count == 0) return string.Empty;
            var parts = new string[ranks.Count];
            for (int index = 0; index < ranks.Count; index++)
                parts[index] = $"R{ranks[index]}";
            return string.Join(" · ", parts);
        }

        private static Color SizeColor(int size)
        {
            return size switch
            {
                4 => Hex("4CAF50"),
                5 => Hex("26A69A"),
                6 => Hex("FF9800"),
                7 => Hex("4A90E2"),
                8 => Hex("E25C4A"),
                9 => Hex("9B59B6"),
                10 => Hex("C0392B"),
                _ => Gray
            };
        }

        private static Color RankColor(int rank, bool hard)
        {
            if (hard) return rank == 4 ? Hex("C62828") : Hex("6A1B9A");
            return rank switch
            {
                1 => Hex("4CAF50"),
                2 => Hex("2196F3"),
                3 => Hex("FF9800"),
                4 => Hex("F44336"),
                5 => Hex("9C27B0"),
                _ => Gray
            };
        }

        private static Color RankBackground(int rank, bool hard)
        {
            if (hard) return rank == 4 ? Hex("FF8A80") : Hex("E040FB");
            return rank switch
            {
                1 => Hex("E8F5E9"),
                2 => Hex("E3F2FD"),
                3 => Hex("FFF8E1"),
                4 => Hex("FCE4EC"),
                5 => Hex("F3E5F5"),
                _ => Color.white
            };
        }

        private static Color RankGoColor(int rank, bool hard)
        {
            if (hard) return rank == 4 ? Hex("7F0000") : Hex("38006B");
            return rank switch
            {
                1 => Hex("388E3C"),
                2 => Hex("1565C0"),
                3 => Hex("E65100"),
                4 => Hex("B71C1C"),
                5 => Hex("6A1B9A"),
                _ => TextColor
            };
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString("#" + value, out Color color)
                ? color
                : Color.white;
        }

        private readonly struct SizedPoolSummary
        {
            public SizedPoolSummary(int total, string sizeRange, string ranks)
            {
                Total = total;
                SizeRange = sizeRange;
                Ranks = ranks;
            }

            public int Total { get; }
            public string SizeRange { get; }
            public string Ranks { get; }
        }
    }
}
