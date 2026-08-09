# GEM-R8-004 Đặc tả Resource Tool và Idle Hint

**STATUS**: COMPLETE  
**GENERATED_AT**: 2026-08-08 19:05:00  
**SOURCE_ROOT**: `D:\Projects\_GameExtract\Main_Meokdoku`

---

## 1. ToolButton (Giao Diện Công Cụ)
Định nghĩa tại `tool_button.gd`:
- **Trạng thái (`enum State`)**:
  - `NO_TOOL`: Không có công cụ (số lượng = 0). Huy hiệu màu xanh lá hiển thị icon dấu `+` hoặc biểu tượng Quảng cáo (Ad) tùy thuộc vào `ABTestManager.ad_compliance_ui`.
  - `HAS_TOOL`: Có công cụ sẵn. Huy hiệu màu đỏ hiển thị số lượng (`badge_count`). Nếu > 99, hiển thị "99+".
  - `FREE`: Giai đoạn tân thủ. Huy hiệu màu xanh lá chữ "Free".
- **Cập nhật UI**: Hàm `_refresh()` tự động điều chỉnh hiển thị Badge tùy vào trạng thái. `ToolButton` đóng gói các animation (Obtain, Click) và không chứa logic domain.

---

## 2. Hợp đồng Tiêu Hao và Quản lý Tài Nguyên (Contract)
Trong `base_game_page.gd` (`_consume_tool` và `_sync_tools_from_state`):
- **Kiểm tra Free Zone**: Nếu `ABTestManager.reward_unlock_level.is_reward_required_at(GameState.get_current_level())` trả về false, công cụ được gắn cờ `FREE`. Dùng không mất điểm, `_consume_tool` return `true` ngay lập tức.
- **Tiêu hao**: Nếu số lượng `> 0`, trừ đi 1. Lưu lại vào hệ thống bằng `GameState.set_tool_count(tool_name, new_count)`. Đồng thời gọi `Tracker.track_prop_use()`. Return `true` để cho phép logic Locate/Hint chạy tiếp.
- **Hết tài nguyên**: Nếu số lượng `<= 0`, hàm trả về `false` để chặn logic, đồng thời gọi `_request_reward_for_tool(btn)` để mời xem quảng cáo.
- **Tác động DDA & Save**: Việc CỘNG thêm tool không làm thay đổi bàn cờ. Nhưng việc SỬ DỤNG thành công Locate/Hint sẽ gọi `GameState.mark_current_level_dirty()` (lưu snapshot) và `GameState.mark_dda_tool_or_revive_used()` (ảnh hưởng tới điều chỉnh độ khó DDA sau này).

---

## 3. Đường Nhận Thưởng (Reward Path)
Khi nhấn nút Tool mà số lượng = 0 (`_request_reward_for_tool`):
- **Cooldown**: Có 1 khóa chống spam chạm 800ms (`_last_tool_deplete_ms`).
- **Phát Quảng Cáo**: Gọi `UniKitManager.show_reward("reward", pos, show_id)`.
- **Nhận thưởng**: Khi quảng cáo xem xong (`ad_rewarded`), callback gọi `_grant_tool_reward()`. Hàm này tạo phần thưởng thông qua `AwardManager.dispatch([AwardItem.make(prop_name, 1)], DIRECT, source)`.
- **Lưu ý**: Khác với luồng tiêu hao xử lý trực tiếp `GameState`, luồng nhận thưởng được giao khoán toàn bộ cho `AwardManager` để đồng bộ UI hoạt ảnh nhận quà.

---

## 4. Gợi ý Nhàn rỗi (Idle Hint)
Định nghĩa tại `base_game_page.gd`:
- **Đếm ngược**: Tích lũy thời gian không thao tác `_idle_time`. Nếu lớn hơn `_idle_hint_delay` (mặc định 20 giây), chuẩn bị phát hiệu ứng.
- **Điều kiện Kích hoạt (`_can_show_idle_hint`)**:
  - `BaseGamePage` đang hiện, chưa qua ván (`_is_complete == false`), không có lỗi sai (`_wrong_guess_pending == false`), và bảng Gợi Ý (`_hint_overlay`) không mở.
  - Cấu hình ABTest `prop_highlight` phải target vào một tool hợp lệ ("locate" hoặc "hint"). Nếu là "none", Idle Hint bị tắt hoàn toàn.
  - Nếu cấu hình là "chỉ chiếu 1 lần trong đời" (`is_once_per_lifetime()`) VÀ `GameState.has_prop_highlight_shown()` = true, thì tắt Idle Hint.
- **Hiệu ứng & Lặp lại**: Phát animation nhấp nháy cho nút Tool được cấu hình. Nếu cấu hình cho phép lặp (`is_repeatable()`), cứ sau mỗi 10 giây (`IDLE_HINT_REPEAT_PLAY_SEC`) hiệu ứng nháy sẽ tự chạy lại.
- **Reset**: Mọi thao tác click xuống bàn cờ hoặc sử dụng tool đều kích hoạt `_reset_idle_hint()` để đưa `_idle_time` về 0. Đồng thời lưu `GameState.mark_prop_highlight_shown()` để đánh dấu là đã xem.

---

## 5. Tình trạng Cấu hình (Config Check)
- **`prop_highlight`**: Thực sự được sử dụng và kiểm soát toàn bộ vòng đời của Idle Hint (như đã mô tả ở Mục 4).
- **`undo_btn`**: **LÀ CẤU HÌNH RÁC (DEAD CONFIG)**. File `undo_btn_config.gd` có tồn tại, nhưng mã nguồn không có bất kỳ truy vấn nào tới `ABTestManager.undo_btn`. Phù hợp với phát hiện ở R8-002: Chức năng Undo đã bị hủy bỏ hoàn toàn khỏi GamePage.

---

## 6. Đề Xuất Kiến Trúc Unity (C#)
1. **Lớp Domain (GameSession)**: Chứa biến số lượng tool `int LocateCount`, `int HintCount`. Hàm `bool TryConsumeLocate()`. Hàm báo cáo `Action OnToolUsed` để trigger DDA.
2. **Lớp UI (BaseGamePageView)**: Đăng ký lắng nghe sự kiện `OnToolCountChanged` để update `ToolButton`. Chứa biến đếm timer của Idle Hint và ngắt timer khi Event Bus bắn ra sự kiện PlayerTapped.
3. **Adapter (AdManager & AwardManager)**: Khi `TryConsume()` báo false, UI mem gọi ra Adapter để phát quảng cáo. Adapter trả về Success/Fail callback.

---

## 7. Đề Xuất Unit Test Fixture

| Input / Hành động | Trạng thái trước (State) | Kết quả (Expected Output) |
| --- | --- | --- |
| **Tiêu hao Tool có sẵn** | `LocateCount = 1`, `RewardRequired = true` | Gọi `Consume() -> True`. `LocateCount = 0`. UI chuyển Badge sang Green Ad/Plus. Ghi Dirty Snapshot. |
| **Bấm Tool khi bằng 0** | `LocateCount = 0`, `RewardRequired = true` | Gọi `Consume() -> False`. Không Dirty Snapshot. Hàm chặn (Guard). Trigger gọi SDK Ad_Show. |
| **Bấm Tool ở vùng Free** | `LocateCount = 0`, `RewardRequired = false` | Gọi `Consume() -> True`. Không thay đổi số lượng. ToolButton nhãn "Free". |
| **Idle Hint kích hoạt** | Cấu hình Target="hint", Chưa từng chiếu, Ván đang chơi | Chờ 20s. Bắn lệnh TriggerAnimation vào nút Hint. |
| **Idle Hint bị chặn** | Cấu hình Target="locate", Once_per_lifetime, Đã chiếu hôm qua | Chờ 20s. Không bắn lệnh Trigger, timer bị khóa. |

STATUS: COMPLETE
REPORT_ID: GEM-R8-004
