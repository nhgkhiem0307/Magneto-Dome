# KỊCH BẢN TRAILER — Magneto-Dome *(viết lại 25/10: quay toàn bộ bằng OBS)*

**49,6 giây · 1920×1080 · 60 khung/giây · chữ tiếng Anh · phong cách hype kiểu Valorant / Apex.**
Nhạc: *"Upbeat Rock"* — JonasBlakewood (Pixabay), 130 BPM. Trailer kết đúng lúc bài nhạc tự kết (xem Mục 4).
Trường yêu cầu 30–50 giây.

**Nguồn hình:**
- **17 clip góc nhìn thứ nhất** do chủ project tự quay bằng OBS khi chơi thật: có HUD thật, nhân vật có animation thật.
- **3 cảnh toàn đảo** render sẵn từ trước (camera bay quanh đảo, **không có nhân vật**). Chúng là mở màn,
  nhịp chuyển trước màn logo, và nền màn logo.

> Các cảnh render có nhân vật (đấm, nổ, rơi…) **đã bỏ hết** (25/10). Nhân vật dựng bằng máy không có
> animation nên nhìn giả.

Tài liệu này dùng cho **hai người đọc**:

1. **Chủ project**: đọc **Mục 3** (quay) và **Mục 4** (chọn nhạc).
2. **AI edit** (một AI chạy được lệnh trên máy này, ví dụ Claude Code): đọc **Mục 7**. Mục đó đủ thông tin,
   chép nguyên làm lời giao việc là AI làm được.

> **Đã có sẵn, chạy được ngay:** bộ dựng tự động ở `TrailerBuilder/edit/`. Lệnh
> `py -3 TrailerBuilder/edit/build_trailer.py` ra trailer hoàn chỉnh. Clip chưa quay thì hiện **thẻ giữ chỗ**
> ghi rõ cần quay gì; chưa có nhạc thật thì dùng **nhạc tạm**.

---

## 1. Ý tưởng

Mỗi đoạn trả lời **một câu hỏi** của người xem, đúng thứ tự họ sẽ thắc mắc:

| # | Câu hỏi | Trả lời bằng | Thời gian |
|---|---|---|---|
| 1 | Đây là đâu? | Hòn đảo lơ lửng giữa biển mây | 0 – 5.6s |
| 2 | Đánh nhau bằng gì? | Không súng, chỉ có **găng tay từ tính**. Cả bản đồ là đạn | 5.6 – 15s |
| 3 | Có gì đặc biệt? | **Quá Tải**: không có máu, trúng đòn thì nhiễm điện, nhiễm càng nhiều càng bay xa | 15 – 20.6s |
| 4 | Thua kiểu gì? | **Rơi khỏi đảo** là cách thua duy nhất | 20.6 – 26.3s |
| 5 | Còn gì nữa? | Dồn dập: TNT, Shop, vòng chọn đồ, phòng chờ, chiếm khu, tường chắn, lướt. Rồi tới pha đẹp nhất | 26.3 – 41.3s |
| 6 | Tên game? | Logo MAGNETO DOME | 41.3 – 48s |

**Ngôn ngữ thị giác (giữ nhất quán suốt trailer):**
- **Màu:** đỏ (cực +) và xanh (cực −) là hai màu nhấn chính, giống trong game. Xanh lơ `#6EDCFF` cho viền chữ /
  hiệu ứng điện, vàng cam `#FFCD5A` cho thông tin "nguy hiểm".
- **Chữ:** Poppins (font của giao diện game), in hoa, chỉ 2–4 từ một câu.
- **Nhịp:** mọi cú cắt **và mọi va chạm** rơi **đúng phách nhạc**. Bộ dựng tự lo phần va chạm (xem Mục 3.4).
- **Va chạm = 3 lớp hình cùng lúc:** loé trắng + rung khung + tách màu đỏ/xanh 0,16 giây. Tiếng va chạm là **tiếng thật
  của game** trong clip (không ghép tiếng "boom" tổng hợp — nghe giống tiếng nổ TNT, bỏ 25/10).

---

## 2. Bảng thời gian

> 130 BPM: **1 phách = 0,4615s**, 4 phách = 1,846s. **DROP** (bùng nổ) ở **phách 16 = 7,38s** = giây 94,18 của bài nhạc.
> Trailer dài **107,5 phách = 49,6s**: cú kết thật của bài rơi ở phách 104 (48,0s), hợp âm ngân tới hết video.
> Dữ liệu máy đọc nằm ở `TrailerBuilder/edit/timeline.py`. Bảng dưới là bản cho người đọc.
> Cột **"Va chạm rơi vào"**: bộ dựng tìm tiếng va chạm trong clip rồi đặt nó **đúng phách** này.
> Cột **"Âm thanh thêm"** ghi theo bản 24/10. **Từ 25/10 chỉ còn**: riser nhẹ (phách 8), whoosh ngược (15,2), 3 câu giọng
> ARENA CONTROL, sub drop ở màn logo. Mọi boom / hit / zap / whoosh khác đã bỏ — xem Mục 5.

| Giây | Phách | Hình | Nguồn | Va chạm rơi vào | Chữ | Âm thanh thêm |
|---|---|---|---|---|---|---|
| 0.0 – 5.5 | 0–12 | Toàn cảnh đảo, camera bay vòng hạ dần | `cinematic/s1_island` | — | 1,2s `THE SKY ARENA` (mảnh, hiện mờ) | riser dâng từ 3,7s |
| 5.5 – 6.5 | 12–14 | Bắn tia nạp điện vào đá | `01_charge` (đầu tiên) | phách 12 | `NO GUNS.` **đập** | boom |
| 6.5 – 7.4 | 14–16 | Hút đá lao về tay | `02_pull` (ghi tay giây 2,3) | phách 14.5 | `NO BULLETS.` **đập** | boom · whoosh ngược |
| **7.4 – 9.2** | **16–20** | **DROP**: đẩy đá bay vút | `03_push` (to nhất) | **phách 16** | `JUST MAGNETISM.` **đập**, xanh lơ | **boom + zap** · giọng **"Combat."** |
| 9.2 – 11.1 | 20–24 | Rê tâm ngắm qua các tảng đá, khung phân tích ôm từng tảng | `06_scan` (giữa clip) | — | `THE WHOLE MAP IS AMMO` (1/3 dưới) | whoosh |
| 11.1 – 12.9 | 24–28 | Nạp Heavy → hút về tay → bắn | `04_fire` (phát 1) | phách 26.5 | ↑ | hit |
| 12.9 – 14.8 | 28–32 | Bắn TNT, nổ, hất văng | `05_tnt` (vụ 1) | phách 30 | — | boom |
| 14.8 – 18.5 | 32–40 | **Góc người bị đánh**: trúng đòn, HUD điện tích leo lên | `07_overload` (đòn đầu) | phách 32 | `NO HEALTH BAR` → 16,2s `EVERY HIT CHARGES YOU` (vàng) | zap · hit |
| 18.5 – 20.3 | 40–44 | Đấm đối thủ, văng xa | `08_punch` (cú cuối) | phách 42 | `MORE CHARGE.` → `FURTHER YOU FLY.` (đỏ cam) **đập** | hit + boom |
| 20.3 – 22.2 | 44–48 | Hất đối thủ khỏi mép đảo, nhìn họ rơi | `09_edge` (to nhất) | phách 45 | — | boom + zap |
| 22.2 – 25.8 | 48–56 | **Góc người bị hất**: văng khỏi đảo, rơi xuyên mây | `10_fall` (đòn đầu) | phách 48 | `ONE WAY TO LOSE` → 24,0s `THE EDGE.` **đập** (đỏ cam) | hit · whoosh · boom |
| 25.8 – 35.1 | 56–76 | **DỒN DẬP**, 10 cảnh × 0,92s: đẩy (lần quay 1) · Shop · đấm (cú đầu) · vòng chọn đồ · phòng chờ · chiếm khu · tường chắn · kéo áp sát · hất mép (cú đầu) · đẩy (cú đầu) | xem `timeline.py` | giữa mỗi cảnh | 29,5s `1V1 · 2V2 ONLINE` | boom/hit đúng va chạm |
| 35.1 – 38.8 | 76–84 | **Cao trào**: chọn can xăng → cây hoá TNT → bắn → **nổ (slow-mo nhẹ)** | `17_best` · **chưa quay → tạm dùng `18_gasoline`** | phách 82 | — | giọng **"Match point."** · riser · **boom** 37,8s |
| 38.8 – 40.6 | 84–88 | Camera tiến về phía đảo | `cinematic/s1_pushin` | — | — | whoosh ngược |
| **40.6 – 49.6** | 88–107.5 | **MÀN LOGO CÓ MÀU** *(25/10)*: nền xanh đậm + quầng **đỏ trái / xanh phải** (hai cực) + lưới lục giác, vệt sáng ngang + vòng sóng xung kích lúc logo đập vào, **đường sức từ** đỏ→xanh nối cực **+** và **−**, MAGNETO có quầng đỏ/xanh lệch hai bên | `cinematic/s7_bg` | — | `MAGNETO` đập → `DOME` (đỏ→xanh) → `NO GUNS. JUST MAGNETISM.` + `1V1 · 2V2 ONLINE` → tên / lớp / GVHD → `MADE WITH UNITY 6 · PHOTON FUSION 2`. Hình + chữ tối dần 1,6s cuối | **hit + sub drop** · giọng **"Victory."** · cú kết của bài ở 48,0s (loé + boom nhẹ), hợp âm ngân tới hết |

---|---|---|---|---|---|---|
| 0.0 – 5.6 | 0–12 | Toàn cảnh đảo, camera bay vòng hạ dần | `cinematic/s1_island` | — | 1,2s `THE SKY ARENA` (mảnh, hiện mờ) | riser dâng từ 3,75s |
| 5.6 – 6.6 | 12–14 | Bắn tia nạp điện vào đá | `01_charge` (đầu tiên) | phách 12 | `NO GUNS.` **đập** | boom |
| 6.6 – 7.5 | 14–16 | Hút đá bay về tay | `02_pull` (đầu tiên) | phách 14 | `NO BULLETS.` **đập** | boom · whoosh ngược |
| **7.5 – 9.4** | **16–20** | **DROP**: đẩy đá bay vút | `03_push` (to nhất) | **phách 16** | `JUST MAGNETISM.` **đập**, xanh lơ | **boom + zap** · giọng **"Combat."** |
| 9.4 – 11.3 | 20–24 | Rê tâm ngắm qua các tảng đá, khung phân tích ôm từng tảng | `06_scan` (giữa clip) | — | `THE WHOLE MAP IS AMMO` (1/3 dưới) | whoosh |
| 11.3 – 13.1 | 24–28 | Đổi cực → bắn đá trúng đối thủ | `04_fire` (phát 1) | phách 26.5 | ↑ | hit |
| 13.1 – 15.0 | 28–32 | Bắn TNT, nổ, hất văng | `05_tnt` (vụ 1) | phách 30 | — | boom |
| 15.0 – 18.8 | 32–40 | **Góc người bị đánh**: HUD điện tích leo lên, hiện x2.x, x3.x | `07_overload` (đòn đầu) | phách 32 | `NO HEALTH BAR` → 16,4s `EVERY HIT CHARGES YOU` (vàng) | zap · hit |
| 18.8 – 20.6 | 40–44 | Đấm đối thủ nhiễm nhiều điện, văng xa | `08_punch` (cú cuối) | phách 42 | `MORE CHARGE.` → `FURTHER YOU FLY.` (đỏ cam) **đập** | hit + boom |
| 20.6 – 22.5 | 44–48 | Hất đối thủ khỏi mép đảo, nhìn họ rơi | `09_edge` (to nhất) | phách 45 | — | boom + zap |
| 22.5 – 26.3 | 48–56 | **Góc người bị hất**: văng khỏi đảo, rơi xuyên mây, màn hình chết | `10_fall` (đòn đầu) | phách 48 | `ONE WAY TO LOSE` → 24,4s `THE EDGE.` **đập** (đỏ cam) | hit · whoosh · boom |
| 26.3 – 35.6 | 56–76 | **DỒN DẬP**, 10 cảnh × 0,94s: TNT (vụ cuối) · Shop · đấm (cú đầu) · vòng chọn đồ · phòng chờ · chiếm khu · tường chắn · kéo áp sát · bắn (phát cuối) · đẩy (cú cuối) | xem `timeline.py` | giữa mỗi cảnh | 30s `1V1 · 2V2 ONLINE` | boom/hit đúng va chạm |
| 35.6 – 39.4 | 76–84 | **Pha đẹp nhất** | `17_best` (to nhất) | phách 79 | — | giọng **"Match point."** · riser · **boom** 37,0s |
| 39.4 – 41.3 | 84–88 | Camera tiến về phía đảo | `cinematic/s1_pushin` | — | — | whoosh ngược |
| **41.3 – 48.0** | 88–102 | **MÀN LOGO** trên nền đảo làm mờ | `cinematic/s7_bg` | — | `MAGNETO` đập → `DOME` (đỏ→xanh) → `NO GUNS. JUST MAGNETISM.` + `1V1 · 2V2 ONLINE` → tên / lớp / GVHD → `MADE WITH UNITY 6 · PHOTON FUSION 2` | **hit + sub drop** · giọng **"Victory."** · nhạc dứt gọn |

---

## 3. QUAY BẰNG OBS *(chủ project làm)*

### 3.1. Hai bản game trên một máy

Màn hình máy này là **2560×1440**, nên hai cửa sổ game 1920×1080 đặt chồng lên nhau vẫn vừa.
**Không cần ParrelSync**: chạy bản build hai lần là được (game không chặn mở hai bản).

1. Mở `Build_Nop\Magneto-Dome.exe`. Vào **Settings**:
   - Tắt **Fullscreen**, chọn độ phân giải **1920×1080**.
   - **Music = 0** (nhạc nền game lẫn vào sẽ làm bộ dựng dò sai va chạm). **SFX = 100%**.
   - **Độ nhạy chuột** hạ còn ~70% mức quen dùng: xoay chậm và đều thì nhìn điện ảnh hơn.

   Hai bản game **dùng chung cài đặt**, nên chỉnh một lần là cả hai ăn theo.
2. Mở `Magneto-Dome.exe` **lần thứ hai**. Gọi bản đầu là **máy A** (bạn điều khiển chính), bản sau là **máy B** (đối thủ).
3. Máy A: **Create Custom Room** → bấm **1V1** → **COPY** mã phòng. Máy B: dán mã vào ô **Enter Room ID** → **Join**.
   Máy A bấm **Start**.
4. Trong trận, ở cả hai máy: **F1** tắt bảng hướng dẫn, **F4** tắt ô ping.
5. Chuyển qua lại giữa hai máy bằng **Alt+Tab** hoặc bấm vào cửa sổ. Máy nào không được chọn thì nhân vật đứng yên
   (game vẫn chạy nền), làm **đối thủ đứng làm bia** rất tiện.

### 3.2. Cài đặt OBS (làm một lần)

| Mục | Giá trị |
|---|---|
| Settings → Video | Base và Output **1920×1080**, FPS **60** |
| Settings → Output → Recording | Định dạng **mp4** hoặc **mkv** (bộ dựng nhận cả hai), bộ mã hoá phần cứng **AMD (AMF)**, chất lượng cao (CQP 16–18) |
| Settings → Audio | Thu **Desktop Audio**. **Tắt micro** |
| Nguồn hình | Tạo **2 Scene**: `MAY_A` chứa **Window Capture** cửa sổ máy A, `MAY_B` chứa Window Capture cửa sổ máy B. Ô **Capture Method** chọn **Windows 10 (1903 and up)** |

> Window Capture kiểu "Windows 10" **vẫn quay được cửa sổ đang bị cửa sổ khác che**. Đây là mẹo quay
> **góc nhìn người bị đánh** chỉ với một người: OBS để scene `MAY_B`, còn bạn chơi trên máy A. Xem cột "Quay
> máy nào" bên dưới.
>
> ⚠ Không được **thu nhỏ (minimize)** cửa sổ đang quay, nó sẽ đen. Che bằng cửa sổ khác thì không sao.

### 3.3. Danh sách 17 clip

Đặt **đúng tên** (đuôi `.mp4` hoặc `.mkv` đều được) vào `D:\Bt vtc\Capstone Project\Trailer_Footage\obs\`.

**Không cần quay đúng thời lượng, cũng không cần tự cắt.** Cột "Quay ít nhất" chỉ là mức tối thiểu để đủ
hình. Dài hơn bao nhiêu cũng được, bộ dựng (hoặc AI edit) tự cắt đúng khúc cần. Trailer chỉ dùng **khoảng 1–4 giây**
mỗi clip.

| Tên file | Quay ít nhất | Quay máy nào | Làm gì | Va chạm cần có | Mẹo cho đẹp |
|---|---|---|---|---|---|
| `01_charge` | 4s | A | Đứng cách tảng đá **chưa có điện** ~8m, **chuột trái**: tia laze bắn ra, đá hiện viền màu | 1 | Đá ở giữa khung, trời phía sau |
| `02_pull` | 4s | A | Đá **trái dấu** với găng: **giữ chuột trái**, đá bay về tay | 1 | Chọn tảng ở xa ~15m cho thấy đường bay |
| `03_push` | 10s | A | Đá **cùng dấu**: **chuột trái** đẩy đá bay vút ra phía vực. Làm **2–3 lần** trong clip | 2–3 | **Cảnh DROP**, đẹp nhất. Lần mạnh nhất được dùng ở drop, lần cuối dùng ở phần dồn dập |
| `04_fire` | 10s | A | Cầm đá → bấm `1`/`2` đổi cực (vòng tâm ngắm đổi màu) → **chuột phải** bắn **trúng máy B**. **2 phát** | 2 | Máy B đứng cách ~10m, bắn trúng người. Phát 1 dùng ở phách 26.5, phát 2 ở phần dồn dập |
| `05_tnt` | 10s | A | Bắn TNT (thùng đỏ) vào máy B hoặc đất cạnh B: nổ, B văng. **2 vụ nổ** | 2 | Đứng xa ≥8m kẻo tự dính nổ |
| `06_scan` | 6s | A | **Không bấm gì**, rê tâm ngắm thật chậm qua 3–4 tảng đá: khung phân tích hiện, ôm từng tảng | — | Lấy phần giữa clip. Quay 6–8s, rê đều tay |
| `07_overload` | 10s | **B** *(chơi trên A)* | OBS để scene **`MAY_B`**. Chỉnh máy B nhìn về phía A trước, rồi qua máy A **đánh B 4–5 đòn liên tiếp** (bắn đá, đấm). Trên clip: HUD điện tích của B leo **0 → 70–90%**, hiện **x2.x, x3.x** | 4–5 | **Cảnh quan trọng nhất về cơ chế**, số trên HUD phải đọc rõ. Dùng đá to cho điện tăng nhanh |
| `08_punch` | 8s | A | Đứng sát máy B (đã nhiễm nhiều điện), tay trống, **chuột phải** đấm. **2–3 cú**, cú cuối cho B văng xa nhất | 2–3 | Quay ngay sau `07_overload`, lúc B còn nhiều điện |
| `09_edge` | 6s | A | Máy B đứng **sát mép đảo**. Đấm/bắn văng B ra ngoài, **nhìn theo B rơi xuống mây** | 1 | Phải đang **pha chiến đấu** (rơi ở pha mua đồ không chết). Giữ camera theo B 2–3 giây |
| `10_fall` | 8s | **B** *(chơi trên A)* | OBS để scene **`MAY_B`**. Máy B đứng mép đảo, **quay lưng ra vực, nhìn vào trong**. Qua máy A hất B khỏi đảo. Trên clip: văng ra, rơi xuyên biển mây, màn hình chết hiện ra | 1 | Quay luôn cùng lúc với `09_edge` được: dùng **Source Record** hoặc quay hai lần |
| `11_shop` | 4s | A | **Pha mua đồ** đầu round: **B** mở Shop, rê chuột qua các ô, mua **Nước tăng lực** + **EM Barrier Core** | — | Đầu trận có $300, vừa đủ hai món. Pha mua đồ chỉ 15 giây, quay luôn từ lúc vào round |
| `12_radial` | 4s | A | **Giữ Tab** mở vòng chọn đồ, rê chọn Nước tăng lực, thả: màn hình loé | — | Mua ở `11_shop` rồi |
| `13_moves` | 8s | A | Lướt **Q** 1–2 lần, rồi máy B đứng xa 20–40m (**trái dấu** với găng mình): **chuột phải** kéo áp sát | 2+ | Cú cuối (kéo áp sát) dùng ở phần dồn dập |
| `14_zone` | 4s | A | Đứng trong khu chiếm giữa bản đồ: HUD hiện **CAPTURING**, thanh tiến độ chạy | — | Nhìn ra xa, thấy viền khu chiếm |
| `15_barrier` | 4s | A | Dùng **EM Barrier Core** (từ vòng chọn đồ): tường lục giác dựng lên trước mặt | 1 | *Tuỳ chọn*: thiếu thì bộ dựng dùng tạm `13_moves` |
| `16_lobby` | 4s | A (màn hình menu) | Phòng chờ: hai thẻ người chơi, chủ phòng bấm **1V1 / 2V2** qua lại | — | *Tuỳ chọn*: thiếu thì dùng tạm cảnh đảo. Quay **trước** khi bấm Start |
| `17_best` | 1–2 phút | A | **Đánh tự do 1–2 phút**, cố tạo một pha thật đẹp: TNT hất văng, hất khỏi đảo, combo hút–đổi cực–bắn | nhiều | Bộ dựng tự lấy **tiếng to nhất** trong cả clip. Muốn chọn pha khác thì báo AI giây đó |

### 3.4. Quay thoải mái, không cần cắt chuẩn

Bộ dựng **không dùng cả clip**, chỉ lấy một khúc đúng bằng ô của cảnh đó. Nó tự xử lý mọi kiểu clip:

| Clip của bạn | Bộ dựng làm gì |
|---|---|
| **Dư đầu dư đuôi** (bấm quay sớm, tắt muộn) | Bỏ qua phần thừa, chỉ cắt khúc cần |
| Không biết hành động ở giây thứ mấy | **Tự dò tiếng va chạm** trong tiếng game (tia laze, đấm, nổ, lướt…) rồi đặt nó **đúng phách nhạc** |
| Có nhiều hành động trong một clip | Chọn được cái **đầu / cuối / to nhất / thứ 2…** Nhờ vậy một clip dùng được ở hai chỗ khác nhau |
| **Ngắn hơn ô** | **Tự làm chậm** cho vừa (tối đa chậm 2 lần). Vẫn thiếu thì giữ hình ở khung đầu/cuối và in cảnh báo |
| Cảnh không có va chạm (Shop, scan, phòng chờ) | Lấy khúc **ở giữa** clip |

Chỉ cần nhớ ba điều:
1. **Quay dư ra ~2 giây trước và sau** hành động. Dư thì không sao, thiếu mới phải làm chậm.
2. **Nhạc game = 0** khi quay (Mục 3.1). Còn nhạc thì bộ dựng có thể nhận nhầm tiếng trống là va chạm.
3. Mỗi cảnh quay **3–5 lần**, giữ lần đẹp nhất, đặt đúng tên. Tốc độ rê chuột **chậm, mượt, có chủ đích**.

---

### 3.5. Clip đã quay *(25/10)*

18 file OBS, **giữ nguyên tên gốc** (`Magneto-Dome 2026-10-25 hh-mm-ss.mp4`). Bảng `CLIPS` trong `timeline.py` nối tên cảnh
(`01_charge`…) với file thật, gán theo **thứ tự giờ quay** (file 1 và 2 đảo cho nhau) + đối chiếu nội dung:

- 16 cảnh trong danh sách đều có. `03_push` quay 2 lần → dùng cả hai (lần 2 cho drop, lần 1 cho phần dồn dập).
- **Thêm `18_gasoline`**: chọn can xăng, cây hoá TNT, bắn, nổ. Ngoài danh sách nhưng rất đẹp → đang dùng **thay `17_best`**.
- **`17_best` chưa quay**. Bỏ qua được: cảnh cao trào tự dùng `18_gasoline` (ô `alt`). Quay sau thì đặt file vào `obs/`,
  thêm dòng `'17_best': ('<tên file>', 0, <giây cuối dùng được>)` vào `CLIPS`, đổi tiếng boom/hit ở phách 82 về 79.
- **Cuối mỗi clip có bảng Settings** (bấm Esc dừng quay). Bảng `CLIPS` ghi giây cuối dùng được cho từng clip; bộ dựng
  không bao giờ lấy hình / tiếng ngoài khoảng đó. Lần sau dừng quay bằng phím tắt của OBS thì khỏi phải cắt.
- Clip 4 cảnh đầu (17:06–17:11) quay lúc game còn **lỗi mờ 720p** (đã sửa ở bản build 18:18) — hơi nhoè hơn các clip
  sau. Muốn đẹp đều thì quay lại `01_charge`, `02_pull`, `03_push` bằng bản build mới.

## 4. NHẠC *(chủ project tải)*

Chỉ lấy ở **Pixabay Music** (pixabay.com/music): miễn phí, dùng được trong sản phẩm, **không dính bản quyền**.
**Không** lấy nhạc YouTube / TikTok / Spotify.

**Tiêu chí chọn bài:**
- **124–140 BPM** (128 là đẹp nhất: trailer dài đúng 48 giây).
- Có **đoạn mở nhẹ** rồi **"drop"** (bùng trống mạnh) rõ ràng, drop đến trong khoảng giây 6–30 của bài.
- Sau drop còn **ít nhất 35 giây** sôi động.
- Thể loại: electronic / dubstep nhẹ / trailer hybrid / cyberpunk. Tránh nhạc có lời hát.

**Từ khoá tìm trên Pixabay:** `epic trailer electronic`, `action gaming`, `cyberpunk trailer`, `hybrid trailer`,
`dubstep gaming`, `energetic electronic drop`.

**Đặt file:** `D:\Bt vtc\Capstone Project\Trailer_Footage\music\` (chỉ để **1 file** trong đó).
Ghi tên bài + tác giả vào `CREDITS.md` mục 4 (Pixabay).

> Chưa có nhạc thật thì bộ dựng dùng **nhạc tạm** `TrailerBuilder/edit/sfx/temp_beat.wav` (trống 128 BPM tự tạo).

### Bài đã chọn *(25/10)*

*"Upbeat Rock"* — **JonasBlakewood**, Pixabay mã 573489 (`Trailer_Footage/music/`). Đã khai ở `CREDITS.md` mục 4.
Dòng ghi nguồn đặt ở **mô tả video** khi đăng: *Music by JonasBlakewood from Pixabay* (bản có link: xem `CREDITS.md`).

| Đo được | Giá trị |
|---|---|
| Nhịp | **130 BPM** (`beats.py` ước 130,73 — sai 0,6%, đủ làm trôi 0,25s ở cuối; đo lại trên đoạn 60–134s ra 129,99) |
| Khoảng lặng → DROP | lặng ở giây 90,5, **bùng lại ở 94,18** → `MUSIC_OFFSET = 86,794` (drop = phách 16) |
| Cú kết thật của bài | giây **134,8** = phách 104; hợp âm ngân tới ~136,5s rồi tắt |

### Quy tắc đoạn kết: KHÔNG để nhạc bị cắt giữa chừng

Cuối video phải trùng **chỗ bài nhạc tự kết** (hoặc cuối một câu nhạc), không được dừng ngang giữa câu. Hai cách, dùng
riêng hoặc kết hợp:

1. **Kéo dài / rút ngắn màn logo**: sửa `len` của cảnh cuối (`cinematic/s7_bg`) và các dòng chữ `logo_*` / `end_*` trong
   `timeline.py` cho cùng kết thúc. Trailer vẫn phải ≤ 50 giây.
2. **Tua nhạc nhanh / chậm, tối đa ±12,5%**: ô `MUSIC_SPEED` (0.875 – 1.125) trong `timeline.py`. Giữ nguyên cao độ
   (`atempo`). Nhịp hiệu dụng = BPM × MUSIC_SPEED, mọi mốc phách tự co giãn theo, nên tổng thời lượng đổi theo.

Bài hiện tại dùng cách 1: màn logo dài 19,5 phách (thay vì 14,4), video kết lúc hợp âm cuối ngân gần hết, `MUSIC_SPEED = 1.0`.

---

## 5. Âm thanh — ít là đẹp *(sửa 25/10)*

> Bản đầu rải 12 tiếng **boom** tổng hợp + 11 whoosh + 4 zap + 9 hit lên trên tiếng game và nhạc rock. Chủ project nghe:
> *"âm thanh hơi sucks, có tiếng giống TNT"* — boom tổng hợp trùng màu với tiếng nổ TNT trong game, chồng nhiều lớp thì rối
> và giả. Đã bỏ gần hết. **Nguyên tắc: tiếng va chạm lấy từ chính game, tiếng tổng hợp chỉ để nối, càng ít càng tốt.**

| Lớp | Nguồn | Mức | Ghi chú |
|---|---|---|---|
| Nhạc | `Trailer_Footage/music/` | 0.85 | **Tự hạ 40%** khi giọng ARENA CONTROL nói (ducking), lên xuống mềm 80ms |
| Tiếng gốc clip OBS | chính file OBS | 0.3–0.6 | Laze, đấm, nổ, lướt **thật của game**. Tự rơi đúng phách vì hình được cắt theo va chạm. Mờ dần 15ms hai đầu mỗi đoạn (không kêu "tách" lúc cắt cảnh). Clip làm chậm thì tiếng chậm theo, giữ cao độ |
| Tiếng tổng hợp (`sfx.py`) | `TrailerBuilder/edit/sfx/` | 0.45–0.7 | **Chỉ 3 chỗ**: `riser` dâng vào drop · `whoosh_rev` ngay trước drop · `subdrop` lúc hiện logo. **Tránh `boom.wav`** |
| Giọng ARENA CONTROL | `Assets/Resources/Audio/Voice/` | 1.0 | **"Combat."** ở drop · **"Match point."** trước cao trào · **"Victory."** ở màn logo |

Cuối cùng: nén mềm toàn bộ (`tanh`), đỉnh ≈ −0,6 dB. Video kết đúng lúc bài nhạc tự kết (Mục 4).

---|---|---|---|
| Nhạc | `Trailer_Footage/music/` | 0.85 | **Tự hạ 40%** khi giọng ARENA CONTROL nói (ducking), lên xuống mềm 80ms |
| Tiếng gốc clip OBS | chính file OBS | 0.3–0.6 | Tiếng thật của game: laze, đấm, nổ, lướt. **Tự rơi đúng phách** vì hình được cắt theo va chạm. Clip bị làm chậm thì tiếng cũng chậm theo, giữ nguyên cao độ |
| Tiếng trailer | `TrailerBuilder/edit/sfx/`, **tự tổng hợp bằng code** (`sfx.py`) | 0.5–1.0 | `boom` (chữ đập, va chạm lớn), `hit` (trúng đòn), `whoosh` / `whoosh_rev` (chuyển cảnh), `riser` (dâng trước drop), `zap` (điện), `subdrop` (màn logo). Đặt đúng phách va chạm |
| Giọng ARENA CONTROL | `Assets/Resources/Audio/Voice/` | 1.0 | **"Combat."** ở drop · **"Match point."** trước cao trào · **"Victory."** ở màn logo |

Cuối cùng: nén mềm toàn bộ (`tanh`), đỉnh ≈ −0,6 dB. Nhạc **dứt gọn** ở 48s (tắt dần 0,6s), không kéo dài.

---

## 6. Từ điển *(dùng trong `timeline.py`)*

### Lấy đoạn nào của clip

| Ô | Ví dụ | Nghĩa |
|---|---|---|
| `pick` | `'first'` `'last'` `'loudest'` `'hit2'` | Tự dò tiếng va chạm: cái đầu / cuối / to nhất / thứ 2… |
| | `'mid'` *(mặc định)* | Lấy quanh giữa clip (cảnh không có va chạm) |
| `hit` | `2.5` | Va chạm rơi vào phách thứ 2,5 **của cảnh** (0 = ngay đầu cảnh) |
| `src_t` | `12.4` | Ghi tay: khoảnh khắc ở giây 12,4 của clip. Thắng `pick`, dùng khi tự dò chọn sai |
| `nudge` | `-0.15` | Dời khoảnh khắc sớm hơn 0,15s (tiếng va chạm hơi lệch hình) |
| `ss` | `0.6` | Lấy đúng từ giây này (cảnh render sẵn) |
| `speed` | `0.5` | Phát chậm một nửa (slow-motion) |
| `alt` | `dict(src=…, …)` | Cảnh tuỳ chọn: thiếu file thì dùng cảnh này thay |

### Hiệu ứng (`fx`)

> ⚠ **Chữ không được phóng to / thu nhỏ CHẬM liên tục** (sửa 25/10 — "nhiều chữ rung rung, nhìn giật"). ffmpeg co giãn
> và đặt lớp chữ theo điểm ảnh nguyên, nên phóng chậm làm viền chữ nhảy từng nấc 2px. Chữ chỉ chuyển động NHANH lúc
> xuất hiện rồi đứng yên; muốn chữ "sống" thì dùng `sway=True`.

Số sau `@` là giây tính từ đầu cảnh. **`@hit`** = đúng khung va chạm (tự theo `pick` / `hit`).

| Hiệu ứng | Viết | Trông thế nào | Dùng khi |
|---|---|---|---|
| Hiện / tối dần | `fadein:0.6` `fadeout:0.7` | Từ đen hiện lên / về đen | Mở màn, kết thúc |
| Ken Burns | `zoom:1.0>1.08` | Khung phóng to rất chậm suốt cảnh | Cảnh tĩnh, tạo cảm giác "đang tiến tới" |
| Cú đập | `punch` | Đầu cảnh phóng 112% rồi thu về 100% trong 0,22s | Cú cắt trên phách mạnh |
| Loé trắng | `flash@hit` | Sáng vọt rồi tắt dần (~0,25s) | Va chạm / nổ / đầu cảnh drop |
| Rung khung | `shake@hit:24` | Lắc ngang dọc, tắt dần trong ~0,4s | Va chạm mạnh (10 nhẹ → 34 rất mạnh) |
| Tách màu | `rgb@hit` | Kênh đỏ lệch trái, xanh lệch phải 0,16s | "Nhiễu điện từ", đi kèm loé ở va chạm lớn |
| Mờ / tối | `blur:16` `dark:0.38` | Nền mờ tối | Nền màn logo |
| Tiếng gốc | `clipaudio:0.5` | Giữ tiếng game của clip ở mức 0.5 | Mọi cảnh OBS |
| Chữ `title` | — | Chữ mảnh giãn rộng, hiện mờ 0,9s, trôi lên nhẹ rồi **đứng yên** | Mở màn |
| Đung đưa `sway=True` | ô trong `TEXTS` | Lớp lên xuống 5px, 4 giây/nhịp, **mượt dưới 1 điểm ảnh** (Pillow dịch từng khung). Các lớp sway đi cùng một nhịp | Chỉ cụm logo màn kết |
| Chữ `slam` | — | Chữ Black 150px **đập** (135%→100% trong 0,13s), viền đỏ/xanh lệch, quầng sáng | Khẩu hiệu ngắn, đúng phách |
| Chữ `lower` | — | Chữ đậm ở 1/3 dưới, trượt vào từ trái, vạch xanh lơ + nền tối mờ | Câu giải thích (không che giữa khung) |
| Lớp màn logo `end_bg` | — | Nền xanh đậm + quầng đỏ (trái) / xanh (phải) + lưới lục giác + tối góc, phủ alpha lên nền trời | Màn logo — để logo nổi, không chìm vào nền nhạt |
| Lớp `end_fieldlines` | — | Vòng cung đường sức từ đổi màu đỏ→xanh nối hai cực, dấu **+** / **−** vẽ bằng thanh (Poppins không có ký tự `−`) | Màn logo, sau chữ |
| Lớp `end_ring` / `end_streak` | — | Vòng sóng xung kích phóng 0,5→2,6 lần trong 0,55s / vệt sáng ngang kiểu anamorphic | Đúng lúc logo đập vào |

---

## 7. GIAO VIỆC CHO AI EDIT *(chép nguyên mục này)*

> **Bối cảnh:** Bạn dựng trailer 48 giây cho game đồ án "Magneto-Dome" (FPS đối kháng, găng tay từ tính, đảo lơ
> lửng). Máy Windows, project ở `D:\Bt vtc\Capstone Project\Magneto-Dome\`. Quy trình dựng **đã được tự động
> hoá**. Việc của bạn: **kiểm nguyên liệu, chạy, soát bằng mắt, tinh chỉnh `timeline.py`**. Không dựng lại từ
> đầu, không đổi phong cách.
>
> **File quan trọng:**
> - `TRAILER_SCRIPT.md`: kịch bản. Mục 2 là bảng thời gian, Mục 3.3 là danh sách clip, Mục 6 là từ điển ô/hiệu ứng.
> - `TrailerBuilder/edit/timeline.py`: **file DUY NHẤT bạn được sửa** để chỉnh trailer (cảnh, chữ, âm thanh,
>   nhạc). Đơn vị thời gian là **phách**.
> - `TrailerBuilder/edit/build_trailer.py`: bộ dựng (ffmpeg + numpy + Pillow). Chỉ sửa khi cần thêm loại hiệu ứng mới.
> - `TrailerBuilder/edit/beats.py`: dò BPM + chỗ drop của bài nhạc.
> - Nguyên liệu (ngoài repo) ở `D:\Bt vtc\Capstone Project\Trailer_Footage\`: `obs\` (clip người dùng quay, giữ nguyên
>   tên OBS gốc — bảng `CLIPS` trong `timeline.py` nối tên cảnh với file và **giây dùng được**, xem Mục 3.5),
>   `cinematic\` (3 cảnh đảo render sẵn, **không sửa, không xoá**), `music\` (1 file nhạc).
> - Clip mới thêm vào thì phải thêm dòng vào `CLIPS`, kèm giây cuối dùng được (bỏ đoạn bảng Settings / màn hình thừa).
> - Python: lệnh `py -3`. Thư viện `numpy pillow imageio-ffmpeg` đã cài. ffmpeg nằm trong `imageio_ffmpeg`.
>
> **Quy trình:**
> 1. Liệt kê `Trailer_Footage\obs\`, đối chiếu tên với Mục 3.3. Thiếu file nào thì báo người dùng (vẫn dựng được,
>    cảnh đó hiện thẻ giữ chỗ; `15_barrier` và `16_lobby` là tuỳ chọn, thiếu thì tự dùng cảnh thay).
> 2. Có nhạc thì chạy `py -3 TrailerBuilder/edit/beats.py "<file nhạc>"`, chép 3 dòng `BPM` / `MUSIC_FILE` /
>    `MUSIC_OFFSET` nó in ra vào đầu `timeline.py`. Báo dài quá 50s hoặc OFFSET âm thì báo người dùng chọn bài khác.
>    ⚠ BPM của `beats.py` chỉ là ước lượng (bài hiện tại: ước 130,73, thật 130,0) — kiểm lại trên đoạn dài, sai 0,5% là
>    trôi ~0,25s ở cuối. Rồi xử lý **đoạn kết** theo quy tắc Mục 4: video phải kết ở chỗ bài nhạc tự kết, không cắt giữa câu.
> 3. Chạy `py -3 TrailerBuilder/edit/build_trailer.py --check` (~20 giây, không dựng video). Nó in **bảng mỗi
>    cảnh lấy đoạn nào của clip nào**, kèm danh sách va chạm dò được, và ghi ảnh
>    `Trailer_Footage\_work\check.jpg`: khung hình **đúng khoảnh khắc** chọn cho từng cảnh. **Mở ảnh đó ra xem**:
>    - Khung có đúng hành động mô tả trong Mục 3.3 không (đá vừa bay đi, cú đấm vừa trúng, vụ nổ vừa bùng)?
>    - Sai thì sửa cảnh đó trong `timeline.py` theo thứ tự ưu tiên: đổi `pick` (vd `'loudest'` → `'hit2'`) → nếu vẫn
>      sai, trích vài khung quanh các mốc va chạm in trong bảng
>      (`ffmpeg -ss <giây> -i <clip> -vf fps=10,scale=480:-1,tile=5x2 -frames:v 1 x.jpg`) để tìm giây đúng, ghi vào
>      `src_t` → lệch vài phần giây thì dùng `nudge`.
>    - Dòng `⚠ TRÙNG HÌNH`: hai cảnh lấy cùng một đoạn clip. Đổi `pick` của một cảnh, hoặc báo người dùng quay thêm.
>    - Dòng `⚠ clip quá ngắn`: chấp nhận được nếu chỉ thiếu ít; nhiều thì báo người dùng quay dư ra.
>    - Chạy lại `--check` tới khi ổn.
> 4. Chạy bản xem nhanh `py -3 TrailerBuilder/edit/build_trailer.py --fast` (~1 phút) → soát (bước 6).
> 5. Ổn thì chạy bản thật `py -3 TrailerBuilder/edit/build_trailer.py` (~4–6 phút) → ra
>    `Trailer_Footage\MagnetoDome_Trailer.mp4` (không còn đuôi `_DRAFT` khi đủ clip + có nhạc thật).
> 6. **Soát bằng mắt:** trích ~20 khung ở các mốc trong bảng Mục 2 (`ffmpeg -ss <giây> -i <file> -frames:v 1`),
>    ghép thành ảnh tổng. Xem:
>    - Chữ không che hành động chính, không tràn mép. Câu `lower` nằm 1/3 dưới, không đè lên HUD điện tích góc dưới trái
>      ở cảnh `07_overload` (đè thì đổi chữ sang `slam` hoặc dời `at` của chữ).
>    - Loé / rung **đúng khung va chạm**, không sớm / muộn.
>    - Không còn thẻ giữ chỗ (nếu đã đủ clip).
>    - Tổng thời lượng 45–50s, 1920×1080, 60fps, có tiếng. Số khung = thời lượng × 60.
> 7. Báo người dùng: đường dẫn file, thời lượng, những gì đã chỉnh, những gì cần họ quyết định / quay lại.
>
> **Cách chỉnh thường gặp** (đều trong `timeline.py`):
> - Va chạm không rơi đúng phách → **đừng** sửa `at` của cảnh. Sửa `pick` / `src_t` / `nudge` (Mục 6).
> - Muốn va chạm rơi chỗ khác trong cảnh → sửa `hit` (phách tính từ đầu cảnh), nhớ sửa luôn `at` của tiếng
>   `hit.wav` / `boom.wav` tương ứng trong `SOUNDS` (= `at` của cảnh + `hit`).
> - Muốn slow-motion một cảnh → thêm `speed=0.5`.
> - Chữ quá dài → rút gọn, tối đa 4 từ.
> - Muốn cảnh dài / ngắn hơn → sửa `len`, **dời `at` của mọi cảnh phía sau** cho liền mạch (không chồng, không hở).
>   Quy đổi: `phách = giây × BPM / 60`.
>
> **KHÔNG được:**
> - Dùng nhạc / âm thanh / hình ảnh từ nguồn không rõ giấy phép (chỉ Pixabay, tiếng tự tạo, tiếng trong game).
> - Sửa bất cứ thứ gì trong `Assets/` (đó là project Unity), xoá hoặc sửa file trong `Trailer_Footage\cinematic\`.
> - Dùng hình render có nhân vật (đã bỏ vì nhân vật không có animation).
> - Thêm chữ tiếng Việt vào phần chính (trailer chữ tiếng Anh; riêng dòng tên / lớp / GVHD ở màn logo là tiếng Việt).
> - Làm trailer dài quá 50 giây.

---

## 8. Kiểm tra trước khi nộp

- [ ] Đủ 17 clip OBS (hoặc 15 + 2 tuỳ chọn dùng cảnh thay), không còn thẻ giữ chỗ.
- [ ] `--check` không còn dòng `⚠ TRÙNG HÌNH`; ảnh `check.jpg` đúng hành động ở mọi cảnh.
- [ ] Nhạc thật từ Pixabay, đã ghi vào `CREDITS.md`.
- [ ] Mọi cú cắt / va chạm khớp nhịp: nghe bằng tai nghe, xem 2 lần.
- [ ] Đọc kịp từng câu chữ (mỗi câu ≥ 0,9s, câu `lower` ≥ 1,4s).
- [ ] Giọng ARENA CONTROL nghe rõ trên nền nhạc.
- [ ] Màn logo: tên, lớp, GVHD đúng chính tả có dấu.
- [ ] 45–50s · 1920×1080 · 60fps · MP4 · có tiếng.

## 9. Không dùng AI thì sao?

Mở `TrailerBuilder/edit/timeline.py` bằng Notepad, sửa ô `pick` / `src_t` / chữ, chạy
`py -3 TrailerBuilder/edit/build_trailer.py --check` để soát, rồi bỏ `--check` để dựng. Muốn dựng tay bằng
CapCut: dùng Mục 2 làm bảng cắt, nhập 17 clip OBS + 3 cảnh đảo trong `cinematic\` + file nhạc, đặt cảnh theo
cột "Giây", chữ theo cột "Chữ", va chạm theo cột "Va chạm rơi vào".
