# Báo cáo Audit Unity (Dự án Meowdoku) cho hạng mục R16 Feedback / Rate Us / Helpshift

Dựa trên mã nguồn Unity hiện tại, dưới đây là tình trạng so khớp với bản gốc Godot.

## 1. UI Registry & Tracker Constants
- `UiName.Feedback`, `UiName.RateUs`, `UiName.RateUsV2`
  - **Tình trạng:** Đã tồn tại (Already implemented).
  - **Bằng chứng:** `Assets\_Project\Scripts\Core\UI\UIContracts.cs` → `enum UiName`, dòng 19-21.
- `TrackerCatalog` (Btn.SUBMIT, Btn.RATE_US, Dlg.RATE, Scr.FEEDBACK, v.v.)
  - **Tình trạng:** Đã tồn tại (Already implemented).
  - **Bằng chứng:** `Assets\_Project\Scripts\Core\Tracking\TrackerService.cs` → `TrackerCatalog`, dòng 6, 8, 11 (Screen), dòng 104, 105 (Dialog), dòng 197, 202 (Button). Các keys `feedback_scr`, `rate_dlg`, `feedback_dlg`, `submit`, `rate_us`, `feedback_record` đều đã được định nghĩa.

## 2. Game Win Flow (Rate Us Trigger)
- `GameWinPagePresenter`
  - **Tình trạng:** Lớp Presenter đã được tạo nhưng **chưa có** logic gọi RateUs.
  - **Bằng chứng:** `Assets\_Project\Scripts\Gameplay\GameWinPagePresenter.cs` → `ContinueAfterMetaFlows()`, dòng 559. Flow sau Win hiện tại chỉ xử lý DailyMeta và RankActivity (`rank?.GetPendingReward()`), bỏ qua hoàn toàn bước Rate Us / Feedback.

## 3. Game State Persistence
- Cờ `has_shown_rate_us` / `mark_rate_us_shown`
  - **Tình trạng:** Chưa tồn tại (Missing).
  - **Bằng chứng:** Trong `Assets\_Project\Scripts\Core\GameStateData.cs` và `GameStateService.cs` không có trường nào liên quan đến Rate Us. Chỉ có `has_shown_att_guide`.

## 4. Config (AB Test / AB Config)
- `RateUsPopConfig` và `RateUsPopUiConfig`
  - **Tình trạng:** Chưa tồn tại (Missing).
  - **Bằng chứng:** Không tìm thấy script nào định nghĩa 2 config này trong toàn bộ `Assets\_Project`.

## 5. Settings Feedback/FAQ Button
- **Tình trạng:** Nút bấm đã tồn tại, layout có sẵn và logic click đã được bind vào một external service interface.
  - **Bằng chứng:** `Assets\_Project\Scripts\Gameplay\SettingsPagePresenter.cs` → `OpenFeedback()`, dòng 489. Logic kiểm tra `_externalServices.IsOnline`. Nếu có mạng, gọi `_onFeedback?.Invoke()` (fallback ra ngoài scene) hoặc `_externalServices.OpenFeedbackFaq()`.

## 6. External Boundaries & Providers
- **Online State (UniKitManager.is_online):**
  - **Tình trạng:** Đã có interface tương đương (Existing reusable interfaces).
  - **Bằng chứng:** `Assets\_Project\Scripts\Core\UI\SettingsExternalServices.cs` → `ISettingsExternalServices.IsOnline`, dòng 12.
- **Helpshift / FAQ Provider:**
  - **Tình trạng:** Có stub trống, chưa có SDK (Partially implemented / Existing reusable interfaces).
  - **Bằng chứng:** `ISettingsExternalServices.OpenFeedbackFaq()` (dòng 14). `OfflineSettingsExternalServices` đang implement rỗng hàm này.
- **In-App Review / Store Review Provider:**
  - **Tình trạng:** Chưa tồn tại (Missing).
  - **Bằng chứng:** Không tìm thấy bất kỳ interface `IStoreReview` hay plugin `InAppReview` nào.

## 7. Tests hiện tại
- **Tình trạng:** (Existing tests) Có các test cover framework như `SettingsPageContractTests.cs`, `TrackingCoreTests.cs`, `PrimaryNavigationPlayModeTests.cs`. Tuy nhiên, chưa có Unit Test hay PlayMode Test nào chuyên biệt cho popup Rate Us hoặc GameWin flow kết hợp Rate Us.

---

## TỔNG KẾT HẠNG MỤC (STATUS SUMMARY)

### Z. Mismatch với Godot source đã khóa
- Unity chia tách thành `ISettingsExternalServices` thay vì gọi trực tiếp tĩnh (Singleton) như `HelpshiftManager` hay `UniKitManager` trong Godot. Nút Feedback ở Unity được bọc qua injection, tốt cho testability nhưng khác biệt cơ bản về luồng gọi trực tiếp.

### A. Already implemented
- Định nghĩa enum/string của Tracker (Btn, Dlg, Scr).
- Định nghĩa enum `UiName.RateUs`, `UiName.Feedback`.
- UI Layout cơ bản cho nút Feedback trong `SettingsPagePresenter`.

### B. Partially implemented & Existing reusable interfaces
- Boundary cho Online check, FAQ (thông qua `ISettingsExternalServices.IsOnline` và `OpenFeedbackFaq()`).
- Flow Game Win đã dựng cấu trúc (thông qua `ContinueAfterMetaFlows()`), chỉ chờ nhúng luồng gọi Rate Us vào.

### C. Missing
- `GameState.has_shown_rate_us` và các method lưu trữ.
- Lớp model xử lý AB Config `RateUsPopConfig` / `RateUsPopUiConfig`.
- Hai prefab + hai presenter tương ứng cho màn hình `UiName.Feedback` và `UiName.RateUs(V2)`.
- Wrapper / Service tương tác với Store Review API (InAppReviewPlugin / SKStoreReviewController).
- Implementation gọi SDK Helpshift thật cho Android/iOS.

STATUS: COMPLETE
