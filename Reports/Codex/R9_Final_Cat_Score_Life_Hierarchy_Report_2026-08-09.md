# R9 — Final cat, Score value, Life HUD và Gameplay Hierarchy

## Kết quả

- Xác minh domain trước khi sửa UI: mèo thứ tư trên fixture 4×4 phát `CorrectCat`, cộng 840 điểm, `ScoreAfter = 2880`, sau đó mới chuyển `Won`.
- Sửa dòng tổng điểm bị ẩn: Godot Label cho glyph 58 px tràn khung 60 px; Unity `Text` dùng `VerticalOverflow.Truncate` đã cắt cả dòng số. Adapter nay dùng `Overflow` nhưng giữ font size và khung nguồn.
- Win settlement của variant không có fly nay chờ tối thiểu 1,0166667 giây để bubble mèo cuối không bị terminal UI che.
- Port ba `LifeSlot` từ `fish_slot.tscn`: cá đầy/cá mờ, mất mạng theo slot `LivesBefore - 1`, lost 0,8 giây, silent 0,3 giây, revive 0,5 giây.
- Life bonus phát bubble từ vị trí slot, làm mờ cá theo thứ tự nguồn và cập nhật tổng điểm.
- Installer Scene tổ chức lại cây chức năng qua Unity Editor API, không sửa YAML bằng suy đoán:

```text
GameplayScene
├── Systems
│   └── GameplayManager
└── Canvas
    ├── Gameplay
    │   └── Board
    ├── HUD
    │   ├── TopBar
    │   │   └── ScoreDisplay
    │   ├── Feedback
    │   └── Lives
    └── Overlays
```

## Nguồn đối chiếu trực tiếp

- `scripts/module/game/ui/game_page.tscn`: Header/ScoreDisplay/CatHeartRow/HeartBg/LifeSlot layout.
- `scripts/module/game/ui/compont/fish_slot.tscn`: asset và animation track LifeSlot.
- `scripts/module/game/view/life_slot.gd`: `show_alive`, `show_lost`, `play_revive`.
- `scripts/module/game/view/base_game_page.gd`: score event, `lost_index = _lives - 1`, refresh/lost/life bonus sequence.
- `scripts/module/game/view/combo_feedback_view.gd`: score bubble và score label update.

## Kiểm chứng

- Gameplay runtime compile sạch bằng Unity Roslyn response set cộng hai source mới.
- Editor installer compile sạch.
- `GameplayFeedbackTests`: 5/5, gồm mèo thứ tư.
- `GameplayFeedbackPresentationPlanTests`: 4/4, gồm non-fly final bubble gate.
- Baseline trước thay đổi: 250/250. Tổng hiện có 252 case; full Unity suite cần chạy lại sau Refresh vì runner ngoài Unity toàn bộ suite vượt timeout, không có test failure được báo.

## Còn lại sau bước này

- PlayMode xác nhận số tổng, bubble mèo cuối, thứ tự mất cá và Hierarchy sau Refresh.
- Nền pill bo góc và particle/glow cá chưa dựng vì cần adapter bo góc/VFX nguồn; không tự thay bằng hình khác.
- Multiplier/skill pair và score trail/burst thuộc bước tiếp theo.
