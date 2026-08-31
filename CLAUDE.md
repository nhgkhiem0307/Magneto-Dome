# CLAUDE.md — Magneto-Dome

Tài liệu này là bộ nhớ dài hạn cho Claude Code khi làm việc trên project này.
Ngôn ngữ làm việc: **tiếng Việt** (cả trao đổi lẫn comment trong code).

> 📌 **Đang làm tới đâu → xem [PROGRESS.md](PROGRESS.md).**
> File CLAUDE.md này chứa *đặc tả và quy tắc* (ít thay đổi).
> File PROGRESS.md chứa *tiến độ* (cập nhật liên tục). Đọc cả hai trước khi bắt tay vào việc.

---

## 1. Bối cảnh dự án

**Magneto-Dome** — đồ án tốt nghiệp (Capstone) của Nguyễn Hoàng Gia Khiêm, lớp K24M-GAME1, chuyên ngành Lập trình Game.

| Mục | Giá trị |
|---|---|
| Thể loại | FPS đối kháng 2v2, Round-based Tactical Arena PvP |
| Bối cảnh | "Sky Arena" — hòn đảo lơ lửng giữa không trung |
| Điểm độc đáo | Không có súng đạn. Dùng **găng tay từ tính** hút/đẩy vật thể trong map làm đạn |
| Làm một mình | Có — không có team |
| Deadline | Khoảng **giữa tháng 9/2026** (7 tuần tính từ 29/07/2026) |
| Phạm vi bắt buộc | Gameplay hoàn chỉnh + thắng/thua + UI + hình ảnh + âm thanh |

### Về file GDD.txt
GDD là bản nháp **hơi sơ sài và không bắt buộc phải bám sát**. Khi GDD mâu thuẫn với thực tế code hoặc với quyết định gần đây của chủ project, **ưu tiên quyết định của chủ project**. Các mục đã chốt được ghi lại ở Mục 4 bên dưới — dùng mục đó làm chuẩn.

---

## 2. Công nghệ & môi trường

| Thành phần | Phiên bản / Ghi chú |
|---|---|
| Unity | **6000.3.17f1** (Unity 6) |
| Render Pipeline | **URP 17.3.0** |
| Multiplayer | **Photon Fusion 2** (`Assets/Photon/`) |
| Input System | `com.unity.inputsystem` **1.19.0** — đã cài sẵn |
| Test đa client | **ParrelSync** (`Assets/ParrelSync/`) |
| IDE | Visual Studio / Rider |
| OS | Windows 11, shell PowerShell |

### Scene trong Build Settings

| Index | Đường dẫn | Vai trò |
|---|---|---|
| 0 | `Assets/Scenes/SampleScene.unity` | Scene mặc định, chưa dùng |
| 1 | `Assets/MenuScene.unity` | Menu + Lobby + ghép trận |
| 2 | `Assets/TestScene.unity` | **Scene gameplay chính** |

> Ngoài ra còn `Assets/_Recovery/` chứa 7 bản backup scene do Unity tự tạo khi crash (21–27/07/2026). **Không xoá** — đây là phao cứu sinh nếu scene hỏng.

---

## 3. Kiến trúc code hiện tại

### Quy ước quan trọng: đã networked hay chưa?

Đây là điều **quan trọng nhất** cần nhớ về trạng thái project:

| Đã networked (Fusion) | Vẫn là MonoBehaviour thuần |
|---|---|
| `Assets/NetworkRunnerHandler.cs` | `Assets/Scripts/Player/RadialMenuController.cs` ⚠️ |
| `Assets/RoomPlayer.cs` | `Assets/Scripts/Item/MagneticAura.cs` *(chỉ hiệu ứng, không cần)* |
| `Assets/Scripts/GameManager.cs` | `Assets/Scripts/RoundBarrier.cs` *(cố ý — xem ghi chú)* |
| `Assets/Scripts/Player/FPSMovement.cs` | `Assets/Scripts/Dummy/*.cs` *(bù nhìn tập bắn)* |
| `Assets/Scripts/Player/NetworkInputData.cs` | |
| `Assets/Scripts/Player/PlayerHealth.cs` | |
| `Assets/Scripts/Player/PlayerMagnetController.cs` | |
| `Assets/Scripts/Player/InventorySystem.cs` | |
| `Assets/Scripts/Player/PlayerHotbarController.cs` | |
| `Assets/Scripts/Player/PlayerInteract.cs` | |
| `Assets/Scripts/Item/MagneticObject.cs` | |

⚠️ `RadialMenuController` **vẫn đọc `Input.` trực tiếp** nên chạy trên cả nhân vật người khác.
Đã vá tạm bằng cách tắt component đó trong `FPSMovement.Spawned()` khi không phải nhân vật mình.
Sẽ chuyển sang `NetworkBehaviour` cùng lúc làm Shop.

`RoundBarrier` **cố ý KHÔNG networked**: trạng thái đóng/mở suy ra được từ `GameManager.Phase`
(vốn đã `[Networked]`), nên mỗi máy tự tính là khớp nhau. Xem nguyên tắc ở PROGRESS.md.

### Cây thư mục script (code do chủ project viết)

```
Assets/
├── NetworkRunnerHandler.cs      # Khởi tạo Fusion Runner, Host/Client, UI panel menu
├── RoomPlayer.cs                # NetworkBehaviour: đồng bộ NickName + Team trong lobby
└── Scripts/
    ├── Player/
    │   ├── FPSMovement.cs               # [Fusion] WASD + xoay + Dash(Q) + trọng lực + AddImpact()
    │   ├── NetworkInputData.cs          # [Fusion] struct gói input gửi qua mạng + enum InputButton
    │   ├── PlayerMagnetController.cs    # Nạp điện, hút/đẩy, cận chiến, cầm/bắn vật thể
    │   ├── PlayerHealth.cs              # Máu (chưa có giáp)
    │   ├── InventorySystem.cs           # Túi đồ: lưu GameObject thật đã bị ẩn đi
    │   ├── PlayerHotbarController.cs    # Z/X/C rút đạn Normal/Heavy/Spike từ túi
    │   ├── RadialMenuController.cs      # UI vòng tròn giữ Tab, chọn theo góc chuột
    │   └── PlayerInteract.cs
    ├── Item/
    │   ├── MagneticObject.cs            # Điện tích, 4 loại vật thể, va chạm, nổ TNT
    │   ├── MagneticAura.cs              # Hiệu ứng hào quang theo điện tích
    │   └── ItemData.cs                  # ScriptableObject định nghĩa vật phẩm
    ├── MenuUI/
    │   └── RoomItemUI.cs
    └── Dummy/
        ├── DummyGravity.cs              # Bản sao FPSMovement cho bù nhìn tập bắn
        └── DummyMagnetTarget.cs
```

Ngoài ra `Assets/Photon/`, `Assets/ParrelSync/`, `Assets/Resources/` (asset store), `Assets/TutorialInfo/` là **code bên thứ ba — không sửa**.

### Script sẽ tạo trong tương lai
Các file này GDD có nhắc tới nhưng **chưa tồn tại**, sẽ tạo dần khi làm multiplayer:
`GameManager.cs`, `ShopManager.cs`, `NetworkLobbyManager.cs`, `HUDController.cs`.

### Quyết định về cấu trúc: GIỮ CẤU TRÚC GỘP
GDD Chương 5 đề xuất tách nhiều file (`PlayerCameraController.cs`, `HeavyObject.cs`, `SpikeObject.cs`, `ExplosiveObject.cs`). **Không làm theo.** Chủ project đã chốt giữ cấu trúc gộp hiện tại:
- Logic camera nằm trong `FPSMovement.cs`, không tách ra file riêng.
- Cả 4 loại vật thể dùng chung `MagneticObject.cs` với `enum ObjectType`, không tách thành class kế thừa.
- `RoomPlayer.cs` đang làm việc của `NetworkLobbyManager`.

GDD sẽ được sửa lại cho khớp code sau, không phải ngược lại.

---

## 4. Đặc tả gameplay (bản chốt)

### Điều khiển

| Phím | Chức năng |
|---|---|
| `WASD` + chuột | Di chuyển, xoay góc nhìn FPS (dùng `CharacterController`) |
| `Space` | Nhảy. **Lái được khi đang ở trên không** (`FPSMovement.airControl`) |
| `Q` | Dash / Lướt (có cooldown) |
| `1` / `2` | Đổi điện tích găng tay: Dương (Đỏ) / Âm (Xanh) |
| Chuột trái | Vật trung tính → nạp điện. Vật đã có điện → **cùng dấu = ĐẨY**, **trái dấu = HÚT về tay** |
| Chuột phải | Tay trống → cận chiến. Đang cầm vật → **bắn vật đi** |
| `V` | Tung hứng vật đang cầm lên không |
| `Z` / `X` / `C` | Rút đạn Normal / Heavy / Spike từ túi ra tay |
| `Tab` (giữ) | Mở Radial Menu, xoay chuột chọn, thả phím để dùng |
| `B` | Mở Shop (chỉ trong Buy Phase) — *chưa làm* |

### Cận chiến (chuột phải, tay trống)

- **Khoảng cách < 3m, cùng dấu** → Đấm đẩy văng đối thủ (Repel Punch).
- **Khoảng cách < 3m, trái dấu** → Nảy bật nhẹ cả hai bên.
- **Khoảng cách 3–8m, trái dấu** → Kéo áp sát (Grapple): lướt nhanh về phía đối thủ.
  > GDD mô tả có "xích từ trường" vẽ bằng LineRenderer. **Chốt: giữ cách làm hiện tại** (`DashToEnemyRoutine` trong `PlayerMagnetController.cs`) vì đơn giản hơn. Có thể thêm LineRenderer sau nếu còn thời gian.

### Hệ sinh thái đạn (vật thể từ tính)

> ✅ **Cập nhật 09/08:** luật riêng của Heavy và Spike **đã được làm lại**, không còn "tạm bỏ"
> như ghi chú cũ ngày 30/07 nữa. Cả hai nằm trong `PlayerMagnetController` ở nhánh HÚT (trái dấu).
> Riêng tính chất "Spike cắm dính vào người trúng" thì vẫn bỏ, chưa làm.

> ⚠️ **Từ 09/08 các con số dưới đây là ĐIỂM ĐIỆN TÍCH, không phải HP** — xem mục Quá Tải bên dưới.
> Tên hàm `TakeDamage()` và field `baseDamage` giữ nguyên, chỉ ý nghĩa con số là đổi.

| Loại | Nạp điện | Đặc tính theo thiết kế gốc | Đã làm? |
|---|---|---|---|
| **Normal** | 10 | Hút/đẩy linh hoạt | ✅ |
| **Heavy** | 20 | Đang bay thì **KHÔNG hút về tay được**, chỉ cản: tốc ×0.5, sát thương ×0.5 | ✅ *(chưa test)* |
| **Spike** | 25 | Hút nhầm khi nó đang bay tới thì **ăn x2 (50)**. Đã bỏ tính chất cắm dính | ✅ *(chưa test)* |
| **TNT** | 35 | Nổ bán kính 6m, giảm dần theo khoảng cách, hất văng diện rộng | ✅ |

**Heavy và Spike là cặp đối xứng** — cùng dùng `MagneticObject.TryCounterInFlight()` (chỉ cho can
thiệp 1 lần mỗi cú bay, chặn spam giữ chuột), nhưng ý nghĩa ngược nhau: Heavy **thưởng** cho phản
xạ hút, Spike **phạt** phản xạ hút. Người chơi phải nhìn màu vật trước khi bấm.

⚠️ **Heavy không tự nặng.** Code chỉ cho nó `baseDamage 20` + không hút được; khối lượng vật lý
hoàn toàn do `Rigidbody.mass` đặt tay trên prefab. Để mass = 1 giống Normal thì nó bay nhanh y hệt
Normal và người chơi không cảm nhận được gì. Đặt `mass` 3–5 khi làm prefab Heavy.

### ⚡ CHẾ ĐỘ QUÁ TẢI — mục tiêu tối thượng (chốt 09/08/2026)

**Đây là thay đổi thiết kế lớn nhất của project. Game KHÔNG CÒN THANH MÁU.**

Trúng đòn không làm mất máu — nó **nạp điện tích** vào người. Càng nhiễm điện, từ trường
tác động lên bạn càng mạnh, nên cùng một cú đấm sẽ hất bạn đi càng xa.

| Luật | Chi tiết |
|---|---|
| Thanh đo | Điện tích `0 → 100`. Đầy 100% **KHÔNG chết** |
| Lực văng | Nhân tuyến tính `x1.0` (sạch điện) → `x5.0` (đầy điện) |
| Cái chết **duy nhất** | Rơi khỏi đảo (`GameManager.CheckKillZone`) |
| Hết giờ Combat (90s) | Đội có **tổng điện tích thấp hơn** thắng round. Người đã rơi tính là đầy 100 |
| Sang round mới | Xả sạch điện về 0 |

**Vì sao đầy điện không chết:** nếu đầy là chết thì đây chỉ là thanh máu chạy ngược, không có gì mới.
Để cái chết đến từ **VỊ TRÍ** mới tạo được sự căng thẳng thật — đứng giữa sân với 90% điện vẫn an toàn,
đứng sát rìa với 30% đã là mạo hiểm. Đây là cơ chế phần trăm của Smash Bros. đặt vào FPS từ tính.

**Vì sao pha Combat phải có giới hạn giờ:** không còn ai chết vì hết máu, nên hai bên cùng né rìa vực
thì round kéo dài vô tận. Đồng hồ 90s là bắt buộc, không phải tuỳ chọn.

**Hai đường `AddImpact()` phải tách bạch — ĐỪNG GỘP LẠI:**

| Loại chuyển động | `scaleByCharge` | Ví dụ |
|---|---|---|
| Bị đẩy từ bên ngoài | `true` *(mặc định)* | Đấm cận chiến, nổ TNT, trúng vật thể |
| Tự mình tạo ra | `false` | Dash (`Q`), Grapple kéo áp sát |

Nếu nhân hệ số cho cả Dash thì người sắp thua sẽ lướt xa gấp 5 lần — vừa vô lý vừa vỡ cân bằng.

**Ba nguồn knockback, đừng nhầm lẫn** *(cập nhật 09/08)*:

| Nguồn | Lực | Ở đâu |
|---|---|---|
| Đấm cận chiến | `meleePushForce` 200 | `PlayerMagnetController` |
| **Trúng đạn** | `hitKnockbackForce` 120 × (sátthương/10) | `MagneticObject.OnCollisionEnter` |
| Nổ TNT | `explosionForce × 2` = 30 ⚠️ yếu bất thường | `MagneticObject.Explode()` |

⚠️ Knockback khi trúng đạn **trước 09/08 KHÔNG TỒN TẠI** — va chạm chỉ trừ máu rồi thôi.
Nếu thấy nhân vật không bị đẩy khi trúng đạn thì kiểm tra nhánh `CompareTag("Player")` trước tiên.

**Buff sát thương toàn cục:** `PlayerHealth.chargeGainMultiplier` (1.5) nhân **mọi** lượng điện
nhận vào, ngay đầu `TakeDamage()` trước cả giáp. Muốn chỉnh độ sát thương chung thì sửa ô này,
**đừng** đi sửa `baseDamage` từng prefab.

**Ánh xạ lại 2 món Shop** (giữ nguyên tên hàm nên `ShopManager`/`InventorySystem` không phải sửa):
- `AddArmor()` → **Giáp Cách Điện**: hấp thụ điện thay cơ thể, hỏng dần
- `ApplyBandage()` → **Bộ Xả Điện**: xả tối đa 20 điểm, không quá 50% lượng đang mang

⚠️ **Mode này phụ thuộc nặng vào map.** Bị hất bay 15m chỉ đáng sợ khi có rìa vực để rơi.
Trên map cube hiện tại gần như không ai chết, mọi round kết thúc bằng hết giờ — **đó là dấu hiệu
map chưa sẵn sàng, không phải mode hỏng.**

### Vòng đấu & Điều kiện thắng (BẢN CHÍNH THỨC — theo GDD FR-7)

- Thắng 1 round = **hạ gục toàn bộ đội đối phương** *(nay là: hất hết họ khỏi đảo)*,
  **hoặc** tổng điện tích thấp hơn khi hết giờ 90s.
- Chết là **bị loại khỏi round đó**, hồi sinh khi round sau bắt đầu.
- Đội thắng round được **+1 điểm**.
- Thắng chung cuộc: **đạt 5 điểm trước VÀ cách biệt tối thiểu 2 round**.
- Nếu tỉ số 5-4 → vào **Overtime**, đấu tiếp tới khi một đội hơn 2 round (6-4, 7-5...).

> Ghi chú: có lúc chủ project nói "đến 5 trước là thắng luôn", nhưng đã **chốt lại dùng bản GDD có luật cách biệt 2 round**.

### Buy Phase & Kinh tế (chưa làm)

- Đầu mỗi round có **15 giây** đếm ngược, người chơi bị chặn bởi rào chắn (Barrier).
- Thắng round: **+$150**. Thua round: **+$100**. Trần ví: **$750**.

### Cửa hàng (chưa làm)

| Vật phẩm | Giá | Hiệu ứng |
|---|---|---|
| **Shield Armor** *(Giáp Cách Điện)* | $150 | Hấp thụ điện tích thay cơ thể. **Không nằm trong Radial Menu.** Reset mỗi khi hết round |
| **Energy Drink** | $100 | Giảm 15% cooldown Dash |
| **Bandage** *(Bộ Xả Điện)* | $100 | Xả tối đa 20 điểm điện, nhưng không quá 50% lượng đang mang |
| **Gasoline Canister** | $150 | Chuột trái vào 1 vật Normal trong tầm → biến nó thành thùng TNT |
| **EM Barrier Core** | $200 | Thả lõi tạo tường chắn trước mặt, tồn tại 10 giây |

Radial Menu **không làm chậm thời gian** (no slow-motion) ở cả offline lẫn online — đây là chủ ý thiết kế, đòi hỏi phản xạ người chơi.

### InventorySystem — thiết kế gộp

`InventorySystem` là **một hệ thống duy nhất chứa cả hai loại**:
1. **GameObject vật thể thật** nhặt từ map (bàn, ghế...) dùng làm đạn.
2. **Vật phẩm tiêu hao** mua từ Shop.

Cả hai đều có thể lấy ra từ **Hotbar (`Z`/`X`/`C`)** lẫn **Radial Menu (`Tab`)**.

**Cơ chế (viết lại 30/07 để chạy được với Fusion):** túi chỉ lưu **ID** của vật thể trong một
`NetworkArray<NetworkBehaviourId>` sức chứa 16. Bản thân vật thể **vẫn tồn tại trong thế giới**,
chỉ bị tắt renderer và collider đi (`MagneticObject.IsStored`). Rút ra thì bật lại và dịch chuyển
tới tay. Lấy theo thứ tự LIFO.

> ⚠️ Bản cũ dùng `SetParent()` + `SetActive(false)` — **cả hai đều không dùng được với
> `NetworkObject`**. Fusion không hỗ trợ đổi cha giữa trận, và tắt hẳn một `NetworkObject`
> làm hỏng vòng đời mô phỏng của nó. Đừng quay lại cách cũ.
>
> Cũng **đừng dùng Despawn/Spawn lại**: có 5 prefab khác nhau cùng thuộc loại `Normal`,
> spawn lại sẽ ra nhầm prefab. Cách "tắt đi" giữ nguyên đúng vật, đúng điện tích, đúng sát thương.

### Đã hoãn — chưa cần nghĩ tới
- Kỹ năng **`E` — Blink** (dịch chuyển tức thời 10m)
- Kỹ năng **`R` — Phòng thủ** (giảm tốc chạy, giảm sát thương và lực đẩy)

---

## 5. Mục tiêu kỹ thuật: chuyển sang Photon Fusion 2 Host Mode

**Đây là ưu tiên số 1 hiện tại.**

- Kiến trúc chốt: **Host Mode (server-authoritative)** — máy Host giữ `StateAuthority`, tính toàn bộ vật lý để chống gian lận. Chọn Host Mode vì ổn định hơn Shared Mode.
- Region cố định: **`asia`** — **đã cấu hình sẵn** ở `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` (dòng `FixedRegion: asia`), không phải trong code. Đây là cách làm đúng của Fusion 2, không cần set lại trong `NetworkRunnerHandler`.
- Vật thể từ tính (bàn, ghế, TNT) **cần đồng bộ vị trí qua mạng** vì chúng chính là "đạn" gây sát thương → sẽ phải trở thành `NetworkObject` + `NetworkRigidbody3D`. Đây là phần nặng và rủi ro nhất.
- Input: sẽ chuyển sang **New Input System** (package đã cài sẵn), kết hợp với `NetworkInput` struct của Fusion.
- Mục tiêu phi chức năng: **≥60 FPS**, **ping < 100ms** ở region asia.

### ⚠️ Bẫy đã gặp khi chuyển sang Fusion — ĐỌC TRƯỚC KHI CHUYỂN SCRIPT MỚI

Những lỗi dưới đây đã tốn thời gian để tìm ra. Khi chuyển `PlayerMagnetController`,
`PlayerHealth`... sang `NetworkBehaviour`, rất có thể sẽ gặp lại.

**1. `CharacterController` làm nhân vật spawn ở (0,0,0)**
CharacterController giữ một bản toạ độ riêng bên trong nó. Lệnh `Move()` đầu tiên sẽ kéo
nhân vật về toạ độ gốc của prefab thay vì giữ ở chỗ vừa spawn.
→ Cách chữa: `controller.enabled = false;` rồi `controller.enabled = true;` trong `Spawned()`.
Chính Photon cũng làm vậy trong `NetworkCharacterController.cs` của họ (có comment thừa nhận).

**2. `CharacterController` làm Client chạy nhanh gấp nhiều lần**
Fusion "tua lại" (resimulation) nhiều tick mỗi khung hình. `NetworkTransform` tua transform
về đúng vị trí Host xác nhận, nhưng CharacterController vẫn nhớ vị trí cũ → quãng đường bị
cộng dồn mỗi lần tua.
→ Cách chữa: implement `IBeforeAllTicks` và tắt/bật `CharacterController` trong đó.
→ **Bắt buộc:** trên prefab, `NetworkTransform` phải nằm **PHÍA TRÊN** `FPSMovement`, vì Fusion
gọi `BeforeAllTicks` theo thứ tự component từ trên xuống.

**3. `onBeforeSpawned` chỉ chạy trên Host**
Callback thứ 5 của `Runner.Spawn()` không chạy trên máy Client. Mọi giá trị cần Client biết
(ví dụ hướng nhìn ban đầu theo đội) đều **phải là `[Networked]`** mới truyền sang được.

**4. `FixedUpdateNetwork` ghi đè rotation mỗi tick**
Vì `transform.rotation` được đặt lại theo `input.Yaw` mỗi tick, nên xoay nhân vật lúc spawn
là vô nghĩa nếu không đặt luôn `NetworkRunnerHandler.SetLookAngles()` cho khớp.

**5. Chuột phải đọc ở `Update()`, không phải `FixedUpdateNetwork()`**
Tick mạng chạy chậm hơn tốc độ khung hình. Đọc chuột ở tick mạng sẽ mất bớt chuyển động,
gây giật. Chuột được tích luỹ ở `NetworkRunnerHandler.Update()` rồi gửi kèm input.

**6. Quên `NetworkTransform` → máy khác thấy object nằm ở (0,0,0)**
Dấu hiệu: máy **gọi lệnh spawn** thì thấy object đúng chỗ, máy kia không thấy đâu cả
(thực ra nó nằm ở gốc toạ độ). Đã gặp 2 lần: nhân vật, và tường EM Barrier.
`NetworkObject` chỉ nói "object này tồn tại trên mạng", **không** đồng bộ vị trí.

| Loại prefab | Component cần có |
|---|---|
| Đứng yên nhưng cần đúng vị trí *(tường EM Barrier)* | `NetworkObject` + `NetworkTransform` |
| Có Rigidbody *(vật thể từ tính)* | `NetworkObject` + `NetworkRigidbody3D` |
| Nhân vật dùng CharacterController | `NetworkObject` + `NetworkTransform` |
| Vô hình, chỉ chứa dữ liệu *(GameManager)* | Chỉ `NetworkObject` |

**8. Phình collider ngay trong người = vật bay lệch ngẫu nhiên** *(bẫy VẬT LÝ, không phải Fusion — sửa 09/08)*
Vật cầm trên tay bị thu nhỏ còn `heldObjectSize`. Nếu gọi `RestoreScale()` rồi bật va chạm và
`isKinematic = false` **ngay tại `holdPoint`**, collider phình to lồng xuyên qua người chơi.
PhysX phát hiện chồng lấn sâu → bắn ra **lực gỡ kẹt (depenetration)** để tách hai vật ra.
Lực đó lớn hơn lực tung nhiều lần và hướng phụ thuộc hình dạng chỗ chồng → vật bay mỗi lần một kiểu.
→ Cách chữa: hàm `PlayerMagnetController.PrepareForRelease()` — trả cỡ gốc, **dời vật ra khỏi
người theo phương ngang**, rồi mới bật va chạm. Mọi đường buông tay đều phải đi qua hàm này.
→ Kèm theo: tung vật dùng **đặt thẳng `linearVelocity`** thay cho `AddForce(Impulse)`, vì phép gán
ghi đè sạch mọi vận tốc rác, và vật nặng vật nhẹ tung lên cao như nhau.
→ Đo kích thước vật phải dùng `rend.localBounds`, **không dùng `rend.bounds`** — `bounds` là hộp bao
theo trục thế giới nên phình ra khi vật xoay nghiêng, cho ra cỡ khác nhau tuỳ hướng nhìn lúc nhặt.

**9. Ngưỡng "bắt vật vào tay" phải cộng bán kính vật** *(bẫy VẬT LÝ — sửa 09/08)*
Ngưỡng cũ là con số cứng `0.7m` đo từ **tâm (pivot)** vật tới `holdPoint`. Vật to (bàn rộng 3m)
có collider chạm người chơi khi tâm còn cách hơn 2m → **không bao giờ xuống dưới 0.7m** → cứ nghiến
vào người mà không lọt vào tay được.
→ Cách chữa: `grabThreshold = grabDistance + magObj.GetBoundingRadius()`.
→ `GetBoundingRadius()` dùng `localBounds × lossyScale` nên **không đổi khi vật xoay**.
Mọi phép đo kích thước vật thể trong project đều phải đi qua hàm này, đừng dùng `bounds` thẳng.

**10. TNT nổ KHÔNG despawn nữa** *(đổi 09/08 — đọc kỹ trước khi sửa `Explode()`)*
Trước đây `Explode()` gọi `Runner.Despawn(Object)`. Giờ chỉ đặt `IsDestroyed = true` để ẩn vật đi.
Lý do: despawn là xoá vĩnh viễn, nên sang round mới không dựng lại được đúng vật đó — mà spawn lại
từ prefab thì sai vì nhiều prefab khác nhau cùng một loại (xem ghi chú InventorySystem ở Mục 4).
→ Kéo theo: tiếng nổ đã chuyển từ `Despawned()` sang bộ đếm `ExplodeCount` + `OnChangedRender`,
đúng khuôn với `LaunchCount` / `DashCount`.
→ `ApplyStoredState()` giờ ẩn vật khi `IsStored` **hoặc** `IsDestroyed`.

**7. Giá trị Inspector luôn đè lên giá trị mặc định trong code**
Sửa `public float x = 5f;` trong code KHÔNG làm thay đổi component đã tồn tại trong scene/prefab.
Phải sửa trực tiếp ở Inspector. Chỉ field **mới hoàn toàn** mới lấy giá trị mặc định từ code.

### Lộ trình — TOÀN BỘ CODE ĐÃ XONG (05/08/2026)

Sáu bước dưới đây đều đã hoàn thành phần lập trình. Việc còn lại là **map** và một ít dựng UI.
→ **Xem [PROGRESS.md](PROGRESS.md) để biết chính xác còn thiếu gì.**

1. ✅ Gameplay chạy trên Fusion Host Mode
2. ✅ `GameManager` — vòng lặp round, Overtime, KillZone, quay về menu khi hết trận
3. ✅ Shop + kinh tế + 5 vật phẩm
4. ✅ HUD
5. ✅ Âm thanh (`AudioManager`) + Settings (`GameSettings` + `SettingsUI`)
6. ✅ Quan sát khi chết (`SpectatorController`)

---

## 6. Quy tắc làm việc với chủ project

Chủ project **lần đầu dùng Claude Code và lần đầu dùng AI để lập trình**. Hãy giải thích ở mức người mới bắt đầu: nói rõ *tại sao* chứ không chỉ *làm gì*, tránh viết tắt và thuật ngữ không giải thích.

### Ranh giới tuyệt đối — KHÔNG được vi phạm

- ❌ **Không đổi tên class.**
- ❌ **Không đổi tên file.**
- ❌ **Không di chuyển file hay tái cấu trúc thư mục.**
  > Lý do: Unity dùng file `.meta` chứa GUID để liên kết. Di chuyển hoặc đổi tên bên ngoài Unity Editor sẽ làm **vỡ toàn bộ reference** trong scene và prefab — component sẽ hiện "Missing Script" và mất hết dữ liệu đã kéo thả ở Inspector.
- ❌ Không sửa code trong `Assets/Photon/`, `Assets/ParrelSync/`, `Assets/Resources/`, `Assets/TutorialInfo/` (thư viện bên thứ ba).
- ❌ Không xoá `Assets/_Recovery/`.

### Cách sửa code

- ✅ **Được phép tự sửa code**, nhưng phải **hiểu kỹ ý đồ trước khi sửa**. Chủ project lo ngại việc AI hiểu sai yêu cầu rồi sửa lệch hướng.
- Với thay đổi lớn hoặc mơ hồ: **giải thích ý định trước, đợi xác nhận, rồi mới sửa**.
- Với thay đổi nhỏ và rõ ràng: sửa luôn, nhưng nói rõ đã sửa gì và tại sao.
- **Comment trong code viết bằng tiếng Việt** — bám theo phong cách hiện có của codebase.

### Giới hạn của Claude — luôn phải nhớ

**Claude không mở được Unity Editor.** Chỉ sửa được file `.cs`. Mọi thao tác sau đây **chủ project phải tự làm**:
- Bấm Play để test
- Kéo thả component, gán prefab và reference ở Inspector
- Tạo/sửa scene, tạo prefab
- Thêm scene vào Build Settings
- Tạo ScriptableObject asset (`ItemData`)

→ **Sau mỗi lần sửa code, luôn liệt kê rõ ràng các bước thủ công cần làm trong Unity** (dạng danh sách đánh số).

### Cách test

Chủ project dùng linh hoạt cả hai cách:
1. Mở Unity Editor bấm Play (test nhanh, 1 người).
2. Dùng **ParrelSync** tạo clone project để test 2 client cùng lúc (test multiplayer).

---

## 7. Vấn đề đã biết (chưa sửa)

### 🟡 Đang treo chờ quyết định thiết kế

- **`OnPlayerLeft` chưa despawn nhân vật.** Người thoát giữa trận để lại "xác" đứng im trên map.
  Cố ý chưa làm: chủ project muốn gộp chung với cơ chế đền bù kinh tế
  (1v2 thì người còn lại được bù tiền, 1v1 giữ nguyên) — chưa chốt con số.
- **Người vào phòng giữa trận** chưa được xử lý (`OnPlayerJoined` vẫn spawn `RoomPlayer` như thường).

### 🟢 Thiếu tính năng (theo lộ trình, chưa phải lỗi)

- `RadialMenuController.cs` — vẫn là MonoBehaviour thuần, đọc `Input.` trực tiếp (đã vá tạm bằng cách tắt component khi không phải nhân vật mình).
- Nhãn HUD trong TestScene vẫn ghi "MÁU" dù giờ hiển thị điện tích — cần sửa chữ trong Unity.

> Các mục cũ ở đây (thanh giáp, kinh tế, Shop, HUD, âm thanh, Settings UI, MatchEnd về menu)
> **đã làm xong hết** tính tới 05/08. Xem [PROGRESS.md](PROGRESS.md) để biết chính xác còn thiếu gì.

### ⚪ Cảnh báo vô hại

- Console báo `Not allowed to access vertices on mesh ... isReadable is false` từ `QuickOutline`.
  Nguyên nhân: `MagneticAura.Awake()` gọi `AddComponent<Outline>()`, mà QuickOutline cần đọc mesh
  để tính viền mượt. Không ảnh hưởng gameplay, chỉ khiến viền outline hơi xấu ở cạnh.
  → Cách sửa: chọn 5 file trong `Assets/Resources/Polytope Studio/Lowpoly_Environments/Sources/Meshes/Trees/`
  (`PT_Pine_Tree_03_logs`, `PT_Pine_Tree_03_stump`, `PT_Pine_Tree_03_green_cut`,
  `PT_Fruit_Tree_01_logs`, `PT_Fruit_Tree_01_stump`) → Inspector → tab Model → tích `Read/Write Enabled` → Apply.
  Đây là import settings, chỉ sửa được trong Unity Editor.

**11. Trên Host, transform của nhân vật CLIENT không đáng tin để đo tốc độ** *(bẫy Fusion — sửa 16/08)*
`FPSMovement.BeforeAllTicks()` tắt/bật `CharacterController` cho mọi nhân vật mà máy này có quyền
mô phỏng. Trên **Host, điều kiện đó đúng với TẤT CẢ nhân vật**, kể cả của client — nên transform
của họ **nhảy thô từng tick** thay vì được `NetworkTransform` nội suy mượt.
→ Triệu chứng đã gặp: `PlayerAnimatorDriver` tự đo quãng đường giữa hai khung hình để suy ra tốc độ,
kết quả là trên màn hình Host **mọi đối thủ đều hiện animation rơi tự do** dù đang đứng yên.
Máy Client không dính vì ở đó nhân vật Host là proxy, được nội suy đàng hoàng.
→ Cách chữa: **đừng suy từ transform.** Đồng bộ thẳng con số đã tính ở nơi có mô phỏng thật —
`FPSMovement.WalkSpeed01`, `IsGrounded`, `MovingBackward` (cộng lại chưa tới 6 byte/tick).
→ **Bài học chung:** nguyên tắc "suy ra tại chỗ thay vì đồng bộ" của project chỉ đúng khi thứ
dùng để suy ra là ĐÁNG TIN. Transform trên Host không đáng tin cho việc đo tốc độ.
