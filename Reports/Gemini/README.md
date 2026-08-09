# Quy ước báo cáo Gemini

Thư mục này chỉ chứa báo cáo đọc-only do Gemini tạo để cung cấp dữ liệu đầu vào cho Codex.

## Phạm vi được phép

- Gemini chỉ được tạo hoặc cập nhật file báo cáo `.md` trong `Reports/Gemini`.
- Không được sửa `PORTING_ROADMAP.md`, `Docs`, `Assets`, `Packages`, `ProjectSettings` hoặc bất kỳ file mã nguồn nào.
- Không được tạo code, pseudocode, patch, prefab, scene, config hay asset.
- Báo cáo phải là kết quả quét/quan sát cơ học, có đường dẫn và số dòng hoặc bằng chứng cụ thể.

## Tên file

```text
GEM-<ROADMAP>-<SỐ THỨ TỰ>_<TÊN NGẮN>.md
```

Ví dụ:

```text
GEM-R3-001_game_state_persistence.md
GEM-R4-001_level_bank_inventory.md
GEM-R7-001_board_asset_dimensions.md
```

Mỗi `REPORT_ID` chỉ có một file. Gemini cập nhật đúng file đó nếu cần bổ sung, không tạo các bản `final`, `final2`, `new`.

## Trạng thái báo cáo

Đầu mỗi file phải có:

```text
REPORT_ID: GEM-R3-001
STATUS: COMPLETE | PARTIAL | BLOCKED
GENERATED_AT: YYYY-MM-DD HH:mm:ss
SOURCE_ROOT: D:\Projects\_GameExtract\Main_Meokdoku
```

`COMPLETE` chỉ có nghĩa là Gemini đã hoàn tất phạm vi quét được giao; không có nghĩa feature Unity đã hoàn thành.

## Cách Codex sử dụng

- Khi người dùng yêu cầu “làm tiếp”, Codex kiểm tra thư mục này trước.
- Báo cáo có mặt thì Codex dùng làm dữ liệu cho lượt kế tiếp và chỉ spot-check phần quan trọng/bất thường.
- Báo cáo chưa có thì Codex tiếp tục phần độc lập khác trong roadmap, không chờ.
- Chỉ Codex được cập nhật roadmap và quyết định một parity case hoặc giai đoạn đã hoàn thành.

