# R9 Gameplay Feedback Contract Test Report

- Date: 2026-08-09
- Unity: 6000.3.19f1
- Result: **228 passed, 0 failed**

## Scope

- Thêm typed feedback contract cho Correct Cat, Wrong Guess và từng bước Life Bonus.
- Payload giữ position/source, combo, successful-cat count, score trước/sau, base/display/total gain, multiplier hiện tại/trước đó, skill bonus, deduction, lives, rule và conflicting cats.
- `GameplayManager` phản chiếu board trước, phát feedback theo thứ tự nguồn, rồi mới settle Win.
- Correct Cat từ User, Locate, Hint và AutoComplete đều phát đúng source; mark/prefill/restore không tạo score feedback.
- AutoComplete dùng cell rank thật cho skill-score; nguồn chỉ loại skill bonus với Locate/Hint.
- Life Bonus 1/2/3 mạng tạo chuỗi `100`; `100,100`; `100,100,200`, cập nhật final score trước aggregate Win.
- Khóa fly delay contract hiện có trong nguồn: 0,8 s thường; 1,367 s multiplier scroll; 1,45 s multiplier Appear4.

## Source findings

- Godot kết nối handler combo trước handler score-encourage. Combo tăng trước; score-encourage dùng `_se_count` riêng sau đó.
- Dù `score_encourage` mặc định tắt, đường legacy vẫn cộng 600, 680, 760… và hiện combo text từ combo 3.
- Score bubble khi multiplier > 1 hiển thị base gain; skill bonus được hiển thị riêng và không nhập vào `DisplayGain`.
- Wrong Guess reset cả combo và `_se_count`, rồi mới trừ điểm nếu đúng variant deduction.
- Life Bonus thuộc win sequence và chạy từng heart trước khi `on_level_won`/Win page nhận final score.

## Unity adapter boundary

Nguồn chỉ cộng Life Bonus khi `ComboFeedbackView` tồn tại. Unity đặt phép cộng vào session domain để final score không phụ thuộc view có được dựng hay không; production Godot luôn có view này nên kết quả chơi không đổi. Event vẫn giữ đúng thứ tự từng heart.

Presenter/tween/audio chưa được tạo. Hiện event được phát đồng bộ trước Win settlement; bước visual sau phải thêm completion gate để giữ khoảng chờ 0,3 s và fly/arrival timing của nguồn trước khi mở Result.

## Verification

- Legacy combo payload: 600 → 680 → 760, combo text từ lần 3.
- Multiplier payload: lần 3 có base/display 600, multiplier 1,5, previous 1,0, total 900 và delay 1,45 s.
- Deduction payload: score 600 → 500, lives 3 → 2, rule/conflict giữ nguyên session result.
- AutoComplete với Life Bonus: 4 correct feedback + 3 heart events, final score 3280.
