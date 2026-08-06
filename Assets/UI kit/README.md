# Lobby UI kit

Bộ UI cho lobby multiplayer — nền xám tối trung tính, accent hổ phách, viền mảnh + gradient nhẹ + bóng mềm.

```
lobby-ui-kit/
├── sprites/     30 sprite PNG, hầu hết là 9-slice
├── icons/       16 icon outline 64px, màu trắng để tint
├── mockups/     00 style guide + 5 màn hình 1920×1080
└── tools/       script Python sinh lại toàn bộ nếu bạn đổi màu
```

---

## 1. Import settings trong Unity

Chọn hết thư mục `sprites/` và `icons/`, đặt trong Inspector:

| Thuộc tính | Giá trị |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single |
| Mesh Type | **Full Rect** |
| Generate Mip Maps | Off |
| Wrap Mode | Clamp |
| Filter Mode | Bilinear |
| Compression | None (hoặc High Quality) |
| Alpha Is Transparency | ✅ |
| Pixels Per Unit | 100 |

`Mesh Type = Full Rect` là bắt buộc — để Tight thì 9-slice sẽ méo.

## 2. Bảng 9-slice

Mở Sprite Editor → kéo 4 đường guide, hoặc gõ số vào ô L / R / T / B. Số dưới đây đều bằng nhau cả 4 cạnh:

| Sprite | Kích thước | Border | Dùng cho |
|---|---|---|---|
| `panel_main` | 172×172 | **48** | Panel chính mỗi màn hình |
| `card` | 124×124 | **32** | Khối con trong panel, khung team |
| `well` | 96×96 | **18** | Vùng lõm: nền Scroll View |
| `input_normal` | 88×88 | **14** | InputField thường |
| `input_focus` | 100×100 | **20** | InputField khi focus |
| `btn_*` (mọi loại) | 116×116 | **28** | Nút, mọi state |
| `btn_ghost_normal` | 92×92 | **16** | Nút viền |
| `slot_red / blue / empty` | 132×132 | **32** | Slot người chơi |
| `row_*` | 88×88 | **16** | Dòng trong danh sách phòng |
| `toast_error` | 124×124 | **32** | Nền StatusErrorText |
| `pill`, `pill_danger` | 56×56 | **26** | Badge số người, trạng thái |
| `scroll_track`, `scroll_handle` | 16×80 | **8** | Thanh cuộn |
| `divider` | 8×8 | **2** | Đường kẻ |

Panel và button có **bóng mềm nằm trong sprite** (phần padding trong suốt quanh mép). Nghĩa là RectTransform sẽ to hơn phần thân nhìn thấy một chút: `panel_main` dư 26px mỗi cạnh, `btn_*` dư 12px. Cứ cộng thêm khi canh lề.

## 3. Canvas

- Canvas Scaler → **Scale With Screen Size**
- Reference Resolution `1920 × 1080`
- Screen Match Mode → **Match Width Or Height**, Match = `0.5`

Mọi số đo trong mockup đều theo hệ 1920×1080 nên copy thẳng được.

## 4. Font TMP

Tải từ Google Fonts rồi `Window → TextMeshPro → Font Asset Creator`:

- **Poppins** (Regular / Medium / Bold) — toàn bộ UI
- **JetBrains Mono Bold** — riêng mã phòng (`RoomIDText`), vì mono giúp phân biệt `0`/`O`, `1`/`I` khi đọc mã cho bạn bè

Khi tạo font asset nhớ bật **Include Vietnamese** hoặc dán dải ký tự tiếng Việt vào Custom Character List, không thì mất dấu.

Type scale:

| Vai trò | Font | Size |
|---|---|---|
| Tiêu đề màn hình | Poppins Bold | 46 |
| Tiêu đề panel | Poppins Bold | 36 |
| Nhãn nút chính | Poppins Bold | 27 |
| Tên người chơi, nội dung | Poppins Medium | 24 |
| Chữ phụ, mô tả | Poppins Regular | 20 |
| Eyebrow / nhãn nhỏ | Poppins Medium | 15, spacing 2.6 |
| Mã phòng | JetBrains Mono Bold | 44–58 |

## 5. Gán sprite theo hierarchy của bạn

| GameObject | Sprite | Ghi chú |
|---|---|---|
| `NamePanel` | `panel_main` | |
| `NamePanel/NameInput` | `input_normal` → `input_focus` | đổi sprite ở event OnSelect/OnDeselect |
| `NamePanel/ConfirmBtn` | `btn_primary_*` | |
| `MainButtonsPanel` | `panel_main` | |
| `MatchmakingButton` | `btn_primary_*` | nút chính duy nhất của màn — icon `ic_play` |
| `CreateRoomPanelButton` | `btn_secondary_*` | icon `ic_plus` |
| `OpenRoomListButton` | `btn_secondary_*` | icon `ic_globe` |
| `JoinCodeInput` | `input_normal` | font mono, `Character Limit = 6`, ép hoa |
| `JoinByCodeButton` | `btn_secondary_*` | |
| `CreateRoomPanel` | `panel_main` | |
| `RoomIDText` | đặt trong `well` | mono, màu accent `#FFC24B` |
| `ConfirmCreateButton` | `btn_primary_*` | |
| `BackFromCreateButton` | `btn_secondary_*` vuông 56×56 + `ic_back` | |
| `RoomListPanel` | `panel_main` | |
| `Scroll View` (Viewport) | `well` | |
| item trong Content | `row_normal / hover / selected` | prefab dòng phòng |
| `Scrollbar Vertical` | track `scroll_track`, handle `scroll_handle` | rộng 14–16 |
| `BackFromListButton` | `btn_secondary_*` + `ic_back` | |
| `RoomLobbyPanel` | `panel_main` | |
| `PanelRedTeam` / `PanelBlueTeam` | `card` | thêm vạch màu đội ở header |
| slot người chơi (prefab) | `slot_red` / `slot_blue` / `slot_empty` | |
| `StartMatchButton` | `btn_primary_*` | |
| `SwitchTeamButton` | `btn_secondary_*` + `ic_swap` | |
| `LeaveRoomButton` | `btn_danger_*` + `ic_logout` | |
| `StatusErrorText` | nền `toast_error` + `ic_warning` | màu chữ `#FF7B7B` |

Button → **Transition: Sprite Swap**, kéo đúng bộ normal / hover / pressed / disabled. Nút có sẵn state nên không cần Color Tint.

## 6. Màu

```
Nền canvas    #10121A      Chữ chính     #E9ECF3
Panel         #1A1E27      Chữ phụ       #8B94A7
Card          #252B37      Chữ mờ        #5C6577
Viền          #39404F      Accent / CTA  #FFC24B
Team đỏ       #EF525A      Team xanh     #4C86FF
Sẵn sàng      #3ECF8E      Lỗi           #FF7B7B
```

Accent chọn màu hổ phách có lý do: nó không đụng đỏ lẫn xanh của hai đội, nên nút hành động chính luôn đọc ra là "hành động", không bị nhầm thành màu phe. Giữ **một nút accent mỗi màn** — còn lại dùng secondary, không thì accent mất tác dụng dẫn mắt.

## 7. Đổi màu / sinh lại

Sửa dict `P` trong `tools/gen_lib.py` rồi chạy:

```bash
cd tools && python3 gen_sprites.py && python3 gen_icons.py && python3 gen_mockups.py && python3 gen_styleguide.py
```

Cần Python 3 + Pillow. Đường dẫn output nằm ở đầu mỗi file, sửa lại cho khớp máy bạn.
