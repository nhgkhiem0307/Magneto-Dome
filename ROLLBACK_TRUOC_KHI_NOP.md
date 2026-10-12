# ⚠️ KIỂM TRƯỚC KHI NỘP

> **Cập nhật 09/10: ĐÃ HOÀN TÁC XONG phần chế độ chụp ảnh.** Chi tiết bên dưới giữ lại
> để tra cứu nếu sau này dựng cảnh chụp lần nữa.
>
> | Mục | Trạng thái |
> |---|---|
> | Hình ảnh khu chiếm đóng *(vòng lửa)* | ✅ đã bật lại |
> | Canvas HUD | ✅ đã bật lại — **bị tắt lúc chụp ảnh, suýt quên** |
> | 2 tảng đá nạp điện sẵn + đóng băng | ✅ đã xoá thiết lập |
> | 3 tảng đá bị dời chỗ | ✅ đã trả về toạ độ cũ |
> | `fallRespawnDelay` | ✅ đã về 20 trên cả 4 prefab cây *(10/10)* |
> | **Bỏ tick `Development Build`** | ⬜ **CÒN LẠI — làm lúc build bản nộp** |
> | **Tắt object `PhotoCamera` trong TestScene** | ✅ đã xoá khỏi scene 09/10 |
> | **Dọn thư mục build + nén lại zip** | ⬜ **CÒN LẠI — xem mục 7** |

---

## 1. Hình ảnh khu chiếm đóng — ĐANG TẮT

| | |
|---|---|
| Ở đâu | `TestScene` → object có `ControlZone` → ô **Build Visual At Runtime** |
| Đang là | `false` *(tắt)* |
| Phải trả về | **`true`** |
| Tắt ngày | 09/10 |

⚠️ **Đây là thứ dễ quên nhất và hậu quả nặng nhất.** Vòng lửa ở giữa map là **kênh
thông tin duy nhất** cho biết ai đang chiếm khu — nó đổi màu theo đội đang đứng trong đó.
Thiếu nó, người chơi không hiểu vì sao mình thua round, và người chấm không thấy được
cơ chế chiếm khu tồn tại.

Logic chiếm khu vẫn chạy bình thường khi tắt ô này; chỉ phần nhìn biến mất.

---

## 2. Hai tảng đá dựng cảnh chụp — NHỚ TÌM LẠI

| | |
|---|---|
| Ở đâu | `TestScene` → các object đá đã kéo ra giữa sân |
| Ô cần trả | **Start Polarity** → `None` · **Freeze In Air** → tắt |
| Vị trí | Kéo về đúng chỗ cũ, hoặc `Ctrl+Z` nếu chưa lưu |

⚠️ Một tảng đá mang sẵn điện tích từ đầu round là **món quà miễn phí** cho ai đứng gần.
Phá cân bằng mà rất khó phát hiện khi chơi, vì nhìn bề ngoài nó chỉ là một tảng đá sáng màu.

> 💡 Cách tìm nhanh: trong cửa sổ Hierarchy gõ `t:MagneticObject`, rồi bấm từng tảng xem
> ô nào khác `None`. Hoặc bảo tôi quét file scene, tôi liệt kê ra chính xác tảng nào.

---

## 3. Vật rơi khỏi map quay lại sau 5 giây — ✅ ĐÃ TRẢ VỀ 20 *(10/10)*

> 🔍 **Phát hiện lại ngày 10/10 khi đối chiếu tài liệu với prefab.** Trước đó mục này bị
> đánh dấu nhầm là đã xong: mặc định trong **code** đã về `20f` thật, nhưng **giá trị
> Inspector luôn đè lên mặc định code** (xem CLAUDE.md Mục 5 bẫy số 7), mà 4 prefab cây
> vẫn đang lưu số `5`.

| | |
|---|---|
| Ở đâu | `Assets/Resources/Environment/Tree9/Tree9_2.prefab` và `Tree9_3`, `Tree9_4`, `Tree9_5` |
| Trạng thái | ✅ Đã sửa thành `20` trên cả 4 prefab |
| Phải trả về | **`20`** trên cả 4 prefab |
| Đá thì sao | 24 prefab đá **không lưu ô này** nên tự lấy `20` từ code — không phải sửa |

⚠️ Hậu quả nếu bỏ qua: **cây về sau 5 giây, đá về sau 20 giây** — hai loại đạn cùng map mà
luật khác nhau, người chơi không thể đoán được. Mà cây chính là toàn bộ 14 vật loại Normal
trên map, tức hơn một phần ba số đạn.

5 giây là để test cho nhanh. Giữ nguyên thì việc hất đạn của đối thủ xuống vực gần như
mất hết giá trị chiến thuật — đồ về lại ngay.

---

## 4. Phím test F9 / F10 / F12 — TỰ BIẾN MẤT, NHƯNG PHẢI BỎ TICK

| | |
|---|---|
| Phím | `F9` Đỏ thắng round · `F10` Xanh thắng round · `F12` kết thúc trận ngay |
| Điều kiện tồn tại | Chỉ có trong Editor và bản build **có tick Development Build** |
| Phải làm | **Bỏ tick `Development Build`** khi build bản nộp |

Không cần sửa code — bỏ tick là cả ba phím biến mất khỏi file exe.
Phím `K` tự sát cũng cùng cơ chế.

---

## 5. Kiểm tra cuối, theo thứ tự

1. ⬜ Bật lại `Build Visual At Runtime`
2. ⬜ Trả hai tảng đá về `None` và tắt `Freeze In Air`
3. ✅ `fallRespawnDelay` về `20` trên 4 prefab `Tree9_2/3/4/5` *(xong 10/10)*
4. ✅ Tắt object `PhotoCamera` trong `TestScene` *(đã xoá 09/10)*
5. ⬜ Bỏ tick `Development Build`
6. ⬜ Vào trận thử: vòng lửa giữa map **có đổi màu** khi đứng vào không
7. ⬜ Bấm `F12` trong bản build: **không được** có gì xảy ra
8. ⬜ Chơi trọn một trận 2 máy, kết thúc phải về được menu
9. ⬜ Xoá thư mục `*_BurstDebugInformation_DoNotShip` trong bản build mới
10. ⬜ Nén `Build.zip` **lại từ đầu** — file zip cũ là bản có tick Development Build

---

## 6. Object `PhotoCamera` trong TestScene — ✅ ĐÃ XOÁ 09/10

| | |
|---|---|
| Ở đâu | `TestScene` → object tên **`PhotoCamera`** (nằm ở gốc Hierarchy) |
| Trạng thái | **Đã xoá hẳn khỏi scene.** File scene giờ giống hệt lúc commit |
| Thêm ngày | 09/10, để quay cảnh toàn đảo cho trailer |

⚠ **Nếu sau này dựng lại object này thì nhớ xoá trước khi nộp.** Để nó bật mà vào trận thật thì:

| Hại gì | Vì sao |
|---|---|
| **Toàn bộ vật thể đứng im như tượng** | Nó gắn `isKinematic` cho mọi Rigidbody → không bắn được gì |
| **Không thấy gì ngoài camera này** | `depth = 100` nên nó vẽ đè lên camera người chơi |
| **Mất sạch giao diện** | `hideUI` tự tắt mọi Canvas |

Script `Assets/Scripts/MenuUI/PhotoFlyCamera.cs` **vẫn còn** — nó không nằm trong scene nên
không chạy, không hại gì. Cần quay lại thì: `GameObject → Create Empty`, đặt tên `PhotoCamera`,
kéo script vào, đặt Orbit Radius 70 / Orbit Height 26. Mất 10 giây.

⚠ **Nhớ xoá lại trước khi nộp** nếu có dựng lại.

> 💡 Muốn quay phim **trong lúc đang đánh nhau** thì đừng dùng cái này, dùng phím **`F8`**
> (`CinematicCamera`) — nó mượn thông số từ camera người chơi và không đụng vào vật lý.

---

## 6b. Thư mục nộp bài `NOP_BAI/` — đã gom sẵn *(12/10)*

Sáu file theo đúng tên chính thức của trường, đã kiểm không còn dấu vết bài mẫu khoá trước:

| File | Trạng thái |
|---|---|
| BM01 Phiếu đăng ký Project | ✅ đã điền |
| BM02 Đề cương chi tiết | ✅ đã điền, kèm bảng tiến độ 12 tuần |
| BM04 Nhận xét GVHD | ✅ điền sẵn thông tin học viên, phần nhận xét để trống |
| BM05 Nhận xét GV phản biện | ✅ điền sẵn thông tin học viên |
| BM06 Báo cáo tốt nghiệp | ✅ 10/10 ảnh, 0 ô đỏ |
| BM08 Final Project (slide) | ✅ 20 slide |

⬜ **Còn thiếu: BM03 Phiếu theo dõi Project** — chủ project chưa rõ cách làm, chưa đụng tới.

⬜ **Tên GVHD để trống dạng `………`** ở 5 chỗ: BM01, BM02, bìa báo cáo (2 trang), Lời cảm ơn, slide bìa.
Có tên thì tìm chuỗi `………` thay hàng loạt.

⬜ **Hai danh mục trong báo cáo chưa sinh** — xem hướng dẫn ngay trong trang
`DANH MỤC CÁC HÌNH VẼ` và `DANH MỤC CÁC BẢNG`. Sinh xong nhớ `Ctrl+A` rồi `F9` để cập nhật mục lục.

---

## 7. Dọn thư mục build trước khi nén zip

### 7.1 Xoá thư mục `*_BurstDebugInformation_DoNotShip`

Unity tự tạo nó cạnh file `.exe` sau **mọi** lần build. Tên nó ghi thẳng *DoNotShip* —
đây là bản ghi để lập trình viên đọc log sự cố, không phải thứ để phát hành.
Chỉ nặng 1 MB nên không phải chuyện dung lượng — **là chuyện chỉn chu**: nộp bài kèm
một thư mục tên "đừng gửi kèm" thì ngưệi chấm đọc được ngay.

> ✅ Đã xoá khỏi bản build hiện tại ngày 09/10. Nhưng **build lại là nó hiện lại**, phải xoá lại.

### 7.2 Nén `Build.zip` lại từ đầu

File `Build.zip` hiện có là bản **có tick Development Build** và **còn thư mục DoNotShip**.
Đừng nộp file đó. Build lại → xoá DoNotShip → rồi mới nén.

---

## 8. Dung lượng build — để **sau khi nộp** hãy làm

Bản build: **492 MB**, nén zip **244 MB**. Mổ ra:

| Phần | MB | Là gì |
|---|---|---|
| `resources.assets` + `.resS` | **258** | Toàn bộ `Assets/Resources/` — vào hết, dùng hay không |
| `UnityPlayer.dll` | 81 | Engine. Cố định |
| `level1` + `.resS` | 54 | Đồ `TestScene` thật sự chạm tới |
| `Managed/` | 45 | Tất cả code: của mình + Fusion + URP |

**Luật:** Unity chỉ đóng gói asset nào scene trong Build Settings lần tới được.
**Ngoại lệ duy nhất:** thư mục tên `Resources` — vào hết, vì nó tồn tại để `Resources.Load("tên")`
gọi theo tên lúc chạy, mà Unity không đoán trước được sẽ gọi tên nào.

Kết quả quét GUID toàn project ngày 09/10 *(lần theo scene → prefab → material → texture,
đúng cách Unity làm)*:

| Nhóm | Dung lượng nguồn | Ghi chú |
|---|---|---|
| Đang dùng thật | 302 MB | Phải giữ |
| **Không ai nhắc tới, cả project** | **70 MB** *(317 file)* | Xoá an toàn tuyệt đối |
| Chỉ bị đồ không dùng giữ | 153 MB | Phải gỡ cả chuỗi, rủi ro hơn |

70 MB vô chủ gần như toàn bộ là **file `.tif` gốc** trong
`Resources/Environment/Rocks and Boulders 2/Rocks/Source/Textures/` *(60 MB — ảnh gốc
của gói asset, mỗi file 12 MB, không material nào dùng)*, cộng mấy ảnh **mockup thiết kế**
trong `Resources/UI/` và font `Poppins-Light` không dùng.

⚠⚠ **Đừng dọn trước khi nộp.** Xoá asset làm Unity **nhập lại toàn bộ 530 MB**
*(15–30 phút)* và dựng lại thư mục `Library`. Mất cả buổi nếu có gì đó sai, mà **244 MB
nén là cỡ nộp bài bình thưỡng** — chưa phải vấn đề. Giảm được khoảng 30 MB build thôi,
vì `.tif` 12 MB khi vào build bị nén xuống còn vài MB.

Chỉ làm nếu trưỡng có **giới hạn upload** thật chặt. Lúc đó còn một đưỡng an toàn hơn
xoá file: hạ ô **Max Size** của mấy texture nặng nhất ngay trong Unity — đổi được qua lại,
không dịch file nào.
