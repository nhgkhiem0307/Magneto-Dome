# TrailerBuilder — bộ dựng trailer tự động *(24/10, viết lại 25/10)*

Thư mục này **nằm ngoài `Assets/`** nên Unity không biên dịch, không vào bản build.
Kịch bản, danh sách clip cần quay, lời giao việc cho AI edit: **`TRAILER_SCRIPT.md`** (gốc project).

## Nguyên liệu: `D:\Bt vtc\Capstone Project\Trailer_Footage\` (ngoài repo vì nặng)

| Thư mục | Chứa gì |
|---|---|
| `obs\` | Clip góc nhìn thứ nhất chủ project tự quay bằng OBS, **giữ nguyên tên gốc** (25/10: 18 file). Bảng `CLIPS` trong `timeline.py` nối tên cảnh với file + phần dùng được. Danh sách cần quay: `TRAILER_SCRIPT.md` Mục 3.3, đã quay: Mục 3.5 |
| `cinematic\` | **3 cảnh toàn đảo** render sẵn 22/10, **không có nhân vật**: `s1_island.mp4` (8s, bay vòng hạ dần), `s1_pushin.mp4` (7s, tiến về phía đảo), `s7_bg.mp4` (10s, bay chậm trên cao, nền màn logo). 1920×1080, 60fps |
| `music\` | 1 bài nhạc Pixabay — *"Upbeat Rock"*, JonasBlakewood (130 BPM) |
| `_work\` | File tạm khi dựng (tự xoá, tự tạo lại). Có `report.txt` + `check.jpg` sau mỗi lần chạy |

> Các cảnh render **có nhân vật** (đấm, nổ, rơi…) và script render của chúng (`TrailerDirector.cs`, `TrailerRun.cs`,
> `encode.py`) đã **xoá 25/10**: nhân vật dựng bằng máy không có animation, nhìn giả.
> Ảnh gốc từng khung của 3 cảnh đảo còn ở `C:\mdtmp\trailer\` (thư mục tạm), cần thì ghép lại bằng ffmpeg
> (`-framerate 60 -i f%05d.jpg`).

## Các file

| File | Vai trò |
|---|---|
| `edit/timeline.py` | **Bảng thời gian** (cảnh, hiệu ứng, chữ, âm thanh), tính bằng phách nhạc. File duy nhất cần sửa |
| `edit/build_trailer.py` | Bộ dựng: dò khoảnh khắc trong clip, cắt từng cảnh + hiệu ứng (ffmpeg), phủ chữ động (ảnh Pillow), trộn tiếng (numpy), xuất MP4 |
| `edit/sfx.py` + `edit/sfx/` | Tiếng trailer **tự tổng hợp bằng code** (whoosh, boom, hit, riser, zap, subdrop) + nhạc tạm 128 BPM |
| `edit/beats.py` | Dò BPM + chỗ drop của bài nhạc thật, in sẵn 3 dòng chép vào `timeline.py` |

## Chạy

```
py -3 TrailerBuilder/edit/build_trailer.py --check   # ~20s: chỉ dò, in bảng + ảnh _work/check.jpg
py -3 TrailerBuilder/edit/build_trailer.py --fast    # ~1 phút: bản xem nhanh 960x540
py -3 TrailerBuilder/edit/build_trailer.py           # ~4-6 phút: bản thật 1920x1080 60fps
```
→ `Trailer_Footage/MagnetoDome_Trailer[_DRAFT][_fast].mp4` (`_DRAFT` = còn thẻ giữ chỗ hoặc còn nhạc tạm).

## Clip quay tay dài ngắn tuỳ ý: hàm `plan_shot()`

Không cần cắt clip chuẩn. Với mỗi cảnh, bộ dựng:
1. **Tìm khoảnh khắc chính** trong clip. Kiểu `pick`: `first` / `last` / `loudest` / `hit2`… lấy theo tiếng va chạm
   dò được trong tiếng game (tiếng tăng vọt ≥ 9 dB trong 80ms), còn `mid` lấy giữa clip.
   Có thể ghi tay bằng `src_t`.
2. **Đặt khoảnh khắc đó đúng phách** `hit` của cảnh, rồi cắt khúc xung quanh cho đủ độ dài ô.
3. Clip không đủ dài quanh khoảnh khắc thì **tự làm chậm** (tối đa x0.5, tiếng chậm theo bằng `atempo`, giữ cao độ).
   Vẫn thiếu thì giữ khung đầu/cuối (`tpad`) và in cảnh báo.
4. In cảnh báo khi hai cảnh lấy **trùng một đoạn** của cùng clip.

Đã thử bằng clip giả có tiếng "bụp" ở giây biết trước (25/10). Va chạm rơi đúng phách, sai số dưới 1 khung.
Clip 1,5s nhét vào ô 3,75s ra đủ 112 khung. Clip không tiếng thì lùi về lấy giữa clip.

**Bảng `CLIPS`** (25/10): tên cảnh → (file thật, giây đầu, giây cuối dùng được). Cắt bỏ đoạn hỏng ở đầu/đuôi clip (bảng
Settings hiện lúc bấm Esc dừng quay). Nguồn được mở với `-t` dừng đúng giây cuối, tiếng gốc cũng cắt theo, nên không bao
giờ lộ phần bị bỏ. **`MUSIC_SPEED`** (0.875–1.125): tua nhạc ±12,5% giữ cao độ, nhịp hiệu dụng tự đổi theo.

⚠ Bẫy đã gặp: `tpad` phải đứng **trước** `setpts=PTS-STARTPTS` trong chuỗi lọc, đặt sau thì không ăn.
⚠ **Phủ chữ bằng 17 đầu vào `-i ảnh.png` làm ffmpeg 7 KẸT CHÉO** (25/10: bộ nhớ 1,1 GB, CPU đứng im, 11 phút không ra
khung nào): luồng ảnh lặp sinh khung liên tục trong khi chuỗi overlay chỉ cần chúng ở giây 40+, hàng đợi đầy, các luồng
chờ nhau. Giờ nạp ảnh chữ bằng bộ lọc `movie=` ngay trong chuỗi lọc (chỉ đọc khi cần) → bước phủ chữ còn ~10 giây.
Số khung mỗi cảnh tính từ mốc **tuyệt đối** (làm tròn đầu + cuối), làm tròn từng cảnh riêng thì trôi nhịp.
⚠ **Chữ phóng chậm = chữ rung** (25/10): `scale` + `overlay` của ffmpeg chỉ chạy theo điểm ảnh nguyên → lớp chữ phóng 1,5%
trong vài giây nhảy cỡ 2px mỗi vài khung, viền giật. Đã bỏ hết phóng chậm; chuyển động chậm duy nhất là `sway=True`
(`render_sway_movie`: Pillow dịch từng khung bằng nội suy bicubic, ghi `.mov` mã png có kênh trong suốt). Đo được: bước lớn
nhất 0,35px/khung, đường cong mượt.
