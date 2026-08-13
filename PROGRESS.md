# TIẾN ĐỘ — Magneto-Dome

> Ghi cho session sau. Đọc file này trước, rồi đọc [CLAUDE.md](CLAUDE.md) để nắm đặc tả và quy tắc.
> **Cập nhật:** 09/08/2026 · **Deadline:** ~16/09/2026 (còn ~5 tuần)

---

## 1. Tóm tắt một dòng

Game **chạy được trọn vẹn**: vào phòng → chia đội → đánh nhau → tính điểm → mua đồ → dùng đồ
→ có đội vô địch → về menu. **Toàn bộ code đã xong.** Việc còn lại là **map** và một ít dựng UI.

> ⚡ **09/08: đã đổi mục tiêu tối thượng sang CHẾ ĐỘ QUÁ TẢI.** Không còn thanh máu — trúng đòn
> nạp điện tích, càng nhiễm càng bị hất xa, chết chỉ khi rơi khỏi đảo. Code xong và đã biên dịch
> sạch (`dotnet build`, 0 error), **chưa test lần nào.** Đặc tả đầy đủ ở [CLAUDE.md](CLAUDE.md) mục 4.

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

### 🟡 Dựng UI còn thiếu

| Việc | Ghi chú |
|---|---|
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

### 🔵 ĐỀ XUẤT LỚN: Hồi sinh + Khu chiếm đóng *(bàn 09/08, CHƯA làm, chủ project sẽ quay lại)*

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
