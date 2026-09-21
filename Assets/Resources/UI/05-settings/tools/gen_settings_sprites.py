"""Settings UI sprites, hologram/cyan language.

Wide controls need their own 9-slice borders: the shop card sprites have
border 40, which exceeds the height of a 14px slider track and would be
rejected by Unity.
"""
import os, math
from PIL import Image, ImageDraw, ImageFilter, ImageChops

OUT = "/mnt/user-data/outputs/lobby-ui-kit/settings_sprites"
os.makedirs(OUT, exist_ok=True)

SS = 4
CY = "#2BE8FF"
CY_HI = "#B8FBFF"
CY_DIM = "#0E7A94"
INK = "#050A12"
RED = "#FF6B72"


def rgb(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def masked(l, m):
    o = l.copy()
    o.putalpha(ImageChops.multiply(o.getchannel("A"), m))
    return o


def scan(size, alpha, period=4):
    w, h = size
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for y in range(0, h, period * SS):
        d.rectangle([0, y, w, y + SS], fill=(255, 255, 255, alpha))
    return im


def chamfer(w, h, ch):
    return [(ch, 0), (w - ch, 0), (w, ch), (w, h - ch), (w - ch, h),
            (ch, h), (0, h - ch), (0, ch)]


def slab(name, size, ch, fill_a, edge, edge_w=1.4, pad=0, glow=None,
         scan_a=12, bracket=0, accent=None, fill_col=INK):
    """Chamfered panel/button. pad leaves room for the glow."""
    w, h = size
    W, H = (w + pad * 2) * SS, (h + pad * 2) * SS
    p = pad * SS
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    pts = [(x + p, y + p) for x, y in
           chamfer(w * SS, h * SS, ch * SS)]

    if glow:
        g = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ImageDraw.Draw(g).polygon(pts, fill=rgb(glow, 66))
        im.alpha_composite(g.filter(ImageFilter.GaussianBlur(6 * SS)))

    m = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m).polygon(pts, fill=255)
    body = Image.new("RGBA", (W, H), rgb(fill_col, fill_a))
    if scan_a:
        body.alpha_composite(masked(scan((W, H), scan_a), m))
    im.alpha_composite(masked(body, m))

    d = ImageDraw.Draw(im)
    d.line(pts + [pts[0]], fill=edge, width=int(edge_w * SS))

    if bracket:
        bl = bracket * SS
        d.line([(p + ch * SS, p), (p + ch * SS + bl, p)], fill=edge,
               width=int((edge_w + 1.4) * SS))
        d.line([(W - p - ch * SS, H - p), (W - p - ch * SS - bl, H - p)],
               fill=edge, width=int((edge_w + 1.4) * SS))

    if accent:
        col, th = accent
        bar = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ImageDraw.Draw(bar).rectangle([0, H - p - th * SS, W, H - p],
                                      fill=rgb(col))
        im.alpha_composite(masked(bar, m))

    im.resize((w + pad * 2, h + pad * 2), Image.LANCZOS).save(
        f"{OUT}/{name}.png")


# ------------------------------------------------------------- containers ---
# panel: 200x200, border 44
slab("panel", (200, 200), 22, 226, rgb(CY, 96), 1.6, scan_a=10, bracket=46)
# row behind each setting: 96x96, border 26
slab("row", (96, 96), 10, 172, (255, 255, 255, 40), 1.2, scan_a=10)
slab("row_hover", (96, 96), 10, 196, rgb(CY, 130), 1.4, scan_a=12)

# ---------------------------------------------------------------- buttons ---
# 96x96, border 26 — works down to 56px tall
def btn(name, fill_a, edge, ew=1.4, glow=None, accent=None, col=INK):
    slab(name, (96, 96), 12, fill_a, edge, ew, pad=10, glow=glow,
         scan_a=12, bracket=22, accent=accent, fill_col=col)


btn("btn_normal", 200, (255, 255, 255, 60))
btn("btn_hover", 222, rgb(CY), 2.0, glow=CY, accent=(CY, 3))
btn("btn_pressed", 238, rgb(CY_DIM), 2.0)
btn("btn_disabled", 158, (255, 255, 255, 22))
btn("btn_danger_normal", 200, rgb(RED, 120), 1.6, col="#180A0C")
btn("btn_danger_hover", 224, rgb(RED), 2.0, glow=RED, accent=(RED, 3),
    col="#1E0B0E")

# ----------------------------------------------------------------- slider ---
# track 64x14, stretched horizontally only -> border L/R 8, T/B 0
def bar(name, fill, a, edge=None, scan_a=0, glow=None):
    w, h = 64, 14
    W, H = w * SS, h * SS
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    m = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, W - 1, H - 1],
                                        radius=3 * SS, fill=255)
    body = Image.new("RGBA", (W, H), rgb(fill, a))
    if scan_a:
        body.alpha_composite(masked(scan((W, H), scan_a, 3), m))
    im.alpha_composite(masked(body, m))
    if edge:
        ImageDraw.Draw(im).rounded_rectangle(
            [0, 0, W - 1, H - 1], radius=3 * SS, outline=edge,
            width=int(1.2 * SS))
    im.resize((w, h), Image.LANCZOS).save(f"{OUT}/{name}.png")


bar("slider_track", INK, 200, (255, 255, 255, 40), scan_a=9)
bar("slider_fill", CY, 255)
bar("slider_fill_dim", CY_DIM, 255)

# handle: diamond in a ring, 56x56 with glow room
D = 56
P = D * SS
hd = Image.new("RGBA", (P, P), (0, 0, 0, 0))
c = P / 2
g = Image.new("RGBA", (P, P), (0, 0, 0, 0))
ImageDraw.Draw(g).ellipse([c - 15 * SS, c - 15 * SS, c + 15 * SS, c + 15 * SS],
                          fill=rgb(CY, 92))
hd.alpha_composite(g.filter(ImageFilter.GaussianBlur(5 * SS)))
d = ImageDraw.Draw(hd)
r = 13 * SS
d.ellipse([c - r, c - r, c + r, c + r], fill=rgb(INK, 236),
          outline=rgb(CY), width=int(2.0 * SS))
k = 5.4 * SS
d.polygon([(c, c - k), (c + k, c), (c, c + k), (c - k, c)], fill=rgb(CY_HI))
hd.resize((D, D), Image.LANCZOS).save(f"{OUT}/slider_handle.png")

# --------------------------------------------------------------- dropdown ---
slab("dd_normal", (96, 96), 10, 200, (255, 255, 255, 56), 1.4, scan_a=11)
slab("dd_hover", (96, 96), 10, 220, rgb(CY), 1.8, scan_a=12)
slab("dd_template", (96, 96), 10, 238, rgb(CY, 110), 1.4, scan_a=9)
slab("dd_item", (48, 48), 0, 0, (0, 0, 0, 0), 0, scan_a=0)
slab("dd_item_hover", (48, 48), 6, 210, rgb(CY, 150), 1.2, scan_a=10,
     fill_col="#0A1A22")

# ----------------------------------------------------------------- toggle ---
def toggle(name, on):
    D = 52
    P = D * SS
    im = Image.new("RGBA", (P, P), (0, 0, 0, 0))
    pts = chamfer(P, P, 9 * SS)
    if on:
        g = Image.new("RGBA", (P, P), (0, 0, 0, 0))
        ImageDraw.Draw(g).polygon(pts, fill=rgb(CY, 74))
        im.alpha_composite(g.filter(ImageFilter.GaussianBlur(5 * SS)))
    m = Image.new("L", (P, P), 0)
    ImageDraw.Draw(m).polygon(pts, fill=255)
    body = Image.new("RGBA", (P, P), rgb("#0A1A22" if on else INK,
                                         214 if on else 190))
    body.alpha_composite(masked(scan((P, P), 12), m))
    im.alpha_composite(masked(body, m))
    d = ImageDraw.Draw(im)
    d.line(pts + [pts[0]], fill=rgb(CY) if on else (255, 255, 255, 56),
           width=int((2.0 if on else 1.4) * SS))
    im.resize((D, D), Image.LANCZOS).save(f"{OUT}/{name}.png")


toggle("toggle_off", False)
toggle("toggle_on", True)

# checkmark for the toggle
D = 40
P = D * SS
ck = Image.new("RGBA", (P, P), (0, 0, 0, 0))
d = ImageDraw.Draw(ck)
pts = [(9 * SS, 21 * SS), (16 * SS, 28 * SS), (31 * SS, 11 * SS)]
d.line(pts, fill=rgb(CY_HI), width=int(4.0 * SS), joint="curve")
for p_ in pts:
    d.ellipse([p_[0] - 2 * SS, p_[1] - 2 * SS, p_[0] + 2 * SS, p_[1] + 2 * SS],
              fill=rgb(CY_HI))
ck.resize((D, D), Image.LANCZOS).save(f"{OUT}/toggle_check.png")

# chevron for the dropdown arrow
D = 40
P = D * SS
ar = Image.new("RGBA", (P, P), (0, 0, 0, 0))
d = ImageDraw.Draw(ar)
d.line([(11 * SS, 15 * SS), (20 * SS, 25 * SS), (29 * SS, 15 * SS)],
       fill=rgb(CY), width=int(3.2 * SS), joint="curve")
ar.resize((D, D), Image.LANCZOS).save(f"{OUT}/dd_arrow.png")

print(len(os.listdir(OUT)), "sprites ->", OUT)
for f in sorted(os.listdir(OUT)):
    im = Image.open(f"{OUT}/{f}")
    print(f"  {f:26} {im.size[0]}x{im.size[1]}")
