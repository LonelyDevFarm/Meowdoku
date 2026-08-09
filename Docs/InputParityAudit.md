# Input parity audit — 2026-08-08

## Kết luận

Lỗi tap/swipe trễ rồi tác động vào cell theo vị trí cursor mới có hai nguyên nhân trong bản Unity, không phải luật gameplay Godot: prototype từng nhận input trên từng `CellView` thay vì một `BoardView`, và adapter lưu endgame từng chạy PBKDF2 100.000 vòng + verify + fsync đồng bộ trên main thread. Input ownership đã chuyển về board; lưu endgame runtime nay chụp JSON bất biến rồi mã hóa/ghi trên một worker tuần tự, chỉ flush đồng bộ ở pause/focus-out/quit.

## Đối chiếu toàn chuỗi

| Hạng mục | Godot nguồn | Unity trước audit | Sau sửa | Kết luận |
|---|---|---|---|---|
| Nơi nhận pointer-down | `BoardView._gui_input` | Mỗi `CellView.OnPointerDown` | `BoardView.OnPointerDown` | Sai lệch bản port đã sửa |
| Cell tham gia hit-test | `cell.mouse_filter = MOUSE_FILTER_IGNORE` | Root và icon đều `raycastTarget = true` | Mọi `Graphic` trong cell không raycast | Sai lệch bản port đã sửa |
| Xác định cell bắt đầu | Board-local `pointer_to_cell(mb.position)` | Cell event target, sau đó cố hiệu chỉnh | Board-local từ press position đã latch | Đã về đúng ownership nguồn |
| Tap đầu | Action được trả ngay trong `on_drag_start` | Từng có pending 0,25/0,35 giây | Trả ngay; window chỉ nhận tap thứ hai | Đã sửa ở lần trước |
| Motion khi giữ pointer | `BoardView._input`, không có drag threshold | `IDragHandler` trên cell | `IDragHandler` trên board, `useDragThreshold = false` | Adapter Unity tương đương |
| Tick khi đang kéo | `SwipeGuardRecognizer.on_drag_tick` mỗi frame | Thiếu | Gọi mỗi frame trong `GameplayManager.Update` | Thiếu sót port đã sửa |
| Pointer-up | Board kết thúc stroke và commit step | Cell pointer-up | Board pointer-up; focus/disable hủy stroke | Đã sửa ownership/lifecycle |
| InputSystem/EventSystem dồn và phát Point/Click muộn | Không có khác biệt này trong Godot | Cả per-cell event, board event và raw-position latch vẫn phải chờ EventSystem phát gesture | Raw mouse-down/move/up gọi board gesture ngay; `EventSystem.RaycastAll` chỉ xác nhận board là hit trên cùng để giữ overlay blocking | Adapter Unity bắt buộc |
| Mapping cell | Padding/slot cố định hoặc A/B config của nguồn | `GridLayoutGroup` tạm và cell size động | Chưa đổi trong R6 | Không gây trễ; phải hoàn thiện ở R7 |
| Visual X | Action tức thời; nét đầu animation hiện khoảng 0,068 giây | Icon Unity bật ngay | Chưa port animation | Không thể gây chọn nhầm cell; thuộc visual parity sau |
| Save snapshot | MARK debounce 0,5 giây, CAT/ERROR ghi ngay | Unity mã hóa PBKDF2, verify và fsync ngay trên main thread | Giữ nguyên thời điểm snapshot nhưng ghi file trên worker; request liên tiếp gộp trạng thái mới nhất, lifecycle flush chờ durability | Sai lệch adapter gây khựng định kỳ đã sửa |
| Multi-touch | Luồng gameplay chủ yếu dựa primary pointer/mouse event | Khóa một pointer, latch theo device | Chưa test thiết bị thật | Cổng R6 còn mở |

## File đã thay đổi

- `Assets/_Project/Scripts/Gameplay/BoardView.cs`
- `Assets/_Project/Scripts/Gameplay/CellView.cs`
- `Assets/_Project/Scripts/Gameplay/GameplayManager.cs`
- `Assets/_Project/Scripts/Gameplay/Input/SwipeGuardRecognizer.cs`
- `Assets/_Project/Scripts/Core/SaveStore.cs`
- `Assets/_Project/Scripts/Core/GameStateRepository.cs`
- `Assets/_Project/Scripts/Core/GameStateService.cs`
- `Assets/_Project/Tests/EditMode/GameStateRepositoryTests.cs`
- `PORTING_ROADMAP.md`
- `Docs/SourceMap.md`
- `Docs/ParityChecklist.md`

## Xác minh

- Compile assembly Gameplay bằng Unity Roslyn: sạch lỗi.
- Compile Core, Gameplay và EditMode Tests bằng Unity Roslyn sau bản sửa persistence: sạch lỗi.
- Smoke test repository nền: enqueue đầu khoảng 5 ms; coalesce ghi đúng snapshot mới nhất, round-trip và clear đều đạt.
- Runner ngoài Unity: 190 test pass; 7 test data/generator không chạy do runtime ngoài Unity thiếu `System.Array.Fill/Reverse`, không liên quan input.
- Chưa đóng `P-INPUT-001`: cần refresh/compile trong Unity rồi retest PlayMode.

## Kịch bản PlayMode bắt buộc

1. Click-thả cell A rồi di chuyển ngay sang B: chỉ A đổi state.
2. Click-thả cell trống A rồi đưa cursor qua cell MARK B: A thành MARK, B không bị xóa.
3. Kéo nhanh từ cell 1 đến 7: cell bắt đầu đổi ngay và các cell bị bỏ qua được nội suy, không đổi cell kề ngoài đường.
4. Double tap cùng solution cell: tap đầu MARK tức thời, tap thứ hai kết thúc ở CAT; không tác động cell cursor đi qua sau đó.
5. Thả pointer ngoài board và mất focus: stroke kết thúc/hủy sạch, lần input sau hoạt động độc lập.
6. Click hoặc kéo-thả liên tục qua nhiều chu kỳ 0,5 giây: không còn nhịp đứng theo lúc snapshot được lưu.

Raw mouse path và UI mouse path có cờ ownership riêng: một sequence đã được raw path nhận sẽ làm các callback mouse tương ứng từ EventSystem no-op, tránh tap/drag/up bị phát hai lần. Touch tiếp tục đi qua EventSystem và press-position latch cho tới khi có kiểm thử thiết bị thật.
