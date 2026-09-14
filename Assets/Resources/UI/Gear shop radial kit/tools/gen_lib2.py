"""v2 shape language: tight radii, one chamfered corner, flat fills,
hairline borders instead of solid gray, no bevel highlight."""
import math
from PIL import Image, ImageDraw, ImageFilter, ImageChops
from gen_lib import SS, rgb, vgrad

P2 = {
    "bg_deep":     "#0C0E14",
    "surface":     "#161A22",
    "raised":      "#1E232C",
    "raised_hi":   "#272D38",
    "sunken":      "#0F1219",
    "text":        "#EDEFF4",
    "text_dim":    "#8A93A4",
    "text_faint":  "#565F70",
    "accent":      "#FFC24B",
    "accent_dim":  "#C8940F",
    "on_accent":   "#161003",
    "red":         "#EF525A",
    "blue":        "#4C86FF",
    "success":     "#3ECF8E",
    "danger":      "#FF6B72",
}
HAIR = (255, 255, 255, 26)      # hairline border
HAIR_HI = (255, 255, 255, 52)   # hairline on hover / focus


def outline(w, h, r, ch_tl=0, ch_br=0, steps=14):
    """Point list for a rect with radius r and optional chamfered corners."""
    pts = []

    def arc(cx, cy, a0, a1):
        for i in range(steps + 1):
            a = math.radians(a0 + (a1 - a0) * i / steps)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))

    if ch_tl:
        pts.append((ch_tl, 0))
    else:
        arc(r, r, 180, 270)
    arc(w - r, r, 270, 360)
    if ch_br:
        pts.append((w, h - ch_br))
        pts.append((w - ch_br, h))
    else:
        arc(w - r, h - r, 0, 90)
    arc(r, h - r, 90, 180)
    if ch_tl:
        pts.append((0, ch_tl))
    return pts


def mask(size, r, ch_tl=0, ch_br=0):
    w, h = size
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).polygon(outline(w - 1, h - 1, r, ch_tl, ch_br), fill=255)
    return m


def shadow(size, r, ch_tl, ch_br, blur, off, a, pad):
    w, h = size
    lay = Image.new("RGBA", (w + pad * 2, h + pad * 2), (0, 0, 0, 0))
    sh = Image.new("RGBA", lay.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).polygon(
        [(x + pad, y + pad + off) for x, y in
         outline(w - 1, h - 1, r, ch_tl, ch_br)], fill=(0, 0, 0, a))
    lay.alpha_composite(sh.filter(ImageFilter.GaussianBlur(blur)))
    return lay


def slab(size, r, fill, fill2=None, border=HAIR, bw=1, ch_tl=0, ch_br=0,
         pad=0, sh=None, accent_edge=None, alpha_f=1.0):
    """Build one v2 sprite.

    accent_edge = (color, thickness) -> solid bar along the bottom edge,
    the v2 way of marking 'this is the active / primary thing'.
    """
    w, h = size
    W, H = (w + pad * 2) * SS, (h + pad * 2) * SS
    bwv, p = w * SS, pad * SS
    bh, r2 = h * SS, r * SS
    ctl, cbr = ch_tl * SS, ch_br * SS

    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    if sh:
        blur, off, a = sh
        canvas.alpha_composite(
            shadow((bwv, bh), r2, ctl, cbr, blur * SS, off * SS, a, p))

    body = (vgrad((bwv, bh), fill, fill2) if fill2
            else Image.new("RGBA", (bwv, bh), rgb(fill)))
    m = mask((bwv, bh), r2, ctl, cbr)
    body.putalpha(m)

    if accent_edge:
        col, th = accent_edge
        bar = Image.new("RGBA", (bwv, bh), (0, 0, 0, 0))
        ImageDraw.Draw(bar).rectangle(
            [0, bh - th * SS, bwv, bh], fill=rgb(col))
        bar.putalpha(ImageChops.multiply(bar.getchannel("A"), m))
        body.alpha_composite(bar)

    if border:
        st = Image.new("RGBA", (bwv, bh), (0, 0, 0, 0))
        ImageDraw.Draw(st).polygon(
            outline(bwv - 1, bh - 1, r2, ctl, cbr), outline=border,
            width=int(bw * SS))
        body.alpha_composite(st)

    canvas.alpha_composite(body, (p, p))
    out = canvas.resize((w + pad * 2, h + pad * 2), Image.LANCZOS)
    if alpha_f < 1.0:
        out.putalpha(ImageChops.multiply(
            out.getchannel("A"),
            Image.new("L", out.size, int(255 * alpha_f))))
    return out
