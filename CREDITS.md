# TÀI NGUYÊN BÊN THỨ BA — Magneto-Dome

Khai báo toàn bộ asset, thư viện và công cụ không do tác giả tự làm.
Chép mục này vào báo cáo đồ án.

> ⚠️ **Ô nào ghi `[CẦN ĐIỀN]` là phần chỉ bạn biết** — hãy mở lại trang tải về, xem mục
> License rồi điền vào. Làm sớm khi còn nhớ, đừng để tới tuần cuối.

---

## 1. Nền tảng & thư viện

| Tên | Nhà cung cấp | Giấy phép | Dùng để làm gì |
|---|---|---|---|
| Unity 6000.3.17f1 | Unity Technologies | Unity Personal | Game engine |
| Universal Render Pipeline 17.3.0 | Unity Technologies | Kèm theo Unity | Đồ hoạ |
| Input System 1.19.0 | Unity Technologies | Kèm theo Unity | Xử lý phím và chuột |
| TextMesh Pro | Unity Technologies | Kèm theo Unity | Chữ trên giao diện |
| **Photon Fusion 2** | Photon Engine (Exit Games) | Gói miễn phí, cần tài khoản | Toàn bộ phần mạng nhiều người chơi |
| **ParrelSync** | Mã nguồn mở | MIT *(cần xác nhận lại)* | Tạo bản sao project để test 2 client |

## 2. Model và texture

| Thư mục trong project | Nguồn | Giấy phép | Ghi chú |
|---|---|---|---|
| `Resources/Meshy_AI_Floating_Isle_.../` | **Meshy AI** (sinh bằng AI) | `[CẦN ĐIỀN]` | Model đảo bay. Xem mục 5 bên dưới |
| `Resources/Tree9/` | `[CẦN ĐIỀN]` | `[CẦN ĐIỀN]` | Cây trên map |
| `Resources/Rocks and Boulders 2/` | `[CẦN ĐIỀN]` | `[CẦN ĐIỀN]` | Đá, tảng đá |
| `Resources/UI kit/` | `[CẦN ĐIỀN]` | `[CẦN ĐIỀN]` | Icon và khung giao diện |
| `Resources/Aura/` | `[CẦN ĐIỀN]` | `[CẦN ĐIỀN]` | Hiệu ứng hào quang từ tính |
| `Resources/QuickOutline/` | `[CẦN ĐIỀN]` | `[CẦN ĐIỀN]` | Viền sáng quanh vật thể |

## 3. Nhân vật và animation *(đang làm)*

| Hạng mục | Nguồn | Giấy phép | Trạng thái |
|---|---|---|---|
| Model nhân vật **"Vanguard By T. Choonyung"** | **Mixamo** (Adobe) | Miễn phí, không phí bản quyền, dùng thương mại được. Cấm bán lại / phát tán file gốc | ⬜ Đang làm |
| Animation Idle / Run / Fall | **Mixamo** (Adobe) | Như trên | ⬜ Đang làm |

Đã tra lại điều khoản Mixamo ngày 16/08/2026:
- [Mixamo FAQ — Adobe](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html)
- [Mixamo License Guide — LicenseOrg](https://www.licenseorg.com/guide/3d-assets/mixamo)

Nội dung Mixamo *"available for free, with no licensing or royalty fees, for unlimited
commercial or non commercial use"*. Ràng buộc duy nhất: không đóng gói lại file gốc thành
bộ asset để bán hoặc phát tán. Nướng vào game đã build thì hoàn toàn hợp lệ.

Vanguard là nhân vật trong thư viện chuẩn của Mixamo nên **đã rig sẵn theo chuẩn Mixamo** —
animation Mixamo khớp 100%, không phải qua auto-rig, không có rủi ro retarget lệch.

## 4. Âm thanh *(chưa có gì)*

| Hạng mục | Nguồn dự kiến | Giấy phép | Trạng thái |
|---|---|---|---|
| 19 clip hiệu ứng | freesound.org / kenney.nl | Xem từng file — freesound trộn nhiều loại CC | ⬜ Chưa làm |

> Freesound có nhiều mức: CC0 (thoải mái), CC-BY (phải ghi tên tác giả),
> và vài loại cấm dùng thương mại. **Ghi lại tên tác giả + đường dẫn NGAY khi tải**,
> tìm lại sau rất mất công.

## 5. Ghi chú về nội dung sinh bằng AI

Model đảo bay được tạo bằng **Meshy AI**. Hai việc nên làm:

1. **Kiểm tra điều khoản của Meshy** ứng với gói bạn dùng — gói miễn phí và gói trả phí
   thường khác nhau về quyền sử dụng.
2. **Chủ động nói ra trong báo cáo và khi bảo vệ.** Dùng công cụ AI để tạo tài nguyên là
   chuyện bình thường và hợp lệ, nhưng giấu đi rồi bị hỏi trúng thì rất bất lợi.
   Nói trước thì đó chỉ là một lựa chọn công cụ.

Phần **mã nguồn** được viết với sự hỗ trợ của Claude (Anthropic) — các commit có ghi
`Co-Authored-By`. Nếu trường có quy định về việc dùng AI hỗ trợ lập trình, hãy khai báo
theo đúng quy định đó.

---

## Nguyên tắc chung

**Khai báo thừa còn hơn thiếu.** Dùng asset miễn phí là chuyện bình thường ở đồ án —
gần như ai cũng vậy. Nhưng một model đẹp bất thường mà không được nhắc tới trong báo cáo
sẽ khiến hội đồng đặt câu hỏi về cả những phần bạn thật sự tự làm.

Khai báo đầy đủ: mất một dòng trong báo cáo.
Không khai báo: rủi ro mất niềm tin vào toàn bộ đồ án.
