using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Meowdoku.Core;

namespace Meowdoku.Gameplay
{
    // Script quản lý toàn bộ Bàn cờ (Board).
    // Gắn vào một GameObject có GridLayoutGroup để tự động xếp các ô.
    public class BoardView : MonoBehaviour
    {
        [Header("Board Settings")]
        public GameObject cellPrefab;    // Kéo Prefab CellView vào đây
        public Transform cellsContainer; // Nơi chứa các ô (thường là chính GameObject này)
        
        [Header("Colors (Palette)")]
        public Color[] regionColors;     // Bảng màu cho các khu vực

        private CellView[,] _cells;
        private int _puzzleSize = 0;
        private int[][] _solutionCols;

        // Xóa sạch bàn cờ cũ nếu có
        public void ClearBoard()
        {
            foreach (Transform child in cellsContainer)
            {
                Destroy(child.gameObject);
            }
            _cells = null;
            _puzzleSize = 0;
        }

        // Khởi tạo bàn cờ mới dựa trên kích thước và dữ liệu bản đồ
        public void SetupBoard(int puzzleSize, int[][] regions, int[] colorMap)
        {
            ClearBoard();
            _puzzleSize = puzzleSize;
            _cells = new CellView[puzzleSize, puzzleSize];

            for (int r = 0; r < puzzleSize; r++)
            {
                for (int c = 0; c < puzzleSize; c++)
                {
                    // Sinh ra một ô (Cell) mới
                    GameObject cellObj = Instantiate(cellPrefab, cellsContainer);
                    cellObj.name = $"Cell_{r}_{c}";
                    
                    CellView cellView = cellObj.GetComponent<CellView>();
                    
                    // Xác định màu nền của ô dựa vào region (khu vực)
                    int regionIdx = regions[r][c];
                    int colorIdx = colorMap != null && regionIdx < colorMap.Length 
                                   ? colorMap[regionIdx] 
                                   : regionIdx % regionColors.Length;
                                   
                    cellView.SetRegionColor(regionColors[colorIdx]);
                    
                    _cells[r, c] = cellView;
                }
            }
        }

        // Thay đổi trạng thái của một ô cụ thể khi người chơi bấm vào
        public void SetCellState(int r, int c, CellStateType state, bool playAnim = true)
        {
            if (r < 0 || r >= _puzzleSize || c < 0 || c >= _puzzleSize) return;
            if (_cells == null || _cells[r, c] == null) return;

            CellStateType current = _cells[r, c].GetState();
            
            // Ngăn không cho đổi nếu ô đang là Mèo
            if (current == CellStateType.CAT && state != CellStateType.CAT) return;

            _cells[r, c].ChangeState(state, playAnim);
        }

        // Lấy trạng thái của một ô
        public CellStateType GetCellState(int r, int c)
        {
            if (r < 0 || r >= _puzzleSize || c < 0 || c >= _puzzleSize) return CellStateType.EMPTY;
            if (_cells == null || _cells[r, c] == null) return CellStateType.EMPTY;

            return _cells[r, c].GetState();
        }
    }
}
