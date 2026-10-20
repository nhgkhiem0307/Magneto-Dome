# -*- coding: utf-8 -*-
# Ghép poster A1 dọc (594 x 841 mm, 200 dpi = 4677 x 6622) từ ảnh render keyart.png
# Chạy:  py -3 PosterBuilder/make_poster.py   (cần Pillow: py -3 -m pip install pillow)
# Ra:    NOP_BAI/Poster_MagnetoDome_A1.pdf + .png
import io, os, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops, ImageEnhance

D = os.path.dirname(os.path.abspath(__file__)) + '/'
OUT = D + '../NOP_BAI/'
F = D + 'fonts/Poppins-'
base = Image.open(D + 'keyart.png').convert('RGB')
W, H = base.size
S = W / 944.0                       # thiết kế theo khung 944 rộng, nhân lên
def s(v): return int(round(v * S))
def font(w, px): return ImageFont.truetype(D + 'fonts/BeVietnamPro-' + w + '.ttf', s(px))
def fontP(w, px): return ImageFont.truetype(F + w + '.ttf', s(px))   # Poppins: chỉ cho logo chữ Anh

RED = (255, 76, 56)
BLUE = (70, 160, 255)
NAVY = (8, 18, 40)
WHITE = (255, 255, 255)
GOLD = (255, 205, 90)

# --- Chỉnh màu nhẹ: tăng tương phản + bão hoà cho ảnh in ---
img = ImageEnhance.Contrast(base).enhance(1.08)
img = ImageEnhance.Color(img).enhance(1.08)
img = img.convert('RGBA')

def vgrad(y0, y1, color, a0, a1):
    """Lớp phủ dải màu theo chiều dọc từ y0 (độ đặc a0) tới y1 (a1)"""
    lay = Image.new('RGBA', (W, H), color + (0,))
    col = Image.new('L', (1, H), 0)
    px = col.load()
    for y in range(H):
        if y <= y0: a = a0
        elif y >= y1: a = a1
        else:
            t = (y - y0) / float(y1 - y0); t = t * t * (3 - 2 * t)
            a = a0 + (a1 - a0) * t
        px[0, y] = int(255 * a)
    lay.putalpha(col.resize((W, H)))
    return lay

img = Image.alpha_composite(img, vgrad(0, s(460), NAVY, 0.66, 0.0))          # đỉnh: tối cho chữ logo nổi
img = Image.alpha_composite(img, vgrad(s(915), s(1075), NAVY, 0.0, 0.93))    # đáy: bảng thông tin

def text_layer(txt, fnt, fill, anchor_xy, anchor='mm', tracking=0):
    """Vẽ chữ lên lớp trong suốt riêng (để còn đổ bóng / tô dải màu)"""
    lay = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    if tracking == 0:
        d.text(anchor_xy, txt, font=fnt, fill=fill, anchor=anchor)
        return lay
    # Giãn chữ: tự đặt từng ký tự
    widths = [d.textlength(c, font=fnt) for c in txt]
    total = sum(widths) + tracking * (len(txt) - 1)
    x, y = anchor_xy
    if anchor[0] == 'm': x -= total / 2
    elif anchor[0] == 'r': x -= total
    for c, w in zip(txt, widths):
        d.text((x, y), c, font=fnt, fill=fill, anchor='l' + anchor[1])
        x += w + tracking
    return lay

def shadow(lay, blur, alpha, offset=(0, 0), color=(0, 0, 0)):
    a = lay.split()[3].filter(ImageFilter.GaussianBlur(blur))
    a = a.point(lambda v: int(min(255, v * alpha)))
    sh = Image.new('RGBA', (W, H), color + (0,))
    sh.putalpha(a)
    return ImageChops.offset(sh, offset[0], offset[1])

def hgrad_fill(lay, c0, c1):
    """Tô dải màu ngang (đỏ -> xanh) vào phần có chữ của lớp"""
    a = lay.split()[3]
    box = a.getbbox()
    g = Image.new('RGBA', (W, H))
    gd = ImageDraw.Draw(g)
    for x in range(box[0], box[2]):
        t = (x - box[0]) / float(box[2] - box[0])
        c = tuple(int(c0[i] + (c1[i] - c0[i]) * t) for i in range(3))
        gd.line([(x, box[1]), (x, box[3])], fill=c + (255,))
    g.putalpha(a)
    return g

def put(lay, sh_blur=10, sh_alpha=0.85, glow=None):
    global img
    img = Image.alpha_composite(img, shadow(lay, s(sh_blur), sh_alpha, (0, s(4))))
    if glow:
        img = Image.alpha_composite(img, shadow(lay, s(glow[1]), glow[2], (0, 0), glow[0]))
    img = Image.alpha_composite(img, lay)

cx = W // 2

# ================= ĐẦU POSTER: logo =================
put(text_layer(u'CAPSTONE PROJECT  ·  GAME PROGRAMMING', font('SemiBold', 19), (220, 235, 255, 255), (cx, s(56)), tracking=s(5)), 6, 0.7)

logo1 = text_layer(u'MAGNETO', fontP('Black', 150), WHITE + (255,), (cx, s(152)), tracking=s(4))
put(logo1, 16, 0.9, glow=((120, 190, 255), 30, 0.55))

dome = text_layer(u'DOME', fontP('Black', 118), WHITE + (255,), (cx, s(266)), tracking=s(26))
dome = hgrad_fill(dome, RED, BLUE)
put(dome, 14, 0.9, glow=((255, 255, 255), 26, 0.25))

# Hai vạch hai bên chữ DOME: cực đỏ (+) bên trái, cực xanh (-) bên phải
d = ImageDraw.Draw(img)
bb = dome.split()[3].getbbox()
yl = (bb[1] + bb[3]) // 2
d.rounded_rectangle([s(70), yl - s(3), bb[0] - s(28), yl + s(3)], radius=s(3), fill=RED + (255,))
d.rounded_rectangle([bb[2] + s(28), yl - s(3), W - s(70), yl + s(3)], radius=s(3), fill=BLUE + (255,))
put(text_layer(u'+', font('Black', 46), RED + (255,), (s(52), yl - s(2))), 6, 0.6)
put(text_layer(u'−', font('Black', 46), BLUE + (255,), (W - s(52), yl - s(2))), 6, 0.6)

put(text_layer(u'NO GUNS. JUST MAGNETISM.', font('Bold', 40), WHITE + (255,), (cx, s(352)), tracking=s(3)), 10, 0.9)
put(text_layer(u'Không súng đạn — chỉ có găng tay từ tính', font('Medium', 25), (225, 238, 255, 255), (cx, s(393))), 8, 0.9)

# ================= CHÂN POSTER: 3 điểm nhấn + thông tin =================
feats = [
    (RED,  u'MAGNETIC GLOVES', [u'Nạp điện, hút, đẩy và bắn', u'mọi vật thể trên đảo']),
    (GOLD, u'OVERLOAD',        [u'Không có thanh máu — càng', u'nhiễm điện càng văng xa']),
    (BLUE, u'1V1 · 2V2 ONLINE', [u'Đấu theo round, chiếm khu,', u'rơi khỏi đảo là thua']),
]
colw = (W - s(2 * 64)) / 3.0
top = s(1092)
for i, (c, title, lines) in enumerate(feats):
    x0 = s(64) + colw * i
    xc = int(x0 + colw / 2)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([xc - s(26), top, xc + s(26), top + s(6)], radius=s(3), fill=c + (255,))
    put(text_layer(title, font('Bold', 25), WHITE + (255,), (xc, top + s(36)), tracking=s(1.5)), 6, 0.6)
    for j, ln in enumerate(lines):
        put(text_layer(ln, font('Regular', 18), (200, 215, 235, 255), (xc, top + s(72) + s(25) * j)), 4, 0.5)

# Vạch ngăn mảnh
d = ImageDraw.Draw(img)
yl = s(1216)
d.line([(s(90), yl), (W - s(90), yl)], fill=(255, 255, 255, 60), width=max(1, s(1)))

put(text_layer(u'Nguyễn Hoàng Gia Khiêm', font('SemiBold', 24), WHITE + (255,), (cx, s(1248))), 4, 0.5)
put(text_layer(u'MSHV 144010124020  ·  Lớp K24M-GAME1  ·  GVHD: cô Phạm Thị Hằng', font('Regular', 17), (190, 205, 228, 255), (cx, s(1280))), 4, 0.5)
put(text_layer(u'UNITY 6  ·  URP  ·  PHOTON FUSION 2', font('Medium', 14), (130, 160, 200, 255), (cx, s(1305)), tracking=s(3)), 4, 0.5)

out = img.convert('RGB')
out.save(OUT + 'Poster_MagnetoDome_A1.png', dpi=(200, 200))
out.save(OUT + 'Poster_MagnetoDome_A1.pdf', resolution=200.0, quality=95)
out.resize((944, 1336), Image.LANCZOS).save(D + 'poster_preview.png')   # bản nhỏ xem nhanh
print('ok', out.size)
