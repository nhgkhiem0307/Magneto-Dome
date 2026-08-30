# TIẾN ĐỘ — Magneto-Dome

> Ghi cho session sau. Đọc file này trước, rồi đọc [CLAUDE.md](CLAUDE.md) để nắm đặc tả và quy tắc.
> **Cập nhật:** 30/08/2026 · **Deadline:** ~16/09/2026 (còn ~17 ngày)

---

## ⭐ VIỆC CẦN LÀM — chốt ngày 30/08 (còn 17 ngày)

Đã build `.exe` thành công và test sơ bộ. Art ổn. **Toàn bộ code đã xong** —
việc còn lại gần như chỉ là kéo thả trong Unity và test.

### 🔴 Phải làm

| # | Việc | Ở đâu | Thiếu thì sao |
|---|---|---|---|
| 1 | **Chơi trọn một trận, 2 máy** | — | Rủi ro số 1. Khu chiếm đóng + hồi sinh + Quá Tải chưa từng chạy đủ một vòng có người thật |
| 2 | **Bắt đầu viết báo cáo** | — | Rủi ro số 2. Chưa viết chữ nào. Chương "Khó khăn & giải pháp" chép thẳng được từ 11 cái bẫy trong CLAUDE.md mục 5 |
| 3 | **Gán 5 icon vào 5 file `ItemData`** | `Assets/Items/Type/*.asset` | Shop + Radial Menu trống trơn. **~5 phút** — xem mục "Gán icon" bên dưới |
| 4 | **Dựng Shop UI + Radial Menu** | TestScene | Chưa có object nào. Cấu trúc đầy đủ ở mục bên dưới |
| 5 | Thêm **nút mở Settings** ở MenuScene | MenuScene | Hiện chỉ mở được bằng `Esc`, người chơi không đoán ra |
| 6 | Gán 2 ô âm thanh: `sfxRoundWin`, `sfxRoundLose` | AudioManager (MenuScene) | Thắng/thua round im lặng |

### 🟡 Nên làm

| # | Việc | Ghi chú |
|---|---|---|
| 7 | Cắt bớt `Resources/Environment/Nature/Textures` | **194MB**, 5 file PNG 21–23MB. CHƯA commit. Xoá texture không dùng TRƯỚC khi đưa vào git — sau này xoá cũng không lấy lại được dung lượng |
| 8 | `knockbackText` trên HUDController | Hiện hệ số `x2.4`, đang bỏ trống |
| 9 | Trang trí nốt: bụi, hoa, đá bay lơ lửng dưới đảo | Công cụ `EnvironmentScatter` đã sẵn |

### ⚫ Tuần cuối, đừng làm sớm

| # | Việc | Vì sao đợi |
|---|---|---|
| 10 | **Xoá phím debug `K`** — 3 chỗ: `NetworkInputData.cs`, `NetworkRunnerHandler.OnInput()`, `PlayerMagnetController.FixedUpdateNetwork()` | Còn cần để test vòng round một mình |
| 11 | Đặt lại `Points To Win = 5` nếu có hạ để test | |
| 12 | Build cuối, **bỏ tick** `Development Build` | |

### ✂️ ĐÃ QUYẾT ĐỊNH BỎ

**Room list / danh sách phòng.** `OnSessionListUpdated()` đang rỗng, `RoomItemUI.cs`
là file mẫu 16 dòng, và không chỗ nào gọi `JoinSessionLobby()` — tức là chưa làm gì cả,
tốn ~2 tiếng để hoàn thành.

Bỏ vì **vào phòng bằng mã code đã chạy tốt**. Room list chỉ là tiện lợi, không phải yêu cầu.
Hai tiếng đó đổi lấy việc người chơi khỏi gõ 4 ký tự, mà lại là code MẠNG chưa từng test —
đúng loại dễ đẻ bug ở tuần cuối. Với hội đồng, "vào phòng bằng mã như Among Us" nghe hoàn chỉnh.

*(Nếu muốn lấp chỗ trống: đổi `roomListPanel` thành một dòng chữ hướng dẫn nhập mã.)*

### ✅ Kiểm tra lại — tưởng thiếu nhưng ĐÃ CÓ

- **`SettingsUI` đã gắn ở CẢ HAI scene** *(xác nhận 30/08)* — việc nợ từ 24/08 đã xong.
  Nhưng **chưa có nút nào gọi `Toggle()`/`Open()`**, chỉ mở được bằng `Esc`. Xem việc #5.
- **5 icon Shop ĐÃ CÓ SẴN** ở `Assets/Resources/UI kit/Icons/`, chỉ là chưa gán. Xem việc #3.
- **Tên người chơi**: chuỗi hoàn chỉnh `nameInput` → `LocalPlayerName` →
  `RoomPlayer.Spawned()` → `RPC_SetPlayerInfo` → `NickName` *(networked)* → `UpdateLobbyUI()`.
  Nếu thấy hiện "Player" là do **không gõ tên trước khi vào phòng** — code có nhánh
  `if (!string.IsNullOrEmpty(nameInput.text))`.
- `ButtonClickSound` đã gắn ở **cả hai** scene.
- `SpectatorController` đã gắn (việc nợ từ 05/08).

---

## 📐 DỰNG UI CÒN THIẾU — hướng dẫn đầy đủ *(soạn 30/08)*

### Bước 0 — Gán icon (làm TRƯỚC, ~5 phút)

Icon đã tồn tại sẵn, chỉ chưa ai kéo vào. **Gán một lần là cả Shop lẫn Radial Menu
đều có icon**, vì cả hai script đều đọc `ItemData.icon` chứ không kéo `Image` bằng tay.

| File `ItemData` *(`Assets/Items/Type/`)* | Kéo icon *(`Assets/Resources/UI kit/Icons/`)* |
|---|---|
| `Shield Armor.asset` | `Icon_ShieldArmor` |
| `Energy Drink.asset` | `Icon_EnergyDrink` |
| `Bandage.asset` | `Icon_Bandage` |
| `Gasoline Canister.asset` | `Icon_GasolineCanister` |
| `EM Barrier Core.asset` | `Icon_EMBarrior` |

⚠️ Ảnh phải để **Texture Type = Sprite (2D and UI)** mới kéo vào ô `Sprite` được.

### 🛒 Shop UI

```
Canvas                          ← gắn script ShopUI VÀO ĐÂY
└── ShopPanel                   ← ô "Shop Panel"
    ├── Text_Money              ← ô "Money Text"    ($500)
    ├── Text_Timer              ← ô "Timer Text"    (còn 12s)
    └── ItemContainer           ← thêm Horizontal Layout Group
        ├── ShopButton_0 … ShopButton_4
```

Mỗi `ShopButton_x` = **Button** + component **`ShopItemButton`**, có 3 object con:

```
ShopButton_0  (Button + ShopItemButton)
├── Icon      (Image)     ← ô "Icon Image"
├── Name      (TMP_Text)  ← ô "Name Text"
└── Price     (TMP_Text)  ← ô "Price Text"
```

Không cần gõ tên/giá bằng tay — `ShopItemButton.Setup()` tự lấy từ `ItemData`.

Ô Inspector của `ShopUI`: `Shop Panel`, `Money Text`, `Timer Text`,
`Item Buttons` *(Size = 5)*, `Shop Key = B`.

**⚠️ THỨ TỰ 5 NÚT PHẢI ĐÚNG.** Nút thứ `i` mua món thứ `i` trong
`ShopManager.catalogue` *(trên `Player.prefab`)*. Thứ tự thật đã kiểm tra 30/08:

| Index | Món |
|---|---|
| 0 | Shield Armor |
| 1 | Energy Drink |
| 2 | Bandage |
| 3 | Gasoline Canister |
| 4 | EM Barrier Core |

Kéo sai thứ tự = bấm mua giáp lại ra nước tăng lực.

### ⭕ Radial Menu

```
Canvas                          ← gắn script RadialMenuController VÀO ĐÂY
└── RadialMenuUI                ← ô "Radial Menu UI"
    ├── Slot_EnergyDrink        ← đặt 4 ô quanh tâm màn hình:
    ├── Slot_Bandage               trên, phải, dưới, trái
    ├── Slot_Gasoline
    └── Slot_EMBarrier
```

Mỗi `Slot_x` cần **Image + CanvasGroup** trên chính nó, và 2 object con:

```
Slot_EnergyDrink   (Image + CanvasGroup)
├── Icon           (Image)     ← ô "Icon Image"
└── Count          (TMP_Text)  ← ô "Count Text"   (x2)
```

`RadialMenuController` → `Slots` **Size = 4**, mỗi phần tử điền 5 ô:
`Consumable Type` *(chọn đúng loại)*, `Slot Rect` *(chính `Slot_x`)*,
`Canvas Group`, `Slot Image` *(Image nền)*, `Icon Image`, `Count Text`.

Ngoài ra: `Item Data Source` **Size = 4** *(kéo 4 ItemData tiêu hao, thứ tự không quan trọng)*,
`Menu Key = Tab`, `Dead Zone Radius = 40`.

⚠️ **Bắt buộc có `CanvasGroup`** trên mỗi slot — script làm mờ ô hết hàng bằng
alpha `0.85` → `0.4`. Thiếu nó thì không phân biệt được còn hàng hay hết.

### ⚠️ Bẫy chung cho CẢ BA bảng UI (Shop, Radial, Settings)

**Script phải đặt trên CANVAS, KHÔNG đặt trên panel con.**

Cả ba script đều tự `SetActive(false)` panel của mình. Nếu script nằm *trên* panel đó
thì tắt panel = tắt luôn script → `Update()` ngừng chạy → bấm `B`/`Tab`/`Esc`
không mở lại được nữa. Đây là lỗi im lặng, không hiện gì ở Console.

Kéo reference cũng vậy: ô `On Click ()` của nút phải kéo **Canvas** vào, không kéo panel.

---
## 1. Tóm tắt một dòng

Game **chạy được trọn vẹn**: vào phòng → chia đội → đánh nhau → tính điểm → mua đồ → dùng đồ
→ có đội vô địch → về menu. **Toàn bộ code đã xong.** Việc còn lại là **map** và một ít dựng UI.

> ⚡ **09/08: đã đổi mục tiêu tối thượng sang CHẾ ĐỘ QUÁ TẢI.** Không còn thanh máu — trúng đòn
> nạp điện tích, càng nhiễm càng bị hất xa, chết chỉ khi rơi khỏi đảo. Code xong và đã biên dịch
> sạch (`dotnet build`, 0 error), **chưa test lần nào.** Đặc tả đầy đủ ở [CLAUDE.md](CLAUDE.md) mục 4.

> 🎬 **16/08: thêm rung camera + cơ chế ngủ đông cho vật thể.** Cả hai đều sinh ra từ việc bắt đầu
> dựng map thật (địa hình gồ ghề). Biên dịch sạch 0 error, **chưa test lần nào.** Chi tiết ở mục 2b.

> 👁️ **16/08 (buổi 2): ánh sáng + nhãn quan.** Bật post-processing (game đang để `Tonemapping = None`),
> tăng chất lượng bóng đổ, và thêm `PlayerVisuals` — outline phân biệt địch/bạn + quả cầu năng lượng
> ở tay thay cho model găng chưa có. Sửa xong 2 lỗi animation nháy "falling idle".
> **Aura sét và marker đồng đội CHƯA TẠO.** Xem mục 2b và danh sách việc nợ ở mục 3.

---

## 2b. Việc làm ngày 16/08 — CHƯA TEST

### 🎬 Rung camera *(file mới: `Scripts/Player/CameraShake.cs`)*

MonoBehaviour thuần, **cố ý không networked** — rung camera chỉ mình mình thấy, gửi qua mạng là
phí băng thông. Ba nguồn rung: **đi bộ** (nhấp nhô đầu), **Dash**, **bắn vật đang cầm**.

⚠️ **Nguyên tắc quan trọng: chỉ có MỘT chỗ được ghi vào transform của camera** — đó là
`FPSMovement.Render()`. `CameraShake` chỉ *tính ra* độ lệch rồi để `Render()` cộng vào.
Cho nó tự xoay camera thì hai bên tranh nhau ghi, ai chạy sau xoá công người trước, mà thứ tự
Unity gọi hàm thì không đoán được.

| Chi tiết kỹ thuật | Vì sao |
|---|---|
| Dash móc vào `OnDashPerformed()` **có sẵn**, không tạo đường mới | Rung thẳng trong `FixedUpdateNetwork` sẽ cộng trauma 5-6 lần mỗi cú do Fusion tua lại |
| Bắn vật phải thêm `FireCount` **`[Networked]`** | `FireGrabbedObject()` nằm sau `if (!HasStateAuthority) return;` nên chỉ chạy trên Host — máy Client bắn sẽ không rung gì cả |
| Dao động bằng **Perlin noise**, không phải `Random.Range` | Random cho camera giật xành xạch như hỏng; Perlin liền mạch mới ra cảm giác chấn động |
| Trauma **bình phương** trước khi dùng | Mắt người cảm nhận phi tuyến. Lấy thẳng thì cái đuôi lắc lay mãi như camera bị lỏng |

**Bước Unity còn thiếu:** Add Component `Camera Shake` lên **GameObject Camera** trong `Player.prefab`
(không phải lên Player). Chưa gắn thì không có gì rung, code vẫn chạy bình thường.

### 😴 Ngủ đông vật thể *(`MagneticObject`)*

Vật đứng yên `sleepDelay` giây → khoá cứng `isKinematic = true`. Bị hút/đẩy/đâm/nổ → tự tỉnh.

**Vì sao cần:** địa hình gồ ghề làm vật nằm trên dốc trượt và rung mãi không dứt. Unity có
`Rigidbody.sleepThreshold` sẵn nhưng nó chỉ ngủ khi vật *thật sự* đứng yên — trên dốc thì trọng lực
kéo liên tục nên không bao giờ đạt. Khoá thẳng bằng `isKinematic` thì dốc cỡ nào cũng nằm im.
Lợi ích kèm theo: Host thôi mô phỏng, `NetworkRigidbody3D` thôi gửi vị trí mỗi tick.

⚠️ **Hai cái bẫy đã xử lý, đừng gỡ ra:**

1. **`AddExplosionForce` vô tác dụng lên vật kinematic.** PhysX bỏ qua mọi lực tác động lên vật
   kinematic. Nên `Explode()` phải gọi `WakeUp()` cho từng vật **trước khi** cộng lực, nếu không
   bom nổ giữa đống bàn ghế mà không cái nào nhúc nhích.
2. **Vật ngủ bị đâm thì mất sạch động lượng.** Kinematic được PhysX coi như tường khối lượng vô hạn.
   `WakeUpFromImpact()` phải tính lại động lượng bằng tay theo tỉ lệ khối lượng, nếu không vật chỉ
   tỉnh dậy rồi đứng nguyên tại chỗ.

**Tự chữa lệch trạng thái:** các đường hút/đẩy/bắn bên `PlayerMagnetController` đặt thẳng
`isKinematic = false` chứ không gọi `WakeUp()`. `UpdateSleepState()` phát hiện và tự sửa cờ.
Cố ý làm vậy thay vì đi sửa 6 chỗ bên kia — bớt rủi ro đụng vào đường bắn đã cân bằng xong.

🔧 **Sửa 16/08 (khi test vòng 2): vật thể ngủ NGAY, không rơi tự do lấy một giây.**

Triệu chứng: vài cái cây đổ rạp ngay khi round bắt đầu, và một số vật không bao giờ nằm im.

Nguyên nhân: `ResetForNewRound()` **cố ý đánh thức** mọi vật với lý do *"nó cần rơi xuống và tự
ổn định"*. Lý do đó sai — cây cao mảnh chỉ cần collider chạm đất lệch chút là trọng lực lật đổ,
mà muốn ngủ lại phải đứng yên liên tục `sleepDelay` giây nên nó đổ hẳn mới thôi.

`_originalPosition` **chính là chỗ đã đặt tay trong Editor**. Vật lý "ổn định" chỉ có thể đẩy vật
RỜI KHỎI chỗ đó, không bao giờ đưa nó về đúng hơn.

→ Giờ `ResetForNewRound()` đóng băng ngay **sau** khi Teleport, và `Spawned()` cũng cho ngủ luôn
(bịt khoảng hở `warmupDuration` 2 giây trước round đầu).
→ Hệ quả **có chủ đích**: vật đặt lơ lửng sẽ **treo giữa không trung** thay vì rơi.
Đó là dấu hiệu đặt sai chỗ trong Editor, sửa ở Editor mới đúng — đừng sửa lại code cho nó rơi.

⚠️ **Phải BỎ tick `Is Kinematic` thủ công trên prefab.** Để nguyên thì vật **bất tử**: nó đứng yên,
nhưng cờ `IsSleeping` vẫn `false` nên `OnCollisionEnter` không đánh thức, bắn gì vào cũng trơ ra.

### 🐛 Animation nháy "falling idle" — HAI lỗi chồng nhau, đã sửa cả hai

Triệu chứng: đang chạy thì nhân vật vừa khựng về đứng yên vừa nháy sang rơi tự do, lúc bị lúc không.
Và khi test 2 máy, **chỉ nhân vật Host là bình thường khi nhìn từ máy Client**.

**Lỗi 1 — `AC_Player.controller`: transition `Fall → Locomotion` để điều kiện `Grounded = false`**
(đáng lẽ `true`). Thành vòng lặp vô hạn: Grounded false → vào Fall → Fall thấy false → nhảy ngược
về Locomotion → lại vào Fall. Hai transition đều có duration 0.15s nên Animator kẹt vĩnh viễn giữa
hai animation đang trộn dở — đó chính là cái "falling idle" nhìn thấy. ✅ Đã sửa.

**Lỗi 2 — Client ghi vào `[Networked]` mà nó không sở hữu.** `FPSMovement.FixedUpdateNetwork()`
chỉ chặn bằng `GetInput()`, mà hàm đó trả `true` ở **hai** nơi: trên Host với mọi nhân vật, VÀ trên
máy Client với nhân vật của chính họ. Nên Client cũng ghi `WalkSpeed01` / `IsGrounded` /
`MovingBackward`. Fusion coi đó là giá trị dự đoán, mỗi lần nhận gói tin từ Host lại huỷ đi và
chạy lại các tick → con số dao động → animation nhấp nháy.

Và dự đoán ở đây **không thể đúng được**, vì nó dựa trên `controller.velocity` — giá trị nội bộ của
CharacterController, mà `BeforeAllTicks()` lại tắt/bật component đó liên tục. Không tất định thì
hai máy không bao giờ tính ra cùng kết quả.

→ Đó là lý do chỉ nhân vật **Host** hiện đúng khi nhìn từ Client: nó là proxy nên Client không
đụng vào, cứ nhận sao dùng vậy.

✅ Đã sửa: bọc `if (!HasStateAuthority) return;` trước khi ghi. Đánh đổi là animation của chính mình
trễ ~nửa vòng ping — không ai nhận ra, còn nhấp nháy thì ai cũng thấy.

⚠️ Nhưng **nhấp nhô camera vẫn tính tại chỗ** qua biến mới `LocalWalkSpeed01` (không networked).
Camera chỉ mình mình nhìn nên không cần khớp với ai; bắt nó chờ Host thì head bob lag theo ping.

**Kèm theo — coyote time cho `isGrounded`:** `controller.isGrounded` chỉ nói được về lần `Move()`
gần nhất, nên trên địa hình gồ ghề nó tắt đúng một tick rồi bật lại (đi qua khe giữa hai collider,
leo gờ nhỏ, bước xuống dốc thoải). Giờ phải rời đất **liên tục** `groundedGraceTime` (0.15s) mới
báo là đang bay. Cố ý KHÔNG lọc phần trọng lực — trọng lực phải phản ứng theo va chạm thật từng tick.

### 💡 Ánh sáng & post-processing *(16/08)*

Game trông nhạt vì ba thứ đều là mặc định chưa ai động vào: **`Tonemapping = None`** (thủ phạm lớn
nhất — vùng sáng bị cắt phẳng thành trắng bệch), mọi hiệu ứng `intensity = 0`, và
**Color Grading = LDR**.

May là camera **đã bật** Post Processing sẵn và `DefaultVolumeProfile` **đã được gán làm profile
toàn cục** (đối chiếu GUID với `UniversalRenderPipelineGlobalSettings`) — nên chỉ cần đổ nội dung
vào, không phải tạo GameObject Volume nào trong scene.

```
Assets/Settings/DefaultVolumeProfile.asset
  Tonemapping   None -> Neutral      ← nền tảng cho mọi thứ còn lại
  Saturation    0    -> +22
  Contrast      0    -> +12
  PostExposure  0    -> +0.2
  Bloom         0    -> 0.7   threshold 1   ← làm găng đỏ/xanh và quả cầu năng lượng loé sáng
  Vignette      0    -> 0.28

Assets/Settings/PC_RPAsset.asset          (quality level "PC" đang dùng, m_CurrentQuality = 1)
  ShadowDistance              50   -> 85    ← map ~56m, để 50 thì bóng biến mất giữa sân
  MainLightShadowmapResolution 2048 -> 4096
  ShadowAtlasResolution        256 -> 2048  ← 256 là đèn phụ đổ bóng VỠ NÁT
  ColorGradingMode             LDR -> HDR   ← bắt buộc để bloom/tonemapping đúng
```

⬜ **Còn phải làm trong Unity:** hạ **Ambient Intensity** `1` → `0.6`
(Window → Rendering → Lighting → Environment). Đây là thứ làm bóng đậm nhất — ambient đang để 1
nghĩa là vùng trong bóng vẫn được chiếu sáng đầy đủ nên bóng nhìn nhạt như vệt xám.
Không sửa hộ vì giá trị này nằm trong `TestScene.unity` đang mở.

> Tụt FPS thì trả lại theo thứ tự: `Shadow Res 4096 → 2048`, rồi `Shadow Distance 85 → 65`.

### 👁️ Nhãn quan nhân vật *(file mới: `Scripts/Player/PlayerVisuals.cs`)*

**Vấn đề:** nhân vật rất khó nhìn thấy giữa map nhiều cây cối, nhất là trên màn hình nhỏ.
Và chưa có cách nào phân biệt địch với đồng đội.

⚠️ **Cái bẫy suýt mắc: màu ĐỘI và màu CỰC GĂNG trùng nhau hoàn toàn.**
`Team` 0 = Đỏ / 1 = Xanh, mà cực găng cũng Dương = Đỏ / Âm = Xanh dương. Nếu cho outline
toàn thân mang màu điện tích thì nhìn một người viền đỏ sẽ không biết đó là *"địch đội Đỏ"*
hay *"đồng đội đang mang cực Dương"* — aura không những không giúp mà còn phá luôn.

**Nguyên tắc đã chốt: mỗi loại tin một kênh riêng, không kênh nào mang hai nghĩa.**

| Thông tin | Kênh | Màu |
|---|---|---|
| Cực găng của **địch** | Outline toàn thân + quả cầu ở tay | Đỏ / Xanh dương |
| **Đồng đội** | Outline + marker trên đầu | Xanh **lá** |
| **Địch** | *(không gán màu riêng)* | — nhận ra bằng LOẠI TRỪ |
| Mức nhiễm điện | Aura sét quanh người | Vàng |

**Vì sao địch KHÔNG được gán màu riêng:** cho địch viền cam hay đỏ thì nó cạnh tranh thị giác
với chính màu cực găng — thứ quan trọng nhất cần đọc. Để địch "sạch màu" thì thứ duy nhất rực rỡ
trên người họ là cực găng, mắt bị hút thẳng vào đó.

**Che khuất — khác nhau có chủ đích:**

| | Chế độ | Vì sao |
|---|---|---|
| Đồng đội | `Outline.Mode.OutlineAll` — **xuyên tường** | Vị trí đồng đội là thông tin cho không |
| Địch | `Outline.Mode.OutlineVisible` — **có che khuất** | Núp sau tường là mất viền, thò nửa người thì chỉ nửa đó hiện. Cho xuyên tường thành gian lận nhìn xuyên vách |

Cả hai chế độ đều có sẵn trong **QuickOutline** (`Resources/Environment/QuickOutline/`), không phải viết shader.

💡 **KHÔNG CẦN ASSET GĂNG TAY.** `character.fbx` là **Humanoid rig**, nên lấy được xương bàn tay
bằng `animator.GetBoneTransform(HumanBodyBones.LeftHand)`. Script tự tạo quả cầu phát sáng gắn vào đó.
Quả cầu còn **đọc tốt hơn** model găng thật: găng chỉ vài pixel ở 30m và hay bị thân che, còn quả cầu
là nguồn sáng nên Bloom kéo hào quang loang ra. Việc này gỡ nút thắt "chưa có asset găng + FPS view".

> ⚠️ `CreatePrimitive` luôn kèm Collider — script xoá ngay khi tạo. Sót một cái là đủ phá hệ vật lý.

**Trạng thái:**

| Phần | Xong? |
|---|---|
| Outline địch (màu cực găng, có che khuất) | ✅ code xong |
| Outline đồng đội (xanh lá, xuyên tường) | ✅ code xong |
| Quả cầu năng lượng ở hai tay | ✅ code xong |
| Gắn `PlayerVisuals` lên `Player.prefab` | ⬜ **cần làm trong Unity** |
| **Aura sét vàng** (ParticleSystem) | ⬜ **CHƯA TẠO** — material `M_LightningAura` đã có, thiếu ParticleSystem |
| **Marker trên đầu đồng đội** | ⬜ **CHƯA TẠO** |

Hai ô cuối để trống thì script vẫn chạy bình thường, chỉ là không có hiệu ứng đó.

**Cách tạo aura sét (không cần texture):** ParticleSystem con của Player, Scale `(0.7, 1.8, 0.7)`,
Shape `Box` + Emit from `Shell`, bật **Noise** (Strength 1.5) và **Trails** (Lifetime 0.3) —
hai module này biến chấm sáng thành tia điện ngoằn ngoèo. Material dùng
`URP/Particles/Unlit` + Surface `Transparent` + Blending **`Additive`**, để **trắng nguyên bản**;
màu vàng và HDR Intensity `+2` đặt ở **Start Color** của Particle System.
`Emission → Rate over Time` để **0**, script tự ghi đè theo mức nhiễm.

> ⚠️ Nhớ gán cả **Trail Material** trong Renderer, quên là vệt trail ra màu hồng cánh sen.
> ⚠️ `_BaseColor` của shader Particles **không có tag `[HDR]`** nên không kéo Intensity được —
> đó là lý do phải đặt màu ở Start Color chứ không phải ở material.

### 🔧 Sửa: nhiều collider trên một vật *(`PlayerMagnetController`)*

Ba chỗ dùng `GetComponent<Collider>()` **số ít** đã gộp thành `SetIgnoreCollisionWithPlayer()`
duyệt hết mọi collider. Cần vì cây/đá dùng **nhiều BoxCollider ghép** thay cho MeshCollider lõm.
Sót một collider là dính lại đúng **bẫy số 8** ở CLAUDE.md: cái chưa tắt nằm chồng trong người chơi,
PhysX bắn ra lực gỡ kẹt, vật bay đi mỗi lần một hướng.

---

## 2. Đã xong và đã kiểm chứng trên 2 máy

Lobby (mã PIN, đổi đội) · spawn theo đội đúng vị trí và hướng nhìn · WASD + xoay + Dash ·
đổi điện tích · cận chiến 3 kiểu · nạp điện / hút / cầm / ném vật thể · máu + giáp ·
vòng đấu đầy đủ (BuyPhase 15s → Combat → RoundEnd → tính điểm → hồi sinh) · rào chắn spawn ·
KillZone · Shop (mua bán, trừ tiền, thưởng cuối round) · túi đồ · Radial Menu ·
4 vật phẩm tiêu hao · HUD.

### Giá trị Inspector đã cân bằng tay (ghi lại phòng khi prefab hỏng)

Đọc thẳng từ file `.prefab` ngày 09/08 nên đây là giá trị THẬT, không phải mặc định trong code.

```
[Project Settings → Physics]
  Gravity              (0, -3, 0)      ← mặc định Unity là -9.81. Chỉ ảnh hưởng RIGIDBODY (đạn)

[Player.prefab]
FPSMovement          moveSpeed 13 · mouseSensitivity 1 · dashForce 100 · dashCooldown 1
                     gravity -9.81 · mass 3 · drag 5     ← gravity này CHỈ cho nhân vật
PlayerMagnetController  pullForce 13 · pushForce 50 · shootRange 50
                     meleeRange 4 · meleeCooldown 1 · dashLockRange 20
                     meleePushForce 200 · grapplePullForce 100
                     heldObjectSize 1 · onlyShrinkLargeObjects true
PlayerHealth         maxCharge 100 · knockbackAtMaxCharge 5 · chargeGainMultiplier 1.5
                     maxArmor 30 · armorPerPurchase 10
                     bandageMaxDischarge 20 · bandageChargeRatio 0.5
MagneticObject       hitKnockbackForce 120  ← lực hất khi TRÚNG ĐẠN (mới 09/08, trước đây = 0)
PlayerEconomy        start 500 · cap 750 · win 150 · lose 100

[GameManager.prefab]
                     buyPhase 15 · roundEnd 5 · matchEnd 8 · warmup 2
                     killZoneY -20 · pointsToWin 5 · requiredLead 2
                     combatDuration — CHƯA có trong prefab, sẽ lấy mặc định 90 khi Unity biên dịch lại

[Shop]               Armor 150 · EnergyDrink 100 · Bandage 100 · Gasoline 150 · EMBarrier 200
```

> ⚠️ **Hai trọng lực riêng biệt, đừng lẫn.** Project Settings `-3` cho vật thể (đạn bay lơ lửng),
> `FPSMovement.gravity` `-9.81` cho nhân vật (rơi bình thường). Tính quỹ đạo đạn phải dùng **-3**.

> 🔴 **`knockbackAtMaxCharge = 5` gần như chắc chắn quá lố.** Quãng đường bị đẩy ≈ `(lực/mass) × 0.2`
> với `drag 5`. Với `meleePushForce 200` và `mass 3`: sạch điện đã bay **13m**, đầy điện bay **67m** —
> văng khỏi mọi map. Bắt đầu bằng **2.5**, nếu vẫn quá thì hạ `meleePushForce` 200 → 120
> trước khi động vào hệ số. *(Ước lượng trên giấy, chưa test.)*

---

## 3. Còn phải làm

### 🔴 Map — rủi ro lớn nhất, dồn thời gian vào đây

Hiện **toàn cube và model mặc định Unity**. Đây là thứ mất điểm rõ nhất khi bảo vệ.

Đừng dựng đẹp — cần **map đọc được, chơi được**. Ghép từ asset đã có trong project
(Polytope Studio, Fantasy Environments, Unvik Cross Plains) nhanh hơn tự dựng nhiều.

⚠️ Đổi map làm hỏng 4 giá trị đã chỉnh, phải cân bằng lại sau:
`redTeamSpawnPoint` · `blueTeamSpawnPoint` · vị trí 2 rào chắn · `killZoneY`

🔥 **Từ 09/08 map còn quan trọng hơn nữa.** Chế độ Quá Tải sống chết bằng **rìa vực**:
bị hất bay 15m chỉ đáng sợ khi có chỗ để rơi xuống. Map phải có **nhiều rìa hở, ít tường bao**,
và nên có vài mỏm hẹp nhô ra ngoài để tạo điểm nóng. Map kín sẽ giết mode này.

Dấu hiệu map chưa đạt: **mọi round đều kết thúc bằng hết giờ 90s** thay vì có người rơi.

#### 📐 Bố cục đã thiết kế: **"LÕI TỪ"** *(09/08, chưa dựng)*

Đảo bát giác, một lõi nâng cao ở giữa, 4 mỏm nhô ra vực ở 4 góc chéo.

```
      ◆TNT                                      ◆TNT
        ╲     ╔══════════════════════════╗     ╱
         ╲────╢   ▓▓▓  SPAWN ĐỎ  ▓▓▓     ╟────╱      Z=62 (y=+4)
              ║        [rào chắn]        ║
              ║   ╭───────╨───╨──────╮   ║
   V Ự C      ║dốc│   LÕI TỪ  y=+7   │dốc║      V Ự C
              ║   │     r = 9m       │   ║
              ║   ╰───────╥───╥──────╯   ║
              ║        [rào chắn]        ║
         ╱────╢   ▓▓▓ SPAWN XANH ▓▓▓     ╟────╲      Z=18 (y=+4)
        ╱     ╚══════════════════════════╝     ╲
      ◆TNT                                      ◆TNT
            X=12                          X=68
```

| Khu | Cao | Kích thước | Vai trò |
|---|---|---|---|
| Vành đai | `y=0` | rộng ~19m | Sân đánh chính |
| **Lõi Từ** | `y=+7` | bán kính 9m | Cao điểm tranh chấp, 4 dốc lên |
| 2 bệ spawn | `y=+4` | 12×10m | Có rào chắn Buy Phase |
| 4 mỏm chéo | `y=0` | 10×10m, cổ nối 4m | **Chỗ để TNT** |

```
Tâm map      (40, 0, 40)          Spawn Đỏ    (40, 4, 62) yaw 180
Lõi Từ       (40, 7, 40) r=9      Spawn Xanh  (40, 4, 18) yaw 0
Rào Đỏ       (40, 4, 56)          Rào Xanh    (40, 4, 24)
4 mỏm        (16,0,16) (64,0,16) (16,0,64) (64,0,64)
Đảo bát giác X và Z từ 12 đến 68  ·  killZoneY -20
```

**Nguyên tắc rải đạn — đừng làm qua loa:** *đạn ngon nhất nằm ở chỗ dễ chết nhất.*
Normal ×5–6 ở vành đai gần spawn · Heavy ×2 trên Lõi Từ · Spike ×2 vành đai nửa ngoài ·
**TNT ×4 ở đúng đầu 4 mỏm**. Nếu rải đều khắp map thì không ai có lý do ra rìa vực → mode Quá Tải chết yểu.

**Vì sao vành đai rộng 19m:** một cú `meleePushForce 200` lúc sạch điện đẩy đi ~13m.
Đứng giữa vành đai thì sống, nửa ngoài thì chết. Nhiễm đầy điện (~33m) thì đứng đâu cũng chết.
Sự leo thang đến từ **kích thước map**, không phải từ code.

> ⚠️ **DỰNG BẰNG CUBE TRẮNG TRƯỚC.** Không texture, không model, không ánh sáng.
> Chắc chắn sẽ phải sửa kích thước sau khi test. Sửa cube mất 5 giây; sửa khu đã kitbash 40 model
> mất nửa buổi, rồi sẽ ngại sửa và chấp nhận một map dở. Khoá layout xong mới dán art.

### 🔴 Hai lỗi phát hiện 16/08 khi dựng map — SỬA TRONG UNITY, KHÔNG PHẢI CODE

**1. Thiếu tag `Magnetic` → bấm chuột vào vật không ăn gì.**

Triệu chứng: "nhiều cục đá và cây bắn điện tích không lên được, cục khác thì được".
Nguyên nhân: prefab `Tree9_*` có `Tag = Untagged`, phải override tay từng cái trong scene.
Đọc `TestScene.unity` ngày 16/08: chỉ **8 object** được override, trong khi log báo **~16** vật
có Rigidbody. Raycast lọc bằng `CompareTag("Magnetic")` nên bỏ qua hoàn toàn số còn lại.

→ **Cách sửa:** đặt `Tag = Magnetic` ngay **trên prefab**, khỏi tick tay từng bản sao.
Đúng cái bẫy đã ghi sẵn ở mục "Các bước tạo 1 vật thể từ tính mới" — vẫn dính lại lần nữa.

**2. Concave Mesh Collider — 32 dòng lỗi đỏ trong Console.**

```
Concave Mesh Colliders are not supported when used with dynamic Rigidbody GameObjects.
Scene hierarchy path "Environment/Rock1A"
```

Dính: `Rock1A` `Rock4A` `Tree9_2/3/4/5` và các bản sao. Hậu quả thật: **những vật này gần như
không va chạm được gì** — đạn bay xuyên qua, người chơi lọt qua.

Unity chỉ cấm MeshCollider **lõm** đi cùng **Rigidbody động**. Hai đường sửa:

| Vật đó là gì | Làm gì |
|---|---|
| **Trang trí** (đa số cây) | **Xoá Rigidbody + MagneticObject.** MeshCollider lõm hợp lệ ngay khi không còn Rigidbody. Giải quyết luôn cả chuyện cây đổ nhào và giảm tải mạng |
| **Làm đạn** (có tag `Magnetic`) | 2 BoxCollider — 1 thân, 1 vòm lá — đặt **trên chính GameObject gốc** |

⚠️ **Đừng tách collider ra GameObject con.** Raycast dùng `hit.collider.GetComponent<MagneticObject>()`,
trúng vào con thì trả `null` và mất hẳn khả năng tương tác.

✅ Hai BoxCollider **chồng lên nhau một chút là tốt** (khỏi hở khe cho đạn lọt qua). Collider cùng
một Rigidbody thì PhysX không bao giờ cho chúng va chạm với nhau, nên không sinh lực gỡ kẹt.

> 💡 **Cây đổ nhào là đúng vật lý, không phải lỗi.** Khúc gỗ dựng đứng có Rigidbody động, trọng tâm
> cao, đặt trên dốc thì nó *phải* ngã. Câu hỏi thật là cây đó có cần là vật thể vật lý không —
> với phần lớn cây trong map thì không.

### ⬜ Việc trong Unity còn nợ *(tính tới 16/08)*

| Việc | Ở đâu | Thiếu thì sao |
|---|---|---|
| Gắn `CameraShake` lên **GameObject Camera** | `Player.prefab` | Không có gì rung |
| Gắn `PlayerVisuals` lên **object gốc Player** | `Player.prefab` | Không có outline / quả cầu tay |
| Tạo **ParticleSystem aura sét** rồi kéo vào ô `Lightning Aura` | `Player.prefab` | Không hiện mức nhiễm điện |
| Tạo **marker trên đầu** rồi kéo vào ô `Ally Marker` | `Player.prefab` | Không đánh dấu đồng đội |
| Hạ **Ambient Intensity** `1` → `0.6` | Lighting → Environment | Bóng nhạt như vệt xám |
| Bỏ tick **`Is Kinematic`** thủ công trên prefab vật thể | `Items/*.prefab` | Vật **bất tử** — bắn gì vào cũng trơ |
| Đặt **`Tag = Magnetic` trên prefab** cây/đá | `Resources/Tree9/*`, `Rock*` | Bấm chuột vào không ăn gì |
### 🟡 Trang trí đảo — CHƯA LÀM (dự kiến ~1 tiếng)


Đã có công cụ sẵn: `Assets/Scripts/EnvironmentScatter.cs`. Gắn lên Empty GameObject,
kéo prefab vào danh sách, chuột phải component → **"Rải trang trí"**. Không ưng thì
**"Xoá hết"** rồi rải lại, mỗi lần ra một bố cục khác.

Cần tải thêm **cỏ / bụi / hoa** — project chưa có cái nào. Nguồn khuyên dùng:
**Quaternius Ultimate Nature Pack** (CC0, cùng phong cách lowpoly với đá đang có).
Đá thì đã có sẵn bộ *Rocks and Boulders 2*, dùng luôn.

Rải thành **nhiều lớp thưa** thay vì một lớp dày:

| Object | Prefab | Count | Align To Slope |
|---|---|---|---|
| `Scatter_Grass` | cỏ | 500 | tắt |
| `Scatter_Bushes` | bụi | 80 | tắt |
| `Scatter_Rocks` | đá *(đã có)* | 60 | **bật** |
| `Scatter_Flowers` | hoa, nấm | 40 | tắt |

⚠️ **`Strip Colliders` PHẢI BẬT.** Cỏ có collider sẽ chặn đường đạn và làm nó lệch hướng —
đúng cái lỗi đã mất cả buổi 16/08 để sửa. Trang trí chỉ cần nhìn thấy, không cần chạm được.

⚠️ **Chừa trống lối đi giữa sân.** Đặt vùng rải ở rìa đảo. Cỏ um tùm giữa đấu trường vừa
che tầm nhìn vừa làm khó nhìn vật thể và đối thủ.

### 🟡 Dựng UI còn thiếu

| Việc | Ghi chú |
|---|---|
| ~~Nhãn pha "CHIẾN ĐẤU" chiếm chỗ giữa màn hình~~ | ✅ **Đã sửa 16/08** — `HUDController` tắt hẳn `phaseText` (SetActive false) khi vào pha Combat, bật lại ở các pha khác. Đang đánh nhau thì ai cũng biết là đang đánh nhau, dòng chữ đó chỉ tranh chú ý với thứ cần nhìn thật |
| **Sửa nhãn HUD "MÁU" → "ĐIỆN TÍCH"** | Chỉ đổi chữ. Ô `Health Fill`/`Health Text` giữ nguyên reference, KHÔNG phải gán lại |
| *(tuỳ chọn)* Thêm TMP text cho ô `Knockback Text` | Hiện `x2.4` — cho người chơi biết đang nguy hiểm cỡ nào |
| Gắn `SpectatorController` lên Canvas TestScene | + 1 panel + 1 TMP text. Rất nhẹ |
| Gắn `SettingsUI` lên Canvas MenuScene | 3 slider âm lượng, slider độ nhạy, dropdown độ phân giải, toggle fullscreen, nút đóng |
| *(tuỳ chọn)* `SettingsUI` cho TestScene | Mở Settings giữa trận |

> **Luôn gắn script UI lên Canvas, KHÔNG lên panel con.** Panel tắt thì `Update()` chết theo,
> bấm phím sẽ không mở lại được. Áp dụng cho cả ShopUI / RadialMenu / HUD / Settings / Spectator.

### 🟡 Âm thanh — 18 ô clip trên `AudioManager` (MenuScene)

Chưa cần đủ. Năm cái quan trọng nhất: `sfxHit` `sfxExplosion` `sfxDash` `sfxCharge` `sfxRoundStart`.
Nguồn: freesound.org, kenney.nl. **Ghi nguồn trong báo cáo.**

### ⚫ Trước khi nộp — xoá phím debug

- `InputButton.DebugSuicide` trong `NetworkInputData.cs`
- Dòng gửi phím `K` trong `NetworkRunnerHandler.OnInput()`
- Khối tự sát trong `PlayerMagnetController.FixedUpdateNetwork()`
- Đặt lại `Points To Win = 5` nếu có hạ để test

### 🟢 Chưa test (code xong, chưa chạy thử lần nào)

- **🎬 Rung camera** *(làm 16/08)* — cần Add Component `CameraShake` lên Camera trước đã
  - Đi bộ có nhấp nhô không, có chóng mặt không (hạ `Bob Vertical Amount` hoặc bỏ tick nếu có)
  - Dash và bắn vật có rung đúng **một lần** không — rung nhiều lần là dấu hiệu Fusion tua lại lọt qua
  - Máy **Client** bắn vật có rung không (đây là lý do phải thêm `FireCount` networked)
- **😴 Ngủ đông vật thể** *(làm 16/08)* — nhớ bỏ tick `Is Kinematic` thủ công trước
  - Vật trên dốc có trượt một đoạn rồi **dừng hẳn** không
  - Bắn vật khác vào vật đang ngủ — nó phải văng đi tự nhiên, không được trơ ra
  - **TNT nổ có thổi bay được đồ đạc đang ngủ không** (nghi ngờ nhất)
  - ⚠️ **`NetworkRigidbody3D` có ghi đè `isKinematic` không** — tài liệu Fusion không nói rõ.
    Dấu hiệu xung đột: vật vẫn trượt dù đã ngủ, hoặc Client thấy vật ở chỗ khác Host.
    → Cách chữa nếu dính: đổi từ `isKinematic` sang `RigidbodyConstraints.FreezeAll`
- **Nhiều BoxCollider trên một vật** *(sửa 16/08)* — cầm cây lên bấm `V` tung.
  Bay lệch loạn xạ mỗi lần một hướng = còn collider nào chưa được tắt va chạm
- **Reset map đầu round mới** *(làm 09/08)* — `GameManager.ResetWorldObjects()`
  - Mọi vật thể có về đúng chỗ cũ, sạch điện, hết bị chế thành TNT không?
  - Thùng TNT đã nổ ở round trước có **sống lại** không?
  - Túi đồ có bị xoá phần vật thể map, mà **vẫn giữ đồ mua từ Shop** không?
  - Vật đang cầm trên tay lúc hết round có bị buông ra đúng cách không?
- **Nắm được vật TO** *(sửa 09/08)* — kéo cái bàn/khúc gỗ to nhất về, phải vào tay được
- **⚡ Toàn bộ chế độ Quá Tải** — ưu tiên test số 1
  - Lực văng có tăng đúng theo điện tích không (Console in `x2.4` mỗi lần trúng đòn)
  - Dash và Grapple có giữ nguyên cự ly ở mọi mức điện không (**phải giữ nguyên**)
  - Hết giờ 90s có phân thắng bại đúng theo tổng điện tích không
  - Giáp Cách Điện và Bộ Xả Điện còn hoạt động sau khi ánh xạ lại không
- Về MenuScene sau khi hết trận → **tạo/vào phòng mới có bình thường không?**
- 4 hiệu ứng vật phẩm qua Radial Menu
- Luật riêng Heavy (cản) và Spike (hút nhầm ăn x2)
- Quan sát khi chết

---

## 4. Nguyên tắc kiến trúc — đã trả cổ tức nhiều lần

### Chỉ đồng bộ những gì BẮT BUỘC

Thứ nào suy ra được tại chỗ thì tính tại chỗ. Đã dùng cho:

| Thứ | Cách làm |
|---|---|
| Vật cầm trên tay | Chỉ truyền `GrabbedObjectId`, mỗi máy tự đặt vị trí trong `LateUpdate()` |
| Thu nhỏ vật cầm | Mỗi máy tự đo bounds rồi scale, `SyncScale` để tắt |
| Rào chắn Buy Phase | Suy từ `GameManager.Phase`, là MonoBehaviour thường |
| Âm thanh | Móc vào `OnChangedRender` sẵn có, 0 byte thêm |
| Quan sát khi chết | Chỉ đổi camera nào đang bật |

### Một thứ chỉ được có MỘT chỗ ghi vào *(rút ra 16/08)*

`FPSMovement.Render()` là nơi duy nhất ghi vào transform của camera. `CameraShake` không tự xoay
camera mà chỉ *tính ra* độ lệch để `Render()` cộng vào.

Nếu hai script cùng ghi một thứ thì ai chạy sau sẽ xoá công người trước, mà thứ tự Unity gọi hàm
phụ thuộc vào thứ tự component và loại hàm (`Update` / `LateUpdate` / `Render` của Fusion) — rất
khó đoán và hay đổi khi thêm component mới. Gộp về một chỗ ghi thì không bao giờ có lớp lỗi đó.

Cùng lý do với việc **Animation gió đung đưa phải làm bằng vertex shader, không bằng Animator**:
Animator ghi `transform`, mà Rigidbody và Fusion cũng ghi `transform`. Shader đẩy đỉnh mesh lúc vẽ
nên không tranh với ai, và collider cũng đứng yên — đó là điều mong muốn chứ không phải hạn chế.

### Âm thanh / hiệu ứng: KHÔNG gọi trong `FixedUpdateNetwork()`

Fusion tua lại nhiều tick mỗi khung hình → một cú dash kêu 5–6 lần.
Cách chữa: biến đếm `[Networked]` + `OnChangedRender`. Xem `DashCount`, `MeleeCount`, `LaunchCount`.

### Đã thử và BỎ: cho Client dự đoán trước thao tác vật thể

Gỡ `if (!HasStateAuthority) return;` → **tệ hơn hẳn**, vật cầm trên tay giật và trễ nặng hơn.
Đã lùi. **Độ trễ đều đặn dễ quen tay hơn giật ngẫu nhiên.** Đừng thử lại.

### Vật bay lệch ngẫu nhiên → nghi collider chồng lấn TRƯỚC, đừng nghi lực

Lỗi tung `V` lệch loạn (sửa 09/08) hoá ra không phải do lực tung sai, mà do `RestoreScale()`
làm collider phình to **ngay trong người chơi**, PhysX bắn ra lực gỡ kẹt để tách ra.

Dấu hiệu nhận biết: **vật càng TO càng lệch nhiều**, vật nhỏ lại bay chuẩn. Nếu thấy quy luật
này thì gần như chắc chắn là chồng lấn collider chứ không phải sai số lực.

Chi tiết đầy đủ ở [CLAUDE.md](CLAUDE.md) mục 5, bẫy số 8.

### "Giật" khi test ParrelSync thường KHÔNG phải lỗi mạng

Đã mất nhiều thời gian nghi ngờ đồng bộ, cuối cùng đo ra **FPS chỉ 15–30** ở cửa sổ clone.
Test máy phụ → mượt hoàn toàn.

**ĐO FPS TRƯỚC KHI nghi ngờ code mạng.** Giảm tải: đóng Scene view ở clone (ăn nhiều nhất),
bật VSync, hạ độ phân giải Game view, hoặc build `.exe` chạy 1 build + 1 Editor.

### Các bẫy Fusion khác

Xem **CLAUDE.md mục 5** — 7 bẫy đã gặp kèm cách chữa (CharacterController, `NetworkTransform`,
`onBeforeSpawned` chỉ chạy trên Host, giá trị Inspector đè code...).

---

## 5. Bản đồ code

```
Assets/
├── NetworkRunnerHandler.cs   Lobby, gửi input, spawn, quay về menu. DontDestroyOnLoad singleton
├── RoomPlayer.cs             Đồng bộ tên + đội trong phòng chờ
└── Scripts/
    ├── GameManager.cs        Vòng đấu, tính điểm, hồi sinh, KillZone, Overtime
    ├── ShopManager.cs        Giao dịch qua RPC, Host duyệt        (trên Player.prefab)
    ├── AudioManager.cs       Singleton âm thanh                    (trong MenuScene)
    ├── GameSettings.cs       Class tĩnh: độ nhạy chuột, fullscreen
    ├── RoundBarrier.cs       Rào spawn, MonoBehaviour thường       (trong TestScene)
    ├── Player/               FPSMovement · PlayerMagnetController · PlayerHealth
    │                         InventorySystem · PlayerHotbarController · PlayerInteract
    │                         RadialMenuController · NetworkInputData · PlayerEconomy
    │                         CameraShake  ← MonoBehaviour thuần, đặt trên Camera
    │                         PlayerVisuals ← outline địch/bạn, quả cầu tay, aura sét
    │                         PlayerAnimatorDriver · GlovePolarityColor
    ├── Item/                 MagneticObject · MagneticAura · ItemData · EMBarrier
    └── MenuUI/               HUDController · ShopUI · ShopItemButton
                              SettingsUI · SpectatorController · RoomItemUI
```

**Quy ước:** script UI đặt trên Canvas trong scene, đọc `FPSMovement.Local` để tìm nhân vật mình.
Script gameplay đặt trên `Player.prefab`.

### Prefab

`Prefab/Player.prefab` — 8 script + NetworkObject + NetworkTransform + CharacterController + Camera
*(NetworkTransform phải nằm TRÊN FPSMovement)*
`Prefab/GameManager.prefab` · `Prefab/EMBarrier.prefab` · `Prefab/RoomPlayer.prefab`
`Items/Type/*.asset` — 7 ItemData (tên, giá, icon, consumableType)
`Items/*.prefab` — prefab vật thể từ tính, cần `NetworkObject` + `NetworkRigidbody3D`

### Gasoline Canister — giới hạn cần nhớ

`ConvertToTNT()` **chỉ đổi được vật loại Normal**. Spike, Heavy, và TNT sẵn đều trả về `false`.
Đúng theo đặc tả, không phải lỗi. Dùng hụt thì **không mất chai**.

⚠️ Nhưng bấm hụt **không có phản hồi nào** — không tiếng, không log, không thông báo.
Người chơi sẽ tưởng vật phẩm hỏng. Nên thêm tiếng "tạch" hoặc dòng chữ trên HUD nếu còn thời gian.

**Cách nó nổ:** không có cơ chế riêng. Chỉ đổi `CurrentType = TNT`, rồi đi theo đúng đường của
TNT thường — `OnCollisionEnter` chỉ nổ khi `isMovingAsBullet == true`.
→ **Vật đã chế nằm dưới đất, ai đi vào cũng KHÔNG nổ.** Phải bắn/đẩy nó đi rồi đâm vào gì đó mới nổ.
Đây là "bom ném", không phải "mìn đặt".

### Các bước tạo 1 vật thể từ tính mới

| # | Việc | Ghi chú |
|---|---|---|
| 1 | Kéo model vào scene | |
| 2 | **Tag = `Magnetic`** | ⚠️ Thiếu là raycast bỏ qua hoàn toàn, bấm chuột không ăn gì |
| 3 | **Collider** | Không tick Is Trigger |
| 4 | **Rigidbody** | |
| 5 | **NetworkObject** + **NetworkRigidbody3D** | Thiếu cái sau là máy kia thấy vật ở (0,0,0) |
| 6 | **MagneticObject** | `objectType` · `baseDamage` (10/20/25, TNT dùng `tntDamage` 35) · `itemData` |
| 7 | **MagneticAura** *(tuỳ chọn)* | Hiệu ứng hào quang |

Renderer phải có Material riêng — script tự đổi màu theo điện tích.

---

## 6. Quyết định còn treo

### ✅ ĐÃ LÀM 16/08: Hồi sinh + Khu chiếm đóng — CHƯA TEST

> Toàn bộ đề xuất bên dưới **đã được code xong**, biên dịch sạch 0 error.
> Ba quyết định khác với bản đề xuất gốc:
>
> 1. **`ControlZone` là MonoBehaviour thuần**, không phải NetworkBehaviour. Tiến độ chiếm
>    nằm trên `GameManager` (vốn đã là NetworkObject). Tránh phải đặt NetworkObject sẵn
>    trong scene — thêm một chỗ có thể hỏng mà không được gì.
> 2. **Mỗi đội một thanh riêng, KHÔNG tụt.** Bỏ cơ chế tụt vì nó kéo dài round và gây ức chế;
>    đồng hồ `combatDuration` đã đủ chặn round lê thê.
> 3. **`EndRoundByCharge()` đã bị XOÁ.** Hết giờ giờ so **tiến độ chiếm**, không so điện tích.
>    `CheckRoundOver()` (xoá sổ cả đội) cũng bỏ — có hồi sinh thì xoá sổ chỉ là tạm thời.
>
> **Việc còn phải làm trong Unity:**
> 1. Tạo Empty GameObject giữa map → gắn **`ControlZone`** → chỉnh `radius` / `halfHeight`
>    *(Scene view có vẽ hình trụ vàng để căn)*
> 2. Dựng UI cho 5 ô mới trên `HUDController`: `zoneGroup`, `zoneRedFill`, `zoneBlueFill`,
>    `zoneStatusText`, `respawnCountdownText`. Bỏ trống vẫn chạy, chỉ là không thấy tiến độ.
>
> **Số liệu mặc định** *(trên `GameManager.prefab`)*: `zoneCaptureRate 10` · `zoneProgressToWin 100`
> · `zoneTwoPlayerMultiplier 1.5` · `deathZoneBonus 8` · `respawnDelay 5`
> → đứng một mình 10 giây liên tục là thắng round.

<details>
<summary>Bản đề xuất gốc ngày 09/08 (giữ lại để tra lý do thiết kế)</summary>

### 🔵 Hồi sinh + Khu chiếm đóng

Ý của chủ project: vì game xoay quanh knockback, hãy **bỏ luật "chết là bị loại cả round"**,
đổi sang **chết → hồi sinh sau X giây**, và thêm **một khu vực nhỏ mà người chơi phải đứng
trong đó một thời gian** để thắng.

**Vì sao đáng làm — 3 lý do cụ thể:**

1. **Sửa đúng lỗ hổng lớn nhất của Quá Tải.** Người chơi vốn sẽ tránh xa rìa vực.
   Khu chiếm đóng **ép họ đứng vào một chỗ cố định do mình chọn** → quyết định luôn nơi giao tranh.
2. **Làm knockback bớt ức chế.** Bị hất khỏi map hiện mất cả round (ngồi nhìn 90s, rất cay).
   Có hồi sinh thì chết = mất thời gian + mất vị trí, không mất trận → được phép để hệ số
   knockback mạnh tay hơn.
3. **Xoá bớt code.** `EndRoundByCharge()` (hết giờ so tổng điện) vốn là giải pháp tình thế cho
   "không ai chết thì round không kết thúc". Có khu chiếm đóng thì round luôn có đường kết thúc
   tự nhiên → **xoá hàm đó đi**.

**Chi phí ước lượng: ~1,5–2 ngày code.** Hai mảnh khó nhất đã có sẵn dùng lại được nguyên vẹn:
`NetworkRunnerHandler.GetSpawnPosition(team, index)` và `PlayerHealth.Respawn(pos, yaw)`.

| Việc | Ước lượng |
|---|---|
| Hồi sinh có hẹn giờ | `TickTimer` trong `PlayerHealth`, đếm trong vòng lặp `CheckKillZone` đã có | ~40 dòng |
| `ControlZone.cs` mới | Đếm người trong bán kính bằng `PlayerHealth.AllPlayers`, không cần `OverlapSphere` | ~130 dòng |
| Sửa `GameManager` | Đổi điều kiện thắng round, **xoá** `EndRoundByCharge()` | ~50 dòng |
| HUD | Thanh chiếm đóng + đồng hồ hồi sinh | ~50 dòng + dựng UI |

**KHÔNG phải đụng tới:** kinh tế, Shop, rào chắn Buy Phase, âm thanh, `SpectatorController`,
toàn bộ `MagneticObject`. Cấu trúc round giữ nguyên (best-of-5), chỉ đổi *cách thắng một round*.

#### ⚠️ Bẫy BẮT BUỘC phải chốt trước khi code

`PlayerHealth.Respawn()` hiện **xả điện tích về 0**. Ghép với hồi sinh tự động thì:
đang nhiễm 95% điện, sắp bị hất bay → **tự nhảy xuống vực** → 5 giây sau sống lại sạch điện.
**Tự sát thành nước đi tối ưu.**

→ Cách chữa khuyên dùng: **chết thì đội địch được cộng tiến độ chiếm đóng (+8%)**.
Giải quyết gọn cả ba việc: tự sát luôn có giá · hất địch khỏi map **trực tiếp đẩy mình tới
chiến thắng** (knockback và mục tiêu khớp thành một) · không cần thêm luật rườm rà nào.
*(Phương án thay thế: hồi sinh chỉ xả 50% điện — đơn giản hơn nhưng không có sự cộng hưởng trên.)*

#### Phạm vi đề xuất cho bản đầu

- **Làm:** 1 khu chiếm đóng **cố định**, hình tròn. Cả hai đội cùng đứng = **đóng băng**. Đủ 100% = thắng round.
- **Cắt:** khu chiếm đóng **di chuyển** (kiểu Hardpoint). Tốn gấp đôi, phải cân bằng lại từ đầu.

#### Tin tốt về map

**Lõi Từ ở giữa map (mục 3) chính là khu chiếm đóng lý tưởng, không phải vẽ lại gì.**
Cao 7m trống trải → đứng đó là phơi mình · bị đấm trên cao thì bay xa hơn hẳn ·
cách rìa đủ xa để không chết ngay, đủ gần để một cú mạnh là xong · 4 dốc lên nên không thủ kín được.
Chỉ cần thêm `ControlZone` ở `(40, 7, 40)` bán kính ~7m.

#### Còn phải chốt 2 con số

1. **Thời gian hồi sinh** — đề xuất **5 giây**
2. **Chết có cộng tiến độ cho địch không** — đề xuất **có, +8%**

> 🚨 **Cảnh báo về việc đổi hướng liên tục.** Đây là lần đổi thiết kế thứ hai trong hai ngày
> (30/07 bỏ luật riêng từng loại đạn → 09/08 Quá Tải → 09/08 đề xuất này). Bản thân thay đổi
> hợp lý, nhưng còn **5 tuần và map vẫn chưa dựng**. Nếu quay lại làm cái này thì
> **làm xong rồi KHOÁ thiết kế**, dồn toàn bộ thời gian còn lại cho map và test.
> Có ý tưởng thứ ba thì ghi vào đây để đó, đừng làm.

</details>

- **Đền bù kinh tế khi đồng đội thoát giữa trận** — `OnPlayerLeft` chưa despawn nhân vật,
  người thoát để lại xác đứng im. Chủ project muốn gộp với cơ chế bù tiền 1v2, chưa chốt con số.
- **Xử lý hoà** (cả hai đội cùng chết) — tạm chọn: không ai được điểm, sang round mới.

---

## 7. Đề xuất thứ tự cho 6 tuần còn lại

Còn ~5 tuần tính từ 09/08.

| Tuần | Việc |
|---|---|
| 1 | **Map — dựng blockout bằng cube theo bố cục "Lõi Từ"**, test Quá Tải, cân lại `knockbackAtMaxCharge` |
| 2 | *(nếu chọn làm)* Hồi sinh + Khu chiếm đóng — xem mục 6. Rồi **KHOÁ THIẾT KẾ** |
| 3 | Dán art lên map + âm thanh + dựng nốt Settings/Spectator |
| 4 | Test kỹ 2 máy, cân bằng lại toàn bộ số liệu |
| 5 | **Dự phòng + build + viết báo cáo + chuẩn bị bảo vệ** |

Chừa tuần cuối là cố ý. Đồ án luôn phát sinh.

⚠️ Nếu tới cuối tuần 2 mà map vẫn chưa chơi được thì **bỏ mục 6, giữ nguyên Quá Tải** và dồn hết
cho map. Game có bố cục tử tế mà luật đơn giản vẫn hơn game luật hay mà map toàn cube.
