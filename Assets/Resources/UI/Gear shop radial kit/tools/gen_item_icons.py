"""Item icons for the radial menu and shop. Outline style, white, 128px
(bigger than the HUD icons: these are displayed large in both surfaces)."""
import os, math
from PIL import Image, ImageDraw

OUT = "/mnt/user-data/outputs/lobby-ui-kit/item_icons"
os.makedirs(OUT, exist_ok=True)

S, G, EXPORT = 8, 64, 128
W = 3.2
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


def poly(d, pts, fill=C):
    d.polygon([(x * S, y * S) for x, y in pts], fill=fill)


def emit(name, fn):
    im = new()
    fn(ImageDraw.Draw(im))
    im.resize((EXPORT, EXPORT), Image.LANCZOS).save(f"{OUT}/item_{name}.png")
    print("ok item_" + name)


# ------------------------------------------------------ armor / giap (shield)
def _armor(d):
    body = [(32, 8), (54, 17), (54, 34), (32, 56), (10, 34), (10, 17)]
    path(d, body, closed=True)
    # up arrow inside — this is an armor UPGRADE, not a generic shield
    path(d, [(32, 44), (32, 21)], w=W)
    path(d, [(23, 30), (32, 21), (41, 30)], w=W)


emit("armor", _armor)


# ------------------------------------------------- energy drink (can, tall)
def _energy(d):
    d.rounded_rectangle([21 * S, 11 * S, 43 * S, 55 * S], radius=6 * S,
                        outline=C, width=int(W * S))
    path(d, [(22, 21), (42, 21)], w=W - 0.8)
    # lightning bolt on the body
    poly(d, [(34, 26), (27, 39), (31.5, 39), (29.5, 50), (37, 36),
             (32.5, 36)])


emit("energy", _energy)


# --------------------------------------------------------- bandage (roll)
def _bandage(d):
    # cross on a rounded square — a first-aid read is instant at 48px,
    # a coiled roll is not
    d.rounded_rectangle([11 * S, 11 * S, 53 * S, 53 * S], radius=9 * S,
                        outline=C, width=int(W * S))
    d.rounded_rectangle([28 * S, 19 * S, 36 * S, 45 * S], radius=2.5 * S,
                        fill=C)
    d.rounded_rectangle([19 * S, 28 * S, 45 * S, 36 * S], radius=2.5 * S,
                        fill=C)


emit("bandage", _bandage)


# ----------------------------------------------------- gasoline canister
def _gasoline(d):
    d.rounded_rectangle([13 * S, 20 * S, 45 * S, 56 * S], radius=4 * S,
                        outline=C, width=int(W * S))
    # handle: a closed loop sitting on the shoulder
    d.rounded_rectangle([19 * S, 9 * S, 39 * S, 21 * S], radius=4 * S,
                        outline=C, width=int((W - 0.6) * S))
    # spout on the right shoulder
    path(d, [(45, 27), (52, 24), (52, 16)], w=W - 0.6)
    # X brace on the face
    path(d, [(20, 29), (38, 47)], w=W - 1.2)
    path(d, [(38, 29), (20, 47)], w=W - 1.2)


emit("gasoline", _gasoline)


# ------------------------------------------------------- EM barrier core
def _em_core(d):
    # dome
    d.arc([9 * S, 16 * S, 55 * S, 62 * S], 180, 360, fill=C, width=int(W * S))
    path(d, [(9, 39), (55, 39)], w=W)
    # inner dome line — layered shield read
    d.arc([17 * S, 24 * S, 47 * S, 54 * S], 180, 360, fill=C,
          width=int((W - 1.0) * S))
    # bolt in the core
    poly(d, [(34, 20), (27, 33), (31.5, 33), (29.5, 42), (37, 29), (32.5, 29)])
    # base
    d.rounded_rectangle([12 * S, 41 * S, 52 * S, 49 * S], radius=3 * S,
                        outline=C, width=int((W - 0.6) * S))


emit("em_core", _em_core)
