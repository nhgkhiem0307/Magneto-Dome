# TIẾN ĐỘ — Magneto-Dome

> Nhật ký tiến độ. Cập nhật sau mỗi buổi làm việc.
> Đọc kèm [CLAUDE.md](CLAUDE.md) — file đó chứa đặc tả và quy tắc, file này chỉ ghi *đang làm tới đâu*.

**Cập nhật lần cuối:** 30/07/2026
**Deadline:** khoảng 16/09/2026 (còn ~7 tuần)

---

## Lộ trình (tư duy Valorant)

| # | Giai đoạn | Mức | Tiến độ |
|---|---|---|---|
| 0 | Chuyển gameplay sang Fusion Host Mode | 🔴 Bắt buộc | ✅ **XONG** |
| 1 | Khung xương vòng đấu (`GameManager`) | 🔴 Bắt buộc | ✅ **XONG** |
| 2 | Buy Phase + rào chắn spawn | 🔴 Bắt buộc | 🟡 Code xong, **chờ dựng rào trong Unity** |
| — | Đại tu túi đồ cho chạy với Fusion | 🔴 Bắt buộc | ✅ **XONG** — chờ test |
| 3a | Kinh tế (tiền, thưởng round) + thanh giáp | 🟡 Cắt được | ✅ **XONG — đã test** |
| 3b | Shop UI + mua bán | 🟡 Cắt được | ✅ **XONG — đã test** |
| 3c | 4 hiệu ứng vật phẩm + Radial Menu | 🟡 Cắt được | ✅ **XONG** |
| 4 | HUD | 🔴 Bắt buộc | ⬜ **← đang tới đây** |
| 5 | Chết & Quan sát (xem qua mắt đồng đội) | 🟢 Cắt được | 🟡 **Code xong** — chờ dựng UI |
| 6a | Âm thanh | 🔴 Bắt buộc | 🟡 **Code xong** — chờ gán file âm thanh |
| 6b | Settings UI | 🔴 Bắt buộc | 🟡 **Code xong** — chờ dựng UI |
| 6c | Polish + build | 🔴 Bắt buộc | ⬜ |

### Chia thời gian dự kiến

| Tuần | Việc |
|---|---|
| 1 | Giai đoạn 2 (buy phase, rào chắn) |
| 2 | Đại tu `InventorySystem` + Giai đoạn 3 (kinh tế, shop) |
| 3 | Giai đoạn 4 — HUD |
| 4 | Âm thanh + Settings UI |
| 5 | Polish, build, sửa lỗi |
| 6–7 | **Dự phòng + viết báo cáo + chuẩn bị bảo vệ** |

Chừa 2 tuần cuối là **cố ý**. Đồ án luôn phát sinh, và còn phải viết báo cáo.

---

## Trạng thái từng script

| Script | Trạng thái |
|---|---|
| `NetworkRunnerHandler.cs` | ✅ Lobby, gửi input, spawn theo đội, spawn GameManager |
| `RoomPlayer.cs` | ✅ Đồng bộ tên + đội trong phòng chờ |
| `NetworkInputData.cs` | ✅ 7 nút (kèm 1 nút debug cần xoá trước khi nộp) |
| `FPSMovement.cs` | ✅ Đã test 2 máy |
| `PlayerHealth.cs` | ✅ Máu, đội, sống/chết, hồi sinh |
| `PlayerMagnetController.cs` | ✅ Điện tích, cận chiến, hút/đẩy/cầm/bắn |
| `MagneticObject.cs` | ✅ `NetworkBehaviour` + Fusion Physics Addon |
| `GameManager.cs` | ✅ Vòng đấu, tính điểm, hồi sinh, KillZone, Overtime |
| `RoundBarrier.cs` | ✅ Rào chắn Buy Phase *(cố ý không networked)* |
| `PlayerEconomy.cs` | ✅ Ví tiền, thưởng cuối round, trần $750 |
| `ShopManager.cs` | ✅ Giao dịch qua RPC, Host duyệt |
| `MenuUI/ShopUI.cs` | ✅ Panel cửa hàng, phím `B` |
| `MenuUI/ShopItemButton.cs` | ✅ Một ô hàng: icon, tên, giá |
| `Item/EMBarrier.cs` | ✅ Tường tạm, tự huỷ sau 10s |
| `InventorySystem.cs` | ✅ Viết lại dùng `NetworkArray<NetworkBehaviourId>` |
| `PlayerHotbarController.cs` | ✅ `Z`/`X`/`C` đi qua mạng |
| `PlayerInteract.cs` | ✅ Phím `F` đi qua mạng |
| `RadialMenuController.cs` | 🟡 **Đã viết lại xong**, nhưng chưa dựng UI nên chưa chạy |

---

## Đã chạy được và đã kiểm chứng trên 2 máy

- Tạo phòng bằng mã PIN, vào phòng, đổi đội, hiển thị danh sách 2 đội
- Host bắt đầu trận → load `TestScene` → spawn mỗi người đúng vị trí + hướng nhìn của đội mình
- Di chuyển WASD, xoay chuột, Dash `Q`
- Đổi điện tích `1`/`2` — đối thủ thấy đúng cực găng
- Cận chiến đủ 3 trường hợp: đấm văng, nảy bật, kéo áp sát
- Nạp điện, hút, cầm, ném vật thể — **vật dính chặt vào tay, không trễ**
- Máu host-authoritative, không bị trừ trùng
- **Vòng đấu trọn vẹn**: BuyPhase → Combat → RoundEnd → tính điểm → hồi sinh → lặp lại

### Giá trị đã chỉnh tay ở Inspector (ghi lại phòng khi prefab hỏng)

`meleeRange = 4` · `dashLockRange = 20` · `meleePushForce = 100` · `grapplePullForce = 100`
`moveSpeed = 13` · `mouseSensitivity = 1` · `dashForce = 100`
`redTeamSpawnPoint = (40, 6, 60)` · `blueTeamSpawnPoint = (40, 6, 20)`

---

## 🧠 Bài học đã rút ra (đọc trước khi debug)

### 1. "Giật" khi test ParrelSync thường KHÔNG phải lỗi mạng

Mất nhiều thời gian nghi ngờ đồng bộ, cuối cùng đo ra **FPS chỉ 15–30** ở cửa sổ clone.
Hai Unity Editor chạy URP trên một máy là quá nặng. Test trên **máy phụ → mượt hoàn toàn**.

**Lần sau thấy giật, ĐO FPS TRƯỚC KHI nghi ngờ code mạng.** Cách giảm tải:
1. Đóng tab **Scene view** ở cửa sổ clone (nó vẽ lại toàn cảnh lần nữa — ăn nhiều nhất)
2. Bật **VSync** (`Project Settings → Quality`), hiện đang `0` nên cửa sổ focus giành sạch CPU
3. Hạ độ phân giải Game view ở clone
4. Chuẩn nhất: **build ra `.exe`** rồi chạy 1 build + 1 Editor

### 2. Chỉ đồng bộ những gì bắt buộc

Vật cầm trên tay từng bị trễ nặng vì Host đặt vị trí rồi truyền qua mạng. Nhưng vị trí vật
**suy ra được** từ vị trí bàn tay — mà máy nào cũng biết chính xác. Giải pháp: chỉ truyền
`GrabbedObjectId` (ai cầm vật nào), còn mỗi máy tự đặt vật vào tay trong `LateUpdate()`.

Sẽ dùng lại nguyên tắc này cho hiệu ứng găng tay, âm thanh, animation.

### 3. Đã thử và BỎ: cho Client dự đoán trước thao tác vật thể

Gỡ `if (!HasStateAuthority) return;` để Client tự đoán → **tệ hơn hẳn**, vật cầm trên tay
giật và trễ nặng hơn. Đã lùi. Kết luận: **độ trễ đều đặn dễ quen tay hơn giật ngẫu nhiên.**

### 4. Đẩy vật từ môi trường không gây sát thương *(đã sửa 30/07)*

Vật nằm dưới đất thì **đang chạm mặt đất**. Vừa đẩy đi là dính ngay một lần `OnCollisionEnter`
với mặt đất, mà code cũ coi mọi va chạm không phải người chơi là "hết bay" → gọi
`ResetBulletState()` → mất tư cách đạn trước cả khi bay tới đối thủ.

Bắn từ tay thì không dính, vì lúc đó vật đang lơ lửng giữa không trung.

**Cách sửa:** xét **tốc độ** thay vì "cứ va là hết". Còn bay nhanh hơn `minBulletSpeed` (mặc định 3)
thì vẫn là đạn. Kèm `bulletArmTime` (0.25s) khoá lúc vừa phóng, vì lực đẩy phải sang bước vật lý
kế tiếp mới biến thành vận tốc.

**Bài học chung:** hai đường "đẩy từ môi trường" và "bắn từ tay" trước đây tự viết riêng nên
lệch nhau. Giờ cả hai gọi chung `MagneticObject.LaunchAsBullet()`. Có logic trùng lặp ở hai nơi
thì sớm muộn cũng lệch.

### 5. Các bẫy Fusion khác

Xem **CLAUDE.md mục 5** — có danh sách 6 bẫy đã gặp kèm cách chữa.

---

## 🔴 Quyết định đang chờ

### Đền bù kinh tế khi đồng đội thoát giữa trận

`OnPlayerLeft` hiện **chưa despawn** nhân vật — người thoát để lại "xác" đứng im trên map.
Cố ý chưa làm, vì muốn gộp chung với cơ chế: 1v2 thì người còn lại được bù tiền, 1v1 giữ nguyên.
**Chưa chốt con số.**

### Xử lý hoà (cả hai đội cùng chết)

Đã tạm chọn: **không ai được điểm, sang round mới luôn.** Sửa dễ nếu muốn khác.

---

---

---

---

# 🔖 ĐANG DỞ TỚI ĐÂY — đọc mục này trước tiên

Dừng giữa chừng ngày 30/07 khi đang làm **Radial Menu**.
Shop đã xong và đã test: mua bán, trừ tiền, giáp, thưởng cuối round đều chạy đúng.

## 1. Một sửa đổi code CHƯA áp dụng

Trong `FPSMovement.Spawned()` còn đoạn tắt `RadialMenuController` trên nhân vật người khác:

```csharp
if (!isMine)
{
    RadialMenuController radialMenu = GetComponent<RadialMenuController>();
    if (radialMenu != null) radialMenu.enabled = false;
}
```

Đoạn này **giờ đã thành code chết** — Radial Menu không còn nằm trên prefab nhân vật nữa,
nó đã chuyển sang Canvas. Xoá đi là xong, không ảnh hưởng gì.

## 2. Phát hiện quan trọng về Radial Menu cũ

Kiểm tra `Player.prefab` thấy `radialMenuUI: {fileID: 0}` và `slots: []` — **rỗng hoàn toàn**.

Nghĩa là Radial Menu **chưa từng chạy được** trong bản multiplayer. Các tham chiếu UI ngày xưa
được gán trên bản Player *đặt sẵn trong scene*, mà bản đó đã bị xoá khi chuyển sang spawn động.
Prefab thì chưa bao giờ được gán.

→ Vì vậy đã viết lại theo đúng kiểu `ShopUI`: **UI nằm trên Canvas trong scene**, không nằm
trên prefab nhân vật. Prefab không tham chiếu được tới object trong scene.

## 3. Cấu trúc UI Radial Menu cần dựng

```
Canvas
└── RadialMenuController          ← gắn script vào ĐÂY (giống ShopUI)
    │
    └── RadialMenuPanel           → kéo vào ô "Radial Menu UI"
        ├── Slot_EnergyDrink      Anchored Position (106, 106)    - trên phải
        │   ├── Background  (Image)  → ô Slot Image
        │   ├── Icon        (Image)  → ô Icon Image
        │   └── CountText   (TMP)    → ô Count Text
        │   + CanvasGroup trên chính Slot  → ô Canvas Group
        │   + RectTransform của Slot       → ô Slot Rect
        ├── Slot_Bandage          (-106,  106)   - trên trái
        ├── Slot_Gasoline         (-106, -106)   - dưới trái
        └── Slot_EMBarrier        ( 106, -106)   - dưới phải
```

**Vị trí 4 ô nằm ở GÓC CHÉO, không phải trên/dưới/trái/phải.**

Công thức chọn múi trong code là `floor(góc / 90)` với 4 ô, nên mỗi múi rộng 90° và
tâm của nó rơi vào đường chéo:

| Index | Vùng góc | Tâm ô | Món |
|---|---|---|---|
| `slots[0]` | 0°–90° | 45° trên-phải | Energy Drink |
| `slots[1]` | 90°–180° | 135° trên-trái | Bandage |
| `slots[2]` | 180°–270° | 225° dưới-trái | Gasoline Canister |
| `slots[3]` | 270°–360° | 315° dưới-phải | EM Barrier Core |

Đặt sai vị trí thì rê chuột lên trên nhưng lại sáng ô bên phải.
Cả 4 ô để **Anchor = Middle Center**.

Ô **`Item Data Source`**: kéo 4 asset `ItemData` của món tiêu hao vào, để menu tự lấy icon.
Nhờ vậy icon chỉ gán một lần trong `ItemData`, dùng chung cho cả Shop lẫn Radial Menu.

## 4. Việc trong Unity còn lại

- [ ] Xoá component `RadialMenuController` khỏi `Assets/Prefab/Player.prefab`
- [ ] Dựng UI Radial Menu trên Canvas theo cấu trúc trên
- [ ] Dùng **TextMeshPro**, không dùng Legacy Text
      *(bản cũ dùng `UI.Text`, bản mới đã đổi sang `TMP_Text` nên tham chiếu cũ sẽ không nhận)*

## 5. Sau khi dựng xong thì test

4 hiệu ứng vật phẩm **đã viết nhưng CHƯA CHẠY THỬ LẦN NÀO**:

| Món | Kỳ vọng |
|---|---|
| Energy Drink | Cooldown dash giảm 15% trong 15 giây. Đang còn hiệu lực mà dùng tiếp → từ chối, không mất đồ |
| Bandage | Máu 80/100 → hồi 10 → thành 90. Máu đầy mà dùng → từ chối, không mất đồ |
| Gasoline | Dùng xong bấm chuột trái vào vật thường → vật chuyển **màu cam** và thành TNT |
| EM Barrier | Thả tường trước mặt, tự biến mất sau 10 giây, cả 2 máy cùng thấy |

---

## Việc thủ công trong Unity còn tồn

- [x] ~~Dựng 2 rào chắn `Barrier_Red` / `Barrier_Blue`~~ — xong
- [x] ~~Thêm `PlayerEconomy` + `ShopManager` vào `Player.prefab`~~ — xong
- [x] ~~Cấu hình 5 asset `ItemData` + mảng `Catalogue`~~ — xong
- [x] ~~Dựng UI cửa hàng~~ — xong, đã test
- [ ] **Tạo prefab `EM_Barrier`** — bức tường tạm (NetworkObject + EMBarrier + Collider + Renderer),
      gán vào ô `Em Barrier Prefab` của `InventorySystem` trên `Player.prefab`
- [ ] **Dựng UI Radial Menu** — xem mục "ĐANG DỞ TỚI ĐÂY" ở trên
- [ ] Tạo prefab `GameManager` (NetworkObject + GameManager) và gán vào `NetworkRunnerHandler`
- [ ] Bật `Read/Write Enabled` cho 5 file `.fbx` trong
      `Assets/Resources/Polytope Studio/Lowpoly_Environments/Sources/Meshes/Trees/`
      (`PT_Pine_Tree_03_logs`, `PT_Pine_Tree_03_stump`, `PT_Pine_Tree_03_green_cut`,
      `PT_Fruit_Tree_01_logs`, `PT_Fruit_Tree_01_stump`)
      → dẹp cảnh báo QuickOutline. Không gấp, chỉ là cảnh báo vô hại.
- [ ] Chỉnh `Kill Zone Y` trên prefab GameManager cho khớp map (mặc định `-20`)

## ✅ Đã khép kín vòng lặp trận đấu (03/08)

`MatchEnd` giờ tự đưa mọi người về MenuScene sau `matchEndDuration` giây.

**Luồng:** Host hết giờ pha MatchEnd → `NetworkRunnerHandler.ReturnToMenu()` → tắt Runner
→ Client nhận `OnShutdown` → tự gọi `ReturnToMenu()` bên máy mình.

**Cái bẫy đã xử lý:** `NetworkRunnerHandler` là singleton `DontDestroyOnLoad`. Quay về MenuScene
thì bản cũ vẫn sống nhưng **mọi tham chiếu UI của nó đã chết theo scene cũ** → menu hiện ra
một đống nút không bấm được. Cách chữa: bỏ `Instance = null` rồi `Destroy` bản cũ **trước khi**
load scene, để bản nằm sẵn trong MenuScene được nhận vai.

Cũng dọn luôn các danh sách tĩnh (`RoomPlayer.AllPlayers`, `PlayerHealth.AllPlayers`,
`RoomPlayer.Local`) — chúng sống xuyên scene nên không tự mất, để sót thì trận sau đếm nhầm người.

**Cần test:** chơi tới khi một đội thắng chung cuộc → cả 2 máy phải cùng về MenuScene và
**tạo/vào phòng mới được bình thường**.

## 🔊 Âm thanh — code xong, chờ gán file (03/08)

`Assets/Scripts/AudioManager.cs` — singleton `DontDestroyOnLoad`, âm lượng lưu `PlayerPrefs`.

### Nguyên tắc: KHÔNG truyền âm thanh qua mạng

Mọi tiếng động móc vào các hàm `OnChangedRender` đã có sẵn — chúng vốn chạy trên **mọi máy**
mỗi khi trạng thái `[Networked]` đổi. Ai cũng nghe mà không tốn thêm băng thông.

### ⚠️ Bẫy: đừng phát tiếng trong `FixedUpdateNetwork()`

Fusion tua lại nhiều tick mỗi khung hình → một cú dash sẽ kêu 5–6 lần.

Cách chữa cho các hành động xảy ra trong tick: thêm một biến đếm `[Networked]` kèm
`OnChangedRender`. Con số không có ý nghĩa gì, nó chỉ tồn tại để mỗi hành động là
giá trị đổi đúng một lần. Đã dùng cho `DashCount`, `MeleeCount`, `LaunchCount`.

### 14 điểm phát tiếng đã móc

| Sự kiện | Móc ở đâu |
|---|---|
| Trúng đòn / giáp chặn / bị loại | `PlayerHealth` — 3 hàm `OnChangedRender` |
| Dash | `FPSMovement.OnDashPerformed` |
| Cận chiến | `PlayerMagnetController.OnMeleePerformed` |
| Nạp điện / phóng vật / chế TNT | `MagneticObject` — 3 hàm |
| TNT nổ | `MagneticObject.Despawned` *(vì `Explode()` chỉ chạy trên Host)* |
| Buy phase / rào hạ / thắng / thua round | `GameManager.OnPhaseChanged` |
| Mua hàng / dùng vật phẩm | `ShopManager` / `InventorySystem` |

### Việc trong Unity

- [ ] Tạo GameObject `AudioManager` trong **MenuScene**, gắn script `AudioManager`
      *(nó tự `DontDestroyOnLoad`, không cần đặt ở TestScene)*
- [ ] Tìm và gán các file âm thanh vào 18 ô clip
- [ ] Gọi `AudioManager.PlayMenuMusic()` / `PlayGameMusic()` khi cần đổi nhạc nền

Chưa gán clip nào thì game vẫn chạy bình thường, chỉ là im lặng — mọi hàm đều null-check.

## ⚙️ Settings — code xong, chờ dựng UI (03/08)

Hai file mới:
- `Assets/Scripts/GameSettings.cs` — class **tĩnh**, giữ độ nhạy chuột + toàn màn hình, lưu `PlayerPrefs`
- `Assets/Scripts/MenuUI/SettingsUI.cs` — bảng giao diện, đặt trên Canvas

Đủ 6 mục GDD chương 6 yêu cầu: **Master / Music / SFX volume, độ phân giải, fullscreen, độ nhạy chuột.**

### Độ nhạy chuột đã đổi nguồn

Trước đây `NetworkRunnerHandler` đọc thẳng `localPlayer.mouseSensitivity` từ prefab — người chơi
không đổi được lúc chạy. Giờ nó đọc `GameSettings.MouseSensitivity`.

Field trên prefab **vẫn còn tác dụng**: nó là giá trị mặc định cho lần chơi đầu tiên.
`FPSMovement.Spawned()` gọi `SeedDefaultSensitivity()` — chỉ ghi khi người chơi chưa từng tự chỉnh.

### Cấu trúc UI cần dựng

```
Canvas
└── SettingsUI                    ← gắn script vào ĐÂY, không phải vào panel
    └── SettingsPanel             → ô "Settings Panel"
        ├── Nhóm Âm thanh
        │   ├── MasterSlider  + MasterValueText
        │   ├── MusicSlider   + MusicValueText
        │   └── SfxSlider     + SfxValueText
        ├── Nhóm Điều khiển
        │   └── SensitivitySlider + SensitivityValueText
        ├── Nhóm Hiển thị
        │   ├── ResolutionDropdown   (TMP_Dropdown)
        │   └── FullscreenToggle     (Toggle)
        └── CloseButton           → OnClick: SettingsUI.Close()
```

**Không cần đặt Min/Max cho slider** — script tự đặt: âm lượng `0–1`, độ nhạy `0.1–5`.
Danh sách độ phân giải cũng tự sinh từ máy đang chạy, đã lọc trùng tần số quét.

Phím `Esc` mở/đóng, đổi được ở ô `Toggle Key`.

### Việc trong Unity

- [ ] Dựng bảng Settings trên Canvas của **MenuScene**
- [ ] *(tuỳ chọn)* Gắn thêm một bản nữa vào Canvas của **TestScene** để mở Settings giữa trận
- [ ] Nút mở Settings ở màn hình chính → OnClick: `SettingsUI.Open()`

## 👁 Quan sát khi chết — code xong (03/08)

`Assets/Scripts/MenuUI/SpectatorController.cs` — đặt trên **Canvas** của TestScene.

### Vì sao không cần đồng bộ gì thêm

Camera của nhân vật người khác trên máy mình **vốn đã xoay đúng sẵn**:
`FPSMovement.Render()` áp `NetPitch` cho họ, `NetworkTransform` lo hướng thân.
Nên "quan sát" chỉ là **đổi xem camera nào đang bật** — thuần cục bộ, 0 byte băng thông.

### Hành vi

- Bị loại → tự chuyển sang camera đồng đội còn sống
- Đồng đội đó cũng chết → tự nhảy sang người khác, hết người thì về camera của chính mình
- `Space` chuyển giữa các đồng đội *(2v2 thường chỉ có 1 nên ít dùng)*
- Hồi sinh → tự trả camera về cho mình
- **Không xem được đội địch**

HUD cũng bám theo: đang xem đồng đội thì hiện **máu/đạn của họ**, không phải máu 0 của xác mình.
Riêng tâm ngắm và bảng "đã bị loại" vẫn theo trạng thái thật của mình.

### Việc trong Unity

- [ ] Gắn `SpectatorController` lên **Canvas** của TestScene
- [ ] Tạo `SpectatorPanel` + `SpectatingText` (TMP) → kéo vào 2 ô tương ứng
- [ ] *(Không cần gán gì trên prefab nhân vật)*

## Việc phải làm TRƯỚC KHI NỘP BÀI

- [ ] Xoá `InputButton.DebugSuicide` khỏi `NetworkInputData.cs`
- [ ] Xoá dòng gửi phím `K` trong `NetworkRunnerHandler.OnInput()`
- [ ] Xoá khối xử lý tự sát trong `PlayerMagnetController.FixedUpdateNetwork()`
