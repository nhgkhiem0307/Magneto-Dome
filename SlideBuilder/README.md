# SlideBuilder — bộ dựng slide bảo vệ đồ án

Thư mục này **nằm ngoài `Assets/`** nên Unity không đụng tới, không vào bản build.

## Nó là gì

`NOP_BAI/[ĐT-QĐ04-BM08] Final Project.pptx` **không gõ tay mà sinh ra từ `make_slide.js`**.
Muốn sửa slide thì sửa file script rồi chạy lại, đừng sửa thẳng file `.pptx` —
lần chạy sau sẽ ghi đè mất.

## 🔴 ĐỪNG CHÈN ẢNH TAY VÀO FILE .pptx

**Chạy lại script là file `.pptx` bị ghi đè, ảnh chèn tay mất sạch.** Chuyện này đã xảy ra
thật ngày 12/10: bản slide đã chèn ảnh bị một lệnh `cp` ghi đè mất — mà `cp` không đi qua
thùng rác nên không khôi phục được. Phải lấy lại ảnh từ trong file báo cáo.

**Cách đúng: để ảnh trong thư mục `img/` và nhúng đường dẫn vào script.** Dựng lại bao
nhiêu lần ảnh vẫn còn.

| File trong `img/` | Dùng ở slide |
|---|---|
| `icon.png` | 1 — bìa, logo game |
| `shot_dao.png` | 3 — toàn cảnh đảo |
| `shot_hut.png` | 6 — góc nhìn thứ nhất, hút vật thể |
| `shot_hud.png` | 8 — cận cảnh HUD |

Muốn đổi ảnh thì **thay file trong `img/`** rồi chạy lại script, không động vào `.pptx`.
Thêm ảnh mới thì dùng hàm `picImg()` trong `make_slide.js`.

> Khung duy nhất còn để trống là **slide 13 (Demo)** — chỗ đó dành cho video, phải chèn tay.
> Chèn xong thì đừng chạy lại script nữa, hoặc `Save As` ra tên khác để giữ.

## Ba file ảnh kèm theo là gì

Lấy từ slide master chính thức của VTC Academy, bóc ra từ file mẫu của trường:

| File | Dùng làm gì |
|---|---|
| `image1.png` | Nền slide nội dung: thanh tiêu đề xanh, logo VTC góc phải, hoạ tiết góc dưới |
| `image21.png` | Logo VTC Academy màu trắng, nền trong suốt — cho slide bìa và slide cảm ơn |
| `bg_navy.png` | Nền chuyển sắc xanh của slide bìa, tự sinh cho khớp màu mẫu trường |

**Đừng xoá ba file này.** Thiếu chúng thì slide mất sạch nhận diện trường.

## Chạy lại

```bash
cd SlideBuilder
npm install pptxgenjs      # chỉ lần đầu
node make_slide.js         # sinh ra MagnetoDome_Slide.pptx ngay tại đây
```

Rồi chép đè vào `NOP_BAI/[ĐT-QĐ04-BM08] Final Project.pptx`.

## Hình học đã đo sẵn — đừng đoán lại

Các con số này đo từ ảnh nền của trường, đã dùng trong script:

| Mục | Giá trị |
|---|---|
| Khổ slide | 10 × 5,625 inch (`LAYOUT_16x9`) |
| Thanh tiêu đề xanh | y từ **0,376** đến **0,744** inch, kết thúc ngang ở x = **7,48** inch |
| Vùng nội dung an toàn | y từ **0,95** đến **5,10** inch |
| Lề trái / phải | **0,45** và **9,55** inch |
| Hoạ tiết góc dưới | bắt đầu từ y = **5,245** inch — chữ đặt thấp hơn mức này sẽ bị đè |
| Font | Arial (đúng theme của trường) |
| Màu | thanh xanh `#26407D`, chữ tiêu đề `#002060`, nhấn đỏ `#E8453C` và xanh `#2E86DE` |

Hai màu nhấn đỏ/xanh là **cực dương và cực âm của game**, dùng lặp lại thành mô-típ
xuyên suốt. Đừng đổi sang màu khác.

## Kiểm tra sau khi sửa

Máy này **không có LibreOffice**, nhưng có PowerPoint nên xuất được ảnh để soi:

```powershell
$pp = New-Object -ComObject PowerPoint.Application
$d = $pp.Presentations.Open('<đường dẫn>\MagnetoDome_Slide.pptx', -1, 0, 0)
for ($i=1; $i -le $d.Slides.Count; $i++) { $d.Slides.Item($i).Export("C:\mdtmp\out\s$i.png",'PNG',1600,900) }
$d.Close(); $pp.Quit()
```

⚠️ PowerPoint **không xuất được ra đường dẫn quá dài** — dùng thư mục ngắn kiểu `C:\mdtmp`.

Lỗi hay gặp nhất là **chữ tràn khỏi thẻ**, nhìn ảnh mới thấy chứ đọc code không ra.
