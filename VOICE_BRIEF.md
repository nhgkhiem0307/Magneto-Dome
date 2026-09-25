# BẢN CHỈ ĐẠO GIỌNG THÔNG BÁO — Magneto-Dome

Tài liệu thiết kế cho 8 câu giọng trong `Assets/Resources/Audio/Voice/`.
Giữ lại để (1) tạo thêm câu mới cho khớp giọng cũ, (2) đưa vào báo cáo.

> Giọng đặt trên **ElevenLabs**. Khai báo bản quyền xem [CREDITS.md](CREDITS.md) mục 3b.

---

## 1. Nhân vật: ARENA CONTROL

Người đọc **không phải người dẫn chương trình**, mà là **hệ thống máy điều hành đấu
trường** — thứ đang quan sát trận đấu và báo cáo sự kiện.

| Là | Không phải |
|---|---|
| Bình thản, chắc chắn, có thẩm quyền | Hào hứng, hô hào |
| Báo cáo dữ liệu | Cổ vũ cho người chơi |
| Hạ giọng dứt khoát cuối câu | Lên giọng cuối câu như hỏi |

**Ba lý do chọn kiểu này:**
1. **Bối cảnh** — đảo công nghệ, găng tay từ tính, chế độ Quá Tải. Một cỗ máy giám sát
   hợp hơn hẳn một MC thể thao.
2. **Tương phản** — giọng càng bình tĩnh giữa hỗn chiến thì càng lạnh và càng đáng nhớ.
3. **Nghe cả trăm lần** — một trận 5–9 round, mỗi round nghe lại từng ấy câu. Giọng hô
   hào nghe lần thứ ba đã khó chịu.

**Tham chiếu:** giọng hệ thống trong Portal / Halo / Overwatch, giọng thông báo tàu điện.
**Tránh:** bình luận viên esports, giọng trailer phim, giọng AI ngọt ngào.

---

## 2. Prompt đặt giọng (ElevenLabs → Voice Design → Describe the voice)

```
A female arena announcement system for a futuristic combat sport. Mid-to-low register,
smooth and even, with a faint metallic edge as if heard through a stadium PA system.
Calm, controlled authority: never excited, never sing-song, never breathy. Speaks in
short complete statements with a clean downward inflection at the end of every phrase.
Neutral American accent, crisp consonants, tight and dry delivery with no vocal fry.
Measured pace, slightly slower than conversation, with clear space between sentences.
Sounds like a mission control operator or transit station announcer, not a sports
commentator. Emotionally flat but not lifeless - the authority comes from restraint.
```

**Ô preview text** — đừng để nguyên câu mẫu của họ, ElevenLabs dựng giọng dựa trên chính
câu này:

```
Match... point. Combat engaged. Round lost.
```

**Thông số:** Stability **60%** · Similarity **80%** · Style **0%** · Speaker boost **bật**.

---

## 3. Tám câu đang dùng

Gõ **đúng dấu câu** — dấu câu là cách ra lệnh cho máy đọc. `...` là quãng nghỉ thật.
Tạo **từng câu một**, đừng gõ cả 8 câu một lượt (máy sẽ nối ngữ điệu giữa các câu).

| File | Văn bản | Ý đồ |
|---|---|---|
| `buy phase` | `Buy phase... Arm yourself.` | Báo, nghỉ, rồi ra lệnh |
| `combat` | `Combat engaged.` | Câu duy nhất nhanh và gọn |
| `round won` | `Round won.` | Bình thản |
| `round lost` | `Round lost.` | Đọc y hệt câu trên — máy không phán xét |
| `match point` | `Match... point.` | Câu nặng nhất, ngắt giữa hai chữ |
| `overtime` | `Overtime.` | Chậm, thấp |
| `victory` | `Victory.` | Nhạc lo phần ăn mừng |
| `defeat` | `Defeat.` | Đừng để máy tỏ ra thương hại |

### Hai câu đã móc sẵn trong code, chưa có file

Tạo bằng **đúng giọng cũ**, đặt vào cùng thư mục rồi kéo vào `AudioManager`.
Để trống thì im lặng, không lỗi.

| Ô trong AudioManager | Văn bản | Khi nào phát |
|---|---|---|
| `Vo Charge Critical` | `Charge critical.` | Điện tích của bạn vượt 80% — chỉ mình bạn nghe |
| `Vo Ten Seconds` | `Ten seconds.` | Còn 10 giây pha chiến đấu |

---

## 4. Hai nguyên tắc dễ quên

**Giọng khô + bộ lọc = đấu trường.** Chất "giữa đấu trường" không nằm trong giọng mà ở
khâu xử lý: `AudioManager` có sẵn bộ lọc loa vô tuyến (cắt trầm, cắt siêu cao, thêm rè).
Giọng nghe khô khi tải về là **đúng** — tìm giọng tự nó đã vang như loa sân vận động thì
đem lọc nữa sẽ thành vang chồng vang, đục và mất chữ.

**Đánh giá giọng phải nghe trong game**, lúc đang đánh nhau có tiếng nổ và nhạc nền.
Nhiều giọng nghe riêng thì chán mà vào game lại rõ và hợp, và ngược lại.

### Chỉnh trong `AudioManager` nếu nghe chưa ổn

| Nghe thấy | Sửa |
|---|---|
| Rè quá, máy móc quá | `Radio Distortion` 0.22 → 0.1 |
| Mỏng như điện thoại | `Radio High Pass` 380 → 250 |
| Bị tiếng nổ át mất | `Voice Volume` → 1.4 |
| Một câu đọc nhanh quá | `Round Won Speed` (chỉ câu này có ô riêng) |
