# R15 Rank Gift Chest and Frame Flight Report — 2026-08-11

## Nguồn đối chiếu

- `scripts/module/rank_activity/view/rank_gift_cell.gd`
- `scripts/module/rank_activity/ui/rank_gift_cell.tscn`
- `scripts/module/award/view/award_page.gd`
- `assets/prefab/effect_score_trail.tscn`
- `assets/prefab/effect_score_burst.tscn`
- `scripts/module/profile/ui/avatar_cell.tscn`

## Sai lệch đã tìm thấy

- Rank Gift Unity cũ cho thao tác sau 1,6 giây, trong khi nguồn chỉ nhận input sau toàn bộ `Appear1` 3,45 giây hoặc `Appear3` 3,3666666 giây.
- Pha rương từng chuyển ngay sang item. Nguồn giữ `Open1` đủ 2 giây và chỉ phát tín hiệu mở phase item tại 0,8834 giây.
- Podium/rương chỉ là bố cục tĩnh; thiếu stagger ghế/avatar, chest bounce/idle, bốn celebration burst và mapping hình rương theo hạng.
- Frame mới chưa bay về Profile; thiếu trail, arrival burst, mask fade sớm và profile shake.

## Phần đã port

- `RankGiftView` đọc `group` từ display parameters, giữ group 1/2 có rương và group 3 không rương; place 1/2/3 ánh xạ chest tier 3/2/1 như nguồn.
- Port timeline `Appear1`/`Appear3`: backdrop/Win fade, podium scale, ghế/avatar stagger, gold avatar bounce, bốn firework burst, chest appear/bounce/idle và Collect fade. Input chỉ mở sau khi sequence hoàn tất.
- Port `Open1`: khóa input, fade podium/Win/Collect, chest punch/fall, burst tại 0,9 giây, notify phase item tại 0,8834 giây và ẩn Rank Gift sau tổng 2 giây. Trang Award vẫn tồn tại trong lúc chuyển pha.
- `FrameAwardFlightView` tạo 16 đoạn trail, glow và 12 burst star bằng UGUI. Flight bắt đầu ở 0,3 giây, kéo dài 0,45 giây, dùng hai cubic Bézier nguồn: X `[0.389,-0.006,0.933,1]`, Y `[0.544,-0.46,1,1.001]`.
- Khi Home hiện, frame mới bay tới serialized `ProfileEntry`, mask fade 0,2 giây từ lúc flight bắt đầu và Profile shake `1 → 1,15 → 1` lúc đến. Frame đã sở hữu hoặc không có target giữ nhánh effect cũ.
- Project không có Spine Unity runtime và UGUI không có `Line2D`; đây là adapter bắt buộc dùng sprite nguồn, Image và DOTween, không phải thiết kế gameplay mới.
- Prefab được nâng cấp idempotent bằng Unity Prefab API theo nhánh `RankGiftRoot/Effects` và `FrameAddEffect/Flight`; giữ `.meta`, GUID và serialized reference. Tween chạy unscaled và cleanup khi hide/disable/destroy. Không thêm runtime log.

## Kiểm chứng

- Contract test bao phủ duration/cue nguồn, cubic midpoint và toàn bộ serialized prefab reference.
- Lượt PlayMode đầu phát hiện fixture cũ đòi item phase ngay sau một frame; fixture được sửa để chờ cue 0,8834 giây thay vì làm sai production timing.
- Unity EditMode Test Runner: **530 passed, 0 failed, 0 skipped, 0 inconclusive**, duration **66,924 giây** ở lượt cuối.
- Unity PlayMode Test Runner: **17 passed, 0 failed, 0 skipped, 0 inconclusive**, duration **257,562 giây** ở lượt cuối.

## Còn lại

- Nối SFX rương vào audio service khi dependency audio/presenter được khép mà không tạo runtime lookup.
- So pixel/video với bản gốc và kiểm tra profiler/touch/notch trên thiết bị ở R17.
- Hard-kill/process restart và soak nhiều kỳ vẫn thuộc cổng durability R15/R17.
