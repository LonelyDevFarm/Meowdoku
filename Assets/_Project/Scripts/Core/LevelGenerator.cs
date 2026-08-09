using System;
using System.Collections.Generic;
using Meowdoku.Core.Config;
using UnityEngine;

namespace Meowdoku.Core
{
    public static class LevelGenerator
    {
        internal static readonly int[][] DefaultRgb =
        {
            new[] { 230, 168, 193 }, new[] { 193, 101, 138 },
            new[] { 145, 121, 209 }, new[] { 254, 160, 236 },
            new[] { 255, 169, 108 }, new[] { 227, 186, 70 },
            new[] { 97, 130, 181 }, new[] { 166, 190, 216 },
            new[] { 105, 188, 230 }, new[] { 70, 179, 176 },
            new[] { 172, 217, 148 }, new[] { 171, 109, 70 }
        };

        public static readonly Color[] DefaultPalette = ColorsFromRgb(DefaultRgb);

        public static int[] ComputeColorMap(int size, int[][] regions)
        {
            return ComputeColorMapForRgb(size, regions, DefaultRgb);
        }

        public static int[] ComputeColorMapForRgb(int size, int[][] regions, int[][] rgb)
        {
            ValidateArguments(size, regions, rgb);
            List<HashSet<int>> adjacency = BuildAdjacency(size, regions);
            List<int> order = DegreeOrder(size, adjacency);
            return AssignColors(size, adjacency, order, rgb, null);
        }

        public static int[] ComputeColorMapWithSeed(int size, int[][] regions, int seed)
        {
            if (seed == 0) return ComputeColorMap(size, regions);
            ValidateArguments(size, regions, DefaultRgb);
            List<HashSet<int>> adjacency = BuildAdjacency(size, regions);
            var order = new List<int>(size);
            for (int region = 0; region < size; region++) order.Add(region);

            long state = ((long)seed * 1664525L + 1013904223L) & 2147483647L;
            for (int index = size - 1; index > 0; index--)
            {
                state = (state * 1664525L + 1013904223L) & 2147483647L;
                int other = (int)(state % (index + 1));
                int temporary = order[index];
                order[index] = order[other];
                order[other] = temporary;
            }
            return AssignColors(size, adjacency, order, DefaultRgb, null);
        }

        public static int[] ComputeColorMapForRgbWithPattern(
            int size,
            int[][] regions,
            int[][] rgb,
            IReadOnlyList<int> patternRegions)
        {
            return ComputePatternMap(size, regions, rgb, patternRegions, false);
        }

        public static int[] ComputeColorMapForLab(int size, int[][] regions, int[][] rgb)
        {
            ValidateArguments(size, regions, rgb);
            List<HashSet<int>> adjacency = BuildAdjacency(size, regions);
            List<int> order = DegreeOrder(size, adjacency);
            return AssignColors(size, adjacency, order, rgb, BuildLabPalette(rgb));
        }

        public static int[] ComputeColorMapForLabWithPattern(
            int size,
            int[][] regions,
            int[][] rgb,
            IReadOnlyList<int> patternRegions)
        {
            return ComputePatternMap(size, regions, rgb, patternRegions, true);
        }

        public static Color[] ColorsFromRgb(int[][] rgb)
        {
            if (rgb == null) throw new ArgumentNullException(nameof(rgb));
            var colors = new Color[rgb.Length];
            for (int index = 0; index < rgb.Length; index++)
            {
                colors[index] = new Color(
                    rgb[index][0] / 255f,
                    rgb[index][1] / 255f,
                    rgb[index][2] / 255f,
                    1f);
            }
            return colors;
        }

        private static int[] ComputePatternMap(
            int size,
            int[][] regions,
            int[][] rgb,
            IReadOnlyList<int> patternRegions,
            bool useLab)
        {
            ValidateArguments(size, regions, rgb);
            if (patternRegions == null) throw new ArgumentNullException(nameof(patternRegions));
            List<HashSet<int>> adjacency = BuildAdjacency(size, regions);
            double[][] lab = useLab ? BuildLabPalette(rgb) : null;

            var brightnessOrder = new List<BrightnessColor>(rgb.Length);
            for (int color = 0; color < rgb.Length; color++)
            {
                double luminance = 0.299 * rgb[color][0] + 0.587 * rgb[color][1] + 0.114 * rgb[color][2];
                brightnessOrder.Add(new BrightnessColor(luminance, color));
            }
            brightnessOrder.Sort((a, b) =>
            {
                int brightness = a.Value.CompareTo(b.Value);
                return brightness != 0 ? brightness : a.Color.CompareTo(b.Color);
            });

            var darkPool = new List<int>();
            var lightPool = new List<int>();
            for (int index = 0; index < brightnessOrder.Count; index++)
            {
                if (index < patternRegions.Count) darkPool.Add(brightnessOrder[index].Color);
                else lightPool.Add(brightnessOrder[index].Color);
            }

            var colorMap = new int[size];
            Array.Fill(colorMap, -1);
            var usedColors = new HashSet<int>();
            foreach (int region in patternRegions)
            {
                int color = BestColor(region, adjacency, colorMap, usedColors, darkPool, rgb, lab, darkPool[0]);
                colorMap[region] = color;
                usedColors.Add(color);
            }

            var remaining = new List<int>();
            for (int region = 0; region < size; region++) if (colorMap[region] < 0) remaining.Add(region);
            SortBySourceComparator(remaining, adjacency);
            IReadOnlyList<int> pool = lightPool.Count > 0 ? lightPool : darkPool;
            foreach (int region in remaining)
            {
                int color = BestColor(region, adjacency, colorMap, usedColors, pool, rgb, lab, 0);
                colorMap[region] = color;
                usedColors.Add(color);
            }
            return colorMap;
        }

        private static int[] AssignColors(
            int size,
            IReadOnlyList<HashSet<int>> adjacency,
            IReadOnlyList<int> order,
            int[][] rgb,
            double[][] lab)
        {
            var colorMap = new int[size];
            Array.Fill(colorMap, -1);
            var usedColors = new HashSet<int>();
            var pool = new int[rgb.Length];
            for (int color = 0; color < pool.Length; color++) pool[color] = color;
            foreach (int region in order)
            {
                int color = BestColor(region, adjacency, colorMap, usedColors, pool, rgb, lab, 0);
                colorMap[region] = color;
                usedColors.Add(color);
            }
            return colorMap;
        }

        private static int BestColor(
            int region,
            IReadOnlyList<HashSet<int>> adjacency,
            int[] colorMap,
            HashSet<int> usedColors,
            IReadOnlyList<int> pool,
            int[][] rgb,
            double[][] lab,
            int fallback)
        {
            var adjacentColors = new HashSet<int>();
            foreach (int neighbor in adjacency[region])
                if (colorMap[neighbor] >= 0) adjacentColors.Add(colorMap[neighbor]);

            int bestColor = fallback;
            double bestMinimumDistance = -1.0;
            foreach (int candidate in pool)
            {
                if (usedColors.Contains(candidate)) continue;
                double minimumDistance = double.PositiveInfinity;
                foreach (int adjacentColor in adjacentColors)
                {
                    double distance = lab == null
                        ? RgbDistance(rgb[candidate], rgb[adjacentColor])
                        : LabDistance(lab[candidate], lab[adjacentColor]);
                    if (distance < minimumDistance) minimumDistance = distance;
                }
                if (minimumDistance > bestMinimumDistance)
                {
                    bestMinimumDistance = minimumDistance;
                    bestColor = candidate;
                }
            }
            return bestColor;
        }

        private static List<HashSet<int>> BuildAdjacency(int size, int[][] regions)
        {
            var adjacency = new List<HashSet<int>>(size);
            for (int region = 0; region < size; region++) adjacency.Add(new HashSet<int>());
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    int region = regions[row][column];
                    if (column + 1 < size) AddEdge(adjacency, region, regions[row][column + 1]);
                    if (row + 1 < size) AddEdge(adjacency, region, regions[row + 1][column]);
                }
            }
            return adjacency;
        }

        private static List<int> DegreeOrder(int size, IReadOnlyList<HashSet<int>> adjacency)
        {
            var order = new List<int>(size);
            for (int region = 0; region < size; region++) order.Add(region);
            SortBySourceComparator(order, adjacency);
            return order;
        }

        private static void SortBySourceComparator(List<int> order, IReadOnlyList<HashSet<int>> adjacency)
        {
            order.Sort((a, b) =>
            {
                int degreeA = adjacency[a].Count;
                int degreeB = adjacency[b].Count;
                return degreeA != degreeB ? degreeA.CompareTo(degreeB) : a.CompareTo(b);
            });
        }

        private static void AddEdge(List<HashSet<int>> adjacency, int first, int second)
        {
            if (first == second) return;
            adjacency[first].Add(second);
            adjacency[second].Add(first);
        }

        private static double RgbDistance(int[] first, int[] second)
        {
            double red = first[0] - second[0];
            double green = first[1] - second[1];
            double blue = first[2] - second[2];
            return Math.Sqrt(red * red + green * green + blue * blue);
        }

        private static double[][] BuildLabPalette(int[][] rgb)
        {
            var result = new double[rgb.Length][];
            for (int index = 0; index < rgb.Length; index++)
                result[index] = SrgbToLab(rgb[index][0], rgb[index][1], rgb[index][2]);
            return result;
        }

        private static double[] SrgbToLab(double red, double green, double blue)
        {
            double r = SrgbLinear(red / 255.0);
            double g = SrgbLinear(green / 255.0);
            double b = SrgbLinear(blue / 255.0);
            double x = (r * 0.4124 + g * 0.3576 + b * 0.1805) / 0.95047;
            double y = r * 0.2126 + g * 0.7152 + b * 0.0722;
            double z = (r * 0.0193 + g * 0.1192 + b * 0.9505) / 1.08883;
            double fx = LabF(x);
            double fy = LabF(y);
            double fz = LabF(z);
            return new[] { 116.0 * fy - 16.0, 500.0 * (fx - fy), 200.0 * (fy - fz) };
        }

        private static double SrgbLinear(double value)
        {
            return value > 0.04045 ? Math.Pow((value + 0.055) / 1.055, 2.4) : value / 12.92;
        }

        private static double LabF(double value)
        {
            return value > 0.008856 ? Math.Pow(value, 1.0 / 3.0) : 7.787 * value + 16.0 / 116.0;
        }

        private static double LabDistance(double[] first, double[] second)
        {
            double l = first[0] - second[0];
            double a = first[1] - second[1];
            double b = first[2] - second[2];
            return Math.Sqrt(l * l + a * a + b * b);
        }

        private static void ValidateArguments(int size, int[][] regions, int[][] rgb)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (regions == null || regions.Length != size)
                throw new ArgumentException("Regions must be a size-by-size matrix.", nameof(regions));
            if (rgb == null || rgb.Length == 0) throw new ArgumentException("Palette cannot be empty.", nameof(rgb));
            for (int color = 0; color < rgb.Length; color++)
                if (rgb[color] == null || rgb[color].Length < 3)
                    throw new ArgumentException("Each palette entry needs RGB components.", nameof(rgb));
            for (int row = 0; row < size; row++)
            {
                if (regions[row] == null || regions[row].Length != size)
                    throw new ArgumentException("Regions must be a size-by-size matrix.", nameof(regions));
                for (int column = 0; column < size; column++)
                    if (regions[row][column] < 0 || regions[row][column] >= size)
                        throw new ArgumentException("Region ids must be in [0, size).", nameof(regions));
            }
        }

        private readonly struct BrightnessColor
        {
            public readonly double Value;
            public readonly int Color;
            public BrightnessColor(double value, int color) { Value = value; Color = color; }
        }
    }

    public sealed class RegionColorResult
    {
        public RegionColorResult(Color[] palette, int[] colorMap)
        {
            Palette = palette;
            ColorMap = colorMap;
        }

        public Color[] Palette { get; }
        public int[] ColorMap { get; }
    }

    public static class RegionColorPipeline
    {
        private static readonly int[][] Custom =
        {
            Rgb(203,203,36), Rgb(228,95,138), Rgb(141,122,235), Rgb(244,162,228),
            Rgb(255,142,61), Rgb(244,210,123), Rgb(75,127,192), Rgb(162,199,237),
            Rgb(10,174,207), Rgb(13,168,117), Rgb(136,206,122), Rgb(170,113,70)
        };
        private static readonly int[][] NewCell =
        {
            Rgb(205,164,0), Rgb(211,111,143), Rgb(137,121,218), Rgb(248,155,229),
            Rgb(250,157,92), Rgb(251,217,131), Rgb(80,118,165), Rgb(165,198,231),
            Rgb(56,169,192), Rgb(42,140,83), Rgb(139,213,125), Rgb(168,109,74)
        };
        private static readonly int[][] V3 =
        {
            Rgb(201,179,91), Rgb(211,114,145), Rgb(129,117,191), Rgb(239,162,224),
            Rgb(242,162,105), Rgb(240,207,126), Rgb(85,128,180), Rgb(164,197,231),
            Rgb(77,181,202), Rgb(63,160,104), Rgb(161,211,136), Rgb(176,121,89)
        };
        private static readonly int[][] V5 =
        {
            Rgb(172,113,71), Rgb(241,158,128), Rgb(195,106,138), Rgb(175,180,210),
            Rgb(197,140,237), Rgb(164,217,135), Rgb(101,132,179), Rgb(228,188,74),
            Rgb(254,163,233), Rgb(75,181,177), Rgb(222,126,52), Rgb(137,196,230)
        };
        private static readonly int[][] V6 =
        {
            Rgb(201,167,121), Rgb(202,102,102), Rgb(104,109,191), Rgb(150,132,236),
            Rgb(202,120,73), Rgb(74,186,52), Rgb(157,225,227), Rgb(76,171,219),
            Rgb(220,85,153), Rgb(244,164,231), Rgb(126,227,136), Rgb(219,188,72)
        };
        private static readonly int[][] V7 =
        {
            Rgb(182,124,84), Rgb(255,175,146), Rgb(55,185,94), Rgb(137,196,228),
            Rgb(166,115,216), Rgb(164,218,134), Rgb(103,103,195), Rgb(227,189,77),
            Rgb(251,175,234), Rgb(69,183,179), Rgb(221,130,64), Rgb(228,105,156)
        };
        private static readonly int[][] V8 =
        {
            Rgb(211,213,81), Rgb(216,108,144), Rgb(118,122,228), Rgb(229,126,224),
            Rgb(231,111,88), Rgb(220,207,149), Rgb(114,132,179), Rgb(192,209,220),
            Rgb(57,146,149), Rgb(69,154,94), Rgb(153,219,177), Rgb(166,123,99)
        };
        private static readonly int[][] V9 =
        {
            Rgb(220,158,124), Rgb(157,144,65), Rgb(151,207,126), Rgb(97,143,116),
            Rgb(96,216,211), Rgb(118,169,204), Rgb(104,129,208), Rgb(170,104,229),
            Rgb(216,153,228), Rgb(229,92,166), Rgb(187,126,147), Rgb(220,91,106)
        };
        private static readonly int[][] Warm =
        {
            Rgb(248,155,229), Rgb(205,164,0), Rgb(168,109,74),
            Rgb(251,217,131), Rgb(250,157,92), Rgb(211,111,143)
        };
        private static readonly int[][] Cool =
        {
            Rgb(137,121,218), Rgb(139,213,125), Rgb(56,169,192),
            Rgb(42,140,83), Rgb(80,118,165), Rgb(165,198,231)
        };

        public static RegionColorResult Resolve(
            int size,
            int[][] regions,
            int[] suppliedColorMap,
            IReadOnlyList<int> patternRegions,
            RegionColorConfig config = null)
        {
            config = config ?? new RegionColorConfig();
            patternRegions = patternRegions ?? Array.Empty<int>();
            int value = config.Value;
            int[][] rgb = PaletteFor(value, size);
            int[] colorMap = suppliedColorMap ?? LevelGenerator.ComputeColorMap(size, regions);

            switch (value)
            {
                case RegionColorConfig.ValueCellColorV3:
                case RegionColorConfig.ValueNewCellRecompute:
                    colorMap = LevelGenerator.ComputeColorMapForRgb(size, regions, rgb);
                    break;
                case RegionColorConfig.ValuePaletteV5:
                case RegionColorConfig.ValuePaletteV6:
                case RegionColorConfig.ValuePaletteV7:
                    colorMap = patternRegions.Count > 0
                        ? LevelGenerator.ComputeColorMapForRgbWithPattern(size, regions, rgb, patternRegions)
                        : LevelGenerator.ComputeColorMapForRgb(size, regions, rgb);
                    break;
                case RegionColorConfig.ValuePaletteV8:
                case RegionColorConfig.ValuePaletteV9:
                case RegionColorConfig.ValueAllWarm:
                case RegionColorConfig.ValueAllCool:
                case RegionColorConfig.ValueTempBalanced:
                    colorMap = patternRegions.Count > 0
                        ? LevelGenerator.ComputeColorMapForLabWithPattern(size, regions, rgb, patternRegions)
                        : LevelGenerator.ComputeColorMapForLab(size, regions, rgb);
                    break;
            }
            return new RegionColorResult(LevelGenerator.ColorsFromRgb(rgb), colorMap);
        }

        internal static int[][] PaletteFor(int value, int size)
        {
            switch (value)
            {
                case RegionColorConfig.ValueCustomPalette: return Custom;
                case RegionColorConfig.ValueNewCellOnly: return NewCell;
                case RegionColorConfig.ValueCellColorV3: return V3;
                case RegionColorConfig.ValueNewCellRecompute: return NewCell;
                case RegionColorConfig.ValuePaletteV5: return V5;
                case RegionColorConfig.ValuePaletteV6: return V6;
                case RegionColorConfig.ValuePaletteV7: return V7;
                case RegionColorConfig.ValuePaletteV8: return V8;
                case RegionColorConfig.ValuePaletteV9: return V9;
                case RegionColorConfig.ValueAllWarm: return Combine(Warm, Cool, Math.Max(0, size - 6));
                case RegionColorConfig.ValueAllCool: return Combine(Cool, Warm, Math.Max(0, size - 6));
                case RegionColorConfig.ValueTempBalanced:
                    return Combine(Take(Warm, (int)Math.Ceiling(size / 2.0)), Cool, size / 2);
                default: return LevelGenerator.DefaultRgb;
            }
        }

        private static int[][] Combine(int[][] first, int[][] second, int secondCount)
        {
            return Combine(first, first.Length, second, secondCount);
        }

        private static int[][] Combine(int[][] first, int firstCount, int[][] second, int secondCount)
        {
            firstCount = Math.Min(firstCount, first.Length);
            secondCount = Math.Min(secondCount, second.Length);
            var result = new int[firstCount + secondCount][];
            Array.Copy(first, 0, result, 0, firstCount);
            Array.Copy(second, 0, result, firstCount, secondCount);
            return result;
        }

        private static int[][] Take(int[][] source, int count)
        {
            count = Math.Min(count, source.Length);
            var result = new int[count][];
            Array.Copy(source, result, count);
            return result;
        }

        private static int[] Rgb(int red, int green, int blue) { return new[] { red, green, blue }; }
    }
}
