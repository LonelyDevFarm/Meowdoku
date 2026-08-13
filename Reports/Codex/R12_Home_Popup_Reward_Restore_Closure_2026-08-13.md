# R12 Home Popup và Reward Restore Closure — 2026-08-13

## Kết quả

Đã khép `P-HOME-008`. Home priority queue hiện có bằng chứng runtime cho AB switch, Rank reward/open và rewarded-ad restore; lượt này khóa riêng nhánh restore qua Collect, Close và reopen.

- Platform PlayMode: **17/17**, 0 fail, 0 skip, 167,105 giây.
- Full EditMode ổn định gần nhất: **678/678**; không chạy lại vì thay đổi production chỉ reset trạng thái tương tác của một nút và toàn bộ PlayMode assembly đã compile sạch.
- Không thêm SDK quảng cáo thật, network call hoặc runtime log.

## Đối chiếu nguồn

Nguồn trực tiếp:

- `scripts/module/home/view/home_page.gd::_show_ad_reward_restored`
- `scripts/module/result/view/ad_reward_restored_page.gd`
- `scripts/module/ui/queue/ui_popup_queue.gd`
- `assets/cfg/dialog_priority_strategy.json`

Matrix AppScene xác nhận ba normal reward gần nhất mở quota, Hint/Locate pending được gom thành batch, Collect cấp tool và tăng restored-today count, còn Close không cấp tool/quota nhưng vẫn xóa batch đã trình bày đúng nguồn.

## Lỗi lifecycle đã sửa

Lần mở đầu, Collect đặt cả CollectButton và CloseButton thành không tương tác. Unity trước đó chỉ mở lại CollectButton trong `OnShow`, nên cùng popup được pool/reopen thì CloseButton vẫn khóa. `AdRewardRestoredPagePresenter` nay reset CloseButton ở mỗi lần show; ba chu kỳ dùng cùng presenter và không tạo instance trùng.

Sau khi pending queue rỗng, reopen Home chờ đúng `HomePageContract.RewardRestoreDelaySeconds` nhưng không mở popup và queue kết thúc sạch.

## Còn mở

Pixel/animation Home và popup trên hai aspect cùng kiểm tra touch/device vẫn thuộc R12/R17. Production ad SDK/provider là ngoài phạm vi theo quyết định dự án offline.
