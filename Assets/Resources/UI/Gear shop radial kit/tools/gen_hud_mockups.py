import os, math
from PIL import Image, ImageDraw, ImageFilter
from gen_lib import P, rgb, nineslice, font, mono, text

V = os.environ.get("KIT_V", "")
HS = "/mnt/user-data/outputs/lobby-ui-kit/hud_sprites" + V
HI = "/mnt/user-data/outputs/lobby-ui-kit/hud_icons"
OUT = "/mnt/user-data/outputs/lobby-ui-kit/mockups" + V
os.makedirs(OUT, exist_ok=True)
MODERN = bool(V)

B = {"hud_scrim": 16, "hud_scrim_edge": 16, "hud_pill": 28,
     "hud_pill_active": 28, "bar_track": 10, "bar_fill": 10,
     "bar_fill_thin": 7}
_c = {}


def spr(n):
    if n not in _c:
        _c[n] = Image.open(f"{HS}/{n}.png").convert("RGBA")
    return _c[n]


def box(c, n, x, y, w, h, tint=None):
    s = nineslice(spr(n), int(w), int(h), (B[n],) * 4)
    if tint:
        t = Image.new("RGBA", s.size, rgb(tint))
        t.putalpha(s.getchannel("A"))
        s = t
    c.alpha_composite(s, (int(x), int(y)))


def ico(n, size, col, a=255):
    im = Image.open(f"{HI}/ic_{n}.png").convert("RGBA").resize(
        (size, size), Image.LANCZOS)
    t = Image.new("RGBA", im.size, rgb(col, a))
    t.putalpha(im.getchannel("A").point(lambda v: v * a // 255))
    return t


def ot(d, xy, s, f, fill, anchor="la", sw=3):
    """HUD text always carries a dark outline so it survives a bright wall."""
    d.text(xy, s, font=f, fill=rgb(fill), anchor=anchor,
           stroke_width=sw, stroke_fill=(6, 8, 12, 190))


# ------------------------------------------------- fake gameplay backdrop ---
def scene():
    w, h, hz = 1920, 1080, 566
    c = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(c)
    for y in range(h):
        if y < hz:
            f = y / hz
            col = (int(58 + 12 * f), int(68 + 12 * f), int(86 + 10 * f))
        else:
            f = (y - hz) / (h - hz)
            col = (int(34 - 14 * f), int(40 - 16 * f), int(52 - 20 * f))
        d.line([(0, y), (w, y)], fill=col)

    for x in range(-16, 34):
        d.line([(960 + x * 58, hz), (960 + x * 640, h)],
               fill=(52, 60, 76), width=2)
    yy = hz
    step = 6
    while yy < h:
        d.line([(0, yy), (w, yy)], fill=(50, 58, 74), width=2)
        step *= 1.42
        yy += step

    blocks = [(80, 250, 430, hz, "#4A5568"), (350, 360, 560, hz, "#3A4456"),
              (1240, 200, 1560, hz, "#525E72"), (1520, 320, 1840, hz, "#333C4C"),
              (640, 430, 800, hz, "#5C687C"), (1080, 400, 1210, hz, "#2E3644")]
    for x0, y0, x1, y1, col in blocks:
        d.rectangle([x0, y0, x1, y1], fill=rgb(col))
        d.rectangle([x0, y0, x1, y0 + 7], fill=rgb("#6C7A90"))
        d.rectangle([x0, y0, x0 + 5, y1], fill=rgb("#1F2530"))

    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse([170, 120, 900, 780], fill=(255, 226, 168, 132))
    gd.ellipse([1400, 420, 1980, 900], fill=(90, 120, 180, 70))
    c.alpha_composite(glow.filter(ImageFilter.GaussianBlur(200)))
    c = c.filter(ImageFilter.GaussianBlur(2.2))

    vig = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    vd = ImageDraw.Draw(vig)
    vd.ellipse([-420, -300, w + 420, h + 300], fill=(0, 0, 0, 0),
               outline=(0, 0, 0, 0))
    for i in range(60):
        f = i / 59
        vd.ellipse([-420 + 700 * f, -300 + 520 * f,
                    w + 420 - 700 * f, h + 300 - 520 * f],
                   outline=(4, 6, 10, 8), width=14)
    c.alpha_composite(vig)
    return c


# ---------------------------------------------------------------- widgets ---
def group_top(c, red, blue, timer, phase, rnd):
    d = ImageDraw.Draw(c)
    text(d, (960, 34), rnd.upper(), mono(16) if MODERN else font("medium", 16),
         rgb("#A7B0C2"), anchor="mt", spacing=4.2 if MODERN else 3.4)

    sw, sh, sy = 420, 84, 62
    box(c, "hud_scrim", 960 - sw / 2, sy, sw, sh)
    ot(d, (960 - 104, sy + sh / 2 + 2), red, mono(46), P["red"], "mm")
    ot(d, (960, sy + sh / 2), "—", font("regular", 30), "#5C6577", "mm", sw=2)
    ot(d, (960 + 104, sy + sh / 2 + 2), blue, mono(46), P["blue"], "mm")

    ty = sy + sh + 10
    box(c, "hud_pill", 960 - 82, ty, 164, 54)
    ot(d, (960, ty + 28), timer, mono(34), P["text"], "mm")

    if phase:
        text(d, (960, ty + 72), phase.upper(),
             mono(17) if MODERN else font("medium", 17), rgb(P["accent"]),
             anchor="mt", spacing=4.4 if MODERN else 3.6)


def group_center(c, polarity="pos", announce=None):
    d = ImageDraw.Draw(c)
    ch = Image.open(f"{HS}/crosshair.png").convert("RGBA")
    ch = ch.resize((72, 72), Image.LANCZOS)
    c.alpha_composite(ch, (960 - 36, 540 - 36))

    ring = {"pos": "polarity_ring_pos", "neg": "polarity_ring_neg",
            "neutral": "polarity_ring"}[polarity]
    r = Image.open(f"{HS}/{ring}.png").convert("RGBA").resize((84, 84),
                                                              Image.LANCZOS)
    c.alpha_composite(r, (960 - 42, 650 - 42))
    sym, col = {"pos": ("+", P["accent"]), "neg": ("−", "#7BA6FF"),
                "neutral": ("·", P["text_dim"])}[polarity]
    ot(d, (960, 648), sym, font("bold", 38), col, "mm", sw=2)

    if announce:
        ab = Image.open(f"{HS}/announce_bar.png").convert("RGBA")
        c.alpha_composite(ab.resize((1100, 92), Image.LANCZOS), (410, 314))
        ot(d, (960, 360), announce, font("bold", 46), P["text"], "mm")


def group_bl(c, hp, armor, energy=None):
    d = ImageDraw.Draw(c)
    x, base, bw = 44, 1040, 420

    hy = base - 32
    box(c, "bar_track", x, hy, bw, 32)
    hcol = P["success"] if hp > 35 else P["red"]
    if hp > 0:
        box(c, "bar_fill", x + 4, hy + 4, max((bw - 8) * hp / 100, 12), 24,
            tint=hcol)
    ot(d, (x + bw + 26, hy + 16), str(hp), mono(44), P["text"], "lm")

    ay = hy - 40
    box(c, "bar_track", x, ay, bw, 14)
    acol = "#9FB0CC" if armor else "#4C5566"
    if armor > 0:
        box(c, "bar_fill_thin", x + 3, ay + 3, max((bw - 6) * armor / 50, 8), 8,
            tint=acol)
    c.alpha_composite(ico("shield", 24, acol), (x + bw + 26, ay - 5))
    ot(d, (x + bw + 58, ay + 7), str(armor), mono(26), acol, "lm", sw=2)

    if energy:
        ey = ay - 78
        w = 64 + int(d.textlength(energy, font=mono(26)))
        box(c, "hud_pill", x, ey, w, 54)
        c.alpha_composite(ico("energy", 28, P["accent"]), (x + 18, ey + 13))
        ot(d, (x + 54, ey + 28), energy, mono(26), P["accent"], "lm", sw=2)


def group_br(c, money, ammo, active=0):
    d = ImageDraw.Draw(c)
    right, base = 1876, 1036

    chips, tot = [], 0
    for i, (icn, label) in enumerate(ammo):
        w = 68 + int(d.textlength(label, font=mono(26)))
        chips.append((icn, label, w, i == active))
        tot += w + 12
    cx = right - tot + 12
    cy = base - 56
    for icn, label, w, act in chips:
        box(c, "hud_pill_active" if act else "hud_pill", cx, cy, w, 56)
        col = P["accent"] if act else P["text_dim"]
        c.alpha_composite(ico(icn, 26, col), (int(cx + 18), cy + 15))
        ot(d, (cx + 54, cy + 29), label, mono(26), col, "lm", sw=2)
        cx += w + 12

    my = cy - 68
    c.alpha_composite(ico("money", 30, P["accent"]), (right - 30 - int(
        d.textlength(money, font=mono(36))) - 12, my + 8))
    ot(d, (right, my + 24), money, mono(36), P["accent"], "rm")


def dead(c, sub):
    d = ImageDraw.Draw(c)
    ov = Image.open(f"{HS}/dead_overlay.png").convert("RGBA")
    c.alpha_composite(ov.resize((1920, 1080), Image.LANCZOS))
    c.alpha_composite(ico("skull", 92, P["red"], 235), (960 - 46, 400))
    ot(d, (960, 542), "BẠN ĐÃ BỊ LOẠI", font("bold", 52), P["text"], "mm")
    ot(d, (960, 596), sub, font("regular", 24), "#A7B0C2", "mm", sw=2)


# ----------------------------------------------------------------- scenes ---
def hud_combat():
    c = scene()
    group_top(c, "7", "5", "1:32", None, "round 9")
    group_center(c, "pos")
    group_bl(c, 68, 25, "12s")
    group_br(c, "2.450", [("ammo", "12"), ("ammo_heavy", "4"),
                          ("spike", "3")], active=0)
    return c


def hud_buy():
    c = scene()
    group_top(c, "7", "5", "0:24", "pha mua sắm", "round 10")
    group_center(c, "neutral", announce="PHA MUA SẮM")
    group_bl(c, 100, 50, None)
    group_br(c, "5.800", [("ammo", "20"), ("ammo_heavy", "6"),
                          ("spike", "5")], active=1)
    return c


def hud_dead():
    c = scene()
    group_top(c, "7", "6", "0:47", None, "round 9")
    group_bl(c, 0, 0, None)
    group_br(c, "0", [("ammo", "0"), ("ammo_heavy", "0"), ("spike", "0")],
             active=2)
    dead(c, "Đang xem Trung · hồi sinh ở vòng sau")
    return c


if __name__ == "__main__":
    for n, fn in [("06_hud_combat", hud_combat), ("07_hud_buy_phase", hud_buy),
                  ("08_hud_dead", hud_dead)]:
        fn().convert("RGB").save(f"{OUT}/{n}.png", quality=94)
        print("ok", n)
