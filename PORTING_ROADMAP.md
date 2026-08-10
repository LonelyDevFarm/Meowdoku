# Meowdoku Godot → Unity: Roadmap port chính thức

> Cập nhật gần nhất: 2026-08-09  
> Nguồn chuẩn: `D:\Projects\_GameExtract\Main_Meokdoku`  
> Dự án đích: `D:\Projects\Meowdoku`

## 1. Mục tiêu và nguyên tắc

Mục tiêu là dựng lại dự án gốc trong Unity với hành vi, dữ liệu, bố cục, âm thanh và nội dung giống nguồn Godot nhất có thể.

Quy tắc làm việc:

- Mã, scene, tài nguyên và cấu hình Godot là **nguồn sự thật**.
- Mọi code, prefab, scene và adapter được tạo **trước khi roadmap này ra đời** đều là prototype chưa được tin cậy, kể cả khi đang chạy được. Khi chạm tới module tương ứng phải đối chiếu lại nguồn Godot và port/chuyển thể lại; không giữ hành vi cũ chỉ để tương thích với prototype.
- Có thể chuyển đổi trực tiếp thì port 1:1; không tự thiết kế lại luật hoặc luồng.
- Chỉ tạo giải pháp Unity riêng khi Godot dùng API/cấu trúc Unity không hỗ trợ trực tiếp.
- Mọi phần Unity riêng phải được ghi trong **Sổ chuyển thể** ở cuối tài liệu.
- Không tính một mục là hoàn thành chỉ vì đã có file. Mục chỉ hoàn thành khi đủ logic, nối vào luồng thật và qua kiểm thử tương ứng.
- Ưu tiên hành vi và dữ liệu trước, hình ảnh/VFX sau khi kích thước và luồng đã ổn định.
- Không thêm log thường xuyên vào runtime. Chỉ giữ lỗi nghiêm trọng cần chẩn đoán.
- Bản offline dùng giá trị mặc định có trong các `AbConfigBase`; remote A/B và SDK được tách sang giai đoạn tích hợp dịch vụ.

## 2. Cách đọc và cập nhật tiến độ

Ký hiệu:

- `[ ]` Chưa làm
- `[~]` Đang làm hoặc mới chỉ có một phần
- `[x]` Hoàn thành và đã kiểm chứng
- `[!]` Bị chặn hoặc cần quyết định

Định nghĩa hoàn thành cho một hạng mục port:

1. Đã xác định đầy đủ file/hàm/scene nguồn.
2. Đã port hoặc chuyển thể toàn bộ hành vi cần thiết.
3. Không còn phụ thuộc tạm như `PlayerPrefs`, giá trị hard-code hay scene test nếu nguồn có hệ thống thật.
4. Có kiểm thử logic hoặc checklist so sánh trực tiếp Godot ↔ Unity.
5. Hoạt động trong luồng người chơi thật, không chỉ chạy bằng Inspector/Context Menu.
6. Tài liệu này được cập nhật trạng thái và ghi rõ ngoại lệ nếu có.
7. Với thành phần có trước roadmap: đã xác định provenance từng hành vi, loại bỏ giả định/adapter tự viết mà nguồn đã có cách xử lý, và chỉ giữ adapter Unity thật sự bắt buộc trong Sổ chuyển thể.

## 3. Kết quả kiểm kê nguồn gốc

### 3.1 Kiến trúc tổng thể

- Entry scene: `launcher.tscn`; thiết kế dọc cơ sở 1080×1920, tối đa 60 FPS.
- Godot đăng ký 25 autoload/service, trong đó các phụ thuộc gameplay quan trọng nhất là `GameState`, `SoundManager`, `UIManager`, `ABTestManager`, `SessionManager`, `LanguageManager`, `AwardManager` và `ClockTicker`.
- `UIManager` quản lý registry, cache, layer, stack, mask, back, input guard, tải bất đồng bộ và prewarm. Vì vậy ba Unity Scene hiện tại không phải kiến trúc tương đương hoàn chỉnh.
- `GameState` dài khoảng 2.398 dòng và chứa tiến độ level, lựa chọn chiến lược/DDA, mạng sống, tools, daily, setting, thống kê, A/B đã lưu, puzzle gần đây, snapshot cuối game và dữ liệu thưởng.
- `BaseGamePage` dài hơn 4.200 dòng, là trung tâm của gameplay thường và là lớp nền cho Daily Game.
- Có khoảng 89 scene UI; registry production có 33 page/biến thể page.
- Có khoảng 89 lớp cấu hình A/B. Mỗi lớp đã chứa giá trị mặc định, nên có thể port đường chạy mặc định trước mà không cần tự nghĩ cấu hình.

### 3.2 Nhóm chức năng gốc

| Nhóm | Phạm vi đã thấy trong nguồn | Ưu tiên |
|---|---|---:|
| Startup/UI Core | Launcher, Splash, UI registry/stack/layers, popup queue, back/input guard | P0 |
| Game State | SaveStore mã hóa, dual-slot, migration, settings, progress, snapshot | P0 |
| Level Pipeline | Bank thường, SP, LK, LK Modified, LK Style, GC, transform, filter, DDA | P0 |
| Gameplay Core | Cell state, validator, conflicts, completion, level/color generation | P0 |
| Input | Tap, double tap, swipe paint, stroke, swipe protection, undo history | P0 |
| Game Loop | Lives, wrong guess, score/combo, tools, hints, auto-complete, win/fail | P0 |
| Board/UI Game | Board/cell, rule bar, progress/lives, animations, toast, feedback | P0 |
| Tutorial | Bàn 4×4 cố định, 7 bước, hand/mask, IQ/feedback variants | P0 |
| Home/Settings | Home, settings, language, how-to-play, bank entry | P1 |
| Result | Win variants, fail, revive/restart, reward-restored, next level | P1 |
| Daily/Meta | Daily challenge, streak, awards | P1 |
| Profile/Social | Profile/avatar/frame, robot service, rank activity | P2 |
| Product Services | Ads, tracker, auth, cloud sync, push, privacy/ATT, feedback/rate | P2 |
| Debug/Dev | Debug pages, generator, JSON input, mock ad/banner, cheat API | P3 |

## 4. Trạng thái Unity hiện tại

Đánh giá này thay cho danh sách “Giai đoạn 1–7 hoàn thành” cũ. Các phần đã chạy được vẫn được giữ, nhưng trạng thái được tính lại theo độ đầy đủ so với nguồn.

| Thành phần Unity | Trạng thái | Đánh giá so với nguồn |
|---|---:|---|
| Cấu trúc `Assets/_Project` | `[x]` | Có cấu trúc tách riêng cho dự án |
| Sprites/audio/fonts/cfg/bank data | `[~]` | Đã copy phần lớn; cần audit import, tên, metadata và tài nguyên thừa `.import/.tres` |
| DOTween | `[x]` | Đã cài; chỉ dùng khi tương đương animation nguồn |
| `CellState.cs` | `[x]` | Đã port model trạng thái cơ bản |
| `GameScoreModel.cs` | `[x]` | Model/restore, công thức legacy/8 variant và typed score/combo/deduction/life feedback contract đã port/test; presenter hình ảnh còn ở R9 |
| `QueendokuCore.cs` | `[x]` | Đã port API conflict/completion và nối qua `BoardStateModel`; có fixture kiểm tra thứ tự luật nguồn |
| `BankData/LevelBankIO/MiniJson` | `[~]` | Đã đủ regular/SP/LK/LK Modified/LK Style/GC, cache và API size/rank/tier; còn malformed/encrypted-resource PlayMode tests |
| `LevelData.cs` | `[~]` | Selection pipeline, injected inclusive RNG, Daily First Easy, filters, transform, prefill và puzzle ID đã có; còn GameSession dedup retry |
| `LevelGenerator.cs` | `[x]` | Đã đủ RGB, Lab, pattern dark/light, seeded LCG và toàn bộ palette/config branch nguồn; đã nối GameplayManager → BoardView |
| 5.411 level thường | `[x]` | Đã xác thực cấu trúc/solution ở bước kiểm tra trước |
| `CellView.cs` | `[~]` | Đã có state/hint/error, reset lifecycle và SDF bốn góc theo source; icon/VFX/toàn bộ animation variant còn thiếu |
| `BoardView.cs` | `[~]` | Đã có N×N, intrinsic 1008, board-enlarge size 8+, palette nguồn, nền/góc, local pool và single-line region overlay; safe-area đã port, còn device/pixel/video parity |
| Input classes | `[~]` | Tap/double/swipe desktop đã được người dùng xác nhận mượt trong PlayMode sau raw-event adapter và background snapshot writer; còn touch thật/video parity |
| `GameplayManager.cs` | `[~]` | Đã chuyển domain sang `GameSession`, nối snapshot, tool-resource, idle-hint, terminal coordinator và phát typed gameplay feedback sau board update; còn presenter/page navigation/UI gốc |
| `PoolManager.cs` | `[~]` | Prototype global không còn được Board dùng; Cell lifecycle nay do local pool của `BoardView` sở hữu, file legacy chờ cleanup khi audit toàn bộ consumer |
| Rounded-corner shader | `[~]` | Đã port lại từ `cell_bg_round(.hard).gdshader` với bốn bán kính, hard-edge và cache material; chờ PlayMode/device parity |
| Loading/Home/Gameplay scenes | `[~]` | Scene khung hoặc test; Home gần như chưa có nội dung |
| GameState/SaveStore | `[~]` | P0 schema/runtime/dual-slot/migration đã port; endgame encrypted write đã tách khỏi main thread và có lifecycle flush; app-kill thiết bị thật và P1/P2 còn thiếu |
| AB default configuration | `[~]` | Default profile 27 config, gồm input/layout, score, reward/prop highlight và `mark_sound`; remote/timing runtime còn thiếu |
| HintEngine/PreCat | `[~]` | Logic HintEngine và PreCatDecider đã port/test, Hint/Locate resource flow đã nối session; UI hint còn ở R9 |
| UI stack/registry/popup queue | `[~]` | Đã port registry/window/manager cùng popup priority queue + hai config DSL parser; chưa tạo registry asset, Home handlers và scene bootstrap |
| Tutorial/Result/Daily/Meta | `[ ]` | Chưa làm |
| Audio manager/settings | `[~]` | Đã port SoundManager contract, fixed path/polyphony, oldest-voice pool, settings/silent, dynamic path/meow delay và BGM hard-off; còn catalog asset/bootstrap, presenter call sites và PlayMode audio |
| Automated Unity tests | `[~]` | Các assembly Core/Gameplay/Editor/EditMode compile sạch; bổ sung test R7 và UI/startup/popup R10, full Unity suite và PlayMode/UI được gom để chạy sau |

## 5. Thứ tự phụ thuộc

```text
Audit + parity fixtures
        ↓
Contracts dữ liệu + Config mặc định + Save/GameState
        ↓
Level pipeline hoàn chỉnh ───────┐
        ↓                        │
Gameplay core + HintEngine       │
        ↓                        │
Input chuẩn + Board/Cell model   │
        ↓                        │
Game session loop ←──────────────┘
        ↓
UI framework + layout + audio
        ↓
Tutorial → Home → Main Game → Win/Fail
        ↓
Daily/Streak/Award → Profile/Rank
        ↓
SDK/online services → QA/release
```

Không làm VFX chi tiết, bo góc hoặc tinh chỉnh pixel trước khi contract board và hệ thống layout được khóa.

## 6. Roadmap chi tiết

### R0 — Quản trị bản port và bộ đối chiếu

Mục tiêu: mọi thay đổi sau này đều truy được về nguồn và không “lấp chỗ trống” bằng suy đoán.

- `[x]` Kiểm kê autoload, module, scene UI và quy mô mã nguồn.
- `[x]` Xác định launcher, UI registry, BaseGamePage, GameState và AB configs là các trục phụ thuộc.
- `[x]` Tạo `Docs/SourceMap.md`: Godot file/class → Unity file/class.
- `[x]` Tạo `Docs/ParityChecklist.md`: hành vi quan sát được theo từng màn hình.
- `[ ]` Chụp bộ reference từ bản gốc cho 1080×1920: startup, tutorial, home, board 4–10, win, fail, settings.
- `[ ]` Ghi video reference tap/double-tap/swipe/undo/wrong guess/hint/win/fail.
- `[ ]` Tạo danh sách asset nguồn → asset Unity và phát hiện asset thiếu/trùng/thừa.

**Cổng hoàn thành:** mọi hạng mục P0 có file nguồn, kết quả mong đợi và cách kiểm chứng.

### R1 — Assembly, contract và kiểm thử nền

Mục tiêu: tách logic thuần khỏi MonoBehaviour để port/test được 1:1.

- `[~]` Đã tạo asmdef nền cho Core, Gameplay và EditMode Tests; UI/Services sẽ thêm khi bắt đầu module, chờ lần compile đầu xác nhận references.
- `[ ]` Định nghĩa contract cho clock, random seed, storage, config, audio và navigation.
- `[~]` 190 EditMode test case cho core/input/JSON/save/repository đã pass; PlayMode test chưa làm.
- `[ ]` Tạo fixture level đại diện cho size 4–10, rank/tier, SP, LK và level lỗi.
- `[ ]` Tạo comparator cho board state, conflict, completion, transform và color map.
- `[ ]` Chuẩn hóa namespace/tên class theo source map; không đổi thuật ngữ gameplay tùy ý.

**Cổng hoàn thành:** test runner chạy độc lập; failure chỉ ra đúng module/parity case.

### R2 — Tài nguyên và pipeline import

Mục tiêu: Unity dùng đúng tài nguyên gốc với import setting ổn định.

- `[ ]` Kiểm kê sprites, 132 audio file, fonts, Spine và bank/config.
- `[ ]` Phân loại texture: UI, sprite, atlas, nine-slice, board/cell, effect.
- `[ ]` Thiết lập Pixels Per Unit, filter, compression, alpha và Sprite Mode theo nhóm.
- `[ ]` Xác định tài nguyên Godot-only (`.import`, `.tres`, shader/material) cần chuyển đổi hay loại khỏi build.
- `[ ]` Import/kiểm chứng font và fallback cho từng locale.
- `[ ]` Kiểm tra Spine runtime/version và chuyển scene/animation tương ứng.
- `[ ]` Tạo asset catalog/address strategy; chỉ giữ `Resources` cho dữ liệu thực sự cần load bằng tên nếu phù hợp.

**Cổng hoàn thành:** không có asset runtime quan trọng bị Unity import sai; catalog truy được về nguồn.

### R3 — Config mặc định, GameState và persistence

Mục tiêu: thay hard-code/PlayerPrefs tạm bằng state giống nguồn.

- `[~]` Đã port contract typed của `AbConfigBase`, default provider và catalog tra cứu; switch-history/dye timing runtime chưa có.
- `[~]` Đã port đầy đủ policy `region_color`, `size_cycle`, `swipe_protect`, `doubletap_protect`, `score_encourage`; 19 config P0 còn lại mới có metadata/default, chưa có policy/runtime consumer.
- `[x]` Đã tạo `DefaultConfigProfile` gồm đúng 24 config P0 từ `default_value` của nguồn, có đánh dấu 4 config không được manager gốc đăng ký; chưa phụ thuộc SDK remote.
- `[~]` Đã tạo `GameStateData` typed P0 và port legacy-file migration; nguồn không có schema-version migration thực thi. P1/P2 và version tương lai của format Unity chưa hoàn tất.
- `[~]` Đã port schema field P0, repository player/endgame và runtime service cho progress/settings/tools/retry/pre-cat/endgame; win/fail aggregate P0 đã nối, migration tương lai và P1/P2 chưa hoàn tất.
- `[~]` Đã port adapter `SaveStore`: encrypted/authenticated write, verify, dual-slot A/B, flag, retry load và legacy fallback. Endgame runtime serialize snapshot bất biến trên caller rồi coalesce/mã hóa/verify/fsync tuần tự trên worker; pause/focus-out/quit flush chờ hoàn tất. Cần test app-kill/platform filesystem thật.
- `[~]` Đã port runtime settings: music, sound, vibration adapter, people, locale và music-user-modified; UI/audio/language consumers chưa nối.
- `[~]` Đã port bank/main/lkmod progress, current level/tutorial/strategy, locate/hint tools, retry puzzle, pre-cat state, endgame snapshot/stats/id và level-settle counters/DDA. `undo` giữ nguyên anomaly API nguồn; start-toast/tracker/meta state còn ở module sau.
- `[x]` Đã loại `PlayerPrefs` khỏi `LevelData`; progress dùng `GameStateRuntime`/repository với key và tier rule giống nguồn.
- `[~]` Save/load, wrong password, tamper, một/cả hai slot hỏng, flag lỗi, legacy fallback/migration và endgame separation đã pass; app-kill/platform filesystem chưa kiểm thử thực địa.

**Cổng hoàn thành:** restart app khôi phục đúng progress/settings/session P0 và chịu được save bị hỏng một slot.

### R4 — Level/bank pipeline hoàn chỉnh

Mục tiêu: cùng đầu vào state/config phải chọn cùng loại puzzle như Godot.

- `[x]` Đọc/XOR/parse bank level thường và xác thực 5.411 solution.
- `[~]` Port `BankData`, `LevelBankIO`, `LevelEntry`; đủ contract bank nguồn, còn kiểm thử lỗi asset/parser thực địa.
- `[x]` Port đầy đủ bank thường theo size/rank/tier.
- `[x]` Port SP, LK, LK Modified, LK Style và GC.
- `[x]` Port `GetSize`, special mapping, strategy→rank/tier và hard-level rules.
- `[x]` Port `GetNextEntry` và `GetNextEntryMain`, gồm progress, fallback, strict rank và invalid-entry advance.
- `[x]` Port transform/normalize/serialize/canonical puzzle ID và transform 0–7 trong main pool.
- `[~]` Port filter `single_region_num`, recent puzzle protection và remaining attempts; lọc/budget và lịch sử 100 puzzle đã có, retry một lần còn chờ GameSession consumer.
- `[~]` Port `compute_prefill`, `PreCatDecider`, chọn ô rank >= 3 và các khóa pre-cat trong GameState; còn nối quyết định vào GameSession runtime.
- `[x]` Port toàn bộ `LevelGenerator`: RGB, Lab, seeded LCG, pattern và palette từ config.
- `[x]` Parity test các level đặc biệt và chuỗi level 1–250 với config mặc định/fixture bank đại diện.

**Cổng hoàn thành:** pipeline deterministic theo seed/state; không cần fallback tự nghĩ; mọi nhánh bank có test.

### R5 — Gameplay domain thuần

Mục tiêu: toàn bộ luật có thể chạy không cần UI.

- `[x]` Port `CellState` và `QueendokuCore`.
- `[x]` So khớp API conflict: row, column, region và diagonal adjacency.
- `[x]` Port `BoardStateModel` riêng và nối vào `GameplayManager`; `BoardView` không còn là nguồn dữ liệu của input/win check.
- `[x]` Port `HintEngine`: R1 mark/single, R2, R3/R4 subset giới hạn k=6, chain contradiction, cell ranks và R4+.
- `[x]` Port và nối wrong guess → ERROR đúng source (chỉ solver snapshot mới fold thành MARK), rule violation priority và conflicting-cat lookup.
- `[x]` Port remaining cats, completion, correct crosses và false crosses trên board model.
- `[x]` Port GameScoreModel/restore và công thức `score_encourage`: legacy, non-round, multiplier, skill, deduction, life bonus và reset.
- `[x]` Port StepHistory/StepRecord đầy đủ API, action metadata và định dạng serialize/deserialize nguồn.
- `[x]` Toàn bộ domain R5 có fixture không phụ thuộc animation; action thực đã dùng board model, scoring và rule classification. Lives/timing/UI side effect thuộc GameSession R8 và feedback R9.

**Cổng hoàn thành:** cùng board fixture, Godot và Unity cho cùng action legality, hint, score và completion.

### R6 — Input parity

Mục tiêu: hành vi chuột/cảm ứng giống nguồn trên mọi board size và framerate.

- `[~]` Tap, double tap, swipe paint và stroke context đã có test; ERROR là terminal start đúng source và không thể xóa/khởi phát stroke. Cần PlayMode/touch parity.
- `[~]` Đã đối chiếu `BoardGestureRecognizer` và recognizer guard; input scheme/operation nâng cao còn theo R8/R11.
- `[x]` Đã port `DoubletapProtectConfig` và nối cửa sổ theo truth/conflict vào recognizer.
- `[~]` Đã port/nối SwipeAxisGuard, SwipeVelocityGate và `SwipeProtectConfig`; dynamic/touch thực địa chưa xác minh.
- `[~]` Đã khóa một pointer, hủy khi mất focus/disable và xử lý tọa độ ngoài board; UI overlay/touch thật chưa test.
- `[~]` Nội suy cell bỏ qua và axis lock có EditMode tests; cần gesture recording/PlayMode xác nhận không đổi cell kề.
- `[x]` Tap đầu trả action tức thời đúng source; double tap dùng window 0,25/0,35 giây và swipe nội suy không phụ thuộc pending tap. Người dùng đã xác nhận tap/double tap/swipe desktop hoạt động đúng sau bản sửa persistence.
- `[~]` Đã loại kiến trúc prototype bắt pointer trên từng `CellView`: giống Godot, cell chỉ hiển thị/không raycast và một `BoardView` duy nhất nhận down/drag/up, quy đổi board-local rồi phát gesture. Đã bổ sung `on_drag_tick` mỗi frame; cần PlayMode xác nhận bản sửa không còn chọn theo vị trí cursor sau khi nhấn.
- `[x]` Đã loại stall snapshot khỏi input hot path: policy MARK debounce 0,5 giây và CAT/ERROR immediate vẫn theo nguồn, nhưng adapter mã hóa/verify/fsync Unity chạy trên background worker. Smoke test enqueue khoảng 5 ms và người dùng xác nhận không còn nhịp mượt → đứng → mượt trên desktop.
- `[ ]` Xác minh tap vào cat, swipe bắt đầu từ cat và undo theo đúng source.
- `[ ]` PlayMode tests ở 30/60/120 FPS mô phỏng và nhiều kích thước cell.
- `[ ]` Dùng cùng input core cho Tutorial và Main Game, chỉ thay policy/allowed cells.
- `[~]` Cổng desktop PlayMode đã đủ để tiếp tục R8 theo xác nhận của người dùng; touch/multi-pointer trên thiết bị thật vẫn là cổng riêng trước release mobile.

**Cổng hoàn thành:** bộ video gesture gốc và Unity cho cùng chuỗi board state; không có cell ngoài đường kéo bị đổi.

### R7 — Board, Cell và layout chuẩn

Mục tiêu: board 4×4 đến 10×10 đúng logic và đúng pixel trên màn hình dọc.

- `[~]` Runtime grid đã khóa `FixedColumnCount = N`, nên không tự đổi N×N thành bố cục khác.
- `[x]` Port `BoardView.intrinsic_size_for`, padding/gap/slot mặc định và baseline board width 1008; input dùng tọa độ local trước scale đúng nguồn.
- `[~]` Đã port toàn bộ palette config, layout `game_grid_ui` 0–3, nền Board, góc từng cell, hard-edge và `BoardGridOverlay` phân vùng; nhánh offline mặc định `normal/new_cell_only` chờ PlayMode visual parity.
- `[~]` Cell prefab đã có cat/mark, ErrorAppear, hint/prompt và rounded background nguồn; change-source visual cùng toàn bộ variant animation còn thiếu.
- `[x]` Board đã bỏ global `PoolManager` prototype, dùng pool Cell cục bộ với reactivation và reset tween/VFX/state; người dùng xác nhận Cell vẫn sạch/bình thường sau vòng chơi rồi thoát trong PlayMode.
- `[~]` Đã port viewport 1080×2400, keep-width, khoảng thích nghi 1920–2400, safe top/bottom và collapse `HeaderAdaptHolder`; chờ PlayMode/device notch xác nhận.
- `[~]` Đã port `board_size_big`: size 8+ dùng profile big và board rộng `1008×1,04167`; còn so pixel reference cho size 4–10.
- `[~]` Đã chuyển shader SDF bốn góc của source sang UGUI, cache material theo size/góc/hard-state và bù scale đúng nguồn; chờ kiểm tra shader trên thiết bị/PlayMode.
- `[x]` Runtime dùng `RegionColorPipeline` với default `new_cell_only`; palette Inspector tạm không còn ghi đè palette nguồn.

**Cổng hoàn thành:** board không đổi hàng/cột khi resize; sai lệch layout trong ngưỡng đã định; mọi cell reset sạch khi tái sử dụng.

### R8 — Vòng đời một ván Main Game

Mục tiêu: port hành vi P0 của `BaseGamePage`, không tiếp tục nhồi logic vào prototype hiện tại một cách ngẫu nhiên.

- `[~]` Đã tách `GameSession` khỏi view với state machine Loading → Entering → Playing → ResolvingWrongGuess → Won/Failed → Leaving; còn nối page/scene navigation.
- `[~]` Khởi tạo puzzle, typed restore, snapshot schema v2/integrity/repository, retry cache và normal-entry restore đã nối; bank/debug entry modes đầy đủ còn thiếu.
- `[~]` Lives, mistake, pending wrong guess 0,4/0,6 giây, fail và revive theo số life truyền vào đã chạy trong session; heart/fish UI và revive config consumer còn thiếu.
- `[~]` Correct/wrong action, rule violation, score, history và win transition đã chạy qua session; feedback arbitration thuộc R9.
- `[~]` AutoComplete domain đã đúng mark-ring/cat order và không ghi history; PreCat placement API đã có; auto-mark/config runtime còn thiếu và mặc định nguồn đang Off.
- `[~]` Clear, Locate và Hint request/apply/cancel đã chạy qua session; Locate/Hint nay dùng `ToolResourceCoordinator` đúng free zone, decrement, cooldown dùng chung 800 ms và reward-request event. Award/ads cùng ToolButton UI còn chờ adapter/view; Undo UI không có implementation trong source.
- `[~]` Hint cooldown 0,5/0,8 giây và mutex log-free đã nối. Idle policy đã port đúng 20 giây chờ, guard, once/lifetime và nhịp repeat 10 giây chạy → 20 giây chờ; pulse thực tế chờ `ToolButtonView` ở R9.
- `[~]` Save/resume/exit và flush app pause/focus đã nối bằng scheduler 0,5 giây. Win/Fail/Revive/Restart/Quit sở hữu snapshot đúng thời điểm; app-kill PlayMode còn thiếu.
- `[~]` Remaining và completion transition đã ở session; aggregate coordinator phát typed data cho result, còn normal/hard toast cùng progress UI.
- `[~]` Đã port stats P0 cần cho result/DDA: mistake/revive/restart/score/combo, session/ngày, clean/fail/retry và strategy. Elapsed/completion/tools-used presenter data cùng tracker online còn ở R9/R13/R16.

**Cổng hoàn thành:** chơi liên tục level 1–30, thoát/khôi phục, sai/thua/revive/thắng mà state không lệch.

### R9 — Scoring, hint UI, audio và gameplay feedback

Mục tiêu: sau khi logic ổn định mới nối phần cảm giác chơi.

- `[~]` Đã port typed combo/score/deduction/life-bonus feedback, Header hai cột Level/Score, CatHeartRow với tiến độ mèo thật, bitmap bubble, Encourage, pool và completion gate giữ bubble mèo cuối. Đã có LifeSlot 3 cá nguồn, lost/silent/revive timing và life-bonus bubble/score. Multiplier/skill pair dùng đúng bitmap nguồn; score/life flight dùng đúng delay, cubic Bézier, trail/burst lifetime và score bounce. Còn popup của Back/Settings, nền pill bo góc/particle cá và PlayMode/video parity cho VFX mới.
- `[~]` Đã port HintOverlay mặc định, lifecycle open/apply/close, cell clone/highlight tạm thời, R1/R2/R3/R4 preview và stagger timing; hiện dùng fallback tiếng Anh từ CSV nguồn, chain-detail đầy đủ và localization runtime còn thiếu.
- `[~]` Wrong feedback đã giữ ERROR và ErrorAppear trắng→đỏ theo animation mặc định; RuleBar v0 mặc định cùng rule_highlight AB-off đã port. Các AB variant/collapse và PlayMode visual parity còn thiếu.
- `[~]` Đã port `thumb_up` default 0 nên cat-hand/clap/hawk-eye/magnifier tắt đúng bản offline. Toàn bộ Spine source đã copy nhưng Unity chưa có Spine runtime; visual cho AB variant chưa được dựng giả và còn chờ dependency tương thích.
- `[~]` Đã port SoundManager 29 kind/27 fixed path, 39 dynamic combo/meow path, exact polyphony, oldest-voice pool và cleanup. Installer tạo serialized SoundCatalog + `Systems/Audio`, nối mark/cat/error/unmark/hint, BoardEnter, AllCleared và combo voice mặc định; cần Unity Refresh/PlayMode nghe thực tế, các call site Result/Fail thuộc R13.
- `[~]` Đã port BGM state API, duck kinds, dialog/ad flags và settings nhưng giữ hard-off/path rỗng đúng source; external ad/dialog adapters chưa nối.
- `[ ]` Port vibration qua interface platform và giữ no-op trên thiết bị không hỗ trợ.
- `[~]` Đã port tween P0 của score/deduction/Encourage, multiplier/skill, score/life flight trail-burst-bounce và life loss/silent/revive; particle cá vỡ/glow, board enter, cat/mark/error, win/fail và Spine feedback còn thiếu.

**Cổng hoàn thành:** timing không làm thay đổi logic; tắt sound/music/vibration có hiệu lực ngay và được lưu.

### R10 — UI framework và startup

Mục tiêu: tạo tương đương Unity cho UIManager/Launcher thay vì tăng số scene rời rạc không có stack.

- `[~]` Đã port `UiName`, layer và `UIRegistry` kiểu ScriptableObject từ registry Godot; prefab registry asset/variant Win sẽ được điền khi các page tồn tại.
- `[~]` Đã port page cache, stack, z-step, fullscreen occlusion, mask ref-count, back request, release-frame input guard và one-flight prewarm; còn nối AppBootstrap/visual mask và PlayMode.
- `[~]` Đã port popup queue priority giảm dần/stable, cancel/insert-next/flush và parser cho hai JSON gốc; Home handler/AB evaluation thuộc R12/R16.
- `[~]` Đã tạo `AppBootstrap` duy nhất theo serialized composition và đúng startup phase; chưa gắn scene vì Splash/Home/Tutorial prefab chưa tồn tại.
- `[~]` Đã port 60 FPS/keep-awake, first-session state, splash 2,0+0,5 giây, Game+board+bank prewarm; locale implementation thật thuộc R12.
- `[~]` Route bằng `tutorial_done` đến Tutorial/Home đã có trong coordinator; chờ page prefab/registry để chạy.
- `[x]` SDK/privacy/ATT/push/remote/data-sync/shortcut đã nằm sau `IAppStartupExternalServices`; offline dùng no-op và không thể chặn startup.
- `[~]` Đã xác định `SceneLoader.cs` singleton/scene-per-page là prototype không tương đương nguồn; giữ tạm cho scene hiện tại tới khi AppBootstrap/UI navigation thay consumer.

**Cổng hoàn thành:** cold start/warm start/back/overlay đúng, không double-open page hoặc lọt input qua popup.

### R11 — Tutorial

Mục tiêu: người chơi mới hoàn thành đúng tutorial 4×4 gốc rồi vào Game level 1 như nguồn.

- `[~]` Đã port board/solution/region guide cố định, contract layout width 919 và nối `TutorialPagePresenter` với `BoardView`; chờ Unity sinh prefab sau Refresh và PlayMode xác nhận.
- `[~]` Đã port state machine đủ 7 bước và nối chung input core của board vào policy tutorial; chờ PlayMode xác nhận toàn chuỗi.
- `[~]` Đã port allowed cells, required marks, confirm state, double-tap và step completion; fixture đã compile, chờ chạy Unity Test Runner.
- `[~]` Đã port static-hand adapter/tap pulse, swipe loop, mask/mirror cell, message/submessage, select-frame asset và confetti mặc định. Spine hand và IQ CPUParticles chưa có Unity runtime tương đương nên chưa giả lập sai.
- `[~]` Đã port domain và presenter default/check/IQ theo `GuideFeedbackConfig`, gồm SuccessCheck 0,95 giây và IQ bar 0,4 giây; IQ fireworks còn chờ adapter particle trung thực.
- `[~]` Đã port `TutorialDiagonalConfig`, `GuideFeedbackConfig` và dùng `DoubleTapProtectConfig` mặc định từ source; chờ test runner.
- `[~]` Đã nối committer idempotent, `Show(Game, level_index=1)` rồi `Hide(Tutorial)` đúng nguồn; còn chờ registry/startup pages và PlayMode route.
- `[~]` State reset/recreate, event/tween/mask/board-pool cleanup đã nối đúng lifecycle page; app lifecycle PlayMode còn chờ.

**Cổng hoàn thành:** tutorial có cùng chuỗi hành động, chặn sai input và khôi phục/reroute đúng.

### R12 — Home, Settings, Language, How-to-play và Bank

Mục tiêu: hoàn chỉnh vòng điều hướng offline quanh Main Game.

- `[~]` Đã port contract và `HomePagePresenter`: level/hard state, ba A/B config mặc định, Start/Settings/Profile/Back handlers, safe-top header, flow shader, source animation marker/timing, cleanup và level text động. Unity đã sinh prefab, material và registry entry; Daily/Profile dependency cùng PlayMode parity còn chờ.
- `[~]` Đã port Settings core state, presenter và prefab: music/sound/vibration/people persistence, pattern mode/dot persistence, outgame/game-mode visibility, phản hồi toggle, source toast, GenericPopup timing, Restart/Terms/Privacy/Feedback và route Language/HTP có điều kiện. Unity đã sinh prefab/registry entry; HTP page thật, PlayMode và pixel parity còn chờ.
- `[~]` Đã copy nguyên CSV nguồn và port parser/catalog, locale alias/fallback/Chinese canonicalization, persistence, dynamic `LocalizedText`, font adapter zh/ja/ko, Language popup 10 hàng và System/English dropdown. Parser file thật, compile và prefab/registry structure đã kiểm chứng; Unity Test Runner, device glyph và PlayMode locale refresh còn chờ.
- `[~]` Đã port cả `HowToPlay` toàn màn hình và `HowToPlayPaged`: matrix/coordinate nguồn, frame schedule, vòng demo, Previous/Next/Got it, localization/highlight, GenericPopup, silence/cleanup và cell cố định. Unity đã sinh/đăng ký cả hai prefab, không có missing-script marker; còn scene composition, Test Runner và PlayMode/VFX parity.
- `[~]` Đã port Bank browser đủ sáu nhánh nguồn Regular/LK/LK Modified/LK Style/GC/SP: toàn bộ scalar metadata thật, root/size/tier/LK/SP panel state, hard-tier split, selector, back stack, pooled row views và ba shape launch params chính xác. Core/Gameplay/Editor/EditMode compile sạch bằng Unity Roslyn; chờ Auto Refresh sinh/đăng ký `BankPage.prefab`, Test Runner và Game-page composition để chạy route thật.
- `[ ]` Gắn entry Daily/Streak/Rank theo feature availability; phần chưa làm hiển thị đúng source/default, không làm nút giả.
- `[ ]` So pixel/animation Home và settings với reference.

**Cổng hoàn thành:** Home → Settings/HTP/Bank/Game → Home hoạt động ổn, state và text cập nhật ngay.

### R13 — Win, Fail, revive và progression

Mục tiêu: khép kín Main Game từ chọn level đến kết quả và level kế tiếp.

- `[ ]` Port Win page mặc định và variant được config chọn.
- `[ ]` Port score/time/combo/beat percent/pass text và board preview.
- `[ ]` Port win toast variants và thứ tự popup sau thắng.
- `[ ]` Port Fail page, remaining cats, fail text, restart/home.
- `[ ]` Port revive life/free revive logic; quảng cáo dùng contract/no-op hoặc mock trước.
- `[x]` Port level advance, clean win/fail/retry counters và DDA strategy update P0.
- `[x]` Port endgame snapshot clear/restore đúng thời điểm cho Win/Fail/Revive/Restart/Quit.
- `[ ]` Test special/hard level, retry nhiều lần và đóng app tại mọi điểm transition.

**Cổng hoàn thành:** Home → Game → Win/Fail/Revive → Next/Home không mất hoặc tăng progress sai.

### R14 — Daily Challenge, Streak và Award

Mục tiêu: port cụm meta offline theo đúng phụ thuộc.

- `[ ]` Port Daily entry/calendar/model và chọn daily level.
- `[ ]` Port DailyGame kế thừa/chia sẻ GameSession thay vì sao chép logic.
- `[ ]` Port Daily Win/Fail và daily progress/best beat percent.
- `[ ]` Port ClockTicker/date rollover/timezone behavior.
- `[ ]` Port Daily Streak core, page, resume, backfill và protect.
- `[ ]` Port AwardManager, pending/in-flight/history và render direct/streak/rank gift.
- `[ ]` Port popup priority giữa daily/streak/award/home.
- `[ ]` Test đổi ngày, missed day, backfill và crash giữa lúc nhận thưởng.

**Cổng hoàn thành:** dữ liệu ngày/streak/award idempotent, không nhận lặp hoặc mất quà.

### R15 — Profile, Robot và Rank Activity

Mục tiêu: hoàn thiện các hệ thống meta/social có trong bản gốc.

- `[ ]` Port ProfileService, avatar, frame, unlock/equip và red-dot.
- `[ ]` Port RobotService/model/cache dùng cho bảng xếp hạng offline/mock.
- `[ ]` Port RankActivityManager, periods, points, promotion/demotion/reward.
- `[ ]` Port rank pages, open popup, how-to-play và change page.
- `[ ]` Nối rank reward vào AwardManager và Home popup queue.
- `[ ]` Khi backend chưa có, dùng fixture/mock có nguồn rõ; không giả làm production data.

**Cổng hoàn thành:** profile/rank state ổn định qua restart và reward không lặp.

### R16 — SDK, online và product services

Mục tiêu: chỉ tích hợp sau khi bản offline parity đã ổn định.

- `[ ]` Tracker/event schema và session attribution.
- `[ ]` Ads: interstitial/banner/rewarded, cooldown/unlock/protection và audio hooks.
- `[ ]` Auth/device identity/API config.
- `[ ]` Data sync/merge conflict và startup timeout.
- `[ ]` Privacy/CMP/ATT/push permission/local notification.
- `[ ]` Feedback, Rate Us và Helpshift tương đương hoặc platform replacement được duyệt.
- `[ ]` Remote A/B provider; fallback luôn là `DefaultConfigProfile` đã port.
- `[ ]` Crash reporting chỉ log lỗi hữu ích; không khôi phục log debug rác.

**Cổng hoàn thành:** mất mạng/SDK lỗi không chặn startup hay làm hỏng save; consent được tôn trọng.

### R17 — Polish, hiệu năng, QA và release

Mục tiêu: khóa chất lượng sau khi feature parity đạt yêu cầu.

- `[ ]` Pixel comparison toàn bộ màn hình chính ở các aspect ratio mục tiêu.
- `[ ]` Audio/animation timing comparison bằng video.
- `[ ]` Kiểm thử touch thật trên Android/iOS, notch/safe-area và resume.
- `[ ]` Soak test level 1–250, daily, restart và memory/pool.
- `[ ]` Profiling CPU/GPU/GC, atlas/batch/draw call và thời gian startup.
- `[ ]` Kiểm thử save corruption, app kill, update/migration và thiếu mạng.
- `[ ]` Dọn TODO, adapter tạm, asset thừa và development UI khỏi release.
- `[ ]` Build pipeline, signing, versioning, symbols và release checklist.
- `[ ]` Lập danh sách sai khác cuối cùng; chỉ chấp nhận sai khác đã ghi lý do và được duyệt.

**Cổng hoàn thành:** release candidate vượt parity checklist, regression suite và device matrix.

## 7. Các mốc có thể chơi được

| Mốc | Nội dung | Điều kiện |
|---|---|---|
| M0 — Prototype hiện tại | Load bank, board tạm, input cơ bản | Đã có, chưa tính parity hoàn chỉnh |
| M1 — Deterministic Core | Save/config/level/core/input có test | R1–R6 đạt cổng logic |
| M2 — First User Experience | Startup → Tutorial → Home | R7, R10, R11 phần P0 hoàn thành |
| M3 — Main Loop | Home → Game → Win/Fail/Revive → Next | R8, R9, R12, R13 hoàn thành |
| M4 — Offline Content | Bank + Daily + Streak + Award | R14 hoàn thành |
| M5 — Meta/Social | Profile + Robot + Rank | R15 hoàn thành |
| M6 — Production | SDK/online + QA/release | R16–R17 hoàn thành |

## 8. Sổ chuyển thể và phần tạm

| ID | Hạng mục | Loại | Trạng thái/Quyết định cần làm |
|---|---|---|---|
| A-001 | Load bank qua Unity `Resources` | Unity adapter | Tạm chấp nhận; đánh giá lại ở R2 |
| A-002 | `MiniJson.cs` | Unity adapter | Giữ nếu parity parser đủ; thêm malformed-data tests |
| A-003 | Godot board-level `_gui_input`/`_input` → Unity input | Unity adapter bắt buộc | `BoardView` là input surface duy nhất và cell graphics không raycast, đúng `cell.mouse_filter = IGNORE` của nguồn. Desktop mouse down/move/up chạy trực tiếp từ raw `InputSystem.onEvent`; `EventSystem.RaycastAll` chỉ xác nhận board là hit trên cùng để không xuyên overlay, và UI mouse callbacks bị suppress cho sequence đó để không phát đôi. Touch vẫn dùng EventSystem + latch; drag threshold tắt, pointer/focus guard và dynamic tick được giữ. Touch/multi-pointer thật vẫn phải kiểm thử ở R6 |
| A-004 | `GridLayoutGroup.FixedColumnCount` | Adapter layout tạm | Chỉ bảo đảm N×N; thay/hoàn thiện theo intrinsic layout ở R7 |
| A-005 | Hoãn single tap trong cửa sổ double tap để không chớp X | Adapter đã loại bỏ | PlayMode phát hiện độ trễ và swipe không ổn định; recognizer nay trả tap đầu ngay đúng Godot, window 0,25/0,35 giây chỉ nhận lần tap thứ hai |
| A-006 | Bank progress bằng `PlayerPrefs` | Tạm, không đạt parity | Đã xóa; `LevelData` dùng `GameStateRuntime` |
| A-007 | Ba Unity Scene + `SceneLoader` | Kiến trúc tạm | Đánh giá lại theo UIManager/Launcher ở R10 |
| A-008 | Godot dùng SDF shader cho bốn góc cell và hard-edge | Adapter Unity bắt buộc | Port fragment SDF sang UGUI shader, dùng shared material cache theo size/radii/hard; Board overlay dùng UI mesh |
| A-009 | `PoolManager` tự tạo singleton | Unity adapter chưa hoàn chỉnh | Sửa reactivation/reset và lifecycle ở R7 |
| A-010 | `GameplayManager` prototype | Unity coordinator | Đã tách state domain sang `GameSession`; tiếp tục giữ MonoBehaviour này cho pointer/timer/BoardView và không nhồi tools/persistence trở lại |
| A-011 | Palette Inspector/seed 0 | Tạm | Đã thay bằng bank transform seed và `RegionColorConfig` pipeline nguồn; còn pixel/video parity ở R7 |
| A-012 | Godot `ConfigFile.save_encrypted_pass` | Unity adapter bắt buộc | Unity dùng JSON + AES-256-CBC/PBKDF2/HMAC, giữ dual-slot/flag/fallback; không tương thích nhị phân save Godot |
| A-013 | Endgame `_request_save_endgame()` coalesce 0,5 giây | Unity adapter bắt buộc | Gameplay giữ đúng debounce/immediate của nguồn. Runtime repository chụp JSON bất biến, latest-wins coalesce và ghi mã hóa tuần tự trên worker; pause/focus-out/destroy/quit flush. Constructor mặc định đồng bộ được giữ cho test/repository offline |
| A-014 | Tách palette override khỏi `BoardView` sang `RegionColorPipeline` thuần | Unity adapter tổ chức mã | Giữ nguyên thứ tự nhánh, hằng số RGB và thuật toán nguồn; giúp EditMode test mà không cần MonoBehaviour |
| A-015 | Godot global `randi_range` → `IInclusiveRandom` | Unity adapter bắt buộc | Runtime dùng `UnityEngine.Random.Range(min,max+1)`; test tiêm cận trên/dưới để giữ contract khoảng đóng |
| A-016 | Tách trạng thái khỏi Godot `BoardView` thành `BoardStateModel` thuần | Unity adapter tổ chức mã | Giữ nguyên luật set/heal/error của nguồn; `GameplayManager` cập nhật model trước rồi phản chiếu các thay đổi sang `BoardView`; Undo dùng đường restore riêng để có thể hoàn tác CAT |
| A-017 | Tách công thức score khỏi signal/view của `BaseGamePage` thành `GameScoringRules` | Unity adapter tổ chức mã | Giữ đúng thứ tự combo → SE count → gain/multiplier/skill và wrong reset/deduction; UI bubble/fly/life animation để R9 |
| A-018 | Dùng `HintMutex` trong `GameSession` | Unity adapter input guard | Source có class nhưng `BaseGamePage` không gọi; Godot dựa vào overlay chặn input, Unity dùng mutex để có guard tương đương mà không log |
| A-019 | `Undo Last Step` ContextMenu | Debug adapter tạm | Source có StepHistory/UNDO enum nhưng không có handler Undo gameplay; không coi đây là tính năng parity và phải bỏ/ẩn khỏi release nếu không tìm thấy call site khác |
| A-020 | `MainGameTransitionCoordinator` | Unity adapter tổ chức mã | Giữ thứ tự mutation của `GamePage`/`LevelOps`, phát typed transition data cho UI sau này và dùng guard một-lần để tránh callback terminal re-entry làm tăng progress/fail lặp. Restart từ Fail không settle lại; Quit ghi snapshot ngay thay lifecycle hide-page chưa có |
| A-021 | `GameplayFeedbackData` phát từ session result | Unity adapter tổ chức mã | Thay hai signal handler phụ thuộc `ComboFeedbackView` bằng typed payload theo cùng thứ tự. Life Bonus được tính trong domain để final score không phụ thuộc view; presenter phải hoàn tất timing trước Win settlement ở bước visual R9 |
| A-022 | Godot `AudioStreamPlayer.max_polyphony` → Unity voice pool | Unity adapter bắt buộc | Mỗi fixed/dynamic clip dùng số `AudioSource` cố định và cắt voice bắt đầu lâu nhất khi đầy. Clip đi qua serialized `SoundCatalog`, không dùng runtime `Resources.Load`; bootstrap/catalog asset chờ R10 |
| A-023 | Godot `DisplayServer.get_display_safe_area()` + `canvas_items/keep_width` → Unity UGUI | Unity adapter bắt buộc | `CanvasScaler` dùng reference 1080×2400, match width; inset vật lý từ `Screen.safeArea` được đổi sang đơn vị Canvas trước khi áp layout. Giữ đúng collapse `HeaderAdaptHolder`, ratio normal/big và chỉ áp safe-area trên mobile như source |
| A-024 | Godot autoload `UIManager` → Unity bootstrap-owned component | Unity adapter bắt buộc | `UIManager` không tự tạo singleton/global state; AppBootstrap sẽ sở hữu serialized registry/root/mask. Cache, layer stack, Z_STEP=50, fullscreen occlusion, mask ref-count, back và held-button guard giữ contract nguồn. Prefab reference dùng trực tiếp vì dự án chưa có Addressables; async API coalesce/yield một frame và không giả vờ tải nền |
| A-025 | Godot popup handler bằng `has_method/Callable` → Unity explicit handler map | Unity adapter tổ chức mã | `UIPopupConfig.BuildQueueForScene` nhận map key→coroutine có kiểu, tránh reflection nhưng giữ filter OpenScene, priority giảm dần, await từng handler và stable order. `CanExceedLimit` được parse nhưng không áp vì source HomePage hiện cũng không đọc field này |

## 9. Rủi ro đã biết

- Kiểm thử PlayMode 2026-08-08: save/resume giữa ván giữ đúng board. Audit tìm được cả ownership input prototype sai nguồn và PBKDF2 + verify + fsync endgame chạy đồng bộ gây stall main thread định kỳ. Board-level/raw mouse adapter và background endgame writer đã sửa hai điểm này; cổng R6 vẫn mở cho tới khi người dùng retest PlayMode/touch.
- Nguồn có remote A/B; bản extract chỉ bảo đảm đường mặc định. Muốn khớp một bản production cụ thể cần recording hoặc giá trị remote của bản đó.
- Một số chuỗi nguồn đang bị lỗi encoding khi đọc; localization phải lấy từ resource/translation gốc, không chép chuỗi lỗi từ console.
- Scene Godot chứa StyleBox, shader, Spine và animation track không thể copy trực tiếp; phải chuyển thể có reference hình/timing.
- `GameState` lớn và chạm nhiều module. Port toàn bộ một lần dễ sai; cần schema chung nhưng triển khai theo nhóm P0 → P2 với migration test.
- Daily/rank/reward dựa vào thời gian và tính idempotent; làm trước persistence sẽ tạo lỗi mất/nhân thưởng.
- Tinh chỉnh board/cell hiện tại chỉ là tạm. Pixel polish sớm sẽ bị làm lại khi layout/config gốc được port.

## 10. Việc thực hiện ngay tiếp theo

Sprint tiếp theo theo đúng chuỗi phụ thuộc là:

1. Refresh Unity và PlayMode-test R7 ở 1080×1920/1080×2400; xác nhận CanvasScaler, safe layout, board size 4–10 và Console sạch.
2. Dùng `GEM-R11-014` để port Tutorial state machine/domain fixture trước khi dựng UI.
3. Khi Splash/Home/Tutorial prefab có thật, tạo registry asset/root hierarchy và bật AppBootstrap route; không tạo page giả để lấp registry.
4. Chỉ thay consumer của `SceneLoader` khi route UI mới hoạt động; giữ Result/Fail popup ở R13 và device notch/pixel matrix ở R17.

## 11. Nhật ký cập nhật

- **2026-08-08:** kiểm kê lại toàn bộ nguồn Godot và Unity; thay kế hoạch tuyến tính cũ bằng roadmap theo phụ thuộc; phân loại prototype/input/layout/PlayerPrefs là phần tạm cần audit.
- **2026-08-08:** thêm `Docs/SourceMap.md`, `Docs/ParityChecklist.md`, tách assembly Core/Gameplay và tạo EditMode test suite nền; chưa đánh dấu test parity đạt cho tới khi Unity chạy suite thành công.
- **2026-08-08:** hoàn tất các bank pool regular/SP/LK/LK Modified/LK Style/GC, metadata clone, thứ tự pool, GC gate, LK reserved/strict-rank, main interleave 4:1, transform cycle và progress migration; regression 130/130.
- **2026-08-08:** port `single_region_num` coarse/strict filter và budget, canonical puzzle ID SHA-256, tutorial prefill, recent-puzzle snapshot/limit 100; giữ dedup retry ở đúng GameSession layer và hoãn PreCat cell ranking tới HintEngine. Regression 144/144.
- **2026-08-08:** hoàn tất LevelGenerator RGB/Lab/pattern/LCG và mọi palette branch RegionColor 0–12; nối bank transform seed, `patternRegions` và palette override vào board runtime. Giữ comparator tăng degree đúng biểu thức Godot thay vì diễn giải trong báo cáo. Regression 153/153.
- **2026-08-08:** port inclusive RNG contract, Daily First Easy date/snapshot/consume flow và fixture chọn level 1–250. Spot-check sửa hai sai lệch báo cáo: selection level 51+ vẫn clamp strategy 4 và level 20 là special chứ không phải hard. Regression 160/160.
- **2026-08-08:** dùng `GEM-R5-001` và spot-check `queendoku_core.gd`, `board_view.gd`, `cell_view.gd`, `step_history.gd` để port `BoardStateModel`, integrity healing, CAT/error legality và đầy đủ StepHistory serialization; nối model làm nguồn thật cho input/undo/win check, không thêm runtime log. Regression 165/165.
- **2026-08-08:** dùng `GEM-R5-002` nhưng port trực tiếp toàn bộ `hint_engine.gd` để có R1 mark/single, R2 bốn mode, R3/R4 subset k<=6, chain contradiction, cell ranks và R4+; sửa fixture R3 báo cáo dùng sai 5 region trên board 4×4. Port thêm `PreCatDecider` dùng rank >= 3 và RNG tiêm được, không giữ print nguồn. Regression 173/173.
- **2026-08-08:** dùng `GEM-R5-003` và spot-check model/config/BaseGamePage để sửa score prototype `100×combo` thành legacy 600/680/.../cap1320, port đủ `score_encourage` 0–7, restore fallback, multiplier/skill/deduction/life bonus; nối wrong guess vào rule priority và cross statistics. Không mang print score nguồn sang Unity. Regression 179/179.
- **2026-08-08:** dùng `GEM-R8-001` nhưng sửa sai mô tả restore: board dùng `placed_cats/marks/errors`, StepHistory deserialize riêng. Tách `GameSession` thuần với entry/input guards, lives/mistake, wrong 0,4/0,6 giây, win/fail/revive, undo và snapshot domain; `GameplayManager` chỉ điều phối input/timer/view. Regression 184/184.
- **2026-08-08:** dùng `GEM-R8-002` và spot-check toàn bộ tool handlers để port Clear, Locate region priority, Hint order/wrong-mark/apply, cooldown, mutex adapter và AutoComplete diagonal order. Sửa hai sai lệch báo cáo: Locate không nhận skill bonus; HintMutex nguồn không có call site. Ghi Undo ContextMenu là adapter vì source không có gameplay handler. Regression 190/190.
- **2026-08-08:** dùng `GEM-R8-003` và spot-check `game_page.gd` để nối snapshot schema v2/integrity/restore, retry cache, PreCat level 21+/lock/pending/config và debounce 0,5 giây kèm flush pause/focus/exit. Sửa Daily First Easy theo nhánh valid prefill của source; không mang print runtime sang Unity. Regression 193/193.
- **2026-08-08:** bỏ adapter defer single tap sau PlayMode feedback; port lại recognizer đúng source để tap trả action ngay, double tap chỉ giữ cửa sổ nhận lần hai và swipe 1→3 tách start/middle/end ổn định. Thêm fixture paint/erase nhanh qua ba ô. Regression 194/194.
- **2026-08-08:** sửa Unity pointer adapter để tap chốt cell từ row/column của `CellView` thay vì resolve lại cursor, đồng thời chuyển động khi đang giữ được chuyển tiếp qua pointer-enter mà không chờ drag threshold. Thêm fixture chống trôi cell pointer-down. Regression 195/195.
- **2026-08-08:** spot-check `InputSystemUIInputModule` phát hiện pointer-enter có thể chạy trước button-release trong cùng frame; loại bỏ cách chuyển tiếp hover và dùng `IInitializePotentialDragHandler.useDragThreshold=false`, giữ đúng release-before-drag của Unity đồng thời nhận chuyển động đầu tiên như Godot.
- **2026-08-08:** xác nhận `InputSystemUIInputModule` buffer Point/Click riêng rồi xử lý sau callback, khiến frame chậm có thể raycast bằng cursor sau click. Thêm `PointerPressPositionLatch` chốt Point tại click-down, lifecycle subscription sạch và fixture `Point A → Down → Point B → consume A`. Regression 196/196.
- **2026-08-08:** xác nhận Unity compile bốn assembly không có lỗi; port trực tiếp SwipeAxisGuard/SwipeVelocityGate và test gốc, nâng suite lên 55 test case. Chưa tích hợp guard vào input runtime cho tới khi port `swipe_protect` config.
- **2026-08-08:** dùng báo cáo `GEM-R3-001` và spot-check nguồn để port schema P0, player/endgame repository và SaveStore adapter; 67/67 EditMode case pass. Test bắt được và sửa sai lệch `float`/`double` tại ngưỡng vận tốc 1.2.
- **2026-08-08:** dùng `GEM-R3-002` và spot-check 5 file nguồn để port `AbConfigBase`, default provider/profile 24 config P0 và đầy đủ policy của region/size/swipe/double-tap; 27/27 test mới pass. Bốn config không được manager Godot đăng ký được giữ nguyên trạng thái thay vì tự suy diễn.
- **2026-08-08:** dùng `GEM-R3-003` và spot-check các đoạn progress nguồn để port `GameStateService`, key/schema legacy, snapshot, persist/commit và thay toàn bộ `PlayerPrefs` trong `LevelData`; 9/9 test progress/repository đạt, đồng thời regression assembly đã refresh đạt 94/94.
- **2026-08-08:** dùng `GEM-R3-004` và spot-check source để port settings, locate/hint tool signal, `has_used_tool`, current level/tutorial/strategy và vibration adapter; nối strategy level 51 vào `LevelData`. Targeted suite đạt 33/33; `undo` tiếp tục là field legacy không được tool API nguồn hỗ trợ.
- **2026-08-08:** dùng `GEM-R3-005` và spot-check source để port retry puzzle, pre-cat pending/lock/revive và endgame snapshot/stats/id với player/endgame store tách biệt; bỏ print Endgame của nguồn. Targeted suite đạt 40/40, regression assembly trước thay đổi đạt 108/108.
- **2026-08-08:** dùng `GEM-R3-006` và spot-check source để port proactive legacy migration; xác nhận nguồn không có schema-version migration và migration lần đầu chỉ tạo slot A. Bổ sung failure matrix cho cả hai slot/flag/legacy/tmp; targeted suite đạt 17/17.
- **2026-08-08:** dùng `GEM-R6-001` và spot-check recognizer/base page để nối double-tap window provider và swipe axis/velocity guard vào board-local pointer flow. Giữ adapter defer single tap để không chớp X; targeted input suite đạt 25/25, regression assembly trước thay đổi đạt 122/122.
- **2026-08-08:** audit lại toàn chuỗi input sau khi lỗi chọn theo cursor vẫn tái hiện. Nguồn nhận down trên một `BoardView`, nhận motion toàn cục khi giữ nút và đặt mọi `CellView` thành `MOUSE_FILTER_IGNORE`; prototype Unity lại bắt pointer ở từng cell. Đã chuyển down/drag/up về `BoardView`, tắt raycast cell graphics, giữ press-position latch dành riêng cho InputSystem và port `on_drag_tick`. Compile sạch; 189 test qua trong runner ngoài Unity, 7 lỗi hạ tầng runner do `System.Array.Fill/Reverse`, không liên quan thay đổi input. PlayMode retest còn bắt buộc.
- **2026-08-08:** board-level adapter vẫn tái hiện lỗi vì latch ở `InputAction` đã nằm sau bước InputSystem gom event. Chuyển riêng việc chốt vị trí mouse-down xuống raw `InputSystem.onEvent`, không bypass EventSystem/overlay; action callback không được phép ghi đè raw press. Compile sạch, regression ngoài Unity 190 pass/7 lỗi hạ tầng `Array.Fill/Reverse`; PlayMode retest còn bắt buộc.
- **2026-08-08:** raw-position latch vẫn tái hiện vì gesture còn chờ EventSystem dispatch. Desktop mouse path nay xử lý down/move/up trực tiếp trong raw event, dùng manual UI raycast để bảo toàn overlay blocking và suppress callback UI trùng; touch path không đổi. Gameplay assembly compile sạch; PlayMode retest còn bắt buộc.
- **2026-08-08:** xác định nhịp input mượt/đứng lặp lại trùng chính xác snapshot debounce: Unity adapter chạy PBKDF2 100.000 vòng, verify bằng decrypt lần hai và `Flush(true)` trên main thread. Giữ nguyên policy snapshot Godot nhưng thêm runtime endgame queue latest-wins trên worker, immutable JSON capture và flush ở pause/focus-out/destroy/quit. Core/Gameplay/EditMode Tests compile sạch; smoke test enqueue khoảng 5 ms, round-trip/coalesce/clear đạt. Người dùng xác nhận khóa bàn sau thao tác nhanh là hết 3 mạng đúng luật, không phải lỗi session.
- **2026-08-09:** người dùng xác nhận input desktop đã hoàn hảo sau background snapshot writer. Dùng `GEM-R8-004` nhưng spot-check sửa ba sai lệch quan trọng: `prop_highlight=2` là Hint once, repeat chạy 10 giây rồi phải chờ lại 20 giây, và Locate vẫn đánh dấu dirty/DDA khi consume thất bại. Port `reward_unlock_level`, `prop_highlight`, persisted highlight flag, runtime DDA/dirty flags, resource coordinator, reward event và idle controller; không tự tạo award/ad hoặc log. Core/Gameplay/EditMode compile sạch, regression 216/216; ToolButton/pulse view để R9.
- **2026-08-09:** dùng `GEM-R8-005` để dò đường rồi spot-check trực tiếp `game_page.gd`, `game_fail_page.gd`, `level_ops.gd` và `game_state.gd`. Port session/day stats, level won/failed, clean/fail/retry/DDA, aggregate Fail/Revive/Win/Restart/Quit, retry payload và Next; sửa report ở ownership snapshot, guard, clean-win và khác biệt Restart giữa ván/Restart sau Fail. Không tạo UI/result/VFX hay tracker giả. Core/Gameplay/EditMode compile sạch, regression 224/224.
- **2026-08-09:** spot-check trực tiếp hai score handler và `combo_feedback_view.gd` để port typed Correct/Wrong/Life feedback, multiplier/fly metadata, source User/Locate/Hint/AutoComplete và life-bonus final score. Sửa AutoComplete skill rank theo đúng điều kiện nguồn chỉ loại Locate/Hint. Chưa tạo presenter/VFX/audio; async completion gate vẫn mở. Gameplay/EditMode compile sạch, regression 228/228.
- **2026-08-09:** dùng `GEM-R9-001` rồi spot-check toàn bộ `sound_manager.gd`, config/call sites và 27 fixed assets. Port Services assembly, 29-kind enum với hai unmapped no-op, exact polyphony/oldest cutoff, serialized catalog, setting/silent/dynamic meow/voice và BGM hard-off. Bổ sung `mark_sound` vào profile, nối cell/hint contract có suppress đúng Clear/AutoComplete. Chưa tự tạo bootstrap hoặc âm thanh thay thế. Core/Gameplay+Services/EditMode compile sạch, regression 244/244.
- **2026-08-09:** dùng `GEM-R9-002` nhưng bác đề xuất đổi bitmap digit sang TMP và hard-code Encourage width; spot-check `combo_feedback_view.gd`, 4 level-flow scene và `game_page.tscn`. Port batch completion gate, score header, bitmap score/deduction pool, Encourage timing/position động và editor-time scene installer không sửa YAML. Đồng thời port Hint presentation contract/lifecycle và preview stagger từ nguồn; `GEM-R9-003/004` được dùng kiểm tra chéo nhưng report 004 sai `lost_index`, Fail condition và bỏ sót delay variant nên không được áp dụng máy móc. Runtime/editor compile sạch, 244 regression + 6 test mới đều đạt (250/250); cần Unity Refresh để installer ghi scene và PlayMode xác nhận hình ảnh.
- **2026-08-09:** PlayMode xác nhận ba bubble đầu nhưng lộ lỗi dòng tổng điểm bị Unity `Text` truncate và cần bảo toàn phản hồi mèo cuối. Đối chiếu source xác nhận mèo thứ tư cộng 840, tổng 2880 rồi mới Won; sửa overflow tương đương Godot và completion gate tối thiểu bằng thời lượng bubble. Port LifeSlot cá 3 slot, lost index `LivesBefore-1`, silent life bonus và revive timing từ `fish_slot.tscn`; installer nâng cấp Hierarchy thành `Systems` + `Canvas/Gameplay|HUD|Overlays` bằng Editor API. Runtime/editor compile sạch; 9/9 test feedback mục tiêu đạt, gồm 2 case mới (252 case hiện có), full Unity suite chờ Refresh.
- **2026-08-09:** dùng `GEM-R9-005` chỉ để dò file/asset rồi kiểm chứng trực tiếp `combo_feedback_view.gd`, `level_flow_multiplier.gd/.tscn`, `level_flow_skill_score.gd/.tscn` và ba scene trail/burst nguồn. Port bitmap multiplier/skill pair, previous→target timing, clamp cụm, delay 0,8/1,367/1,45 s, cubic Bézier 8 vòng Newton, flight 0,57 s, trail linger 0,067 s, burst 1,5 s và score bounce. Unity UI Image pool là adapter bắt buộc thay Line2D/CPUParticles2D trên Overlay Canvas; giữ count/lifetime/speed/asset nguồn. Installer idempotent chia `HUD/Feedback` thành 5 nhánh pool. Runtime/editor/test assembly compile sạch; thêm 4 flight-math case (256 case hiện có), full Unity suite và PlayMode VFX chờ chạy trong Editor.
- **2026-08-09:** dùng `GEM-R9-006` làm bản đồ rồi sửa lại theo nguồn trực tiếp: offline dùng `rule_info_bar_v0` và `rule_highlight=0`, không phải v4. Port RuleBar v0, HintOverlay/cell clone, pulse 0,65 s, preview stagger, ErrorAppear mặc định trắng→đỏ và sửa wrong double-tap từ MARK sai sang ERROR đúng domain nguồn. Hierarchy mới nằm trong `HUD/RuleBar` và `Overlays/HintOverlay`; scene/prefab chỉ được ghi qua Unity editor API. Core/Gameplay/Editor/EditMode compile sạch; runner độc lập đạt 252 case, 7 case LevelGenerator cũ không chạy được do giới hạn .NET runner (`Array.Fill/Reverse`), không phải regression mới. Unity Refresh/PlayMode visual parity còn chờ xác nhận.
- **2026-08-09:** đóng phản hồi test RuleBar/ERROR: ERROR chặn cả gesture start và không thể xóa đúng source; RuleBar default dùng text wrap + auto-fit + line spacing -10 và Glow nguồn. Ba diagram của v0 được xác minh là `visible=false` khi `rule_text=0`, nên không bật sai variant. Dùng `GEM-R9-007` làm bản đồ nhưng kiểm chứng lại thấy `thumb_up=0`; giữ cat-hand/Spine tắt đúng offline thay vì tạo animation giả khi Unity chưa có Spine runtime. Port `combo_voice`, `meow_feedback`, `thumb_up`, SoundCatalog 27 fixed + 39 dynamic, `Systems/Audio`, BoardEnter/AllCleared và combo voice call site. Compile sạch; runner đạt 257 case, 7 case LevelGenerator vẫn chỉ vướng giới hạn runner ngoài Unity.
- **2026-08-09:** phản hồi PlayMode xác nhận ERROR/X đỏ hoàn tất nhưng phát hiện RuleBar bị đặt theo khoảng 15 px của HintOverlay và board prototype 600 px. Đối chiếu trực tiếp `board_view.gd`, `base_game_page.gd`, `game_page.tscn` và `board_no_fuction.tres`; port intrinsic `108×N+30`, cell 100, gap 4, padding 15, visible width 1008 và vị trí RuleBar/Board theo VBox ratio nguồn. Tách page layout khỏi RuleBar presenter, chuyển Glow `et_mask_001` từ sprite auto-trim sang full-texture 9-slice với margin 120/117/116/116. Gameplay và Editor compile sạch bằng Unity Roslyn; PlayMode visual/input cho board mới chờ người dùng Refresh và kiểm tra.
- **2026-08-09:** người dùng xác nhận board 1008, RuleBar và input sau scale hoạt động ổn. Dùng `GEM-R9-009` làm bản đồ HUD; report giúp giảm quét rộng nhưng gắn `COMPLETE` sai vì bỏ `HeaderAdaptHolder`, active Level/Score override và nhiều offset chi tiết. Spot-check `game_page.tscn`, `back_and_setting_header.tscn`, `combo_feedback_view.gd` và `_update_remaining`; dựng cây `HUD/Header` + `HUD/CatHeartRow/{Target,HeartBg}`, chuyển LifeSlot cũ vào HeartBg, thêm Back/Settings asset nguồn, Level/Score hai cột và progress placed/total từ state thật với pulse 1→1,1→1 trong 0,6 s. Gameplay/Editor compile sạch bằng Unity Roslyn; scene chờ Refresh/PlayMode.
- **2026-08-09:** dùng `GEM-R9-010` để định tuyến nhưng kiểm chứng trực tiếp phát hiện overlay/hard-edge chỉ chạy ở `game_grid_ui=1`, không phải default. Port đúng default offline `game_grid_ui=0` + `region_color=2`: nền Board trắng bo góc 30, cell SDF bo góc 10 được bù scale và palette `new_cell_only`; đồng thời port contract layout 0–3, hard-edge và mesh overlay phân vùng cho variant single-line. Thay `PoolManager` global prototype bằng pool Cell cục bộ trong Board, reset tween/VFX/state và reactivation sạch. Core/Gameplay/Editor/EditMode compile sạch bằng Unity Roslyn; thêm 8 case layout/corner, chờ Unity Refresh và PlayMode visual/lifecycle.
- **2026-08-10:** người dùng xác nhận rounded Board/Cell và local Cell pool vẫn sạch sau chơi rồi thoát. Ba Console error sau thoát đều thuộc Editor installer: bổ sung guard `EditorApplication.isPlaying` cho khoảng ExitingPlayMode ở Feedback/Presentation/Audio installer; Audio installer nay tạo `Assets/_Project/Settings` bằng `AssetDatabase` trước `SoundCatalog.asset`. Editor assembly compile sạch, chờ Unity Refresh và một vòng Play→Stop xác nhận Console.
- **2026-08-10:** người dùng xác nhận vòng Play→Stop không còn Console error. Dùng `GEM-R7-011` để định tuyến safe-area nhưng spot-check bổ sung phần report bỏ sót: `project.godot` dùng viewport 1080×2400 + `canvas_items/keep_width`, `HeaderAdaptHolder` nội suy 0→65 trong khoảng cao 1920→2400 và collapse khi có top safe inset. Port đúng ratio/min-size của profile normal/big, board-enlarge từ size 8 với hệ số 1,04167, `Screen.safeArea` mobile và CanvasScaler match-width. Core/Gameplay/Editor/EditMode compile sạch bằng Unity Roslyn; còn Unity Refresh, PlayMode hai aspect và device-notch parity.
- **2026-08-10:** dùng `GEM-R10-012` làm bản đồ UI framework nhưng loại đề xuất PlayerPrefs/Addressables không có bằng chứng. Spot-check trực tiếp `ui_name.gd`, `ui_registry.gd`, `ui_layer_config.gd`, `ui_base_window.gd`, `ui_frame_window.gd` và `ui_manager.gd`; port enum/registry asset, lifecycle window, cache, stack/layer/Z-step, fullscreen occlusion, mask ref-count, back, held-button release guard và one-flight prewarm. `UIManager` là component do AppBootstrap sở hữu thay vì singleton tự tạo; `SceneLoader` giữ tạm tới khi có route thay thế. Core/Gameplay/Editor/EditMode test assembly compile sạch bằng Unity Roslyn; thêm 4 fixture UI framework, PlayMode được gom lại để người dùng test sau.
- **2026-08-10:** dùng `GEM-R10-013` làm bản đồ rồi sửa ba điểm theo source trực tiếp: launcher thực hiện privacy/push trước CMP/remote; priority JSON là mảng bốn entry; `CanExceedLimit` không được `_build_popup_queue()` sử dụng. Port `UIPopupQueue`, priority/AB trigger/parameter DSL parser, explicit handler map, `UIManager.AwaitHidden`, first-session persisted/runtime split và `AppBootstrap` theo phase với splash tối thiểu 2,0+0,5 giây, concurrent Game/board/bank prewarm, Tutorial/Home route và SDK no-op boundary. Không gắn bootstrap vào scene khi ba page chưa tồn tại. Core/Gameplay/EditMode compile sạch bằng Unity Roslyn; fixture mới bao phủ queue/config/timing/route/first-session, full Unity run để gom sau.
- **2026-08-10:** dùng `GEM-R11-014/015` để định tuyến rồi kiểm chứng trực tiếp `tutorial_page.gd`, config và entry guide trong bank. Port entry `pattern`, puzzle 4×4 id 51, state machine bảy bước, allowed/mask/mirror contract, double-tap 0,35 giây, sáu lượt hint, Current/Check/IQ feedback gate và completion committer idempotent. Sửa bốn điểm report/roadmap theo nguồn: bước đặt mèo cần double-tap, Check/IQ bỏ confirm riêng, hint là reveal/apply ba pha và hoàn thành route Game level 1 chứ không phải Home. Core/Gameplay/Editor/EditMode compile sạch bằng Unity Roslyn; thêm 14 fixture/case, chờ Unity Test Runner và presenter ở lượt kế tiếp.
- **2026-08-10:** sửa compile error `UIPopupStartupTests` sau Refresh: AssetDatabase chưa đưa `AppBootstrap.cs` vào Core response file nên test không thấy `AppStartupContract`. Chuyển contract thuần sang `UIContracts.cs`, giữ component scene riêng và làm mới GUID chưa được serialize của AppBootstrap để ép import lại. Core gồm AppBootstrap và toàn bộ EditMode test assembly compile sạch bằng đúng Unity Roslyn response files; không đổi hành vi startup.
- **2026-08-10:** dùng `GEM-R11-016` làm asset/completion map rồi kiểm chứng trực tiếp `tutorial_page.gd/.tscn`. Dựng `TutorialPagePresenter`, source layout 919 px, mask/mirror clone không bắt raycast, static-hand adapter + swipe timing, message/control, Check/IQ feedback, confetti mặc định, cleanup và route Game level 1. Thêm editor installer sinh prefab theo cây `Board/Mask/Guidance/Feedback` nhưng không tự tạo registry thiếu Splash/Home. Sửa chuyển đổi trục Y Godot→UGUI bằng board bounds thật và thêm `DOTween.Modules` dependency trực tiếp. Core/Gameplay/Editor và fixture tutorial compile sạch bằng Unity Roslyn; chờ Unity Refresh để sinh prefab, Test Runner và PlayMode parity. Spine hand/IQ particle chưa được giả lập khi chưa có runtime tương đương.
- **2026-08-10:** bắt đầu R12 bằng đối chiếu trực tiếp `home_page.gd/.tscn` và ba config `daily_streak`, `leaderboard_func`, `hard_button` trong lúc `GEM-R12-017` chưa có báo cáo. Port nguyên trạng default/policy A/B, level/hard presentation state, kích thước Start 750×160, animation marker `disappear/Entry`, hide delay và reward restore delay. Không suy diễn `NO_REWARD/CHALLENGE_ONLY/NO_LIT` theo tên khi code nguồn hiện chưa làm vậy, và chưa dựng Daily/Settings/Profile giả. Core và EditMode test assembly compile sạch bằng Unity Roslyn; Unity Test Runner và Home presenter/prefab còn chờ.
- **2026-08-10:** dùng `GEM-R12-017` để xác nhận route/asset/cleanup rồi bổ sung các offset và track timing còn thiếu bằng đối chiếu trực tiếp source. Dựng `HomePagePresenter`, safe-top header, level/hard display, Start→Game marker transition, Settings/Profile/Back handlers, popup queue cleanup và BGM serialized boundary. Port `fx_uv_scroll` thành `UIHomeFlow.shader` với tốc độ `(0.015,-0.015)`; logo dùng chính `common/logo.png` làm static adapter vì project chưa có Spine runtime. Installer tạo cây `Background/Root/{Loge,StartBtn,DailyStreakLayout,VBoxContainer}` và bốn entry slot rỗng, không tạo Daily/Rank/Profile giả. Core/Gameplay/Editor/EditMode compile sạch bằng Unity Roslyn; chờ Unity Refresh sinh material/prefab, sau đó mới registry và PlayMode parity.
- **2026-08-10:** Unity Refresh đã sinh `HomePage.prefab` và `HomeFlow.mat`, hierarchy/script reference hợp lệ, Console không có compile/shader/installer error. Dùng `GEM-R12-018` để định tuyến Settings nhưng spot-check trực tiếp phát hiện report bỏ phần persistence pattern và hai config source. Port `settings_language` timing `open_setting`, `blind_mod`, đầy đủ predicate `RuleTextConfig`, ba field `pattern_mode_on/pattern_entry_dot_dismissed/pattern_switch_dot_dismissed` cùng setter idempotent và `SettingsPageContract` cho outgame/game-mode. Default chính xác là Music ẩn, Sound/Vibration/People hiện, Language/Pattern/How-to-play ẩn. Core/EditMode compile sạch bằng Unity Roslyn; presenter/prefab/GenericPopup còn chờ.
- **2026-08-10:** sửa lỗi compile lộ ra ở Refresh của `TutorialPagePrefabInstaller`: installer import trực tiếp `Meowdoku.Core.UI` nên Editor asmdef phải reference `Meowdoku.Core` trực tiếp, không được dựa vào Gameplay bắc cầu. Bổ sung dependency và compile lại Editor sạch bằng cấu hình direct-reference tương đương Unity.
- **2026-08-10:** hoàn thiện Settings presenter/prefab theo source: toggle state/icon/toast cập nhật ngay, preview sound/vibration qua boundary, Restart/Terms/Privacy/Feedback, pattern dot và skip-close HTP. Tách `GenericPopupAnimator` dùng chung theo đúng marker/timing `GenericPopup.res`, port source toast `0,15 + 1,2 + 0,2 s`; Unity sinh prefab không missing script và Core/Gameplay/Editor/EditMode compile sạch.
- **2026-08-10:** copy nguyên `translations.csv` với SHA-256 khớp nguồn, port CSV parser hỗ trợ quoted newline, catalog chỉ giữ current+fallback locale, alias/fallback/Chinese canonicalization, `%s/%d` dynamic text và NotoSourceHan adapter. Smoke parse implementation thật xác nhận 76 cột, 1.695 record, 1.645 key; port Language popup/dropdown, Unity sinh `LocalizationCatalog.asset`, `LanguagePage.prefab` và `UIRegistry.asset` chỉ gồm Home/Tutorial/Setting/Language. Test Runner/PlayMode/device-font vẫn chờ.
- **2026-08-10:** đối chiếu trực tiếp hai script/scene How-to-play và `cell.tscn`, không gộp hai page. Port full demo ba board 3×5 cùng paged demo 4×4/5×5/4×4, toàn bộ matrix/toạ độ/frame wave, clear/loop/slide, Previous/Next/Got it, localization highlight, silence và lifecycle cleanup. Installer dùng 102 nested Cell prefab cố định, mở rộng rounded view per-corner và chỉ đăng ký hai page khi prefab thật tồn tại. Core/Gameplay/Editor/EditMode compile sạch; prefab auto-install, Test Runner, scene composition và PlayMode/VFX parity còn chờ.
