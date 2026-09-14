"""Single ally marker: inverted triangle, neon/glow treatment.

Glow is baked into the sprite rather than left to a Unity bloom pass — it
then survives any camera setup, and post-process bloom on a UI overlay
would light up every other HUD element too.
"""
import os, math
from PIL import Image, ImageDraw, ImageFilter, ImageChops

DIRS = ["/mnt/user-data/outputs/lobby-ui-kit/hud_sprites",
        "/mnt/user-data/outputs/lobby-ui-kit/hud_sprites_v2"]
for p in DIRS:
    os.makedirs(p, exist_ok=True)

S = 8                      # supersample
G = 96                     # design grid — bigger than 64, glow needs headroom
CORE = "#EAFFDF"
NEON = "#59FF40"
DEEP = "#2FD616"


def rgb(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def tri(cx, ty, w, h):
    """Inverted triangle — flat edge on top, apex pointing down."""
    return [(cx - w / 2, ty), (cx + w / 2, ty), (cx, ty + h)]


def rounded(pts, r, steps=9):
    n, out = len(pts), []
    for i in range(n):
        p0, p1, p2 = pts[(i - 1) % n], pts[i], pts[(i + 1) % n]
        v1 = (p0[0] - p1[0], p0[1] - p1[1])
        v2 = (p2[0] - p1[0], p2[1] - p1[1])
        l1 = math.hypot(*v1) or 1
        l2 = math.hypot(*v2) or 1
        a = (p1[0] + v1[0] / l1 * r, p1[1] + v1[1] / l1 * r)
        b = (p1[0] + v2[0] / l2 * r, p1[1] + v2[1] / l2 * r)
        out.append(a)
        for t in range(1, steps):
            f = t / steps
            out.append((
                (1 - f) ** 2 * a[0] + 2 * (1 - f) * f * p1[0] + f ** 2 * b[0],
                (1 - f) ** 2 * a[1] + 2 * (1 - f) * f * p1[1] + f ** 2 * b[1]))
        out.append(b)
    return out


def layer(pts, fill, blur=0, scale=1.0, cx=48, cy=44):
    im = Image.new("RGBA", (G * S, G * S), (0, 0, 0, 0))
    p = [((x - cx) * scale + cx, (y - cy) * scale + cy) for x, y in pts]
    ImageDraw.Draw(im).polygon([(x * S, y * S) for x, y in p], fill=fill)
    return im.filter(ImageFilter.GaussianBlur(blur * S)) if blur else im


def build():
    shape = rounded(tri(48, 26, 40, 40), 4.2)
    canvas = Image.new("RGBA", (G * S, G * S), (0, 0, 0, 0))

    # wide atmospheric halo — this sells "glowing" more than the core does
    canvas.alpha_composite(layer(shape, rgb(NEON, 46), blur=12.0, scale=1.72))
    canvas.alpha_composite(layer(shape, rgb(NEON, 96), blur=6.4, scale=1.40))
    canvas.alpha_composite(layer(shape, rgb(CORE, 130), blur=2.8, scale=1.16))
    # dark contact edge so it still holds against a bright sky
    canvas.alpha_composite(layer(shape, rgb("#03170F", 150), blur=0.9,
                                 scale=1.055))
    canvas.alpha_composite(layer(shape, rgb(DEEP)))

    m = layer(shape, (255, 255, 255, 255)).getchannel("A")

    # inner gradient, brighter toward the top edge
    grad = Image.new("L", (1, G * S))
    gp = grad.load()
    for y in range(G * S):
        f = min(max((y / S - 26) / 40, 0), 1)
        gp[0, y] = int(255 * (1 - f) ** 0.85)
    grad = grad.resize((G * S, G * S), Image.BILINEAR)
    tint = Image.new("RGBA", (G * S, G * S), rgb(NEON))
    tint.putalpha(ImageChops.multiply(m, grad))
    canvas.alpha_composite(tint)

    # hot rim along the top edge — the neon-tube read
    rim = Image.new("RGBA", (G * S, G * S), (0, 0, 0, 0))
    rd = ImageDraw.Draw(rim)
    rd.line([(31 * S, 27.4 * S), (65 * S, 27.4 * S)], fill=rgb(CORE),
            width=int(3.4 * S))
    for x in (29.8, 62.6):
        rd.ellipse([x * S, 25.7 * S, (x + 3.4) * S, 29.1 * S], fill=rgb(CORE))
    # faint core down the axis — reads as a lit tube, not a flat fill
    rd.polygon([(45.4 * S, 29 * S), (50.6 * S, 29 * S), (48 * S, 58 * S)],
               fill=rgb(CORE, 118))
    rim = rim.filter(ImageFilter.GaussianBlur(1.05 * S))
    rim.putalpha(ImageChops.multiply(rim.getchannel("A"), m))
    canvas.alpha_composite(rim)

    return canvas.resize((G, G), Image.LANCZOS)


mk = build()
for p in DIRS:
    mk.save(f"{p}/ally_marker.png")
    for old in ("ally_marker_occluded", "ally_marker_hp", "ally_offscreen",
                "ally_hp_fill"):
        f = f"{p}/{old}.png"
        if os.path.exists(f):
            os.remove(f)
print("ok ally_marker 96x96")
