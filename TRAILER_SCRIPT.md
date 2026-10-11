# KỊCH BẢN TRAILER — Magneto-Dome

Tổng **46 giây** (yêu cầu của trường: 30–50 giây).
Dựng bằng CapCut hoặc bất kỳ phần mềm nào — không cần kỹ xảo, chỉ cắt và chèn chữ.

> **Nguyên tắc xuyên suốt:** mỗi cảnh trả lời **một câu hỏi** của người xem, theo đúng
> thứ tự họ sẽ thắc mắc. Không cảnh nào lặp lại ý của cảnh trước.
>
> 1. Đây là đâu? → 2. Đánh nhau bằng gì? → 3. Có gì đặc biệt? → 4. Thua kiểu gì? → 5. Tên game.

---

## 1. Chuẩn bị trước khi quay

| Việc | Vì sao |
|---|---|
| OBS, quay **1920×1080, 60 fps** | 60 fps mới thấy rõ vật bay và cú văng |
| Mở **2 bản game** *(ParrelSync hoặc 2 máy)* | Mọi cảnh chiến đấu đều cần 2 người |
| Bấm **F1 tắt bảng hướng dẫn** ngay khi vào trận | Nó TỰ HIỆN ở pha chuẩn bị, lọt vào phim là hỏng cảnh |
| Quay **mỗi cảnh 3 lần**, mỗi lần dư 2–3 giây đầu và cuối | Dư để cắt; thiếu thì phải vào quay lại từ đầu |
| Tắt mọi thông báo Windows | Pop-up giữa trailer là mất điểm chỉn chu |

---

## 1b. Quay ở đâu — Editor hay bản build?

| Cảnh | Quay ở đâu | Bằng gì |
|---|---|---|
| **Cảnh 1** — toàn đảo | **Unity Editor**, Game view | **Unity Recorder** |
| **Cảnh 2–6** — đánh nhau | **2 bản build** *(có tick Development Build)* | **OBS** |

❌ **Không quay Scene view.** Scene view dùng camera riêng nên **không có hậu xử lý hình ảnh** —
màu nhạt hơn lúc chơi thật, lại có lưới sàn và gizmos. Giờ đã có object `PhotoCamera`
trong `TestScene` thay thế — bật tick cho nó rồi bấm Play, nó tự xoay quanh đảo đều tuyệt đối.

### Vì sao cảnh 1 dùng Unity Recorder chứ không dùng OBS

OBS quay theo **tốc độ thật**: Editor tụt xuống 40 fps thì phim giạt theo. Recorder thì
**giả lập thọi gian** — mỗi khung hình nó bảo Unity "coi như vừa trôi qua 1/60 giây" rồi chờ
vẽ xong mới sang khung sau, bao lâu cũng được. Kết quả luôn là 60 fps mượt, dù máy chạy chậm.

Cảnh xoay quanh đảo vừa là cảnh đắt nhất vừa là cảnh **nặng nhất** *(cả biển mây nằm trong
khung)* — đúng chỗ dễ tụt fps nhất.

Cài: `Window → Package Manager → Unity Registry → gõ "Recorder" → Install`.
Dùng: `Window → General → Recorder → Recorder Window` → thêm **Movie** → source **Game View**
→ 1920×1080 → 60 fps → bấm **START RECORDING** *(nó tự vào Play)*.

### ⚠ Đừng dùng Recorder cho cảnh đánh nhau

Recorder làm **chậm thọi gian thật**, mà Photon Fusion đồng bộ theo thọi gian thật — nó sẽ
tưởng bạn đang lag và bắt đầu giật ngược vị trí nhân vật. Cảnh 1 không có trận nào chạy nên
vô hại; cảnh đánh nhau thì hỏng hình.

Cũng đừng quay cảnh đánh nhau bằng 2 Editor ParrelSync: hai Editor cộng OBS trên một máy là
quá tải, cảnh cần mượt nhất lại thành cảnh giật nhất. Hai bản build nhẹ hơn nhiều, và trong
bản build có tick `Development Build` bạn vẫn có phím **`F8`** để bay camera giữa trận.

### Bật hay tắt HUD?

**Bật cho mọi cảnh gameplay, tắt cho hai cảnh điện ảnh (1 và 7).**

Với game này **HUD chính là bằng chứng**. Tắt HUD thì người xem chỉ thấy *một nhân vật
bị đánh rồi bay đi* — y hệt mọi game có knockback. Bật HUD, họ thấy **thanh điện đầy dần
và hệ số `x1.0 → x3.5` leo lên**. Chữ trên màn hình là *lời tuyên bố*; con số đang nhảy
là *bằng chứng*. Cảnh 4 sống chết dựa vào đó.

HUD còn nói với người xem "đây là gameplay thật, không phải cắt cảnh dàn dựng" —
Valorant, CS2, Rocket League đều giữ HUD trong cảnh gameplay vì vậy.

| Cảnh | HUD |
|---|---|
| 1 — toàn đảo | **TẮT** — không có người chơi nào ở đó, HUD lúc này là của ai? |
| 2, 3, 5, 6 | **BẬT** |
| 4 — Quá Tải | **BẬT** — cảnh HUD quan trọng nhất trailer |
| 7 — logo | **TẮT** |

**Phải tắt bằng mọi giá:** bảng hướng dẫn `F1` *(nó **tự hiện** đầu round, không bấm
gì nó cũng lên)*, bảng **Stats** của Unity, bảng thống kê mạng của Fusion.

Bạn **không có lựa chọn** cho cảnh góc nhìn thứ nhất: không phím nào tắt HUD mà vẫn giữ
camera nhân vật, vì `F8` thay hẳn camera. Nên nó tự chia đôi gọn: camera nhân vật = HUD bật,
`F8` / `PhotoCamera` = bấm `Space` tắt HUD.

> ⚠ **Đừng bật/tắt HUD giữa một cảnh.** Cú nhá đó trông như lỗi game chứ không như dụng ý.

### Mạo cho cảnh 4: đừng quay cận HUD

Trên khung 1080p thanh điện bé xíu, mà bạn cũng không có cách nào zoom camera vào góc
màn hình. **Quay bình thường rồi phóng to lúc dựng**: trong CapCut nhân đôi đoạn clip,
lớp trên scale 250–300% và kéo sao cho thanh điện vào giữa khung, giữ 2 giây.

### Ô phải đặt đúng trước khi bấm Play

| Ô | Đặt thành | Vì sao |
|---|---|---|
| Độ phân giải trên thanh Game view | **1920×1080** cố định | Để "Free Aspect" thì khung hình đổi theo cỡ cửa sổ |
| Nút **Gizmos** trên Game view | Tắt | Không thì lọt mũi tên và biểu tượng vào phim |
| **Stats** | Tắt | Bảng số liệu che góc phải |
| `Shift+Space` | Game view toàn màn hình | Quay được đủ độ phân giải |

---

## 2. Bảng cảnh quay

### Cảnh 1 — Mở màn · 0:00 → 0:05 *(5 giây)*

| | |
|---|---|
| **Quay gì** | Toàn cảnh hòn đảo lơ lửng giữa biển mây, camera trôi chậm sang ngang hoặc tiến tới |
| **Cách quay** | Bật object `PhotoCamera` trong `TestScene`, bấm Play, quay bằng Unity Recorder *(xem mục 1b)* |
| **Chữ hiện** | `MỘT ĐẤU TRƯỜNG LƠ LỬNG` *(hoặc bỏ chữ, để hình tự nói)* |
| **Âm thanh** | Gió + nhạc vào nhẹ |

> Đây là tài sản hình ảnh mạnh nhất của bạn. Đừng cắt nhanh — để người xem kịp thấy
> hòn đảo không có gì bên dưới.

### Cảnh 2 — Không có súng · 0:05 → 0:12 *(7 giây)*

| | |
|---|---|
| **Quay gì** | Góc nhìn thứ nhất: giơ tay, nạp điện cho một tảng đá *(nó sáng đỏ)*, rồi **đẩy** nó bay đi |
| **Cách quay** | Đứng gần đá, bấm `1` chọn cực dương, chuột trái nạp, chuột trái lần nữa để đẩy |
| **Chữ hiện** | `KHÔNG SÚNG` → *(2 giây sau)* → `KHÔNG ĐẠN` |
| **Lưu ý** | Phải thấy rõ **tia điện trên găng tay** và màu đỏ lan vào tảng đá |

### Cảnh 3 — Cả bản đồ là vũ khí · 0:12 → 0:20 *(8 giây)*

| | |
|---|---|
| **Quay gì** | Hai, ba cú bắn liên tiếp: đá bay, cây bay, một cú **hút vật về tay** rồi bắn tiếp |
| **Cách quay** | Cắt ghép 3 đoạn ngắn 2–3 giây, mỗi đoạn một vật khác nhau |
| **Chữ hiện** | `CẢ BẢN ĐỒ LÀ VŨ KHÍ` |
| **Lưu ý** | Xen một cảnh nhìn từ phía người **bị bắn** — thấy vật lao thẳng vào mặt |

### Cảnh 4 — Quá Tải · 0:20 → 0:30 *(10 giây — cảnh quan trọng nhất)*

| | |
|---|---|
| **Quay gì** | Người chơi trúng liên tiếp 3–4 đòn, **quay cận HUD** thấy chỉ số điện tích tăng dần và hệ số `x1.0 → x3.5` |
| | Rồi một cú đấm cuối hất văng họ đi rất xa |
| **Cách quay** | Một người đứng yên chịu đòn, người kia bắn liên tục rồi đấm. Quay từ máy **người bị đánh** |
| **Chữ hiện** | `TRÚNG ĐÒN KHÔNG MẤT MÁU` → `CÀNG NHIỄM ĐIỆN, CÀNG BAY XA` |
| **Lưu ý** | Phải thấy **con số trên HUD đổi** — đây là bằng chứng cơ chế, không phải lời quảng cáo |

### Cảnh 5 — Chỉ chết khi rơi · 0:30 → 0:37 *(7 giây)*

| | |
|---|---|
| **Quay gì** | Một cú đấm hất người chơi khỏi rìa đảo, họ rơi xuyên qua biển mây |
| **Cách quay** | Đứng gần rìa, người kia đấm. Quay từ máy **người bị hất** để thấy cú rơi |
| **Chữ hiện** | `CHỈ CÓ MỘT CÁCH ĐỂ CHẾT` |
| **Lưu ý** | Cảnh đắt nhất của trailer. Quay ít nhất 5 lần, chọn cú bay xa và đẹp nhất |

### Cảnh 6 — Dồn dập · 0:37 → 0:43 *(6 giây)*

| | |
|---|---|
| **Quay gì** | Cắt nhanh 4–5 đoạn, mỗi đoạn ~1 giây: TNT nổ · đấm cận chiến · vòng lửa khu chiếm đóng · cửa hàng · menu vòng tròn chọn đồ |
| **Cách quay** | Quay riêng từng thứ, cắt ngắn khi dựng |
| **Chữ hiện** | không có chữ — để hình dồn dập |
| **Âm thanh** | Nhạc lên cao trào, cắt đúng nhịp trống |

### Cảnh 7 — Kết · 0:43 → 0:46 *(3 giây)*

| | |
|---|---|
| **Quay gì** | Màn hình tĩnh: logo **MAGNETO-DOME** trên nền tối hoặc trên ảnh hòn đảo đã làm mờ |
| **Chữ hiện** | `MAGNETO-DOME` *(lớn)* · `2v2 TRỰC TUYẾN` · dòng nhỏ: tên bạn, lớp, GVHD |
| **Âm thanh** | Nhạc dứt gọn, không fade dài |

---

## 3. Chữ trên màn hình

- **Ngắn**: tối đa 4 chữ một dòng. Người xem đang nhìn hình, không đọc văn.
- **Giữ 2 giây** mỗi câu. Ngắn hơn là không kịp đọc, dài hơn là chán.
- Đặt **giữa dưới** hoặc **giữa trên**, tránh che HUD và tâm ngắm.
- Màu trắng, chữ đậm, có bóng đổ nhẹ để nổi trên mọi nền.
- Font: dùng luôn **Poppins Bold** cho khớp giao diện game.

## 4. Nhạc

Dùng chính bản nhạc trong game *(Pixabay — đã kiểm giấy phép, dùng được)*, hoặc tải thêm
một bản dồn dập hơn **cũng từ Pixabay**. Đừng lấy nhạc trên YouTube hay TikTok: trailer
nộp bài mà dính bản quyền là mất điểm phần khai báo tài nguyên.

Cắt cảnh **đúng nhịp trống**. Đây là thứ phân biệt trailer nghiệp dư với trailer gọn gàng,
mà lại không tốn thêm công gì — chỉ cần nghe nhạc trước rồi đặt điểm cắt theo.

## 5. Thứ tự làm

1. Quay hết **cảnh 2 → 6** trong một buổi *(cần 2 người)*.
2. Quay **cảnh 1** trong Editor bằng `PhotoCamera` + Recorder *(một mình, làm sau cũng được)*.
   ⚠ Xong nhớ **bỏ tick object `PhotoCamera`** — xem `ROLLBACK_TRUOC_KHI_NOP.md`.
3. Dựng: xếp cảnh theo thứ tự trên → chèn nhạc → cắt theo nhịp → thêm chữ → xuất.
4. Xuất **1920×1080, 60 fps, MP4**.

> ⏱ Ước lượng: quay 1 giờ, dựng 1–2 giờ nếu chưa quen CapCut.

## 6. Ba lỗi thường gặp

| Lỗi | Vì sao hỏng |
|---|---|
| Cảnh dài 5–6 giây mỗi cái | Trailer 46 giây chỉ chứa được 7 cảnh. Dài là nhàm |
| Quay nguyên một trận rồi cắt | Cảnh sẽ nhạt. Phải **dàn dựng** từng cú cho đẹp rồi mới quay |
| Để bảng hướng dẫn F1 lọt vào | Nó tự hiện ở pha chuẩn bị. Bấm F1 tắt ngay khi vào trận |
