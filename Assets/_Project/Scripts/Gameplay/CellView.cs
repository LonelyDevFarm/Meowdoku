using UnityEngine;
using UnityEngine.UI;
using Meowdoku.Core;

namespace Meowdoku.Gameplay
{
    // Script gắn vào Prefab của một Ô (Cell) trên bàn cờ.
    // Dùng để điều khiển hình ảnh hiển thị (Con mèo, dấu chéo, màu nền).
    public class CellView : MonoBehaviour
    {
        [Header("UI References")]
        public Image bgImage;           // Ảnh nền của ô
        public Image catIcon;           // Ảnh con mèo (Đáp án đúng)
        public Image crossIcon;         // Ảnh dấu X (Bỏ qua)
        public Image errorIcon;         // Ảnh lỗi đỏ
        public Image patternImage;      // Lớp họa tiết chìm (nếu có)
        
        [Header("Effects")]
        public ParticleSystem appearVFX; // Hiệu ứng nổ hạt khi mèo xuất hiện

        private CellStateType _currentState = CellStateType.EMPTY;

        // Khởi tạo ban đầu
        private void Awake()
        {
            ResetToEmpty();
        }

        // Đổi màu nền của ô theo Region (khu vực)
        public void SetRegionColor(Color color)
        {
            if (bgImage != null)
            {
                bgImage.color = color;
            }
        }

        // Chuyển đổi trạng thái của ô (Hiển thị mèo, dấu chéo, v.v.)
        public void ChangeState(CellStateType newState, bool playAnim = true)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            UpdateVisuals(playAnim);
        }

        // Lấy trạng thái hiện tại
        public CellStateType GetState()
        {
            return _currentState;
        }

        // Xóa sạch trạng thái, đưa về ô trống
        public void ResetToEmpty()
        {
            _currentState = CellStateType.EMPTY;
            UpdateVisuals(false);
        }

        // Cập nhật hiển thị dựa trên trạng thái hiện tại
        private void UpdateVisuals(bool playAnim)
        {
            // Tắt hết các icon đi trước
            if (catIcon != null) catIcon.gameObject.SetActive(false);
            if (crossIcon != null) crossIcon.gameObject.SetActive(false);
            if (errorIcon != null) errorIcon.gameObject.SetActive(false);

            switch (_currentState)
            {
                case CellStateType.CAT:
                    if (catIcon != null) catIcon.gameObject.SetActive(true);
                    if (playAnim && appearVFX != null) appearVFX.Play();
                    // TODO: Gọi thư viện DOTween để làm hiệu ứng nảy (Bounce) ở đây sau này
                    break;

                case CellStateType.MARK:
                case CellStateType.LOCKED_MARK:
                    if (crossIcon != null) crossIcon.gameObject.SetActive(true);
                    break;

                case CellStateType.ERROR:
                    if (errorIcon != null) errorIcon.gameObject.SetActive(true);
                    // TODO: Chạy hiệu ứng rung lắc báo lỗi ở đây
                    break;
            }
        }
    }
}
