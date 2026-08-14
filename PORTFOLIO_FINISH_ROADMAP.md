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
| Regression gần nhất | Đang theo dõi | Unity compile clean; full EditMode `705/708`, ba lỗi còn lại thuộc Bank readable-string và không liên quan thay đổi Streak/Rank; Portfolio Visual PlayMode gần nhất `4/4`; Platform PlayMode mới nhất vẫn là `25/27` vì chưa chạy lại, còn đúng hai regression timing Win/WinToast đã biết. Test Runner PlayMode ngày 2026-08-14 bị đứng sau khi nạp AppScene nên lượt Rank visual mới chưa hoàn tất; không kết luận toàn bộ regression đã sạch. |

## 4. Việc còn lại bắt buộc

### F1 — Visual pass cho luồng chơi chính

Trạng thái: `ACTIVE — mở lại theo nghiệm thu ảnh người dùng ngày 2026-08-14`

- [x] Chạy AppScene từ dữ liệu mới và ghi inventory trực quan cho Splash, Tutorial, Home, Game, Fail và Win.
- [x] Sửa mọi sprite trắng/mất, icon tạm, text tràn và hierarchy/layout sai nhìn thấy trên luồng chính.
- [x] Đối chiếu asset/timing nguồn trước từng thay đổi; chỉ dùng adapter Unity khi định dạng Godot không hỗ trợ trực tiếp.
- [x] Exact source CAT atlas 297 frame đã được import; lifecycle appear/idle đã được nối vào runtime.
- [x] Kiểm tra 1080×1920 và 1080×2400: Header, RuleBar, Board, BottomTools, popup và safe area không cắt/lệch.

#### Khoảng trống nghiệm thu mới

Quy ước đối chiếu ảnh người dùng: **bên trái là Unity, bên phải là bản nguồn**.

- [x] **VA-01 — Home avatar:** avatar mở được Profile bằng luồng input runtime; Portfolio Visual PlayMode đã đi qua tương tác này.
- [ ] **VA-02 — Home meta/event cards:** ba card meta/sự kiện còn thiếu sprite/state nguồn; Daily đang hiện placeholder loading và sự kiện thứ ba đang vắng mặt/bị khóa.
  - [x] Thẻ Chuỗi đã phục hồi nền vàng, Sun, checked badge, count badge bo tròn và mini-entry có shadow/panel nguồn.
- [ ] **VA-03 — Dữ liệu test portfolio:** phải phơi bày toàn bộ tính năng offline quan trọng; tăng level hoặc dựng local fixture xác định nếu unlock gate đang che tính năng, nhưng không làm yếu production domain rules.
- [x] **VA-04 — Gameplay presentation:** Gameplay dùng đúng nền kem đặc từ nguồn `Color(0.969, 0.949, 0.937, 1)` vì Godot source là `ColorRect`, không phải pattern; header/rule/tools đã được scale theo nguồn và có bằng chứng trực quan ở 1080×1920 cùng 1080×2400.
  - [x] Header Gameplay đã dùng full texture nguồn cho Back/Settings; Cat/Life/Daily Timer dùng pill bo tròn bán kính 42; tiêu đề Màn/Điểm và ba thẻ luật đi qua LocalizationCatalog thay vì text tiếng Anh hard-code.
  - [x] Daily layout chuyển đúng profile nguồn: Cat ở trái, Life ở giữa, Timer ở phải; normal layout được phục hồi riêng khi đổi session.
- [x] **VA-05 — RuleBar:** entry pulse chạy theo timing nguồn và kết thúc với glow alpha bằng `0`.

Nghiệm thu: người dùng có thể đi qua luồng chính mà không thấy placeholder trắng hoặc bố cục vỡ; Console 0 error; EditMode composition đạt.

Bằng chứng: ảnh `Temp/PortfolioVisualAudit` ở 1080×1920 và 1080×2400; Portfolio Visual PlayMode `4/4`; Platform EditMode `89/89`; báo cáo `Reports/Codex/R17_Portfolio_Visual_Pass_2026-08-13.md`.

### F2 — Gameplay/Result VFX và audio cảm nhận

Trạng thái: `NEXT`

- [x] Bổ sung CAT star/glow burst bằng pool UGUI từ `et_glow_002` và `et_star_1`, phát tại mốc khoảng 0,116 giây như `cell.tscn`.
- [x] CAT appear giữ kích thước native của từng atlas frame và node scale nguồn `0.5`; frame lớn được phép vượt nhẹ cell 100 px thay vì bị Unity Image ép nhỏ để luôn lọt trong ô.
- [x] Mèo cuối: score flight và life bonus chạy song song như nguồn; bỏ phần delay cộng thừa `0.8–1.45s`, còn gate ba mạng đúng `1.52s` trước result flow.
- [x] Kiểm tra score bubble, multiplier, tool feedback, rule highlight, board enter và completion không bị che/sai sorting.
- [x] Kiểm tra Fail/Revive/Win: life particle, popup transition, CTA timing và cleanup khi restart/next/back.
- [ ] `USER QA`: nghe trực tiếp BGM, tap/mark/cat/wrong/combo/win/fail; ghi nhận clip thiếu, âm chồng hoặc pool không nhả. Việc nghe chủ quan này không tự nó giữ F2 ở `ACTIVE`.
- [x] Không dựng giả Spine nếu static sprite + DOTween đã truyền đạt đúng ý đồ; mọi sai khác được ghi rõ.
- [x] **VA-06 — Rank-after-win:** dùng overlay tối, fixture portfolio xác định gồm năm đối thủ lân cận, hiển thị nhiều hàng kề nhau, animation và cổng `Tap to Continue` đủ rõ để đọc; cached reopen cleanup an toàn.
- [ ] **VA-07 — Win/Fail:** đối chiếu lại với ảnh nguồn về rays/confetti/overlay tối/cách trình bày CAT/timing còn thiếu; không mặc định VFX tự động hiện tại đã hoàn tất về thị giác.

Nghiệm thu: không còn VFX chính bị thiếu rõ ràng, tween không tồn tại sau khi page đóng, audio không phát khi Sound tắt và không có allocation/pool leak dễ thấy.

Bằng chứng hiện tại: CAT burst giữ timing nguồn `0.1164/0.5/1.02` và pool sáu view; board enter giữ đường diagonal bottom-left → top-right cùng ba curve/timing source; `score_encourage` đã đi qua shared GameStart catalog tại `GameplayManager`; score bubble/multiplier/flight, RuleBar pulse và ToolButton pulse đã chạy thật. Timeline Fail bám nguồn theo thứ tự overlay/cat/title/remaining/encourage/CTA, mở khóa button sau `1.5s`, đóng trong `0.1s`, cleanup và reopen an toàn. Timeline Win bám nguồn theo thứ tự ray/title/cat/glow/body/CTA; one-shot dài `0.394s`, ribbon loop `0.535s` và toàn bộ hiệu ứng được cleanup qua pool. DOTween preallocate `512` tweeners/`128` sequences, không có resize warning. `RateUsStarPointerView` đã chuyển sang MonoScript riêng và cả hai prefab RateUs được rebuild qua Unity API, không còn missing script. Cả `66` clip trong catalog decode thành sample hữu hạn, không im lặng; play-count của luồng chính `BoardEnter/MarkCat/Wrong/Fail/AllCleared/Win` đều đạt. Regression hiện tại: compile clean; Platform EditMode `89/89`; Portfolio Visual PlayMode `4/4`; Platform PlayMode giữ kết quả mới nhất `25/27` và chưa chạy lại, với đúng hai lỗi Win/WinToast đã biết; ảnh `05_Fail`, `06_Win`, `Temp/PortfolioRankAudit/20_RankChange.png` và `Temp/PortfolioRankAudit/23_RankPage.png`; báo cáo `Reports/Codex/R17_Gameplay_Presentation_Closure_2026-08-14.md` và `Reports/Codex/R17_Rank_Visual_Closure_2026-08-14.md`.

Bổ sung regression 2026-08-14: native-frame CAT/source scale `0.5` và last-cat parallel score/life timeline đều có contract test đạt; full EditMode `702/705` với ba lỗi Bank readable-string không liên quan; Portfolio Visual PlayMode `4/4`. Chi tiết ở `Reports/Codex/R17_Last_Cat_Timing_And_Scale_2026-08-14.md`.

Nghe trực tiếp vẫn là `USER QA`, nhưng không phải lý do tự thân để giữ F2 ở `ACTIVE`; F2 vẫn là `NEXT` vì khoảng trống hình ảnh VA-07 còn tồn tại.

### F3 — Visual pass cho các màn hình portfolio phụ

Trạng thái: `NEXT`

- [x] **VA-08 — Streak page:** dùng `bg_9grid`, Sun, best-streak frame, checked dot có dấu tích trắng dựng từ `et_mask_008`, day node tròn, reward chest và back icon từ nguồn; nút Back giữ hit-area `100×100`, nền `normal_btn_bg` trắng `140×140` và icon `54×46` theo scene gốc. Không thêm CAT vào main page vì scene Godot gốc không dùng CAT ở trạng thái này.
- [x] **VA-09 — Rank page:** đã render podium/gifts/nhiều row/CTA bằng local robot fixture; cả hai viewport Rank dùng `RectMask2D`. Header được khôi phục không gian nguồn rộng `1080`, tiêu đề/cá đếm giờ căn giữa đúng; Back/Info dùng toàn bộ texture nguồn `100×100` nên không còn icon bị crop/chồng sai, phía sau là nền tròn trắng. Ba bục, medal, avatar, tên, điểm và quà dùng tọa độ cuối animation của scene Godot; nội dung mỗi row dùng đúng khung `968×180`. Floating self row thu avatar còn `146`, căn giữa với inset `7`, có nền occluder bo góc và không bật shadow khi ghim đáy nên không còn lộ danh sách phía sau. CTA dùng capsule `btn_primary` thay vì kéo méo sprite tròn. How-to Rank đã chuyển từ cột thẳng sang bố cục zigzag trái/phải, gồm grid, cat/fish, rank list, reward và ba mũi tên theo tọa độ nguồn.
- [ ] **Settings reliability:** sau khi thao tác gameplay lặp lại, Settings phải luôn mở được content thay vì chỉ đổi nền dim; đối chiếu và sửa typography khi thực tế, nhưng ưu tiên thấp hơn VA-01..09.
  - [x] Sửa lifecycle Start -> Game -> Back nhanh: không chờ encrypted snapshot fsync trên main thread, Home tự khôi phục trạng thái ba nút khi trở lại top window, release guard không còn phụ thuộc WaitForEndOfFrame của Game View.
  - [ ] USER QA: vào màn thường rồi Back ngay khi có thể; tại Home lần lượt mở Start, Settings và Avatar. Lặp lại sau khi đã đánh vài ô để xác nhận resume vẫn được lưu và không có nút nào bị khóa.

Bank là trình duyệt puzzle dành cho developer; chỉ cần UI portfolio sạch, dễ đọc, không cần độ nổi bật như bản nguồn.

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
| UI chức năng offline | **88–92%** |
| Visual/VFX/audio trình bày | **71–78%** |
| QA/build/portfolio package | **35–45%** |
| **Tổng theo phạm vi portfolio mới** | **~83–87%** |

Phase visual đã được mở lại sau nghiệm thu đối chiếu ảnh song song của người dùng ngày 2026-08-14. Phần trăm chỉ cập nhật theo bằng chứng runtime/nghiệm thu; không tăng vì thêm code chưa được prefab/runtime sử dụng.

## 8. USER QA

- [x] Đã review DOCX nghiệm thu ngày 2026-08-14. Mapping cặp ảnh: `1/2 Home`, `3/4 Streak`, `5/6 Rank`, `7/8 Gameplay`, `9/10 Rank-after-win`, `11/12 Win`; **bên trái là Unity, bên phải là bản nguồn**.
- [ ] Rank manual QA đã sẵn sàng: từ Home với level `>=21` mở Rank; xác nhận Back/Info có nền tròn trắng, CTA là capsule bo tròn, tiêu đề/countdown nằm giữa, avatar khớp từng bục Top 1/2/3, tên/điểm/quà ở đúng dưới avatar và mọi thành phần trong row nằm giữa theo chiều dọc. Mở nút Info và xác nhận hướng dẫn chạy zigzag trái/phải; hàng người chơi ghim dưới cùng không để avatar tràn khỏi frame. Thắng một ván khi Rank tham gia; xác nhận RankChange hiển thị nhiều hàng lân cận, chờ `Tap to Continue`, sau đó danh sách Rank page vẫn có dữ liệu.
- [ ] Last-cat visual QA: tìm mèo cuối; CAT phải hiện ngay, phóng lớn nhẹ vượt ô trong đoạn appear rồi về idle; score/life VFX bắt đầu song song, không có khoảng đứng hình cộng thừa. Khoảng `1.2–1.5s` trình bày trước Win vẫn là chủ ý.
- [ ] Streak visual QA: ở Home xác nhận card Chuỗi có nền vàng + mặt trời + dấu check; mở Chuỗi và xác nhận mặt trời lớn, sáu day node đều tròn, rương ở ngày cuối, nút Back có nền tròn trắng/icon giữa và hoạt động.
- [ ] Gameplay header QA: mở một màn thường và Daily; xác nhận Back/Settings là nút tròn đầy đủ, nền Cat/Life/Timer là pill bo tròn, Daily không chồng ba khối, và locale tiếng Việt hiển thị Màn/Điểm cùng ba câu luật tiếng Việt.

Đây là các kiểm tra mà đánh giá của con người/thiết bị nhanh và chính xác hơn, hoặc Codex không thể trực tiếp cảm nhận kết quả. Codex chỉ hỏi khi phase liên quan đã sẵn sàng.

- [ ] Nghe audio trong AppScene: bật Sound/People, kiểm tra board enter, X mark/unmark, cat, wrong, combo/voice khi có, Fail và Win; không méo tiếng, chồng âm xấu hoặc nhảy âm lượng khó chịu. Sau đó tắt Sound và xác nhận SFX im lặng. BGM chủ ý hard-off để khớp nguồn.
- [ ] Touch/thiết bị thật: tap, swipe, double-tap, rapid drag; kiểm tra vibration bật/tắt trên Android nếu có Android build.
- [ ] Windows human smoke cuối: dữ liệu mới, đi Tutorial → Home → Main → Fail → Restart/Revive → Win → Next, resume giữa ván; chỉ báo lỗi nhìn thấy/nghe thấy/input.
- [ ] Visual preference cuối: duyệt hoặc ghi chú sprite/static-Spine fallback nào còn quá thô cho video CV.
