# Báo cáo Đặc tả & Audit nguồn: R16 Feedback / Rate Us / Helpshift

**Dự án đối chiếu:** `D:\Projects\_GameExtract\Main_Meokdoku`
**Mục tiêu:** Audit toàn bộ luồng Rate Us, Feedback và Helpshift để chuẩn bị port sang Unity.

---

## 1. FeedbackPage & Call Sites
- **File Implementation:** `scripts/module/feedback/view/feedback_page.gd`
- **Hành vi (Behavior):** Là một UI Frame chứa `TextEdit`. Khi người dùng bấm Submit, nội dung text được gửi qua hệ thống Analytics `Tracker.track_btn_click(Tracker.Btn.SUBMIT, self, {"feedback_record": content})`. Sau đó, hiển thị Toast thông báo cảm ơn (`FEEDBACK_TOAST_THANKS_TITLE`). **Không có code gửi API feedback trực tiếp nào khác** ngoài Tracker.
- **Call Sites:**
  - `scripts/module/result/view/game_win_page.gd` (Dòng 331): Hiển thị dưới dạng Dialog (`"as_dlg": true`) nếu người dùng Rate Us <= 4 sao.
  - `scripts/module/ui/panel/launcher.gd` & `cheat_commands.gd`: Các nút mở debug/tester.
  - `scripts/module/home/view/home_page.gd` (Dòng 786): Được tham chiếu pre-load.

## 2. RateUsPage, RateUsPageV2 & Call Sites
- **File Implementation:** 
  - `scripts/module/rate_us/view/rate_us_page.gd` (V1 - Old UI)
  - `scripts/module/rate_us/view/rate_us_page_v2.gd` (V2 - New UI, kế thừa V1, đổi animation thành `GenericPopupV2` và có delay đổ sao lúc mở).
- **Hành vi (Behavior):** Màn hình có 5 ngôi sao (texture sáng/tối). Tracking sự kiện `Tracker.Btn.RATE_US` với params `{"rate_star": _selected_stars}`. Khi đóng/submit, trả về Dictionary `{"star_count": X, "is_submitted": bool}` cho caller xử lý.
- **Call Sites:**
  - `scripts/module/result/view/game_win_page.gd` (Dòng 322): Điểm gọi duy nhất cho user thực tế. 

## 3. InAppReviewManager
- **File Implementation:** `scripts/module/common/in_app_review_manager.gd`
- **Hành vi (Behavior):**
  - OS iOS: Gọi `UniKitManager.request_store_review()`.
  - OS Android: Tìm Singleton Engine `InAppReviewPlugin` và gọi `requestReview()`.
- **Call Sites:**
  - `scripts/module/result/view/game_win_page.gd` (Dòng 329): Chỉ được gọi khi user submit RateUs với số sao `> 4`.

## 4. HelpshiftManager
- **File Implementation:** `scripts/common/helpshift_manager.gd`
- **Khởi tạo & Cấu hình:**
  - Định nghĩa sẵn `ANDROID_APP_ID`, `IOS_PLATFORM_ID`, `IOS_API_KEY` và `DOMAIN`.
  - Sử dụng plugin native `HelpshiftPlugin` thông qua `Engine.get_singleton()`. Chú ý: Code cài đặt (`_install`) hiện tại nhánh iOS đang để trống (`pass`), chỉ implement `install` trên Android.
- **API Chính:**
  - `preheat()`: Chỉ khởi tạo ngầm và fetch unread count nếu `_now() - GameState.get_help_last_open_time() <= 2 * 86400` (48 giờ kể từ lần mở FAQ cuối).
  - `open_faq()`: Cập nhật `help_last_open_time = now`. Gọi `plugin.showFAQs("ALWAYS", metadata, cifs)` và request cập nhật lại unread count.
  - `request_unread()`: Xin số lượng tin nhắn chưa đọc từ plugin. 
- **Metadata (`_build_metadata`, `_build_cifs`):** Lấy rất chi tiết thông tin thiết bị/gameplay từ `UniKitManager` (uuid, country, ab_dyeing_tag, media_source) và `GameState` (current_level, active_days, tool_count, install_version) đóng gói thành JSON đẩy lên Helpshift.
- **Red Dot (Unread Count):** Lắng nghe signal `unread_message_count` từ plugin, đẩy qua `RedDotCenter.set_count("helpshift_unread", count)`.
- **Call Sites:**
  - Mở FAQ: `scripts/module/setting/view/setting_page.gd` (Dòng 596).
  - Request Unread: `setting_page.gd`, `home_page.gd`, `base_game_page.gd` và `NOTIFICATION_APPLICATION_FOCUS_IN` (khi app resume).

## 5. AB Configs Liên quan
- **RateUsPopConfig** (`rate_us_pop_config.gd`):
  - Quyết định điều kiện popup Rate Us. Có các values: `0` (lv >= 8), `1` (lv >= 15), `2` (home sau win), `3` (lv >= 15 VÀ win streak >= 5).
  - *Lưu ý source thực tế:* Hàm `is_eligible_at_game_win` chỉ implement check logic cho Value `0` và `3`. Values `1` và `2` không được xử lý (trả về false).
- **RateUsPopUiConfig** (`rate_us_pop_ui_config.gd`): Value `0` (Old UI), `1` (New UI).
- **GuideFeedbackConfig** & **ErrorFeedbackConfig**: Đây là các config liên quan đến phản hồi UX/UI In-game (Tutorial guide style và animation báo lỗi khi điền sai số), hoàn toàn không liên quan đến hệ thống User Feedback/Rate Us app này.

## 6. Tracker Event / Dlg / Button
- Feedback: `Btn.SUBMIT` (kèm `feedback_record`), `Scr.FEEDBACK`, `Dlg.FEEDBACK`.
- Rate Us: `Btn.RATE_US` (kèm `rate_star`), `Dlg.RATE`.
- Nút Close: `Btn.CLOSE`.

## 7. Điều kiện hiển thị Rate Us (Game Win Page)
- **Eligibility (`_is_rate_us_eligible`):** 
  `ABTestManager.rate_us_pop.is_eligible_at_game_win()` == TRUE 
  VÀ `GameState.has_shown_rate_us()` == FALSE 
  VÀ `UniKitManager.is_online()` == TRUE.
- **Flow (`_show_rate_us`):**
  1. Ẩn nút "Next Level". Block input `APPEAR_DURATION`.
  2. Ghi nhận đã show: `GameState.mark_rate_us_shown()` (cờ này chặn vĩnh viễn popup hiện lại lần sau).
  3. Mở RateUs Page (đợi user tương tác).
  4. Trả về sao. Nếu `> 4`: Gửi request native In-App Review. Nếu `<= 4`: Mở Dialog FeedbackPage.

## 8. Settings → Feedback/FAQ Flow
- Tại `setting_page.gd`, không có luồng độc lập nào để mở FeedbackPage. Nút bấm duy nhất là nút FAQ, gọi thẳng tới `HelpshiftManager.open_faq()`.
- Feedback form chỉ hiện ra cưỡng ép khi Rate Us ở Game Win <= 4 sao.

## 9. Persistence (GameState Fields)
- `_has_shown_rate_us` (bool): Cờ đảm bảo popup Rate Us chỉ hiện đúng 1 lần trong đời (lưu trữ tiến trình save file).
- `_help_last_open_time` (int, unix timestamp): Thời gian cuối mở FAQ. Dùng cho `HelpshiftManager.preheat()` để giữ unread check trong vòng 48 giờ.

---

### TỔNG HỢP NỘI DUNG CUỐI BÁO CÁO

**1. Source files liên quan:**
- `scripts/module/feedback/view/feedback_page.gd`
- `scripts/module/rate_us/view/rate_us_page.gd` / `rate_us_page_v2.gd`
- `scripts/module/result/view/game_win_page.gd`
- `scripts/module/common/in_app_review_manager.gd`
- `scripts/common/helpshift_manager.gd`
- `scripts/module/setting/view/setting_page.gd`

**2. Call graph:**
- `Game Win` -> Check Eligibility -> Hiển thị `RateUsPage/V2`
- `RateUsPage/V2` -> Trả về N sao -> (Nếu > 4) -> `InAppReviewManager.request_review()` -> Native Plugin
- `RateUsPage/V2` -> Trả về N sao -> (Nếu <= 4) -> Hiển thị `FeedbackPage` -> User Submit -> Tracker Ghi nhận Analytics.
- `Settings` -> Click FAQ -> `HelpshiftManager.open_faq()` -> Native Helpshift Plugin.

**3. State/config dependencies:**
- Phụ thuộc `RateUsPopConfig` (gate level), `RateUsPopUiConfig` (UI version).
- Phụ thuộc trạng thái mạng `UniKitManager.is_online()`.
- Trạng thái lưu: `GameState.has_shown_rate_us()`, `GameState.get_help_last_open_time()`.

**4. Exact observed behavior:**
- Feedback text KHÔNG lưu database hay gửi email, chỉ bắn sự kiện Analytics `SUBMIT` chứa nội dung text.
- iOS In-App Review gọi trực tiếp native framework của `UniKitManager`, Android gọi qua plugin Godot riêng lẻ `InAppReviewPlugin`.

**5. Native/plugin dependencies:**
- Singleton: `InAppReviewPlugin` (Android)
- Singleton: `HelpshiftPlugin` (Android)
- Bridge: `UniKitManager` (iOS Store Review)

**6. UNKNOWN / Điểm cần kiểm tra thêm:**
- **UNKNOWN:** `HelpshiftManager._install()` không làm gì cả trên iOS (nhánh rẽ nhánh đang là `pass`). Cần kiểm tra xem dự án iOS có thực sự tích hợp Helpshift theo cách Native Unity hay SDK riêng không, vì Godot source không tự install Helpshift cho iOS.
- **UNKNOWN:** `RateUsPopConfig` cấu hình Value 1 (lv 15) và Value 2 (home sau win) chưa được implement trong code (source code không bắt case này, sẽ auto return false). Có thể do tàn dư config cũ. Cần confirm với Product xem Unity có phải port các rule bị thiếu này không.
