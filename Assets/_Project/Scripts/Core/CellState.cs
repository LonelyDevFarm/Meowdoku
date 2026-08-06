namespace Meowdoku.Core
{
    // Danh sách các Trạng Thái (State) của một ô vuông trên bàn cờ.
    public enum CellStateType
    {
        EMPTY = 0,         // Ô trống
        CAT = 1,           // Ô điền Mèo (Đáp án đúng)
        MARK = 2,          // Ô bị đánh dấu chéo (X)
        ERROR = 3,         // Ô bị lỗi đỏ do điền sai
        DRAFT_CROSS = 4,   // Nháp dấu chéo
        DRAFT_CAT = 5,     // Nháp hình mèo
        LOCKED_MARK = 6    // Bị khóa dấu chéo (do gợi ý/level mặc định)
    }

    // Tiện ích bổ trợ để kiểm tra nhóm trạng thái của ô.
    public static class CellState
    {
        // Trả về true nếu ô đang trống hoặc ở chế độ nháp.
        public static bool IsBlank(CellStateType s)
        {
            return s == CellStateType.EMPTY || s == CellStateType.DRAFT_CROSS || s == CellStateType.DRAFT_CAT;
        }

        // Trả về true nếu ô đang có dấu chéo (X).
        public static bool IsCross(CellStateType s)
        {
            return s == CellStateType.MARK || s == CellStateType.ERROR || s == CellStateType.LOCKED_MARK;
        }
    }
}
