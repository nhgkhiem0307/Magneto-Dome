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
| ~~3~~ | ~~Chơi thử trọn một trận 2 máy bằng `Build_Nop`~~ | ~~Bạn + 1 người~~ | ✅ 15/10 — chủ project đã chơi, ổn. Giờ xoá được `Build/`, `Build.zip`, `Build (2).zip` cũ |
| ~~4~~ | ~~Danh mục các hình vẽ / các bảng trong báo cáo~~ | ~~Claude~~ | ✅ 14/10 |
| ~~5~~ | ~~Chèn video demo vào slide 13~~ | — | ✅ 15/10 **bỏ slide demo** — demo trực tiếp bằng game. Ghi chú cách demo ở slide 12 |

## 🟡 Nên làm

| # | Việc | Ghi chú | Trạng thái |
|---|---|---|---|
| ~~6~~ | ~~Dựng trailer~~ | ✅ 15/10 — bản cuối: `../trailer/Magneto-Dome_Trailer_v3.mp4` (28MB) | ✅ |
| 7 | Xếp ảnh chụp vào slide | 7 ảnh: menu, phòng chờ, toàn cảnh đảo, HUD, hút vật, cửa hàng, bảng F1. Bạn chọn ảnh nào vào slide nào, Claude gắn vào script | ⬜ |
| ~~8~~ | ~~Điền 23 mục còn trống trong `CREDITS.md`~~ | ✅ 14/10 Claude truy được toàn bộ model + thư viện, sửa luôn Phụ lục B báo cáo và slide Credits | ✅ |
| ~~8b~~ | ~~Nguồn 13 file hiệu ứng âm thanh~~ | ✅ 15/10 — Claude truy đủ 18/18 file qua nhãn tải về của Windows. 2 file CC BY đã ghi tên tác giả ở CREDITS, Phụ lục B báo cáo, slide Credits | ✅ |
| ~~9~~ | ~~Đổi chữ `Buying phase` → `BUYING PHASE`~~ | ~~Claude sửa thẳng file scene~~ — đã có trong `Build_Nop` | ✅ 14/10 |
| ~~9b~~ | ~~Thử EM Barrier mới trong trận~~ | — | ✅ 15/10 |
| ~~9c~~ | ~~Thử bảng chết mới + lỗi pha mua đồ (2 máy)~~ | — | ✅ 15/10 |
| 9d | **Thử menu mới** (16/10): mở MenuScene bấm Play → kiểm từng nút vẫn bấm được (nhập tên, tạo phòng, danh sách phòng, vào phòng, cài đặt), camera bay vòng mượt | `Build_Nop` **đã build lại 16/10** có menu mới — thử thẳng bằng bản build | ⬜ |
| 9e | **Thử phòng chờ mới (cần 2 máy)**: Host thấy nút Switch + bấm thẻ người khác là chuyển đội được; Client KHÔNG có nút Switch, thấy "Waiting for host…"; nút COPY chép mã phòng; mở Settings thì giao diện phía sau ẩn đi | `Build_Nop` **đã build lại 16/10** — thử thẳng bằng bản build | ⬜ |
| 9f | **Sương mù trong trận**: chỉ cho Claude chỗ nhìn xa bị xấu (chụp màn hình lúc chơi) | Ảnh so sánh 4 mức ở `C:\mdtmp\fog\SO_SANH_SUONG_MU.png` | ⬜ |
| 9g | **Thử đạn mới + nút Close** (16/10): đẩy/bắn nhanh hơn 20%, bắn ngang bay được gần hết đảo, vật đâm người xong rơi xuống ngay chứ không trôi; nút Close trong Settings bấm được | `Build_Nop` **đã build lại 16/10 tối** — thử thẳng bằng bản build | ⬜ |
| 9i | **Thử 1v1 / 2v2 + Kick (cần 2–3 máy)** (16/10): Ghép trận → bảng hỏi 1v1/2v2 hiện ra, chọn 1v1 thì chỉ ghép với người cũng chọn 1v1; tạo phòng → bấm 1V1/2V2 đổi được, đổi sang 1V1 lúc có 3 người thì hiện dòng đỏ bắt kick; bấm KICK hai lần → máy kia về menu với câu "You were kicked…"; phòng 1v1 đủ 2 người thì máy thứ 3 vào bằng mã bị từ chối; Room List ghi `1v1` / `2v2`. **Thêm (sau 21:53):** người bị kick gõ lại mã phòng thì bị từ chối; có người vào/ra thì phòng chờ hiện một dòng báo | `Build_Nop` **đã build lại 17/10, 18:14** — thử thẳng bằng bản build | ⬜ |
| 9j | **Thử cú đẩy mới** (16/10): đẩy vật nằm dưới đất → nó nhấc lên rồi bay gần ngang (chếch lên rất nhẹ ~2°), vượt gờ thấp; đẩy lúc vật đang nảy → không giật lên; nhìn dốc xuống đất mà đẩy → vẫn bay như vậy chứ không vọt lên trời | `Build_Nop` **đã build lại 17/10, 18:14** — thử thẳng bằng bản build | ⬜ |
| 9k | **Xem lại map TestScene** (16/10): 8 viên đã dời + xoay ngẫu nhiên — mở scene nhìn có viên nào trông lạ không; spawn Đỏ hết bị đá đè; giữa khu chiếm đóng có 2 viên đá | Không ưng viên nào thì bảo Claude, có bản sao lưu | ⬜ |
| 9l | **Thử rào chắn + biển mây mới** (17/10, màu rào đã nhạt lại — có trong bản build 19/10): pha mua đồ nhìn sang không thấy đội kia; áp sát tường thấy gợn sóng; vào trận tường tan từ dưới lên; nhìn dọc tường không thấy đầu tường; đứng mép đảo nhìn ra / nhìn xuống thấy biển mây liền tới chân trời. **Để ý FPS** (mục tiêu ≥ 60 ở 1080p) — tụt thì bảo Claude tắt đáy mây | `Build_Nop` **đã build lại 17/10, 18:14** — thử thẳng bằng bản build | ⬜ |
| 9m | **Thử hiệu ứng mới** (17/10): bắn laze vào vật → thấy sét quấn quanh tia + vật loé; đấm trúng → nổ to, tay khựng nhẹ một nhịp; đấm người nhiều điện → nổ to hơn hẳn. Khựng thấy ì quá thì giảm ô `Hit Stop Duration` trên Player (mặc định 0.06, 0 = tắt) | `Build_Nop` **đã build lại 17/10, 21:35** — thử thẳng bằng bản build | ⬜ |
| 9n | **Thử nổ TNT + can xăng** (17/10): TNT nổ thấy chớp + lửa + khói + tàn lửa, camera rung khi đứng gần; dùng can xăng lên vật → xăng bắn tung + chớp lửa, sau đó vật lách tách tàn lửa + quầng cam; ném vật đó đi thấy vệt tàn lửa bay theo; sang round mới vật hết cháy | `Build_Nop` **đã build lại 17/10, 21:35** — thử thẳng bằng bản build | ⬜ |
| 9o | **Thử hiệu ứng dùng đồ** (17/10): dùng Nước tăng lực → viền xanh chanh + vệt tốc độ + góc nhìn phình ra, sau đó viền phập phồng, 3 giây cuối nháy nhanh; dùng Bộ xả điện → viền xanh lơ + tia điện quanh mép. **Thử cả ở máy Client**: giờ Client dùng đồ đã có tiếng | `Build_Nop` **đã build lại 17/10, 21:35** — thử thẳng bằng bản build | ⬜ |
| 9p | **Thử rào kính mờ (cần 2 máy)** (19/10): pha mua đồ nhìn sang thấy dáng đội kia mờ mờ, **KHÔNG thấy viền đỏ và tên của họ**; đồng đội vẫn có viền xanh. Mờ quá / rõ quá thì chỉnh ô `Haze Alpha` của shader `BarrierCurtain` (0.8; lớn hơn = mờ hơn) | `Build_Nop` **đã build lại 19/10, 20:42** — thử thẳng bằng bản build | ⬜ |
| 9h | **Thử nút Quit Game + bảng rời trận** (16/10): Settings → Quit Game → bảng tối hiện ra, Cancel / `Esc` đóng lại, Quit thì tắt game (Editor: dừng Play). Trong trận bấm Leave match: chữ cảnh báo không còn nhảy lên đè tiêu đề, nút ghi `Confirm (3)` đếm lùi | `Build_Nop` **đã build lại 16/10 tối** — thử thẳng bằng bản build | ⬜ |
| 10 | Xoá thư mục `C:\mdtmp` *(~100MB)* | Thư mục tạm của Claude. **Trong đó có `backup\` chứa bản sao báo cáo trước khi sửa 14/10** — mở báo cáo kiểm thấy ổn rồi hãy xoá | ⬜ |

## ⚪ Để sau / bỏ nếu không kịp

- **BM03 — Phiếu theo dõi Project**: cần giảng viên ký từng tuần → hỏi thầy/cô có bắt buộc không.
- **Poster**: chưa bắt đầu → hỏi trường có yêu cầu không.
- **Dọn 70MB tài nguyên thừa** trong `Resources/`: chỉ làm **sau khi nộp**. Xem `ROLLBACK_TRUOC_KHI_NOP.md` mục 8.


---

## 📌 Ghi nhớ khi làm tiếp

- **Sửa báo cáo xong** → `Ctrl + A` → `F9` → chọn *"Update entire table"* → lưu. Mục lục và danh mục hình/bảng mới cập nhật số trang.
- **Cần build lại** → trong Unity bấm menu **Magneto-Dome → Build bản nộp**, hoặc bảo Claude chạy lúc Unity đang tắt. Phải xoá/đổi tên thư mục `Build_Nop` cũ trước.
