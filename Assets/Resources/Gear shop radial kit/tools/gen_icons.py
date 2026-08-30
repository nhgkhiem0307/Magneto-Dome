"""Outline icons on a 64px grid, drawn white so Unity can tint them."""
import os, math
from PIL import Image, ImageDraw

OUT = "/mnt/user-data/outputs/lobby-ui-kit/icons"
os.makedirs(OUT, exist_ok=True)

S = 8            # supersample
G = 64           # final grid
W = 3.4          # stroke width on the 64 grid
C = (255, 255, 255, 255)


def new():
    return Image.new("RGBA", (G * S, G * S), (0, 0, 0, 0))


def cap(d, p, w=W):
    x, y = p[0] * S, p[1] * S
    r = w * S / 2
    d.ellipse([x - r, y - r, x + r, y + r], fill=C)


def path(d, pts, w=W, closed=False, caps=True):
    p = [(x * S, y * S) for x, y in pts]
    if closed:
        p = p + [p[0]]
    d.line(p, fill=C, width=int(w * S))
    for q in (pts if closed else pts):
        cap(d, q, w)


def arc(d, box, a0, a1, w=W, caps=True):
    b = [v * S for v in box]
    d.arc(b, a0, a1, fill=C, width=int(w * S))
    if caps:
        cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
        rx, ry = (box[2] - box[0]) / 2, (box[3] - box[1]) / 2
        for a in (a0, a1):
            cap(d, (cx + rx * math.cos(math.radians(a)),
                    cy + ry * math.sin(math.radians(a))), w)


def rrect(d, box, r, w=W, fill=None):
    b = [v * S for v in box]
    d.rounded_rectangle(b, radius=r * S, outline=None if fill else C,
                        width=int(w * S), fill=fill)


def poly(d, pts, fill=True):
    p = [(x * S, y * S) for x, y in pts]
    if fill:
        d.polygon(p, fill=C)
    else:
        path(d, pts, closed=True)


def emit(name, fn):
    img = new()
    fn(ImageDraw.Draw(img))
    img.resize((G, G), Image.LANCZOS).save(f"{OUT}/ic_{name}.png")


emit("back", lambda d: (path(d, [(48, 32), (18, 32)]),
                        path(d, [(30, 20), (18, 32), (30, 44)])))

emit("close", lambda d: (path(d, [(20, 20), (44, 44)]),
                         path(d, [(44, 20), (20, 44)])))

emit("check", lambda d: path(d, [(17, 33), (27, 43), (47, 21)]))

emit("plus", lambda d: (path(d, [(32, 18), (32, 46)]),
                        path(d, [(18, 32), (46, 32)])))

emit("chevron_right", lambda d: path(d, [(26, 17), (41, 32), (26, 47)]))

emit("play", lambda d: poly(d, [(25, 17), (49, 32), (25, 47)]))


def _copy(d):
    rrect(d, [13, 21, 39, 51], 5)
    path(d, [(23, 21), (23, 13), (51, 13), (51, 41), (43, 41)])


emit("copy", _copy)


def _refresh(d):
    arc(d, [15, 15, 49, 49], 8, 300, caps=False)
    poly(d, [(42, 33), (56, 33), (49, 20)])


emit("refresh", _refresh)


def _search(d):
    d.ellipse([13 * S, 13 * S, 43 * S, 43 * S], outline=C, width=int(W * S))
    path(d, [(40, 40), (51, 51)], w=W + 0.6)


emit("search", _search)


def _lock(d):
    rrect(d, [15, 29, 49, 52], 5)
    arc(d, [22, 15, 42, 36], 180, 360, caps=False)
    d.ellipse([30 * S, 37 * S, 34 * S, 41 * S], fill=C)


emit("lock", _lock)


def _users(d):
    d.ellipse([13 * S, 14 * S, 29 * S, 30 * S], outline=C, width=int(W * S))
    arc(d, [8, 33, 34, 59], 180, 360, caps=False)
    d.ellipse([37 * S, 17 * S, 49 * S, 29 * S], outline=C, width=int(W * S))
    arc(d, [33, 34, 53, 54], 200, 340, caps=False)


emit("users", _users)


def _logout(d):
    path(d, [(38, 14), (14, 14), (14, 50), (38, 50)])
    path(d, [(30, 32), (52, 32)])
    path(d, [(43, 23), (52, 32), (43, 41)])


emit("logout", _logout)


def _swap(d):
    path(d, [(14, 24), (46, 24)])
    path(d, [(37, 15), (46, 24), (37, 33)])
    path(d, [(50, 40), (18, 40)])
    path(d, [(27, 31), (18, 40), (27, 49)])


emit("swap", _swap)


def _crown(d):
    poly(d, [(13, 46), (17, 20), (28, 32), (32, 15), (36, 32), (47, 20),
             (51, 46)])
    rrect(d, [13, 46, 51, 52], 2, fill=C)


emit("crown", _crown)


def _warning(d):
    path(d, [(32, 13), (54, 50), (10, 50)], closed=True)
    path(d, [(32, 27), (32, 38)])
    cap(d, (32, 45), W + 0.4)


emit("warning", _warning)


def _globe(d):
    d.ellipse([13 * S, 13 * S, 51 * S, 51 * S], outline=C, width=int(W * S))
    d.ellipse([25 * S, 13 * S, 39 * S, 51 * S], outline=C, width=int(W * S))
    path(d, [(14, 25), (50, 25)], w=W - 0.4)
    path(d, [(14, 39), (50, 39)], w=W - 0.4)


emit("globe", _globe)

print("icons ->", OUT, len(os.listdir(OUT)))
