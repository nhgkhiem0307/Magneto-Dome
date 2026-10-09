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
> | `fallRespawnDelay` | ✅ đã về 20 giây |
> | **Bỏ tick `Development Build`** | ⬜ **CÒN LẠI — làm lúc build bản nộp** |

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

## 3. Vật rơi khỏi map quay lại sau **5 giây** — ĐANG LÀ GIÁ TRỊ TEST

| | |
|---|---|
| Ở đâu | Mã nguồn `MagneticObject.cs`, ô `fallRespawnDelay` |
| Đang là | `5f` |
| Nên trả về | **`20f`** |

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
3. ⬜ `fallRespawnDelay` về `20`
4. ⬜ Bỏ tick `Development Build`
5. ⬜ Vào trận thử: vòng lửa giữa map **có đổi màu** khi đứng vào không
6. ⬜ Bấm `F12` trong bản build: **không được** có gì xảy ra
7. ⬜ Chơi trọn một trận 2 máy, kết thúc phải về được menu
