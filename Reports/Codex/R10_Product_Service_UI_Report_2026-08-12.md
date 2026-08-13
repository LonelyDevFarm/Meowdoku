# R10/R16 Product Service UI — parity report (2026-08-12)

## Phạm vi

Lát cắt này chuyển các luồng Feedback, Rate Us, Rate Us V2 và Helpshift từ Godot sang Unity. Theo phạm vi thử nghiệm hiện tại, các dịch vụ online, quảng cáo, review store, rating, feedback upload và analytics không cần SDK thật hay backend thật. Unity chỉ cần giữ đúng contract, lifecycle và điểm móc provider để game chạy offline an toàn.

## Đối chiếu nguồn Godot

| Nguồn | Hành vi đã giữ |
|---|---|
| `scripts/common/helpshift_manager.gd` | Android/iOS app key/domain, preheat khi Help từng mở trong cửa sổ 2 ngày, FAQ install/open, unread request và last-open timestamp. |
| `scripts/page/feedback_page.gd` | UIFrameWindow, input trim, Submit chỉ bật khi có nội dung, submit → thanks → close, close tracking và outside focus release. |
| `scripts/page/rate_us_page.gd` | Năm mức sao, tap/drag chọn sao, close/rate result và lifecycle dọn listener/tween. |
| `scripts/page/rate_us_page_v2.gd` | Restyled presentation, auto-select delay 0,3 giây và cùng kết quả rate/close contract. |
| `scripts/page/game_page.gd` + rate config | Gate level 8/15, win-streak 5; sau thắng chặn input theo delay nguồn, >4 mở store review, ≤4 chuyển Feedback rồi tiếp tục Push/queue. |

## Unity implementation

- `Assets/_Project/Scripts/Core/Platform/ProductServiceContracts.cs`: provider boundary, offline provider, Help configuration, rate result và UI consumer contracts.
- `Assets/_Project/Scripts/Core/Platform/ProductServiceRuntime.cs`: state/config gate, Helpshift preheat/unread, Feedback route, Rate Us route, GameWin hand-off và cleanup event.
- `Assets/_Project/Scripts/Gameplay/FeedbackPagePresenter.cs`: presenter source-backed cho form/thanks/close/focus.
- `Assets/_Project/Scripts/Gameplay/RateUsPagePresenter.cs`: presenter cho bản thường và V2 restyle qua serialized flag; `RateUsPagePresenterV2.cs` chỉ còn compatibility type, prefab hiện dùng base presenter để Unity serialize ổn định.
- `Assets/_Project/Editor/ProductServicePrefabInstaller.cs`: sinh/migrate ba prefab bằng Unity API, bảo toàn source assets và binding close button riêng của frame/popup.
- `Assets/_Project/Editor/UIRegistryAssetInstaller.cs` và `Assets/_Project/Editor/AppRuntimeSceneInstaller.cs`: đăng ký page và nối runtime dưới `App/Systems` trong AppScene.
- `Assets/_Project/Scripts/Gameplay/GameWinPagePresenter.cs`: gọi ProductServiceRuntime theo đúng thứ tự delay → Rate Us/Feedback → Push.

## Ranh giới online được chấp nhận

- Mặc định dùng offline/no-op provider; thiếu SDK không làm hỏng startup, save hoặc gameplay.
- Không gửi `uuid`, LUID, AB, country, level metadata hoặc free-form feedback text ra ngoài. Payload Help hiện để rỗng cho đến khi có yêu cầu rõ ràng.
- Không mô phỏng server, quảng cáo live, store-review callback, account/backend hay network attribution. Nếu cần phát hành thật, chỉ cần thay provider adapter mà không đổi presenter/gameplay contract.

## Kiểm tra

- Unity EditMode targeted Product Services: **22 passed, 0 failed**.
- Unity PlayMode targeted Platform: **3 passed, 0 failed**.
- Prefab V2 đã xác nhận dùng `Meowdoku.Gameplay.RateUsPagePresenter` với `restyled` và binding `frameCloseButton`/`rateCloseButton`.
- Chưa yêu cầu người dùng test visual trực tiếp ở lượt này; visual/pixel parity, native device callback và touch/device validation vẫn là mục QA sau.

## Trạng thái

Contract/runtime composition của Product Services đã nối xong cho bản offline thử nghiệm. Phần còn mở chỉ là kiểm thử trực tiếp UI và adapter SDK thật — đều không cần thiết cho mục tiêu hiện tại của dự án.
