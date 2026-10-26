# -*- coding: utf-8 -*-
"""
BỘ DỰNG TRAILER — đọc timeline.py rồi dựng ra file MP4 hoàn chỉnh bằng ffmpeg + numpy + Pillow.

Chạy:   py -3 TrailerBuilder/edit/build_trailer.py            (dựng đủ, ~3-6 phút)
        py -3 TrailerBuilder/edit/build_trailer.py --fast     (bản xem nhanh 960x540, ~1 phút)
        py -3 TrailerBuilder/edit/build_trailer.py --check    (chỉ dò khoảnh khắc trong từng clip OBS, ~20 giây:
                                                               in bảng + ảnh tổng _work/check.jpg, không dựng video)

Ra:     D:/Bt vtc/Capstone Project/Trailer_Footage/MagnetoDome_Trailer.mp4
        (thiếu cảnh OBS hoặc thiếu nhạc thật -> tên file có đuôi _DRAFT)

Các bước: 1) dò khoảnh khắc trong từng clip + tính đoạn cần cắt   2) dựng từng cảnh (cắt, hiệu ứng)
          3) nối cảnh   4) phủ chữ có chuyển động   5) trộn âm thanh bằng numpy   6) ghép hình + tiếng

CLIP QUAY TAY DÀI NGẮN TUỲ Ý (hàm plan_shot): không cần cắt clip chuẩn. Bộ dựng tìm KHOẢNH KHẮC trong clip
(tiếng va chạm to trong tiếng game, hoặc giữa clip) rồi đặt nó rơi đúng phách. Clip ngắn hơn ô thì tự làm
chậm (tối đa còn 1/2 tốc độ), vẫn thiếu thì giữ khung đầu/cuối và in cảnh báo.
Thư viện cần: numpy, pillow, imageio-ffmpeg   (py -3 -m pip install numpy pillow imageio-ffmpeg)
"""
import os, sys, glob, json, math, shutil, subprocess, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops
import imageio_ffmpeg

sys.stdout.reconfigure(encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.normpath(os.path.join(HERE, '..', '..'))
FOOTAGE = os.path.normpath(os.path.join(PROJECT, '..', 'Trailer_Footage'))
WORK = os.path.join(FOOTAGE, '_work')
FONTS = os.path.join(PROJECT, 'PosterBuilder', 'fonts')
AUDIO = os.path.join(PROJECT, 'Assets', 'Resources', 'Audio')
SFX = os.path.join(HERE, 'sfx')
FF = imageio_ffmpeg.get_ffmpeg_exe()

FAST = '--fast' in sys.argv
CHECK = '--check' in sys.argv
W, H = (960, 540) if FAST else (1920, 1080)
FPS = 30 if FAST else 60
S = W / 1920.0                      # hệ số phóng: mọi số đo trong file viết theo khung 1920
SR = 48000

spec = importlib.util.spec_from_file_location('timeline', os.path.join(HERE, 'timeline.py'))
TL = importlib.util.module_from_spec(spec); spec.loader.exec_module(TL)
# MUSIC_SPEED: tua bài nhạc nhanh/chậm (tối đa ±12,5%) để cú kết của bài khớp cuối video. Nhịp hiệu dụng
# đổi theo, nên mọi mốc phách (cảnh, chữ, tiếng) tự co giãn theo - không phải sửa gì khác.
MUSIC_SPEED = getattr(TL, 'MUSIC_SPEED', 1.0)
if abs(MUSIC_SPEED - 1) > 0.125 + 1e-9:
    raise SystemExit(f'MUSIC_SPEED = {MUSIC_SPEED}: chỉ được tua trong khoảng 0.875 - 1.125 (±12,5%)')
BEAT = 60.0 / (TL.BPM * MUSIC_SPEED)


def b2s(beat):
    return beat * BEAT


def run(args, quiet=True, cwd=None):
    r = subprocess.run([FF, '-y', '-hide_banner', '-loglevel', 'error'] + args, capture_output=True, text=True, cwd=cwd)
    if r.returncode != 0:
        print(r.stderr[-3000:])
        raise SystemExit('ffmpeg lỗi: ' + ' '.join(args[:6]))


def font(name, px):
    return ImageFont.truetype(os.path.join(FONTS, name + '.ttf'), max(8, int(px * S)))


# =====================================================================================
# 0) CHỮ -> ẢNH PNG (nền trong suốt, cỡ đúng khung hình)
# =====================================================================================

def text_layer(txt, fnt, fill, tracking=0, anchor_xy=None):
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    widths = [d.textlength(c, font=fnt) for c in txt]
    total = sum(widths) + tracking * S * (len(txt) - 1)
    x, y = anchor_xy if anchor_xy else (W / 2, H / 2)
    x -= total / 2
    for c, w in zip(txt, widths):
        d.text((x, y), c, font=fnt, fill=fill, anchor='lm')
        x += w + tracking * S
    return img


def glow(layer, color, radius, strength):
    a = layer.split()[3].filter(ImageFilter.GaussianBlur(radius * S)).point(lambda v: min(255, int(v * strength)))
    g = Image.new('RGBA', layer.size, color + (0,)); g.putalpha(a)
    return g


def shadow(layer, radius, alpha, dy=4):
    a = layer.split()[3].filter(ImageFilter.GaussianBlur(radius * S)).point(lambda v: int(v * alpha))
    sh = Image.new('RGBA', layer.size, (0, 0, 0, 0)); sh.putalpha(a)
    return ImageChops.offset(sh, 0, int(dy * S))


def compose(*layers):
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    for l in layers:
        out = Image.alpha_composite(out, l)
    return out


def chroma_split(layer, px):
    """Viền đỏ lệch trái + xanh lệch phải: cảm giác nhiễu điện từ."""
    a = layer.split()[3]
    red = Image.new('RGBA', layer.size, (255, 60, 60, 0)); red.putalpha(a.point(lambda v: int(v * 0.55)))
    cyan = Image.new('RGBA', layer.size, (60, 220, 255, 0)); cyan.putalpha(a.point(lambda v: int(v * 0.55)))
    return compose(ImageChops.offset(red, -int(px * S), 0), ImageChops.offset(cyan, int(px * S), 0))


# =====================================================================================
# MÀN LOGO CÓ MÀU (25/10) - nền trời mờ bản cũ ra một màu xám xanh, logo trắng chìm vào nền.
# Dùng đúng màu của game: cực + đỏ (255,76,56) bên trái, cực - xanh (70,160,255) bên phải.
# =====================================================================================
POLE_RED, POLE_BLUE = (255, 76, 56), (70, 160, 255)
LOGO_Y = 0.575          # đường giữa chữ DOME = trục nối hai cực


def _radial(cx, cy, rx, ry):
    """Mảng 0..1 dạng quầng mềm (Gauss) tâm (cx, cy), bán kính rx, ry - theo pixel."""
    y, x = np.mgrid[0:H, 0:W].astype(np.float32)
    return np.exp(-(((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2))


def end_background():
    """Lớp phủ nền: xanh đậm + quầng ĐỎ trái / XANH phải (hai cực) + tím ở giữa + lưới lục giác mờ + tối góc.
    Phủ kiểu alpha lên nền trời -> nền có màu, logo trắng nổi hẳn lên."""
    navy = np.array([7, 14, 38], np.float32)
    edge = 1 - _radial(W / 2, H * 0.48, W * 0.62, H * 0.62)              # 0 ở giữa -> 1 ở góc
    a_navy = 0.62 + 0.33 * edge
    red = _radial(W * 0.06, H * LOGO_Y, W * 0.42, H * 0.55)
    blue = _radial(W * 0.94, H * LOGO_Y, W * 0.42, H * 0.55)
    mid = _radial(W / 2, H * 0.47, W * 0.30, H * 0.30)
    rgb = navy[None, None, :] * np.ones((H, W, 1), np.float32)
    def mix(rgb, col, k):
        return rgb * (1 - k[..., None]) + np.array(col, np.float32)[None, None, :] * k[..., None]
    rgb = mix(rgb, POLE_RED, red * 0.78)
    rgb = mix(rgb, POLE_BLUE, blue * 0.78)
    rgb = mix(rgb, (120, 90, 235), mid * 0.35)
    alpha = np.clip(a_navy + (red + blue) * 0.1, 0, 0.96)
    img = Image.fromarray(np.dstack([np.clip(rgb, 0, 255), alpha * 255]).astype(np.uint8), 'RGBA')
    # Lưới lục giác (hoạ tiết rào chắn trong game), sáng hơn gần giữa
    hexl = Image.new('RGBA', (W, H), (0, 0, 0, 0)); hd = ImageDraw.Draw(hexl)
    r = 46 * S; dx = r * 1.732; dy = r * 1.5
    row = 0; yv = -r
    while yv < H + r:
        xv = -dx + (dx / 2 if row % 2 else 0)
        while xv < W + dx:
            pts = [(xv + r * math.cos(math.radians(60 * k + 30)), yv + r * math.sin(math.radians(60 * k + 30))) for k in range(7)]
            hd.line(pts, fill=(140, 220, 255, 255), width=max(1, int(1.5 * S)))
            xv += dx
        yv += dy; row += 1
    mask = (_radial(W / 2, H * 0.5, W * 0.45, H * 0.5) * 0.10 + 0.025) * 255
    hexl.putalpha(Image.fromarray(np.minimum(np.array(hexl.split()[3], np.float32), mask).astype(np.uint8)))
    return compose(img, hexl)


def _grad_color(u):
    """u 0..1: đỏ (cực +) -> tím -> xanh (cực -)."""
    return tuple(int(POLE_RED[i] + (POLE_BLUE[i] - POLE_RED[i]) * u) for i in range(3))


def end_fieldlines():
    """Đường sức từ: các vòng cung đổi màu đỏ -> xanh nối hai cực, nằm SAU logo. Kèm dấu + / - ở hai cực."""
    xl, xr, yc = W * 0.16, W * 0.84, H * LOGO_Y
    lines = Image.new('RGBA', (W, H), (0, 0, 0, 0)); ld = ImageDraw.Draw(lines)
    for amp, alpha in [(-0.30, 235), (-0.38, 185), (-0.47, 135), (0.17, 120), (0.25, 80)]:
        pts = []
        for i in range(181):
            u = i / 180
            pts.append((xl + (xr - xl) * u, yc + amp * H * math.sin(math.pi * u)))
        for i in range(180):
            c = _grad_color(i / 180)
            ld.line([pts[i], pts[i + 1]], fill=c + (alpha,), width=max(2, int(4 * S)))
    out = compose(glow(lines, (200, 170, 255), 14, 1.8), lines)
    # Hai cực: chấm sáng + dấu
    poles = Image.new('RGBA', (W, H), (0, 0, 0, 0)); pd = ImageDraw.Draw(poles)
    for x, col, sign in [(xl, POLE_RED, '+'), (xr, POLE_BLUE, '−')]:
        rr = 30 * S
        pd.ellipse([x - rr, yc - rr, x + rr, yc + rr], fill=col + (255,), outline=(255, 255, 255, 255), width=max(2, int(3 * S)))
        # Vẽ dấu bằng thanh chữ nhật, không dùng chữ: Poppins không có ký tự '−' (U+2212), '+' cỡ nhỏ thì méo
        bl, bw = 14 * S, 3.5 * S
        pd.rectangle([x - bl, yc - bw, x + bl, yc + bw], fill=(255, 255, 255, 255))
        if sign == '+': pd.rectangle([x - bw, yc - bl, x + bw, yc + bl], fill=(255, 255, 255, 255))
    return compose(out, glow(poles, (255, 255, 255), 22, 0.9), poles)


def end_ring():
    """Vòng sóng xung kích lúc logo đập vào (phóng to + mờ đi trong text_anim)."""
    l = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(l)
    cx, cy, r = W / 2, H * 0.47, H * 0.16
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=(235, 245, 255, 255), width=max(2, int(7 * S)))
    return compose(glow(l, (150, 210, 255), 16, 1.3), l)


def end_streak():
    """Vệt sáng ngang (kiểu ống kính anamorphic) loé qua sau chữ MAGNETO."""
    y, x = np.mgrid[0:H, 0:W].astype(np.float32)
    core = np.exp(-((y - H * 0.40) / (5 * S)) ** 2) * np.exp(-((x - W / 2) / (W * 0.36)) ** 2)
    halo = np.exp(-((y - H * 0.40) / (40 * S)) ** 2) * np.exp(-((x - W / 2) / (W * 0.28)) ** 2) * 0.45
    a = np.clip(core + halo, 0, 1)
    col = np.dstack([170 + 85 * core, 225 + 30 * core, np.full_like(core, 255)])
    return Image.fromarray(np.dstack([np.clip(col, 0, 255), a * 255]).astype(np.uint8), 'RGBA')


def render_text(t, path):
    style = t['style']
    acc = tuple(t.get('accent', (255, 255, 255)))
    cyan = (110, 220, 255)
    if style == 'title':
        l = text_layer(t['text'], font('Poppins-SemiBold', 58), (235, 245, 255, 255), tracking=22, anchor_xy=(W / 2, H * 0.80))
        img = compose(shadow(l, 10, 0.7), glow(l, cyan, 18, 0.9), l)
    elif style == 'slam':
        l = text_layer(t['text'], font('Poppins-Black', 150), acc + (255,), tracking=4)
        img = compose(shadow(l, 18, 0.85, 8), glow(l, acc if acc != (255, 255, 255) else cyan, 30, 0.75), chroma_split(l, 7), l)
    elif style == 'lower':
        y = H * 0.83
        l = text_layer(t['text'], font('Poppins-Bold', 64), acc + (255,), tracking=8, anchor_xy=(W / 2, y))
        box = l.split()[3].getbbox()
        bar = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        bd = ImageDraw.Draw(bar)
        if box:
            bd.rectangle([box[0] - 28 * S, y - 34 * S, box[0] - 22 * S, y + 34 * S], fill=cyan + (255,))
            plate = Image.new('RGBA', (W, H), (0, 0, 0, 0))
            ImageDraw.Draw(plate).rectangle([box[0] - 40 * S, y - 52 * S, box[2] + 40 * S, y + 52 * S], fill=(4, 10, 22, 150))
            plate = plate.filter(ImageFilter.GaussianBlur(14 * S))
            bar = compose(plate, bar)
        img = compose(bar, shadow(l, 8, 0.8), glow(l, cyan, 14, 0.45), l)
    elif style == 'logo_magneto':
        l = text_layer('MAGNETO', font('Poppins-Black', 210), (255, 255, 255, 255), tracking=6, anchor_xy=(W / 2, H * 0.40))
        # Quầng ĐỎ lệch trái + XANH lệch phải (hai cực) thay cho quầng trắng xanh nhạt của bản đầu
        red = ImageChops.offset(glow(l, POLE_RED, 34, 0.95), -int(14 * S), 0)
        blue = ImageChops.offset(glow(l, POLE_BLUE, 34, 0.95), int(14 * S), 0)
        img = compose(shadow(l, 22, 0.9, 10), red, blue, glow(l, (255, 255, 255), 10, 0.5), chroma_split(l, 6), l)
    elif style == 'logo_dome':
        l = text_layer('DOME', font('Poppins-Black', 150), (255, 255, 255, 255), tracking=40, anchor_xy=(W / 2, H * 0.575))
        a = l.split()[3]; bx = a.getbbox()
        grad = Image.new('RGBA', (W, H)); gd = ImageDraw.Draw(grad)
        for x in range(bx[0], bx[2]):           # đỏ (cực +) -> xanh (cực -)
            k = (x - bx[0]) / max(1, bx[2] - bx[0])
            gd.line([(x, bx[1]), (x, bx[3])], fill=(int(255 + (70 - 255) * k), int(76 + (160 - 76) * k), int(56 + (255 - 56) * k), 255))
        grad.putalpha(a)
        lines = Image.new('RGBA', (W, H), (0, 0, 0, 0)); ld = ImageDraw.Draw(lines)
        yy = (bx[1] + bx[3]) / 2
        ld.rectangle([W * 0.16, yy - 4 * S, bx[0] - 40 * S, yy + 4 * S], fill=(255, 76, 56, 255))
        ld.rectangle([bx[2] + 40 * S, yy - 4 * S, W * 0.84, yy + 4 * S], fill=(70, 160, 255, 255))
        img = compose(shadow(grad, 18, 0.9), glow(grad, (255, 255, 255), 26, 0.3), glow(lines, (255, 255, 255), 8, 0.6), lines, grad)
    elif style == 'end_bg': img = end_background()
    elif style == 'end_fieldlines': img = end_fieldlines()
    elif style == 'end_ring': img = end_ring()
    elif style == 'end_streak': img = end_streak()
    elif style == 'end_tagline':
        l = text_layer('NO GUNS. JUST MAGNETISM.', font('Poppins-Bold', 52), (255, 255, 255, 255), tracking=6, anchor_xy=(W / 2, H * 0.70))
        l2 = text_layer('1V1  ·  2V2  ONLINE', font('Poppins-SemiBold', 30), cyan + (255,), tracking=12, anchor_xy=(W / 2, H * 0.765))
        img = compose(shadow(l, 10, 0.8), l, shadow(l2, 8, 0.8), l2)
    elif style == 'end_credits':
        l = text_layer('Nguyễn Hoàng Gia Khiêm  ·  K24M-GAME1  ·  GVHD: cô Phạm Thị Hằng',
                       ImageFont.truetype(os.path.join(FONTS, 'BeVietnamPro-Medium.ttf'), int(28 * S)), (215, 225, 240, 255), anchor_xy=(W / 2, H * 0.88))
        img = compose(shadow(l, 6, 0.8), l)
    elif style == 'end_madewith':
        l = text_layer('MADE WITH UNITY 6  ·  PHOTON FUSION 2', font('Poppins-Medium', 22), (150, 175, 210, 255), tracking=8, anchor_xy=(W / 2, H * 0.925))
        img = compose(shadow(l, 6, 0.8), l)
    else:
        raise SystemExit('style chữ lạ: ' + style)
    img.save(path)


# =====================================================================================
# 1) TÌM CLIP + DÒ KHOẢNH KHẮC + TÍNH ĐOẠN CẦN CẮT
# =====================================================================================

MIN_SPEED = 0.5      # clip ngắn hơn ô: được làm chậm tối đa tới mức này (so với tốc độ yêu cầu)


def clip_key(src):
    """'obs/04_fire' / 'obs/04_fire.mp4' -> '04_fire' (khoá tra bảng CLIPS trong timeline.py)."""
    return os.path.splitext(os.path.basename(src))[0]


def find_src(src):
    """'obs/04_fire' hoặc 'obs/04_fire.mp4' -> đường dẫn thật. Nhận mọi đuôi OBS hay ra (mp4/mkv/mov).
    Có trong bảng CLIPS (timeline.py) thì lấy đúng file ghi ở đó - file OBS giữ nguyên tên gốc, không phải đổi."""
    entry = getattr(TL, 'CLIPS', {}).get(clip_key(src))
    if entry:
        p = os.path.join(FOOTAGE, 'obs', entry[0])
        return p if os.path.isfile(p) else None
    p = os.path.join(FOOTAGE, src)
    if os.path.isfile(p): return p
    base = os.path.splitext(p)[0] if os.path.splitext(p)[1].lower() in ('.mp4', '.mkv', '.mov') else p
    for ext in ('.mp4', '.mkv', '.mov', '.webm', '.flv'):
        if os.path.isfile(base + ext): return base + ext
    return None


_info_cache = {}


def clip_info(path):
    """(thời lượng giây, có tiếng không) - đọc từ dòng 'Duration' ffmpeg in ra."""
    if path in _info_cache: return _info_cache[path]
    r = subprocess.run([FF, '-hide_banner', '-i', path], capture_output=True, text=True, errors='replace')
    dur, audio = 0.0, ' Audio:' in r.stderr
    for line in r.stderr.splitlines():
        if 'Duration:' in line:
            hh, mm, ss = line.split('Duration:')[1].split(',')[0].strip().split(':')
            dur = int(hh) * 3600 + int(mm) * 60 + float(ss)
    _info_cache[path] = (dur, audio)
    return dur, audio


_onset_cache = {}


def find_hits(path):
    """Danh sách khoảnh khắc va chạm trong tiếng game của clip: [(giây, độ to), ...] theo thứ tự thời gian.
    Va chạm = tiếng TĂNG VỌT >= 9 dB trong 80ms và đủ to (không quá 22 dB dưới tiếng to nhất clip).
    Giây trả về là lúc tiếng BẮT ĐẦU vọt lên (khung va chạm), không phải đỉnh tiếng.
    Hai va chạm cách nhau < 0,3s coi là một (giữ cái to hơn)."""
    if path in _onset_cache: return _onset_cache[path]
    sr, hop = 22050, 220                                   # khung 10ms
    raw = subprocess.run([FF, '-hide_banner', '-loglevel', 'error', '-i', path, '-vn', '-f', 'f32le', '-ac', '1',
                          '-ar', str(sr), '-'], capture_output=True).stdout
    x = np.frombuffer(raw, '<f4').astype(np.float64) if raw else np.zeros(0)
    hits = []
    if len(x) > sr * 0.3:
        n = len(x) // hop
        e = np.sqrt((x[:n * hop].reshape(n, hop) ** 2).mean(1)) + 1e-6
        db = 20 * np.log10(e)
        top = db.max()
        cand = []
        for i in range(8, n - 5):
            peak = db[i:i + 5].max()
            if peak - db[i - 8:i].mean() >= 9 and peak >= top - 22 and db[i] > db[i - 1] + 2:
                cand.append((i * hop / sr, float(e[i:i + 10].max())))
        for t, a in cand:                                  # gộp các va chạm sát nhau
            if hits and t - hits[-1][0] < 0.3:
                if a > hits[-1][1]: hits[-1] = (hits[-1][0], a)
            else:
                hits.append((t, a))
    _onset_cache[path] = hits
    return hits


def clip_window(src, path):
    """(giây đầu, giây cuối) phần DÙNG ĐƯỢC của clip. Bảng CLIPS cho phép cắt bỏ đầu/đuôi hỏng (vd bảng
    Settings hiện lên lúc bấm Esc dừng quay). Mọi khoảnh khắc / đoạn cắt đều nằm gọn trong khoảng này."""
    L, _ = clip_info(path)
    entry = getattr(TL, 'CLIPS', {}).get(clip_key(src))
    if entry and len(entry) >= 3:
        return max(0.0, entry[1]), min(L, entry[2])
    return 0.0, L


def pick_moment(path, pick, L, t0=0.0):
    """Giây của khoảnh khắc chính theo kiểu `pick`, tính TRONG cửa sổ dùng được [t0, t0+L]
    (giây trả về đã trừ t0). Trả (giây, ghi chú)."""
    if pick == 'mid': return L / 2, 'giữa clip'
    hits = [(t - t0, a) for t, a in find_hits(path) if t0 <= t <= t0 + L]
    if not hits: return L / 2, '⚠ không dò được va chạm (clip không có tiếng?) -> lấy giữa clip'
    lst = ', '.join(f'{h[0]:.2f}' for h in hits[:8]) + (' …' if len(hits) > 8 else '')
    if pick == 'first': t = hits[0][0]
    elif pick == 'last': t = hits[-1][0]
    elif pick == 'loudest': t = max(hits, key=lambda h: h[1])[0]
    elif pick.startswith('hit'):
        k = int(pick[3:]) - 1
        if k >= len(hits): return hits[-1][0], f'⚠ chỉ có {len(hits)} va chạm ({lst}), không có {pick} -> lấy cái cuối'
        t = hits[k][0]
    else: raise SystemExit('pick lạ: ' + pick)
    return t, f'{pick} = {t:.2f}s  (va chạm dò được: {lst})'


def seg_frames(shot):
    """(khung đầu, số khung) của cảnh, tính từ mốc TUYỆT ĐỐI (làm tròn đầu + cuối) - không cộng dồn sai số.
    Một phách ở 128 BPM = 28,125 khung (60fps): làm tròn từng cảnh riêng thì 26 cảnh trôi lệch nhịp ~0,3s."""
    f0 = round(b2s(shot['at']) * FPS)
    f1 = round(b2s(shot['at'] + shot['len']) * FPS)
    return f0, f1 - f0


def plan_shot(shot):
    """Tính đoạn cần cắt từ clip nguồn. Trả dict: path, start, speed, pad (giây giữ khung đầu), hit (giây
    trong cảnh), moment (giây trong clip), D (độ dài cảnh), notes. path=None nếu thiếu file (-> thẻ giữ chỗ).

      ss=...      -> lấy đúng từ giây đó (cảnh render sẵn, biết chính xác)
      src_t=...   -> khoảnh khắc chính nằm ở giây này của clip (người/AI xem clip rồi ghi tay - thắng mọi pick)
      pick=...    -> tự dò khoảnh khắc: 'first' 'last' 'loudest' 'hit2' (va chạm thứ 2)... hoặc 'mid' (mặc định)
      hit=x       -> khoảnh khắc đó rơi vào phách thứ x tính từ đầu cảnh (mặc định 0; 'mid' thì giữa cảnh)
      nudge=s     -> dời khoảnh khắc thêm s giây (âm = sớm hơn), khi tiếng va chạm lệch hình một chút
      speed=x     -> tốc độ phát (0.5 = chậm một nửa)
      alt=dict()  -> cảnh TUỲ CHỌN: thiếu file thì dùng cảnh thay thế này (src + cách lấy riêng của nó)"""
    f0, nf = seg_frames(shot)
    D = nf / FPS
    path = find_src(shot['src'])
    if path is None and shot.get('alt'):
        sub = {k: v for k, v in shot.items() if k in ('at', 'len', 'fx', 'note')}
        sub.update(shot['alt'])
        p = plan_shot(sub)
        p['notes'].insert(0, f"(chưa có {shot['src']} -> tạm dùng {shot['alt']['src']})")
        p['src'] = sub['src']
        return p
    if path is None:
        return dict(path=None, src=shot['src'], start=0, speed=1, pad=0, hit=0, moment=0, D=D, L=0,
                    notes=['THIẾU FILE -> thẻ giữ chỗ'])
    # Làm việc trong "toạ độ cửa sổ": 0 = đầu phần dùng được, L = độ dài phần dùng được
    t0, t1 = clip_window(shot['src'], path)
    L = t1 - t0
    sp0 = shot.get('speed', 1.0)
    notes = []
    pick = shot.get('pick', 'mid')
    if 'ss' in shot:
        m, h = shot['ss'] - t0, 0.0
        notes.append(f"ss={shot['ss']}")
    else:
        if 'src_t' in shot: m, why = shot['src_t'] - t0, f"src_t={shot['src_t']} (ghi tay)"
        else: m, why = pick_moment(path, pick, L, t0)
        notes.append(why)
        m += shot.get('nudge', 0.0)
        fell_back = why.startswith('⚠ không dò')
        if (pick == 'mid' and 'hit' not in shot and 'src_t' not in shot) or fell_back:
            h = D / 2
        else:
            h = max(0.0, b2s(shot['at'] + shot.get('hit', 0)) - f0 / FPS)   # chính xác theo khung đã làm tròn
    # Tốc độ lớn nhất mà đoạn [m - h*sp, m + (D-h)*sp] còn nằm gọn trong clip
    sp = sp0
    if h > 1e-6: sp = min(sp, m / h)
    if D - h > 1e-6: sp = min(sp, (L - m) / (D - h))
    pad = 0.0
    if sp >= sp0 * MIN_SPEED:
        if sp < sp0 * 0.98: notes.append(f'clip ngắn/khoảnh khắc sát mép -> làm chậm còn x{sp:.2f}')
    else:
        sp = sp0 * MIN_SPEED
        notes.append(f'⚠ clip quá ngắn quanh khoảnh khắc (clip dài {L:.1f}s): chậm x{sp:.2f} vẫn thiếu '
                     f'-> giữ khung đầu/cuối. Nên quay dư ra trước/sau hành động!')
    start = m - h * sp
    if start < 0: pad, start = -start / sp, 0.0
    if t0 > 0 or t1 < clip_info(path)[0] - 0.01: notes.append(f'chỉ dùng {t0:.2f}s-{t1:.2f}s của clip (bảng CLIPS)')
    # Trả về toạ độ THẬT của file
    return dict(path=path, src=shot['src'], start=start + t0, speed=sp, pad=pad, hit=h, moment=m + t0, D=D, L=L,
                end=t1, notes=notes)


def placeholder_card(shot, path):
    """Thẻ giữ chỗ cho cảnh OBS chưa quay: nền tối có lưới + chữ ghi rõ cần quay gì."""
    img = Image.new('RGB', (W, H), (10, 16, 30))
    d = ImageDraw.Draw(img)
    for x in range(0, W, int(64 * S)): d.line([(x, 0), (x, H)], fill=(18, 28, 48))
    for y in range(0, H, int(64 * S)): d.line([(0, y), (W, y)], fill=(18, 28, 48))
    d.rectangle([W * 0.06, H * 0.26, W * 0.94, H * 0.74], outline=(255, 190, 70), width=max(2, int(4 * S)))
    d.text((W / 2, H * 0.35), 'CẢNH CẦN QUAY BẰNG OBS', font=ImageFont.truetype(os.path.join(FONTS, 'BeVietnamPro-Bold.ttf'), int(46 * S)), fill=(255, 190, 70), anchor='mm')
    d.text((W / 2, H * 0.46), shot['src'], font=font('Poppins-Bold', 40), fill=(255, 255, 255), anchor='mm')
    d.text((W / 2, H * 0.58), shot.get('note', ''), font=ImageFont.truetype(os.path.join(FONTS, 'BeVietnamPro-Medium.ttf'), int(26 * S)), fill=(200, 215, 235), anchor='mm')
    if shot.get('pick', 'mid') != 'mid' and 'ss' not in shot:
        d.text((W / 2, H * 0.67), f"tự dò: {shot['pick']} -> rơi vào phách {shot['at'] + shot.get('hit', 0):g}",
               font=ImageFont.truetype(os.path.join(FONTS, 'BeVietnamPro-Medium.ttf'), int(22 * S)), fill=(120, 150, 190), anchor='mm')
    img.save(path)


# =====================================================================================
# 2) DỰNG TỪNG CẢNH
# =====================================================================================

def parse_fx(fx, hit=0.0):
    """'@hit' trong hiệu ứng = đúng khung khoảnh khắc chính của cảnh (vd 'flash@hit', 'shake@hit:30')."""
    o = dict(flash=[], shake=[], rgb=[], zoom=None, punch=False, fadein=0, fadeout=0, blur=0, dark=0, clipaudio=0)
    def at(v): return round(hit, 4) if v == 'hit' else float(v)
    for f in fx:
        name, _, arg = f.partition(':')
        if name.startswith('flash@'): o['flash'].append(at(name[6:]))
        elif name.startswith('shake@'): o['shake'].append((at(name[6:]), float(arg or 20)))
        elif name.startswith('rgb@'): o['rgb'].append(at(name[4:]))
        elif name == 'zoom': a, b = arg.split('>'); o['zoom'] = (float(a), float(b))
        elif name == 'punch': o['punch'] = True
        elif name in ('fadein', 'fadeout', 'blur', 'dark', 'clipaudio'): o[name] = float(arg)
    return o


def segment_filters(fx, dur):
    """Chuỗi bộ lọc ffmpeg cho một cảnh. t = giây tính từ đầu cảnh."""
    z0, z1 = fx['zoom'] or (1.0, 1.0)
    margin = 1.06 if fx['shake'] else 1.0
    Z = f"(max({margin},{z0}+({z1}-{z0})*min(1,t/{dur:.4f}))"
    if fx['punch']: Z += "+0.12*pow(max(0,1-t/0.22),2)"
    Z += ")"
    sx = '+'.join(f"{a*S:.1f}*sin(t*71)*exp(-(t-{t0})*7)*gte(t,{t0})" for t0, a in fx['shake']) or '0'
    sy = '+'.join(f"{a*S*0.7:.1f}*sin(t*53+1.3)*exp(-(t-{t0})*7)*gte(t,{t0})" for t0, a in fx['shake']) or '0'
    f = [f"scale=w='trunc({W}*{Z}/2)*2':h='trunc({H}*{Z}/2)*2':eval=frame",
         f"crop={W}:{H}:x='(iw-{W})/2+({sx})':y='(ih-{H})/2+({sy})'"]
    for t0 in fx['rgb']:
        f.append(f"rgbashift=rh={-int(12*S)}:bh={int(12*S)}:gv={int(4*S)}:enable='between(t,{t0},{t0+0.16})'")
    br = '+'.join(f"0.55*exp(-(t-{t0})*13)*gte(t,{t0})" for t0 in fx['flash'])
    if fx['dark']: br = (br + '+' if br else '') + f"(-{fx['dark']})"
    if fx['blur']: f.append(f"gblur=sigma={fx['blur']*S:.1f}")
    f.append(f"eq=contrast=1.07:saturation=1.14" + (f":brightness='{br}':eval=frame" if br else ''))
    f.append("vignette=angle=PI/4.6")
    if fx['fadein']: f.append(f"fade=in:st=0:d={fx['fadein']}")
    if fx['fadeout']: f.append(f"fade=out:st={dur - fx['fadeout']:.3f}:d={fx['fadeout']}")
    return ','.join(f)


def render_segment(i, shot, plan):
    nframes = seg_frames(shot)[1]
    dur = nframes / FPS
    out = os.path.join(WORK, f'seg_{i:02d}.mp4')
    fx = parse_fx(shot.get('fx', []), plan['hit'])
    vf = segment_filters(fx, dur)
    if plan['path']:
        sp, pad = plan['speed'], plan['pad']
        # -t trước -i: nguồn dừng ĐÚNG ở cuối phần dùng được -> thiếu hình thì tpad giữ khung cuối,
        # không bao giờ lộ phần bị cắt bỏ (bảng Settings...)
        inp = ['-ss', f"{plan['start']:.3f}", '-t', f"{max(0.05, plan['end'] - plan['start']):.3f}", '-i', plan['path']]
        # tpad ĐẦU chuỗi: giữ khung đầu (pad) / khung cuối khi clip thiếu, rồi mới đổi tốc độ.
        # ⚠ tpad phải đứng TRƯỚC setpts=PTS-STARTPTS - đặt sau thì tpad không ăn (đã thử: thiếu khung, cảnh sau trôi nhịp)
        pre = (f"tpad=start_mode=clone:start_duration={pad * sp:.4f}:stop_mode=clone:stop_duration=4,"
               f"setpts=(PTS-STARTPTS)/{sp:.5f},fps={FPS},"
               f"scale={W}:{H}:force_original_aspect_ratio=increase,crop={W}:{H},setpts=PTS-STARTPTS,")
    else:
        card = os.path.join(WORK, f'card_{i:02d}.png')
        placeholder_card(shot, card)
        inp = ['-loop', '1', '-framerate', str(FPS), '-i', card]
        pre = f"fps={FPS},"
    run(inp + ['-t', f'{dur + 0.5:.4f}', '-vf', pre + vf + ",setsar=1,format=yuv420p",
               '-frames:v', str(nframes), '-an', '-r', str(FPS), '-c:v', 'libx264', '-preset', 'veryfast' if FAST else 'medium',
               '-crf', '22' if FAST else '15', out])
    return out


def atempo_chain(sp):
    """atempo chỉ nhận 0.5-2 mỗi nấc -> nối nhiều nấc cho tốc độ ngoài khoảng đó. Giữ nguyên cao độ tiếng."""
    parts = []
    while sp < 0.5: parts.append('atempo=0.5'); sp /= 0.5
    while sp > 2.0: parts.append('atempo=2.0'); sp /= 2.0
    parts.append(f'atempo={sp:.5f}')
    return ','.join(parts)


def clip_audio(plan):
    """Tiếng gốc của đoạn clip đã cắt, cùng tốc độ với hình."""
    args = [FF, '-hide_banner', '-loglevel', 'error', '-ss', f"{plan['start']:.3f}", '-i', plan['path'],
            '-t', f"{min((plan['D'] - plan['pad']) * plan['speed'], plan['end'] - plan['start']):.3f}", '-vn']
    if abs(plan['speed'] - 1) > 1e-3: args += ['-af', atempo_chain(plan['speed'])]
    args += ['-f', 'f32le', '-ac', '2', '-ar', str(SR), '-']
    raw = subprocess.run(args, capture_output=True).stdout
    return np.frombuffer(raw, dtype='<f4').reshape(-1, 2).astype(np.float64) if raw else np.zeros((0, 2))


def report(plans):
    """In bảng: mỗi cảnh lấy đoạn nào của clip nào, kèm cảnh báo. Ghi ra _work/report.txt.
    Thêm ảnh tổng _work/check.jpg: khung hình ĐÚNG KHOẢNH KHẮC chính của từng cảnh, để soát bằng mắt."""
    lines, thumbs = [], []
    used = {}
    for i, (shot, p) in enumerate(zip(TL.SHOTS, plans)):
        head = f"{i+1:2d}. phách {shot['at']:>5g}-{shot['at']+shot['len']:<5g} {p['src']:<26}"
        if p['path'] is None:
            lines.append(head + ' THIẾU FILE (thẻ giữ chỗ) · ' + shot.get('note', '')); continue
        a, b = p['start'], p['start'] + (p['D'] - p['pad']) * p['speed']
        lines.append(head + f" lấy {a:6.2f}s-{b:6.2f}s / {p['L']:.1f}s"
                     + (f" · x{p['speed']:.2f}" if abs(p['speed'] - 1) > 1e-3 else '')
                     + (f" · giữ khung đầu {p['pad']:.2f}s" if p['pad'] > 0 else ''))
        for n in p['notes']: lines.append('       ' + n)
        for j, (a2, b2) in used.get(p['path'], []):            # cảnh trùng đoạn với cảnh trước
            ov = min(b, b2) - max(a, a2)
            if ov > 0.5 * min(b - a, b2 - a2):
                lines.append(f'       ⚠ TRÙNG HÌNH với cảnh {j+1} (cùng đoạn clip) -> quay thêm hành động trong clip này, '
                             f'hoặc đổi pick')
        used.setdefault(p['path'], []).append((i, (a, b)))
        thumbs.append((i, shot, p))
    txt = '\n'.join(lines)
    print(txt)
    with open(os.path.join(WORK, 'report.txt'), 'w', encoding='utf-8') as f: f.write(txt + '\n')
    if not thumbs: return
    tw, th, cols = 384, 216, 5
    rows = (len(thumbs) + cols - 1) // cols
    sheet = Image.new('RGB', (tw * cols, (th + 30) * rows), (12, 16, 26))
    d = ImageDraw.Draw(sheet)
    f = ImageFont.truetype(os.path.join(FONTS, 'BeVietnamPro-Medium.ttf'), 17)
    for k, (i, shot, p) in enumerate(thumbs):
        jpg = os.path.join(WORK, f'moment_{i:02d}.jpg')
        subprocess.run([FF, '-y', '-hide_banner', '-loglevel', 'error', '-ss', f"{max(0, p['moment']):.3f}", '-i', p['path'],
                        '-frames:v', '1', '-vf', f'scale={tw}:{th}:force_original_aspect_ratio=increase,crop={tw}:{th}', jpg])
        x, y = (k % cols) * tw, (k // cols) * (th + 30)
        if os.path.exists(jpg): sheet.paste(Image.open(jpg), (x, y + 30))
        d.text((x + 6, y + 6), f"{i+1}. b{shot['at'] + shot.get('hit', 0):g} {os.path.basename(p['src'])} @{p['moment']:.2f}s",
               font=f, fill=(255, 220, 120))
    sheet.save(os.path.join(WORK, 'check.jpg'), quality=88)
    print('  ảnh soát khoảnh khắc:', os.path.join(WORK, 'check.jpg'))


# =====================================================================================
# 4) PHỦ CHỮ CÓ CHUYỂN ĐỘNG
# =====================================================================================

def text_anim(style, d):
    """(biểu thức scale, biểu thức x, y, fade-in, fade-out) cho overlay. t = giây từ lúc chữ hiện.

    ⚠ KHÔNG cho chữ phóng to / thu nhỏ CHẬM liên tục (sửa 25/10 - chủ project: "nhiều chữ rung rung, nhìn giật").
    overlay + scale của ffmpeg chỉ làm việc theo ĐIỂM ẢNH NGUYÊN: phóng chậm thì mỗi vài khung cỡ ảnh nhảy 2px rồi căn
    giữa lại -> viền chữ giật từng nấc thay vì trôi. Chỉ dùng chuyển động NHANH lúc xuất hiện (đập 0,13-0,18s, trượt,
    hiện dần), sau đó chữ ĐỨNG YÊN. Muốn chữ đung đưa thì dùng ô sway=True (render_sway_movie: dịch dưới 1 điểm ảnh)."""
    if style == 'slam':
        return "1+0.35*pow(max(0,1-t/0.13),2)", "(W-w)/2", "(H-h)/2", 0.06, 0.14
    if style == 'lower':
        return "1", f"(W-w)/2-{80*S:.0f}*pow(max(0,1-t/0.25),3)", "(H-h)/2", 0.18, 0.2
    if style == 'title':
        return "1", "(W-w)/2", f"(H-h)/2+{24*S:.0f}*pow(max(0,1-t/1.2),2)", 0.9, 0.5
    if style == 'logo_magneto':
        return "1+0.5*pow(max(0,1-t/0.18),2)", "(W-w)/2", "(H-h)/2", 0.05, 0.6
    if style == 'end_bg':          # nền màu: hiện nhanh cùng cú loé, rồi đứng yên
        return "1", "(W-w)/2", "(H-h)/2", 0.12, 0.6
    if style == 'end_fieldlines':  # đường sức từ: hiện dần (đung đưa cùng cụm logo qua sway=True)
        return "1", "(W-w)/2", "(H-h)/2", 0.7, 0.6
    if style == 'end_ring':        # sóng xung kích: 0.5 -> 2.6 lần trong 0.55s rồi tắt
        return "0.5+2.1*min(1,t/0.55)", "(W-w)/2", "(H-h)/2", 0.02, 0.45
    if style == 'end_streak':      # vệt sáng ngang: loé lên rồi tắt dần
        return "1", "(W-w)/2", "(H-h)/2", 0.04, 0.7
    if style == 'logo_dome':
        return "1+0.3*pow(max(0,1-t/0.16),2)", "(W-w)/2", "(H-h)/2", 0.05, 0.6
    return "1", "(W-w)/2", f"(H-h)/2+{14*S:.0f}*pow(max(0,1-t/0.4),2)", 0.35, 0.6


SWAY_AMP = 5.0        # điểm ảnh (khung 1920) - biên độ đung đưa lên xuống của cụm logo
SWAY_PERIOD = 4.0     # giây cho một nhịp đung đưa
SWAY_RAMP = 1.0       # giây để biên độ tăng dần từ 0 (không giật lúc bắt đầu)


def render_sway_movie(png, out, at, d, t0):
    """Lớp chữ ĐUNG ĐƯA NHẸ, mượt dưới 1 điểm ảnh: dịch ảnh bằng Pillow (nội suy bicubic) từng khung rồi ghi thành
    video có kênh trong suốt (.mov, mã png). ffmpeg overlay chỉ dịch được theo điểm ảnh nguyên -> chuyển động chậm
    sẽ giật từng nấc, nên phải tự làm ở đây.
    t0 = mốc chung (giây trên trailer) để MỌI lớp sway cùng một nhịp - cụm logo, đường sức từ, hai cực đi cùng nhau,
    các đường nối không lệch khỏi chữ."""
    img = Image.open(png).convert('RGBA')
    n = int(round(d * FPS)) + 1
    p = subprocess.Popen([FF, '-y', '-hide_banner', '-loglevel', 'error', '-f', 'rawvideo', '-pix_fmt', 'rgba',
                          '-s', f'{W}x{H}', '-r', str(FPS), '-i', '-', '-c:v', 'png', '-f', 'mov', out],
                         stdin=subprocess.PIPE)
    for i in range(n):
        ta = at + i / FPS - t0                                   # giây kể từ mốc chung
        ramp = min(1.0, max(0.0, ta / SWAY_RAMP))
        dy = SWAY_AMP * S * ramp * math.sin(2 * math.pi * ta / SWAY_PERIOD)
        fr = img.transform(img.size, Image.AFFINE, (1, 0, 0, 0, 1, -dy), resample=Image.BICUBIC)
        p.stdin.write(fr.tobytes())
    p.stdin.close(); p.wait()


def overlay_texts(body, out):
    # Ảnh chữ được nạp bằng bộ lọc `movie` NGAY TRONG chuỗi lọc, KHÔNG dùng mỗi ảnh một `-i`.
    # ⚠ Bẫy (25/10): 17 ảnh = 17 luồng đầu vào; ffmpeg 7 đọc các luồng song song, luồng ảnh lặp sinh khung
    # liên tục trong khi chuỗi overlay chỉ cần chúng ở giây 40+ -> hàng đợi đầy, các luồng chờ nhau, KẸT CHÉO
    # (bộ nhớ phình 1,1 GB, CPU đứng im, 11 phút không ra khung nào). `movie` chỉ đọc khi bộ lọc cần tới.
    # Chạy ffmpeg với thư mục làm việc = WORK để tên ảnh không phải thoát ký tự (đường dẫn có dấu cách, dấu ':').
    inputs, chains = ['-i', body], []
    last = '[0:v]'
    for k, t in enumerate(TL.TEXTS):
        png = os.path.join(WORK, f'text_{k:02d}.png')
        render_text(t, png)
        at, d = b2s(t['at']), b2s(t['len'])
        sc, x, y, fi, fo = text_anim(t['style'], d)
        fo = t.get('fadeout', fo)          # chữ màn logo mờ cùng nhịp với hình nền
        if t.get('sway'):                  # lớp đung đưa: đã dựng sẵn thành video trong suốt
            sway_t0 = min(b2s(x['at']) for x in TL.TEXTS if x.get('sway'))
            render_sway_movie(png, os.path.join(WORK, f'text_{k:02d}.mov'), at, d, sway_t0)
            src = f"movie=text_{k:02d}.mov,setpts=PTS-STARTPTS,trim=duration={d:.4f},"
        else:
            src = f"movie=text_{k:02d}.png,loop=loop=-1:size=1:start=0,setpts=N/({FPS}*TB),trim=duration={d:.4f},"
        chains.append(src + f"format=rgba,scale=w='trunc(iw*({sc})/2)*2':h='trunc(ih*({sc})/2)*2':eval=frame,"
                      f"fade=in:st=0:d={fi}:alpha=1,fade=out:st={max(0, d-fo):.3f}:d={fo}:alpha=1,setpts=PTS+{at:.4f}/TB[t{k}]")
        chains.append(f"{last}[t{k}]overlay=x='{x}':y='{y}':eval=frame:enable='between(t,{at:.4f},{at+d:.4f})'[v{k}]")
        last = f'[v{k}]'
    run(inputs + ['-filter_complex', ';'.join(chains), '-map', last, '-r', str(FPS), '-c:v', 'libx264',
                  '-preset', 'veryfast' if FAST else 'slow', '-crf', '22' if FAST else '16', '-pix_fmt', 'yuv420p', out],
        cwd=WORK)


# =====================================================================================
# 5) ÂM THANH
# =====================================================================================

def load_audio(path, start=0.0, dur=None, speed=1.0):
    args = [FF, '-hide_banner', '-loglevel', 'error', '-ss', f'{max(0, start):.3f}', '-i', path]
    if dur: args += ['-t', f'{dur:.3f}']
    if abs(speed - 1) > 1e-4: args += ['-af', atempo_chain(speed)]   # tua nhạc, giữ nguyên cao độ
    args += ['-f', 'f32le', '-ac', '2', '-ar', str(SR), '-']
    raw = subprocess.run(args, capture_output=True).stdout
    return np.frombuffer(raw, dtype='<f4').reshape(-1, 2).astype(np.float64) if raw else np.zeros((0, 2))


def resolve_sound(name):
    if name.startswith('game:'): return os.path.join(AUDIO, name[5:])
    if name.startswith('voice:'): return os.path.join(AUDIO, 'Voice', name[6:] + '.mp3')
    return os.path.join(SFX, name)


def mix_audio(total, segs_audio, out):
    n = int(total * SR) + SR
    mix = np.zeros((n, 2))
    music_bus = np.zeros((n, 2))
    duck = np.ones(n)

    def put(bus, sig, at, gain):
        i = int(at * SR)
        if i < 0: sig, i = sig[-i:], 0
        m = min(len(sig), n - i)
        if m > 0: bus[i:i + m] += sig[:m] * gain

    # Nhạc
    music = TL.MUSIC_FILE or next(iter(sorted(glob.glob(os.path.join(FOOTAGE, 'music', '*.*')))), None)
    real_music = bool(music and os.path.exists(music))
    if not real_music: music = os.path.join(SFX, 'temp_beat.wav')
    sig = load_audio(music, start=max(0, TL.MUSIC_OFFSET), speed=MUSIC_SPEED if real_music else 1.0)
    put(music_bus, sig, max(0, -TL.MUSIC_OFFSET) / (MUSIC_SPEED if real_music else 1.0), TL.MUSIC_GAIN)
    # Tiếng gốc của các clip OBS - mờ dần 15ms hai đầu, không thì mỗi cú cắt cảnh kêu "tách"
    edge = int(0.015 * SR)
    for at, sig, gain in segs_audio:
        sig = sig.copy()
        if len(sig) > 2 * edge:
            ramp = np.linspace(0, 1, edge)[:, None]
            sig[:edge] *= ramp; sig[-edge:] *= ramp[::-1]
        put(mix, sig, at, gain)
    # Hiệu ứng + giọng
    for s in TL.SOUNDS:
        if s.get('gain', 1) <= 0: continue
        p = resolve_sound(s['file'])
        if not os.path.exists(p): print('   (thiếu âm thanh)', p); continue
        sig = load_audio(p)
        at = b2s(s['at'])
        put(mix, sig, at, s.get('gain', 1.0))
        if s.get('duck'):                      # hạ nhạc 40% suốt câu nói, lên xuống mềm
            i0, i1 = int(at * SR), int((at + len(sig) / SR + 0.15) * SR)
            ramp = int(0.08 * SR)
            env = np.ones(n); env[i0:i1] = 0.6
            k = np.ones(ramp) / ramp
            duck = np.minimum(duck, np.convolve(env, k, mode='same'))
    mix += music_bus * duck[:, None]
    # Kết thúc: nhạc tắt gọn ở cuối trailer
    end = int(total * SR); fade = int(0.6 * SR)
    mix[end - fade:end] *= np.linspace(1, 0, fade)[:, None]; mix[end:] = 0
    peak = np.max(np.abs(mix)) + 1e-9
    mix = np.tanh(mix / peak * 1.35) / np.tanh(1.35) * 0.93       # nén mềm, đỉnh -0.6 dB
    data = (mix[:end] * 32767).astype('<i2')
    import wave
    with wave.open(out, 'wb') as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())
    return real_music


# =====================================================================================

def main():
    if not os.path.isdir(SFX) or not os.listdir(SFX):
        subprocess.run([sys.executable, os.path.join(HERE, 'sfx.py')], check=True)
    if os.path.isdir(WORK): shutil.rmtree(WORK)
    os.makedirs(WORK)
    total = max(seg_frames(s)[0] + seg_frames(s)[1] for s in TL.SHOTS) / FPS
    print(f'Trailer {total:.2f}s · {TL.BPM} BPM · {W}x{H} {FPS}fps' + (' · BẢN NHANH' if FAST else ''))

    print('  dò khoảnh khắc trong từng clip...')
    plans = [plan_shot(s) for s in TL.SHOTS]
    report(plans)
    if CHECK: return
    missing = [s for s, p in zip(TL.SHOTS, plans) if p['path'] is None]

    segs, segs_audio = [], []
    for i, (shot, p) in enumerate(zip(TL.SHOTS, plans)):
        print(f'  cảnh {i+1:2d}/{len(TL.SHOTS)}  {p["src"]}')
        segs.append(render_segment(i, shot, p))
        g = parse_fx(shot.get('fx', []))['clipaudio']
        if g > 0 and p['path']:
            segs_audio.append((seg_frames(shot)[0] / FPS + p['pad'], clip_audio(p), g))

    lst = os.path.join(WORK, 'list.txt')
    with open(lst, 'w', encoding='utf-8') as f:
        for s in segs: f.write(f"file '{s.replace(os.sep, '/')}'\n")
    body = os.path.join(WORK, 'body.mp4')
    run(['-f', 'concat', '-safe', '0', '-i', lst, '-c', 'copy', body])
    print('  phủ chữ...')
    video = os.path.join(WORK, 'video.mp4')
    overlay_texts(body, video)
    print('  trộn âm thanh...')
    wav = os.path.join(WORK, 'audio.wav')
    real_music = mix_audio(total, segs_audio, wav)

    draft = bool(missing) or not real_music or FAST
    name = 'MagnetoDome_Trailer' + ('_DRAFT' if draft else '') + ('_fast' if FAST else '') + '.mp4'
    final = os.path.join(FOOTAGE, name)
    run(['-i', video, '-i', wav, '-map', '0:v', '-map', '1:a', '-c:v', 'copy', '-c:a', 'aac', '-b:a', '256k',
         '-shortest', '-movflags', '+faststart', final])
    print('\nXONG ->', final)
    if missing:
        print(f'  ⚠ {len(missing)} cảnh dùng THẺ GIỮ CHỖ (chưa có file OBS):')
        for s in {m["src"]: m for m in missing}.values(): print('     -', s['src'], '·', s.get('note', ''))
    if not real_music:
        print('  ⚠ Đang dùng NHẠC TẠM. Bỏ file nhạc thật vào Trailer_Footage/music/ rồi chạy lại.')


if __name__ == '__main__':
    main()
