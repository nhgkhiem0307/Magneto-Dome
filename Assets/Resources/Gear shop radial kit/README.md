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

| Sprite | Kích thước | L | R | T | B | Dùng cho |
|---|---|---|---|---|---|---|
| `panel_main` | 172×172 | 48 | 48 | 48 | 48 | Panel chính mỗi màn hình |
| `card` | 124×124 | 32 | 32 | 32 | 32 | Khối con trong panel, khung team |
| `toast_error` | 124×124 | 32 | 32 | 32 | 32 | Nền StatusErrorText |
| `slot_red / slot_blue / slot_empty` | 132×132 | 32 | 32 | 32 | 32 | Slot người chơi |
| `btn_primary_*` (4 state) | 116×116 | 28 | 28 | 28 | 28 | Nút chính |
| `btn_secondary_*` (4 state) | 116×116 | 28 | 28 | 28 | 28 | Nút phụ |
| `btn_danger_*` (3 state) | 116×116 | 28 | 28 | 28 | 28 | Nút rời phòng |
| `btn_ghost_hover` | 108×108 | 24 | 24 | 24 | 24 | Nút viền, state hover |
| `input_focus` | 100×100 | 20 | 20 | 20 | 20 | InputField khi focus |
| `well` | 96×96 | 18 | 18 | 18 | 18 | Vùng lõm: nền Scroll View |
| `btn_ghost_normal` | 92×92 | 16 | 16 | 16 | 16 | Nút viền, state thường |
| `row_normal / hover / selected` | 88×88 | 16 | 16 | 16 | 16 | Dòng trong danh sách phòng |
| `input_normal` | 88×88 | 14 | 14 | 14 | 14 | InputField thường |
| `pill`, `pill_danger` | 56×56 | 26 | 26 | 26 | 26 | Badge số người, trạng thái |
| `scroll_track`, `scroll_handle` | 16×80 | **0** | **0** | 8 | 8 | Thanh cuộn dọc |
| `divider` | 8×8 | 0 | 0 | 0 | 0 | Đường kẻ — để Image Type `Simple` |

Thanh cuộn chỉ rộng 16px nên không đặt L/R được (8+8 = 16, phần giữa còn 0px, Unity không cho). Nó cũng chỉ cần giãn theo chiều dọc nên để L=R=0 là đúng.

Quy tắc chung khi tự thêm sprite mới: **L + R phải nhỏ hơn chiều rộng ảnh, T + B phải nhỏ hơn chiều cao.**

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

---

# Phần 2 — HUD trong trận

```
hud_sprites/   13 sprite
hud_icons/     9 icon (shield, shield_full, ammo, ammo_heavy, spike, energy, money, skull, timer)
mockups/       06_hud_combat · 07_hud_buy_phase · 08_hud_dead
```

HUD chơi theo luật khác lobby: nó nằm **đè lên gameplay**, nền phía sau lúc sáng lúc tối và luôn thay đổi. Nên ba quy tắc:

1. **Không dùng panel đặc.** Chỉ scrim mờ (`hud_scrim`, `hud_pill`) ở đúng chỗ có chữ.
2. **Mọi chữ trên HUD phải có viền tối.** Trong TMP: bật `Outline` ở material, `Thickness` ~`0.18`, màu `#06080C`. Không có viền thì chữ trắng biến mất khi ngắm vào tường sáng.
3. **Số liệu dùng JetBrains Mono, không dùng Poppins.** Máu tụt từ 100 → 99 mà font tỉ lệ thì cả cụm số nhảy qua nhảy lại, rất khó chịu khi nhìn bằng khoé mắt. Mono thì mọi chữ số rộng bằng nhau, đứng yên.

## Bảng 9-slice cho HUD

| Sprite | Kích thước | L R T B | Image Type |
|---|---|---|---|
| `hud_scrim` | 72×72 | 16 | Sliced |
| `hud_scrim_edge` | 72×72 | 16 | Sliced |
| `hud_pill` | 60×60 | 28 | Sliced |
| `hud_pill_active` | 60×60 | 28 | Sliced |
| `bar_track` | 40×40 | 10 | Sliced |
| `bar_fill` | 40×40 | 10 | **Filled** |
| `bar_fill_thin` | 28×28 | 7 | **Filled** |
| `crosshair` | 64×64 | 0 | Simple |
| `polarity_ring` / `_pos` / `_neg` | 96×96 | 0 | Simple |
| `announce_bar` | 120×72 | 0 | Simple |
| `dead_overlay` | 384×216 | 0 | Simple |

`bar_fill` và `bar_fill_thin` **màu trắng** — đổi màu bằng ô `Color` của component Image, không cần sprite riêng cho từng màu:

| Trạng thái | Màu |
|---|---|
| Máu bình thường | `#3ECF8E` |
| Máu dưới 35 | `#EF525A` — đổi bằng code, đây là cảnh báo quan trọng nhất trên HUD |
| Giáp | `#9FB0CC` |
| Giáp = 0 | `#4C5566` |

Với Image Type = Filled: `Fill Method` = `Horizontal`, `Fill Origin` = `Left`, rồi set `fillAmount = hp / 100f`.

## Gán theo hierarchy HUD của bạn

| GameObject | Sprite / Font | Ghi chú |
|---|---|---|
| `Crosshair` | `crosshair` | để 72×72, đừng để 16×16 — quá nhỏ, mất hút trên nền sáng |
| `PolarityIndicator` | `polarity_ring_pos` / `_neg` / `polarity_ring` | xem ghi chú vị trí bên dưới |
| `PolarityText` | Poppins Bold 38 | `+` màu `#FFC24B`, `−` màu `#7BA6FF`, trung tính `·` màu `#8B94A7` |
| `Announcement` | nền `announce_bar` (1100×92) | Poppins Bold 46, trắng |
| `RoundNumber` | Poppins Medium 16 | viết HOA, character spacing `20`, màu `#A7B0C2` |
| `Score` | nền `hud_scrim` 420×84 | |
| `RedScore` / `BlueScore` | JetBrains Mono Bold 46 | `#EF525A` / `#4C86FF` |
| `Divider` | Poppins Regular 30 | `#5C6577` |
| `TimerText` | nền `hud_pill` 164×54 · Mono Bold 34 | |
| `PhaseText` | Poppins Medium 17 | HOA, spacing `20`, màu `#FFC24B` |
| `HealthBar/Background` | `bar_track` 420×32 | |
| `HealthBar/Fill` | `bar_fill`, Filled | chừa 4px mỗi cạnh so với Background |
| `HealthText` | Mono Bold 44 | đặt bên phải thanh, cách 26px |
| `ArmorGroup/Background` | `bar_track` 420×14 | cách thanh máu 40px |
| `ArmorGroup/Fill` | `bar_fill_thin`, Filled | |
| `ArmorText` | Mono Bold 26 | kèm `ic_shield` 24px |
| `EnergyDrinkGroup` | `hud_pill` cao 54 | |
| `EnergyDrinkGroup/Icon` | `ic_energy` 28px | |
| `MoneyText` | Mono Bold 36 `#FFC24B` | kèm `ic_money` 30px |
| `NormalAmmo` | `hud_pill` / `hud_pill_active` | `ic_ammo` — khối vuông |
| `HeavyAmmo` | như trên | `ic_ammo_heavy` — khối tạ 1000 tấn |
| `SpikeAmmo` | như trên | `ic_spike` — quả cầu gai |

Ba icon này xuất ở 128×128 thay vì 64×64 như phần còn lại, vì `ic_ammo_heavy` có chữ bên trong cần thêm pixel để không bị nhoè. Import settings vẫn y hệt, chỉ khác kích thước file.
| `DeadPanel/Overlay` | `dead_overlay`, Anchor Stretch | đã có sẵn vignette, không cần thêm ảnh đen |
| `DeadPanel/Text` | Poppins Bold 52 | kèm `ic_skull` 92px màu `#EF525A` phía trên |

Loại đạn đang cầm dùng `hud_pill_active` + chữ màu accent; hai loại còn lại dùng `hud_pill` + chữ `#8B94A7`. Chỉ **một** chip sáng tại một thời điểm.

## Hai chỗ mình đổi so với hierarchy của bạn

**PolarityIndicator: `Pos (0, -50)` → `Pos (0, -110)`.** Ở -50 thì vòng tròn 40-80px đè lên nhánh dưới của tâm ngắm, vừa che tầm nhìn vừa làm người chơi tưởng nó là một phần của reticle. Đẩy xuống -110 là đủ tách bạch mà mắt vẫn thấy được bằng thị giác ngoại vi. Nếu bạn muốn giữ -50 thì phải bỏ phần nền tối đặc của vòng, chỉ để lại viền mảnh.

**Crosshair: ~16×16 → 72×72.** Sprite gốc 64px có sẵn viền đen bao quanh; ở 16px thì viền đó co lại còn chưa tới 1px và tan mất trên nền sáng. Kích thước hiển thị 72 cho nhánh dài khoảng 13px — đúng tầm của game bắn súng.

## Về dấu tiếng Việt

Trong mockup mình render bằng file Poppins có sẵn trên máy dựng, thiếu vài glyph nên `Ạ Ị Ộ` hiện thành ô vuông. Đó là lỗi của file font ở đây, không phải lỗi thiết kế — bản Poppins tải từ Google Fonts có đủ. Nhớ dán dải ký tự tiếng Việt vào Custom Character List khi tạo Font Asset (xem phần 4 ở trên).

---

# Bản v2 — hiện đại hơn

```
sprites_v2/      30 sprite, TÊN và KÍCH THƯỚC y hệt bản gốc
hud_sprites_v2/  13 sprite HUD
mockups_v2/      cả 8 màn + style guide dựng lại theo v2
```

Bản gốc mang ngôn ngữ mobile casual: bo góc lớn, gradient, viền xám đặc, gờ sáng ở mép trên tạo cảm giác nổi khối. v2 đổi sang ngôn ngữ kỹ thuật hơn:

| | Bản gốc | v2 |
|---|---|---|
| Bo góc panel | 20 | **6** |
| Bo góc nút | 12 | **4** |
| Góc trên trái | bo tròn | **cắt vát 14–18px** |
| Nền | gradient dọc | **màu phẳng** |
| Viền | `#39404F` đặc | **hairline trắng alpha 26** |
| Gờ sáng mép trên | có | **bỏ** |
| Nút chính | gradient vàng | **vàng phẳng + gạch đậm dưới đáy** |
| Vạch màu team | thanh ngang trên đầu | **thanh dọc bên trái** |
| Nhãn nhỏ | Poppins Medium | **JetBrains Mono, HOA, spacing 3.2** |
| PolarityIndicator | vòng tròn | **ô vuông cắt vát 2 góc chéo** |
| Nền tối | `#10121A` | `#0C0E14` |
| Panel | `#1A1E27` | `#161A22` |
| Card | `#252B37` | `#1E232C` |

Bảng màu chức năng giữ nguyên: accent `#FFC24B`, team đỏ `#EF525A`, team xanh `#4C86FF`, sẵn sàng `#3ECF8E`, lỗi `#FF6B72`.

Ba thứ tạo nên cảm giác "hiện đại" ở đây, nói rõ để bạn tự chỉnh sau này:

1. **Góc cắt vát.** Chỉ cắt góc trên-trái, không cắt cả bốn. Cắt cả bốn thành ra hình bát giác, trông như hộp thoại cũ. Cắt một góc thì nó là dấu hiệu có chủ đích.
2. **Viền hairline thay vì viền xám.** Viền màu đặc luôn đọc ra là "cái khung"; viền trắng alpha thấp đọc ra là "mép của một bề mặt". Chênh lệch nhỏ nhưng đổi hẳn cảm giác.
3. **Tương phản cỡ chữ mạnh hơn.** Số to hẳn, nhãn nhỏ hẳn và dùng mono giãn chữ. Bản cũ mọi cỡ chữ đều na ná nhau nên phẳng lì về mặt thị giác.

## Đổi sang v2 trong Unity

Tên file, kích thước ảnh và số 9-slice của v2 **giống hệt** bản gốc. Nên nếu bạn đã import bản gốc và nhập border rồi:

Copy đè file từ `sprites_v2/` lên `sprites/` trong project — Unity giữ nguyên file `.meta` theo đường dẫn, nghĩa là **không phải nhập lại border, không phải gán lại sprite vào Image**. Mở lại Unity là thấy giao diện mới.

Nếu muốn giữ cả hai để so, import vào thư mục riêng rồi gán tay.

Bảng màu có đổi vài mã nền, nhớ sửa lại Color của Canvas background và các Text theo bảng trên.

---

# Ally marker

`hud_sprites/ally_marker.png` · **96×96** · Image Type `Simple` · border 0 · Mesh Type `Full Rect`

Tam giác ngược, màu dạ quang `#59FF40`.

## Về màu

`#59FF40` là xanh lá neon độ bão hoà rất cao. Nó nổi trên nền cỏ nhờ **độ sáng** chứ không nhờ khác tông màu — cỏ low-poly của bạn tối và ngả vàng, marker thì sáng chói và ngả vàng-xanh. Trên trời và núi xám thì tách rõ.

Không đụng đỏ `#EF525A` và xanh dương `#4C86FF` của hai đội trong lobby.

## Glow đã nướng sẵn trong ảnh

Không cần bật Bloom trong post-processing. Ba lý do:

1. Bloom là hiệu ứng toàn màn hình — bật lên thì mọi thứ khác trên HUD cũng sáng theo, kể cả những chỗ bạn không muốn.
2. Marker nằm trên Canvas Overlay thì bloom của URP không chạm tới được.
3. Glow nướng sẵn cho kết quả giống nhau trên mọi máy, mọi setting đồ hoạ.

Vì glow chiếm phần rìa ảnh nên **sprite 96×96 nhưng phần tam giác đặc chỉ khoảng 40×40 ở giữa**. Khi canh vị trí, tính theo tâm ảnh chứ đừng tính theo mép.

## Kích thước hiển thị

| Khoảng cách | Size |
|---|---|
| Gần (dưới 15m) | 96 px |
| Trung bình | 64 px |
| Xa | 44 px |
| Rất xa (trên 60m) | 32 px |

Đừng cho nhỏ hơn 28px — dưới ngưỡng đó quầng glow co lại thành một chấm mờ và mất hẳn tác dụng.

## Import settings

Giống mọi sprite HUD khác, nhưng **Alpha Is Transparency phải tick** — glow là vùng alpha thấp, không tick thì rìa bị viền đen xấu.

---

# Radial menu + Shop — hologram

```
item_icons/       5 icon vật phẩm, 128×128
radial_sprites/   7 sprite
shop_sprites/     7 sprite
mockups_v2/       10_radial_menu.png · 11_shop.png
```

Phong cách: hologram/tech, màu nhấn **cyan `#2BE8FF`**. Chi tiết dồn ra viền và góc (ngoặc góc, vòng khắc vạch, chữ telemetry); phần giữa để trống cho icon và số. Đó là thứ giữ cho "chi tiết" không thành "rối" trên một UI mà người chơi chỉ liếc 2 giây.

## Bốn thứ tạo nên chất hologram

1. **Vạch scan ngang** nướng sẵn trong mọi sprite nền — alpha 13/255, chu kỳ 4px. Tín hiệu rẻ nhất và mạnh nhất.
2. **Vòng khắc vạch** quanh mép slot: 48 vạch, cứ 4 vạch có một vạch dài. Nhìn ra "thiết bị đo", không phải nút bấm.
3. **Ngoặc góc** — ở slot đang chọn, ở viền màn hình, ở góc panel shop. Không bao kín, chỉ gợi ý khung.
4. **Chữ telemetry**: mã ngắn viết hoa (`MED`, `FUEL`, `EMB`, `STIM`), JetBrains Mono 13–15px, màu cyan hoặc xám. Đây là thứ làm UI "có vẻ đang chạy" thay vì tĩnh.

## Vì sao đổi sang cyan

Cyan là màu của điện/từ trường — hợp `Magneto-Dome` hơn xanh lá. Quan trọng hơn: nó tách hẳn khỏi đồng cỏ và tán cây low-poly, trong khi xanh neon `#59FF40` nằm cùng họ màu với nền.

`#59FF40` vẫn giữ cho **ally marker** — marker phải khác hẳn UI để không bị nhầm là nút bấm.

## Radial menu — sprite

| Sprite | Kích thước | Border | Image Type |
|---|---|---|---|
| `slot_normal` | 180×180 | 0 | Simple |
| `slot_hover` | 180×180 | 0 | Simple |
| `slot_disabled` | 180×180 | 0 | Simple |
| `reticle_ring` | 760×760 | 0 | Simple |
| `hub` | 260×260 | 0 | Simple |
| `select_pointer` | 220×220 | 0 | Simple |
| `screen_scrim` | 960×540 | 0 | Simple |

`reticle_ring` là vòng lớn phía sau toàn bộ menu, hiển thị **820×820**. Cho nó xoay chậm (khoảng 20 giây một vòng) là menu sống hẳn lên — một dòng `transform.Rotate(0,0,-18*Time.deltaTime)`.

## Radial menu — số đo

Tâm ở `(0, -24)`. Bốn slot:

| GameObject | anchoredPosition |
|---|---|
| `Slot_EnergyDrink` | `(0, 318)` |
| `Slot_Bandage` | `(330, 0)` |
| `Slot_Gasoline` | `(0, -318)` |
| `Slot_EMBarrier` | `(-330, 0)` |

Trái/phải xa hơn trên/dưới vì màn hình rộng hơn cao — để bằng nhau thì nhìn ra hình bầu dục.

`Slot_x`: RectTransform `214 × 214`.
- `Icon` — `70×70` (chọn: `78×78`), vị trí `(0, 14)`
- `Count` — Mono Bold 27, vị trí `(0, -44)`, format `x{n}`

Mã telemetry (`STIM`/`MED`/`FUEL`/`EMB`) ở `(0, 52)`, Mono 14. Tên vật phẩm ở `(0, -133)`, Poppins Medium 19. Cả hai là TMP con thêm vào, hierarchy gốc chưa có.

Hub giữa: `286×286`, ba dòng — mã (Mono 17, cyan), tên (Poppins Bold 30, tự thu nhỏ nếu dài), số lượng (Mono 30, `#B8FBFF`).

| Trạng thái | Sprite | Icon | Count |
|---|---|---|---|
| Đang chọn | `slot_hover` | `#2BE8FF` | `#2BE8FF` |
| Còn hàng | `slot_normal` | `#EDEFF4` | `#EDEFF4` |
| Hết | `slot_disabled` | `#4E5A6B` | `#4E5A6B` |

## Shop — sprite

| Sprite | Kích thước | Border | Image Type |
|---|---|---|---|
| `shop_panel` | 200×200 | 44 | Sliced |
| `btn_normal` / `_hover` / `_pressed` / `_disabled` / `_owned` | 140×140 | 40 | Sliced |
| `pill` | 80×80 | 38 | Sliced |

Border của card là **40** chứ không phải 26 như bản trước, vì góc vát 14px cộng padding glow 12px cần chỗ. Để 26 thì góc vát bị kéo méo.

`ShopButton_x` là Button nên dùng được **Sprite Swap**.

## Shop — số đo

- `ShopPanel`: `1620 × 660`, `anchoredPosition (0, 96)`
- `Text_Money` / `Text_Timer`: nền `pill` `246 × 70`. Mỗi pill hai dòng — nhãn nhỏ (`CREDIT` / `TIMER`, Mono 13, `#7F8CA0`) và giá trị bên dưới.
- `ItemContainer`: Horizontal Layout Group, `Spacing = 26`, Child Force Expand tắt
- `ShopButton_x`: Layout Element, `Preferred Width = 276`, `Preferred Height = 386`

Trong mỗi nút:

| Object | Vị trí | Font |
|---|---|---|
| mã telemetry | góc trên trái `(34, -32)` | Mono 14 |
| số đang có | góc trên phải `(-34, -32)` | Mono 14 |
| `Icon` | `(0, 128)`, `112×112` | — |
| đường kẻ ngang | `y = -22` | 1px |
| `Name` | `(0, -54)` | Poppins Medium 22 |
| `Price` | `(0, -104)` | Mono Bold 38 |
| phím tắt | `(0, -156)` | Mono 21 |

| Trạng thái | Name | Price |
|---|---|---|
| Đủ tiền | `#EDEFF4` | `#FFC24B` |
| Không đủ | `#4E5A6B` | `#FF6B72` |
| Hover | `#EDEFF4`, Icon `#2BE8FF` | `#FFC24B` |

Giữ **vàng cho giá tiền**. Tiền là thứ duy nhất trong shop có ý nghĩa số học thật, cho nó màu riêng thì mắt nhảy thẳng vào đó.

## Viền màn hình

Bốn ngoặc góc ở `48px` từ mép, dài 30px, dày 2px, màu cyan. Cộng với hai dòng chữ telemetry ở góc trên. Đây là thứ khiến toàn màn hình đọc ra là "đang xem qua thiết bị", chứ không phải "có cái menu dán lên màn hình".

Trong Unity: 4 Image nhỏ hoặc một Image duy nhất kéo giãn với sprite khung — cách sau nhẹ hơn nhưng phải làm thêm sprite 9-slice; 4 Image thì linh hoạt hơn khi đổi tỉ lệ màn hình.
