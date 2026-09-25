# TÀI NGUYÊN BÊN THỨ BA — Magneto-Dome

Khai báo toàn bộ asset, thư viện và công cụ không do tác giả tự làm.
Chép mục này vào báo cáo đồ án.

> **Cập nhật 23/09/2026.** Đã rà lại toàn bộ thư mục `Assets/` và đối chiếu với file
> README/license nằm sẵn trong từng bộ asset.
>
> - ✅ = đã xác minh được **bằng bằng chứng trong chính project**
> - ⬜ = **chỉ bạn biết**, phải mở lại trang tải về để điền

---

## 1. Nền tảng & thư viện

| Tên | Nhà cung cấp | Giấy phép | Dùng để làm gì |
|---|---|---|---|
| Unity 6000.3.17f1 | Unity Technologies | Unity Personal | Game engine |
| Universal Render Pipeline 17.3.0 | Unity Technologies | Kèm theo Unity | Đồ hoạ |
| Input System 1.19.0 | Unity Technologies | Kèm theo Unity | Xử lý phím và chuột |
| TextMesh Pro | Unity Technologies | Kèm theo Unity | Chữ trên giao diện |
| **Photon Fusion 2** | Photon Engine (Exit Games) | Gói miễn phí *(tối đa 100 CCU)*, cần tài khoản | Toàn bộ phần mạng nhiều người chơi |
| **ParrelSync** | Ubbe Games / cộng đồng mã nguồn mở | MIT ⬜ *(xác nhận lại trên GitHub)* | Tạo bản sao project để test 2 client |
| NanoSockets | *(kèm trong Photon Fusion)* | ✅ Có sẵn file `libnanosockets_LICENSE.txt` trong `Assets/Photon/Fusion/Plugins/NanoSockets/` | Tầng mạng cấp thấp |

---

## 2. Model, texture, shader

| Thư mục | Nguồn | Giấy phép | Trạng thái |
|---|---|---|---|
| `Resources/Environment/QuickOutline/` | **Chris Nolet** ✅ | MIT ⬜ | ✅ File `Readme.txt` ghi rõ *"Developed by Chris Nolet (c) 2018"*. Bản trên GitHub (`chrisnolet/QuickOutline`) dùng giấy phép MIT — đối chiếu lại cho chắc |
| `Resources/Environment/Island/` | **Meshy AI** *(sinh bằng AI)* | ⬜ | Model đảo bay. Xem mục 5 |
| `Resources/Environment/Rocks and Boulders 2/` | ⬜ *(nghi Unity Asset Store)* | ⬜ | Tên thư mục trùng với một bộ asset trên Asset Store — mở lại Package Manager → My Assets để xem tên tác giả |
| `Resources/Environment/Tree9/` | ⬜ | ⬜ | Cây trên map |
| `Resources/Environment/Fantasy Skybox FREE/` | **Render Knight** ✅ | Unity Asset Store — gói **FREE** ✅ | Bầu trời của cả bản đồ (`FS000_Day_03`). File `Readme.txt` trong thư mục ghi rõ tác giả, website `render-knight.com` và link Asset Store |
| `Resources/Environment/Nature/` | ⬜ **← ưu tiên tìm lại** | ⬜ | Cỏ, hoa, bụi. Xem cảnh báo bên dưới |

> ⚠️ **`Nature/` là mục rủi ro nhất.** Hai thư mục con tên
> `FBX-20260824T044903Z-1-001` và `Textures-20260824T044905Z-1-001` — đây là kiểu đặt tên
> **Google Drive tự sinh khi nén thư mục để tải về**. Nghĩa là bộ này tải từ một đường dẫn
> Drive, không phải Asset Store, nên **không có hoá đơn hay lịch sử tải nào để tra ngược**.
>
> Tìm lại đường dẫn đó trong lịch sử trình duyệt ngày **24/08/2026** khi còn kịp. Nếu không
> tìm ra nguồn, cân nhắc thay bằng bộ khác có giấy phép rõ ràng — đây là thứ dễ bị hỏi nhất.

---

## 3. Nhân vật và animation

| Hạng mục | Nguồn | Giấy phép | Trạng thái |
|---|---|---|---|
| Model **"Vanguard By T. Choonyung"** | **Mixamo** (Adobe) | Miễn phí, không phí bản quyền, dùng thương mại được. Cấm bán lại / phát tán file gốc | ✅ Đã tra 16/08/2026 |
| Animation Idle / Run / Fall / Hook Punch | **Mixamo** (Adobe) | Như trên | ✅ |

Nguồn đã tra:
- [Mixamo FAQ — Adobe](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html)
- [Mixamo License Guide — LicenseOrg](https://www.licenseorg.com/guide/3d-assets/mixamo)

Mixamo ghi *"available for free, with no licensing or royalty fees, for unlimited commercial
or non commercial use"*. Ràng buộc duy nhất: không đóng gói lại file gốc thành bộ asset để
bán hoặc phát tán. Nướng vào game đã build thì hoàn toàn hợp lệ.

Vanguard nằm trong thư viện chuẩn của Mixamo nên **đã rig sẵn theo chuẩn Mixamo** — animation
khớp 100%, không phải qua auto-rig, không có rủi ro retarget lệch.

---

## 3b. Giọng thông báo trong trận — `Resources/Audio/Voice/`

8 file: `buy phase`, `combat`, `round won`, `round lost`, `match point`, `overtime`,
`victory`, `defeat`.

| Hạng mục | Nguồn | Giấy phép |
|---|---|---|
| **Lời thoại** | Chủ project tự viết | — |
| **Giọng đọc** | **ElevenLabs** (sinh bằng AI) | ⬜ Gói miễn phí: **BẮT BUỘC ghi nguồn**, chỉ dùng phi thương mại |

> ⚠️ **Đây là giọng AI, không phải người thu — phải nói rõ trong báo cáo.** Gói miễn phí
> của ElevenLabs yêu cầu ghi nguồn, nên đây không phải chuyện "nên ghi cho lịch sự" mà là
> **điều kiện của giấy phép**. Không ghi là dùng sai giấy phép.
>
> ⬜ Mở lại trang ElevenLabs, xem đang ở gói nào rồi chép đúng câu ghi nguồn họ yêu cầu
> vào báo cáo. Câu thường dùng: *"Voices generated using ElevenLabs."*
>
> ⬜ Nếu giọng lấy từ **Voice Library** (giọng thu từ người thật rồi nhân bản) thì xem
> trang giọng đó có yêu cầu ghi tên người cho mượn giọng không.

**Bản chỉ đạo giọng** *(prompt, thông số, lời thoại từng câu)* lưu ở [VOICE_BRIEF.md](VOICE_BRIEF.md)
— tài liệu này cũng là thứ đáng đưa vào báo cáo: nó cho thấy giọng được đặt theo một
thiết kế nhân vật cụ thể, không phải bấm bừa ra rồi dùng.

---

## 4. Âm thanh — 18 file, `Resources/Audio/`

⬜ **Toàn bộ mục này cần bạn điền.** Không có cách nào tra ngược nguồn từ file âm thanh.

| File | Nguồn | Giấy phép |
|---|---|---|
| `0ArmorHIt.ogg`, `0Hit.ogg` | ⬜ | ⬜ |
| `charge.ogg`, `lasershoot.wav`, `launch.mp3` | ⬜ | ⬜ |
| `dash.wav`, `punch.wav`, `death.wav`, `explode.wav` | ⬜ | ⬜ |
| `buy .mp3`, `buy phase.mp3`, `start.mp3`, `tnt convert.mp3`, `useitem.mp3` | ⬜ | ⬜ |
| `click.mp3`, `error.mp3` | ⬜ | ⬜ |
| `GPBG.mp3` *(nhạc nền trong trận)* | ⬜ | ⬜ |
| `pripac-relaxing-vibes-for-gaming-focus-323623.mp3` *(nhạc menu)* | ⬜ **Tên file có mã số — nhiều khả năng từ Pixabay** | ⬜ |

> 💡 **Mẹo:** file cuối có dạng tên `<tác-giả>-<tiêu-đề>-<id>.mp3` — đây đúng kiểu đặt tên khi
> tải từ **Pixabay Audio**. Tra `pripac 323623` là ra ngay. Pixabay dùng giấy phép riêng của họ:
> miễn phí, dùng thương mại được, không bắt ghi nguồn — nhưng **vẫn nên ghi**.
>
> Freesound thì phức tạp hơn: có CC0 (thoải mái), CC-BY (**bắt buộc ghi tên tác giả**), và
> vài loại cấm dùng thương mại. Nếu bạn tải từ đó thì phải xem từng file một.

---

## 5. Nội dung tự tạo — KHÔNG phải bên thứ ba

Ghi rõ ở đây vì đây là **điểm cộng cho bạn**, đừng để lẫn vào mục asset đi mượn.

| Hạng mục | Bằng chứng |
|---|---|
| **`Resources/UI kit/`** — 30 sprite, 16 icon, 5 mockup màn hình *(một số sprite đã chỉnh màu lại bằng tay sau khi sinh)* | ✅ Có thư mục `tools/` chứa `gen_sprites.py`, `gen_icons.py`, `gen_mockups.py`, `gen_styleguide.py`, `gen_lib.py` — toàn bộ sinh ra bằng script, không tải về |
| **`Resources/Gear shop radial kit/`** — icon Shop, Radial Menu, HUD, marker đồng đội | ✅ Có `gen_holo_sprites.py`, `gen_ammo_icons.py`, `gen_hud_assets.py`, `gen_ally_marker.py`, `gen_holo_mockups.py` |
| Toàn bộ mã nguồn trong `Assets/Scripts/` | Xem mục 6 |
| Lời thoại 8 câu giọng thông báo | ✅ Chủ project tự viết. Còn GIỌNG ĐỌC là của ElevenLabs — xem mục 3b |
| Thiết kế gameplay, bản đồ, cân bằng số liệu | Tự làm |
| **`Assets/Textures/CloudPuffs.png`** — ảnh mây cho biển mây quanh đảo (`CloudBank.cs`) | ⚠️ **Tự tạo nhưng DẪN XUẤT** — xem cảnh báo ngay dưới |

> Hai bộ UI ở trên **tự sinh bằng chương trình**, nên vừa không vướng bản quyền, vừa là thứ
> đáng nói trong báo cáo: bạn viết công cụ để sinh asset thay vì đi tải.

> ⚠️ **`CloudPuffs.png` phải nói rõ là ảnh DẪN XUẤT, đừng ghi là "tự vẽ".** Nó được cắt ra
> từ chính ảnh bầu trời **Fantasy Skybox FREE** (tách từng đám mây bằng thuật toán loang
> vùng, chuyển sang thang xám rồi làm mềm rìa), nên bản quyền gốc vẫn thuộc Render Knight.
> Gói FREE cho dùng trong game, kể cả sửa đổi — điều bị cấm là **bán lại chính bộ asset**.
> Nướng vào game thì hợp lệ, nhưng khai là do mình vẽ thì sai sự thật.

**Ngoại lệ trong mục này:** font **Poppins** (`Resources/UI kit/Font/`) là của Google Fonts,
giấy phép **SIL Open Font License (OFL)** — miễn phí kể cả dùng thương mại. Đây là font đi
mượn, không phải tự làm, nên vẫn phải khai báo.

---

## 6. Nội dung sinh bằng AI — nói trước, đừng để bị hỏi

**Model đảo bay** tạo bằng **Meshy AI**. Hai việc cần làm:

1. ⬜ **Kiểm tra điều khoản của Meshy** ứng với gói bạn dùng — gói miễn phí và gói trả phí
   thường khác nhau về quyền sử dụng thương mại.
2. **Chủ động nói ra trong báo cáo và khi bảo vệ.**

**Mã nguồn** được viết với sự hỗ trợ của **Claude (Anthropic)** — các commit đều ghi
`Co-Authored-By: Claude`. Lịch sử git là bằng chứng minh bạch, không phải điểm yếu.

⬜ Nếu trường có quy định riêng về việc dùng AI hỗ trợ, khai báo theo đúng quy định đó.

> Dùng công cụ AI để tạo tài nguyên và hỗ trợ lập trình là chuyện bình thường và hợp lệ.
> Nhưng **giấu đi rồi bị hỏi trúng thì rất bất lợi** — nói trước thì đó chỉ là một lựa chọn
> công cụ, giấu đi mà bị phát hiện thì thành vấn đề trung thực.

---

## Việc còn phải làm — xếp theo mức rủi ro

| # | Việc | Vì sao gấp |
|---|---|---|
| 1 | Tìm lại nguồn bộ **`Nature/`** | Tải từ Google Drive, không có lịch sử tra ngược. Càng để lâu càng khó nhớ |
| 2 | Điền nguồn **18 file âm thanh** | Nếu có file nào CC-BY mà không ghi tên tác giả là **vi phạm giấy phép thật** |
| 3 | Kiểm điều khoản **Meshy AI** | Ảnh hưởng tới model chiếm phần lớn bản đồ |
| 4 | Xác nhận nguồn `Tree9/`, `Rocks and Boulders 2/` | Mở Package Manager → My Assets là ra |
| 5 | Xác nhận giấy phép ParrelSync và QuickOutline | Cả hai gần như chắc chắn MIT, chỉ cần đối chiếu |

---

## Nguyên tắc chung

**Khai báo thừa còn hơn thiếu.** Dùng asset miễn phí là chuyện bình thường ở đồ án —
gần như ai cũng vậy. Nhưng một model đẹp bất thường mà không được nhắc tới trong báo cáo
sẽ khiến hội đồng đặt câu hỏi về cả những phần bạn thật sự tự làm.

Khai báo đầy đủ: mất một dòng trong báo cáo.
Không khai báo: rủi ro mất niềm tin vào toàn bộ đồ án.
