using System;
using System.Collections.Generic;
using Meowdoku.Core.Localization;

namespace Meowdoku.Core.UI
{
    public readonly struct HowToPlayCell : IEquatable<HowToPlayCell>
    {
        public HowToPlayCell(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; }
        public int Column { get; }

        public bool Equals(HowToPlayCell other) =>
            Row == other.Row && Column == other.Column;

        public override bool Equals(object value) =>
            value is HowToPlayCell other && Equals(other);

        public override int GetHashCode() => (Row * 397) ^ Column;
    }

    public sealed class HowToPlayWave
    {
        public HowToPlayWave(int startFrame, params HowToPlayCell[] cells)
        {
            StartFrame = startFrame;
            Cells = Array.AsReadOnly(cells ?? Array.Empty<HowToPlayCell>());
        }

        public int StartFrame { get; }
        public IReadOnlyList<HowToPlayCell> Cells { get; }
    }

    public sealed class HowToPlayFullDemo
    {
        public HowToPlayFullDemo(
            string[] colors,
            HowToPlayCell[] animatedCats,
            HowToPlayCell[] staticCats,
            HowToPlayCell[] staticMarks,
            bool hasError,
            int errorFrame,
            HowToPlayCell errorCell,
            params HowToPlayWave[] waves)
        {
            Colors = Array.AsReadOnly(colors ?? Array.Empty<string>());
            AnimatedCats = Array.AsReadOnly(
                animatedCats ?? Array.Empty<HowToPlayCell>());
            StaticCats = Array.AsReadOnly(
                staticCats ?? Array.Empty<HowToPlayCell>());
            StaticMarks = Array.AsReadOnly(
                staticMarks ?? Array.Empty<HowToPlayCell>());
            HasError = hasError;
            ErrorFrame = errorFrame;
            ErrorCell = errorCell;
            Waves = Array.AsReadOnly(waves ?? Array.Empty<HowToPlayWave>());
        }

        public IReadOnlyList<string> Colors { get; }
        public IReadOnlyList<HowToPlayCell> AnimatedCats { get; }
        public IReadOnlyList<HowToPlayCell> StaticCats { get; }
        public IReadOnlyList<HowToPlayCell> StaticMarks { get; }
        public bool HasError { get; }
        public int ErrorFrame { get; }
        public HowToPlayCell ErrorCell { get; }
        public IReadOnlyList<HowToPlayWave> Waves { get; }
    }

    public sealed class HowToPlayPagedDemo
    {
        public HowToPlayPagedDemo(
            string[] colors,
            HowToPlayCell cat,
            bool hasError,
            int errorFrame,
            HowToPlayCell errorCell,
            string captionKey,
            params HowToPlayWave[] waves)
        {
            Colors = Array.AsReadOnly(colors ?? Array.Empty<string>());
            Cat = cat;
            HasError = hasError;
            ErrorFrame = errorFrame;
            ErrorCell = errorCell;
            CaptionKey = captionKey ?? string.Empty;
            Waves = Array.AsReadOnly(waves ?? Array.Empty<HowToPlayWave>());
        }

        public IReadOnlyList<string> Colors { get; }
        public HowToPlayCell Cat { get; }
        public bool HasError { get; }
        public int ErrorFrame { get; }
        public HowToPlayCell ErrorCell { get; }
        public string CaptionKey { get; }
        public IReadOnlyList<HowToPlayWave> Waves { get; }
    }

    /// <summary>
    /// Immutable data and exact timing copied from the two source HTP scripts.
    /// Vector2i.x in Godot is represented by Row and y by Column.
    /// </summary>
    public static class HowToPlayContract
    {
        public const int FramesPerSecond = 60;
        public const int PaletteBlue = 8;
        public const int PalettePink = 1;
        public const int PaletteYellow = 5;

        public const int FullRows = 3;
        public const int FullColumns = 5;
        public const float FullCellPixels = 100f;
        public const float FullRenderSlotPixels = 140f;
        public const float FullBoardScale = 1.33f;
        public const float FullCardWidth = 717f;
        public const float FullCardHeight = 434f;
        public const float FullCardLeft = 181f;
        public const float FullBoardMarginY = 12f;
        public const float FullTitleCenterDeltaY = -70f;
        public const int FullCrossStepFrames = 5;
        public const int FullStartDelayFrames = 6;
        public const int FullGapFrames = 12;
        public const int FullLastGapFrames = 24;

        public const int PagedSlotPixels = 108;
        public const int PagedCellGapPixels = 4;
        public const float PagedBoardPixels = 810f;
        public const float PagedBoardTop = 653f;
        public const float PagedClipTop = 608f;
        public const float PagedClipWidth = 900f;
        public const int PagedCrossStepFrames = 6;
        public const int PagedStartDelayFrames = 6;
        public const float PagedHoldAfterSeconds = 1.6f;
        public const float PagedSlideSeconds = 16f / FramesPerSecond;
        public const float PagedSlideDistance = PagedClipWidth;

        public const float CrossAnimationSeconds = 0.35f;
        public const float ErrorAnimationSeconds = 1.1f;
        public const float DemoDisappearSeconds = 0.1f;
        public const string HighlightColor = "#d94848";

        public static readonly IReadOnlyList<float> FullCardTops =
            Array.AsReadOnly(new[] { 403f, 1021f, 1640f });

        public static readonly IReadOnlyList<float> FullDividerTops =
            Array.AsReadOnly(new[] { 863f, 1488f });

        public static readonly IReadOnlyList<HowToPlayFullDemo> FullDemos =
            Array.AsReadOnly(new[]
            {
                new HowToPlayFullDemo(
                    new[] { "BBBBP", "BBBPY", "BBPYP" },
                    new[] { Cell(0, 1) },
                    new[] { Cell(2, 3) },
                    new[] { Cell(1, 4) },
                    true,
                    72,
                    Cell(2, 1),
                    Wave(134, Cell(0, 0), Cell(0, 2), Cell(0, 3)),
                    Wave(158, Cell(1, 0), Cell(1, 1), Cell(1, 2)),
                    Wave(182, Cell(2, 0))),
                new HowToPlayFullDemo(
                    new[] { "PYPYP", "BBBBB", "PYPYP" },
                    new[] { Cell(1, 2) },
                    Array.Empty<HowToPlayCell>(),
                    Array.Empty<HowToPlayCell>(),
                    false,
                    0,
                    default,
                    Wave(72, Cell(0, 2), Cell(2, 2)),
                    Wave(92, Cell(1, 0), Cell(1, 1), Cell(1, 3), Cell(1, 4))),
                new HowToPlayFullDemo(
                    new[] { "PPBBY", "PBBBP", "YBBPP" },
                    new[] { Cell(1, 2) },
                    Array.Empty<HowToPlayCell>(),
                    Array.Empty<HowToPlayCell>(),
                    false,
                    0,
                    default,
                    Wave(72,
                        Cell(2, 1), Cell(1, 1), Cell(0, 1), Cell(0, 2),
                        Cell(0, 3), Cell(1, 3), Cell(2, 3), Cell(2, 2)))
            });

        public static readonly IReadOnlyList<HowToPlayPagedDemo> PagedDemos =
            Array.AsReadOnly(new[]
            {
                new HowToPlayPagedDemo(
                    new[] { "BBBY", "BBYY", "BBPY", "BBPY" },
                    Cell(0, 1),
                    true,
                    72,
                    Cell(2, 0),
                    "GAME_RULE_ONE_PER_COLOR",
                    Wave(163, Cell(0, 0), Cell(1, 0), Cell(3, 0)),
                    Wave(194, Cell(1, 1), Cell(2, 1), Cell(3, 1)),
                    Wave(223, Cell(0, 2))),
                new HowToPlayPagedDemo(
                    new[] { "PBBBB", "PYBBB", "PYBBB", "PYBBB", "PBBBB" },
                    Cell(1, 1),
                    false,
                    0,
                    default,
                    "GAME_RULE_ONE_PER_LINE",
                    Wave(72, Cell(0, 1), Cell(2, 1), Cell(3, 1), Cell(4, 1)),
                    Wave(108, Cell(1, 0), Cell(1, 2), Cell(1, 3), Cell(1, 4))),
                new HowToPlayPagedDemo(
                    new[] { "PBBB", "PYBB", "PYBB", "PYYB" },
                    Cell(1, 1),
                    false,
                    0,
                    default,
                    "GAME_RULE_NO_TOUCH",
                    Wave(72,
                        Cell(2, 0), Cell(1, 0), Cell(0, 0), Cell(0, 1),
                        Cell(0, 2), Cell(1, 2), Cell(2, 2), Cell(2, 1)))
            });

        public static float SecondsAtFrame(int frame) =>
            Math.Max(0, frame) / (float)FramesPerSecond;

        public static float PagedBoardScale(int rows, int columns) =>
            rows <= 0 || columns <= 0
                ? 1f
                : PagedBoardPixels /
                  (Math.Max(rows, columns) * PagedSlotPixels);

        public static int PaletteIndex(char colorCode)
        {
            switch (colorCode)
            {
                case 'P': return PalettePink;
                case 'Y': return PaletteYellow;
                default: return PaletteBlue;
            }
        }

        public static string HighlightKeyword(string ruleKey, string locale)
        {
            string language = LocalizationLocaleContract.MainLanguage(locale);
            bool chinese = string.Equals(
                language,
                "zh",
                StringComparison.OrdinalIgnoreCase);
            bool english = string.Equals(
                language,
                "en",
                StringComparison.OrdinalIgnoreCase);
            if (!chinese && !english) return string.Empty;

            switch (ruleKey)
            {
                case "GAME_RULE_ONE_PER_COLOR":
                    return chinese ? "颜色" : "color";
                case "GAME_RULE_ONE_PER_LINE":
                    return chinese ? "同行同列" : "column and row";
                case "GAME_RULE_NO_TOUCH":
                    return chinese ? "相邻" : "adjacent";
                default:
                    return string.Empty;
            }
        }

        private static HowToPlayCell Cell(int row, int column) =>
            new(row, column);

        private static HowToPlayWave Wave(
            int frame,
            params HowToPlayCell[] cells) => new(frame, cells);
    }
}
