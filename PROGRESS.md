# TIẾN ĐỘ — Magneto-Dome

> Ghi cho session sau. Đọc file này trước, rồi đọc [CLAUDE.md](CLAUDE.md) để nắm đặc tả và quy tắc.
> **Cập nhật:** 14/10/2026 · **Deadline:** hạn 12/10 đã được lùi — **chưa biết ngày mới**

> ### 📋 DANH SÁCH VIỆC CÒN LẠI ĐÃ CHUYỂN SANG [VIEC_CAN_LAM.md](VIEC_CAN_LAM.md) *(từ 14/10)*
> File này giữ **nhật ký**, file kia giữ **việc phải làm** — đừng chép danh sách việc vào đây nữa.
>
> **Đã xong 14/10:** báo cáo có Danh mục bảng (16) + Danh mục hình (10) tự sinh kèm số trang,
> xoá danh sách bảng gõ tay bị sai, tách 7 ảnh khỏi style chú thích · tạo icon
> `Assets/Textures/GameIcon.png` *(chưa gắn — chờ chủ project làm trong Player Settings)* ·
> sửa bảng scene build trong CLAUDE.md *(SampleScene không còn trong build)* ·
> **build bản nộp chính thức** bằng `Assets/Editor/BuildNop.cs` chạy dòng lệnh: icon đã gắn,
> Development Build tắt (0 file .pdb), ra `Build_Nop/` + `Build_Nop.zip` 214MB. Còn thiếu: chơi thử 2 máy ·
> điền tên GVHD **cô Phạm Thị Hằng** vào báo cáo (2 bìa + Lời cảm ơn, "thầy/cô" → "cô") và bìa slide
> *(sửa `make_slide.js` rồi dựng lại — slide 13 vẫn chưa có video nên không mất gì)* ·
> đổi toàn bộ **"sinh viên" → "học viên"** (4 chỗ trong slide, 1 câu trong báo cáo) cho khớp biểu mẫu trường ·
> nhãn cửa hàng `Buying phase` → `BUYING PHASE` *(sau khi đã build `Build_Nop`)* ·
> **CREDITS truy nguồn bằng đối chiếu GUID với bộ nhớ đệm Asset Store**: đá = Manufactura K4,
> cây = Pixel Games; `Nature` = Quaternius *Ultimate Stylized Nature Pack* (CC0) — truy ra nhờ
> đường dẫn sót trong `AllModels.blend`. ParrelSync, QuickOutline xác nhận MIT. Tra điều khoản
> Meshy (gói free = CC BY 4.0) và ElevenLabs (gói free = ghi `elevenlabs.io`, phi thương mại).
> Phụ lục B báo cáo thêm 3 dòng; slide dự phòng 4 sửa 2 chỗ sai (nhân vật ghi nhầm Asset Store,
> thiếu Meshy). Còn lại: nguồn 13 file hiệu ứng âm thanh — chỉ chủ project biết ·
> **EM Barrier có hình riêng**: tấm khiên ghép ô lục giác lơ lửng 0.3m trên lõi kim loại, màu
> trung tính trắng–xanh lơ, 4×3m. Dựng bằng mã (`EMBarrier.Visual`) + shader mới `HexShield.shader`.
> Có hiệu ứng lan ra lúc thả, gợn sóng khi trúng (đồng bộ mạng qua `HitCount`), nhấp nháy 2 giây
> cuối, tan về lõi. Tường giờ tự dò mặt đất (`InventorySystem.FindGroundBelow`) — trước đó chìm 0.5m.
> Đã xem trước bằng ảnh chụp từ Unity chạy ngầm. **Chưa test trong trận thật.** ·
> **Xoá con bù nhìn khỏi TestScene** (đã có sẵn `Assets/Prefab/Dummy.prefab`, bản trong scene chỉ khác
> tên + vị trí nên không mất gì) · **build lại `Build_Nop` + zip 214MB** — đã có tường mới, nhãn
> `BUYING PHASE`, không còn bù nhìn ·
> **Sửa lỗi rơi trong pha mua đồ**: trước đó vùng chết chỉ kiểm trong pha chiến đấu → rơi lúc mua
> đồ là rơi mãi dưới vực, combat vừa mở thì chết luôn (mất 5s + địch +8). Giờ rơi ngoài pha chiến
> đấu → `RescueToSpawn` kéo về điểm xuất phát, giữ giáp/đồ. Kèm: `CheckRespawns` chạy cả trong pha
> mua (phím K lúc test), `FPSMovement.ClearMotion()` xoá đà rơi mỗi lần dịch chuyển ·
> **Bảng chết mới `DeathScreen.cs`** (thay chữ "DEAD"): ai hất mình + bằng đòn gì (đấm/đạn/TNT,
> ghi qua `PlayerHealth.RegisterHit`, nhớ 6 giây), vòng đếm ngược, "địch +8", mẹo chơi. Đã xem
> trước bằng ảnh chụp. **Chưa test trong trận thật.** Đã build lại `Build_Nop` + zip 214MB (14/10) có cả hai thay đổi.

> **Đã xong 15/10:** bỏ slide demo (slide 13 cũ) — chủ project demo trực tiếp bằng game.
> Deck còn 15 + 4 dự phòng. Mục "06 DEMO TRỰC TIẾP" vẫn giữ trong slide Nội dung để số mục
> 7–9 không lệch; cách demo + mẹo bấm F1 chuyển vào ghi chú người nói của slide 12 ·
> **Tiêu đề slide kiểu pha Việt–Anh**: 3. Tech stack · 4. … — Magnetic Gloves / Ammo Types /
> Overload Mode / Match Format / Netcode · 5. … — Workflow / Top 3 Bugs. 4 slide cuối bỏ chữ
> "Dự phòng N" + dòng "Mở slide này khi hội đồng hỏi…", đổi tên Timeline / Balance Sheet /
> Code Architecture / Credits, kéo nội dung lên cho đỡ trống · trailer xong (`trailer/Magneto-Dome_Trailer_v3.mp4`) ·
> sửa số cũ trên slide: 45 → **49 tệp** (16/10: **50 tệp**, thêm MenuBackdrop) mã nguồn, 20.000 → **21.000 dòng**, báo cáo 48 → **53 trang**;
> ghi chú slide Kết quả thay "hai ô đỏ phải đo" bằng số đo thật (85 FPS 1920×1080, ping 16 ms).
> **Chủ project đã chơi thử trọn trận 2 máy bằng `Build_Nop` (15/10): ổn** — gồm EM Barrier mới,
> bảng chết mới, kéo về khi rơi lúc mua đồ. Buổi demo: có người chơi cùng làm đối thủ, mạng đã kiểm.
> **Truy đủ nguồn 18/18 file âm thanh** qua nhãn ẩn `Zone.Identifier` Windows gắn vào file tải
> từ Internet (ghi URL trang gốc). 7 Freesound (5 CC0; `explode.wav` lucas-post **CC BY 4.0**, `death.wav`
> AlexMurphy53 **CC BY-NC 4.0** — bắt buộc ghi tên), 2 Kenney CC0, 9 Pixabay. Đã thêm vào CREDITS,
> Phụ lục B báo cáo (vẫn 53 trang) và slide Credits.

> **Đã xong 16/10 — làm mới MenuScene:** nền là **cảnh 3D thật** của hòn đảo (bản sao chỉ có hình,
> 4.190 vật thể, 0 object mạng) với camera bay vòng chậm (`MenuBackdrop.cs`), ánh sáng + bầu trời +
> sương mù chép từ TestScene. Logo chữ MAGNETO / D O M E / gạch đỏ–xanh / "2V2 MAGNETIC ARENA".
> Menu chính dời sang trái để lộ đảo; lớp phủ tối dần về bên trái (`MenuOverlay.png`); màn hình phụ
> có lớp tối nhẹ cho dễ đọc. Sửa thanh cuộn Room List hình chữ L, sửa chữ mẫu đội Đỏ/Xanh bị ghi
> ngược trong scene. Thay Hình 3.3 + 3.4 trong báo cáo bằng ảnh menu mới. Sửa bằng công cụ Editor
> chạy ngầm (đã xoá sau khi xong). **Chưa test trong Unity / bản build. `Build_Nop` CHƯA có menu mới.**
> · Menu: **nắng xoay theo camera** (đảo không bao giờ ngược sáng) + **chỉnh màu tông xanh** chỉ cho menu.
> · **Phòng chờ mới `LobbyView.cs`**: thẻ người chơi, YOU, vương miện chủ phòng, mã phòng to + COPY,
>   "Waiting for host to start..." cho Client. **Chỉ chủ phòng được đổi đội** (chặn ở RPC phía Host);
>   Host bấm thẻ để chuyển người khác sang đội kia. `RoomPlayer.IsHost` mới (`[Networked]`).
>   Hình 3.4 báo cáo thay bằng phòng chờ mới.
> · **Settings tạm ẩn giao diện phía sau** khi mở; nền bảng đổi sang màu trơn (cả 2 scene).
> · Xoá chữ `z` thừa gõ nhầm cuối `RoomPlayer.cs` (11:25) — có nó là cả project không biên dịch được.
> · Sương mù trong trận: đã chụp thử 4 mức, khác biệt nhỏ — **chờ chủ project chỉ ra chỗ xa bị xấu**.
> · Báo cáo: sửa câu "người chơi chọn đội" thành luật mới (chủ phòng xếp đội), cập nhật lại mục lục (vẫn 53 trang).
> · **Đã build lại `Build_Nop` + zip 214MB (16/10)** — có menu 3D, phòng chờ mới, Settings ẩn nền, luật đổi đội.
> · **Rà GDD toàn bộ (16/10)** — nhiều chỗ cũ từ 24/08: tốc độ 13→**7**, dash 100→**70**, tiền đầu 500→**$300**,
>   tầm cận chiến 4m/4–20m→**6m/6–60m**, grapple 100→**250**, Z/X/C ô C là **TNT**, thêm phím Space/F/Esc/F1/F4, lực văng ×5→**×4**,
>   "chưa có danh sách phòng"→**đã có**, bầu trời là Fantasy Skybox (không còn Gradient Sky), danh sách lớp thêm 14 lớp mới.
>   Kéo theo: `CLAUDE.md` (tầm cận chiến 3m/3–8m sai, phím `B` ghi "chưa làm", thiếu `F`/`Esc`), báo cáo ("ba mét"→sáu mét,
>   Phụ lục A hệ số lực đẩy 0.975→0.95), slide (3m/3–8m→6m/6–60m), `BALANCE.md` (thêm mục Kinh tế + `dashLockRange`).
> · **Đường bay đạn (16/10):** tốc độ +20% (đẩy 50→60, bắn 45→54; hút giữ nguyên 49); đạn đang bay
>   rơi chậm (`bulletGravityMultiplier` 2→0.5, ~76m thay vì ~30m); vật hết làm đạn rơi nhanh tới khi
>   chạm đất (`afterBulletGravityMultiplier` 2.5, ô mới). Báo cáo 2.5.2 vốn viết "giảm trọng lực khi
>   bay" — giờ code mới khớp (trước đó code lại TĂNG ×2).
> · **Sửa nút Close bảng Settings bấm không ăn:** khung `Panel` nằm sau nút trong danh sách nên chặn
>   cú bấm. Sắp lại thứ tự trong `SettingsPanel.prefab` (dùng chung menu + trong trận). Ghi bẫy #16.
> · **Nút Quit Game trong Settings (16/10):** cạnh nút Close, bảng hỏi lại "QUIT GAME?" phủ tối toàn
>   màn hình. Dựng bằng code (`SettingsUI.BuildQuitUI`) từ bản sao nút Leave Match + `LeaveConfirmPanel`
>   nên cùng tông, không phải sửa prefab. Editor thì dừng Play, bản build thì `Application.Quit()`.
> · **Sửa bảng "Leave match?" đếm ngược đè tiêu đề:** số giây chuyển sang nút (`Confirm (3)`), bảng nới
>   500 → 620, câu cảnh báo ép một dòng (dài quá thì tự thu nhỏ chữ).
> · **Đã build lại `Build_Nop` + zip 214MB (16/10, 21:07)** — có đủ: đạn mới, nút Close, Quit Game, bảng rời trận.
> · **Sửa chữ "MOVE TO RED/BLUE" trên thẻ phòng chờ lòi ra ngoài viền thẻ** (sau bản build 21:07): khung chữ
>   lùi vào thẳng mép với chữ YOU, nới 200 → 240.
> · **Thể thức 1v1 / 2v2 + nút Kick (16/10, sau bản build 21:07):** ghép trận nhanh hỏi 1v1 hay 2v2
>   (`ModePicker.cs` mới, tự dựng bằng code); phòng chờ có nút 1V1/2V2 cho chủ phòng; phòng đông hơn sức
>   chứa thì không đổi được, phải kick bớt; nút KICK bấm hai lần; Room List ghi thể thức + sức chứa thật.
>   Phòng 1v1 chặn người thứ 3 bằng `OnConnectRequest`. Đã kiểm bằng ảnh chụp, **chưa thử 2 máy**.
>   Báo cáo: sửa mục 1.2.3 (bỏ câu "một chế độ chơi duy nhất 2v2"), mục đường vào trận, phòng chờ,
>   Bảng 3.6, chụp lại Hình 3.4 — vẫn 53 trang, mục lục đã cập nhật. Slide + GDD: 51 → **52 tệp**, ~22.500 dòng
>   (sinh lại .pptx).
> · **Đã build lại `Build_Nop` + zip 214MB (16/10, 21:53)** — có thêm 1v1/2v2 + Kick và sửa chữ "MOVE TO".
> · **Cải thiện phòng chờ (sau bản build 21:53):** người bị kick không vào lại được phòng (sổ đen theo
>   `ConnectionToken`); dòng báo "X joined / left / was kicked", "Host switched the room to 1v1",
>   "Blue team is full"; gợi ý dưới nút Start khi lệch người hoặc ở một mình (không chặn); phòng 1v1 thẻ nằm
>   giữa vùng thẻ. Kiểm bằng ảnh chụp, **chưa thử 2 máy**.
> · **Cú đẩy (chuột trái) chếch lên một góc cố định ~1.9°:** `pushUpwardBias` 0.055 → **0.033** (3/5, chủ project
>   chọn), và góc không còn đổi theo hướng nhìn (trước đó nhìn dốc xuống thì chếch tới ~30°). `pushLiftOffset` 0.3 → **0.5** và đổi nghĩa thành "nâng TỚI 0.5m tính từ mặt
>   đất" (hàm `GroundGap`) — vật đang nảy trên không không bị giật lên thêm.
> · **Rải lại đạn trên TestScene (16/10):** vẫn **38 viên** (23 đá Heavy + 15 cây Normal), dời 8 viên:
>   `Rock6C (1)` nằm TRÊN điểm spawn Đỏ và `Rock6B` sát spawn Xanh → đưa vào mép trong khu chiếm đóng (khu
>   trước đó không có viên nào); `Rock1D (1)` từ góc căn cứ Đỏ → mép trái vùng giữa (đang trống); 5 vật chồng
>   lên rào chắn → lùi vào căn cứ (chủ project thử thì chồng rào không sao, nhưng vị trí mới vẫn ổn nên giữ).
>   Phân bố mới: căn cứ Đỏ 10 · căn cứ Xanh 10 · giữa-trái 8 · giữa-phải 8 · trong khu 2. Mép vật gần nhất
>   cách người spawn ≥ **2,4 m**, không cặp nào đè nhau. Xoay **ngẫu nhiên trục Y** cho 32 viên đứng thẳng,
>   6 viên nghiêng cố ý (`Rock1D (4)`, `Rock4A (1)`, `Rock4B (2)`, `Rock5B (2)`, hai `Rock6C` lật úp) giữ nguyên;
>   hạ các viên lơ lửng cho chạm đất (`Rock1B (1)` hở 0,31m...). Sao lưu scene cũ:
>   `C:\mdtmp\map\TestScene_TRUOC_KHI_DOI_DA.unity`. ⚠️ Bản sao đảo ở MenuScene vẫn mang vị trí đá CŨ (chỉ là nền).
> · **Nâng cấp rào chắn + giấu giới hạn bản đồ (17/10):** rào thành màn năng lượng theo màu đội, đặc ~99% *(19/10 đổi sang kính mờ — xem mục trên)*
>   (trước trận hai đội không thấy nhau — đã thử với người nộm đặt phía sau), lưới lục giác, vạch sáng chân
>   tường, gợn sóng khi chạm, tan rã từ dưới lên khi vào trận (va chạm vẫn tắt ngay). Không thấy đầu tường vì
>   chỉ tan ngoài mép đảo. Biển mây: thêm **đáy mây** trải tới chân trời, làm mềm viền ảnh mây (có cụm cạnh
>   thẳng), cụm mây mờ khi camera lọt vào. Shader mới `BarrierCurtain`, `CloudFloor`; ảnh `CloudPuffs.png`
>   bật Read/Write. Không sửa scene — mọi thứ dựng bằng code. Kiểm bằng ảnh chụp, **chưa đo FPS** (đáy mây
>   và rào đều là lớp trong suốt phủ rộng; tụt FPS thì tắt `CloudBank.buildCloudFloor` trước). Việc "sương mù
>   giới hạn tầm nhìn" treo từ 16/10 coi như được thay bằng đáy biển mây.
>   Thêm cùng ngày: tường chạy tiếp xuống dưới vách đảo, chìm dần vào biển mây (đứng mép đảo nhìn xuống vẫn thấy).
>   Mỗi ô lục giác lệch màu nhẹ (sáng tối ±20%, sắc ±6°) + vài ô thỉnh thoảng loé sáng — chủ project thấy các ô đều màu quá.
> · **Nâng hiệu ứng tia nạp điện + cú đấm (17/10, sau bản build 18:14):** tia laze có 2 sợi sét lách tách
>   quấn quanh (`ArcCrackle`), vật trúng loé vòng sóng + tia lửa (`CombatVFX.ChargeBeam`). Cú đấm trúng nổ
>   to hơn và **to dần theo điện tích nạn nhân** — chớp sáng, hai vòng sóng lệch nhịp, 9→19 tia lửa
>   (`CombatVFX.PunchImpact`). **Khựng hình** 0,06 giây trên máy người đấm (`PlayerMagnetController.hitStopDuration`,
>   `FirstPersonViewmodel.RequestHitStop`) — chỉ dừng tay viewmodel + giật camera, KHÔNG dùng `Time.timeScale`
>   (dừng thời gian trên Host là lệch mô phỏng mạng). Chờ nắm đấm duỗi ~30% mới khựng.
> · **Nổ TNT + can xăng có hiệu ứng (17/10, sau bản build 18:14):** `CombatVFX.Explosion` — chớp trắng lớn,
>   quả cầu lửa vàng→cam→đỏ, khói xám bốc lên ~2 giây, tàn lửa văng thành vệt, vòng sóng ĐÚNG bán kính sát
>   thương (giữ như cũ) + vòng trắng loang nhanh; camera rung theo khoảng cách (≤ 3 lần bán kính). Can xăng:
>   `CombatVFX.GasolineSplash` lúc đổ (xăng hổ phách bắn tung + chớp lửa + vòng cam) và **tàn lửa + quầng cam
>   lách tách liên tục** trên vật suốt lúc còn là thùng nổ (`CreateHazardEmbers`, bay theo khi bị ném; tắt khi
>   cất túi / đã nổ / sang round). Lần đầu dùng **Particle System** dựng bằng code (lửa/khói cần đốm mềm) —
>   ~60 hạt mỗi vụ nổ, tự huỷ. Bẫy gặp: khói (che sáng) vẽ đè lên trước lửa → lỗ tối giữa quả cầu lửa; chữa
>   bằng `ParticleSystemRenderer.sortingFudge` (lửa -20, khói +20).
> · **Hiệu ứng màn hình khi tự dùng vật phẩm buff (17/10):** `BuffScreenFX.cs` mới. Nước tăng lực: viền loé xanh
>   chanh + vệt tốc độ ở rìa + góc nhìn phình +8° một nhịp (`CameraShake.AddFovPunch`, gọi qua
>   `FPSMovement.PunchFov`); suốt 15 giây hiệu lực viền phập phồng nhịp tim, **3 giây cuối nháy nhanh**. Bộ xả
>   điện: viền xanh lơ + tia điện chạy quanh mép + rung nhẹ. Giữa màn hình luôn để trống. Chỉ người dùng thấy.
>   **Sửa lỗi cũ:** tiếng "dùng đồ" trước đây chỉ phát trên máy Host (`UseConsumable` chạy trên Host mà lại xét
>   `HasInputAuthority`) → Client dùng đồ không nghe gì. Giờ đi qua bộ đếm `[Networked] ConsumableUseCount` tới
>   đúng máy người dùng. Slide + GDD: 52 → **53 tệp**, ~24.000 dòng (sinh lại .pptx).
> · **Rào chắn nhạt màu lại (17/10, sau commit 037ea53):** thân màn sáng hơn + pha trắng (`_HazeDark` 0.045→0.11,
>   ô mới `_HazeBright` 0.36, `_HazePale` 0.28). Độ đặc giữ nguyên.
> · **Đã build lại `Build_Nop` + zip 214MB (19/10, 18:33)** — có thêm màu rào nhạt. Đã push lên GitHub.
> · **Rào chắn thành KÍNH MỜ + che viền địch (19/10, sau bản build 18:33):** chủ project muốn rào trong suốt
>   hơn "kiểu Valorant nhưng đậm hơn chút" — giờ thấy dáng phía sau nhưng nhoè 8 điểm + méo theo khói + ám
>   màu đội (lấy từ Opaque Texture, đã bật sẵn). **Sửa lỗi lộ viền địch xuyên rào:** viền `OutlineVisible`
>   của QuickOutline chỉ bị che bởi thứ GHI CHIỀU SÂU, mà rào trong suốt trước đó không ghi. Thêm lượt
>   `BarrierDepth` ghi chiều sâu ở phần tường đậm + đưa tường về `renderQueue` 2995 (trước mọi vật trong
>   suốt) → viền địch, **bảng tên trên đầu** (phát hiện thêm lúc chụp thử), hiệu ứng phía sau đều bị che;
>   viền đồng đội vẫn xuyên. Kiểm bằng ảnh chụp với model Player thật.
> · **Đã build lại `Build_Nop` + zip 214MB (19/10, 20:42)** — có rào kính mờ + che viền/tên địch. Đã push.
> · **Đã build lại `Build_Nop` + zip 214MB (17/10, 21:35)** — có thêm hiệu ứng tia nạp điện, cú đấm + khựng hình,
>   nổ TNT, can xăng, hiệu ứng dùng đồ.
> · **Đã build lại `Build_Nop` + zip 214MB (17/10, 18:14)** — có đủ: 1v1/2v2 + Kick, cải thiện phòng chờ, cú đẩy ngang,
>   map rải lại đá, rào chắn mới, biển mây vô tận. Shader `Barrier Curtain` + `Cloud Floor` đã vào bản build.

> ### 🗄️ *(CŨ — 25/09, giữ để tra cứu)* CÒN ĐÚNG NHỮNG VIỆC NÀY
> | # | Việc | Ai làm |
> |---|---|---|
> | 1 | **Viết báo cáo** — chưa có chữ nào. Rủi ro lớn nhất | Chủ project |
> | 2 | **Chơi trọn một trận 2 máy** | Chủ project |
> | 3 | **Icon game** (Player Settings → Default Icon) — đang là logo Unity | 5 phút |
> | 4 | Ô **`knockbackText`** trên HUD còn trống — hệ số văng `x2.4` không hiện | 10 phút |
> | 5 | **18 ô ⬜ trong CREDITS** — chủ yếu nguồn 18 file âm thanh. Có file CC-BY mà không ghi tác giả là vi phạm giấy phép thật | Chủ project |
> | 6 | *(tuỳ chọn)* 2 câu giọng `Charge critical.` / `Ten seconds.` — code đã móc sẵn, chỉ thiếu file | Xem [VOICE_BRIEF.md](VOICE_BRIEF.md) |
> | 7 | *(tuỳ chọn)* Trang trí đảo bằng `EnvironmentScatter`, ~1 tiếng | Chủ project |
> | 8 | Build cuối: **bỏ tick `Development Build`** | Chủ project |
>
> **Đã xong 25/09:** nerf lực đẩy 25% · round 5 phút, chiếm khu 60 giây · bất tử 2 giây
> sau hồi sinh (kèm nhấp nháy) · đồng hồ đỏ + đập nhịp 10 giây cuối · cầm đá không còn
> che màn hình · sửa lỗi hiện hai mã phòng · giọng thông báo ElevenLabs · CREDITS sửa lại
> cho đúng nguồn giọng.
>
> **Đã xong 23/09:** phiên bản `1.0.0` · CREDITS bổ sung Fantasy Skybox.

---

## ⭐ VIỆC CẦN LÀM — chốt ngày 30/08 (còn 17 ngày)

Đã build `.exe` thành công và test sơ bộ. Art ổn. **Toàn bộ code đã xong** —
việc còn lại gần như chỉ là kéo thả trong Unity và test.

### 🔴 Phải làm

| # | Việc | Ở đâu | Thiếu thì sao |
|---|---|---|---|
| 1 | **Chơi trọn một trận, 2 máy** | — | Rủi ro số 1. Khu chiếm đóng + hồi sinh + Quá Tải chưa từng chạy đủ một vòng có người thật |
| 2 | **Bắt đầu viết báo cáo** | — | Rủi ro số 2. Chưa viết chữ nào. Chương "Khó khăn & giải pháp" chép thẳng được từ 11 cái bẫy trong CLAUDE.md mục 5 |
| ~~3~~ | ~~Gán 5 icon vào `ItemData`~~ | — | ✅ **XONG** — cả 5 file đều đã có `itemIcon` |
| ~~4~~ | ~~Dựng Shop UI + Radial Menu~~ | — | ✅ **XONG** — còn mỗi ô  bỏ trống |
| ~~5~~ | ~~Thêm **nút mở Settings** ở MenuScene~~ | — | ✅ **XONG** *(kiểm 31/08)* — xem bên dưới |
| 6 | Gán 2 ô âm thanh: `sfxRoundWin`, `sfxRoundLose` | AudioManager (MenuScene) | Thắng/thua round im lặng. ⚠️ *(19/09)* Project **chưa có file** nào cho việc này — phải tải về trước |
| 14 | **Icon cho game** *(19/09)* | Player Settings → Icon | `.exe` và taskbar đang hiện logo Unity mặc định — người chấm thấy đầu tiên |

### 🟡 Nên làm

| # | Việc | Ghi chú |
|---|---|---|
| ~~7~~ | ~~Cắt `Nature/Textures` 194MB~~ | ✅ **XONG** — thư mục đã xoá và chưa từng vào git |
| 8 | `knockbackText` trên HUDController | Hiện hệ số `x2.4`, đang bỏ trống |
| 9 | Trang trí nốt: bụi, hoa, đá bay lơ lửng dưới đảo | Công cụ `EnvironmentScatter` đã sẵn |
| ~~13~~ | ~~Bảng chữ soi vật đặt sai chỗ với vật quá to / quá nhỏ~~ | ✅ **XONG 19/09** — chọn hướng 2: `renderer.bounds.max.y` + `SmoothDamp` riêng độ cao. Chưa test |

#### 🔍 Việc 13 — bảng chữ soi vật bố trí chưa ổn *(ghi nhận 01/09, chưa sửa)*

Triệu chứng: `MagneticObjectInspector` đặt bảng chữ nổi trên đầu vật, nhưng vật to và vật nhỏ
cho ra kết quả lệch hẳn nhau — cái thì chữ lửng lơ cách vật cả mét, cái thì chữ dính vào vật.

**Nguyên nhân đã biết.** Code đang tính:

```csharp
labelRoot.position = Current.transform.position + Vector3.up * (radius + labelHeightOffset);
```

mà `GetBoundingRadius()` trả về **nửa ĐƯỜNG CHÉO** của hộp bao (`worldSize.magnitude * 0.5f`),
không phải nửa chiều cao. Với vật **bẹt và rộng** — cái bàn 3m × 3m × 0.1m — đường chéo dài
tới 4.2m nên nửa của nó là 2.1m, trong khi vật chỉ cao 5cm. Bảng chữ bay lơ lửng cách mặt bàn
hơn 2 mét. Ngược lại vật nhỏ tròn thì đường chéo xấp xỉ chiều cao nên lại vừa đúng — đó là lý do
lúc đúng lúc sai chứ không sai đều.

**Hai hướng chữa, chưa chọn:**

| Cách | Được | Mất |
|---|---|---|
| Dùng `localBounds.extents.y × lossyScale.y` | Đúng chiều cao thật | Sai khi vật bị xoay nghiêng 90° |
| Dùng `renderer.bounds.max.y` (hộp bao thế giới) | Luôn đúng đỉnh thật | Phình/co khi vật xoay → chữ nhấp nhô lúc vật đang bay |

Hướng thứ hai kèm làm mượt (`Lerp` vị trí) có lẽ là đúng nhất, nhưng phải test mới biết.

⚠️ Đừng quay lại dùng `renderer.bounds` cho việc ĐO KÍCH THƯỚC — bẫy số 9 ở CLAUDE.md vẫn
đúng nguyên. Ở đây là bài toán khác: **định vị theo đỉnh**, không phải đo cỡ.

### ⚫ Tuần cuối, đừng làm sớm

| # | Việc | Vì sao đợi |
|---|---|---|
| ~~10~~ | ~~Xoá phím debug `K`~~ | ✅ **19/09** — không xoá mà bọc `#if UNITY_EDITOR` ở cả đầu gửi (`OnInput`) lẫn đầu nhận (`PlayerMagnetController`). Bản build không còn phím K, Editor vẫn test được |
| 11 | Đặt lại `Points To Win = 5` nếu có hạ để test | |
| 12 | Build cuối, **bỏ tick** `Development Build` | |

---

## 🔥 Ngày 11/10 — TỤT LẠI CÒN 27 FPS, TÌM RA LÀ DO BIỂN MÂY

**Đợt tối ưu thứ hai. Đọc cùng chương 01/09 bên dưới — hai đợt này ghép lại thành câu
chuyện hoàn chỉnh cho mục 3.5 báo cáo: tối ưu xong rồi vẫn có thể tụt lại vì tính năng mới.**

### Triệu chứng
Đo bằng Xbox Game Bar trên bản build, **chạy một bản duy nhất**, CPU và RAM đều còn dư:
**trung bình 27 FPS**, đỉnh khoảng 30. Trong khi mốc đo ngày 01/09 là **56 FPS**.

### Suy luận
CPU còn dư mà khung hình vẫn thấp ⇒ **nghẽn ở GPU**. Mà GPU nghẽn trong khi scene chỉ có
**1 đèn thời gian thực** và khoảng 1 triệu tam giác ⇒ không phải ánh sáng, không phải hình
học ⇒ còn lại **fill rate** (tô pixel).

Git cho biết sau mốc 01/09 có 8 script mới, trong đó commit **14/09 "mây quanh đảo"** thêm
`CloudBank` — đúng loại tải gây nghẽn fill rate.

### Vì sao biển mây đắt

| Ô | Trước | Vấn đề |
|---|---|---|
| `puffCount` | 700 | 700 tấm phẳng **trong suốt** xếp chồng → overdraw khổng lồ |
| `puffSizeFar` | 150 | Mỗi tấm phủ một mảng lớn màn hình |
| `softParticles` | bật | Bắt bật ảnh độ sâu + đọc độ sâu cho **từng pixel mây** |

Nặng thêm: cả biển mây là **một mesh duy nhất** có hộp bao rộng ~2400 đơn vị, nên Unity
**không bao giờ cắt bỏ được phần ngoài tầm nhìn** — quay đi đâu cũng vẽ đủ.

Chính tooltip trong `CloudBank.cs` đã ghi sẵn: *"`puffCount` — ĐÂY LÀ Ô CỨU HỘ KHI TỤT FPS."*

### Đã sửa ngày 11/10

| Ô | Trước | Sau | Ở đâu |
|---|---|---|---|
| `puffCount` | 700 | **320** | `TestScene` → `CloudBank` |
| `puffSizeFar` | 150 | **125** | `TestScene` → `CloudBank` |
| `softParticles` | 1 | **0** | `TestScene` → `CloudBank` |
| Camera `Anti-aliasing` | SMAA | **None** | `Player.prefab` → Camera |

⚠️ **Ô khử răng cưa là lỗi trả tiền hai lần.** `PC_RPAsset` đã bật **MSAA 2×** (phần cứng),
mà camera lại bật thêm **SMAA** (một lượt xử lý toàn màn hình nữa) — hai thứ làm cùng một
việc. Bỏ SMAA **không mất gì về hình ảnh** vì MSAA vẫn chạy.

### Đã loại trừ, khỏi mất công tìm lại
- **Hậu xử lý**: `MotionBlur` có `intensity = 0` nên URP bỏ qua hẳn. `TestScene` không có `Volume` nào.
- **Viền sáng QuickOutline**: mặc định `enabled = false`, chỉ bật khi vật được nạp điện.
- **Đèn điểm theo vật** (`MagneticAura.auraPointLight`): không prefab đạn nào gán.

### Kết quả đợt 1: 27 → **38 FPS**

Khá hơn nhưng vẫn dưới mốc 56 của ngày 01/09. Đào tiếp mới lòi ra nguyên nhân thật.

### 🎯 NGUYÊN NHÂN LỚN NHẤT: ĐANG ĐO Ở 1440p, KHÔNG PHẢI 1080p

Máy thử nghiệm: **AMD Radeon RX 6600**, i5-12400F, màn hình **2560 × 1440**.

Card này thừa sức chạy game low-poly. Vấn đề là `ProjectSettings.defaultIsNativeResolution`
đang bật, nên **bản build tự mở ở đúng độ phân giải màn hình** — tức 1440p.

| Độ phân giải | Số điểm ảnh | So với 1080p |
|---|---|---|
| 2560 × 1440 | 3.686.400 | **178%** |
| 1920 × 1080 | 2.073.600 | 100% |

Game đang nghẽn ở khâu tô điểm ảnh, nên FPS gần như **tỉ lệ nghịch với số điểm ảnh**.
38 FPS ở 1440p quy về 1080p là khoảng **67 FPS** — tức chỉ tiêu ≥60 đạt được, chỉ là
trước giờ đo nhầm chỗ.

→ **Đã đổi `defaultIsNativeResolution` thành `0`** (11/10). Bản build giờ luôn mở ở
**1920 × 1080**, không phụ thuộc màn hình của người chấm. Người chơi vẫn đổi được trong
`Settings → Resolution` (dropdown đã gán sẵn ở cả hai scene).

> 📌 **Bài học cho báo cáo: chỉ tiêu FPS phải đi kèm độ phân giải.** Ghi "≥60 FPS" mà
> không nói ở độ phân giải nào là câu không kiểm chứng được. Ghi
> **"≥60 FPS ở 1920×1080"** mới là một chỉ tiêu đúng nghĩa.

### 🎯 THỦ PHẠM THỨ HAI: SSAO CHẠY Ở ĐỘ PHÂN GIẢI ĐẦY ĐỦ

GPU báo **96% tải** mà chỉ ra 38 FPS trên một RX 6600 với scene low-poly — con số đó tự nó
nói rằng còn thứ gì đó nặng bất thường, không phải chỉ tại độ phân giải.

Tìm ra: `PC_Renderer.asset` đang bật **Screen Space Ambient Occlusion**, với cấu hình đắt
gần như tối đa:

| Ô | Giá trị | Vì sao đắt |
|---|---|---|
| `Downsample` | **0** | AO tính ở **độ phân giải đầy đủ** — tức toàn bộ 1440p |
| `AOMethod` | **0** = Blue Noise | Phương pháp đẹp hơn nhưng nặng hơn Interleaved Gradient |
| `Source` | **1** = DepthNormals | **Bắt vẽ lại TOÀN BỘ hình học một lượt nữa** để lấy depth + normal |
| `AfterOpaque` | **0** | Chạy trước pha opaque nên lượt vẽ phụ kia là bắt buộc |
| `BlurQuality` | **0** = High | Lượt làm mờ đắt nhất |

Nặng nhất là dòng `Source: DepthNormals`: hòn đảo ~1 triệu tam giác **bị raster hoá hai
lần mỗi khung hình**, một lần cho prepass của SSAO và một lần cho pha vẽ thật. Cộng thêm
lượt AO toàn màn hình và lượt blur chất lượng cao, ở 1440p là vài mili giây mỗi khung.

→ **Đã TẮT (`m_Active: 0`) ngày 11/10.**

Với tạo hình low-poly, một đèn định hướng và bầu trời sáng, `Intensity` chỉ 0.4 — phần
bóng tiếp xúc trong khe gần như không nhìn thấy. Không đáng đánh đổi một phần tư ngân
sách khung hình.

> 🔁 **Muốn bật lại mà vẫn nhẹ:** `Downsample: 1` *(AO ở nửa độ phân giải)* ·
> `AOMethod: 1` *(Interleaved Gradient)* · `BlurQuality: 2` *(Low)*. Bộ này rẻ hơn khoảng
> bốn lần mà nhìn gần như không khác.

### ✅ KẾT QUẢ CUỐI: 85 FPS

| Mốc | FPS | Hoàn cảnh |
|---|---|---|
| Trước đợt 1 | **27** | 2560×1440, SSAO bật, 700 cụm mây |
| Sau khi dọn mây + bỏ SMAA | **38** | 2560×1440 |
| Sau khi tắt SSAO + về 1920×1080 | **85** | 1920×1080, bản đóng gói |

**Vượt chỉ tiêu ≥60.** Ghi vào báo cáo là **"85 FPS ở 1920×1080"** — luôn kèm độ phân giải,
vì chính bản build đó chạy ở 1440p chỉ được 38.

Đã điền vào: `BaoCao_MagnetoDome.docx` *(bảng nghiệm thu + đoạn mục 3.5)* và
`Slide_MagnetoDome.pptx` *(slide 7 và slide 4)*.

### Nếu vẫn chưa đủ, hạ tiếp theo thứ tự này
1. `CloudBank.outerRadius` 500 → 380
2. `PC_RPAsset.m_SoftShadowQuality` 3 → 1
3. `PC_RPAsset.m_MainLightShadowmapResolution` 2048 → 1024
4. `PC_RPAsset.m_MSAA` 2 → Disabled *(lúc này mới bật lại FXAA trên camera)*

> 📏 **Đo ở cùng một chỗ đứng, cùng hướng nhìn** — tốt nhất đứng giữa đảo nhìn ra chân
> trời, chỗ nhiều mây nhất. Khác góc nhìn thì các con số không so với nhau được.

---

## 🚀 Ngày 01/09 — TỐI ƯU HIỆU NĂNG (chương hay nhất cho báo cáo)

**Kết quả: 20 FPS → 56 FPS, 7 triệu tam giác → 1 triệu.** Đây là câu chuyện có số liệu
trước/sau rõ ràng nhất của cả project — nên đưa thẳng vào báo cáo.

### Cách tìm ra, theo đúng thứ tự

Quan trọng không kém kết quả: **loại trừ bằng số đo, không đoán.**

| Bước | Đo được | Loại trừ được gì |
|---|---|---|
| 1 | Batches 100–300 | Không phải nghẽn lệnh vẽ. Static batching + SRP Batcher đang chạy tốt |
| 2 | Chỉ **1 đèn** thời gian thực | Không phải ánh sáng |
| 3 | Vật lý 50Hz, `AutoSyncTransforms` tắt | Không phải vật lý |
| 4 | Texture giới hạn 2048, đã nén | Không phải VRAM |
| 5 | **Tris 7 triệu** | 👉 Thủ phạm: hình học |

### Thủ phạm: hòn đảo Meshy AI

File FBX **52.7 MB**, khoảng 2 triệu tam giác, **không có LODGroup nào** trong scene.
Model do AI sinh ra không hề được giản lược — nó làm ra để nhìn ngắm, không phải chạy game.

Tệ hơn: `m_ShadowCascadeCount: 4` khiến hòn đảo được vẽ lại **5 lần mỗi khung hình**
(1 lần hiển thị + 4 lần cho bốn tầng bóng đổ).

**Chữa:** Blender → Decimate → xuất đè. **52.7 MB → 2.56 MB**, giữ nguyên file `.meta`
nên GUID không đổi, mọi tham chiếu trong scene và prefab vẫn nguyên.

### Đã hạ 4 ô trong `PC_RPAsset.asset`

```
m_ShadowCascadeCount          4    -> 2     ← lớn nhất, bớt 2 lượt vẽ toàn hình học
m_ShadowDistance              85   -> 55
m_MSAA                        4    -> 2
m_MainLightShadowmapResolution 4096 -> 2048
```

### Ba lỗi hiệu năng trong CODE, đã sửa

**1. Game KHÔNG hề giới hạn khung hình** *(nguyên nhân nóng máy)*
`vSyncCount = 0` và không chỗ nào đặt `Application.targetFrameRate`. GPU đẩy 600–1500
khung hình mỗi giây mà màn 60Hz chỉ lấy được 60 — phần còn lại vẽ xong rồi vứt.
→ Thêm `GameSettings.ApplyFrameCap()`, mặc định **120 FPS, KHÔNG VSync**.

> ⚠️ **ĐÃ THỬ VSYNC VÀ PHẢI BỎ.** VSync *giữ khung hình lại* chờ màn hình quét, xếp hàng
> 1–2 khung → trễ chuột 16–33ms, game ngắm bắn cảm nhận được ngay. `targetFrameRate` thì
> chỉ *cho CPU ngủ*, không giữ gì cả. Cả hai chặn GPU chạy hoang như nhau, nhưng chỉ một
> cách giữ được cảm giác chuột.

**2. `ButtonClickSound` quét toàn scene 4 lần/giây**
`FindObjectsByType` với `FindObjectsInactive.Include` phải duyệt **hơn 4000 vật**, mà
TestScene có **TỚI HAI** component này. → Giờ chỉ lần quét ĐẦU mới duyệt toàn scene,
các lần sau chỉ quét cây con dưới Canvas. Danh sách phòng ở MenuScene vẫn bắt được
vì nó cũng sinh ra dưới Canvas.

**3. `Assets/Resources` nặng 528 MB** — *chưa sửa, cố ý*
Unity nhồi **mọi thứ** trong thư mục tên `Resources` vào build, kể cả file không dùng.
Ảnh hưởng dung lượng và thời gian khởi động, **không** ảnh hưởng FPS.
→ Để nguyên tới sau deadline. Di chuyển thư mục là thao tác dễ vỡ nhất trong Unity.
Nêu ở mục "hướng phát triển" trong báo cáo là đủ.

### ⚠️ Bài học lớn nhất, đừng quên

**Đo trong Unity Editor KHÔNG phải đo hiệu năng thật.** Editor cõng thêm Scene view vẽ
song song, móc profiler, kiểm tra an toàn mọi lời gọi API, và chạy mono thay vì IL2CPP.
Chênh 2–3 lần là bình thường. **Phải đo trên bản `.exe`.**
Mẹo: đóng tab Scene view khi đo trong Editor, thường lấy lại 20–40%.

---

## 🌐 Ngày 01/09 — HOST THOÁT GIỮA TRẬN LÀM MỌI NGƯỜI ĐỨNG HÌNH

Ba lỗi riêng biệt chồng lên nhau, không phải một.

**1. `OnDisconnectedFromServer` bỏ trống.** Đây là nguyên nhân chính.
Đừng tưởng `OnShutdown` lo hộ — hai callback báo hai chuyện khác nhau:

| Callback | Nghĩa |
|---|---|
| `OnDisconnectedFromServer` | "Đường truyền tới Host đứt" |
| `OnShutdown` | "Runner ở MÁY NÀY đã dừng hẳn" |

Mất Host **không** làm Runner client tự dừng — nó ngồi chờ kết nối lại. Scene vẫn chạy
nhưng không còn ai mô phỏng: nhân vật đứng im, bấm gì cũng không phản hồi.

**2. Treo cứng do gọi `Shutdown()` hai lần** *(nguy hiểm nhất)*

```
Host thoát → Fusion tắt Runner ở Client → gọi OnShutdown
  → OnShutdown gọi ReturnToMenu
    → ReturnToMenu chạy "await Shutdown()" trên Runner ĐANG TẮT DỞ
      → lệnh chờ KHÔNG BAO GIỜ hoàn thành → kẹt vĩnh viễn
```

→ Chữa bằng cờ `_runnerIsDown`, đặt trong `OnShutdown` **trước** khi gọi `ReturnToMenu`.
Kèm `try/catch` vì `ReturnToMenu` là `async void` — lỗi ném ra không ai bắt được và nó
giết luôn phần còn lại của hàm.

**3. Nguồn "crash":** `HUDController` chạy `gm.PhaseTimer.RemainingTime(gm.Runner)` mỗi
khung hình. Khi Host rời đi, **mạng sập trước còn object thì còn nằm đó thêm vài khung** —
`gm` khác null nhưng Runner đã chết → ném lỗi mỗi khung hình, FPS về gần 0.
→ Chữa bằng `gm.Object.IsValid`.

**Test phải đủ BA cách**, vì mỗi cách đi qua đường code khác nhau:
bấm nút thoát *(OnShutdown)* · Alt+F4 *(OnDisconnectedFromServer)* · ngắt Wi-Fi *(cùng callback, lý do khác)*.

---

## 🇬🇧 Ngày 01/09 — ĐỔI CHỮ NGƯỜI CHƠI SANG TIẾNG ANH

Phạm vi đã chốt: **chỉ chữ người chơi nhìn thấy**. `Debug.Log` (53), `[Tooltip]` (240)
và toàn bộ comment **giữ tiếng Việt** — chúng chỉ hiện trong Console và Inspector,
người chấm không thấy khi chơi, mà chủ project thì cần đọc được.

~28 chuỗi ở 5 file: `HUDController` (tên pha, kết quả round, trạng thái khu),
`NetworkRunnerHandler` (mã phòng + 5 thông báo lỗi), `SpectatorController`,
`MagneticObjectInspector`, `GameSettings`. Menu và Shop vốn đã tiếng Anh sẵn.

> 💡 Dịch theo **thuật ngữ thể loại**, không dịch sát nghĩa: "ROUND QUYẾT ĐỊNH" →
> `MATCH POINT` (không phải "Decisive Round"), "THUA ROUND NÀY LÀ HẾT" → `ELIMINATION`.
> Đây là từ vựng ai chơi Valorant/CS đều hiểu ngay.

⬜ **Cần kiểm khi test:** vài chuỗi tiếng Anh **dài hơn** bản tiếng Việt
(`MATCH POINT - WIN TO TAKE IT ALL`). Ngó xem ô chữ thông báo có tràn không —
nếu tràn thì bật `Auto Size` trên TMP_Text.

---

### ✂️ ĐÃ QUYẾT ĐỊNH BỎ

**Room list / danh sách phòng.** `OnSessionListUpdated()` đang rỗng, `RoomItemUI.cs`
là file mẫu 16 dòng, và không chỗ nào gọi `JoinSessionLobby()` — tức là chưa làm gì cả,
tốn ~2 tiếng để hoàn thành.

Bỏ vì **vào phòng bằng mã code đã chạy tốt**. Room list chỉ là tiện lợi, không phải yêu cầu.
Hai tiếng đó đổi lấy việc người chơi khỏi gõ 4 ký tự, mà lại là code MẠNG chưa từng test —
đúng loại dễ đẻ bug ở tuần cuối. Với hội đồng, "vào phòng bằng mã như Among Us" nghe hoàn chỉnh.

*(Nếu muốn lấp chỗ trống: đổi `roomListPanel` thành một dòng chữ hướng dẫn nhập mã.)*

### ✅ Kiểm tra lại — tưởng thiếu nhưng ĐÃ CÓ

- **Nút Settings ở MenuScene ĐÃ XONG** *(đối chiếu file scene 31/08)*. Chuỗi đầy đủ:
  `SettingButton` *(active, trong `MainButtonsPanel` → `Canvas`)* → `SettingsUI.Open()`.
  Đóng bằng `CloseButton` trong panel → `Close()`, hoặc `Esc`.
  Script nằm **trên Canvas**, không dính bẫy "đặt script trên panel nó tự tắt".

  ⚠️ Trong **file prefab gốc** `SettingsPanel.prefab`, `CloseButton` có `m_Target` **rỗng**.
  Chạy được là nhờ **cả hai scene đều override** trỏ vào `SettingsUI` của scene mình — bắt buộc
  phải vậy, vì nút nằm TRONG prefab còn `SettingsUI` nằm NGOÀI (trên Canvas), prefab không tự
  tham chiếu ra ngoài được. **Kéo prefab này vào scene mới thì nút Đóng sẽ chết**, phải gán lại tay.

- **`ItemData` icon: đã gán đủ 5** *(31/08)* — field tên là `itemIcon`, cả 5 file đều trỏ sprite thật.
- **Shop UI + Radial Menu: đã dựng xong trong TestScene** *(31/08)* — `ShopUI` + 5 `ShopItemButton`,
  `RadialMenuController` 4 slot điền kín, 4 `itemDataSource`.
  Còn đúng **một ô trống: `ShopUI.timerText`** → Buy Phase không hiện đồng hồ đếm ngược.
- `pointsToWin: 5` và `requiredLead: 2` trên `GameManager.prefab` đã đúng, không phải chỉnh lại.

- **`SettingsUI` đã gắn ở CẢ HAI scene** *(xác nhận 30/08)* — việc nợ từ 24/08 đã xong.
  Nhưng **chưa có nút nào gọi `Toggle()`/`Open()`**, chỉ mở được bằng `Esc`. Xem việc #5.
- **5 icon Shop ĐÃ CÓ SẴN** ở `Assets/Resources/UI kit/Icons/`, chỉ là chưa gán. Xem việc #3.
- **Tên người chơi**: chuỗi hoàn chỉnh `nameInput` → `LocalPlayerName` →
  `RoomPlayer.Spawned()` → `RPC_SetPlayerInfo` → `NickName` *(networked)* → `UpdateLobbyUI()`.
  Nếu thấy hiện "Player" là do **không gõ tên trước khi vào phòng** — code có nhánh
  `if (!string.IsNullOrEmpty(nameInput.text))`.
- `ButtonClickSound` đã gắn ở **cả hai** scene.
- `SpectatorController` đã gắn (việc nợ từ 05/08).

---

## 📐 DỰNG UI CÒN THIẾU — hướng dẫn đầy đủ *(soạn 30/08)*

### Bước 0 — Gán icon (làm TRƯỚC, ~5 phút)

Icon đã tồn tại sẵn, chỉ chưa ai kéo vào. **Gán một lần là cả Shop lẫn Radial Menu
đều có icon**, vì cả hai script đều đọc `ItemData.icon` chứ không kéo `Image` bằng tay.

| File `ItemData` *(`Assets/Items/Type/`)* | Kéo icon *(`Assets/Resources/UI kit/Icons/`)* |
|---|---|
| `Shield Armor.asset` | `Icon_ShieldArmor` |
| `Energy Drink.asset` | `Icon_EnergyDrink` |
| `Bandage.asset` | `Icon_Bandage` |
| `Gasoline Canister.asset` | `Icon_GasolineCanister` |
| `EM Barrier Core.asset` | `Icon_EMBarrior` |

⚠️ Ảnh phải để **Texture Type = Sprite (2D and UI)** mới kéo vào ô `Sprite` được.

### 🛒 Shop UI

```
Canvas                          ← gắn script ShopUI VÀO ĐÂY
└── ShopPanel                   ← ô "Shop Panel"
    ├── Text_Money              ← ô "Money Text"    ($500)
    ├── Text_Timer              ← ô "Timer Text"    (còn 12s)
    └── ItemContainer           ← thêm Horizontal Layout Group
        ├── ShopButton_0 … ShopButton_4
```

Mỗi `ShopButton_x` = **Button** + component **`ShopItemButton`**, có 3 object con:

```
ShopButton_0  (Button + ShopItemButton)
├── Icon      (Image)     ← ô "Icon Image"
├── Name      (TMP_Text)  ← ô "Name Text"
└── Price     (TMP_Text)  ← ô "Price Text"
```

Không cần gõ tên/giá bằng tay — `ShopItemButton.Setup()` tự lấy từ `ItemData`.

Ô Inspector của `ShopUI`: `Shop Panel`, `Money Text`, `Timer Text`,
`Item Buttons` *(Size = 5)*, `Shop Key = B`.

**⚠️ THỨ TỰ 5 NÚT PHẢI ĐÚNG.** Nút thứ `i` mua món thứ `i` trong
`ShopManager.catalogue` *(trên `Player.prefab`)*. Thứ tự thật đã kiểm tra 30/08:

| Index | Món |
|---|---|
| 0 | Shield Armor |
| 1 | Energy Drink |
| 2 | Bandage |
| 3 | Gasoline Canister |
| 4 | EM Barrier Core |

Kéo sai thứ tự = bấm mua giáp lại ra nước tăng lực.

### ⭕ Radial Menu

```
Canvas                          ← gắn script RadialMenuController VÀO ĐÂY
└── RadialMenuUI                ← ô "Radial Menu UI"
    ├── Slot_EnergyDrink        ← đặt 4 ô quanh tâm màn hình:
    ├── Slot_Bandage               trên, phải, dưới, trái
    ├── Slot_Gasoline
    └── Slot_EMBarrier
```

Mỗi `Slot_x` cần **Image + CanvasGroup** trên chính nó, và 2 object con:

```
Slot_EnergyDrink   (Image + CanvasGroup)
├── Icon           (Image)     ← ô "Icon Image"
└── Count          (TMP_Text)  ← ô "Count Text"   (x2)
```

`RadialMenuController` → `Slots` **Size = 4**, mỗi phần tử điền 5 ô:
`Consumable Type` *(chọn đúng loại)*, `Slot Rect` *(chính `Slot_x`)*,
`Canvas Group`, `Slot Image` *(Image nền)*, `Icon Image`, `Count Text`.

Ngoài ra: `Item Data Source` **Size = 4** *(kéo 4 ItemData tiêu hao, thứ tự không quan trọng)*,
`Menu Key = Tab`, `Dead Zone Radius = 40`.

⚠️ **Bắt buộc có `CanvasGroup`** trên mỗi slot — script làm mờ ô hết hàng bằng
alpha `0.85` → `0.4`. Thiếu nó thì không phân biệt được còn hàng hay hết.

### ⚠️ Bẫy chung cho CẢ BA bảng UI (Shop, Radial, Settings)

**Script phải đặt trên CANVAS, KHÔNG đặt trên panel con.**

Cả ba script đều tự `SetActive(false)` panel của mình. Nếu script nằm *trên* panel đó
thì tắt panel = tắt luôn script → `Update()` ngừng chạy → bấm `B`/`Tab`/`Esc`
không mở lại được nữa. Đây là lỗi im lặng, không hiện gì ở Console.

Kéo reference cũng vậy: ô `On Click ()` của nút phải kéo **Canvas** vào, không kéo panel.

---
## 1. Tóm tắt một dòng

Game **chạy được trọn vẹn**: vào phòng → chia đội → đánh nhau → tính điểm → mua đồ → dùng đồ
→ có đội vô địch → về menu. **Toàn bộ code đã xong.** Việc còn lại là **map** và một ít dựng UI.

> ⚡ **09/08: đã đổi mục tiêu tối thượng sang CHẾ ĐỘ QUÁ TẢI.** Không còn thanh máu — trúng đòn
> nạp điện tích, càng nhiễm càng bị hất xa, chết chỉ khi rơi khỏi đảo. Code xong và đã biên dịch
> sạch (`dotnet build`, 0 error), **chưa test lần nào.** Đặc tả đầy đủ ở [CLAUDE.md](CLAUDE.md) mục 4.

> 🎬 **16/08: thêm rung camera + cơ chế ngủ đông cho vật thể.** Cả hai đều sinh ra từ việc bắt đầu
> dựng map thật (địa hình gồ ghề). Biên dịch sạch 0 error, **chưa test lần nào.** Chi tiết ở mục 2b.

> 👁️ **16/08 (buổi 2): ánh sáng + nhãn quan.** Bật post-processing (game đang để `Tonemapping = None`),
> tăng chất lượng bóng đổ, và thêm `PlayerVisuals` — outline phân biệt địch/bạn + quả cầu năng lượng
> ở tay thay cho model găng chưa có. Sửa xong 2 lỗi animation nháy "falling idle".
> **Aura sét và marker đồng đội CHƯA TẠO.** Xem mục 2b và danh sách việc nợ ở mục 3.

---

## 2b. Việc làm ngày 16/08 — CHƯA TEST

### 🎬 Rung camera *(file mới: `Scripts/Player/CameraShake.cs`)*

MonoBehaviour thuần, **cố ý không networked** — rung camera chỉ mình mình thấy, gửi qua mạng là
phí băng thông. Ba nguồn rung: **đi bộ** (nhấp nhô đầu), **Dash**, **bắn vật đang cầm**.

⚠️ **Nguyên tắc quan trọng: chỉ có MỘT chỗ được ghi vào transform của camera** — đó là
`FPSMovement.Render()`. `CameraShake` chỉ *tính ra* độ lệch rồi để `Render()` cộng vào.
Cho nó tự xoay camera thì hai bên tranh nhau ghi, ai chạy sau xoá công người trước, mà thứ tự
Unity gọi hàm thì không đoán được.

| Chi tiết kỹ thuật | Vì sao |
|---|---|
| Dash móc vào `OnDashPerformed()` **có sẵn**, không tạo đường mới | Rung thẳng trong `FixedUpdateNetwork` sẽ cộng trauma 5-6 lần mỗi cú do Fusion tua lại |
| Bắn vật phải thêm `FireCount` **`[Networked]`** | `FireGrabbedObject()` nằm sau `if (!HasStateAuthority) return;` nên chỉ chạy trên Host — máy Client bắn sẽ không rung gì cả |
| Dao động bằng **Perlin noise**, không phải `Random.Range` | Random cho camera giật xành xạch như hỏng; Perlin liền mạch mới ra cảm giác chấn động |
| Trauma **bình phương** trước khi dùng | Mắt người cảm nhận phi tuyến. Lấy thẳng thì cái đuôi lắc lay mãi như camera bị lỏng |

~~**Bước Unity còn thiếu:** Add Component `Camera Shake` lên GameObject Camera~~
✅ **Đã gắn rồi** *(đối chiếu `Player.prefab` ngày 30/08)*.

### 💥 Cảm giác đánh & ăn đòn *(30/08 — CHƯA TEST)*

`CameraShake` từ một hệ thống rung tách thành **ba hệ thống độc lập**. Lý do phải tách chứ không
chỉ vặn to nhỏ: đánh ra và ăn đòn **ngược nhau về bản chất**, cùng một bộ tham số không ra được cả hai.

| # | Hệ thống | Cảm giác | Tần số | Đuôi tắt |
|---|---|---|---|---|
| 1 | `AddTrauma` *(cũ)* | Bị xóc | Cao (25) | Nhanh — bình phương |
| 2 | `AddKick` **mới** | **Đánh ra, dứt khoát** | Lò xo cứng 280 | Nảy đúng một nhịp |
| 3 | `AddDisorient` **mới** | **Ăn đòn, váng đầu** | Thấp (2.6) | Chậm — căn bậc hai |

**Cú giật (bắn / đấm):** lệch **tức thì** rồi lò xo kéo về, kèm camera thụt về sau.
Cộng thẳng vào độ lệch chứ không cộng vào vận tốc — cộng vào vận tốc thì mất vài khung hình mới
tới đỉnh, cú đánh thành "nhão". Trục ngang đảo dấu ngẫu nhiên mỗi cú nên bắn liên tiếp không lặp lại.

⚠️ Lò xo cứng + Euler = **nổ tung** nếu `dt` lớn (máy khựng vì nạp texture hoặc Fusion tua lại).
Đã chia nhỏ thành các bước ≤ 1/120 giây. Đừng gỡ vòng `for` đó ra.

**Váng đầu (ăn đòn):** ba thứ cùng lúc — lảo đảo chậm, **FOV phập phồng**, **mờ màn hình** 0.45s.
Thứ thật sự gây buồn nôn là **trục nghiêng đầu (Z)**, nhân `disorientRollBias` 2.2 lần và chạy chậm
hơn nữa: não quen với đường chân trời nằm ngang, nghiêng nó đi mới phá được tiền đình.
Lắc trái phải chỉ gây khó ngắm. **Muốn choáng hơn thì tăng `Disorient Roll Bias`, đừng tăng biên độ.**

**Mờ màn hình dựng BẰNG CODE** — `VolumeProfile` + `DepthOfField` (Gaussian) tạo lúc chạy, gắn
`Volume` priority 100 đè lên profile toàn cục. Không phải tạo asset hay GameObject nào trong Editor.
Tạo **muộn** ở cú ăn đòn đầu tiên, không tạo trong `Awake` — `Awake` chạy trên cả nhân vật người khác,
tạo ở đó thì phòng 4 người sinh 4 bộ mờ thừa.

| Móc vào đâu | Chỗ |
|---|---|
| Bắn vật | `PlayerMagnetController.OnObjectFired()` — bộ đếm `FireCount` có sẵn |
| Đấm | `PlayerMagnetController.OnMeleePerformed()` — dùng chung `MeleeCount` với tiếng đấm |
| Trúng đòn | `PlayerHealth.OnChargeChanged()` |
| **Trúng đòn khi CÒN GIÁP** | `PlayerHealth.OnArmorChanged()` |

⚠️ Ô cuối bắt buộc phải có: còn giáp thì điện tích **không tăng chút nào**, chỉ móc vào điện tích
thì mặc giáp vào là mọi cú trúng đòn im lìm, người chơi tưởng chưa bị bắn trúng.

Độ choáng tỉ lệ với **lượng điện thật sự nạp vào**, có sàn `hitMinStrength` 0.3 để đòn sượt vẫn có
phản hồi. Đối chiếu: đạn thường ~15 điểm → choáng 0.43 · Heavy ~30 → 0.86 · TNT ~52 → kịch khung.

> ⚠️ Nếu **mờ màn hình không chạy** mà phần lảo đảo vẫn chạy: kiểm tra **Volume Mask** của Camera
> trong `Player.prefab` có bao gồm layer của nhân vật không. Mặc định là Everything nên thường không
> phải đụng tới. Không sửa được thì bỏ tick `Enable Hit Blur`, hai phần kia vẫn nguyên.

### 🦘 Nhảy + lái trên không *(30/08 — CHƯA TEST)*

Thiếu sót lớn: game **chưa từng có phím nhảy**. Đã thêm `Space` (đổi được ở `FPSMovement.jumpKey`).

| Chỗ sửa | Nội dung |
|---|---|
| `NetworkInputData.cs` | `InputButton.Jump = 11` |
| `NetworkRunnerHandler.OnInput()` | Đọc `localPlayer.jumpKey` |
| `FPSMovement` mục 4 | Quy đổi `jumpHeight` (mét) → vận tốc bật |
| `FPSMovement` mục 6 | Nhân `airControl` vào phần di chuyển khi đang bay |

⚠️ **`jumpPressed` phải đọc ở MỤC 3, không phải mục 4** dù mãi mục 4 mới dùng. Cuối mục 3 có dòng
`PreviousButtons = input.Buttons` — sau dòng đó thì "tick trước" và "tick này" giống hệt nhau nên
`WasPressed()` luôn trả `false`. Nhảy sẽ không chạy mà Console **không báo lỗi gì cả**.

⚠️ **Hai biến chạm đất, đừng nhầm.** `controller.isGrounded` đổi giá trị sau mỗi lệnh `Move()`:
- `groundedNow` (mục 4, **trước** Move) — cho nhảy và lái trên không
- `touchingGround` (mục 7, **sau** Move) — cho animation và coyote time *(vốn đã có)*

Dùng lẫn hai cái sẽ ra tick vừa được coi là đang bay (mất lái) vừa được coi là chạm đất (cho nhảy tiếp).

**Điền chiều cao bằng MÉT** (`jumpHeight = 2`), code tự quy đổi `v = √(2·g·h)`. Vì vận tốc bật phụ
thuộc ô `gravity`; điền thẳng vận tốc thì mỗi lần chỉnh trọng lực lại phải tính tay.

**Lái trên không đã có sẵn từ trước mà không ai để ý:** phần `moveDirection * moveSpeed` được tính
lại từ đầu mỗi tick theo phím đang bấm, **không cộng dồn vào vận tốc** — nên game vốn đã cho lái
100% giữa không trung. Việc thêm vào chỉ là cái **nút vặn để GIẢM bớt**.

> 🔴 **`airControl` là nút vặn cân bằng, không phải trang trí.** Cái chết duy nhất của chế độ Quá Tải
> là rơi khỏi đảo. Lái trên không càng mạnh thì cú hất văng càng vô nghĩa — cứ giữ phím hướng về đảo
> là bay ngược lại được. Đang để **0.8**, tức là vẫn lái rất thoải mái. **Nếu test thấy bị đấm bay
> mà vẫn tự bơi về được thì hạ ô này xuống**, đừng đi tăng lực đấm.

`airControl` **chỉ nhân vào phần người chơi tự sinh ra**, không nhân vào `velocity` và `impact` —
người chơi giữ nguyên quán tính của cú đấm. Nhân cả cụm thì bị đấm giữa trời lại bay chậm hơn
bị đấm dưới đất.

**Không cần dựng animation mới:** `PlayerAnimatorDriver` đã có tham số `Grounded`, nên nhảy tự
chuyển sang animation bay/rơi có sẵn.

### 😴 Ngủ đông vật thể *(`MagneticObject`)*

Vật đứng yên `sleepDelay` giây → khoá cứng `isKinematic = true`. Bị hút/đẩy/đâm/nổ → tự tỉnh.

**Vì sao cần:** địa hình gồ ghề làm vật nằm trên dốc trượt và rung mãi không dứt. Unity có
`Rigidbody.sleepThreshold` sẵn nhưng nó chỉ ngủ khi vật *thật sự* đứng yên — trên dốc thì trọng lực
kéo liên tục nên không bao giờ đạt. Khoá thẳng bằng `isKinematic` thì dốc cỡ nào cũng nằm im.
Lợi ích kèm theo: Host thôi mô phỏng, `NetworkRigidbody3D` thôi gửi vị trí mỗi tick.

⚠️ **Hai cái bẫy đã xử lý, đừng gỡ ra:**

1. **`AddExplosionForce` vô tác dụng lên vật kinematic.** PhysX bỏ qua mọi lực tác động lên vật
   kinematic. Nên `Explode()` phải gọi `WakeUp()` cho từng vật **trước khi** cộng lực, nếu không
   bom nổ giữa đống bàn ghế mà không cái nào nhúc nhích.
2. **Vật ngủ bị đâm thì mất sạch động lượng.** Kinematic được PhysX coi như tường khối lượng vô hạn.
   `WakeUpFromImpact()` phải tính lại động lượng bằng tay theo tỉ lệ khối lượng, nếu không vật chỉ
   tỉnh dậy rồi đứng nguyên tại chỗ.

**Tự chữa lệch trạng thái:** các đường hút/đẩy/bắn bên `PlayerMagnetController` đặt thẳng
`isKinematic = false` chứ không gọi `WakeUp()`. `UpdateSleepState()` phát hiện và tự sửa cờ.
Cố ý làm vậy thay vì đi sửa 6 chỗ bên kia — bớt rủi ro đụng vào đường bắn đã cân bằng xong.

🔧 **Sửa 16/08 (khi test vòng 2): vật thể ngủ NGAY, không rơi tự do lấy một giây.**

Triệu chứng: vài cái cây đổ rạp ngay khi round bắt đầu, và một số vật không bao giờ nằm im.

Nguyên nhân: `ResetForNewRound()` **cố ý đánh thức** mọi vật với lý do *"nó cần rơi xuống và tự
ổn định"*. Lý do đó sai — cây cao mảnh chỉ cần collider chạm đất lệch chút là trọng lực lật đổ,
mà muốn ngủ lại phải đứng yên liên tục `sleepDelay` giây nên nó đổ hẳn mới thôi.

`_originalPosition` **chính là chỗ đã đặt tay trong Editor**. Vật lý "ổn định" chỉ có thể đẩy vật
RỜI KHỎI chỗ đó, không bao giờ đưa nó về đúng hơn.

→ Giờ `ResetForNewRound()` đóng băng ngay **sau** khi Teleport, và `Spawned()` cũng cho ngủ luôn
(bịt khoảng hở `warmupDuration` 2 giây trước round đầu).
→ Hệ quả **có chủ đích**: vật đặt lơ lửng sẽ **treo giữa không trung** thay vì rơi.
Đó là dấu hiệu đặt sai chỗ trong Editor, sửa ở Editor mới đúng — đừng sửa lại code cho nó rơi.

⚠️ **Phải BỎ tick `Is Kinematic` thủ công trên prefab.** Để nguyên thì vật **bất tử**: nó đứng yên,
nhưng cờ `IsSleeping` vẫn `false` nên `OnCollisionEnter` không đánh thức, bắn gì vào cũng trơ ra.

### 🐛 Animation nháy "falling idle" — HAI lỗi chồng nhau, đã sửa cả hai

Triệu chứng: đang chạy thì nhân vật vừa khựng về đứng yên vừa nháy sang rơi tự do, lúc bị lúc không.
Và khi test 2 máy, **chỉ nhân vật Host là bình thường khi nhìn từ máy Client**.

**Lỗi 1 — `AC_Player.controller`: transition `Fall → Locomotion` để điều kiện `Grounded = false`**
(đáng lẽ `true`). Thành vòng lặp vô hạn: Grounded false → vào Fall → Fall thấy false → nhảy ngược
về Locomotion → lại vào Fall. Hai transition đều có duration 0.15s nên Animator kẹt vĩnh viễn giữa
hai animation đang trộn dở — đó chính là cái "falling idle" nhìn thấy. ✅ Đã sửa.

**Lỗi 2 — Client ghi vào `[Networked]` mà nó không sở hữu.** `FPSMovement.FixedUpdateNetwork()`
chỉ chặn bằng `GetInput()`, mà hàm đó trả `true` ở **hai** nơi: trên Host với mọi nhân vật, VÀ trên
máy Client với nhân vật của chính họ. Nên Client cũng ghi `WalkSpeed01` / `IsGrounded` /
`MovingBackward`. Fusion coi đó là giá trị dự đoán, mỗi lần nhận gói tin từ Host lại huỷ đi và
chạy lại các tick → con số dao động → animation nhấp nháy.

Và dự đoán ở đây **không thể đúng được**, vì nó dựa trên `controller.velocity` — giá trị nội bộ của
CharacterController, mà `BeforeAllTicks()` lại tắt/bật component đó liên tục. Không tất định thì
hai máy không bao giờ tính ra cùng kết quả.

→ Đó là lý do chỉ nhân vật **Host** hiện đúng khi nhìn từ Client: nó là proxy nên Client không
đụng vào, cứ nhận sao dùng vậy.

✅ Đã sửa: bọc `if (!HasStateAuthority) return;` trước khi ghi. Đánh đổi là animation của chính mình
trễ ~nửa vòng ping — không ai nhận ra, còn nhấp nháy thì ai cũng thấy.

⚠️ Nhưng **nhấp nhô camera vẫn tính tại chỗ** qua biến mới `LocalWalkSpeed01` (không networked).
Camera chỉ mình mình nhìn nên không cần khớp với ai; bắt nó chờ Host thì head bob lag theo ping.

**Kèm theo — coyote time cho `isGrounded`:** `controller.isGrounded` chỉ nói được về lần `Move()`
gần nhất, nên trên địa hình gồ ghề nó tắt đúng một tick rồi bật lại (đi qua khe giữa hai collider,
leo gờ nhỏ, bước xuống dốc thoải). Giờ phải rời đất **liên tục** `groundedGraceTime` (0.15s) mới
báo là đang bay. Cố ý KHÔNG lọc phần trọng lực — trọng lực phải phản ứng theo va chạm thật từng tick.

### 💡 Ánh sáng & post-processing *(16/08)*

Game trông nhạt vì ba thứ đều là mặc định chưa ai động vào: **`Tonemapping = None`** (thủ phạm lớn
nhất — vùng sáng bị cắt phẳng thành trắng bệch), mọi hiệu ứng `intensity = 0`, và
**Color Grading = LDR**.

May là camera **đã bật** Post Processing sẵn và `DefaultVolumeProfile` **đã được gán làm profile
toàn cục** (đối chiếu GUID với `UniversalRenderPipelineGlobalSettings`) — nên chỉ cần đổ nội dung
vào, không phải tạo GameObject Volume nào trong scene.

```
Assets/Settings/DefaultVolumeProfile.asset
  Tonemapping   None -> Neutral      ← nền tảng cho mọi thứ còn lại
  Saturation    0    -> +22
  Contrast      0    -> +12
  PostExposure  0    -> +0.2
  Bloom         0    -> 0.7   threshold 1   ← làm găng đỏ/xanh và quả cầu năng lượng loé sáng
  Vignette      0    -> 0.28

Assets/Settings/PC_RPAsset.asset          (quality level "PC" đang dùng, m_CurrentQuality = 1)
  ShadowDistance              50   -> 85    ← map ~56m, để 50 thì bóng biến mất giữa sân
  MainLightShadowmapResolution 2048 -> 4096
  ShadowAtlasResolution        256 -> 2048  ← 256 là đèn phụ đổ bóng VỠ NÁT
  ColorGradingMode             LDR -> HDR   ← bắt buộc để bloom/tonemapping đúng
```

⬜ **Còn phải làm trong Unity:** hạ **Ambient Intensity** `1` → `0.6`
(Window → Rendering → Lighting → Environment). Đây là thứ làm bóng đậm nhất — ambient đang để 1
nghĩa là vùng trong bóng vẫn được chiếu sáng đầy đủ nên bóng nhìn nhạt như vệt xám.
Không sửa hộ vì giá trị này nằm trong `TestScene.unity` đang mở.

> Tụt FPS thì trả lại theo thứ tự: `Shadow Res 4096 → 2048`, rồi `Shadow Distance 85 → 65`.

### 👁️ Nhãn quan nhân vật *(file mới: `Scripts/Player/PlayerVisuals.cs`)*

**Vấn đề:** nhân vật rất khó nhìn thấy giữa map nhiều cây cối, nhất là trên màn hình nhỏ.
Và chưa có cách nào phân biệt địch với đồng đội.

⚠️ **Cái bẫy suýt mắc: màu ĐỘI và màu CỰC GĂNG trùng nhau hoàn toàn.**
`Team` 0 = Đỏ / 1 = Xanh, mà cực găng cũng Dương = Đỏ / Âm = Xanh dương. Nếu cho outline
toàn thân mang màu điện tích thì nhìn một người viền đỏ sẽ không biết đó là *"địch đội Đỏ"*
hay *"đồng đội đang mang cực Dương"* — aura không những không giúp mà còn phá luôn.

**Nguyên tắc đã chốt: mỗi loại tin một kênh riêng, không kênh nào mang hai nghĩa.**

| Thông tin | Kênh | Màu |
|---|---|---|
| Cực găng của **địch** | Outline toàn thân + quả cầu ở tay | Đỏ / Xanh dương |
| **Đồng đội** | Outline + marker trên đầu | Xanh **lá** |
| **Địch** | *(không gán màu riêng)* | — nhận ra bằng LOẠI TRỪ |
| Mức nhiễm điện | Aura sét quanh người | Vàng |

**Vì sao địch KHÔNG được gán màu riêng:** cho địch viền cam hay đỏ thì nó cạnh tranh thị giác
với chính màu cực găng — thứ quan trọng nhất cần đọc. Để địch "sạch màu" thì thứ duy nhất rực rỡ
trên người họ là cực găng, mắt bị hút thẳng vào đó.

**Che khuất — khác nhau có chủ đích:**

| | Chế độ | Vì sao |
|---|---|---|
| Đồng đội | `Outline.Mode.OutlineAll` — **xuyên tường** | Vị trí đồng đội là thông tin cho không |
| Địch | `Outline.Mode.OutlineVisible` — **có che khuất** | Núp sau tường là mất viền, thò nửa người thì chỉ nửa đó hiện. Cho xuyên tường thành gian lận nhìn xuyên vách |

Cả hai chế độ đều có sẵn trong **QuickOutline** (`Resources/Environment/QuickOutline/`), không phải viết shader.

💡 **KHÔNG CẦN ASSET GĂNG TAY.** `character.fbx` là **Humanoid rig**, nên lấy được xương bàn tay
bằng `animator.GetBoneTransform(HumanBodyBones.LeftHand)`. Script tự tạo quả cầu phát sáng gắn vào đó.
Quả cầu còn **đọc tốt hơn** model găng thật: găng chỉ vài pixel ở 30m và hay bị thân che, còn quả cầu
là nguồn sáng nên Bloom kéo hào quang loang ra. Việc này gỡ nút thắt "chưa có asset găng + FPS view".

> ⚠️ `CreatePrimitive` luôn kèm Collider — script xoá ngay khi tạo. Sót một cái là đủ phá hệ vật lý.

**Trạng thái:**

| Phần | Xong? |
|---|---|
| Outline địch (màu cực găng, có che khuất) | ✅ code xong |
| Outline đồng đội (xanh lá, xuyên tường) | ✅ code xong |
| Quả cầu năng lượng ở hai tay | ✅ code xong |
| Gắn `PlayerVisuals` lên `Player.prefab` | ⬜ **cần làm trong Unity** |
| **Aura sét vàng** (ParticleSystem) | ⬜ **CHƯA TẠO** — material `M_LightningAura` đã có, thiếu ParticleSystem |
| **Marker trên đầu đồng đội** | ⬜ **CHƯA TẠO** |

Hai ô cuối để trống thì script vẫn chạy bình thường, chỉ là không có hiệu ứng đó.

**Cách tạo aura sét (không cần texture):** ParticleSystem con của Player, Scale `(0.7, 1.8, 0.7)`,
Shape `Box` + Emit from `Shell`, bật **Noise** (Strength 1.5) và **Trails** (Lifetime 0.3) —
hai module này biến chấm sáng thành tia điện ngoằn ngoèo. Material dùng
`URP/Particles/Unlit` + Surface `Transparent` + Blending **`Additive`**, để **trắng nguyên bản**;
màu vàng và HDR Intensity `+2` đặt ở **Start Color** của Particle System.
`Emission → Rate over Time` để **0**, script tự ghi đè theo mức nhiễm.

> ⚠️ Nhớ gán cả **Trail Material** trong Renderer, quên là vệt trail ra màu hồng cánh sen.
> ⚠️ `_BaseColor` của shader Particles **không có tag `[HDR]`** nên không kéo Intensity được —
> đó là lý do phải đặt màu ở Start Color chứ không phải ở material.

### 🔧 Sửa: nhiều collider trên một vật *(`PlayerMagnetController`)*

Ba chỗ dùng `GetComponent<Collider>()` **số ít** đã gộp thành `SetIgnoreCollisionWithPlayer()`
duyệt hết mọi collider. Cần vì cây/đá dùng **nhiều BoxCollider ghép** thay cho MeshCollider lõm.
Sót một collider là dính lại đúng **bẫy số 8** ở CLAUDE.md: cái chưa tắt nằm chồng trong người chơi,
PhysX bắn ra lực gỡ kẹt, vật bay đi mỗi lần một hướng.

---

## 2. Đã xong và đã kiểm chứng trên 2 máy

Lobby (mã PIN, đổi đội) · spawn theo đội đúng vị trí và hướng nhìn · WASD + xoay + Dash ·
đổi điện tích · cận chiến 3 kiểu · nạp điện / hút / cầm / ném vật thể · máu + giáp ·
vòng đấu đầy đủ (BuyPhase 15s → Combat → RoundEnd → tính điểm → hồi sinh) · rào chắn spawn ·
KillZone · Shop (mua bán, trừ tiền, thưởng cuối round) · túi đồ · Radial Menu ·
4 vật phẩm tiêu hao · HUD.

### Giá trị Inspector đã cân bằng tay (ghi lại phòng khi prefab hỏng)

Đọc thẳng từ file `.prefab` ngày 09/08 nên đây là giá trị THẬT, không phải mặc định trong code.

```
[Project Settings → Physics]
  Gravity              (0, -3, 0)      ← mặc định Unity là -9.81. Chỉ ảnh hưởng RIGIDBODY (đạn)

[Player.prefab]
FPSMovement          moveSpeed 13 · mouseSensitivity 1 · dashForce 100 · dashCooldown 1
                     gravity -9.81 · mass 3 · drag 5     ← gravity này CHỈ cho nhân vật
PlayerMagnetController  pullForce 13 · pushForce 50 · shootRange 50
                     meleeRange 4 · meleeCooldown 1 · dashLockRange 20
                     meleePushForce 200 · grapplePullForce 100
                     heldObjectSize 1 · onlyShrinkLargeObjects true
PlayerHealth         maxCharge 100 · knockbackAtMaxCharge 5 · chargeGainMultiplier 1.5
                     maxArmor 30 · armorPerPurchase 10
                     bandageMaxDischarge 20 · bandageChargeRatio 0.5
MagneticObject       hitKnockbackForce 120  ← lực hất khi TRÚNG ĐẠN (mới 09/08, trước đây = 0)
PlayerEconomy        start 500 · cap 750 · win 150 · lose 100

[GameManager.prefab]
                     buyPhase 15 · roundEnd 5 · matchEnd 8 · warmup 2
                     killZoneY -20 · pointsToWin 5 · requiredLead 2
                     combatDuration — CHƯA có trong prefab, sẽ lấy mặc định 90 khi Unity biên dịch lại

[Shop]               Armor 150 · EnergyDrink 100 · Bandage 100 · Gasoline 150 · EMBarrier 200
```

> ⚠️ **Hai trọng lực riêng biệt, đừng lẫn.** Project Settings `-3` cho vật thể (đạn bay lơ lửng),
> `FPSMovement.gravity` `-9.81` cho nhân vật (rơi bình thường). Tính quỹ đạo đạn phải dùng **-3**.

> 🔴 **`knockbackAtMaxCharge = 5` gần như chắc chắn quá lố.** Quãng đường bị đẩy ≈ `(lực/mass) × 0.2`
> với `drag 5`. Với `meleePushForce 200` và `mass 3`: sạch điện đã bay **13m**, đầy điện bay **67m** —
> văng khỏi mọi map. Bắt đầu bằng **2.5**, nếu vẫn quá thì hạ `meleePushForce` 200 → 120
> trước khi động vào hệ số. *(Ước lượng trên giấy, chưa test.)*

---

## 3. Còn phải làm

### 🔴 Map — rủi ro lớn nhất, dồn thời gian vào đây

Hiện **toàn cube và model mặc định Unity**. Đây là thứ mất điểm rõ nhất khi bảo vệ.

Đừng dựng đẹp — cần **map đọc được, chơi được**. Ghép từ asset đã có trong project
(Polytope Studio, Fantasy Environments, Unvik Cross Plains) nhanh hơn tự dựng nhiều.

⚠️ Đổi map làm hỏng 4 giá trị đã chỉnh, phải cân bằng lại sau:
`redTeamSpawnPoint` · `blueTeamSpawnPoint` · vị trí 2 rào chắn · `killZoneY`

🔥 **Từ 09/08 map còn quan trọng hơn nữa.** Chế độ Quá Tải sống chết bằng **rìa vực**:
bị hất bay 15m chỉ đáng sợ khi có chỗ để rơi xuống. Map phải có **nhiều rìa hở, ít tường bao**,
và nên có vài mỏm hẹp nhô ra ngoài để tạo điểm nóng. Map kín sẽ giết mode này.

Dấu hiệu map chưa đạt: **mọi round đều kết thúc bằng hết giờ 90s** thay vì có người rơi.

#### 📐 Bố cục đã thiết kế: **"LÕI TỪ"** *(09/08, chưa dựng)*

Đảo bát giác, một lõi nâng cao ở giữa, 4 mỏm nhô ra vực ở 4 góc chéo.

```
      ◆TNT                                      ◆TNT
        ╲     ╔══════════════════════════╗     ╱
         ╲────╢   ▓▓▓  SPAWN ĐỎ  ▓▓▓     ╟────╱      Z=62 (y=+4)
              ║        [rào chắn]        ║
              ║   ╭───────╨───╨──────╮   ║
   V Ự C      ║dốc│   LÕI TỪ  y=+7   │dốc║      V Ự C
              ║   │     r = 9m       │   ║
              ║   ╰───────╥───╥──────╯   ║
              ║        [rào chắn]        ║
         ╱────╢   ▓▓▓ SPAWN XANH ▓▓▓     ╟────╲      Z=18 (y=+4)
        ╱     ╚══════════════════════════╝     ╲
      ◆TNT                                      ◆TNT
            X=12                          X=68
```

| Khu | Cao | Kích thước | Vai trò |
|---|---|---|---|
| Vành đai | `y=0` | rộng ~19m | Sân đánh chính |
| **Lõi Từ** | `y=+7` | bán kính 9m | Cao điểm tranh chấp, 4 dốc lên |
| 2 bệ spawn | `y=+4` | 12×10m | Có rào chắn Buy Phase |
| 4 mỏm chéo | `y=0` | 10×10m, cổ nối 4m | **Chỗ để TNT** |

```
Tâm map      (40, 0, 40)          Spawn Đỏ    (40, 4, 62) yaw 180
Lõi Từ       (40, 7, 40) r=9      Spawn Xanh  (40, 4, 18) yaw 0
Rào Đỏ       (40, 4, 56)          Rào Xanh    (40, 4, 24)
4 mỏm        (16,0,16) (64,0,16) (16,0,64) (64,0,64)
Đảo bát giác X và Z từ 12 đến 68  ·  killZoneY -20
```

**Nguyên tắc rải đạn — đừng làm qua loa:** *đạn ngon nhất nằm ở chỗ dễ chết nhất.*
Normal ×5–6 ở vành đai gần spawn · Heavy ×2 trên Lõi Từ · Spike ×2 vành đai nửa ngoài ·
**TNT ×4 ở đúng đầu 4 mỏm**. Nếu rải đều khắp map thì không ai có lý do ra rìa vực → mode Quá Tải chết yểu.

**Vì sao vành đai rộng 19m:** một cú `meleePushForce 200` lúc sạch điện đẩy đi ~13m.
Đứng giữa vành đai thì sống, nửa ngoài thì chết. Nhiễm đầy điện (~33m) thì đứng đâu cũng chết.
Sự leo thang đến từ **kích thước map**, không phải từ code.

> ⚠️ **DỰNG BẰNG CUBE TRẮNG TRƯỚC.** Không texture, không model, không ánh sáng.
> Chắc chắn sẽ phải sửa kích thước sau khi test. Sửa cube mất 5 giây; sửa khu đã kitbash 40 model
> mất nửa buổi, rồi sẽ ngại sửa và chấp nhận một map dở. Khoá layout xong mới dán art.

### 🔴 Hai lỗi phát hiện 16/08 khi dựng map — SỬA TRONG UNITY, KHÔNG PHẢI CODE

**1. Thiếu tag `Magnetic` → bấm chuột vào vật không ăn gì.**

Triệu chứng: "nhiều cục đá và cây bắn điện tích không lên được, cục khác thì được".
Nguyên nhân: prefab `Tree9_*` có `Tag = Untagged`, phải override tay từng cái trong scene.
Đọc `TestScene.unity` ngày 16/08: chỉ **8 object** được override, trong khi log báo **~16** vật
có Rigidbody. Raycast lọc bằng `CompareTag("Magnetic")` nên bỏ qua hoàn toàn số còn lại.

→ **Cách sửa:** đặt `Tag = Magnetic` ngay **trên prefab**, khỏi tick tay từng bản sao.
Đúng cái bẫy đã ghi sẵn ở mục "Các bước tạo 1 vật thể từ tính mới" — vẫn dính lại lần nữa.

**2. Concave Mesh Collider — 32 dòng lỗi đỏ trong Console.**

```
Concave Mesh Colliders are not supported when used with dynamic Rigidbody GameObjects.
Scene hierarchy path "Environment/Rock1A"
```

Dính: `Rock1A` `Rock4A` `Tree9_2/3/4/5` và các bản sao. Hậu quả thật: **những vật này gần như
không va chạm được gì** — đạn bay xuyên qua, người chơi lọt qua.

Unity chỉ cấm MeshCollider **lõm** đi cùng **Rigidbody động**. Hai đường sửa:

| Vật đó là gì | Làm gì |
|---|---|
| **Trang trí** (đa số cây) | **Xoá Rigidbody + MagneticObject.** MeshCollider lõm hợp lệ ngay khi không còn Rigidbody. Giải quyết luôn cả chuyện cây đổ nhào và giảm tải mạng |
| **Làm đạn** (có tag `Magnetic`) | 2 BoxCollider — 1 thân, 1 vòm lá — đặt **trên chính GameObject gốc** |

⚠️ **Đừng tách collider ra GameObject con.** Raycast dùng `hit.collider.GetComponent<MagneticObject>()`,
trúng vào con thì trả `null` và mất hẳn khả năng tương tác.

✅ Hai BoxCollider **chồng lên nhau một chút là tốt** (khỏi hở khe cho đạn lọt qua). Collider cùng
một Rigidbody thì PhysX không bao giờ cho chúng va chạm với nhau, nên không sinh lực gỡ kẹt.

> 💡 **Cây đổ nhào là đúng vật lý, không phải lỗi.** Khúc gỗ dựng đứng có Rigidbody động, trọng tâm
> cao, đặt trên dốc thì nó *phải* ngã. Câu hỏi thật là cây đó có cần là vật thể vật lý không —
> với phần lớn cây trong map thì không.

### ⬜ Việc trong Unity còn nợ *(tính tới 16/08)*

| Việc | Ở đâu | Thiếu thì sao |
|---|---|---|
| Gắn `CameraShake` lên **GameObject Camera** | `Player.prefab` | Không có gì rung |
| Gắn `PlayerVisuals` lên **object gốc Player** | `Player.prefab` | Không có outline / quả cầu tay |
| Tạo **ParticleSystem aura sét** rồi kéo vào ô `Lightning Aura` | `Player.prefab` | Không hiện mức nhiễm điện |
| Tạo **marker trên đầu** rồi kéo vào ô `Ally Marker` | `Player.prefab` | Không đánh dấu đồng đội |
| Hạ **Ambient Intensity** `1` → `0.6` | Lighting → Environment | Bóng nhạt như vệt xám |
| Bỏ tick **`Is Kinematic`** thủ công trên prefab vật thể | `Items/*.prefab` | Vật **bất tử** — bắn gì vào cũng trơ |
| Đặt **`Tag = Magnetic` trên prefab** cây/đá | `Resources/Tree9/*`, `Rock*` | Bấm chuột vào không ăn gì |
### 🟡 Trang trí đảo — CHƯA LÀM (dự kiến ~1 tiếng)


Đã có công cụ sẵn: `Assets/Scripts/EnvironmentScatter.cs`. Gắn lên Empty GameObject,
kéo prefab vào danh sách, chuột phải component → **"Rải trang trí"**. Không ưng thì
**"Xoá hết"** rồi rải lại, mỗi lần ra một bố cục khác.

Cần tải thêm **cỏ / bụi / hoa** — project chưa có cái nào. Nguồn khuyên dùng:
**Quaternius Ultimate Nature Pack** (CC0, cùng phong cách lowpoly với đá đang có).
Đá thì đã có sẵn bộ *Rocks and Boulders 2*, dùng luôn.

Rải thành **nhiều lớp thưa** thay vì một lớp dày:

| Object | Prefab | Count | Align To Slope |
|---|---|---|---|
| `Scatter_Grass` | cỏ | 500 | tắt |
| `Scatter_Bushes` | bụi | 80 | tắt |
| `Scatter_Rocks` | đá *(đã có)* | 60 | **bật** |
| `Scatter_Flowers` | hoa, nấm | 40 | tắt |

⚠️ **`Strip Colliders` PHẢI BẬT.** Cỏ có collider sẽ chặn đường đạn và làm nó lệch hướng —
đúng cái lỗi đã mất cả buổi 16/08 để sửa. Trang trí chỉ cần nhìn thấy, không cần chạm được.

⚠️ **Chừa trống lối đi giữa sân.** Đặt vùng rải ở rìa đảo. Cỏ um tùm giữa đấu trường vừa
che tầm nhìn vừa làm khó nhìn vật thể và đối thủ.

### 🟡 Dựng UI còn thiếu

| Việc | Ghi chú |
|---|---|
| ~~Nhãn pha "CHIẾN ĐẤU" chiếm chỗ giữa màn hình~~ | ✅ **Đã sửa 16/08** — `HUDController` tắt hẳn `phaseText` (SetActive false) khi vào pha Combat, bật lại ở các pha khác. Đang đánh nhau thì ai cũng biết là đang đánh nhau, dòng chữ đó chỉ tranh chú ý với thứ cần nhìn thật |
| **Sửa nhãn HUD "MÁU" → "ĐIỆN TÍCH"** | Chỉ đổi chữ. Ô `Health Fill`/`Health Text` giữ nguyên reference, KHÔNG phải gán lại |
| *(tuỳ chọn)* Thêm TMP text cho ô `Knockback Text` | Hiện `x2.4` — cho người chơi biết đang nguy hiểm cỡ nào |
| Gắn `SpectatorController` lên Canvas TestScene | + 1 panel + 1 TMP text. Rất nhẹ |
| Gắn `SettingsUI` lên Canvas MenuScene | 3 slider âm lượng, slider độ nhạy, dropdown độ phân giải, toggle fullscreen, nút đóng |
| *(tuỳ chọn)* `SettingsUI` cho TestScene | Mở Settings giữa trận |

> **Luôn gắn script UI lên Canvas, KHÔNG lên panel con.** Panel tắt thì `Update()` chết theo,
> bấm phím sẽ không mở lại được. Áp dụng cho cả ShopUI / RadialMenu / HUD / Settings / Spectator.

### 🟡 Âm thanh — 18 ô clip trên `AudioManager` (MenuScene)

Chưa cần đủ. Năm cái quan trọng nhất: `sfxHit` `sfxExplosion` `sfxDash` `sfxCharge` `sfxRoundStart`.
Nguồn: freesound.org, kenney.nl. **Ghi nguồn trong báo cáo.**

### ⚫ Trước khi nộp — xoá phím debug

- `InputButton.DebugSuicide` trong `NetworkInputData.cs`
- Dòng gửi phím `K` trong `NetworkRunnerHandler.OnInput()`
- Khối tự sát trong `PlayerMagnetController.FixedUpdateNetwork()`
- Đặt lại `Points To Win = 5` nếu có hạ để test

### 🟢 Chưa test (code xong, chưa chạy thử lần nào)

- **🎬 Rung camera** *(làm 16/08)* — cần Add Component `CameraShake` lên Camera trước đã
  - Đi bộ có nhấp nhô không, có chóng mặt không (hạ `Bob Vertical Amount` hoặc bỏ tick nếu có)
  - Dash và bắn vật có rung đúng **một lần** không — rung nhiều lần là dấu hiệu Fusion tua lại lọt qua
  - Máy **Client** bắn vật có rung không (đây là lý do phải thêm `FireCount` networked)
- **😴 Ngủ đông vật thể** *(làm 16/08)* — nhớ bỏ tick `Is Kinematic` thủ công trước
  - Vật trên dốc có trượt một đoạn rồi **dừng hẳn** không
  - Bắn vật khác vào vật đang ngủ — nó phải văng đi tự nhiên, không được trơ ra
  - **TNT nổ có thổi bay được đồ đạc đang ngủ không** (nghi ngờ nhất)
  - ⚠️ **`NetworkRigidbody3D` có ghi đè `isKinematic` không** — tài liệu Fusion không nói rõ.
    Dấu hiệu xung đột: vật vẫn trượt dù đã ngủ, hoặc Client thấy vật ở chỗ khác Host.
    → Cách chữa nếu dính: đổi từ `isKinematic` sang `RigidbodyConstraints.FreezeAll`
- **Nhiều BoxCollider trên một vật** *(sửa 16/08)* — cầm cây lên bấm `V` tung.
  Bay lệch loạn xạ mỗi lần một hướng = còn collider nào chưa được tắt va chạm
- **Reset map đầu round mới** *(làm 09/08)* — `GameManager.ResetWorldObjects()`
  - Mọi vật thể có về đúng chỗ cũ, sạch điện, hết bị chế thành TNT không?
  - Thùng TNT đã nổ ở round trước có **sống lại** không?
  - Túi đồ có bị xoá phần vật thể map, mà **vẫn giữ đồ mua từ Shop** không?
  - Vật đang cầm trên tay lúc hết round có bị buông ra đúng cách không?
- **Nắm được vật TO** *(sửa 09/08)* — kéo cái bàn/khúc gỗ to nhất về, phải vào tay được
- **⚡ Toàn bộ chế độ Quá Tải** — ưu tiên test số 1
  - Lực văng có tăng đúng theo điện tích không (Console in `x2.4` mỗi lần trúng đòn)
  - Dash và Grapple có giữ nguyên cự ly ở mọi mức điện không (**phải giữ nguyên**)
  - Hết giờ 90s có phân thắng bại đúng theo tổng điện tích không
  - Giáp Cách Điện và Bộ Xả Điện còn hoạt động sau khi ánh xạ lại không
- Về MenuScene sau khi hết trận → **tạo/vào phòng mới có bình thường không?**
- 4 hiệu ứng vật phẩm qua Radial Menu
- Luật riêng Heavy (cản) và Spike (hút nhầm ăn x2)
- Quan sát khi chết

---

## 4. Nguyên tắc kiến trúc — đã trả cổ tức nhiều lần

### Chỉ đồng bộ những gì BẮT BUỘC

Thứ nào suy ra được tại chỗ thì tính tại chỗ. Đã dùng cho:

| Thứ | Cách làm |
|---|---|
| Vật cầm trên tay | Chỉ truyền `GrabbedObjectId`, mỗi máy tự đặt vị trí trong `LateUpdate()` |
| Thu nhỏ vật cầm | Mỗi máy tự đo bounds rồi scale, `SyncScale` để tắt |
| Rào chắn Buy Phase | Suy từ `GameManager.Phase`, là MonoBehaviour thường |
| Âm thanh | Móc vào `OnChangedRender` sẵn có, 0 byte thêm |
| Quan sát khi chết | Chỉ đổi camera nào đang bật |

### Một thứ chỉ được có MỘT chỗ ghi vào *(rút ra 16/08)*

`FPSMovement.Render()` là nơi duy nhất ghi vào transform của camera. `CameraShake` không tự xoay
camera mà chỉ *tính ra* độ lệch để `Render()` cộng vào.

Nếu hai script cùng ghi một thứ thì ai chạy sau sẽ xoá công người trước, mà thứ tự Unity gọi hàm
phụ thuộc vào thứ tự component và loại hàm (`Update` / `LateUpdate` / `Render` của Fusion) — rất
khó đoán và hay đổi khi thêm component mới. Gộp về một chỗ ghi thì không bao giờ có lớp lỗi đó.

Cùng lý do với việc **Animation gió đung đưa phải làm bằng vertex shader, không bằng Animator**:
Animator ghi `transform`, mà Rigidbody và Fusion cũng ghi `transform`. Shader đẩy đỉnh mesh lúc vẽ
nên không tranh với ai, và collider cũng đứng yên — đó là điều mong muốn chứ không phải hạn chế.

### Âm thanh / hiệu ứng: KHÔNG gọi trong `FixedUpdateNetwork()`

Fusion tua lại nhiều tick mỗi khung hình → một cú dash kêu 5–6 lần.
Cách chữa: biến đếm `[Networked]` + `OnChangedRender`. Xem `DashCount`, `MeleeCount`, `LaunchCount`.

### Đã thử và BỎ: cho Client dự đoán trước thao tác vật thể

Gỡ `if (!HasStateAuthority) return;` → **tệ hơn hẳn**, vật cầm trên tay giật và trễ nặng hơn.
Đã lùi. **Độ trễ đều đặn dễ quen tay hơn giật ngẫu nhiên.** Đừng thử lại.

### Vật bay lệch ngẫu nhiên → nghi collider chồng lấn TRƯỚC, đừng nghi lực

Lỗi tung `V` lệch loạn (sửa 09/08) hoá ra không phải do lực tung sai, mà do `RestoreScale()`
làm collider phình to **ngay trong người chơi**, PhysX bắn ra lực gỡ kẹt để tách ra.

Dấu hiệu nhận biết: **vật càng TO càng lệch nhiều**, vật nhỏ lại bay chuẩn. Nếu thấy quy luật
này thì gần như chắc chắn là chồng lấn collider chứ không phải sai số lực.

Chi tiết đầy đủ ở [CLAUDE.md](CLAUDE.md) mục 5, bẫy số 8.

### "Giật" khi test ParrelSync thường KHÔNG phải lỗi mạng

Đã mất nhiều thời gian nghi ngờ đồng bộ, cuối cùng đo ra **FPS chỉ 15–30** ở cửa sổ clone.
Test máy phụ → mượt hoàn toàn.

**ĐO FPS TRƯỚC KHI nghi ngờ code mạng.** Giảm tải: đóng Scene view ở clone (ăn nhiều nhất),
bật VSync, hạ độ phân giải Game view, hoặc build `.exe` chạy 1 build + 1 Editor.

### Các bẫy Fusion khác

Xem **CLAUDE.md mục 5** — 7 bẫy đã gặp kèm cách chữa (CharacterController, `NetworkTransform`,
`onBeforeSpawned` chỉ chạy trên Host, giá trị Inspector đè code...).

---

## 5. Bản đồ code

*(Đối chiếu thư mục thật 17/10 — 53 tệp ngoài `Editor/`.)*

```
Assets/
├── NetworkRunnerHandler.cs   Lobby, ghép trận, gửi input, spawn, quay về menu, kick + sổ đen. DontDestroyOnLoad
├── RoomPlayer.cs             Tên + đội trong phòng chờ, chế độ 1v1/2v2 (trên RoomPlayer chủ phòng), Kick()
├── Editor/BuildNop.cs        Build bản nộp 1 lệnh (icon + tắt Development Build). Chỉ Editor  (14/10)
└── Scripts/
    ├── GameManager.cs        Vòng đấu, tính điểm, hồi sinh, KillZone, Overtime
    ├── ShopManager.cs        Giao dịch qua RPC, Host duyệt        (trên Player.prefab)
    ├── AudioManager.cs       Singleton âm thanh                    (trong MenuScene)
    ├── GameSettings.cs       Class tĩnh: độ nhạy chuột, fullscreen
    ├── RoundBarrier.cs       Rào pha mua đồ — MonoBehaviour thường, hình shader BarrierCurtain (17/10)
    ├── ControlZone.cs        Khu chiếm đóng giữa map (MonoBehaviour, tiến độ nằm trên GameManager)
    ├── CloudBank.cs          Biển mây + đáy mây + làm mềm viền ảnh mây (17/10). [ExecuteAlways]
    ├── CursorLock.cs         Trọng tài khoá / thả con trỏ chuột
    ├── EnvironmentScatter.cs Công cụ rải trang trí (chỉ dùng khi dựng map)
    ├── Player/               FPSMovement · PlayerMagnetController · PlayerHealth · InventorySystem
    │                         PlayerHotbarController · PlayerInteract · RadialMenuController
    │                         NetworkInputData · PlayerEconomy · PlayerAnimatorDriver
    │                         CameraShake  ← MonoBehaviour thuần trên Camera; giữ quyền ghi FOV (cả AddFovPunch)
    │                         FirstPersonViewmodel ← tay góc nhìn thứ nhất, khựng tay khi đấm (17/10)
    │                         PlayerVisuals ← outline địch/bạn · GloveArcs ← tia điện trên găng
    │                         GlovePolarityColor · MagneticObjectInspector (công cụ Editor)
    ├── Item/                 MagneticObject · MagneticAura · ItemData · EMBarrier
    │                         CombatVFX ← kho hiệu ứng: tia nạp điện + sét, đấm, nổ TNT, can xăng (17/10)
    ├── MenuUI/               HUDController · ShopUI · ShopItemButton · SettingsUI · SpectatorController
    │                         RoomItemUI · MatchResultPanel · LoadingScreen · ControlsOverlay · PingDisplay
    │                         DeathScreen · MenuBackdrop · LobbyView · ModePicker · BuffScreenFX
    │                         Billboard · ButtonClickSound · CinematicCamera · PhotoFlyCamera
    └── Dummy/                DummyGravity · DummyMagnetTarget  (bù nhìn tập bắn, đã gỡ khỏi TestScene)
```

**Quy ước:** script UI đặt trên Canvas trong scene, đọc `FPSMovement.Local` để tìm nhân vật mình.
Script gameplay đặt trên `Player.prefab`.

### Prefab

`Prefab/Player.prefab` — 8 script + NetworkObject + NetworkTransform + CharacterController + Camera
*(NetworkTransform phải nằm TRÊN FPSMovement)*
`Prefab/GameManager.prefab` · `Prefab/EMBarrier.prefab` · `Prefab/RoomPlayer.prefab`
`Items/Type/*.asset` — 7 ItemData (tên, giá, icon, consumableType)
`Items/*.prefab` — prefab vật thể từ tính, cần `NetworkObject` + `NetworkRigidbody3D`

### Gasoline Canister — giới hạn cần nhớ

`ConvertToTNT()` **chỉ đổi được vật loại Normal**. Spike, Heavy, và TNT sẵn đều trả về `false`.
Đúng theo đặc tả, không phải lỗi. Dùng hụt thì **không mất chai**.

*(17/10)* Dùng **thành công** thì đã có hiệu ứng: xăng bắn tung + chớp lửa + vòng cam, rồi vật lách tách
tàn lửa suốt lúc còn là thùng nổ (`CombatVFX.GasolineSplash` / `CreateHazardEmbers`).
⚠️ Nhưng bấm **hụt** vẫn **không có phản hồi nào** — không tiếng, không log, không thông báo.
Người chơi sẽ tưởng vật phẩm hỏng. Nên thêm tiếng "tạch" hoặc dòng chữ trên HUD nếu còn thời gian.

**Cách nó nổ:** không có cơ chế riêng. Chỉ đổi `CurrentType = TNT`, rồi đi theo đúng đường của
TNT thường — `OnCollisionEnter` chỉ nổ khi `isMovingAsBullet == true`.
→ **Vật đã chế nằm dưới đất, ai đi vào cũng KHÔNG nổ.** Phải bắn/đẩy nó đi rồi đâm vào gì đó mới nổ.
Đây là "bom ném", không phải "mìn đặt".

### Các bước tạo 1 vật thể từ tính mới

| # | Việc | Ghi chú |
|---|---|---|
| 1 | Kéo model vào scene | |
| 2 | **Tag = `Magnetic`** | ⚠️ Thiếu là raycast bỏ qua hoàn toàn, bấm chuột không ăn gì |
| 3 | **Collider** | Không tick Is Trigger |
| 4 | **Rigidbody** | |
| 5 | **NetworkObject** + **NetworkRigidbody3D** | Thiếu cái sau là máy kia thấy vật ở (0,0,0) |
| 6 | **MagneticObject** | `objectType` · `baseDamage` (10/20/25, TNT dùng `tntDamage` 35) · `itemData` |
| 7 | **MagneticAura** *(tuỳ chọn)* | Hiệu ứng hào quang |

Renderer phải có Material riêng — script tự đổi màu theo điện tích.

---

## 6. Quyết định còn treo

### ✅ ĐÃ LÀM 16/08: Hồi sinh + Khu chiếm đóng — CHƯA TEST

> Toàn bộ đề xuất bên dưới **đã được code xong**, biên dịch sạch 0 error.
> Ba quyết định khác với bản đề xuất gốc:
>
> 1. **`ControlZone` là MonoBehaviour thuần**, không phải NetworkBehaviour. Tiến độ chiếm
>    nằm trên `GameManager` (vốn đã là NetworkObject). Tránh phải đặt NetworkObject sẵn
>    trong scene — thêm một chỗ có thể hỏng mà không được gì.
> 2. **Mỗi đội một thanh riêng, KHÔNG tụt.** Bỏ cơ chế tụt vì nó kéo dài round và gây ức chế;
>    đồng hồ `combatDuration` đã đủ chặn round lê thê.
> 3. **`EndRoundByCharge()` đã bị XOÁ.** Hết giờ giờ so **tiến độ chiếm**, không so điện tích.
>    `CheckRoundOver()` (xoá sổ cả đội) cũng bỏ — có hồi sinh thì xoá sổ chỉ là tạm thời.
>
> **Việc còn phải làm trong Unity:**
> 1. Tạo Empty GameObject giữa map → gắn **`ControlZone`** → chỉnh `radius` / `halfHeight`
>    *(Scene view có vẽ hình trụ vàng để căn)*
> 2. Dựng UI cho 5 ô mới trên `HUDController`: `zoneGroup`, `zoneRedFill`, `zoneBlueFill`,
>    `zoneStatusText`, `respawnCountdownText`. Bỏ trống vẫn chạy, chỉ là không thấy tiến độ.
>
> **Số liệu mặc định** *(trên `GameManager.prefab`)*: `zoneCaptureRate 10` · `zoneProgressToWin 100`
> · `zoneTwoPlayerMultiplier 1.5` · `deathZoneBonus 8` · `respawnDelay 5`
> → đứng một mình 10 giây liên tục là thắng round.

<details>
<summary>Bản đề xuất gốc ngày 09/08 (giữ lại để tra lý do thiết kế)</summary>

### 🔵 Hồi sinh + Khu chiếm đóng

Ý của chủ project: vì game xoay quanh knockback, hãy **bỏ luật "chết là bị loại cả round"**,
đổi sang **chết → hồi sinh sau X giây**, và thêm **một khu vực nhỏ mà người chơi phải đứng
trong đó một thời gian** để thắng.

**Vì sao đáng làm — 3 lý do cụ thể:**

1. **Sửa đúng lỗ hổng lớn nhất của Quá Tải.** Người chơi vốn sẽ tránh xa rìa vực.
   Khu chiếm đóng **ép họ đứng vào một chỗ cố định do mình chọn** → quyết định luôn nơi giao tranh.
2. **Làm knockback bớt ức chế.** Bị hất khỏi map hiện mất cả round (ngồi nhìn 90s, rất cay).
   Có hồi sinh thì chết = mất thời gian + mất vị trí, không mất trận → được phép để hệ số
   knockback mạnh tay hơn.
3. **Xoá bớt code.** `EndRoundByCharge()` (hết giờ so tổng điện) vốn là giải pháp tình thế cho
   "không ai chết thì round không kết thúc". Có khu chiếm đóng thì round luôn có đường kết thúc
   tự nhiên → **xoá hàm đó đi**.

**Chi phí ước lượng: ~1,5–2 ngày code.** Hai mảnh khó nhất đã có sẵn dùng lại được nguyên vẹn:
`NetworkRunnerHandler.GetSpawnPosition(team, index)` và `PlayerHealth.Respawn(pos, yaw)`.

| Việc | Ước lượng |
|---|---|
| Hồi sinh có hẹn giờ | `TickTimer` trong `PlayerHealth`, đếm trong vòng lặp `CheckKillZone` đã có | ~40 dòng |
| `ControlZone.cs` mới | Đếm người trong bán kính bằng `PlayerHealth.AllPlayers`, không cần `OverlapSphere` | ~130 dòng |
| Sửa `GameManager` | Đổi điều kiện thắng round, **xoá** `EndRoundByCharge()` | ~50 dòng |
| HUD | Thanh chiếm đóng + đồng hồ hồi sinh | ~50 dòng + dựng UI |

**KHÔNG phải đụng tới:** kinh tế, Shop, rào chắn Buy Phase, âm thanh, `SpectatorController`,
toàn bộ `MagneticObject`. Cấu trúc round giữ nguyên (best-of-5), chỉ đổi *cách thắng một round*.

#### ⚠️ Bẫy BẮT BUỘC phải chốt trước khi code

`PlayerHealth.Respawn()` hiện **xả điện tích về 0**. Ghép với hồi sinh tự động thì:
đang nhiễm 95% điện, sắp bị hất bay → **tự nhảy xuống vực** → 5 giây sau sống lại sạch điện.
**Tự sát thành nước đi tối ưu.**

→ Cách chữa khuyên dùng: **chết thì đội địch được cộng tiến độ chiếm đóng (+8%)**.
Giải quyết gọn cả ba việc: tự sát luôn có giá · hất địch khỏi map **trực tiếp đẩy mình tới
chiến thắng** (knockback và mục tiêu khớp thành một) · không cần thêm luật rườm rà nào.
*(Phương án thay thế: hồi sinh chỉ xả 50% điện — đơn giản hơn nhưng không có sự cộng hưởng trên.)*

#### Phạm vi đề xuất cho bản đầu

- **Làm:** 1 khu chiếm đóng **cố định**, hình tròn. Cả hai đội cùng đứng = **đóng băng**. Đủ 100% = thắng round.
- **Cắt:** khu chiếm đóng **di chuyển** (kiểu Hardpoint). Tốn gấp đôi, phải cân bằng lại từ đầu.

#### Tin tốt về map

**Lõi Từ ở giữa map (mục 3) chính là khu chiếm đóng lý tưởng, không phải vẽ lại gì.**
Cao 7m trống trải → đứng đó là phơi mình · bị đấm trên cao thì bay xa hơn hẳn ·
cách rìa đủ xa để không chết ngay, đủ gần để một cú mạnh là xong · 4 dốc lên nên không thủ kín được.
Chỉ cần thêm `ControlZone` ở `(40, 7, 40)` bán kính ~7m.

#### Còn phải chốt 2 con số

1. **Thời gian hồi sinh** — đề xuất **5 giây**
2. **Chết có cộng tiến độ cho địch không** — đề xuất **có, +8%**

> 🚨 **Cảnh báo về việc đổi hướng liên tục.** Đây là lần đổi thiết kế thứ hai trong hai ngày
> (30/07 bỏ luật riêng từng loại đạn → 09/08 Quá Tải → 09/08 đề xuất này). Bản thân thay đổi
> hợp lý, nhưng còn **5 tuần và map vẫn chưa dựng**. Nếu quay lại làm cái này thì
> **làm xong rồi KHOÁ thiết kế**, dồn toàn bộ thời gian còn lại cho map và test.
> Có ý tưởng thứ ba thì ghi vào đây để đó, đừng làm.

</details>

- **Đền bù kinh tế khi đồng đội thoát giữa trận** — `OnPlayerLeft` chưa despawn nhân vật,
  người thoát để lại xác đứng im. Chủ project muốn gộp với cơ chế bù tiền 1v2, chưa chốt con số.
- **Xử lý hoà** (cả hai đội cùng chết) — tạm chọn: không ai được điểm, sang round mới.

---

## 7. Đề xuất thứ tự cho 6 tuần còn lại

Còn ~5 tuần tính từ 09/08.

| Tuần | Việc |
|---|---|
| 1 | **Map — dựng blockout bằng cube theo bố cục "Lõi Từ"**, test Quá Tải, cân lại `knockbackAtMaxCharge` |
| 2 | *(nếu chọn làm)* Hồi sinh + Khu chiếm đóng — xem mục 6. Rồi **KHOÁ THIẾT KẾ** |
| 3 | Dán art lên map + âm thanh + dựng nốt Settings/Spectator |
| 4 | Test kỹ 2 máy, cân bằng lại toàn bộ số liệu |
| 5 | **Dự phòng + build + viết báo cáo + chuẩn bị bảo vệ** |

Chừa tuần cuối là cố ý. Đồ án luôn phát sinh.

⚠️ Nếu tới cuối tuần 2 mà map vẫn chưa chơi được thì **bỏ mục 6, giữ nguyên Quá Tải** và dồn hết
cho map. Game có bố cục tử tế mà luật đơn giản vẫn hơn game luật hay mà map toàn cube.
