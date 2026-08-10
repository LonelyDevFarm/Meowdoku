# R12 Bank Browser Report — 2026-08-10

## Phạm vi

Port page Bank đang tồn tại trong Godot, không gộp nó vào level progression
thường và không tạo entry giả trên Home. Page nguồn có sáu pool:

- Regular
- LK
- LK Modified
- LK Style
- GC
- SP

## Nguồn đối chiếu trực tiếp

- `scripts/module/bank/view/bank_page.gd`
- `scripts/module/bank/ui/bank_page.tscn`
- `scripts/module/bank/model/bank_data.gd`
- `scripts/module/bank/model/level_bank_io.gd`
- `scripts/module/game/view/game_page.gd`
- 25 file `assets/resources/levels/bankData*.json`

Các báo cáo Gemini R3/R4 cũ chỉ được dùng làm chỉ mục. Schema, panel flow và
launch dictionary được xác minh lại từ source/asset thật vì báo cáo cũ không
bao phủ Bank page UI và thiếu `r1…r5`.

## Schema dữ liệu

Probe read-only giải XOR toàn bộ 25 bank asset cho union field thực tế:

`colorMap, date, id, label, maxR, pattern, patternRegions, r, r1, r2, r3,
r4, r5, regionMap, seed, seq, size, solution, steps, tier, transform`.

`LevelEntry` trước lượt này làm mất `id/date/label/r1…r5/transform/seq`.
Model nay giữ và clone toàn bộ scalar trên; cached entry vẫn không bị presenter
hoặc level selection sửa trực tiếp.

## Contract Bank

`BankBrowserContract` port:

- initial priority `go_lk_style`, rồi `go_lk`, rồi `go_regular`, size mặc định 7;
- root/regular-size/variant-size/tier/LK-list/SP-list state và back stack;
- size/rank/count lookup trực tiếp từ `BankData`;
- đúng hard-tier key list và cách source tách normal tier `N` khỏi `H`;
- selector one-based, clamp 1…count;
- đúng ba hình dạng tham số Game:
  - Regular/LK Style/GC: seed, strategy steps, style/GC/tier flags;
  - LK/LK Modified: id, maxR, LK/modified flags, không bịa strategy fields;
  - SP: id, strategy steps, SP flag và custom color map.

Board arrays được clone khi tạo request để Game consumer không thể làm bẩn
cache Bank.

## Presenter và prefab

`BankBrowserPagePresenter` giữ cây chức năng theo source:

```text
BankPage
├── Header
├── RootPanel
├── RegularSizePanel
├── TierPanel
├── ListPanel (SP)
├── LKPanel (LK/LK Modified)
└── VariantSizePanel (LK Style/GC)
```

- Root có đúng sáu card, pool rỗng không hiện.
- Size/tier/LK/SP row dùng template serialized và local reuse pool; reopen không
  destroy/re-instantiate lại toàn bộ row.
- Button được bind release-frame guard cả khi row mới materialize.
- Header/panel Back, LK selector và row launch đều cleanup listener theo
  window lifecycle.
- Text Trung và màu/rank description giữ nguyên Bank page nguồn; không dịch tự
  sáng tạo vì source hard-code các chuỗi này.
- Installer chỉ dùng Unity Editor serialization API, không sửa YAML thủ công.

## Kiểm chứng

- `Meowdoku.Core` compile sạch bằng Unity 6000.3.19f1 Roslyn response file.
- `Meowdoku.Gameplay`, `Meowdoku.Editor` và
  `Meowdoku.EditModeTests` compile sạch bằng response file đã thay reference
  sang Core/Gameplay vừa build trong `Temp/CodexCompileBank`.
- Fixture khóa union scalar, clone, initial/back state, hard-tier list, exact
  launch keys/value cho Regular/LK Modified/SP, invalid index và cấu trúc prefab.
- Chưa tuyên bố Test Runner pass vì lượt này không điều khiển Unity GUI.

## Trạng thái bàn giao để Refresh/Test

- `BankBrowserPagePrefabInstaller` sẽ tạo
  `Assets/_Project/Prefabs/UI/BankPage.prefab` khi Unity trở lại Edit Mode và
  Auto Refresh xong, sau đó `UIRegistryAssetInstaller` thêm `UiName.Bank`.
- Trước Refresh cuối, prefab/registry Bank chưa được tính là bằng chứng hoàn tất.
- Bản Godot không có nút Home mở Bank; Bank được mở từ Return Bank trong Game
  hoặc debug command. Vì Game hiện chưa được bọc/đăng ký thành UI page, không
  tạo nút Home giả chỉ để demo. Route Bank → Game/Prev/Next/Return Bank sẽ được
  kiểm thử sau khi Game UI composition hoàn tất.
