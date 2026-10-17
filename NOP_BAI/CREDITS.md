# TÀI NGUYÊN BÊN THỨ BA — Magneto-Dome

Khai báo toàn bộ asset, thư viện và công cụ không do tác giả tự làm.
Chép mục này vào báo cáo đồ án.

> **Cập nhật 14/10/2026.** Truy được nguồn **toàn bộ model môi trường** và **cả hai thư viện**.
> Cách truy, để hội đồng hỏi thì trả lời được:
> - **Đối chiếu GUID với bộ nhớ đệm Asset Store** (`AppData\Roaming\Unity\Asset Store-5.x`):
>   mỗi gói `.unitypackage` chứa các thư mục đặt tên theo GUID của từng file. GUID trùng
>   khớp với file trong project là chứng cứ chắc chắn — không phải đoán theo tên thư mục.
> - **Đọc chuỗi ký tự còn sót bên trong file 3D** — file `.blend` lưu cả đường dẫn trên máy người làm.
> - **Tra trang gốc / GitHub API** để lấy giấy phép.
>
> - ✅ = đã xác minh **bằng chứng cứ cụ thể**, có ghi cách xác minh
> - ⬜ = **chỉ bạn biết** (bạn dùng gói nào, tải ở đâu) — không có dấu vết nào để tra

---

## 1. Nền tảng & thư viện

| Tên | Nhà cung cấp | Giấy phép | Dùng để làm gì |
|---|---|---|---|
| Unity 6000.3.17f1 | Unity Technologies | Unity Personal | Game engine |
| Universal Render Pipeline 17.3.0 | Unity Technologies | Kèm theo Unity | Đồ hoạ |
| Input System 1.19.0 | Unity Technologies | Kèm theo Unity | Xử lý phím và chuột |
| TextMesh Pro | Unity Technologies | Kèm theo Unity | Chữ trên giao diện |
| **Photon Fusion 2** | Photon Engine (Exit Games) | Gói miễn phí *(tối đa 100 CCU)*, cần tài khoản | Toàn bộ phần mạng nhiều người chơi |
| **ParrelSync** | **VeriorPies** — © 2018 Greg M, © 2020 Ian and Contributors | **MIT** ✅ *(file `LICENSE.md` trên GitHub `VeriorPies/ParrelSync`, tra 14/10)* | Tạo bản sao project để test 2 client. **Chỉ dùng trong Editor, không có trong bản build** |
| NanoSockets | *(kèm trong Photon Fusion)* | ✅ Có sẵn file `libnanosockets_LICENSE.txt` trong `Assets/Photon/Fusion/Plugins/NanoSockets/` | Tầng mạng cấp thấp |

---

## 2. Model, texture, shader

| Thư mục | Nguồn | Giấy phép | Trạng thái |
|---|---|---|---|
| `Resources/Environment/QuickOutline/` | **Chris Nolet** ✅ | **MIT** ✅ — *"Copyright (c) 2018 Chris Nolet"* | ✅ `Readme.txt` trong thư mục ghi tác giả; giấy phép tra qua GitHub API repo `chrisnolet/QuickOutline` ngày 14/10 |
| `Resources/Environment/Island/` | **Meshy AI** *(sinh bằng AI)* | Gói miễn phí: **CC BY 4.0 — bắt buộc ghi "Meshy"**. Gói trả phí: không bắt buộc | Model đảo bay. **Cứ ghi nguồn Meshy** là đúng trong cả hai trường hợp — xem mục 6 |
| `Resources/Environment/Rocks and Boulders 2/` | **Manufactura K4** — gói *"Rock and Boulders 2"* ✅ | Unity Asset Store — **Standard Unity Asset Store EULA** ✅ | ✅ GUID khớp **169/169** file với gói trong bộ nhớ đệm Asset Store (tải 10/08/2026) |
| `Resources/Environment/Tree9/` | **Pixel Games** — gói *"Realistic Tree 9 [Rainbow Tree]"* ✅ | Unity Asset Store — **Standard Unity Asset Store EULA** ✅ | ✅ GUID khớp **19/19** file (tải 10/08/2026). Toàn bộ 14 vật loại Normal trên map là cây này |
| `Resources/Environment/Fantasy Skybox FREE/` | **Render Knight** ✅ | Unity Asset Store — gói **FREE** ✅ | Bầu trời của cả bản đồ (`FS000_Day_03`). `Readme.txt` ghi tác giả + `render-knight.com`. GUID khớp 5/132 file — chỉ giữ lại đúng phần đang dùng |
| `Resources/Environment/Nature/` | **Quaternius** — *"Ultimate Stylized Nature Pack"* (05/2022) ✅ | **CC0** ✅ *(public domain, không bắt ghi nguồn, dùng thương mại được)* | Cỏ, hoa, bụi. Xem cách truy ra ngay dưới |

> ✅ **`Nature/` — từng là mục rủi ro nhất, đã truy ra 14/10.**
> Bộ này tải qua Google Drive (tên thư mục `FBX-20260824T044903Z-1-001` là kiểu Drive tự đặt
> khi nén), nên không có lịch sử Asset Store. Manh mối là file `AllModels.blend` trong Downloads,
> tải **cùng ngày 24/08**. Bên trong file còn sót đường dẫn trên máy của chính tác giả:
>
> `C:\Users\Usuario\Desktop\Work\Blender\_FreeModels\Nature\Ultimate Stylized Nature - May 2022\Textures\Grass.png`
>
> Ba texture `Bush_Leaves.png`, `Flowers.png`, `Grass.png` trong đó **trùng tên** với ba texture
> trong `Nature/Textures`. Trang `quaternius.com/packs/ultimatestylizednature.html` xác nhận:
> tác giả Quaternius, phát hành **May 2022**, giấy phép **CC0**.
>
> Nên ghi tên Quaternius dù CC0 không bắt buộc — nhắc tên người làm là phép lịch sự, và nó
> cho hội đồng thấy bạn truy nguồn tới nơi.

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
| **Giọng đọc** | **ElevenLabs** (sinh bằng AI) | Gói miễn phí: **bắt buộc ghi `elevenlabs.io`**, cấm dùng thương mại. Gói trả phí: có giấy phép thương mại. ⬜ *Bạn dùng gói nào* |

> ✅ **Điều khoản đã tra 14/10** *(trang trợ giúp chính thức của ElevenLabs)*: nội dung tạo bằng
> gói miễn phí phải ghi nguồn bằng chữ **"elevenlabs.io"** hoặc **"11.ai"**, và không được dùng
> vào mục đích thương mại. Đồ án tốt nghiệp là **phi thương mại** nên hợp lệ, miễn là có ghi nguồn.
>
> **Cách an toàn: cứ ghi *"Giọng thông báo tạo bằng ElevenLabs (elevenlabs.io)"*** — đúng với
> cả gói miễn phí lẫn trả phí, khỏi phải nhớ mình dùng gói nào.

> ✅ **Có bằng chứng máy đọc được ngay trong file.** Cả 8 file giọng đều mang **chứng chỉ
> nội dung C2PA** do ElevenLabs nhúng vào, ghi rõ tác giả `"Eleven Labs Inc."` và loại nội
> dung `trainedAlgorithmicMedia` — tức "phương tiện do thuật toán đã huấn luyện tạo ra".
>
> Đây là thứ đáng nhắc trong báo cáo: minh bạch về việc dùng AI **có chứng cứ kiểm chứng
> được**, không phải lời khai suông. Người chấm có thể tự kiểm bằng công cụ đọc C2PA.

> ⚠️ **Đây là giọng AI, không phải người thu — phải nói rõ trong báo cáo.** Gói miễn phí
> của ElevenLabs yêu cầu ghi nguồn, nên đây không phải chuyện "nên ghi cho lịch sự" mà là
> **điều kiện của giấy phép**. Không ghi là dùng sai giấy phép.
>
>
> ⬜ Nếu giọng lấy từ **Voice Library** (giọng thu từ người thật rồi nhân bản) thì xem
> trang giọng đó có yêu cầu ghi tên người cho mượn giọng không.

**Bản chỉ đạo giọng** *(prompt, thông số, lời thoại từng câu)* lưu ở [VOICE_BRIEF.md](VOICE_BRIEF.md)
— tài liệu này cũng là thứ đáng đưa vào báo cáo: nó cho thấy giọng được đặt theo một
thiết kế nhân vật cụ thể, không phải bấm bừa ra rồi dùng.

---

## 4. Âm thanh — 18 file, `Resources/Audio/` *(truy đủ nguồn 15/10)*

> ✅ **Cả 18 file đã có nguồn chính xác — không đoán file nào.** Cách truy: khi trình duyệt tải
> một file từ Internet, Windows gắn vào file một **nhãn ẩn `Zone.Identifier`** ghi lại đúng địa chỉ
> trang đã tải. Nhãn này đi theo file khi kéo vào Unity, nên đọc ra được đúng trang gốc của từng
> file. Giấy phép từng file Freesound tra trực tiếp trên trang của file đó (Freesound **không có
> giấy phép chung** — mỗi file một giấy phép riêng).

### ⚠️ Hai file BẮT BUỘC ghi tên tác giả

| File | Nguồn | Giấy phép | Phải làm |
|---|---|---|---|
| `explode.wav` | *"Electronic bomb.wav"* — **lucas-post**, Freesound `freesound.org/people/lucas-post/sounds/521620/` | **CC BY 4.0** | Ghi tên tác giả |
| `death.wav` | *"Villain death.wav"* — **AlexMurphy53**, Freesound `freesound.org/people/AlexMurphy53/sounds/571387/` | **CC BY-NC 4.0** | Ghi tên tác giả + **chỉ dùng phi thương mại** — đồ án là phi thương mại nên hợp lệ. ⚠️ Nếu sau này bán game thì **phải thay file này** |

### Freesound — CC0 (dùng tự do, không bắt ghi nguồn)

| File | Tên gốc — tác giả | Trang |
|---|---|---|
| `charge.ogg` | *"Charge up"* — SamsterBirdies | `freesound.org/people/SamsterBirdies/sounds/483883/` |
| `dash.wav` | *"Dash Sound Effect"* — ArTiX.0 | `freesound.org/people/ArTiX.0/sounds/742717/` |
| `lasershoot.wav` | *"Retro, Laser Shot 04.wav"* — LilMati | `freesound.org/people/LilMati/sounds/414888/` *(thẻ trong file ghi tên thật Mattias Michael Lahoud — cùng một người)* |
| `launch.mp3` | *"Huge machine gun shoot.mp3"* — soundX360 | `freesound.org/people/soundX360/sounds/441756/` |
| `punch.wav` | *"Punching Bag 2"* — D_Yonqui | `freesound.org/people/D_Yonqui/sounds/864752/` |

### Kenney — CC0

| File | Nguồn |
|---|---|
| `0ArmorHIt.ogg`, `0Hit.ogg` | Bộ **Impact Sounds** của **Kenney** (`kenney.nl/assets/impact-sounds`), giấy phép **CC0** |

### Pixabay — Pixabay Content License (dùng được trong game, không bắt ghi nguồn)

Tên người đăng và số hiệu đọc từ đường dẫn tải gốc *(khuôn Pixabay: `<người-đăng>-<tên-bài>-<số>`)*.

| File | Tên gốc — người đăng | Số hiệu |
|---|---|---|
| `GPBG.mp3` *(nhạc nền trong trận)* | *"Arena"* — audiolibraryinfinite | 270364 |
| `pripac-relaxing-vibes-for-gaming-focus-323623.mp3` *(nhạc menu)* | *"Relaxing Vibes for Gaming Focus"* — pripac | 323623 |
| `buy .mp3` | *"Item Equip"* — freesound_community | 6904 |
| `buy phase.mp3` | *"Cash Register Open"* — Universfield | 567194 |
| `tnt convert.mp3` | *"Pouring Water Into Glass"* — Universfield | 116975 |
| `click.mp3` | *"Mouse Click"* — matthewvakaliuk73627 | 290204 |
| `error.mp3` | *"UI Error Pop"* — soundshelfstudio | 515668 |
| `start.mp3` | *"Game Start"* — người đăng mã số 49447089 | 317318 |
| `useitem.mp3` | *"Camera Release Sound from Exposure"* — conmsutton2016 | 236390 |

> **Bài cũ đã bị gỡ bỏ (28/09):** trước đó nhạc trận là *"In Flight"* của **Alegend** tải từ
> **freetouse.com**. Đọc kỹ điều khoản thì giấy phép miễn phí của họ **cấm dùng trong
> "Digital Products"**, mà game chính là một Digital Product — chỉ gói trả phí mới được.
> Đã xoá file khỏi project và thay bằng bài Pixabay ở trên.
>
> ⚠️ Đáng ghi vào báo cáo như một bài học quy trình: **xoá khỏi ô gán là chưa đủ**, phải
> xoá hẳn file. Thư mục `Resources/` được Unity đóng gói vào bản build **toàn bộ**, kể cả
> file không ai dùng tới - để lại là bản nộp vẫn chứa nội dung không có quyền phát tán.

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
| **Tường EM Barrier** — khiên lục giác, lõi kim loại, vệt sáng dưới đất *(14/10)* | ✅ Không dùng model hay texture nào. Hình dựng bằng mã trong `EMBarrier.cs`, vẻ ngoài vẽ bằng shader tự viết `Resources/Shaders/HexShield.shader` |
| Shader rào chắn đầu round `ForceField.shader`, bầu trời `SkyArenaGradientSky.shader` | ✅ Tự viết, có chú thích giải thích trong file |

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

1. ✅ **Điều khoản Meshy đã tra 14/10** *(trang trợ giúp + bảng giá chính thức)*:
   - Gói **miễn phí**: model thuộc giấy phép **CC BY 4.0** — dùng được, kể cả thương mại,
     nhưng **bắt buộc ghi nguồn Meshy** trong phần mô tả / readme của dự án.
   - Gói **trả phí**: model thuộc về bạn, không bắt buộc ghi nguồn.
   - → **Cứ ghi *"Model đảo bay tạo bằng Meshy AI (meshy.ai)"*** — đúng trong cả hai trường hợp.
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
| ~~1~~ | ~~Tìm lại nguồn bộ `Nature/`~~ | ✅ 14/10 — Quaternius, CC0 |
| ~~2~~ | ~~Điền nguồn 13 file hiệu ứng âm thanh~~ | ✅ 15/10 — truy đủ 18/18 file qua nhãn `Zone.Identifier`. Hai file CC BY đã ghi tên tác giả |
| ~~3~~ | ~~Kiểm điều khoản Meshy AI~~ | ✅ 14/10 — cứ ghi nguồn Meshy là đúng |
| ~~4~~ | ~~Xác nhận nguồn `Tree9/`, `Rocks and Boulders 2/`~~ | ✅ 14/10 — đối chiếu GUID |
| ~~5~~ | ~~Xác nhận giấy phép ParrelSync và QuickOutline~~ | ✅ 14/10 — cả hai MIT |
| ~~6~~ | ~~Tên người đăng 2 bài nhạc Pixabay~~ | ✅ 15/10 — đọc từ đường dẫn tải gốc |

---

## Nguyên tắc chung

**Khai báo thừa còn hơn thiếu.** Dùng asset miễn phí là chuyện bình thường ở đồ án —
gần như ai cũng vậy. Nhưng một model đẹp bất thường mà không được nhắc tới trong báo cáo
sẽ khiến hội đồng đặt câu hỏi về cả những phần bạn thật sự tự làm.

Khai báo đầy đủ: mất một dòng trong báo cáo.
Không khai báo: rủi ro mất niềm tin vào toàn bộ đồ án.
