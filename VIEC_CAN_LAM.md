# 📋 VIỆC CẦN LÀM — Magneto-Dome

> **Cập nhật: 15/10/2026**
>
> File này là **danh sách việc còn lại**, ngắn gọn, để mở ra là biết làm gì tiếp.
> Chi tiết kỹ thuật từng việc nằm ở các file khác, có ghi rõ ở từng mục.
> Làm xong việc nào thì **gạch ngang** cả dòng (`~~chữ~~`), đổi `⬜` thành `✅` và ghi ngày.

---

## ❓ Ba câu cần trả lời trước

| # | Câu hỏi | Vì sao cần |
|---|---|---|
| 1 | **Hạn nộp mới và ngày bảo vệ là ngày nào?** | Hạn cũ 12/10 đã qua. Biết ngày mới xếp được lịch |
| ~~2~~ | ~~Tên giảng viên hướng dẫn?~~ | ✅ **Phạm Thị Hằng** *(14/10)* |
| 3 | Dòng ghi tên Claude trong commit GitHub — **giữ hay bỏ?** | Đó là lý do Claude hiện trong danh sách contributor |

---

## 🔴 Bắt buộc trước khi nộp

| # | Việc | Ai làm | Trạng thái |
|---|---|---|---|
| ~~1~~ | ~~Điền tên giảng viên hướng dẫn~~ | ~~Claude~~ | ✅ 14/10 — báo cáo *(2 bìa + Lời cảm ơn)* và bìa slide. Các ô "Ký và ghi rõ họ tên" ở BM01/02/04 **để trống cho cô ký tay** |
| ~~2~~ | ~~Build bản nộp chính thức + gắn icon game~~ | ~~Claude~~ | ✅ 14/10 → `Build_Nop.zip` |
| 3 | Chơi thử trọn một trận 2 máy bằng `Build_Nop\Magneto-Dome.exe` *(vòng lửa đổi màu, hết trận về menu)* → ổn rồi mới xoá `Build/`, `Build.zip`, `Build (2).zip` cũ | Bạn + 1 người | ⬜ |
| ~~4~~ | ~~Danh mục các hình vẽ / các bảng trong báo cáo~~ | ~~Claude~~ | ✅ 14/10 |
| ~~5~~ | ~~Chèn video demo vào slide 13~~ | — | ✅ 15/10 **bỏ slide demo** — demo trực tiếp bằng game. Ghi chú cách demo ở slide 12 |

## 🟡 Nên làm

| # | Việc | Ghi chú | Trạng thái |
|---|---|---|---|
| ~~6~~ | ~~Dựng trailer~~ | ✅ 15/10 — bản cuối: `../trailer/Magneto-Dome_Trailer_v3.mp4` (28MB) | ✅ |
| 7 | Xếp ảnh chụp vào slide | 7 ảnh: menu, phòng chờ, toàn cảnh đảo, HUD, hút vật, cửa hàng, bảng F1. Bạn chọn ảnh nào vào slide nào, Claude gắn vào script | ⬜ |
| ~~8~~ | ~~Điền 23 mục còn trống trong `CREDITS.md`~~ | ✅ 14/10 Claude truy được toàn bộ model + thư viện, sửa luôn Phụ lục B báo cáo và slide Credits | ✅ |
| 8b | Nguồn **13 file hiệu ứng âm thanh** *(đánh, nổ, dash, mua đồ, click…)* | Không còn dấu vết nào trên máy — chỉ bạn nhớ tải ở đâu. Xem `CREDITS.md` mục 4 | ⬜ |
| ~~9~~ | ~~Đổi chữ `Buying phase` → `BUYING PHASE`~~ | ~~Claude sửa thẳng file scene~~ — đã có trong `Build_Nop` | ✅ 14/10 |
| 9b | **Thử EM Barrier mới trong trận** *(làm luôn trong buổi demo thử)*: mua ($200) → thả → bắn vật vào xem gợn sóng → đợi 10 giây xem nhấp nháy rồi tan. Thả cả trên dốc | `Build_Nop` **đã build lại 14/10** — có tường mới, nhãn viết hoa, đã xoá bù nhìn. Thử luôn bằng bản build cũng được | ⬜ |
| 9c | **Thử bảng chết mới + lỗi pha mua đồ** (cần 2 máy): (a) nhảy khỏi đảo lúc mua đồ → phải được kéo về chỗ xuất phát ngay, còn nguyên đồ; (b) để địch đấm/bắn văng khỏi đảo → bảng chết hiện **tên họ** + đòn gì; (c) tự nhảy xuống lúc đánh nhau → "FELL OFF" | `Build_Nop` **đã build lại 14/10**, có sẵn 2 thay đổi này — thử thẳng bằng bản build | ⬜ |
| 10 | Xoá thư mục `C:\mdtmp` *(~100MB)* | Thư mục tạm của Claude. **Trong đó có `backup\` chứa bản sao báo cáo trước khi sửa 14/10** — mở báo cáo kiểm thấy ổn rồi hãy xoá | ⬜ |

## ⚪ Để sau / bỏ nếu không kịp

- **BM03 — Phiếu theo dõi Project**: cần giảng viên ký từng tuần → hỏi thầy/cô có bắt buộc không.
- **Poster**: chưa bắt đầu → hỏi trường có yêu cầu không.
- **Dọn 70MB tài nguyên thừa** trong `Resources/`: chỉ làm **sau khi nộp**. Xem `ROLLBACK_TRUOC_KHI_NOP.md` mục 8.


---

## 📌 Ghi nhớ khi làm tiếp

- **Sửa báo cáo xong** → `Ctrl + A` → `F9` → chọn *"Update entire table"* → lưu. Mục lục và danh mục hình/bảng mới cập nhật số trang.
- **Cần build lại** → trong Unity bấm menu **Magneto-Dome → Build bản nộp**, hoặc bảo Claude chạy lúc Unity đang tắt. Phải xoá/đổi tên thư mục `Build_Nop` cũ trước.
