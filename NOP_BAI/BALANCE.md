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
| `killZoneY` | **-20** | Inspector | Rơi thấp hơn mức này là chết — **chỉ trong pha chiến đấu**. Pha khác thì được kéo về điểm xuất phát *(14/10)* |

`Player.prefab` — `PlayerHealth`

| Ô | Giá trị | Nguồn | Ý nghĩa |
|---|---|---|---|
| `attackerMemory` | **6** | code | Bị đối thủ đẩy xong bao nhiêu giây mà rơi thì vẫn tính là họ hạ mình (hiện tên trên bảng chết). Quá lâu thì tính là tự rơi *(14/10)* |

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
| `knockbackMultiplier` | **0.95** | Inspector | Hệ số lực đẩy TOÀN CỤC (đấm + TNT + đạn). Hạ 0.975 → 0.95 ngày 10/10 |
| `stunDuration` | **1** | Inspector | Choáng khi trúng đòn (giây) |

## Chiến đấu & găng tay

`Player.prefab` — `PlayerMagnetController`

| Ô | Giá trị | Nguồn | Ý nghĩa |
|---|---|---|---|
| `meleeRange` | **6** | Inspector | Tầm đấm |
| `meleePushForce` | **200** | Inspector | Lực đấm |
| `meleeCooldown` | **0.35** | Inspector | Hồi chiêu đấm (giây) |
| `hitStopDuration` | **0.06** | Inspector *(ô mới 17/10)* | Khựng tay khi đấm TRÚNG (giây) — chỉ hình ảnh trên máy người đấm, không đổi luật. 0 = tắt, quá 0.1 thấy ì tay |
| `grapplePullForce` | **250** | Inspector | Lực kéo áp sát |
| `dashLockRange` | **60** | Inspector | Tầm xa nhất để khoá mục tiêu kéo áp sát (mặc định code 20). Kéo áp sát xảy ra khi địch ở xa hơn `meleeRange` và gần hơn số này |
| `pullForce` | **49** | Inspector | Tốc độ hút vật về tay, m/s. Nâng 36 → 49 ngày 10/10 (+35%). ⚠️ Ô này còn nhân vào cú phản đòn khi hút nhầm Spike — xem `spikePullBoost` |
| `spikePullBoost` | **1.5** | Inspector | Hút nhầm bóng gai thì nó lao ngược lại với lực `pullForce × 1.5`. Từ 10/10 là `49 × 1.5 = 73.5` (trước 54) — cú phạt Spike mạnh lên 35% theo pullForce, **có chủ ý** |
| `pushSpeed` | **60** | Inspector | Tốc độ đẩy vật (chuột trái, vật cùng dấu). Nâng 50 → 60 ngày 16/10 (+20%) |
| `pushLiftOffset` | **0.5** | Inspector | Trước khi đẩy, nâng vật **TỚI** độ cao này (m) tính từ mặt đất, để bay qua gờ đất. Vật đang nảy trên không, đã cao sẵn thì không nâng thêm. Đổi 0.3 → 0.5 ngày 16/10 |
| `pushUpwardBias` | **0.033** | Inspector | Cú đẩy chếch lên một góc **cố định ~1.9°**, nhìn lên hay xuống cũng vậy. Hạ 0.055 → 0.033 (= 3/5) ngày 16/10. Trước 16/10 góc này đổi theo hướng nhìn: nhìn dốc xuống thì chếch tới ~30° — đã sửa |
| `fireSpeed` | **54** | Inspector | Tốc độ bắn vật đang cầm (chuột phải). Nâng 45 → 54 ngày 16/10 (+20%) |
| `shootRange` | **50** | Inspector | Tầm với của găng |
| `heldObjectSize` | **1.2** | Inspector | Cạnh dài nhất của vật cầm trên tay |
| `heldObjectThickness` | **0.55** | code | Bề ngang tối đa của vật cầm trên tay |
| `aimAssistRadius` | **1.2** | Inspector | Bán kính hỗ trợ ngắm |
| `aimAssistAngle` | **12** | Inspector | Góc hỗ trợ ngắm xa |
| `closeAssistAngle` | **70** | Inspector | Góc hỗ trợ ngắm gần |

## Kinh tế *(thêm 16/10)*

`Player.prefab` — `PlayerEconomy`

| Ô | Giá trị | Nguồn | Ý nghĩa |
|---|---|---|---|
| `startingMoney` | **300** | Inspector | Tiền đầu trận (mặc định code 500 — Inspector đè lên) |
| `winReward` | **150** | Inspector | Thắng round |
| `loseReward` | **100** | Inspector | Thua round |
| `moneyCap` | **750** | Inspector | Trần ví |

## Tường EM Barrier *(thêm 14/10)*

`EMBarrier.prefab` — `EMBarrier` · khoảng cách thả nằm ở `Player.prefab` — `InventorySystem`

| Ô | Giá trị | Nguồn | Ý nghĩa |
|---|---|---|---|
| `lifetime` | **10** | Inspector | Tồn tại bao nhiêu giây. 2 giây cuối nhấp nháy báo sắp hết |
| `panelSize` | **4 × 3** | code | Rộng × cao tấm khiên (mét). Va chạm tự khớp theo số này |
| `hoverHeight` | **0.3** | code | Tấm khiên lơ lửng cách đất. Va chạm vẫn chạm sát đất → cao tổng **3.3m** |
| `colliderThickness` | **0.35** | code | Độ dày va chạm. Hình thì mỏng như tờ giấy, nhưng va chạm mỏng quá thì vật bay nhanh xuyên qua |
| `emBarrierDistance` | **3** | Inspector | Thả cách người chơi bao nhiêu mét |

> ⚠️ **Trước 14/10 tường thực ra chỉ cao 2.5m trên mặt đất**, dày 0.5m: khối hộp 4×3 đặt tâm ở
> gốc nhân vật (cao hơn chân ~1m), nên 0.5m chân tường chìm dưới đất. Giờ tường tự dò mặt đất,
> đứng đúng trên đó → **che cao hơn 0.8m so với trước**.

## Đạn — prefab vật thể từ tính

| Loại | Điện tích gây ra | Khối lượng | Số prefab |
|---|---|---|---|
| **Heavy** | 20 | 3 | 12 |
| **Normal** | 10 | 1 | 4 |

> ⚠️ Điện tích thật mà nạn nhân nhận = cột trên × `chargeGainMultiplier`. Muốn chỉnh độ
> sát thương chung thì sửa hệ số đó, **đừng** đi sửa từng prefab.

### Ô chung trên mọi prefab vật thể — `MagneticObject`

> **16/10 — đường bay của đạn:** trọng lực project chỉ -3.
> - `bulletGravityMultiplier` **2 → 0.5**: đạn đang bay rơi chậm (gia tốc 1.5). Bắn ngang với 54 m/s
>   bay được ~76m trước khi chạm đất — ngang cả đảo. Trước đó ~30m, chưa tới nửa đảo.
>   Đã sửa trên **16 prefab** + mặc định code (prefab đá không lưu ô này, tự lấy mặc định).
> - `afterBulletGravityMultiplier` **2.5** *(ô mới, code)*: từ lúc vật hết làm đạn tới khi chạm đất
>   thì rơi với gia tốc 7.5 — hết cảnh vật đâm người xong trôi lững lờ. Không áp lên vật nằm yên,
>   vật tung bằng `V`, vật bị nổ hất.

| Ô | Giá trị | Nguồn | Ý nghĩa |
|---|---|---|---|
| `hitKnockbackForce` | **120** | Inspector | Lực hất khi trúng đạn, nhân tiếp với (sát thương / 10) |
| `bulletKnockbackMultiplier` | **1.2** | code + prefab + scene | Hệ số nhân thêm cho lực hất của đạn. Hạ 1.6 → 1.2 ngày 10/10 (nerf 25%), **đã đồng bộ cả ba nơi** |

> ✅ **Đã đồng bộ ba nơi ngày 10/10**: mặc định code (`MagneticObject.cs:57`), 4 prefab
> `Tree9_*`, và 38 vật ghi đè trong `TestScene`. Nhờ vậy kéo thêm vật mới vào map thì nó
> hất đúng bằng những vật đang có — trước đó vật mới sẽ mạnh hơn 33% mà không ai nhận ra.


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
