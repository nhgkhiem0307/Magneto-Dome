# TIẾN ĐỘ — Magneto-Dome

> Nhật ký tiến độ. Cập nhật sau mỗi buổi làm việc.
> Đọc kèm [CLAUDE.md](CLAUDE.md) — file đó chứa đặc tả và quy tắc, file này chỉ ghi *đang làm tới đâu*.

**Cập nhật lần cuối:** 30/07/2026
**Deadline:** khoảng 16/09/2026 (còn ~7 tuần)

---

## Tổng quan lộ trình

| # | Hạng mục | Tiến độ |
|---|---|---|
| 1 | Chuyển gameplay sang Fusion Host Mode | 🟡 **~45%** — đang làm |
| 2 | `GameManager` — vòng đấu, thắng/thua, KillZone | ⬜ Chưa bắt đầu |
| 3 | Shop + hệ thống kinh tế | ⬜ Chưa bắt đầu |
| 4 | HUD / UI trong trận | ⬜ Chưa bắt đầu |
| 5 | Âm thanh + Settings UI | ⬜ Chưa bắt đầu |
| 6 | Polish | ⬜ Chưa bắt đầu |

---

## Bước 1 — Chuyển sang Fusion Host Mode (chi tiết)

| Script | Trạng thái | Ghi chú |
|---|---|---|
| `NetworkRunnerHandler.cs` | ✅ Xong | Lobby, gửi input, spawn theo đội |
| `RoomPlayer.cs` | ✅ Xong | Đồng bộ tên + đội trong phòng chờ |
| `NetworkInputData.cs` | ✅ Xong | 6 nút: Dash, 2 phím điện tích, Fire, Melee, Toss |
| `FPSMovement.cs` | ✅ Xong | Đã test 2 máy: di chuyển, xoay, dash đều đúng |
| `PlayerHealth.cs` | 🟡 Code xong, **chưa test được** | Máu đã `[Networked]` + có log `OnChangedRender`. Chưa test vì cần bắn vật thể mới mất máu |
| `PlayerMagnetController.cs` | 🟡 Điện tích + cận chiến ✅ **đã test 2 máy**. Phần vật thể chưa | |
| `MagneticObject.cs` | ✅ **Đã test 2 máy** — cả hai đều thấy vật bay mượt | `NetworkBehaviour` + Fusion Physics Addon (Cách A) |
| `InventorySystem.cs` | ⬜ Chưa | |
| `PlayerHotbarController.cs` | ⬜ Chưa | Vẫn đọc `Input.` trên mọi nhân vật |
| `RadialMenuController.cs` | ⬜ Chưa | Vẫn đọc `Input.` trên mọi nhân vật |
| `PlayerInteract.cs` | ⬜ Chưa | Vẫn đọc `Input.` trên mọi nhân vật |

---

## Đã làm xong & đã kiểm chứng trên 2 máy

- Tạo phòng bằng mã PIN 5 số, vào phòng, đổi đội, hiển thị danh sách 2 đội.
- Host bấm Bắt Đầu Trận → load `TestScene` → spawn mỗi người 1 nhân vật đúng vị trí đội mình.
- Nhân vật quay mặt vào giữa map lúc spawn (chỉnh bằng `Red/Blue Team Spawn Yaw` ở Inspector).
- Di chuyển WASD, xoay chuột, Dash `Q` — mượt và đúng trên cả Host lẫn Client.

### Ba lỗi đã sửa dứt điểm trong buổi này

1. **Client spawn ở `(0,0,0)`** — `CharacterController` nhớ toạ độ cũ. Sửa bằng tắt/bật CC trong `Spawned()`.
2. **Client chạy nhanh gấp nhiều lần** — Fusion tua lại nhiều tick, CC cộng dồn quãng đường. Sửa bằng `IBeforeAllTicks`.
3. **Host không thấy Client di chuyển** — là hệ quả của 2 lỗi trên, tự hết.

> Chi tiết nguyên nhân và cách chữa đã ghi ở **CLAUDE.md mục 5** ("Bẫy đã gặp khi chuyển sang Fusion"). Đọc lại mục đó trước khi chuyển script mới.

---

## ✅ Đã kiểm chứng thêm (buổi 30/07)

- Đổi điện tích `1` / `2` — đồng bộ đúng, đối thủ thấy được cực găng.
- Cận chiến đầy đủ 3 trường hợp: đấm văng, nảy bật, kéo áp sát.
- Giá trị đã chỉnh tay ở Inspector: `meleeRange = 4`, `dashLockRange = 20`,
  `meleePushForce = 100`, `grapplePullForce = 100`.

## ✅ Cách A đã qua kiểm chứng (30/07) — KHÔNG cần lùi về Cách B

Hạn 1 ngày đã qua an toàn. Kết quả test 2 máy bằng ParrelSync:

- **Di chuyển WASD + Dash Q vẫn ổn nguyên** sau khi cài `RunnerSimulatePhysics3D`.
  Đây là rủi ro lớn nhất của Cách A, và nó đã không xảy ra.
- Nạp điện, hút, cầm, ném vật thể — **cả hai máy đều thấy vật bay mượt**.
- Còn tồn: **client có chút độ trễ** khi thao tác với vật thể.

### Nguyên nhân của độ trễ còn lại

Trong `PlayerMagnetController.FixedUpdateNetwork()` vẫn còn dòng `if (!HasStateAuthority) return;`.
Nó chặn Client tự đoán trước, nên Client phải chờ Host xử lý xong rồi mới thấy kết quả.

### ❌ ĐÃ THỬ VÀ BỎ: cho Client dự đoán trước thao tác vật thể

Đã gỡ rào `if (!HasStateAuthority) return;` cho Client tự đoán trước. **Kết quả tệ hơn hẳn:**

- Vật cầm trên tay trễ **nặng hơn** trước, nhất là khi vừa cầm vừa di chuyển
- Vật bị giật khi di chuyển theo tay

→ **Đã lùi lại.** Kết luận: độ trễ đều đặn dễ quen tay hơn giật ngẫu nhiên.
Đừng thử lại nếu chưa có cách xử lý sai lệch dự đoán tử tế hơn.

**Giữ lại 2 thứ dù đã lùi** (vì chúng vốn đúng hơn cách cũ, không liên quan tới dự đoán):
- `grabbedObject` → `[Networked] GrabbedObjectId` — biến thường không được Fusion tua lại
- `grabbedRb` → property tính tươi mỗi lần dùng

### 🐛 Lỗi phát hiện nhờ phép thử này: nhân vật đối phương bị giật

Triệu chứng: nhìn nhân vật người khác thấy **giật liên tục nhưng vị trí vẫn đúng**,
còn nhìn chính mình thì mượt. Xảy ra ở cả Host lẫn Client.

**Nguyên nhân:** `IBeforeAllTicks` trong `FPSMovement` tắt/bật `CharacterController`
trên **mọi** nhân vật, kể cả nhân vật người khác. Nhưng nhân vật người khác đang được
`NetworkTransform` nội suy cho mượt — tắt/bật CC ép transform nhảy về vị trí thô từng tick,
phá nát nội suy.

**Đã sửa:** thêm `if (!HasStateAuthority && !HasInputAuthority) return;` vào đầu `BeforeAllTicks`.
Lỗi này có từ lúc sửa bug "client chạy nhanh gấp bội", chỉ là tới giờ mới lộ. **Cần test lại.**

## ⏸ Chưa test được: `PlayerHealth`

**Lý do:** cách duy nhất mất máu hiện tại là **trúng vật thể do địch bắn**, mà `MagneticObject`
chưa lên mạng. Trên máy Host thì bắn được, nhưng Client không nhìn thấy vật bay.

Đã thêm log để khi nào test được là đối chiếu ngay:

| Log | In ở đâu | Nội dung |
|---|---|---|
| `[SÁT THƯƠNG]` | Chỉ máy Host | Ai trúng đòn, mất bao nhiêu máu |
| `[MÁU]` | **Mọi máy** | Máu còn lại. Ghi rõ "MÁU CỦA BẠN" hay "MÁU ĐỐI PHƯƠNG" |

→ Mở Console ở **cả 2 cửa sổ** ParrelSync. Nếu con số `[MÁU]` ở hai bên **khớp nhau** thì máu đã
đồng bộ đúng. Nếu lệch, hoặc chỉ một bên hiện log, là còn lỗi.

> **Lưu ý:** hút / đẩy / cầm / bắn vật thể hiện **chỉ đúng trên máy Host**. Client bấm thì Host vẫn xử lý,
> nhưng Client sẽ **không nhìn thấy vật bay**. Đây là dự kiến, không phải lỗi mới.

---

## 🔴 Quyết định đang chờ

### ~~1. Physics cho `MagneticObject`~~ → ✅ ĐÃ CHỐT: **Cách A (Fusion Physics Addon)**

Chốt ngày 30/07. Đặt hạn **1 ngày** để thử; nếu di chuyển vỡ hoặc vật thể rung giật không chữa được
thì lùi về Cách B (chỉ dùng `NetworkTransform`). Code `MagneticObject` đã viết theo hướng
**dùng chung được cho cả hai cách** — khác nhau chỉ ở component gắn trên prefab, không phải sửa code.

<details>
<summary>Bảng so sánh gốc (giữ lại để tham khảo nếu phải lùi)</summary>


**Vấn đề:** `NetworkRigidbody3D` **không có** trong project. Bản Fusion đã cài chỉ có
`NetworkTransform`, `NetworkTRSP`, `NetworkCharacterController`, `NetworkMecanimAnimator`.
`NetworkRigidbody3D` nằm trong **Fusion Physics Addon**, phải tải riêng từ trang Photon.

Hai hướng đi, chưa chọn:

| | Cách A — Cài Physics Addon | Cách B — Host tự tính, `NetworkTransform` đồng bộ |
|---|---|---|
| Việc phải làm | Tải + cài addon từ Photon | Không cài gì thêm |
| Độ trễ khi Client bắn | Thấp (có dự đoán trước) | Cao (~100–200ms mới thấy vật bay) |
| Độ khó | Trung bình | Thấp |
| Rủi ro | Addon có thể phải chỉnh cấu hình | Cảm giác bắn hơi "nặng tay" ở máy Client |

Vì vật thể **chính là đạn** của game, độ trễ ảnh hưởng trực tiếp tới cảm giác chơi.

</details>

### 2. Đền bù kinh tế khi đồng đội thoát giữa trận

`OnPlayerLeft` hiện **chưa despawn** nhân vật — người thoát để lại "xác" đứng im trên map.
Cố ý chưa làm, vì muốn gộp chung với cơ chế: 1v2 thì người còn lại được bù tiền, 1v1 giữ nguyên.
**Chưa chốt con số.**

---

## Việc thủ công trong Unity còn tồn

- [ ] Bật `Read/Write Enabled` cho 5 file `.fbx` trong
      `Assets/Resources/Polytope Studio/Lowpoly_Environments/Sources/Meshes/Trees/`
      (`PT_Pine_Tree_03_logs`, `PT_Pine_Tree_03_stump`, `PT_Pine_Tree_03_green_cut`,
      `PT_Fruit_Tree_01_logs`, `PT_Fruit_Tree_01_stump`)
      → dẹp đống cảnh báo QuickOutline trong Console. Không gấp, chỉ là cảnh báo vô hại.

---

## Việc tiếp theo (theo thứ tự)

1. Test cận chiến + điện tích bằng ParrelSync *(xem mục "Cần test ngay")*
2. Chốt hướng physics → chuyển `MagneticObject` sang mạng
3. Hoàn tất phần vật thể của `PlayerMagnetController` (hút / đẩy / cầm / bắn)
4. `InventorySystem` + `PlayerHotbarController` + `RadialMenuController`
5. Hết bước 1 → bắt đầu `GameManager`
