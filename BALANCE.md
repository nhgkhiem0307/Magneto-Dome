# BẢNG SỐ LIỆU CÂN BẰNG — Magneto-Dome

> Sinh tự động ngày **25/09/2026** bằng cách quét thẳng prefab và scene.


⚠️ **File này là bản CHỤP, không phải nguồn.** Nguồn thật vẫn nằm trong prefab và scene.
Sửa số ở đây KHÔNG làm game đổi theo. Tác dụng của nó là:

1. **Phao cứu sinh** — scene hay prefab hỏng thì còn biết đường gõ lại.
2. **Nguyên liệu báo cáo** — chương cân bằng gameplay chép thẳng từ đây.
3. **Thấy được cái gì lệch** — cột "Nguồn" cho biết con số đang nằm ở Inspector hay đang
   lấy mặc định từ code.

⚠️ **Cột Nguồn quan trọng hơn vẻ ngoài của nó.** `Inspector` nghĩa là sửa trong Unity mới
ăn — sửa giá trị mặc định trong code sẽ KHÔNG có tác dụng gì (bẫy số 7 ở CLAUDE.md).
`code` nghĩa là ô đó chưa từng được lưu vào prefab, nên sửa trong code là ăn ngay.


## Vòng đấu & round

`GameManager.prefab` — `GameManager`

| Ô | Giá trị | Nguồn | Ý nghĩa |
|---|---|---|---|
| `pointsToWin` | **5** | Inspector | Số round cần thắng để vô địch |
| `requiredLead` | **2** | Inspector | Cách biệt tối thiểu, không đủ thì Overtime |
| `buyPhaseDuration` | **15** | Inspector | Pha chuẩn bị (giây) |
| `combatDuration` | **300** | code | Pha chiến đấu (giây) |
| `roundEndDuration` | **5** | Inspector | Nghỉ giữa round (giây) |
| `respawnDelay` | **5** | code | Chết bao lâu thì sống lại (giây) |
| `killZoneY` | **-20** | Inspector | Rơi thấp hơn mức này là chết |

## Khu chiếm đóng

`GameManager.prefab` — `GameManager`

| Ô | Giá trị | Nguồn | Ý nghĩa |
|---|---|---|---|
| `zoneProgressToWin` | **100** | code | Tiến độ cần đạt để thắng round |
| `zoneCaptureRate` | **1.667** | code | Tiến độ cộng mỗi giây khi đứng một mình |
| `zoneTwoPlayerMultiplier` | **1.5** | code | Hệ số khi có 2 người cùng đội |
| `deathZoneBonus` | **8** | code | Tiến độ thưởng khi hạ một địch |

## Điện tích & Quá Tải

`Player.prefab` — `PlayerHealth`

| Ô | Giá trị | Nguồn | Ý nghĩa |
|---|---|---|---|
| `maxCharge` | **100** | Inspector | Điện tích tối đa |
| `knockbackAtMaxCharge` | **4** | Inspector | Hệ số lực văng khi đầy điện |
| `chargeGainMultiplier` | **1.5** | Inspector | Nhân MỌI lượng điện nhận vào |
| `maxArmor` | **30** | Inspector | Giáp tối đa |
| `spawnProtectDuration` | **2** | code | Bất tử sau hồi sinh (giây) |
| `chargeCriticalRatio` | **0.8** | code | Ngưỡng cảnh báo quá tải |

## Di chuyển

`Player.prefab` — `FPSMovement`

| Ô | Giá trị | Nguồn | Ý nghĩa |
|---|---|---|---|
| `moveSpeed` | **7** | Inspector | Tốc độ chạy |
| `jumpHeight` | **1** | Inspector | Độ cao nhảy (mét) |
| `airControl` | **0.8** | Inspector | Lái được trên không |
| `dashForce` | **70** | Inspector | Lực Dash |
| `dashCooldown` | **1** | Inspector | Hồi chiêu Dash (giây) |
| `mass` | **3** | Inspector | Khối lượng - CHIA vào mọi lực đẩy |
| `knockbackMultiplier` | **0.975** | Inspector | Hệ số lực đẩy TOÀN CỤC |
| `stunDuration` | **1** | Inspector | Choáng khi trúng đòn (giây) |

## Chiến đấu & găng tay

`Player.prefab` — `PlayerMagnetController`

| Ô | Giá trị | Nguồn | Ý nghĩa |
|---|---|---|---|
| `meleeRange` | **6** | Inspector | Tầm đấm |
| `meleePushForce` | **200** | Inspector | Lực đấm |
| `meleeCooldown` | **0.35** | Inspector | Hồi chiêu đấm (giây) |
| `grapplePullForce` | **250** | Inspector | Lực kéo áp sát |
| `pullForce` | **36** | Inspector | Lực hút vật |
| `pushSpeed` | **50** | Inspector | Tốc độ đẩy vật |
| `shootRange` | **50** | Inspector | Tầm với của găng |
| `heldObjectSize` | **1.2** | Inspector | Cạnh dài nhất của vật cầm trên tay |
| `heldObjectThickness` | **0.55** | code | Bề ngang tối đa của vật cầm trên tay |
| `aimAssistRadius` | **1.2** | Inspector | Bán kính hỗ trợ ngắm |
| `aimAssistAngle` | **12** | Inspector | Góc hỗ trợ ngắm xa |
| `closeAssistAngle` | **70** | Inspector | Góc hỗ trợ ngắm gần |

## Đạn — prefab vật thể từ tính

| Loại | Điện tích gây ra | Khối lượng | Số prefab |
|---|---|---|---|
| **Heavy** | 20 | 3 | 12 |
| **Normal** | 10 | 1 | 4 |

> ⚠️ Điện tích thật mà nạn nhân nhận = cột trên × `chargeGainMultiplier`. Muốn chỉnh độ
> sát thương chung thì sửa hệ số đó, **đừng** đi sửa từng prefab.


## Đạn THỰC SỰ có trên map (TestScene)

| Loại | Số vật trên map | Điện tích gây ra |
|---|---|---|
| Normal | **14** | 10 |
| Heavy | **24** | 20 |
| Spike | **0** | — |
| TNT | **0** | — |
| **Tổng** | **38** | |

> Normal = 14 cây (`Tree9_* Variant`), Heavy = 24 tảng đá (`Rock* Variant`).
> Spike và TNT chưa có vật nào trên map: TNT chỉ xuất hiện khi ai đó mua
> Chai Xăng để chế một vật Normal thành TNT.
