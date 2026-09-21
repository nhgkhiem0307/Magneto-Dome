# Settings UI

```
sprites/   21 sprite
mockups_v2/         12_settings.png · 13_leave_confirm.png
```

## Sprite và border

| Sprite | Kích thước | L R T B | Image Type |
|---|---|---|---|
| `panel` | 200×200 | 44 | Sliced |
| `row` | 96×96 | 26 | Sliced |
| `row_hover` | 96×96 | 26 | Sliced |
| `btn_normal` / `_hover` / `_pressed` / `_disabled` | 116×116 | 36 | Sliced |
| `btn_danger_normal` / `_hover` | 116×116 | 36 | Sliced |
| `dd_normal` / `dd_hover` | 96×96 | 26 | Sliced |
| `dd_template` | 96×96 | 26 | Sliced |
| `dd_item_hover` | 48×48 | 14 | Sliced |
| `slider_track` | 64×14 | **L8 R8 T0 B0** | Sliced |
| `slider_fill` | 64×14 | **L8 R8 T0 B0** | Sliced |
| `slider_fill_dim` | 64×14 | **L8 R8 T0 B0** | Sliced |
| `slider_handle` | 56×56 | 0 | Simple |
| `toggle_off` / `toggle_on` | 52×52 | 0 | Simple |
| `toggle_check` | 40×40 | 0 | Simple |
| `dd_arrow` | 40×40 | 0 | Simple |

**Ba sprite slider để T=0 B=0**, không phải 8 cả bốn cạnh. Thanh chỉ cao 14px, đặt T=B=8 thì 8+8=16 > 14 và Unity từ chối. Nó cũng chỉ cần giãn ngang.

`btn_*` border **36** (không phải 26) vì sprite có 10px padding cho glow cộng góc vát 12px.

## Dựng trong Unity

`SettingsPanel`: sprite `panel`, `1180 × 800`, `anchoredPosition (0, -40)`.

| Object | Font | Size | Màu |
|---|---|---|---|
| `Title` | Poppins Bold | 46 | `#EDEFF4` |

Thêm một TMP nhỏ dưới Title: `SYS // CONFIG`, Mono 14, `#2BE8FF`, cùng gạch cyan 56×3 bên dưới.

### Mỗi `Slider_x`

RectTransform `1088 × 84`, xếp dọc cách nhau **96px** (Vertical Layout Group, Spacing 12).

| Object | Sprite / Font | Số đo |
|---|---|---|
| `Background` | `row` (hoặc `row_hover` khi trỏ vào) | phủ kín 1088×84 |
| `Text` | Poppins Medium 24, `#EDEFF4` | `(30, -42)` từ mép trái trên |
| `ValueText` | Mono Bold 28, `#B8FBFF` | căn phải, `(-34, 0)` |
| `Fill Area` → `Fill` | `slider_fill` | cao 14 |
| `Handle Slide Area` → `Handle` | `slider_handle` | `56×56` |

Thanh trượt rộng **420**, đặt cách mép phải 118px (chừa chỗ cho `ValueText`).

Nhãn telemetry (`MASTER`, `MUSIC`, `SFX`, `SENS`) là TMP con thêm vào, Mono 13, `#7F8CA0`, ở `(30, -22)`. Hierarchy gốc chưa có — bỏ qua cũng được.

`Background` của Slider dùng `slider_track`, không dùng `row` — `row` là nền của cả dòng, `slider_track` là rãnh thanh trượt. Hai thứ khác nhau.

### `Dropdown_Resolution`

- nền `dd_normal`, `320 × 56`
- `Label`: Mono 23, `#EDEFF4`
- `Arrow`: `dd_arrow`, `40×40`, đặt cách mép phải 48px
- `Template`: nền `dd_template`, mỗi `Item` cao 48, `Item Background` dùng `dd_item_hover` (để trống khi không hover)

### `Toggle_Fullscreen`

- `Background`: `toggle_off` khi tắt, `toggle_on` khi bật, `52×52`
- con của `Background` là `Checkmark`: `toggle_check`, `40×40`, offset `(6, -6)`
- `Text`: Poppins Medium 24

Unity Toggle mặc định chỉ bật/tắt `Checkmark`. Muốn nền cũng đổi (viền cyan + glow) thì phải đổi sprite của `Background` bằng code trong `onValueChanged`.

### Nút

| Object | Sprite | Màu chữ |
|---|---|---|
| `CloseButton` | `btn_hover` | `#2BE8FF` |
| `LeaveButton` | `btn_danger_normal` | `#FF6B72` |

Cả hai `300 × 72`, Poppins Bold 25. `CloseButton` bên phải, `LeaveButton` bên trái — nút phá hoại đặt xa nút thoát thường.

`LeaveButton` có object con `Image` trong hierarchy — dùng cho icon `ic_logout` từ `hud_icons/`, `28×28`.

### `LeaveConfirmPanel`

`660 × 340`, sprite `panel`, đặt giữa màn hình. Phải có **scrim riêng** phủ lên cả SettingsPanel, không thì hai panel chồng nhau khó đọc — thêm một Image `screen_scrim` làm con đầu tiên của `LeaveConfirmPanel`.

| Object | Font | Màu |
|---|---|---|
| `Title` | Poppins Bold 40 | `#EDEFF4` |
| `Text` | Poppins Regular 21 | `#7F8CA0` |
| `CountText` | Mono 19 | `#4E5A6B` |
| `ConfirmButton` | Poppins Bold 24, sprite `btn_danger_hover` | `#FF6B72` |
| `CancelButton` | Poppins Bold 24, sprite `btn_normal` | `#EDEFF4` |

Nút `250 × 84`. **`CancelButton` bên trái, `ConfirmButton` bên phải** — và `Cancel` nên là nút được chọn sẵn khi panel mở, để bấm Enter theo phản xạ không làm mất vòng đấu.

`CountText` là đồng hồ đếm ngược tự đóng. Thêm một TMP nữa cho dòng `WARN // LEAVE MATCH`, Mono 14, `#FF6B72`, đặt ngay dưới Title.
