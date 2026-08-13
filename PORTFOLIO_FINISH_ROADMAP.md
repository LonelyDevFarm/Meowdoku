# Meowdoku — Kế hoạch hoàn thiện bản Portfolio

> Bảng điều hành chính từ 2026-08-13.  
> Roadmap port chi tiết cũ được giữ tại `PORTING_ROADMAP.md` làm lịch sử kỹ thuật, không dùng để quyết định việc tiếp theo.  
> Nguồn đối chiếu: `D:\Projects\_GameExtract\Main_Meokdoku`.

## 1. Mục tiêu hoàn thành

Tạo một bản Unity có thể build và chơi trọn luồng để đưa vào portfolio:

`Startup → Tutorial/Home → Main Game → Win/Fail/Revive → Next/Home`

Các màn hình offline quan trọng phải mở được, có bố cục và asset hợp lý, không có ô trắng do thiếu sprite, không có lỗi Console, save/load hoạt động và có build Windows dùng để trình diễn.

Đây không còn là dự án thương mại. Logic, trải nghiệm chơi, VFX quan trọng và asset nguồn vẫn phải được đối chiếu cẩn thận; pixel tuyệt đối, SDK/native và dịch vụ online thật không phải điều kiện hoàn thành.

## 2. Cách đọc bảng

| Trạng thái | Ý nghĩa |
|---|---|
| `DONE` | Đã nối vào luồng thật và có bằng chứng compile/test hoặc nghiệm thu |
| `ACTIVE` | Công việc Codex đang thực hiện |
| `NEXT` | Bắt buộc, sẽ làm sau mục ACTIVE |
| `OPTIONAL` | Chỉ làm nếu còn thời gian hoặc giúp portfolio rõ rệt |
| `OUT` | Ngoài phạm vi, không được tính là việc còn thiếu |

Mỗi lượt chỉ có tối đa một nhóm `ACTIVE`. Một nhóm chỉ chuyển sang `DONE` khi đạt tiêu chí nghiệm thu ghi ngay trong bảng.

## 3. Trạng thái nền đã khóa

| Khối | Trạng thái | Bằng chứng hiện tại |
|---|---|---|
| Core, level, save/load, input | `DONE` | Logic chính, recent puzzle, pre-cat, palette, input tap/swipe/double-tap, Undo và lifecycle đã có regression |
| AppScene và navigation | `DONE` | Startup, Tutorial/Home, cached page, Back/mask/input guard và các route chính đã có PlayMode |
| Main Game | `DONE` | Board, score, life, tools, Win/Fail/Revive/Next và Bank/Daily entry chạy trong AppScene |
| UI chức năng offline | `DONE` | Home, Settings, Language, HTP, Bank, Daily, Streak, Award, Profile và Rank đã có presenter/prefab/route |
| Dịch vụ thương mại | `DONE` theo phạm vi offline | Ads/IAP/rating/feedback/online dùng provider no-op an toàn; không tích hợp SDK thật |
| Reset dữ liệu test | `DONE` | Menu `Meowdoku/Test/Reset All Local Data`, có kiểm tra đường dẫn và hộp xác nhận |
| Tutorial VFX | `DONE` | Fireworks/IQ burst dùng line/ribbon/star/glow nguồn và pool cố định |
| Life VFX | `DONE` | Ba LifeSlot có ReviveGlow, 6 fish + 6 glow; mất/hồi mạng dùng timing nguồn |
| How-to-play Back icon | `DONE` | Adapter UGUI mesh cho SVG nguồn, không phụ thuộc SVG package |
| Regression gần nhất | `DONE` | Unity compile sạch; Platform EditMode `54/54`, 0 failed ngày 2026-08-13 |

## 4. Việc còn lại bắt buộc

### F1 — Visual pass cho luồng chơi chính

Trạng thái: `ACTIVE`

- [ ] Chạy AppScene từ dữ liệu mới và ghi inventory trực quan cho Splash, Tutorial, Home, Game, Fail và Win.
- [ ] Sửa mọi sprite trắng/mất, icon tạm, text tràn và hierarchy/layout sai nhìn thấy trên luồng chính.
- [ ] Đối chiếu asset/timing nguồn trước từng thay đổi; chỉ dùng adapter Unity khi định dạng Godot không hỗ trợ trực tiếp.
- [ ] Hoàn thiện CAT appear bằng phương án ổn định. Hiện static sprite đã có fade và bounce nền theo timing nguồn; atlas 297 frame là `OPTIONAL` vì importer Unity 6 không nhận metadata nguồn an toàn.
- [ ] Kiểm tra 1080×1920 và 1080×2400: Header, RuleBar, Board, BottomTools, popup và safe area không cắt/lệch.

Nghiệm thu: người dùng có thể đi qua luồng chính mà không thấy placeholder trắng hoặc bố cục vỡ; Console 0 error; EditMode composition đạt.

### F2 — Gameplay/Result VFX và audio cảm nhận

Trạng thái: `NEXT`

- [ ] Bổ sung CAT star/glow burst bằng pool UGUI từ `et_glow_002` và `et_star_1`, phát tại mốc khoảng 0,116 giây như `cell.tscn`.
- [ ] Kiểm tra score bubble, multiplier, tool feedback, rule highlight, board enter và completion không bị che/sai sorting.
- [ ] Kiểm tra Fail/Revive/Win: life particle, popup transition, CTA timing và cleanup khi restart/next/back.
- [ ] Nghe trực tiếp BGM, tap/mark/cat/wrong/combo/win/fail; sửa clip thiếu, âm chồng hoặc pool không nhả.
- [ ] Không dựng giả Spine nếu static sprite + DOTween đã truyền đạt đúng ý đồ; mọi sai khác được ghi rõ.

Nghiệm thu: không còn VFX chính bị thiếu rõ ràng, tween không tồn tại sau khi page đóng, audio không phát khi Sound tắt và không có allocation/pool leak dễ thấy.

### F3 — Visual pass cho các màn hình portfolio phụ

Trạng thái: `NEXT`

- [ ] Home/Settings/Language/How-to-play/Bank.
- [ ] Daily/Streak/Award.
- [ ] Profile/Rank và các popup liên quan.
- [ ] Thay placeholder nhìn thấy được bằng sprite nguồn; thống nhất font, màu, button state và cây hierarchy theo chức năng.
- [ ] Ưu tiên màn hình có thể xuất hiện trong video/CV; màn hình dịch vụ no-op chỉ cần sạch và không chặn luồng.

Nghiệm thu: mỗi page mở/đóng/reopen được từ AppScene, không mất binding, không text debug, không khối trắng ngoài nền màu chủ ý.

### F4 — QA và build trình diễn

Trạng thái: `NEXT`

- [ ] Chạy full EditMode và Platform PlayMode sau khi kết thúc visual pass.
- [ ] Smoke test dữ liệu mới: Tutorial → Home → Main Win → Next → Fail → Revive/Restart → Home.
- [ ] Smoke test save/resume: thoát giữa ván, mở lại, reset data từ menu.
- [ ] Kiểm tra build settings chỉ dùng `AppScene` làm entry; legacy Loading/Home/Gameplay scene không thuộc production flow.
- [ ] Tạo Windows development build sạch và chạy thử ngoài Editor.
- [ ] Android build là `OPTIONAL`; chỉ làm nếu toolchain hiện có hoạt động, không biến thành blocker.

Nghiệm thu: build Windows mở được, chơi trọn luồng, save hoạt động, không crash và không có lỗi nghiêm trọng trong log.

### F5 — Gói portfolio và bàn giao

Trạng thái: `NEXT`

- [ ] README ngắn: mục tiêu port Godot→Unity, kiến trúc, hệ thống nổi bật, cách chạy và phạm vi offline.
- [ ] Danh sách adapter đáng nói: UI manager/cache, save dual-slot, input, UGUI VFX pool, SVG chevron, Spine fallback.
- [ ] Ảnh hoặc video ngắn của Startup/Home/Gameplay/Win/Fail và một màn hình meta.
- [ ] Ghi Known Differences cuối cùng, chỉ gồm sai khác đã chủ động chấp nhận.
- [ ] Chốt commit/build label dùng cho CV.

Nghiệm thu: người xem repository hiểu dự án làm gì, mở ở đâu và thấy được kỹ năng Unity mà không cần đọc roadmap kỹ thuật cũ.

## 5. Ngoài phạm vi — không làm tiếp

| Hạng mục | Quyết định |
|---|---|
| SDK quảng cáo/IAP/store review/feedback thật | `OUT` — giữ provider offline/no-op |
| Backend, login thật, cloud sync production | `OUT` |
| Remote A/B production | `OUT` — dùng default/source fixture |
| Pixel-diff tuyệt đối mọi màn hình | `OUT` — chỉ yêu cầu visual hợp lý và bám nguồn |
| Spine Unity runtime | `OUT` — dùng static sprite/DOTween khi cần |
| iOS signing, ATT, push thật | `OUT` |
| Soak thủ công toàn bộ level 1–250 | `OPTIONAL` — automated fixture và smoke representative đủ cho portfolio |
| Phục hồi các scene prototype Loading/Home/Gameplay | `OUT` — production dùng AppScene + prefab page |
| Xóa prototype/asset thừa chỉ để “sạch” | `OPTIONAL` — chỉ xóa khi chứng minh không có reference và giúp build |

## 6. Thứ tự thực hiện cố định

1. Hoàn thành `F1`.
2. Hoàn thành `F2`.
3. Hoàn thành `F3`.
4. Khóa build ở `F4`.
5. Viết tài liệu/trình bày ở `F5`.

Không quay lại mở rộng SDK/online hoặc các parity chi tiết đã bị loại phạm vi, trừ khi chúng gây lỗi trực tiếp cho luồng offline.

## 7. Tiến độ gọn

| Phạm vi còn dùng để điều hành | Ước lượng |
|---|---:|
| Logic và luồng chơi bắt buộc | **94–97%** |
| UI chức năng offline | **90–94%** |
| Visual/VFX/audio trình bày | **64–72%** |
| QA/build/portfolio package | **35–45%** |
| **Tổng theo phạm vi portfolio mới** | **~82–86%** |

Phần trăm chỉ cập nhật sau khi một phase chuyển hẳn sang `DONE`; không tăng vì thêm code chưa được prefab/runtime sử dụng.
