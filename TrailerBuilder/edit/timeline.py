# -*- coding: utf-8 -*-
"""
BẢNG THỜI GIAN TRAILER — file DỮ LIỆU duy nhất cần sửa khi dựng lại / chỉnh trailer. (viết lại 25/10)
build_trailer.py đọc file này rồi dựng. Mọi mốc thời gian tính bằng PHÁCH NHẠC (beat), để đổi
bài nhạc thì chỉ cần sửa BPM + MUSIC_OFFSET, các cú cắt tự rơi đúng nhịp.

Quy ước:
  phách x       -> 0 = đầu trailer. 4 phách = 1 ô nhịp (bar). 128 BPM: 1 phách = 0,469s.
  Đoạn DROP (bùng nổ) nằm ở phách 16. Nhạc thật phải được đặt sao cho chỗ "drop" của bài
  rơi đúng phách 16  ->  MUSIC_OFFSET = (giây có drop trong bài) - DROP_BEAT * 60 / BPM.

Nguồn hình:
  cinematic/...  3 cảnh TOÀN ĐẢO render sẵn (không có nhân vật) - Trailer_Footage/cinematic/
  obs/...        clip chủ project tự quay bằng OBS, góc nhìn thứ nhất - Trailer_Footage/obs/
                 (ghi tên KHÔNG cần đuôi: tự nhận .mp4 / .mkv / .mov)

Toàn bộ ý đồ từng cảnh + cách quay: TRAILER_SCRIPT.md (gốc project).
"""

# ==================== NHẠC ====================
# "Upbeat Rock" - JonasBlakewood (Pixabay, mã 573489). Đã khai ở CREDITS.md mục 4.
# Đo 25/10: 130 BPM, khoảng lặng ở giây 90,5, DROP ở giây 94,18 (= phách 16), CÚ KẾT THẬT của bài ở giây 134,8
# (= phách 104), hợp âm cuối ngân tới ~136,5s rồi tắt -> trailer dài 107,5 phách để nhạc tự kết, không bị cắt ngang.
BPM = 130.0            # nhịp của bài nhạc (beats.py ước 130,73 - đo lại trên đoạn 60-134s ra 129,99)
MUSIC_FILE = None      # None = tự lấy file đầu tiên trong Trailer_Footage/music/, không có thì dùng nhạc tạm
MUSIC_OFFSET = 86.794  # bắt đầu phát bài từ giây này (= drop 94,18 - 16 phách)
MUSIC_GAIN = 0.85
DROP_BEAT = 16
# Tua bài nhanh/chậm để cú kết của bài khớp cuối video - TỐI ĐA ±12,5% (0.875 - 1.125), giữ nguyên cao độ.
# Nhịp hiệu dụng = BPM x MUSIC_SPEED, mọi mốc phách tự co giãn theo. Cách khác: kéo dài / rút ngắn màn logo
# (len của cảnh cuối + các dòng chữ end_*). Chọn cách nào thì nghe lại đoạn cuối xem nhạc có bị cắt giữa câu không.
MUSIC_SPEED = 1.0

# ==================== CLIP OBS ====================
# tên dùng trong SHOTS -> (file thật trong Trailer_Footage/obs/, giây đầu, giây cuối phần DÙNG ĐƯỢC).
# File OBS giữ nguyên tên gốc. Giây cuối cắt bỏ đoạn bảng Settings hiện lên lúc bấm Esc dừng quay
# (dò tự động theo độ tối vùng giữa màn hình 25/10, clip 07_overload chỉnh tay vì bảng hiện trên màn chết sáng).
# Gán theo thứ tự giờ quay (chủ project dặn: file 1 và 2 đảo cho nhau) + xem nội dung từng clip.
CLIPS = {
    '01_charge':   ('Magneto-Dome 2026-10-25 17-06-47.mp4', 0.0, 4.47),
    '02_pull':     ('Magneto-Dome 2026-10-25 17-06-32.mp4', 0.0, 4.07),
    '03_push_b':   ('Magneto-Dome 2026-10-25 17-10-32.mp4', 0.0, 4.70),   # lần quay đẩy thứ 1
    '03_push':     ('Magneto-Dome 2026-10-25 17-11-02.mp4', 0.0, 5.07),   # lần quay đẩy thứ 2 (đẩy 2 tảng)
    '04_fire':     ('Magneto-Dome 2026-10-25 19-44-32.mp4', 0.0, 5.57),   # nạp Heavy -> hút về tay -> bắn
    '05_tnt':      ('Magneto-Dome 2026-10-25 19-53-22.mp4', 0.0, 6.90),
    '06_scan':     ('Magneto-Dome 2026-10-25 19-54-54.mp4', 0.0, 7.53),
    '07_overload': ('Magneto-Dome 2026-10-25 20-41-49.mp4', 0.0, 14.10),  # góc bị đánh: trúng đòn -> văng khỏi đảo -> KNOCKED OUT
    '08_punch':    ('Magneto-Dome 2026-10-25 20-46-52.mp4', 0.0, 7.50),
    '09_edge':     ('Magneto-Dome 2026-10-25 20-49-12.mp4', 0.0, 7.10),
    '10_fall':     ('Magneto-Dome 2026-10-25 20-51-55.mp4', 0.0, 9.53),
    '11_shop':     ('Magneto-Dome 2026-10-25 20-53-44.mp4', 0.0, 7.57),
    '12_radial':   ('Magneto-Dome 2026-10-25 20-54-10.mp4', 0.0, 10.10),  # vòng chọn đồ + dựng tường EM
    '18_gasoline': ('Magneto-Dome 2026-10-25 20-54-27.mp4', 0.90, 8.73),  # THÊM: can xăng biến cây thành TNT -> nổ
    '13_moves':    ('Magneto-Dome 2026-10-25 20-55-19.mp4', 0.0, 7.40),
    '14_zone':     ('Magneto-Dome 2026-10-25 20-56-31.mp4', 0.0, 6.97),
    '15_barrier':  ('Magneto-Dome 2026-10-25 20-57-48.mp4', 0.0, 13.83),
    '16_lobby':    ('Magneto-Dome 2026-10-25 20-59-34.mp4', 0.0, 15.57),
    # '17_best' chưa quay -> cảnh cao trào dùng tạm 18_gasoline (alt)
}

# ==================== CẢNH (VIDEO) ====================
# at/len : vị trí + độ dài trên trailer, tính bằng PHÁCH. Cảnh sau bắt đầu đúng chỗ cảnh trước kết thúc.
#
# LẤY ĐOẠN NÀO CỦA CLIP (clip quay tay dài ngắn tuỳ ý, KHÔNG cần cắt chuẩn):
#   pick  : 'first' 'last' 'loudest' 'hit2' 'hit3'...  -> tự dò TIẾNG VA CHẠM trong tiếng game của clip
#                                                       (va chạm đầu / cuối / to nhất / thứ 2...)
#           'mid' (mặc định)                         -> lấy quanh GIỮA clip (cảnh không có va chạm: Shop, scan...)
#   hit   : khoảnh khắc đó rơi vào phách thứ mấy CỦA CẢNH (0 = ngay đầu cảnh). Đặt va chạm đúng phách = đã tai.
#   src_t : ghi tay giây của khoảnh khắc trong clip (khi tự dò chọn sai) - thắng pick
#   nudge : dời khoảnh khắc thêm vài phần giây (âm = sớm hơn) khi tiếng va chạm lệch hình
#   ss    : lấy đúng từ giây này (dùng cho cảnh render sẵn)
#   speed : tốc độ phát (0.5 = chậm một nửa). Clip ngắn quá thì bộ dựng TỰ làm chậm cho vừa (tối đa x0.5)
#   alt   : cảnh tuỳ chọn - thiếu file thì dùng cảnh này thay
#
# fx (hiệu ứng) - xem build_trailer.py -> segment_filters(). Số sau @ là giây tính từ đầu cảnh,
#                 '@hit' = đúng khung khoảnh khắc chính (tự theo pick/hit):
#   'fadein:0.6' 'fadeout:0.7'  mờ từ đen / về đen        'zoom:1.0>1.08'  phóng chậm suốt cảnh
#   'punch'                     đầu cảnh phóng 12% rồi thu về trong 0.22s
#   'flash@hit' 'shake@hit:24' 'rgb@hit'   loé trắng / rung (biên độ px) / tách màu đỏ-xanh 0.16s
#   'blur:16' 'dark:0.38'       mờ / tối (nền màn logo)  'clipaudio:0.5'  giữ tiếng gốc của clip ở mức 0.5
# note : in lên thẻ giữ chỗ khi chưa có file
HIT = ['flash@hit', 'shake@hit:26', 'rgb@hit']          # bộ 3 va chạm mạnh dùng chung

SHOTS = [
    # --- 1. ĐÂY LÀ ĐÂU? (0 - 5.6s) ---
    dict(at=0,  len=12, src='cinematic/s1_island.mp4', ss=0.6, fx=['fadein:0.6', 'zoom:1.0>1.07']),

    # --- 2. ĐÁNH NHAU BẰNG GÌ? (5.6 - 15s) ---
    dict(at=12, len=2, src='obs/01_charge', pick='first', hit=0, fx=['punch', 'flash@hit', 'clipaudio:0.5'],
         note='Chuột trái vào tảng đá CHƯA có điện -> tia laze, đá hiện viền màu'),
    dict(at=14, len=2, src='obs/02_pull', src_t=2.3, hit=0.5,   # clip không có tiếng va chạm rõ -> ghi tay lúc đá lao về fx=['punch', 'flash@hit', 'clipaudio:0.5'],
         note='Giữ chuột trái HÚT đá trái dấu bay về tay'),
    dict(at=16, len=4, src='obs/03_push', pick='loudest', hit=0, fx=HIT + ['clipaudio:0.5'],
         note='DROP: chuột trái ĐẨY đá cùng dấu bay vút ra phía vực'),
    dict(at=20, len=4, src='obs/06_scan', pick='mid', fx=['zoom:1.0>1.05', 'clipaudio:0.3'],
         note='Rê tâm ngắm chậm qua 3-4 tảng đá: khung phân tích ôm từng tảng'),
    dict(at=24, len=4, src='obs/04_fire', pick='hit1', hit=2.5, fx=['flash@hit', 'shake@hit:18', 'clipaudio:0.55'],
         note='Cầm đá -> bấm 1/2 đổi cực (vòng tâm ngắm đổi màu) -> chuột phải bắn trúng đối thủ'),
    dict(at=28, len=4, src='obs/05_tnt', pick='hit1', hit=2, fx=HIT + ['clipaudio:0.6'],
         note='Bắn TNT vào đối thủ / mặt đất gần họ -> nổ, hất văng'),

    # --- 3. CÓ GÌ ĐẶC BIỆT? QUÁ TẢI (15 - 20.6s) ---
    dict(at=32, len=8, src='obs/07_overload', pick='first', hit=0, fx=['flash@hit', 'clipaudio:0.6'],
         note='GÓC NGƯỜI BỊ ĐÁNH: trúng 4-5 đòn, HUD điện tích leo 0 -> 80%, hiện x2.x x3.x'),
    dict(at=40, len=4, src='obs/08_punch', pick='last', hit=2, fx=['zoom:1.02>1.1'] + HIT + ['clipaudio:0.6'],
         note='Đấm (chuột phải tay trống) đối thủ đã nhiễm nhiều điện -> văng rất xa'),

    # --- 4. THUA KIỂU GÌ? RƠI KHỎI ĐẢO (20.6 - 26.3s) ---
    dict(at=44, len=4, src='obs/09_edge', pick='loudest', hit=1, fx=HIT + ['clipaudio:0.6'],
         note='Đối thủ đứng mép đảo -> đấm/bắn văng khỏi đảo, nhìn theo họ rơi xuống mây'),
    dict(at=48, len=8, src='obs/10_fall', pick='first', hit=0, fx=['flash@hit', 'shake@hit:30', 'rgb@hit', 'clipaudio:0.6'],
         note='GÓC NGƯỜI BỊ ĐÁNH: bị hất khỏi mép đảo, rơi xuyên biển mây, màn hình chết hiện ra'),

    # --- 5. CÒN GÌ NỮA? DỒN DẬP - mỗi cảnh 2 phách (26.3 - 35.6s) ---
    dict(at=56, len=2, src='obs/03_push_b', pick='loudest', hit=0.5, fx=HIT + ['clipaudio:0.5'],
         note='(lần quay đẩy thứ 1)'),   # clip TNT chỉ có 1 vụ nổ, đã dùng ở phách 30
    dict(at=58, len=2, src='obs/11_shop', pick='mid', fx=['punch', 'clipaudio:0.4'],
         note='Pha mua đồ: B mở Shop, rê chuột, mua 1-2 món'),
    dict(at=60, len=2, src='obs/08_punch', pick='first', hit=0.5, fx=HIT + ['clipaudio:0.5'],
         note='(dùng lại clip đấm - cú đấm ĐẦU)'),
    dict(at=62, len=2, src='obs/12_radial', pick='mid', fx=['punch', 'clipaudio:0.4'],
         note='Giữ Tab mở vòng chọn đồ, chọn Nước tăng lực, thả -> màn hình loé'),
    dict(at=64, len=2, src='obs/16_lobby', pick='mid', fx=['punch', 'clipaudio:0.3'],
         note='Phòng chờ: thẻ người chơi, chủ phòng bấm 1V1 / 2V2',
         alt=dict(src='cinematic/s1_pushin.mp4', ss=4.0)),
    dict(at=66, len=2, src='obs/14_zone', pick='mid', fx=['punch', 'clipaudio:0.4'],
         note='Đứng trong khu chiếm giữa map: HUD hiện CAPTURING, thanh tiến độ chạy'),
    dict(at=68, len=2, src='obs/15_barrier', pick='hit2', hit=0.5,   # tiếng thứ 2 (~10s) = lúc tường dựng lên fx=['flash@hit', 'clipaudio:0.4'],
         note='Thả EM Barrier Core: bức tường lục giác dựng lên trước mặt',
         alt=dict(src='obs/13_moves', pick='first', hit=0.5)),
    dict(at=70, len=2, src='obs/13_moves', pick='last', hit=0.5, fx=['flash@hit', 'shake@hit:16', 'clipaudio:0.4'],
         note='Lướt Q, rồi chuột phải KÉO ÁP SÁT đối thủ ở xa'),
    dict(at=72, len=2, src='obs/09_edge', pick='first', hit=0.75, fx=HIT + ['clipaudio:0.5'],
         note='(dùng lại clip hất khỏi mép - cú ĐẦU)'),   # clip bắn chỉ có 1 phát, đã dùng ở phách 26.5
    dict(at=74, len=2, src='obs/03_push', pick='first', hit=0.5, fx=['flash@hit', 'shake@hit:20', 'clipaudio:0.5'],
         note='(dùng lại clip đẩy - cú đẩy CUỐI)'),

    # --- CAO TRÀO (35.6 - 41.3s) ---
    dict(at=76, len=8, src='obs/17_best', pick='loudest', hit=3, fx=['zoom:1.0>1.08', 'flash@hit', 'shake@hit:34', 'rgb@hit', 'clipaudio:0.6'],
         note='PHA ĐẸP NHẤT: đánh tự do 1-2 phút, bộ dựng tự lấy tiếng to nhất',
         # Can xăng: bắn cây TNT ở giây 7,0, NỔ ở giây 8,0, sau đó chỉ còn 0,7s dùng được -> đặt tiếng nổ ở phách 82
         # (hit=6), bộ dựng tự chậm ~x0,8 thành slow-mo đúng lúc nổ. Đủ diễn biến: chọn can xăng -> cây hoá TNT -> bắn -> nổ
         alt=dict(src='obs/18_gasoline', src_t=8.0, hit=6)),
    dict(at=84, len=4, src='cinematic/s1_pushin.mp4', ss=2.4, fx=['zoom:1.0>1.06']),

    # --- 6. MÀN LOGO (41.3 - 48s) ---
    # 19,5 phách: nhạc ra cú kết ở phách 104 (= giây 7,385 của cảnh) rồi ngân -> hình tối dần đúng 1,6s cuối
    dict(at=88, len=19.5, src='cinematic/s7_bg.mp4', ss=0.5, fx=['blur:16', 'dark:0.38', 'flash@0', 'flash@7.385', 'fadeout:1.6']),
]

# ==================== CHỮ ====================
# sway=True: lớp đung đưa nhẹ (chỉ dùng cho cụm logo). ĐỪNG cho chữ phóng to/thu nhỏ chậm - ffmpeg làm tròn điểm ảnh
#            nên chữ giật từng nấc (lỗi "chữ rung rung" 25/10).
# style: 'title'  chữ mảnh giãn rộng, hiện mờ dần (mở màn)
#        'slam'   chữ đậm cỡ lớn ĐẬP vào màn hình (phóng 135% -> 100%), có tách màu đỏ/xanh
#        'lower'  chữ vừa ở 1/3 dưới, trượt vào từ trái, có vạch xanh lơ
#        'logo_*' / 'end_*'  các lớp màn logo (dựng sẵn trong build_trailer.py)
# accent: màu nhấn của chữ (mặc định trắng)
TEXTS = [
    dict(at=2.5, len=8,   style='title', text='THE SKY ARENA'),
    dict(at=12,  len=2,   style='slam',  text='NO GUNS.'),
    dict(at=14,  len=2,   style='slam',  text='NO BULLETS.'),
    dict(at=16,  len=4,   style='slam',  text='JUST MAGNETISM.', accent=(110, 220, 255)),
    dict(at=20,  len=8,   style='lower', text='THE WHOLE MAP IS AMMO'),
    dict(at=32,  len=3,   style='lower', text='NO HEALTH BAR'),
    dict(at=35,  len=5,   style='lower', text='EVERY HIT CHARGES YOU', accent=(255, 205, 90)),
    dict(at=40,  len=2,   style='slam',  text='MORE CHARGE.'),
    dict(at=42,  len=2,   style='slam',  text='FURTHER YOU FLY.', accent=(255, 110, 90)),
    dict(at=48,  len=4,   style='lower', text='ONE WAY TO LOSE'),
    dict(at=52,  len=4,   style='slam',  text='THE EDGE.', accent=(255, 110, 90)),
    dict(at=64,  len=8,   style='lower', text='1V1  ·  2V2  ONLINE'),
    # Các lớp màn logo kéo tới hết video (107,5) và mờ dần CÙNG lúc với hình nền (fadeout 1,6s).
    # 4 lớp màu đặt TRƯỚC logo (vẽ phía dưới): nền xanh đậm + quầng đỏ/xanh hai cực, vệt sáng ngang, đường sức từ
    # đỏ -> xanh có dấu + / -, vòng sóng xung kích. Thêm 25/10: bản đầu nền xám xanh nhạt, logo dễ bị bỏ qua.
    dict(at=88,  len=19.5, style='end_bg',        fadeout=1.6),
    dict(at=88,  len=2.0,  style='end_streak'),
    dict(at=88.5, len=19.0, style='end_fieldlines', fadeout=1.6, sway=True),
    dict(at=88,  len=1.4,  style='end_ring'),
    # sway=True: cụm logo + đường sức từ + khẩu hiệu đung đưa nhẹ CÙNG MỘT NHỊP (lên xuống 5px, 4 giây/nhịp, mượt dưới
    # 1 điểm ảnh - xem render_sway_movie). Chữ tên / "made with" đứng yên.
    dict(at=88,  len=19.5, style='logo_magneto', fadeout=1.6, sway=True),
    dict(at=89,  len=18.5, style='logo_dome',    fadeout=1.6, sway=True),
    dict(at=91,  len=16.5, style='end_tagline',  fadeout=1.6, sway=True),
    dict(at=93,  len=14.5, style='end_credits',  fadeout=1.6),
    dict(at=94,  len=13.5, style='end_madewith', fadeout=1.6),
]

# ==================== ÂM THANH ====================
# file: tên trong TrailerBuilder/edit/sfx/  |  'game:<tên>' = Assets/Resources/Audio/<tên>
#       'voice:<tên>' = giọng ARENA CONTROL (Assets/Resources/Audio/Voice/<tên>.mp3)
# at  : phách (có thể lẻ) | gain: âm lượng | duck: hạ nhạc khi giọng nói
#
# ÍT LÀ ĐẸP (sửa 25/10 theo góp ý "âm thanh hơi sucks, có tiếng giống TNT ghép vô"): bản trước rải 12 tiếng boom
# tổng hợp (nghe y như tiếng nổ TNT trong game) + 11 whoosh + 4 zap + 9 hit, chồng lên tiếng game thật và nhạc rock
# -> rối, giả. Giờ va chạm CHỈ dùng tiếng game thật trong clip (clipaudio - tự rơi đúng phách nhờ pick/hit) + nhạc.
# Tiếng tổng hợp chỉ còn 3 chỗ nhẹ. Muốn nhấn thêm thì nhấn ít, và tránh boom.wav (trùng màu với tiếng nổ TNT).
SOUNDS = [
    dict(at=8,     file='riser.wav',        gain=0.45),   # dâng nhẹ 8 phách vào drop
    dict(at=15.2,  file='whoosh_rev.wav',   gain=0.45),   # hút vào ngay trước drop
    dict(at=16.3,  file='voice:combat',     gain=1.0, duck=True),
    dict(at=76,    file='voice:match point', gain=1.0, duck=True),
    dict(at=88,    file='subdrop.wav',      gain=0.7),    # màn logo
    dict(at=88.6,  file='voice:victory',    gain=1.0, duck=True),
]
