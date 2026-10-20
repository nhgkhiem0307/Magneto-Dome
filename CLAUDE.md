# CLAUDE.md — Magneto-Dome

Tài liệu này là bộ nhớ dài hạn cho Claude Code khi làm việc trên project này.
Ngôn ngữ làm việc: **tiếng Việt** (cả trao đổi lẫn comment trong code).

> 📌 **Đang làm tới đâu → xem [PROGRESS.md](PROGRESS.md).**
> File CLAUDE.md này chứa *đặc tả và quy tắc* (ít thay đổi).
> File PROGRESS.md chứa *tiến độ* (cập nhật liên tục). Đọc cả hai trước khi bắt tay vào việc.
>
> 📋 **Sửa bất cứ thứ gì thì phải cập nhật tài liệu liên quan ngay** — bảng tra cứu
> "sửa cái gì → sửa file nào" nằm ở **Mục 6**, kèm danh sách toàn bộ tài liệu của project.

---

## 1. Bối cảnh dự án

**Magneto-Dome** — đồ án tốt nghiệp (Capstone) của Nguyễn Hoàng Gia Khiêm, lớp K24M-GAME1, chuyên ngành Lập trình Game.

| Mục | Giá trị |
|---|---|
| Thể loại | FPS đối kháng 2v2, Round-based Tactical Arena PvP. Có thêm thể thức **1v1** *(16/10)* |
| Bối cảnh | "Sky Arena" — hòn đảo lơ lửng giữa không trung |
| Điểm độc đáo | Không có súng đạn. Dùng **găng tay từ tính** hút/đẩy vật thể trong map làm đạn |
| Làm một mình | Có — không có team |
| Deadline | Khoảng **giữa tháng 9/2026** (7 tuần tính từ 29/07/2026) |
| Phạm vi bắt buộc | Gameplay hoàn chỉnh + thắng/thua + UI + hình ảnh + âm thanh |

### Về file GDD.md
GDD *(`GDD.md` ở thư mục gốc — trước đây là `GDD.txt`)* là bản nháp **hơi sơ sài và không bắt buộc phải bám sát**. Khi GDD mâu thuẫn với thực tế code hoặc với quyết định gần đây của chủ project, **ưu tiên quyết định của chủ project**. Các mục đã chốt được ghi lại ở Mục 4 bên dưới — dùng mục đó làm chuẩn.

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
| 0 | `Assets/MenuScene.unity` | Menu + Lobby + ghép trận — **scene chạy đầu tiên khi mở game** |
| 1 | `Assets/TestScene.unity` | **Scene gameplay chính** |

> *(Đối chiếu `EditorBuildSettings.asset` ngày 14/10.)* `Assets/Scenes/SampleScene.unity` vẫn
> nằm trong project nhưng **không còn trong danh sách build** — bảng cũ ghi nó là số 0 là sai.

**Icon game:** `Assets/Textures/GameIcon.png` (512×512, vuông) — **đã gắn** vào Player Settings → `Default Icon` 14/10.

**Build bản nộp:** chạy `Assets/Editor/BuildNop.cs` (menu `Magneto-Dome → Build bản nộp`, hoặc dòng lệnh
`-executeMethod BuildNop.Build` khi Unity đang tắt). Tự gắn icon + tắt Development Build, ra `../Build_Nop`.

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
├── RoomPlayer.cs                # NetworkBehaviour: đồng bộ NickName + Team trong lobby. Giữ chế độ 1v1/2v2
│                                #   (ô RoomTeamSize trên RoomPlayer của chủ phòng) + hàm Kick()
├── Editor/
│   └── BuildNop.cs              # Build BẢN NỘP 1 lệnh: gắn icon, tắt Development Build, ra ../Build_Nop
│                                #   (menu Magneto-Dome → Build bản nộp). Chỉ chạy trong Editor, không vào game
└── Scripts/
    ├── Player/
    │   ├── FPSMovement.cs               # [Fusion] WASD + xoay + Dash(Q) + trọng lực + AddImpact()
    │   ├── NetworkInputData.cs          # [Fusion] struct gói input gửi qua mạng + enum InputButton
    │   ├── PlayerMagnetController.cs    # Nạp điện, hút/đẩy, cận chiến, cầm/bắn vật thể
    │   ├── PlayerHealth.cs              # Máu (chưa có giáp)
    │   ├── InventorySystem.cs           # Túi đồ: lưu GameObject thật đã bị ẩn đi
    │   ├── PlayerHotbarController.cs    # Z/X/C rút đạn Normal/Heavy/TNT (tạm) từ túi
    │   ├── RadialMenuController.cs      # UI vòng tròn giữ Tab, chọn theo góc chuột
    │   └── PlayerInteract.cs
    ├── Item/
    │   ├── MagneticObject.cs            # Điện tích, 4 loại vật thể, va chạm, nổ TNT
    │   ├── MagneticAura.cs              # Hiệu ứng hào quang theo điện tích
    │   ├── CombatVFX.cs                 # Kho hiệu ứng chiến đấu dựng bằng mã (không ảnh, không prefab): tia nạp
    │   │                                #   điện + sét, đấm, nổ TNT (Particle System), can xăng (17/10)
    │   ├── EMBarrier.cs                 # [Fusion] Tường tạm 10s của EM Barrier Core. Hình tự dựng bằng mã
    │   │                                #   (lớp lồng EMBarrier.Visual) + shader Resources/Shaders/HexShield.shader
    │   └── ItemData.cs                  # ScriptableObject định nghĩa vật phẩm
    ├── MenuUI/
    │   ├── RoomItemUI.cs
    │   ├── PingDisplay.cs           # Hiện ping góc trên phải, tự dựng UI bằng code, F4 ẩn/hiện
    │   ├── DeathScreen.cs           # Màn hình khi chết (14/10): ai hất mình + đòn gì, vòng đếm ngược,
    │   │                            #   địch +8, mẹo chơi. Tự dựng UI bằng code. Thay DeadPanel cũ của HUD
    │   ├── MenuBackdrop.cs          # (16/10) Camera bay vòng quanh đảo làm nền menu + nắng xoay theo camera
    │   │                            #   + chỉnh màu tông xanh (Volume tạo lúc chạy). Trên Main Camera của MenuScene
    │   ├── LobbyView.cs             # (16/10) Phòng chờ dạng thẻ người chơi: YOU, vương miện chủ phòng, COPY mã
    │   │                            #   phòng, Host bấm thẻ để chuyển đội, nút 1V1/2V2, nút KICK. Trên RoomLobbyPanel
    │   ├── ModePicker.cs            # (16/10) Bảng hỏi "1v1 hay 2v2?" khi bấm Ghép Trận. Tự dựng bằng code,
    │   │                            #   không gắn vào scene — NetworkRunnerHandler gọi ModePicker.Show()
    │   └── BuffScreenFX.cs          # (17/10) Hiệu ứng trên màn hình khi tự dùng Nước tăng lực / Bộ xả điện.
    │                                #   Tự dựng bằng code; InventorySystem gọi qua bộ đếm mạng ConsumableUseCount
    └── Dummy/
        ├── DummyGravity.cs              # Bản sao FPSMovement cho bù nhìn tập bắn
        └── DummyMagnetTarget.cs
                                         # 14/10: con bù nhìn đã XOÁ khỏi TestScene. Prefab vẫn giữ ở
                                         #   Assets/Prefab/Dummy.prefab — kéo vào scene là dùng lại được
```

Ngoài ra `Assets/Photon/`, `Assets/ParrelSync/`, `Assets/Resources/` (asset store), `Assets/TutorialInfo/` là **code bên thứ ba — không sửa**.

### ⚠️ MenuScene có một BẢN SAO hòn đảo làm nền *(16/10)*

Object `MenuBackdrop_Island` trong `MenuScene` là bản sao **chỉ có hình** của nhóm `Environment`
trong TestScene — đã lột hết `NetworkObject`, `Rigidbody`, va chạm và script gameplay (chỉ giữ
`CloudBank` cho biển mây). Kèm theo: ánh sáng, bầu trời `FS000_Day_03`, sương mù chép từ TestScene;
lớp phủ tối `Assets/Textures/MenuOverlay.png`; logo chữ `MD_Logo` dựng sẵn trong scene.

- **TUYỆT ĐỐI không đặt `NetworkObject` nào vào MenuScene.** Menu có Fusion Runner chạy (phòng chờ),
  object mạng thừa trong scene có thể bị đăng ký nhầm.
- **Sửa đảo trong TestScene thì bản sao ở menu KHÔNG tự cập nhật.** Thường không sao (chỉ là nền),
  nhưng nếu đổi hẳn hình dáng đảo thì phải chép lại.

### Phòng chờ — CHỈ CHỦ PHÒNG được đổi đội *(chốt 16/10)*

Client không tự đổi đội được. Chủ phòng đổi đội cho chính mình (nút Switch Team) hoặc bấm vào
thẻ của người khác để chuyển họ sang đội kia (khi đội kia còn chỗ, tối đa `RoomPlayer.TeamSize` = 2).
- Chốt chặn thật ở **phía Host**: `RoomPlayer.RPC_RequestTeamChange` bỏ qua yêu cầu nếu `!IsHost`.
  Ẩn nút ở Client (`LobbyView`) chỉ là lớp giao diện.
- `RoomPlayer.IsHost` là `[Networked]` — Client không tự biết ai là Host.
- Host đổi đội người khác bằng `RoomPlayer.TryMoveToTeam()` — Host giữ quyền ghi mọi RoomPlayer nên không cần RPC.

### Thể thức 1v1 / 2v2 + nút Kick *(chốt 16/10)*

- **Ghép trận nhanh:** bấm nút thì hỏi 1v1 hay 2v2 (`ModePicker`), chỉ ghép vào phòng đúng thể thức.
  Nhớ lựa chọn lần trước (`PlayerPrefs` khoá `quick_match_team_size`).
- **Tạo phòng thường:** luôn mở ở 2v2, chủ phòng đổi bằng nút **1V1 / 2V2** ở đầu phòng chờ.
- **Đổi sang thể thức nhỏ hơn khi phòng đông hơn sức chứa → TỪ CHỐI**, hiện dòng báo "kick bớt người
  trước". Game **không tự đuổi ai** (chủ project chốt). Đổi được thì tự dàn lại đội (ưu tiên chuyển người
  không phải chủ phòng).
- **Bấm Start: không chặn gì** (chủ project chốt) — như trước, kể cả lệch người.
- Chế độ lưu ở **`RoomPlayer.RoomTeamSize` trên RoomPlayer của chủ phòng**, đọc qua `RoomPlayer.TeamSize`.
  Kèm thuộc tính phòng Photon `"ts"` để danh sách phòng / ghép trận đọc được mà chưa cần vào phòng.
- ⚠️ **Phòng nào cũng mở 4 chỗ trên Photon** — Fusion không đổi được số chỗ sau khi tạo. Phòng 1v1 bị chặn
  ở người thứ 3 bằng code: `NetworkRunnerHandler.OnConnectRequest` (từ chối ngay từ cửa) + lớp thứ hai
  trong `OnPlayerJoined`. Hiển thị sức chứa thì dùng `NetworkRunnerHandler.SessionCapacity()`, **đừng dùng
  `session.MaxPlayers`** (luôn là 4).
- **Kick:** nút KICK trên thẻ người khác, chỉ chủ phòng thấy, **bấm hai lần** (lần đầu hiện SURE?).
  `RoomPlayer.Kick()` gửi RPC báo trước để máy kia tự rời phòng và hiện "You were kicked…", rồi 1,5 giây sau
  Host cắt kết nối phòng khi máy kia không chịu rời.
- **Người bị kick không vào lại được phòng đó** (sổ đen theo `ConnectionToken` của Fusion, xoá khi mở phòng
  mới). Thẻ nhận dạng sinh ngẫu nhiên **mỗi lần mở game**, cố ý KHÔNG lưu `PlayerPrefs` — các clone ParrelSync
  trên cùng máy dùng chung PlayerPrefs, lưu vào đó thì kick một clone là cấm luôn mọi clone.
- Phòng chờ báo một dòng khi có người vào / ra / bị kick, khi chủ phòng đổi chế độ (Client thấy), và khi
  bấm Switch Team mà đội kia đầy. Dưới nút Start có gợi ý "Teams are uneven" / "You're alone" — **chỉ nhắc,
  không chặn**.
- ⚠️ Font `fontMedium` của phòng chờ **không có dấu gạch dài "—"** (hiện thành khoảng trống). Dùng "·".

### Rào chắn pha mua đồ + biển mây — giấu giới hạn bản đồ *(17/10)*

- **Rào chắn** (`RoundBarrier.cs` + shader `Resources/Shaders/BarrierCurtain.shader`): màn năng lượng theo
  **màu đội**, dạng **KÍNH MỜ kiểu Valorant nhưng đậm hơn** *(19/10 — trước đó đặc ~99%, chủ project đổi ý)*:
  thấy dáng người / cây phía sau nhưng nhoè + ám màu đội (lấy ảnh từ **Opaque Texture** — đã bật sẵn trong
  `PC_RPAsset`, tắt đi là mất kính mờ). Ô chỉnh: `_HazeAlpha` 0.8 (độ mờ), `_BlurPx` 7, `_Distort`, `_BgTint`.
  **Viền địch, bảng tên trên đầu, hiệu ứng phía sau rào bị CHE**: shader có lượt riêng `BarrierDepth` ghi
  chiều sâu ở phần tường đủ đậm (độ che > 0.5), và tường vẽ ở `renderQueue` **2995** — trước mọi vật trong
  suốt khác — nên chúng thua phép thử chiều sâu. Viền đồng đội (`OutlineAll`, ZTest Always) vẫn xuyên tường.
  ⚠️ Đừng đưa `renderQueue` về sau 3000: bảng tên địch (chữ trong suốt, vẽ ở 3000) sẽ đọc được xuyên rào.
  Vật liệu dựng bằng code lúc chạy (vật liệu `New Material` / shader Force Field trong scene chỉ còn
  là dự phòng khi không tìm thấy shader mới). Gợn sóng khi người chơi áp sát hoặc vật thể đâm vào; vào trận
  thì **va chạm tắt ngay**, hình tan từ dưới lên 0,8 giây.
  ⚠️ **Đừng đổi cách tan sang "theo khoảng cách tới camera"** — đã thử: nhìn xiên dọc tường thì phần xa trong
  suốt, nhìn xuyên được sang căn cứ địch. Tường chỉ tan **ngoài mép đảo** (tính từ tâm đảo, `fadeStart` 40 →
  `fadeEnd` 85) và khi lên cao; đảo gần tròn nên mọi đường nhìn giữa hai căn cứ đều cắt tường ở chỗ đặc.
  Phía dưới: tường **chạy tiếp xuống dưới vách đảo rồi chìm vào biển mây** (`_DepthFadeStart` 6 → `_DepthFadeEnd`
  45m dưới mặt đảo) — chủ project muốn đứng mép đảo nhìn xuống vẫn thấy. Vạch sáng chân tường chỉ ở đúng ngang
  mặt đất và trong bán kính đảo (`_IslandRadius` 36), không thì phần dưới vực sáng rực cả mảng.
  Mỗi ô lục giác lệch màu nhẹ: sáng tối ±20% (`_CellBright` 0.4), sắc chỉ ±6° (`_CellHue` 0.2), ~7% ô thỉnh
  thoảng loé (`_Sparkle`). ⚠️ Đừng tăng `_CellHue` — đã thử ±13°: màu đỏ bão hoà xoay vài độ là ra ô màu ô liu /
  hồng tím như vải vụn. Đường viền ô giữ một màu.
  Độ đậm nhạt của thân màn: `_HazeDark` 0.11 / `_HazeBright` 0.36 / `_HazePale` 0.28 (pha trắng) — đã nhạt
  lại 17/10 theo yêu cầu. Muốn đậm lại: giảm ba ô này (bản đậm cũ là 0.045 / 0.20 / 0). Không đụng tới độ đặc.
- **Biển mây** (`CloudBank.cs`) có thêm 3 thứ, đều bật/tắt được trong Inspector:
  - `buildCloudFloor` — **đáy biển mây** (shader `CloudFloor.shader`): tấm mây mềm dưới các cụm, tan vào
    màu chân trời → nhìn ra / nhìn xuống không còn thấy khoảng trời phẳng lì giữa các cụm.
    `floorHorizonColor` phải khớp màu trời ngay trên chân trời, lệch là lộ đường kẻ ngang.
  - `softenPuffEdges` — ảnh `CloudPuffs.png` có cụm vẽ sát mép ô với cạnh gần thẳng đứng; script tạo bản sao
    viền tan dần lúc chạy. **Cần ảnh bật Read/Write** (đã bật trong `.meta` 17/10).
  - `nearFadeSize` — cụm mây mờ đi khi camera lọt vào (lúc rơi khỏi đảo).
  Đáy biển mây là object ẩn (`HideAndDontSave`) — không thấy trong Hierarchy, không ghi vào scene.
  MenuScene cũng có `CloudBank` nên nền menu tự có đáy biển mây.

**Bảng Settings tạm ẩn mọi giao diện phía sau khi mở** (`SettingsUI.HideEverythingBehind`): ẩn các
object anh em trong cùng Canvas + các Canvas gốc có thứ tự vẽ thấp hơn (HUD, màn hình chết). Đóng
lại thì trả đúng những thứ đang hiện lúc mở. Nền bảng đổi từ ảnh `dd_template` (có màu sẵn, chỉnh alpha
không ăn) sang màu trơn xanh đậm 60% — sửa trên cả MenuScene lẫn TestScene.

**Nút Quit Game** *(16/10)* nằm bên trái nút Close, có ở cả menu lẫn trong trận. Bấm vào thì hiện bảng
"QUIT GAME?" phủ tối toàn màn hình, nút Cancel được chọn sẵn, `Esc` chỉ đóng bảng này. Cả nút lẫn bảng
**dựng bằng code lúc chạy** (`SettingsUI.BuildQuitUI`), là **bản sao** nút Leave Match và
`LeaveConfirmPanel` — không có trong prefab, đừng đi tìm trong Hierarchy. Dòng cảnh báo đổi theo hoàn
cảnh: chủ phòng thoát thì báo trận/phòng sẽ kết thúc cho mọi người.
Bảng "Leave match?" cũng được sửa: số giây đếm ngược nằm trên nút (`Confirm (3)`), câu cảnh báo luôn
một dòng — trước đây số giây chèn vào dòng thứ hai làm câu phình lên đè tiêu đề.

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
| Chuột trái | Vật trung tính → nạp điện. Vật đã có điện → **cùng dấu = ĐẨY**, **trái dấu = HÚT về tay**. Đang cầm vật **chưa có điện** → nạp ngay theo cực găng *(19/10)* |
| Chuột phải | Tay trống → cận chiến. Đang cầm vật → **chỉ bắn được khi vật CÙNG dấu với găng** *(19/10)*. Trái dấu → không bắn, viền đỏ + rung nhẹ báo lỗi. Chưa có điện → đẩy nhẹ ra trước, sát thương ×0.3 |
| `V` | Tung hứng vật đang cầm lên không |
| `Z` / `X` / `C` | Rút đạn Normal / Heavy / **TNT** từ túi ra tay. *(Ô `C` tạm là TNT từ 19/09 vì map chưa có vật Spike — đổi lại ở `PlayerHotbarController.ThirdSlotType`)* |
| `Tab` (giữ) | Mở Radial Menu, xoay chuột chọn, thả phím để dùng |
| `B` | Mở Shop (chỉ trong Buy Phase) |
| `F` | Nhặt vật phía trước vào túi / cất vật đang cầm vào túi (`PlayerInteract.interactKey`) |
| `Esc` | Mở/đóng bảng Settings |
| `F1` | Bật/tắt bảng hướng dẫn điều khiển *(tự hiện ở pha chuẩn bị)*. Phím phụ `H` cho Editor |
| `F4` | Bật/tắt ô hiển thị **ping** ở góc trên bên phải *(`PingDisplay.cs`)* |

### Cận chiến (chuột phải, tay trống)

- **Khoảng cách < 6m, cùng dấu** → Đấm đẩy văng đối thủ (Repel Punch).
- **Khoảng cách < 6m, trái dấu** → Nảy bật nhẹ cả hai bên.
- **Khoảng cách 6–60m, trái dấu** → Kéo áp sát (Grapple): lướt nhanh về phía đối thủ.
  > *(Đối chiếu prefab 16/10: `meleeRange 6`, `dashLockRange 60` — bản cũ ghi 3m / 3–8m là sai.
  > Mặc định trong code là 4 và 20, nhưng Inspector đè lên.)*
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
| **TNT** | 35 | Nổ bán kính 6m, giảm dần theo khoảng cách, hất văng diện rộng. Đang bay tự nổ khi địch lọt vào **5.2m** (`tntProximityRadius`, +30% ngày 19/10) | ✅ |

**Luật bắn vật đang cầm** *(chốt 19/10)*: cùng dấu đẩy / trái dấu hút áp dụng cả cho vật trên tay. Vật HÚT về tay
**luôn trái dấu** với găng (chỉ trái dấu mới hút được) và điện tích vật giữ tới hết round → muốn bắn phải **bấm
1/2 đổi cực găng trước**. Đây là nhịp kỹ năng có chủ ý, không phải lỗi. Bấm bắn khi trái dấu → vật ở lại trên tay,
viền đỏ (`BuffScreenFX.Denied`, qua bộ đếm `WrongPolarityCount`). Vật chưa có điện: chuột trái nạp ngay theo cực
găng; chuột phải đẩy nhẹ (`neutralPushSpeed` 7, vẫn là đạn nhưng sát thương × `neutralPushDamageScale` 0.3).

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
| Lực văng | Nhân tuyến tính `x1.0` (sạch điện) → `x4.0` (đầy điện) ⚠️ *mặc định trong code là 5, nhưng `Player.prefab` đang để **4** — Inspector mới là thứ chạy thật* |
| Cái chết **duy nhất** | Rơi khỏi đảo (`GameManager.CheckKillZone`) — **chỉ tính trong pha chiến đấu** |
| Rơi ngoài pha chiến đấu | **Không chết** — `PlayerHealth.RescueToSpawn()` kéo về điểm xuất phát, giữ giáp/đồ, địch không được điểm *(chốt 14/10)* |
| Hết giờ Combat (**5 phút**) | Đội có **tiến độ chiếm khu cao hơn** thắng round. Bằng nhau thì **HOÀ**, không ai được điểm |
| Sang round mới | Xả sạch điện về 0 |

**Vì sao đầy điện không chết:** nếu đầy là chết thì đây chỉ là thanh máu chạy ngược, không có gì mới.
Để cái chết đến từ **VỊ TRÍ** mới tạo được sự căng thẳng thật — đứng giữa sân với 90% điện vẫn an toàn,
đứng sát rìa với 30% đã là mạo hiểm. Đây là cơ chế phần trăm của Smash Bros. đặt vào FPS từ tính.

**Vì sao pha Combat phải có giới hạn giờ:** không còn ai chết vì hết máu, nên hai bên cùng né rìa vực
thì round kéo dài vô tận. Đồng hồ **5 phút** (`combatDuration = 300`) là bắt buộc, không phải tuỳ chọn.

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
| Nổ TNT | `explosionPlayerKnockback` 180 ở tâm → 40% ở mép (`explosionKnockbackEdge`), hướng ra xa tâm theo phương ngang *(19/10 — trước là `explosionForce × 2` = 30, gần như không đẩy được ai)* | `MagneticObject.Explode()` |

⚠️ Knockback khi trúng đạn **trước 09/08 KHÔNG TỒN TẠI** — va chạm chỉ trừ máu rồi thôi.
Nếu thấy nhân vật không bị đẩy khi trúng đạn thì kiểm tra nhánh `CompareTag("Player")` trước tiên.

**🚫 KHÔNG CÓ SÁT THƯƠNG ĐỒNG ĐỘI** *(chốt 19/09)*. Lý do chính: mỗi người chết thì
**đội ĐỐI PHƯƠNG được +8 tiến độ chiếm khu** (`GameManager.deathZoneBonus`), bất kể ai hạ
người đó. Nên đánh chết đồng đội là tự tặng điểm cho địch, lại còn mất một người giữ khu.
Mọi chỗ đi qua một hàm duy nhất `PlayerHealth.AreTeammates()`:

| Đòn | Trúng đồng đội |
|---|---|
| Đấm / Grapple / hỗ trợ ngắm | Bỏ qua đồng đội khi **chọn mục tiêu** (`PlayerMagnetController.IsTeammate`) |
| Đạn | Dừng lại như đâm tường, không nạp điện, không đẩy, **không xuyên qua** |
| TNT nổ / kích nổ gần | Bỏ qua đồng đội của **người ném**. Chính người ném **vẫn dính** sóng nổ |
| TNT **chưa ai ném** | Không thuộc phe nào → trúng tất cả, như bẫy của map |

`AreTeammates(a, a)` trả **false** — bản thân không phải đồng đội. Đừng "sửa" thành true,
sẽ vô tình đổi luật người ném TNT tự dính nổ. Bù nhìn (không có `PlayerHealth`) luôn là địch.

**Buff sát thương toàn cục:** `PlayerHealth.chargeGainMultiplier` (1.5) nhân **mọi** lượng điện
nhận vào, ngay đầu `TakeDamage()` trước cả giáp. Muốn chỉnh độ sát thương chung thì sửa ô này,
**đừng** đi sửa `baseDamage` từng prefab.

**Ánh xạ lại 2 món Shop** (giữ nguyên tên hàm nên `ShopManager`/`InventorySystem` không phải sửa):
- `AddArmor()` → **Giáp Cách Điện**: hấp thụ điện thay cơ thể, hỏng dần
- `ApplyBandage()` → **Bộ Xả Điện**: xả tối đa 20 điểm, không quá 50% lượng đang mang

⚠️ **Mode này phụ thuộc nặng vào map.** Bị hất bay 15m chỉ đáng sợ khi có rìa vực để rơi.
Trên map cube hiện tại gần như không ai chết, mọi round kết thúc bằng hết giờ — **đó là dấu hiệu
map chưa sẵn sàng, không phải mode hỏng.**

### Vòng đấu & Điều kiện thắng (ĐỐI CHIẾU VỚI CODE 10/10/2026)

> ⚠️ **Mục này từng sai suốt một thời gian dài** — vẫn chép luật cũ của GDD trong khi code
> đã đổi từ 16/08. Dưới đây là luật ĐÃ ĐỐI CHIẾU VỚI `GameManager.cs`. Nếu sau này sửa luật,
> nhớ sửa luôn cả `GDD.md`, báo cáo và slide — xem bảng tra ở Mục 6.

**Chỉ có HAI đường thắng một round**, cộng khả năng hoà. `EndRound()` chỉ được gọi từ
`CheckZoneVictory()`, `EndRoundByProgress()` và phím debug — không còn đường nào khác:

| Đường | Chi tiết |
|---|---|
| **Chiếm đủ khu** | Tiến độ chạm `100` là thắng round ngay lập tức |
| **Hết giờ 5 phút** | Đội có tiến độ **cao hơn** thắng (`EndRoundByProgress`) |
| **Hoà** | Hết giờ mà hai bên bằng tiến độ → không đội nào được điểm |

❌ **KHÔNG còn luật "hạ gục toàn đội địch là thắng round".** Hàm `EndRoundByCharge()` so tổng
điện tích cũng **đã bị bỏ** — nó vốn chỉ là giải pháp tình thế khi chưa có khu chiếm đóng.

❌ **Chết KHÔNG còn là bị loại khỏi round** *(đổi 16/08)*. Chết chỉ lấy đi **thời gian**
(hồi sinh sau 5 giây) và **vị trí** (bị đưa về điểm xuất phát), cộng **+8 tiến độ cho đối
phương**. Nhờ vậy lực văng được phép mạnh tay mà không gây ức chế.
- Đội thắng round được **+1 điểm**.
- Thắng chung cuộc: **đạt 5 điểm trước VÀ cách biệt tối thiểu 2 round**.
- Nếu tỉ số 5-4 → vào **Overtime**, đấu tiếp tới khi một đội hơn 2 round (6-4, 7-5...).

> Ghi chú: có lúc chủ project nói "đến 5 trước là thắng luôn", nhưng đã **chốt lại dùng bản GDD có luật cách biệt 2 round**.

### Buy Phase & Kinh tế (chưa làm)

- Đầu mỗi round có **15 giây** đếm ngược, người chơi bị chặn bởi rào chắn (Barrier).
- Thắng round: **+$300**. Thua round: **+$200** *(x2 ngày 19/10, trước là 150/100)*. Trần ví: **$750**.

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
- Mục tiêu phi chức năng: **≥60 FPS ở 1920×1080**, **ping < 100ms** ở region asia.
  > ✅ **Đã đạt 11/10: 85 FPS** trên bản đóng gói (RX 6600, 1920×1080). Chỉ tiêu FPS
  > **bắt buộc phải kèm độ phân giải** — cùng bản build đó chạy ở 2560×1440 chỉ được
  > 38 FPS. Xem chương "Ngày 11/10" trong PROGRESS.md.

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

### 📋 SỬA GÌ THÌ CẬP NHẬT TÀI LIỆU ĐÓ — BẮT BUỘC, KHÔNG CẦN HỎI

**Chốt 10/10/2026 theo yêu cầu của chủ project.**

Mỗi khi thay đổi bất cứ thứ gì — một con số cân bằng, một luật chơi, một script, một
tài nguyên — thì **cập nhật luôn mọi tài liệu có nhắc tới thứ đó, ngay trong cùng lượt
làm việc**. Không chờ được nhắc, không để dành "lát nữa làm".

**Vì sao bắt buộc:** tài liệu sai còn tệ hơn không có tài liệu. Một con số cũ nằm trong
`BALANCE.md` hay trong báo cáo sẽ được tin là đúng, rồi nó đi thẳng vào bài bảo vệ. Lúc
hội đồng hỏi "số này ở đâu ra" mà mở code lên thấy số khác là mất điểm, trong khi sửa
một dòng bảng lúc đó chỉ tốn mười giây.

**Cách làm:** sửa xong code thì tra bảng dưới, mở đúng những file được liệt kê, sửa,
rồi **nói rõ trong câu trả lời là đã cập nhật file nào**.

#### Tra cứu: sửa cái gì → phải cập nhật file nào

| Vừa thay đổi | Cập nhật những file này |
|---|---|
| **Con số cân bằng** *(Inspector, prefab, hằng số trong code)* | `BALANCE.md` · `CLAUDE.md` Mục 4 nếu số đó có trong đặc tả · `NOP_BAI/[...BM06] Báo cáo tốt nghiệp.docx` Phụ lục A · `NOP_BAI/[...BM08] Final Project.pptx` slide 7, 8 và slide 17 *(Balance Sheet)* |
| **Luật chơi, cơ chế mới, bỏ cơ chế** | `CLAUDE.md` Mục 4 · `GDD.md` · `BaoCao` chương 2 · `Slide` slide 6–9 |
| **Phím điều khiển** | `CLAUDE.md` Mục 4 bảng phím · `ControlsOverlay.cs` *(tự đọc KeyCode nên thường không phải sửa, nhưng phải kiểm)* · `TRAILER_SCRIPT.md` nếu ảnh hưởng tới cách quay |
| **Thêm / xoá / đổi vai trò script** | `CLAUDE.md` Mục 3 *(cây thư mục + bảng đã-networked-chưa)* · `PROGRESS.md` Mục 5 bản đồ code · `Slide` 18 *(Code Architecture)* |
| **Tài nguyên bên thứ ba** *(asset, nhạc, font, giọng)* | `CREDITS.md` · `BaoCao` Phụ lục B · `Slide` 19 *(Credits)* |
| **Câu thoại / giọng thông báo** | `VOICE_BRIEF.md` · `CREDITS.md` mục 3b |
| **Thay đổi TẠM để test hoặc chụp ảnh** | `ROLLBACK_TRUOC_KHI_NOP.md` — ghi ngay lúc thay, đừng tin trí nhớ |
| **Sửa xong một lỗi đã biết** | `CLAUDE.md` Mục 7 · `PROGRESS.md` |
| **Làm xong một hạng mục** | `PROGRESS.md` |
| **Bẫy kỹ thuật mới phát hiện** | `CLAUDE.md` Mục 5 *(danh sách bẫy Fusion/vật lý)* |

#### Danh sách tài liệu của project — khỏi phải đi tìm

| File | Chứa gì | Ai đọc |
|---|---|---|
| `CLAUDE.md` | Đặc tả và quy tắc. **Ít thay đổi** | Claude, mỗi phiên |
| `PROGRESS.md` | Tiến độ, nhật ký công việc. **Thay đổi liên tục** | Claude, mỗi phiên |
| `BALANCE.md` | Toàn bộ con số cân bằng, kèm nơi chỉnh | Chủ project khi cân bằng game |
| `GDD.md` | Tài liệu thiết kế game | Nộp kèm |
| `CREDITS.md` | Khai báo tài nguyên bên thứ ba và giấy phép | Nộp kèm, hội đồng hay hỏi |
| `VOICE_BRIEF.md` | Chỉ đạo giọng ARENA CONTROL, prompt ElevenLabs | Khi làm thêm câu thoại |
| `TRAILER_SCRIPT.md` | Kịch bản trailer, cách quay, chính sách HUD | Khi quay trailer |
| `ROLLBACK_TRUOC_KHI_NOP.md` | Mọi thứ phải trả lại trước khi nộp | **Đọc trước khi build bản nộp** |
| `VIEC_CAN_LAM.md` | **Danh sách việc còn lại**, ngắn gọn, kèm hướng dẫn build bản nộp từng bước | Chủ project — **mở ra đầu tiên** |
| `NOP_BAI/[ĐT-QĐ04-BM06] Báo cáo tốt nghiệp.docx` | Báo cáo 53 trang *(Word đếm, 14/10)*, 4 chương + 2 phụ lục, 10 hình, 16 bảng. Có Danh mục hình/bảng tự sinh — sửa nội dung xong nhớ `Ctrl+A` → `F9` | Nộp chính |
| `NOP_BAI/[ĐT-QĐ04-BM08] Final Project.pptx` | 15 slide bảo vệ + 4 slide phụ cuối *(Timeline, Balance Sheet, Code Architecture, Credits — từ 15/10 không còn ghi chữ "Dự phòng" trên slide)*. Tiêu đề kiểu pha Việt–Anh. **Không có slide demo** — demo trực tiếp bằng game (bỏ 15/10), ghi chú cách demo nằm ở slide 12 | Buổi bảo vệ |
| `SlideBuilder/` | **Script sinh ra file .pptx.** Sửa slide thì sửa ở đây rồi chạy lại | Xem `SlideBuilder/README.md` |
| `NOP_BAI/Poster_MagnetoDome_A1.pdf` | Poster A1 dọc *(19/10)*. Ảnh nền render từ game | In / trưng bày |
| `PosterBuilder/` | **Script sinh ra poster** (`make_poster.py`) + ảnh nền + font. Sửa chữ ở đây rồi chạy lại. `PosterShot.cs` trong đó cố ý nằm ngoài `Assets/` | Xem `PosterBuilder/README.md` |
| `NOP_BAI/` | **Thư mục nộp bài** — gom đủ 6 biểu mẫu theo tên chính thức của trường | Nộp cho Bộ Môn |

**Bộ biểu mẫu của trường** *(mẫu gốc ở Drive của khoa, bản đã điền nằm trong `NOP_BAI/`)*:

| Mã | Tên | Ai điền |
|---|---|---|
| BM01 | Phiếu đăng ký Project | Học viên — ✅ đã điền |
| BM02 | Đề cương chi tiết | Học viên — ✅ **cập nhật từ bản gốc của chủ project**, không phải từ bài mẫu |
| BM03 | Phiếu theo dõi Project | Học viên + GVHD ký từng tuần — ⬜ **chưa làm** |
| BM04 | Nhận xét của GVHD | GVHD viết — ✅ đã điền sẵn thông tin học viên |
| BM05 | Nhận xét của GV phản biện | GV phản biện viết — ✅ đã điền sẵn thông tin học viên |
| BM06 | Báo cáo tốt nghiệp | Học viên — ✅ chỉ còn **một bản duy nhất** trong `NOP_BAI/` |
| BM08 | Final Project (slide) | Học viên — ✅ sinh từ `SlideBuilder/` |

> 📌 **Thông tin học viên dùng chung cho mọi biểu mẫu:** Nguyễn Hoàng Gia Khiêm ·
> MSHV **144010124020** · Lớp **K24M-GAME1** · Ngành Công nghệ thông tin ·
> Chuyên ngành Lập trình Game · Khoá **K24** · Cơ sở **Đà Nẵng**.
> **Giảng viên hướng dẫn: cô Phạm Thị Hằng** *(chốt 14/10)* — đã điền ở báo cáo (2 bìa + Lời cảm ơn)
> và bìa slide. Các ô "(Ký và ghi rõ họ tên)" trong biểu mẫu thì **để trống** — cô ký và viết tay.

⚠️ Hai file `.docx` và `.pptx` là **định dạng nhị phân**, sửa khác với sửa file `.md`:
- `.docx` — đọc được bằng cách giải nén `word/document.xml`; sửa thì dùng thư viện, hoặc
  **nói rõ cho chủ project tự sửa tay** nếu chỉ là vài chữ.
- `.pptx` — **đừng sửa thẳng**, sửa `SlideBuilder/make_slide.js` rồi chạy lại.
  Nhưng nhớ: chạy lại là **mất ảnh đã chèn vào các khung ảnh**.

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

### ✅ Đã xử lý 01/09 — người chơi thoát / vào giữa trận

- **`OnPlayerLeft` đã despawn nhân vật.** Không còn "xác" đứng im trên map.
  Dọn dẹp phần sau gần như miễn phí: `PlayerHealth.Despawned()` tự gỡ khỏi `AllPlayers`,
  mà mọi vòng lặp trong `GameManager` đều duyệt danh sách đó — thưởng tiền, hồi sinh,
  KillZone, tiến độ chiếm khu đều tự sạch theo, **không phải sửa `GameManager` dòng nào**.

  ⚠️ Ba điều kiện bắt buộc, thiếu một là sinh lỗi mới:
  `runner.IsServer` *(Client despawn sẽ bị Fusion từ chối)* ·
  `_spawnedPlayers.Remove(player)` *(không xoá thì họ vào lại trận sau sẽ **không spawn được**,
  vì bị dòng `ContainsKey` chặn)* · kiểm `!= null` *(object có thể đã bị huỷ đường khác)*.

- **Người vào giữa trận: CHẶN HẲN.** Chốt thiết kế 01/09.
  Đây là đấu 2v2 theo round có tính điểm — người vào giữa chừng đẻ ra loạt câu hỏi không có
  đáp án hay: vào đội nào khi đang 2v1, có tiền không hay $0 sau 5 round, điểm tính từ đâu.
  CS, Valorant, Rocket League đều chặn vì đúng lý do đó.

  Làm **hai lớp**: `SessionInfo.IsOpen/IsVisible = false` lúc bấm Bắt Đầu *(lịch sự — họ
  không kết nối được ngay từ đầu)*, và `runner.Disconnect(player)` trong `OnPlayerJoined`
  *(bảo đảm đúng đắn — vẫn có khe hở vài mili giây giữa lúc bấm vào và lúc phòng khoá xong)*.

### 🟡 Đang treo chờ quyết định thiết kế

- **Đền bù kinh tế khi lệch quân số** (1v2 thì đội ít người được bù tiền, 1v1 giữ nguyên).
  Chưa chốt con số. Đề xuất: **+$100 mỗi đầu round** cho đội thiếu người, không phải một cục —
  cho cục thì họ tiêu hết ngay round đó rồi các round sau lại thiệt như cũ.

### 🟢 Thiếu tính năng (theo lộ trình, chưa phải lỗi)

- `RadialMenuController.cs` — vẫn là MonoBehaviour thuần, đọc `Input.` trực tiếp (đã vá tạm bằng cách tắt component khi không phải nhân vật mình).
- ~~Nhãn HUD ghi "MÁU"~~ — **đã hết, kiểm lại 10/10.** Quét toàn bộ `TestScene` không
  còn nhãn nào ghi "MÁU"; các nhãn HUD đều là `New Text` rồi được `HUDController` ghi
  lúc chạy. *(Lưu ý khi tự kiểm: TextMeshPro lưu chữ ở khoá `m_text` chữ thường,
  grep `m_Text` kiểu cũ sẽ không ra gì.)*
- ~~`Canvas / ShopPanel / TitleText / Text` ghi `Buying phase` viết thường~~ — **đã sửa
  14/10** thành `BUYING PHASE`, sửa thẳng file `TestScene.unity` lúc Unity tắt.
- ✅ **HỆ SỐ LỰC VĂNG TRÊN HUD — đã xong 11/10.** Trước đó code vẫn chạy nhưng ô
  `HUDController.knockbackText` để trống nên không bao giờ hiện ra. Nay đã gán:
  `Canvas / HUD / Group_BottomLeft / KnockbackText`, cỡ chữ 36, màu đỏ `#EE525A`.

  📌 **Nó TỰ ẨN khi chưa nhiễm điện** *(chủ project yêu cầu)*. Ngưỡng là
  `KnockbackMultiplier >= 1.05f`, không phải `> 1.0f`: chuỗi `:F1` làm tròn một chữ số
  nên mọi giá trị dưới 1.05 vẫn in ra đúng chữ `x1.0`. Nhờ vậy **không khung hình nào
  hiện "x1.0"**, và loạt ảnh chụp lúc điện tích bằng 0 trước đó vẫn khớp với bản cuối.

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

**12. Lực "đặt lại mỗi tick" gặp cờ "một lần mỗi cú bay" thì sinh ra vòng lặp sát thương** *(bẫy thiết kế — sửa 10/10)*
Triệu chứng: hút một vật về tay mà có địch đứng chắn đường thì họ **bay khỏi đảo ngay lập tức**,
mạnh hơn mọi đòn khác nhiều lần.
→ Nguyên nhân: đường HÚT trong `PlayerMagnetController` đặt lại `linearVelocity` **và** gọi
`MagneticObject.MarkAsBullet()` **mỗi tick** chừng nào còn giữ chuột. Vật trúng người thì
`ResetBulletState()` hạ cờ `isMovingAsBullet` — nhưng tick ngay sau đó `MarkAsBullet()` dựng cờ lên
lại, vật vẫn bị ép tốc độ nên đâm tiếp. Lặp khoảng **60 lần mỗi giây**, mỗi lần đều cộng điện và hất
văng, mà lực văng lại nhân theo lượng điện (tới ×5) — nên nó tự nhân lên theo cấp số.
→ Cách chữa: `MagneticObject.HitCooldownTimer` + ô `hitCooldown` (0.6 giây). Trúng người xong thì
**không được phong làm đạn lại** trong chừng đó. Đặt chốt chặn trong `MarkAsBullet()` chứ không phải
ở chỗ va chạm, để nó chặn **mọi** đường phong đạn lặp lại, kể cả phản ứng dây chuyền vật đâm vật.
→ `LaunchAsBullet()` (cú bắn) **cố ý không xét** khoảng khoá này — bắn là hành động một lần, không lặp.
→ **Bài học chung:** chỗ nào đặt lại trạng thái **mỗi tick** thì mọi cờ "chỉ một lần" đi qua chỗ đó
đều vô hiệu. Hai cơ chế đó không tự biết về nhau; phải có đồng hồ đứng giữa.

**13. Kiểm `!= null` KHÔNG phải là kiểm "đã spawn"** *(bẫy Fusion — sửa 10/10)*
Triệu chứng: Console spam liên tục mỗi khung hình
`InvalidOperationException: Error when accessing PlayerHealth.Team. Networked properties can only be accessed when Spawned() has been called.`
→ Nguyên nhân: component C# **có mặt ngay khi prefab được tạo**, nhưng Fusion chỉ cho đọc
thuộc tính `[Networked]` sau khi `Spawned()` chạy. Giữa hai thời điểm đó, `thing != null`
trả **true** mà `thing.Team` lại **ném exception**. Mọi chỗ trong project kiểm mỗi `!= null`
rồi đọc thẳng `.Team` đều dính.
→ Vì sao nó **spam** chứ không báo một lần: chỗ đọc nằm trong `Update()` và kết quả được
dùng để thoát khỏi trạng thái "chưa biết". Exception làm phép gán không bao giờ hoàn tất,
nên khung hình sau lại thử lại — vòng lặp vô tận. Thủ phạm gốc là `PlayerVisuals.TryResolveRelation()`.
→ Cách chữa: `PlayerHealth.NetworkReady` *(đã đổi từ private thành public)* —
`Object != null && Object.IsValid && Runner != null`. **Đọc bất kỳ thuộc tính `[Networked]`
nào của `PlayerHealth` từ script khác thì phải kiểm nó trước.** Đã vá 5 chỗ:
`PlayerVisuals`, `HUDController` (2 chỗ), `MatchResultPanel`, `SpectatorController`.
→ Hai thời điểm hay dính nhất: **vừa vào trận** (nhân vật người khác đã tạo nhưng chưa spawn
xong) và **lúc kết thúc trận** (Fusion đang tắt, object đã despawn mà script vẫn chạy `Update`).
→ **Bài học chung:** với Fusion, "có tồn tại" và "dùng được" là hai chuyện khác nhau.
Danh sách như `PlayerHealth.AllPlayers` thì an toàn hơn vì chỉ thêm vào trong `Spawned()`,
nhưng `GetComponent<PlayerHealth>()` thì **không** bảo đảm gì cả.

**14. Trong `NetworkBehaviour`, chữ `Object` là object MẠNG của Fusion** *(bẫy Fusion — gặp 14/10)*
Viết `Object.Destroy(x)` bên trong một lớp kế thừa `NetworkBehaviour` (kể cả **lớp lồng** bên trong
nó) sẽ báo lỗi biên dịch `CS0120` / `CS0176`: tên `Object` bị hiểu là thuộc tính `Object`
(kiểu `NetworkObject`) của Fusion, không phải `UnityEngine.Object`.
→ Cách chữa: ghi đủ tên `UnityEngine.Object.Destroy(...)`. Gặp ở `EMBarrier.Visual.Dispose()`.

**16. Trong giao diện Unity, object nằm SAU trong danh sách con sẽ CHẶN CÚ BẤM của object nằm trước** *(gặp 16/10)*
Unity vẽ object con theo thứ tự từ trên xuống trong Hierarchy — cái sau đè lên cái trước, và ảnh
nào có `Raycast Target` nằm trên cùng thì nhận cú bấm. Triệu chứng: nút Close của bảng Settings bấm
không ăn, vì khung nền `Panel` (phủ gần hết màn hình) nằm SAU nút trong danh sách và chờm lên nó.
→ Cách chữa: khung nền phải là con ĐẦU TIÊN, các nút nằm sau. Đã sắp lại trong `SettingsPanel.prefab`.
→ Nút "bấm không ăn" thì kiểm thứ tự anh em trước tiên, trước khi nghi code.

**17. Hai vật TRONG SUỐT to bị vẽ sai thứ tự — vật ở xa đè lên vật ở gần** *(gặp 17/10)*
Unity xếp thứ tự vẽ các vật trong suốt theo khoảng cách từ camera tới **tâm** object, không theo từng điểm.
Biển mây là MỘT lưới rộng hàng trăm mét có tâm ở giữa đảo, nên đứng ở giữa map thì "tâm mây" gần hơn tâm
bức tường rào → mây vẽ SAU tường → mây phía bên kia hiện đè lên trước màn chắn.
→ Cách chữa ban đầu: đặt `renderQueue` cố định (tường rào 3050 > mây 3000; đáy biển mây 2990 < mây).
→ **Cách chữa cuối (19/10):** tường rào vẽ **SỚM** (2995) và **GHI CHIỀU SÂU** ở phần đủ đậm (lượt `BarrierDepth`).
Thứ trong suốt nào nằm sau tường đều thua phép thử chiều sâu, bất kể Unity xếp thứ tự ra sao — chữa luôn
cả viền và bảng tên địch lộ xuyên rào. Bài học: với vật trong suốt cần CHE thứ phía sau, xếp thứ tự vẽ
không đủ, phải ghi chiều sâu. Gặp vật trong suốt "nhảy lên trước" bất chợt thì nghi thứ tự vẽ trước tiên.
→ Cùng bẫy trong **hệ hạt** (nổ TNT 17/10): khói (che sáng) vẽ đè lên trước lửa → lỗ tối giữa quả cầu lửa.
Chữa bằng `ParticleSystemRenderer.sortingFudge`: số **nhỏ hơn** = vẽ sau = nằm trên (lửa -20, khói +20).

**15. `line` là từ khoá dành riêng của HLSL — đừng đặt làm tên biến trong shader** *(gặp 14/10)*
Báo lỗi khó hiểu: `syntax error: unexpected token 'line'`. Đó là kiểu dữ liệu của geometry shader.
Các tên khác cũng dính: `point`, `triangle`, `lineadj`, `triangleadj`, `sample`.
Kèm theo: trong khối `Properties`, chữ trong `[Header(...)]` **không được có dấu gạch ngang** —
báo `Parse error ... expecting TVAL_ID`.
