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

| Đã networked (Fusion) | Vẫn là singleplayer (MonoBehaviour thuần) |
|---|---|
| `Assets/NetworkRunnerHandler.cs` | `Assets/Scripts/Player/PlayerMagnetController.cs` |
| `Assets/RoomPlayer.cs` | `Assets/Scripts/Player/PlayerHealth.cs` |
| `Assets/Scripts/Player/FPSMovement.cs` | `Assets/Scripts/Player/InventorySystem.cs` |
| `Assets/Scripts/Player/NetworkInputData.cs` | `Assets/Scripts/Player/PlayerHotbarController.cs` |
| | `Assets/Scripts/Player/RadialMenuController.cs` |
| | `Assets/Scripts/Player/PlayerInteract.cs` |
| | `Assets/Scripts/Item/MagneticObject.cs` |
| | `Assets/Scripts/Item/MagneticAura.cs` |
| | `Assets/Scripts/Dummy/*.cs` |

**Đã chạy được multiplayer:** Lobby, di chuyển/xoay/dash của nhân vật, spawn theo đội.
**Chưa:** toàn bộ phần chiến đấu (hút/đẩy/bắn/cận chiến), máu, túi đồ, vật thể từ tính.

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

> ⚠️ **Trạng thái hiện tại (chốt 30/07):** đã **tạm bỏ hết luật riêng** của Normal / Heavy / Spike.
> Cả 3 loại giờ hút, đẩy, va chạm **y hệt nhau**, chỉ khác đúng con số sát thương.
> Chủ project sẽ thêm lại các luật này sau. TNT vẫn giữ nguyên cơ chế nổ.

| Loại | Sát thương | Đặc tính theo thiết kế gốc | Đã làm? |
|---|---|---|---|
| **Normal** | 10 HP | Hút/đẩy linh hoạt | ✅ |
| **Heavy** | 20 HP | Không hút được khi đang bay; giảm 50% lực nếu bị cản | ⬜ Đã bỏ, thêm lại sau |
| **Spike** | 25 HP | Cắm dính vào người trúng; nhận x2 (50 HP) nếu nạn nhân hút sai lầm khi nó đang bay tới | ⬜ Đã bỏ, thêm lại sau |
| **TNT** | 35 HP | Nổ bán kính 6m, sát thương giảm dần theo khoảng cách, hất văng diện rộng | ✅ |

### Vòng đấu & Điều kiện thắng (BẢN CHÍNH THỨC — theo GDD FR-7)

- Thắng 1 round = **hạ gục toàn bộ đội đối phương**.
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
| **Shield Armor** | $150 | Tự động cộng vào thanh giáp ngay khi mua. **Không nằm trong Radial Menu.** Reset mỗi khi hết round |
| **Energy Drink** | $100 | Giảm 15% cooldown Dash |
| **Bandage** | $100 | Hồi tối đa 20 HP, nhưng không quá 50% lượng máu đã mất |
| **Gasoline Canister** | $150 | Chuột trái vào 1 vật Normal trong tầm → biến nó thành thùng TNT |
| **EM Barrier Core** | $200 | Thả lõi tạo tường chắn trước mặt, tồn tại 10 giây |

Radial Menu **không làm chậm thời gian** (no slow-motion) ở cả offline lẫn online — đây là chủ ý thiết kế, đòi hỏi phản xạ người chơi.

### InventorySystem — thiết kế gộp

`InventorySystem` là **một hệ thống duy nhất chứa cả hai loại**:
1. **GameObject vật thể thật** nhặt từ map (bàn, ghế...) dùng làm đạn.
2. **Vật phẩm tiêu hao** mua từ Shop.

Cả hai đều có thể lấy ra từ **Hotbar (`Z`/`X`/`C`)** lẫn **Radial Menu (`Tab`)**.

Cơ chế hiện tại: khi nhặt, vật thể thật được `SetParent` vào Player, tắt vật lý và `SetActive(false)` để ẩn đi. Khi rút ra thì đảo ngược lại. Lấy theo thứ tự LIFO.

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

**6. Giá trị Inspector luôn đè lên giá trị mặc định trong code**
Sửa `public float x = 5f;` trong code KHÔNG làm thay đổi component đã tồn tại trong scene/prefab.
Phải sửa trực tiếp ở Inspector. Chỉ field **mới hoàn toàn** mới lấy giá trị mặc định từ code.

### Lộ trình đã thống nhất

1. **Chuyển gameplay sang Fusion Host Mode** ← đang ở đây
   - ✅ `FPSMovement` (di chuyển, xoay, dash) + `NetworkInputData` + spawn theo đội
   - ⬜ `PlayerMagnetController` (hút/đẩy/bắn/cận chiến) — phần khó nhất
   - ⬜ `PlayerHealth`, `InventorySystem`, `PlayerHotbarController`, `RadialMenuController`
   - ⬜ `MagneticObject` → `NetworkObject` + `NetworkRigidbody3D`
2. `GameManager` — vòng lặp round, đếm ngược, điều kiện thắng/Overtime, KillZone (rơi khỏi đảo)
3. Shop + hệ thống kinh tế
4. HUD / UI (máu, giáp, tiền, đồng hồ, tỉ số, tâm ngắm)
5. Âm thanh + Settings UI (volume, độ phân giải, fullscreen, độ nhạy chuột)
6. Polish

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

### 🔴 Ưu tiên cao — sẽ gây lỗi ngay khi test 2 người

- **Các script điều khiển chạy trên CẢ nhân vật của người khác.**
  `PlayerMagnetController`, `RadialMenuController`, `PlayerHotbarController`, `PlayerInteract`
  vẫn là `MonoBehaviour` và đọc `Input.` trong `Update()`. Khi có 4 người chơi, trên máy bạn sẽ
  có 4 bản cùng đọc chuột của bạn — bấm chuột trái là cả 4 nhân vật cùng hút đồ.
  → Vá tạm: tắt các component đó trong `FPSMovement.Spawned()` khi `HasInputAuthority == false`.
  Chưa làm, đang chờ chốt.

### 🟡 Đang treo chờ quyết định thiết kế

- **`OnPlayerLeft` chưa despawn nhân vật.** Người thoát giữa trận để lại "xác" đứng im trên map.
  Cố ý chưa làm: chủ project muốn gộp chung với cơ chế đền bù kinh tế
  (1v2 thì người còn lại được bù tiền, 1v1 giữ nguyên) — chưa chốt con số.
- **Người vào phòng giữa trận** chưa được xử lý (`OnPlayerJoined` vẫn spawn `RoomPlayer` như thường).

### 🟢 Thiếu tính năng (theo lộ trình, chưa phải lỗi)

- `PlayerHealth.cs` — chết chỉ `Debug.Log` rồi hồi đầy máu. Chưa có loại khỏi round, chưa respawn, chưa có thanh giáp.
- `RadialMenuController.cs` — `itemQuantity` là số nhập tay ở Inspector, **chưa nối với `InventorySystem`**. `UseItemFromRadialMenu()` mới chỉ `Debug.Log`.
- Chưa có: `GameManager`, hệ thống round, kinh tế, Shop, HUD, âm thanh, Settings UI, KillZone.

### ⚪ Cảnh báo vô hại

- Console báo `Not allowed to access vertices on mesh ... isReadable is false` từ `QuickOutline`.
  Nguyên nhân: `MagneticAura.Awake()` gọi `AddComponent<Outline>()`, mà QuickOutline cần đọc mesh
  để tính viền mượt. Không ảnh hưởng gameplay, chỉ khiến viền outline hơi xấu ở cạnh.
  → Cách sửa: chọn 5 file trong `Assets/Resources/Polytope Studio/Lowpoly_Environments/Sources/Meshes/Trees/`
  (`PT_Pine_Tree_03_logs`, `PT_Pine_Tree_03_stump`, `PT_Pine_Tree_03_green_cut`,
  `PT_Fruit_Tree_01_logs`, `PT_Fruit_Tree_01_stump`) → Inspector → tab Model → tích `Read/Write Enabled` → Apply.
  Đây là import settings, chỉ sửa được trong Unity Editor.
