"""Replacement ammo icons — this game throws objects, not bullets."""
import math
from PIL import Image, ImageDraw
from gen_lib import font

ICO = "/mnt/user-data/outputs/lobby-ui-kit/hud_icons"
S, G, OUTPX = 8, 64, 128     # 64 design grid, exported at 128 for the label
W = 3.6
C = (255, 255, 255, 255)


def new():
    return Image.new("RGBA", (G * S, G * S), (0, 0, 0, 0))


def cap(d, p, w=W):
    r = w * S / 2
    d.ellipse([p[0] * S - r, p[1] * S - r, p[0] * S + r, p[1] * S + r], fill=C)


def path(d, pts, w=W, closed=False):
    p = [(x * S, y * S) for x, y in pts]
    d.line(p + ([p[0]] if closed else []), fill=C, width=int(w * S))
    for q in pts:
        cap(d, q, w)


def emit(name, fn):
    im = new()
    fn(ImageDraw.Draw(im))
    im.resize((OUTPX, OUTPX), Image.LANCZOS).save(f"{ICO}/ic_{name}.png")
    print("ok ic_" + name)


# ------------------------------------------- normal: khoi vuong (isometric)
def _cube(d):
    top, ru, rl = (32, 9), (53, 21), (53, 44)
    bot, ll, lu = (32, 56), (11, 44), (11, 21)
    ctr = (32, 32)
    path(d, [top, ru, rl, bot, ll, lu], closed=True)
    path(d, [lu, ctr], w=W - 0.5)
    path(d, [ru, ctr], w=W - 0.5)
    path(d, [ctr, bot], w=W - 0.5)


emit("ammo", _cube)


# ------------------------------------------------- spike: qua cau gai (solid)
def _spikeball(d):
    cx, cy, r_in, r_out, n = 32, 32, 15.5, 28.5, 9
    for i in range(n):
        a = 2 * math.pi * i / n - math.pi / 2
        half = math.pi / n * 0.62
        pts = [(cx + r_in * math.cos(a - half), cy + r_in * math.sin(a - half)),
               (cx + r_out * math.cos(a), cy + r_out * math.sin(a)),
               (cx + r_in * math.cos(a + half), cy + r_in * math.sin(a + half))]
        d.polygon([(x * S, y * S) for x, y in pts], fill=C)
    d.ellipse([(cx - r_in) * S, (cy - r_in) * S,
               (cx + r_in) * S, (cy + r_in) * S], fill=C)
    # hollow core so it reads as a ball, not a blob
    d.ellipse([(cx - 6.2) * S, (cy - 6.2) * S,
               (cx + 6.2) * S, (cy + 6.2) * S], fill=(0, 0, 0, 0))


emit("spike", _spikeball)


# ------------------------------------- heavy: khoi vuong ghi 1000 TON
def _weight(d):
    fx0, fy0, fx1, fy1 = 8, 25, 45, 57      # mat truoc
    dx, dy = 10, 9                           # do sau khoi
    d.polygon([((fx0) * S, fy0 * S), ((fx0 + dx) * S, (fy0 - dy) * S),
               ((fx1 + dx) * S, (fy0 - dy) * S), (fx1 * S, fy0 * S)], fill=C)
    d.polygon([(fx1 * S, fy0 * S), ((fx1 + dx) * S, (fy0 - dy) * S),
               ((fx1 + dx) * S, (fy1 - dy) * S), (fx1 * S, fy1 * S)], fill=C)
    d.rectangle([fx0 * S, fy0 * S, fx1 * S, fy1 * S], fill=C)

    hole = (0, 0, 0, 0)
    d.rectangle([(fx0 + 2.6) * S, (fy0 + 2.6) * S,
                 (fx1 - 2.6) * S, (fy1 - 2.6) * S], fill=hole)
    d.polygon([((fx0 + 2.4) * S, (fy0 - 1.4) * S),
               ((fx0 + dx - 0.6) * S, (fy0 - dy + 2.2) * S),
               ((fx1 + dx - 2.6) * S, (fy0 - dy + 2.2) * S),
               ((fx1 - 2.4) * S, (fy0 - 1.4) * S)], fill=hole)

    f1 = font("bold", int(12.4 * S))
    f2 = font("bold", int(7.2 * S))
    cx = (fx0 + fx1) / 2 * S
    d.text((cx, 38 * S), "1000", font=f1, fill=C, anchor="mm")
    d.text((cx, 49.6 * S), "TON", font=f2, fill=C, anchor="mm")


emit("ammo_heavy", _weight)
