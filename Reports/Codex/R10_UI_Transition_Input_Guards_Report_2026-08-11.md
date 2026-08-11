# R10 UI transition và input guard

Ngày: 2026-08-11  
Unity: 6000.3.19f1

## Kết quả

Đã đóng phần stress transition/input guard trên desktop AppScene và port khoảng trống `UIManager.block_input_briefly` từ Godot. Không có runtime log mới.

- EditMode: **511 passed, 0 failed, 0 skipped, 0 inconclusive**, thời gian **53,655 giây**.
- PlayMode: **9 passed, 0 failed, 0 skipped, 0 inconclusive**, thời gian **117,966 giây**.

## Bằng chứng nguồn

- `scripts/module/ui/ui_manager.gd`: `block_input_briefly` tạo `_InputBlocker`, phủ toàn target, bắt input ở z tuyệt đối 4095 và tự hủy theo timer.
- `scripts/module/game/view/base_game_page.gd`: sai mèo cuối cùng khóa Game trong 2,0 giây trước Game Over.
- `scripts/module/result/view/game_fail_page.gd`: mỗi lần hiện Fail khóa toàn page 1,5 giây.
- `scripts/module/daily/view/daily_win_page.gd`: Daily Win khóa toàn page 2,0 giây.
- `scripts/module/result/view/game_win_page.gd`: Win chỉ khóa theo `APPEAR_DURATION` khi Rate Us hoặc Push Guide thực sự đủ điều kiện; nhánh này chưa được tự bật trong Unity vì module tương ứng chưa port.

## Chuyển thể Unity

- `UIManager.BlockInputBriefly` tạo transparent `Image` + nested `Canvas` + `GraphicRaycaster`, full-rect và sorting order 4095. Gọi lại trên cùng target thay deadline/blocker cũ; coroutine dùng realtime và dọn ownership khi hết hạn hoặc manager bị hủy.
- `GameplayPagePresenter` nghe batch feedback và khóa root Game 2,0 giây khi `WrongGuess` đưa `LivesAfter` về 0.
- `GameFailPagePresenter` dùng blocker 1,5 giây thay cho disable button tạm, vì source giữ hình/tint button bình thường và chặn pointer ở lớp phủ.
- `GameWinPagePresenter` dùng blocker 2,0 giây cho Daily Win; normal Win không bị khóa sai ngoài điều kiện Rate Us/Push chưa tồn tại.

## PlayMode stress

`AppScene_TransitionAndInputGuards_SurviveStress` xác nhận:

- pointer down/up thật trên Home Settings giữ release-frame guard đến cuối frame;
- local blocker có đúng raycast canvas z=4095, refresh không hết theo deadline cũ và chỉ còn một blocker;
- 96 vòng `Hide → Closing → Show` giữ cùng instance, không duplicate shown/hidden, không rò mask và ép Z compaction vẫn trong range nguồn;
- hai `ShowAsync(Language)` đồng thời dùng one-flight, chỉ create/show một page và không kẹt loading;
- các fail flow hiện có xác nhận blocker Game 2,0 giây và Fail 1,5 giây cùng tồn tại trước khi revive/restart.

## Còn mở

- Touch/raycast xuyên lớp trên thiết bị thật và notch/aspect thuộc R17, không đóng chỉ bằng fixture desktop.
- Khi Rate Us/Push Guide được port, phải nối consumer Win có điều kiện rồi bổ sung PlayMode cho đúng call site nguồn.
