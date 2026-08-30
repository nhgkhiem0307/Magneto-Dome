# GAME DESIGN DOCUMENT — MAGNETO-DOME

> **Bản cập nhật:** 24/08/2026 · Viết lại theo đúng game đã dựng.
> Bản gốc `GDD.txt` (viết trước khi lập trình) giữ nguyên để đối chiếu.
> **Chương 8** liệt kê mọi thay đổi so với bản gốc kèm lý do.

---

## A. THÔNG TIN CHUNG

| Mục | Nội dung |
|---|---|
| Học viên | Nguyễn Hoàng Gia Khiêm |
| MSHV | 144010124020 · Lớp K24M-GAME1 |
| Ngành | Công nghệ thông tin — Lập trình Game |
| Tên project | **Magneto-Dome** |
| Thể loại | FPS đối kháng 2v2, Round-based Tactical Arena PvP |
| Nền tảng | PC (Windows), Unity 6000.3.17f1 + URP 17.3.0 |
| Mạng | Photon Fusion 2, Host Mode, Region `asia` |

---

## B. MỤC ĐÍCH & YÊU CẦU

**Mục đích:** xây dựng một trò chơi FPS đối kháng nhiều người chơi hoàn chỉnh, có đủ vòng lặp từ vào phòng tới khi có đội vô địch, đáp ứng bốn tiêu chuẩn: tính năng, trải nghiệm, kỹ thuật và trực quan.

**Yêu cầu cốt lõi:** thay thế hoàn toàn súng đạn bằng **găng tay từ tính phân cực**. Người chơi nạp điện cho vật thể trong màn chơi rồi hút/đẩy chúng làm vũ khí. Kết quả mỗi pha giao tranh do hệ thống vật lý thời gian thực quyết định, không phải do chỉ số sát thương định sẵn.

---

# CHƯƠNG 1 — TỔNG QUAN

## 1.1. Lý do chọn đề tài

Thể loại FPS truyền thống đã bão hòa ở phần *cách bắn*. Đề tài này thay lớp đó bằng **tương tác vật lý từ trường**: người chơi không có vũ khí, mà biến môi trường thành vũ khí.

## 1.2. Bối cảnh

**"Sky Arena"** — một hòn đảo lơ lửng giữa không trung. Bối cảnh này không chỉ để đẹp: nó là **nền tảng của luật thắng thua**, vì cái chết duy nhất trong game là rơi khỏi đảo.

## 1.3. Điểm mới — ba tầng

**Tầng 1 — Đạn là môi trường.** Không có súng. Mọi vật thể trên map (cây, đá) đều nạp điện được và trở thành đạn. Đạn là tài nguyên hữu hạn, dùng hết phải đi nhặt.

**Tầng 2 — Luật hút/đẩy hai chiều.** Cùng dấu thì đẩy, trái dấu thì hút. Vì đối thủ cũng có găng tay, mọi vật đang bay đều có thể bị bên kia can thiệp. Mỗi cú bắn là một pha đọc tình huống, không phải một phép tính sát thương.

**Tầng 3 — Chế độ Quá Tải.** Không có thanh máu. Trúng đòn làm bạn **nhiễm điện**; càng nhiễm thì từ trường tác động lên bạn càng mạnh, và cùng một cú đấm sẽ hất bạn đi càng xa. Cái chết đến từ **vị trí**, không từ con số.

---

# CHƯƠNG 2 — CƠ CHẾ CỐT LÕI

## 2.1. Di chuyển

| Phím | Chức năng |
|---|---|
| `WASD` + chuột | Di chuyển, xoay góc nhìn (`CharacterController`) |
| `Q` | Lướt (Dash), có hồi chiêu |

**Giá trị:** `moveSpeed 13` · `dashForce 100` · `dashCooldown 1s` · `gravity -9.81`

> Hai kỹ năng `E` (Blink) và `R` (Phòng thủ) trong bản GDD gốc đã **bỏ**. Xem Chương 8.

## 2.2. Găng tay từ tính

| Phím | Chức năng |
|---|---|
| `1` / `2` | Đổi cực găng: **Dương (Đỏ)** / **Âm (Xanh)** |
| Chuột trái | Vật trung tính → **nạp điện**. Vật đã có điện → cùng dấu **ĐẨY**, trái dấu **HÚT về tay** |
| Chuột phải | Tay trống → **cận chiến**. Đang cầm vật → **bắn đi** |
| `V` | Tung vật đang cầm lên không |
| `Z` / `X` / `C` | Rút đạn Normal / Heavy / Spike từ túi |
| `Tab` (giữ) | Radial Menu — rê chuột chọn, thả phím để dùng |
| `B` | Mở Shop (chỉ trong Buy Phase) |

## 2.3. Cận chiến (chuột phải, tay trống)

| Điều kiện | Kết quả |
|---|---|
| < 4m, **cùng dấu** | Đấm đẩy văng đối thủ (`meleePushForce 200`) |
| < 4m, **trái dấu** | Nảy bật nhẹ cả hai bên |
| 4–20m, **trái dấu** | Kéo áp sát — lướt nhanh về phía đối thủ (`grapplePullForce 100`) |

## 2.4. Hệ sinh thái đạn

Bốn loại vật thể dùng chung một lớp `MagneticObject` với `enum ObjectType`.

| Loại | Nạp điện | Luật riêng |
|---|---|---|
| **Normal** | 10 | Hút/đẩy linh hoạt |
| **Heavy** | 20 | Đang bay thì **KHÔNG hút về tay được**. Bù lại **cản được**: tốc ×0.5, sát thương ×0.5 |
| **Spike** | 25 | Hút nhầm khi nó đang bay tới thì **ăn gấp đôi (50)** |
| **TNT** | 35 | Nổ bán kính 6m, giảm dần theo khoảng cách, hất văng diện rộng |

**Heavy và Spike là cặp đối xứng.** Cùng dùng một cơ chế can thiệp (`TryCounterInFlight`, chỉ cho can thiệp **một lần mỗi cú bay** để chặn spam giữ chuột), nhưng ý nghĩa ngược nhau: Heavy **thưởng** cho phản xạ hút, Spike **phạt** phản xạ hút. Người chơi buộc phải nhìn màu vật trước khi bấm.

## 2.5. ⚡ CHẾ ĐỘ QUÁ TẢI — thay cho thanh máu

Đây là thay đổi thiết kế lớn nhất so với bản GDD gốc.

| Luật | Chi tiết |
|---|---|
| Thanh đo | Điện tích `0 → 100`. Đầy 100% **KHÔNG chết** |
| Lực văng | Nhân tuyến tính `×1.0` (sạch điện) → `×5.0` (đầy điện) |
| Cái chết **duy nhất** | Rơi khỏi đảo (`killZoneY = -20`) |
| Sang round mới | Xả sạch điện về 0 |

**Vì sao đầy điện không chết:** nếu đầy là chết thì đây chỉ là thanh máu chạy ngược, không có gì mới. Để cái chết đến từ **VỊ TRÍ** mới tạo được sự căng thẳng thật — đứng giữa sân với 90% điện vẫn an toàn, đứng sát rìa với 30% đã là mạo hiểm.

**Ba nguồn lực văng:**

| Nguồn | Lực |
|---|---|
| Đấm cận chiến | 200 |
| Trúng đạn | 120 × (sát thương ÷ 10) |
| Nổ TNT | `explosionForce × 2` |

**Nút chỉnh sát thương toàn cục:** `chargeGainMultiplier = 1.5` nhân mọi lượng điện nhận vào, tại một chỗ duy nhất.

---

# CHƯƠNG 3 — VÒNG ĐẤU, KHU CHIẾM ĐÓNG & KINH TẾ

## 3.1. Cấu trúc một round

```
WaitingToStart (2s) → BuyPhase (15s) → Combat (90s) → RoundEnd (5s) → round kế
                       rào chắn hạ ↑                    tính điểm ↑
```

## 3.2. Điều kiện thắng round — Khu Chiếm Đóng

Giữa bản đồ có một **khu chiếm đóng** hình trụ (`radius 13`, `halfHeight 3`).

| Luật | Chi tiết |
|---|---|
| Chiếm | Đứng trong khu → tiến độ đội tăng `10`/giây |
| Hai người cùng đội | Nhân tốc độ `×1.5` — thưởng phối hợp, không biến thành cuộc đua dồn người |
| **Tranh chấp** | Hai đội cùng có mặt → **đóng băng**, không ai tiến |
| Thắng round | Đạt `100` tiến độ |
| Hết giờ 90s | Đội có tiến độ **cao hơn** thắng |
| Địch chết | Đội mình **+8** tiến độ |

**Vì sao chết cho địch điểm:** hồi sinh xả sạch điện tích. Nếu chết mà không mất gì thì tự nhảy xuống vực lúc nhiễm nặng sẽ thành nước đi tối ưu. Cộng tiến độ cho địch chặn đứng lối chơi đó — **và khiến việc hất địch khỏi đảo trực tiếp đẩy mình tới chiến thắng**, nên lực văng và mục tiêu round trở thành cùng một thứ.

## 3.3. Hồi sinh

Chết **không** còn là bị loại hết round. Sau **5 giây** người chơi sống lại tại điểm xuất phát, sạch điện. Cái chết lấy đi **thời gian** và **vị trí**, không lấy đi trận đấu.

Nhờ vậy lực văng có thể mạnh tay mà không gây ức chế.

## 3.4. Điều kiện thắng chung cuộc

- Đạt **5 điểm** trước **VÀ** cách biệt tối thiểu **2 round**
- Tỉ số 5-4 → **Overtime**, đấu tiếp tới khi một đội hơn 2 round (6-4, 7-5…)

## 3.5. Kinh tế

| Mục | Giá trị |
|---|---|
| Tiền khởi đầu | $500 |
| Thắng round | +$150 |
| Thua round | +$100 |
| Trần ví | $750 |

## 3.6. Cửa hàng — 5 vật phẩm

| Vật phẩm | Giá | Hiệu ứng |
|---|---|---|
| **Giáp Cách Điện** | $150 | Hấp thụ điện thay cơ thể (`maxArmor 30`, `+10`/lần mua). Không nằm trong Radial Menu. Reset mỗi round |
| **Nước Tăng Lực** | $100 | Giảm 15% hồi chiêu Dash trong 15 giây |
| **Bộ Xả Điện** | $100 | Xả tối đa 20 điểm điện, không quá 50% lượng đang mang |
| **Chai Xăng Tẩy Chế** | $150 | Chuột trái vào một vật **Normal** → biến thành thùng TNT |
| **Lõi Tường Điện Từ** | $200 | Thả lõi tạo tường chắn trước mặt, tồn tại 10 giây |

## 3.7. Túi đồ & Radial Menu

`InventorySystem` là **một hệ thống duy nhất** chứa cả hai loại: vật thể thật nhặt từ map, và vật phẩm tiêu hao mua từ Shop. Cả hai lấy ra được từ Hotbar (`Z`/`X`/`C`) lẫn Radial Menu.

Radial Menu **không làm chậm thời gian** ở cả offline lẫn online — chủ ý thiết kế, đòi hỏi phản xạ thật.

---

# CHƯƠNG 4 — MẠNG & ĐỒNG BỘ

## 4.1. Sảnh chờ

- Host tạo phòng → sinh **mã PIN** ngẫu nhiên (Session Name)
- Client vào phòng bằng mã PIN
- Đồng bộ tên người chơi và đội (Đỏ/Xanh) qua `[Rpc]`, tối đa 2 người/đội
- Region cố định **`asia`**, cấu hình ở `PhotonAppSettings.asset`

## 4.2. Kiến trúc

**Host Mode (server-authoritative).** Máy Host giữ `StateAuthority` và tính toàn bộ vật lý, chống gian lận.

## 4.3. Nguyên tắc đồng bộ — *chỉ đồng bộ những gì BẮT BUỘC*

Đây là nguyên tắc xuyên suốt project:

| Thứ | Cách làm |
|---|---|
| Vật cầm trên tay | Chỉ truyền `GrabbedObjectId`, mỗi máy tự đặt vị trí trong `LateUpdate()` |
| Rào chắn Buy Phase | Suy từ `GameManager.Phase`, là MonoBehaviour thường |
| Âm thanh | Móc vào `OnChangedRender` sẵn có, **0 byte** thêm |
| Rung camera | Thuần cục bộ, người khác không nhìn thấy |
| Trạng thái khu chiếm đóng | Mỗi máy tự đếm người trong bán kính từ vị trí đã đồng bộ |

⚠️ **Giới hạn của nguyên tắc này** — bài học đắt nhất của project: nó **chỉ đúng khi thứ dùng để suy ra là ĐÁNG TIN**. Đã có lần suy tốc độ nhân vật từ transform, nhưng trên máy Host thì transform của client bị nhảy thô từng tick (do `BeforeAllTicks` tắt/bật `CharacterController`), khiến mọi đối thủ hiện animation rơi tự do dù đang đứng yên. Phải chuyển sang đồng bộ thẳng ba giá trị đã tính đúng ở nơi có mô phỏng thật.

## 4.4. Âm thanh & hiệu ứng — không gọi trong `FixedUpdateNetwork()`

Fusion tua lại (resimulation) nhiều tick mỗi khung hình → một cú dash sẽ kêu 5–6 lần. Giải pháp: biến đếm `[Networked]` + `OnChangedRender` (`DashCount`, `MeleeCount`, `LaunchCount`, `FireCount`, `ExplodeCount`).

---

# CHƯƠNG 5 — THIẾT KẾ HƯỚNG ĐỐI TƯỢNG

## 5.1. Quyết định kiến trúc: giữ cấu trúc gộp

Bản GDD gốc đề xuất tách nhiều file (`PlayerCameraController`, `HeavyObject`, `SpikeObject`, `ExplosiveObject`, `NetworkLobbyManager`). **Không làm theo.**

Lý do: bốn loại vật thể chia sẻ khoảng 90% logic (điện tích, va chạm, cất túi, reset round). Tách thành lớp kế thừa sẽ nhân bốn lần số chỗ phải sửa mỗi khi đổi một luật chung, đổi lại chỉ để tách vài chục dòng khác biệt. Với một người làm thì cấu trúc gộp ít lỗi hơn hẳn.

## 5.2. Danh sách lớp thực tế

**Nhân vật** — `Assets/Scripts/Player/`

| Lớp | Vai trò |
|---|---|
| `FPSMovement` | Di chuyển, xoay, Dash, trọng lực, `AddImpact()` |
| `PlayerMagnetController` | Nạp điện, hút/đẩy, cận chiến, cầm/bắn/tung vật |
| `PlayerHealth` | Điện tích, giáp, hệ số lực văng, hồi sinh |
| `InventorySystem` | Túi đồ gộp: vật thể map + vật phẩm tiêu hao |
| `PlayerHotbarController` | `Z`/`X`/`C` rút đạn |
| `RadialMenuController` | UI vòng tròn chọn nhanh |
| `PlayerVisuals` | Nhãn quan: viền địch/bạn, quả cầu tay, aura sét |
| `PlayerAnimatorDriver` | Nối chuyển động vào Animator, giấu thân mình ở góc nhìn thứ nhất |
| `CameraShake` | Rung camera: đi bộ, Dash, bắn vật |
| `PlayerEconomy` | Tiền, thưởng cuối round |
| `NetworkInputData` | Struct gói input gửi qua mạng |

**Vật thể** — `Assets/Scripts/Item/`

| Lớp | Vai trò |
|---|---|
| `MagneticObject` | Điện tích, 4 loại, va chạm, nổ, ngủ đông, reset round |
| `MagneticAura` | Hào quang theo điện tích |
| `EMBarrier` | Tường điện từ |
| `ItemData` | ScriptableObject định nghĩa vật phẩm |

**Hệ thống**

| Lớp | Vai trò |
|---|---|
| `GameManager` | Vòng đấu, khu chiếm đóng, hồi sinh, KillZone, Overtime |
| `ControlZone` | Đánh dấu vùng chiếm đóng, đếm người bên trong |
| `ShopManager` | Giao dịch qua RPC, Host duyệt |
| `NetworkRunnerHandler` | Khởi tạo Runner, Host/Client, lobby, gửi input |
| `RoomPlayer` | Đồng bộ tên + đội trong phòng chờ |
| `AudioManager` | Âm thanh, tự đổi nhạc theo scene |
| `GameSettings` | Class tĩnh: độ nhạy chuột, fullscreen |
| `CursorLock` | **Trọng tài duy nhất** quyết định khoá/thả con trỏ |
| `RoundBarrier` | Rào spawn, suy từ `GameManager.Phase` |

**Giao diện** — `Assets/Scripts/MenuUI/`

`HUDController` · `ShopUI` · `ShopItemButton` · `SettingsUI` · `SpectatorController` · `RoomItemUI` · `ButtonClickSound` · `Billboard`

**Công cụ Editor**

`EnvironmentScatter` — rải trang trí hàng loạt bằng raycast, có vùng cấm mọc.

---

# CHƯƠNG 6 — TRỰC QUAN, CÀI ĐẶT & YÊU CẦU PHI CHỨC NĂNG

## 6.1. Nhãn quan — ba kênh, mỗi kênh một nghĩa

| Thông tin | Kênh | Màu |
|---|---|---|
| Địch hay bạn | Viền quanh người + marker trên đầu | 🟢 Xanh lá = đồng đội. **Địch không có màu riêng** |
| Cực găng của địch | Viền + 2 quả cầu ở tay | 🔴 Đỏ = Dương · 🔵 Xanh = Âm |
| Mức nhiễm điện | Aura sét quanh người | 🟡 Vàng, dày dần theo mức |

**Vì sao địch cố ý không có màu riêng:** nếu địch có viền cam hay đỏ, nó cạnh tranh thị giác với chính **màu cực găng** — thứ quan trọng nhất cần đọc để quyết định hút hay đẩy. Để địch "sạch màu" thì thứ duy nhất rực rỡ trên người họ là cực găng. Địch được nhận ra bằng **loại trừ**.

**Khác biệt về che khuất:** viền đồng đội **xuyên tường** (vị trí đồng đội là thông tin cho không); viền địch **có che khuất** (cho xuyên tường thì thành gian lận nhìn xuyên vách).

## 6.2. Đồ hoạ

- **URP 17.3.0**, Bloom + Tonemapping ACES
- Shader tự viết: **Gradient Sky** (bầu trời chuyển màu, có màu riêng cho vực bên dưới) và **Force Field** (rào chắn, tường điện từ)
- Sương mù Exponential Squared, màu khớp chân trời

## 6.3. Cài đặt

Slider Master / BGM / SFX · Slider độ nhạy chuột · Dropdown độ phân giải · Toggle Fullscreen. Lưu vào `PlayerPrefs`, nhớ giữa các lần chơi và tự theo sang scene trận.

## 6.4. Yêu cầu phi chức năng

| Mã | Yêu cầu |
|---|---|
| NFR-1 | ≥ 60 FPS trên PC mục tiêu trong pha va chạm vật lý phức tạp |
| NFR-2 | Ping < 100ms tại region `asia`, nội suy mượt qua Fusion 2 |
| NFR-3 | Phản hồi thị giác tức thì khi đổi cực găng; Radial Menu không trễ khung hình |

---

# CHƯƠNG 7 — KẾT LUẬN & HƯỚNG PHÁT TRIỂN

**Ưu điểm:** lối chơi từ tính độc đáo · Chế độ Quá Tải khiến cái chết đến từ vị trí thay vì từ con số · Khu chiếm đóng ép giao tranh vào một chỗ do người thiết kế chọn · Kiến trúc mạng Host Mode ổn định.

**Hạn chế:** giới hạn 1 bản đồ · Yêu cầu phần cứng xử lý vật lý đa đối tượng · Chưa có danh sách phòng (vào phòng bằng mã PIN).

**Hướng phát triển:** thanh điện tích nổi trên đầu mọi người · hitstop khi trúng đòn mạnh · vệt sáng theo đường đạn · thêm bản đồ · thêm vật phẩm Shop.

---

# CHƯƠNG 8 — ĐỐI CHIẾU VỚI GDD GỐC

## 8.1. Giữ nguyên — phần cốt lõi không đổi

Găng tay từ tính hai cực · không súng đạn · hút/đẩy theo dấu điện tích · cận chiến theo luật khắc chế màu găng · 4 loại vật thể · FPS 2v2 · Sky Arena đảo bay · vòng đấu + Buy Phase 15s + rào chắn · kinh tế $150/$100, trần $750 · 5 vật phẩm Shop · Radial Menu không slow-motion · thắng chung cuộc 5 round cách biệt 2 · Photon Fusion 2 Host Mode region asia · Settings UI · các NFR.

**→ Bản sắc của game không đổi.**

## 8.2. Thay đổi lớn

| # | GDD gốc | Thực tế | Lý do |
|---|---|---|---|
| 1 | **Thanh máu HP.** Đạn gây 10/20/25/35 HP | **Chế độ Quá Tải.** Trúng đòn nạp điện, lực văng ×1→×5, chỉ chết khi rơi khỏi đảo | Thanh máu khiến bối cảnh "đảo bay" thành trang trí. Quá Tải biến vực thành cơ chế thật, và tạo được sự leo thang tự nhiên trong round |
| 2 | **Thắng round = hạ gục toàn đội địch** | **Chiếm khu ở giữa** | Không còn ai chết vì hết máu, nên cần mục tiêu khác. Khu chiếm đóng còn ép giao tranh vào một chỗ cố định thay vì để hai bên né rìa vực |
| 3 | **Chết = bị loại hết round** | **Hồi sinh sau 5 giây** | Bị hất khỏi map mà mất cả round thì rất ức chế (ngồi nhìn 90 giây). Có hồi sinh thì lực văng được phép mạnh tay |
| 4 | Pha Combat không giới hạn giờ | **90 giây** | Hệ quả bắt buộc của #1: không ai chết vì hết máu thì round có thể kéo dài vô tận |

## 8.3. Bỏ bớt

| Bỏ | Lý do |
|---|---|
| Kỹ năng `E` (Blink) và `R` (Phòng thủ) | Cắt phạm vi để hoàn thiện phần cốt lõi. Ba kỹ năng làm sơ sài kém hơn một kỹ năng làm kỹ |
| Spike **cắm dính** vào người trúng | Luật "hút nhầm ăn ×2" đã đủ tạo bản sắc cho Spike |
| Grapple vẽ **xích từ trường** bằng LineRenderer | Cách hiện tại (lướt nhanh về phía địch) đơn giản và ít lỗi hơn. Thuần thẩm mỹ |
| Tách lớp `HeavyObject` / `SpikeObject` / `ExplosiveObject` | Xem 5.1 |
| Tách `PlayerCameraController`, `NetworkLobbyManager` | Logic quá nhỏ để tách file |
| **Danh sách phòng** | Vào phòng bằng mã PIN đã chạy tốt. Danh sách phòng là tiện lợi, không phải yêu cầu, mà lại là code mạng chưa từng test |

## 8.4. Ánh xạ lại — giữ tên, đổi ý nghĩa

Do bỏ thanh máu, hai vật phẩm Shop được ánh xạ lại nhưng **giữ nguyên tên hàm trong code**:

| GDD gốc | Thực tế |
|---|---|
| Giáp phòng hộ — hấp thụ sát thương | **Giáp Cách Điện** — hấp thụ điện tích thay cơ thể |
| Băng Gạc Nano — hồi 20 HP | **Bộ Xả Điện** — xả 20 điểm điện |

Công thức "tối đa 50%" giữ nguyên ở cả hai.

## 8.5. Thêm mới, không có trong GDD gốc

`PlayerVisuals` (nhãn quan địch/bạn) · `CameraShake` · cơ chế **ngủ đông vật thể** (chữa việc vật trượt mãi trên địa hình gồ ghề) · `CursorLock` (trọng tài con trỏ) · `SpectatorController` (quan sát khi chết) · `EnvironmentScatter` (công cụ rải trang trí) · `Billboard` · hai shader tự viết · reset toàn bộ bản đồ mỗi round.

## 8.6. Kết luận đối chiếu

Game **đi đúng hướng ở phần bản sắc** và **đổi hướng có chủ đích ở phần luật thắng thua**.

Mọi thay đổi đều đi theo một mạch: bản GDD gốc mô tả một FPS truyền thống được khoác lớp áo từ tính lên trên. Trong quá trình dựng, phần từ tính chứng minh nó đủ sức làm **trung tâm** chứ không phải lớp áo — nên các hệ thống vay mượn từ FPS truyền thống (thanh máu, hạ gục để thắng) bị thay bằng hệ thống mọc ra từ chính cơ chế từ tính (nhiễm điện, lực văng, rơi khỏi đảo).

Phần bị cắt (`E`, `R`, cắm dính, xích LineRenderer, danh sách phòng) đều là **tính năng phụ**, cắt để dồn thời gian cho phần cốt lõi — không có phần nào thuộc về bản sắc của game.
