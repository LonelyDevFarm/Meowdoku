# R7 — Board và RuleBar theo layout nguồn

## Kết quả

- Dấu X đỏ đã được người dùng xác nhận không còn lỗi; thay đổi này không sửa lại domain ERROR.
- Loại bỏ việc RuleBar dùng khoảng `15 px` lấy nhầm từ `hint_overlay.gd`.
- Board mặc định không còn dùng khung prototype `600×600` để chia cell.
- Port công thức nguồn `intrinsic_size_for(N) = 108×N + 30`, gồm cell `100`, gap mỗi phía `4`, logical padding `15`.
- Scale đồng nhất board đến visible width `1008`; input vẫn tính trong local space bằng padding/slot nguồn nên không lệch hit cell.
- Port vị trí dọc RuleBar và Board từ profile `board_no_fuction.tres` với minimum height/stretch ratio của VBox.
- Glow `et_mask_001.png` dùng toàn texture và Unity sliced image với source patch margin: left `120`, top `117`, right `116`, bottom `116`.

## Cấu trúc trách nhiệm

- `SourceBoardLayout`: số đo thuần của board.
- `BoardView`: dựng GridLayoutGroup và xử lý input theo số đo local.
- `SourceGameplayPageLayout`: phép tính VBox thuần.
- `GameplayPageLayoutPresenter`: đặt RuleBar/Board trên Canvas.
- `GameplayRuleBarPresenter`: chỉ còn phản hồi vi phạm luật, không sở hữu page layout.

## Xác minh

- `Meowdoku.Gameplay` compile sạch bằng Unity Roslyn response file.
- `Meowdoku.Editor` compile sạch bằng Unity Roslyn response file.
- Thêm test cho board size 4/5/8/9/10, fixed visible width và tọa độ VBox tại `1080×1920`.
- Unity batchmode không thể mở vì project đang được một Unity instance khác giữ; cần Refresh để Unity import hai script/test mới, chuyển texture sang Single 9-slice và installer serialize scene.

## Còn mở trong R7

- Border/outline/corner mask và board grid overlay.
- Pool lifecycle đầy đủ.
- Safe-area/aspect profile và board-enlarge AB variant.
- Pixel/video parity cho size 4–10 trên Editor và thiết bị.
