using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.UI;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HowToPlayDemoBoardView : MonoBehaviour
    {
        [SerializeField, Min(1)] private int rows = 1;
        [SerializeField, Min(1)] private int columns = 1;
        [SerializeField] private CellView[] cells;
        [SerializeField, Min(0f)] private float cornerRadius = 8f;

        public int Rows => rows;
        public int Columns => columns;
        public int CellCount => cells?.Length ?? 0;

        public bool ApplyColors(IReadOnlyList<string> colorRows)
        {
            if (colorRows == null || colorRows.Count != rows ||
                cells == null || cells.Length != rows * columns)
                return false;

            Color[] palette = LevelGenerator.DefaultPalette;
            for (int row = 0; row < rows; row++)
            {
                string sourceRow = colorRows[row];
                if (string.IsNullOrEmpty(sourceRow) ||
                    sourceRow.Length != columns)
                    return false;
                for (int column = 0; column < columns; column++)
                {
                    CellView cell = Cell(row, column);
                    if (cell == null) return false;
                    RectTransform cellRect = cell.transform as RectTransform;
                    Vector2 anchoredPosition = cellRect != null
                        ? cellRect.anchoredPosition
                        : Vector2.zero;
                    cell.PrepareForUse(row, column);
                    if (cellRect != null)
                        cellRect.anchoredPosition = anchoredPosition;
                    int paletteIndex = HowToPlayContract.PaletteIndex(
                        sourceRow[column]);
                    if (paletteIndex >= 0 && paletteIndex < palette.Length)
                        cell.SetRegionColor(palette[paletteIndex]);
                    cell.ConfigureBackgroundShape(
                        new Vector4(
                            cornerRadius,
                            cornerRadius,
                            cornerRadius,
                            cornerRadius),
                        false);
                    cell.SetGraphicsRaycastTarget(false);
                }
            }
            return true;
        }

        public CellView Cell(HowToPlayCell position) =>
            Cell(position.Row, position.Column);

        public CellView Cell(int row, int column)
        {
            if (cells == null || row < 0 || row >= rows ||
                column < 0 || column >= columns)
                return null;
            return cells[row * columns + column];
        }

        public void ResetAll()
        {
            if (cells == null) return;
            for (int index = 0; index < cells.Length; index++)
                cells[index]?.ClearDemo();
        }

        internal void ConfigureForTests(
            int sourceRows,
            int sourceColumns,
            CellView[] sourceCells,
            float radius)
        {
            rows = sourceRows;
            columns = sourceColumns;
            cells = sourceCells;
            cornerRadius = radius;
        }
    }
}
