"""Holographic v3 sprites for radial menu + shop.

Detail lives on the edges (corner brackets, tick rings, scan lines); the
middle stays empty so icons and numbers read instantly. That's what keeps
"detailed" from turning into "noisy" on a HUD you glance at for 2 seconds.
"""
import os, math
from PIL import Image, ImageDraw, ImageFilter, ImageChops

ROOT = "/mnt/user-data/outputs/lobby-ui-kit"
RS, SH = f"{ROOT}/radial_sprites", f"{ROOT}/shop_sprites"
for p in (RS, SH):
    os.makedirs(p, exist_ok=True)

SS = 4
CY = "#2BE8FF"        # electric cyan — the magnetic-field read
CY_HI = "#B8FBFF"     # hot core
CY_DIM = "#0E7A94"
INK = "#050A12"


def rgb(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def scanlines(size, alpha=16, period=4):
    """Horizontal scan lines — the single cheapest holographic signal."""
    w, h = size
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for y in range(0, h, period * SS):
        d.rectangle([0, y, w, y + SS], fill=(255, 255, 255, alpha))
    return im


def masked(layer, mask):
    out = layer.copy()
    out.putalpha(ImageChops.multiply(out.getchannel("A"), mask))
    return out


# ======================================================= RADIAL SLOT ========
def slot(name, ring_col, ring_w, fill_a, glow=None, ticks=True,
         tick_col=None, brackets=True, dim=False):
    """Circular slot: disc + tick ring + 4 corner brackets + scan lines."""
    D, R = 180, 68
    P = D * SS
    im = Image.new("RGBA", (P, P), (0, 0, 0, 0))
    c = P / 2
    r = R * SS

    if glow:
        g = Image.new("RGBA", (P, P), (0, 0, 0, 0))
        gr = r + 8 * SS
        ImageDraw.Draw(g).ellipse([c - gr, c - gr, c + gr, c + gr],
                                  fill=rgb(glow, 82))
        im.alpha_composite(g.filter(ImageFilter.GaussianBlur(9 * SS)))

    disc = Image.new("L", (P, P), 0)
    ImageDraw.Draw(disc).ellipse([c - r, c - r, c + r, c + r], fill=255)

    body = Image.new("RGBA", (P, P), rgb(INK, fill_a))
    body.alpha_composite(masked(scanlines((P, P), 14 if not dim else 7), disc))
    body = masked(body, disc)
    im.alpha_composite(body)

    d = ImageDraw.Draw(im)
    d.ellipse([c - r, c - r, c + r, c + r], outline=ring_col,
              width=int(ring_w * SS))

    # tick ring just inside the rim — telemetry texture, not decoration
    if ticks:
        tc = tick_col or ring_col
        for i in range(48):
            a = math.radians(i * 7.5)
            long = (i % 4 == 0)
            r0 = r - (11 if long else 6.5) * SS
            r1 = r - 3.4 * SS
            d.line([(c + r0 * math.cos(a), c + r0 * math.sin(a)),
                    (c + r1 * math.cos(a), c + r1 * math.sin(a))],
                   fill=tc, width=int((1.5 if long else 1.0) * SS))

    # corner brackets outside the disc
    if brackets:
        b = (R + 12) * SS
        ln = 15 * SS
        bw = int(2.0 * SS)
        for sx, sy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
            x, y = c + sx * b, c + sy * b
            d.line([(x, y), (x - sx * ln, y)], fill=ring_col, width=bw)
            d.line([(x, y), (x, y - sy * ln)], fill=ring_col, width=bw)

    im.resize((D, D), Image.LANCZOS).save(f"{RS}/{name}.png")


slot("slot_normal", (255, 255, 255, 62), 1.4, 190,
     tick_col=(255, 255, 255, 46), brackets=False)
slot("slot_hover", rgb(CY), 2.4, 214, glow=CY, tick_col=rgb(CY, 190))
slot("slot_disabled", (255, 255, 255, 24), 1.2, 160,
     tick_col=(255, 255, 255, 20), brackets=False, dim=True)

# ---- rotating reticle ring that sits behind the whole menu
D, R = 760, 348
P = D * SS
rt = Image.new("RGBA", (P, P), (0, 0, 0, 0))
d = ImageDraw.Draw(rt)
c, r = P / 2, R * SS
d.ellipse([c - r, c - r, c + r, c + r], outline=rgb(CY, 54),
          width=int(1.4 * SS))
r2 = r - 16 * SS
for i in range(4):
    a0, a1 = i * 90 + 14, i * 90 + 76
    d.arc([c - r2, c - r2, c + r2, c + r2], a0, a1, fill=rgb(CY, 96),
          width=int(2.4 * SS))
for i in range(72):
    a = math.radians(i * 5)
    if i % 6 == 0:
        r0, w = r + 5 * SS, 2.0
    else:
        r0, w = r + 2 * SS, 1.0
    d.line([(c + r * math.cos(a), c + r * math.sin(a)),
            (c + r0 * math.cos(a), c + r0 * math.sin(a))],
           fill=rgb(CY, 70), width=int(w * SS))
rt.resize((D, D), Image.LANCZOS).save(f"{RS}/reticle_ring.png")

# ---- hub
D, R = 260, 96
P = D * SS
hub = Image.new("RGBA", (P, P), (0, 0, 0, 0))
c, r = P / 2, R * SS
disc = Image.new("L", (P, P), 0)
ImageDraw.Draw(disc).ellipse([c - r, c - r, c + r, c + r], fill=255)
hb = Image.new("RGBA", (P, P), rgb(INK, 206))
hb.alpha_composite(masked(scanlines((P, P), 13), disc))
hub.alpha_composite(masked(hb, disc))
hd = ImageDraw.Draw(hub)
hd.ellipse([c - r, c - r, c + r, c + r], outline=rgb(CY, 120),
           width=int(1.6 * SS))
ri = r - 9 * SS
for i in range(2):
    hd.arc([c - ri, c - ri, c + ri, c + ri], 200 + i * 180, 250 + i * 180,
           fill=rgb(CY, 170), width=int(2.2 * SS))
hub.resize((D, D), Image.LANCZOS).save(f"{RS}/hub.png")

# ---- pointer
D = 220
P = D * SS
pt = Image.new("RGBA", (P, P), (0, 0, 0, 0))
pd = ImageDraw.Draw(pt)
c = P / 2
pd.polygon([(c, 26 * SS), (c + 12 * SS, 52 * SS), (c, 45 * SS),
            (c - 12 * SS, 52 * SS)], fill=rgb(CY))
pt.filter(ImageFilter.GaussianBlur(0.5 * SS)).resize(
    (D, D), Image.LANCZOS).save(f"{RS}/select_pointer.png")

# ---- scrim
W, H = 480, 270
sc = Image.new("RGBA", (W, H), (4, 8, 14, 150))
vg = Image.new("RGBA", (W, H), (0, 0, 0, 0))
vd = ImageDraw.Draw(vg)
for i in range(70):
    f = i / 69
    vd.ellipse([W * .5 - W * .72 * (1 - f * .5), H * .5 - H * .94 * (1 - f * .5),
                W * .5 + W * .72 * (1 - f * .5), H * .5 + H * .94 * (1 - f * .5)],
               outline=(2, 5, 9, 7), width=3)
sc.alpha_composite(vg)
sc.resize((960, 540), Image.LANCZOS).save(f"{RS}/screen_scrim.png")


# =========================================================== SHOP CARDS =====
def chamfer(w, h, ch):
    return [(ch, 0), (w - ch, 0), (w, ch), (w, h - ch), (w - ch, h),
            (ch, h), (0, h - ch), (0, ch)]


def card(name, fill_a, edge, edge_w, glow=None, accent=None, dim=False):
    """9-slice card, 140x140, border 30. Chamfered corners + brackets."""
    D, CH = 140, 14
    P = D * SS
    im = Image.new("RGBA", (P, P), (0, 0, 0, 0))
    m = 12 * SS
    pts = [(x + m, y + m) for x, y in
           chamfer(P - 2 * m, P - 2 * m, CH * SS)]

    if glow:
        g = Image.new("RGBA", (P, P), (0, 0, 0, 0))
        ImageDraw.Draw(g).polygon(pts, fill=rgb(glow, 64))
        im.alpha_composite(g.filter(ImageFilter.GaussianBlur(7 * SS)))

    mask = Image.new("L", (P, P), 0)
    ImageDraw.Draw(mask).polygon(pts, fill=255)
    body = Image.new("RGBA", (P, P), rgb(INK, fill_a))
    body.alpha_composite(masked(scanlines((P, P), 7 if dim else 13), mask))
    im.alpha_composite(masked(body, mask))

    d = ImageDraw.Draw(im)
    d.line(pts + [pts[0]], fill=edge, width=int(edge_w * SS))

    # bracket accents on the top-left and bottom-right chamfers
    bl = 22 * SS
    d.line([(m + CH * SS, m), (m + CH * SS + bl, m)], fill=edge,
           width=int((edge_w + 1.4) * SS))
    d.line([(P - m - CH * SS, P - m), (P - m - CH * SS - bl, P - m)],
           fill=edge, width=int((edge_w + 1.4) * SS))

    if accent:
        col, th = accent
        bar = Image.new("RGBA", (P, P), (0, 0, 0, 0))
        ImageDraw.Draw(bar).rectangle(
            [0, P - m - th * SS, P, P - m], fill=rgb(col))
        im.alpha_composite(masked(bar, mask))

    im.resize((D, D), Image.LANCZOS).save(f"{SH}/{name}.png")


card("btn_normal", 198, (255, 255, 255, 60), 1.4)
card("btn_hover", 220, rgb(CY), 2.2, glow=CY, accent=(CY, 4))
card("btn_pressed", 236, rgb(CY_DIM), 2.2)
card("btn_disabled", 158, (255, 255, 255, 22), 1.2, dim=True)
card("btn_owned", 198, rgb("#FFC24B", 130), 1.6, accent=("#FFC24B", 3))

# ---- shop panel
D, CH = 200, 22
P = D * SS
pn = Image.new("RGBA", (P, P), (0, 0, 0, 0))
pts = chamfer(P, P, CH * SS)
mask = Image.new("L", (P, P), 0)
ImageDraw.Draw(mask).polygon(pts, fill=255)
body = Image.new("RGBA", (P, P), rgb(INK, 224))
body.alpha_composite(masked(scanlines((P, P), 10, 5), mask))
pn.alpha_composite(masked(body, mask))
pd = ImageDraw.Draw(pn)
pd.line(pts + [pts[0]], fill=rgb(CY, 96), width=int(1.6 * SS))
for (x, y), (dx, dy) in (((CH * SS, 0), (1, 0)), ((0, CH * SS), (0, 1)),
                         ((P - CH * SS, P), (-1, 0)), ((P, P - CH * SS), (0, -1))):
    pd.line([(x, y), (x + dx * 46 * SS, y + dy * 46 * SS)], fill=rgb(CY),
            width=int(3.0 * SS))
pn.resize((D, D), Image.LANCZOS).save(f"{SH}/shop_panel.png")

# ---- pill
D, R = 80, 38
P = D * SS
pl = Image.new("RGBA", (P, P), (0, 0, 0, 0))
mask = Image.new("L", (P, P), 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, P - 1, P - 1], radius=R * SS,
                                       fill=255)
body = Image.new("RGBA", (P, P), rgb(INK, 200))
body.alpha_composite(masked(scanlines((P, P), 12), mask))
pl.alpha_composite(masked(body, mask))
ImageDraw.Draw(pl).rounded_rectangle([0, 0, P - 1, P - 1], radius=R * SS,
                                     outline=rgb(CY, 110),
                                     width=int(1.4 * SS))
pl.resize((D, D), Image.LANCZOS).save(f"{SH}/pill.png")

print("radial:", sorted(os.listdir(RS)))
print("shop:", sorted(os.listdir(SH)))
