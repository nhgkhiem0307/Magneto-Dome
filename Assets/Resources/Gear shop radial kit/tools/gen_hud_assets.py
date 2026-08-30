"""HUD sprites. Different constraints from the lobby: these sit on top of
live gameplay, so they lean on translucent scrims + hard contrast instead
of the lobby's opaque cards."""
import os, math
from PIL import Image, ImageDraw, ImageChops
from gen_lib import P, SS, panel, rgb, font, mono

OUT = "/mnt/user-data/outputs/lobby-ui-kit/hud_sprites"
ICO = "/mnt/user-data/outputs/lobby-ui-kit/hud_icons"
os.makedirs(OUT, exist_ok=True)
os.makedirs(ICO, exist_ok=True)
MAN = []


def alpha(img, f):
    im = img.copy()
    im.putalpha(ImageChops.multiply(
        im.getchannel("A"), Image.new("L", im.size, int(255 * f))))
    return im


def save(img, name, border, note):
    img.save(f"{OUT}/{name}.png")
    MAN.append((name, img.size, border, note))


# ------------------------------------------------------------- scrims ------
save(alpha(panel((72, 72), 14, "#161A24", "#0E1017", None, 0, pad=0), 0.62),
     "hud_scrim", 16, "Nền mờ gom nhóm HUD (score, chip)")
save(alpha(panel((72, 72), 14, "#161A24", "#0E1017", "#39404F", 2, pad=0),
           0.72),
     "hud_scrim_edge", 16, "Scrim có viền — dùng cho chip nổi bật")
save(alpha(panel((60, 60), 30, "#161A24", "#0E1017", None, 0, pad=0), 0.62),
     "hud_pill", 28, "Pill mờ cho money / ammo / energy")
save(alpha(panel((60, 60), 30, "#2A2214", "#1A150C", P["accent"], 2, pad=0),
           0.9),
     "hud_pill_active", 28, "Pill cho loại đạn đang cầm")

# --------------------------------------------------------------- bars ------
save(alpha(panel((40, 40), 8, "#0C0E14", "#0C0E14", "#2B3140", 2, pad=0),
           0.85),
     "bar_track", 10, "Rãnh nền cho HealthBar / ArmorGroup")
save(panel((40, 40), 8, "#FFFFFF", "#E4E9F2", None, 0, pad=0),
     "bar_fill", 10, "Thanh fill TRẮNG — tint màu trong Image.Color")
save(panel((28, 28), 6, "#FFFFFF", "#E4E9F2", None, 0, pad=0),
     "bar_fill_thin", 7, "Fill mỏng cho ArmorGroup")

# ------------------------------------------------------- center reticle ----
def crosshair():
    G, S = 64, SS
    im = Image.new("RGBA", (G * S, G * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c, gap, ln = G / 2, 6, 13

    def bar(x0, y0, x1, y1, w, col):
        d.rounded_rectangle([(x0 - w / 2) * S, (y0 - w / 2) * S,
                             (x1 + w / 2) * S, (y1 + w / 2) * S],
                            radius=w / 2 * S, fill=col)

    for col, w in ((rgb("#000000", 205), 5.4), ((255, 255, 255, 255), 2.6)):
        bar(c - gap - ln, c, c - gap, c, w, col)
        bar(c + gap, c, c + gap + ln, c, w, col)
        bar(c, c - gap - ln, c, c - gap, w, col)
        bar(c, c + gap, c, c + gap + ln, w, col)
    d.ellipse([(c - 1.5) * S, (c - 1.5) * S, (c + 1.5) * S, (c + 1.5) * S],
              fill=(255, 255, 255, 255))
    return im.resize((G, G), Image.LANCZOS)


save(crosshair(), "crosshair", 0, "Tâm ngắm — trắng viền đen, Type=Simple")


def ring(fill, edge, a=0.85):
    G, S = 96, SS
    im = Image.new("RGBA", (G * S, G * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    m = 6 * S
    d.ellipse([m, m, G * S - m, G * S - m], fill=rgb(fill),
              outline=rgb(edge), width=4 * S)
    return alpha(im.resize((G, G), Image.LANCZOS), a)


save(ring("#12161F", "#E9ECF3"), "polarity_ring",
     0, "Vòng PolarityIndicator — trạng thái trung tính")
save(ring("#2A2214", P["accent"]), "polarity_ring_pos",
     0, "PolarityIndicator — cực dương")
save(ring("#1C2438", "#4C86FF"), "polarity_ring_neg",
     0, "PolarityIndicator — cực âm")

# --------------------------------------------------------- announcement ----
def announce():
    W, H, S = 120, 72, SS
    im = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W * S, H * S], fill=rgb("#0E1017"))
    ramp = Image.new("L", (W * S, 1))
    px = ramp.load()
    for x in range(W * S):
        f = abs(x / (W * S) - 0.5) * 2
        px[x, 0] = int(255 * max(0.0, 1 - f ** 1.6))
    ramp = ramp.resize((W * S, H * S), Image.BILINEAR)
    im.putalpha(ImageChops.multiply(im.getchannel("A"), ramp))
    return alpha(im.resize((W, H), Image.LANCZOS), 0.78)


save(announce(), "announce_bar", 0,
     "Dải mờ sau Announcement — mờ dần 2 mép, Type=Simple, kéo ngang thoải mái")

# ------------------------------------------------------------ dead panel ---
def vignette():
    W, H = 192, 108
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for i in range(70):
        f = i / 69
        a = int(215 * (f ** 1.5))
        d.ellipse([-W * 0.55 + W * 1.05 * f * 0.5 + W * 0.02,
                   -H * 0.75 + H * 1.25 * f * 0.5,
                   W * 1.55 - W * 1.05 * f * 0.5 - W * 0.02,
                   H * 1.75 - H * 1.25 * f * 0.5],
                  outline=(6, 7, 11, max(a - 200, 0)), width=2)
    base = Image.new("RGBA", (W, H), (6, 7, 11, 110))
    base.alpha_composite(im)
    return base.resize((384, 216), Image.LANCZOS)


save(vignette(), "dead_overlay", 0,
     "Lớp phủ DeadPanel — Anchor Stretch toàn màn, Type=Simple")

# ----------------------------------------------------------------- icons ---
S, G, WD = 8, 64, 3.4
C = (255, 255, 255, 255)


def new():
    return Image.new("RGBA", (G * S, G * S), (0, 0, 0, 0))


def cap(d, p, w=WD):
    r = w * S / 2
    d.ellipse([p[0] * S - r, p[1] * S - r, p[0] * S + r, p[1] * S + r], fill=C)


def path(d, pts, w=WD, closed=False):
    p = [(x * S, y * S) for x, y in pts]
    d.line(p + ([p[0]] if closed else []), fill=C, width=int(w * S))
    for q in pts:
        cap(d, q, w)


def poly(d, pts):
    d.polygon([(x * S, y * S) for x, y in pts], fill=C)


def emit(name, fn):
    im = new()
    fn(ImageDraw.Draw(im))
    im.resize((G, G), Image.LANCZOS).save(f"{ICO}/ic_{name}.png")


emit("shield", lambda d: path(
    d, [(32, 11), (51, 19), (51, 34), (32, 53), (13, 34), (13, 19)],
    closed=True))

emit("shield_full", lambda d: poly(
    d, [(32, 11), (51, 19), (51, 34), (32, 53), (13, 34), (13, 19)]))


def _bullet(d):
    d.rounded_rectangle([25 * S, 26 * S, 39 * S, 54 * S], radius=4 * S, fill=C)
    poly(d, [(25, 26), (32, 9), (39, 26)])


emit("ammo", _bullet)


def _heavy(d):
    for x in (20, 32, 44):
        d.rounded_rectangle([(x - 5) * S, 28 * S, (x + 5) * S, 54 * S],
                            radius=3 * S, fill=C)
        poly(d, [(x - 5, 28), (x, 14), (x + 5, 28)])


emit("ammo_heavy", _heavy)


def _spike(d):
    d.rounded_rectangle([20 * S, 22 * S, 44 * S, 52 * S], radius=5 * S,
                        outline=C, width=int(WD * S))
    path(d, [(32, 10), (32, 22)])
    d.ellipse([28 * S, 31 * S, 36 * S, 39 * S], fill=C)
    path(d, [(32, 39), (32, 45)], w=WD - 0.6)


emit("spike", _spike)


def _can(d):
    d.rounded_rectangle([22 * S, 16 * S, 42 * S, 54 * S], radius=6 * S,
                        outline=C, width=int(WD * S))
    path(d, [(23, 26), (41, 26)], w=WD - 0.8)
    path(d, [(27, 12), (37, 12)], w=WD - 0.6)


emit("energy", _can)


def _money(d):
    path(d, [(32, 10), (32, 54)], w=WD - 0.6)
    path(d, [(43, 20), (26, 20), (22, 27), (26, 32), (38, 32), (42, 38),
             (38, 45), (21, 45)])


emit("money", _money)


def _skull(d):
    d.rounded_rectangle([13 * S, 11 * S, 51 * S, 43 * S], radius=17 * S,
                        outline=C, width=int(WD * S))
    d.ellipse([20 * S, 21 * S, 30 * S, 33 * S], fill=C)
    d.ellipse([34 * S, 21 * S, 44 * S, 33 * S], fill=C)
    poly(d, [(32, 34), (28, 41), (36, 41)])
    d.rounded_rectangle([25 * S, 42 * S, 39 * S, 55 * S], radius=4 * S,
                        outline=C, width=int(WD * S))
    path(d, [(32, 43), (32, 54)], w=WD - 1.2)


emit("skull", _skull)


def _timer(d):
    d.ellipse([13 * S, 15 * S, 51 * S, 53 * S], outline=C, width=int(WD * S))
    path(d, [(32, 24), (32, 34), (40, 39)])
    path(d, [(25, 10), (39, 10)], w=WD - 0.6)


emit("timer", _timer)

print(len(MAN), "hud sprites,", len(os.listdir(ICO)), "icons")
for n, s, b, note in MAN:
    print(f"  {n:22} {s[0]}x{s[1]:<5} border {b}")
