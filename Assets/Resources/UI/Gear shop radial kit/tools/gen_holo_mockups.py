import os, math, sys
sys.path.insert(0, "/home/claude/kit")
from PIL import Image, ImageDraw, ImageFilter
from gen_lib import font, mono, text

ROOT = "/mnt/user-data/outputs/lobby-ui-kit"
RS, SH = f"{ROOT}/radial_sprites", f"{ROOT}/shop_sprites"
II, HI = f"{ROOT}/item_icons", f"{ROOT}/hud_icons"
OUT = f"{ROOT}/mockups_v2"

CY, CY_HI = "#2BE8FF", "#B8FBFF"
GOLD, TXT, DIM, FAINT, RED = "#FFC24B", "#EDEFF4", "#7F8CA0", "#4E5A6B", "#FF6B72"


def rgb(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def ico(path, size, col, a=255):
    im = Image.open(path).convert("RGBA").resize((size, size), Image.LANCZOS)
    t = Image.new("RGBA", im.size, rgb(col))
    al = im.getchannel("A")
    if a < 255:
        al = al.point(lambda v: v * a // 255)
    t.putalpha(al)
    return t


def ot(d, xy, s, f, fill, anchor="la", sw=3):
    d.text(xy, s, font=f, fill=rgb(fill), anchor=anchor,
           stroke_width=sw, stroke_fill=(3, 7, 12, 210))


def nine(src, w, h, b):
    sw, sh = src.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))

    def put(c, x, y, tw, th):
        if tw > 0 and th > 0:
            out.alpha_composite(c.resize((tw, th), Image.BILINEAR), (x, y))
    mw, mh = w - 2 * b, h - 2 * b
    put(src.crop((0, 0, b, b)), 0, 0, b, b)
    put(src.crop((sw - b, 0, sw, b)), w - b, 0, b, b)
    put(src.crop((0, sh - b, b, sh)), 0, h - b, b, b)
    put(src.crop((sw - b, sh - b, sw, sh)), w - b, h - b, b, b)
    put(src.crop((b, 0, sw - b, b)), b, 0, mw, b)
    put(src.crop((b, sh - b, sw - b, sh)), b, h - b, mw, b)
    put(src.crop((0, b, b, sh - b)), 0, b, b, mh)
    put(src.crop((sw - b, b, sw, sh - b)), w - b, b, b, mh)
    put(src.crop((b, b, sw - b, sh - b)), b, b, mw, mh)
    return out


def scene(W=1920, H=1080):
    hz = 520
    c = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(c)
    for y in range(H):
        if y < hz:
            f = y / hz
            d.line([(0, y), (W, y)], fill=(int(140 + 46 * f), int(198 + 30 * f),
                                           int(236 + 14 * f)))
        else:
            f = (y - hz) / (H - hz)
            d.line([(0, y), (W, y)], fill=(int(126 - 52 * f), int(166 - 58 * f),
                                           int(68 - 28 * f)))
    for x0, pk, x1, h, col in [(-90, 220, 430, 250, (112, 136, 154)),
                               (320, 570, 900, 200, (94, 118, 138)),
                               (800, 1120, 1470, 268, (124, 146, 162)),
                               (1320, 1600, 1990, 214, (90, 114, 134))]:
        d.polygon([(x0, hz), (pk, hz - h), (x1, hz)], fill=col)
        d.polygon([(pk, hz - h), (pk + (x1 - pk) * .44, hz - h * .4), (x1, hz)],
                  fill=tuple(int(v * .84) for v in col))
    for cx, sc in [(240, 1.0), (600, .72), (1020, 1.25), (1450, .9), (1760, .64)]:
        b, tw, th = int(60 * sc), int(86 * sc), int(112 * sc)
        base = hz + int(190 * sc)
        d.rectangle([cx - 8 * sc, base - b, cx + 8 * sc, base], fill=(94, 68, 44))
        d.polygon([(cx, base - b - th), (cx + tw / 2, base - b + 9),
                   (cx - tw / 2, base - b + 9)], fill=(64, 132, 58))
        d.polygon([(cx, base - b - th), (cx + tw / 2, base - b + 9),
                   (cx, base - b + 9)], fill=(50, 110, 46))
    for rx, ry, rw in [(420, 760, 92), (1180, 700, 66), (1640, 850, 112)]:
        d.ellipse([rx - rw, ry - rw * .5, rx + rw, ry + rw * .5],
                  fill=(80, 84, 78))
    return c.filter(ImageFilter.GaussianBlur(1.6))


def scrim(c):
    c.alpha_composite(Image.open(f"{RS}/screen_scrim.png").convert(
        "RGBA").resize(c.size, Image.LANCZOS))


def bracket(d, x, y, w, h, col, ln=26, bw=2):
    for sx, sy, ax, ay in ((0, 0, 1, 1), (w, 0, -1, 1), (0, h, 1, -1),
                           (w, h, -1, -1)):
        d.line([(x + sx, y + sy), (x + sx + ax * ln, y + sy)], fill=rgb(col),
               width=bw)
        d.line([(x + sx, y + sy), (x + sx, y + sy + ay * ln)], fill=rgb(col),
               width=bw)


RADIAL = [("energy",   "NƯỚC TĂNG LỰC", "STIM",  2, (0, -318)),
          ("bandage",  "BĂNG GẠC",      "MED",   3, (330, 0)),
          ("gasoline", "CAN XĂNG",      "FUEL",  0, (0, 318)),
          ("em_core",  "LÕI RÀO EM",    "EMB",   1, (-330, 0))]


def radial(sel=1):
    c = scene()
    scrim(c)
    d = ImageDraw.Draw(c)
    cx, cy = 960, 516

    R = 820
    ring = Image.open(f"{RS}/reticle_ring.png").convert("RGBA").resize(
        (R, R), Image.LANCZOS)
    c.alpha_composite(ring, (cx - R // 2, cy - R // 2))

    ang = [0, -90, 180, 90][sel]
    p = Image.open(f"{RS}/select_pointer.png").convert("RGBA").resize(
        (390, 390), Image.LANCZOS).rotate(ang, resample=Image.BICUBIC)
    c.alpha_composite(p, (cx - 195, cy - 195))

    hub = Image.open(f"{RS}/hub.png").convert("RGBA").resize((286, 286),
                                                             Image.LANCZOS)
    c.alpha_composite(hub, (cx - 143, cy - 143))
    key, label, code, cnt, _ = RADIAL[sel]
    ot(d, (cx, cy - 44), code, mono(17), CY, "mm", sw=2)
    fs = 30
    while d.textlength(label, font=font("bold", fs)) > 216 and fs > 17:
        fs -= 1
    ot(d, (cx, cy - 6), label, font("bold", fs), TXT, "mm")
    ot(d, (cx, cy + 38), f"x{cnt}", mono(30), CY_HI, "mm")

    for i, (key, label, code, cnt, (ox, oy)) in enumerate(RADIAL):
        state = ("slot_hover" if i == sel
                 else "slot_disabled" if cnt == 0 else "slot_normal")
        S = 214
        s = Image.open(f"{RS}/{state}.png").convert("RGBA").resize(
            (S, S), Image.LANCZOS)
        x, y = cx + ox, cy + oy
        c.alpha_composite(s, (x - S // 2, y - S // 2))

        on = cnt > 0
        col = CY if i == sel else (TXT if on else FAINT)
        isz = 78 if i == sel else 70
        c.alpha_composite(ico(f"{II}/item_{key}.png", isz, col),
                          (x - isz // 2, y - isz // 2 - 14))
        ot(d, (x, y + 44), f"x{cnt}", mono(27), col, "mm")
        ot(d, (x, y - 52), code, mono(14), CY if i == sel else DIM, "mm", sw=2)
        ot(d, (x, y + S // 2 + 26), label, font("medium", 19),
           TXT if on else FAINT, "mm")

    # telemetry corners — detail at the edges, centre stays clear
    ot(d, (72, 66), "MAGNETO-DOME // INVENTORY BUS", mono(15), CY, sw=2)
    ot(d, (72, 92), "SLOT 04 / 04  ·  LINK OK", mono(15), DIM, sw=2)
    ot(d, (1848, 66), "RND 09", mono(15), DIM, "ra", sw=2)
    ot(d, (1848, 92), "T-01:32", mono(15), CY, "ra", sw=2)
    bracket(d, 48, 48, 1824, 984, CY + "", ln=30, bw=2)

    ot(d, (cx, 1044), "GIỮ  Q   ·   DI CHUỘT ĐỂ CHỌN   ·   THẢ ĐỂ DÙNG",
       mono(17), DIM, "mm", sw=3)
    return c


SHOP = [("armor", "NÂNG GIÁP", "ARM", 150, 2),
        ("energy", "NƯỚC TĂNG LỰC", "STIM", 100, 1),
        ("bandage", "BĂNG GẠC", "MED", 100, 3),
        ("gasoline", "CAN XĂNG", "FUEL", 150, 0),
        ("em_core", "LÕI RÀO EM", "EMB", 200, 1)]
MONEY = 500


def shop(hover=2):
    c = scene()
    scrim(c)
    d = ImageDraw.Draw(c)

    PW, PH = 1620, 660
    px, py = (1920 - PW) // 2, 216
    c.alpha_composite(nine(Image.open(f"{SH}/shop_panel.png").convert("RGBA"),
                           PW, PH, 44), (px, py))

    ot(d, (960, py + 52), "GEAR SHOP", font("bold", 50), TXT, "mm")
    ot(d, (960, py + 92), "PHA MUA SẮM  ·  MAGNETO-DOME", mono(15), CY, "mm",
       sw=2)
    d.rectangle([926, py + 116, 994, py + 119], fill=rgb(CY))

    pill = Image.open(f"{SH}/pill.png").convert("RGBA")
    c.alpha_composite(nine(pill, 246, 70, 38), (px + 46, py + 34))
    ot(d, (px + 76, py + 60), "CREDIT", mono(13), DIM, sw=2)
    ot(d, (px + 76, py + 78), f"${MONEY}", mono(30), GOLD, sw=3)

    c.alpha_composite(nine(pill, 246, 70, 38), (px + PW - 292, py + 34))
    ot(d, (px + PW - 262, py + 60), "TIMER", mono(13), DIM, sw=2)
    c.alpha_composite(ico(f"{HI}/ic_timer.png", 24, CY),
                      (px + PW - 262, py + 78))
    ot(d, (px + PW - 230, py + 80), "còn 12s", font("medium", 23), TXT, sw=3)

    n, bw, bh, gap = len(SHOP), 276, 386, 26
    total = n * bw + (n - 1) * gap
    bx0 = px + (PW - total) // 2
    by = py + 186

    for i, (key, label, code, price, owned) in enumerate(SHOP):
        x = bx0 + i * (bw + gap)
        afford = price <= MONEY
        state = ("btn_hover" if i == hover
                 else "btn_disabled" if not afford else "btn_normal")
        c.alpha_composite(nine(Image.open(f"{SH}/{state}.png").convert("RGBA"),
                               bw, bh, 40), (x, by))

        col = TXT if afford else FAINT
        ot(d, (x + 34, by + 32), code, mono(14), CY if i == hover else DIM,
           sw=2)
        ot(d, (x + bw - 34, by + 32), f"x{owned}", mono(14), DIM, "ra", sw=2)

        c.alpha_composite(ico(f"{II}/item_{key}.png", 112,
                              CY if i == hover else col),
                          (x + bw // 2 - 56, by + 66))
        d.line([(x + 40, by + 214), (x + bw - 40, by + 214)],
               fill=rgb(CY, 70) if i == hover else (255, 255, 255, 34),
               width=1)
        ot(d, (x + bw / 2, by + 246), label, font("medium", 22), col, "mm",
           sw=2)
        ot(d, (x + bw / 2, by + 296), f"${price}", mono(38),
           GOLD if afford else RED, "mm")

        k = 46
        kb = Image.new("RGBA", (k, 38), (0, 0, 0, 0))
        kd = ImageDraw.Draw(kb)
        kd.polygon([(6, 0), (k - 1, 0), (k - 1, 31), (k - 7, 37), (0, 37),
                    (0, 6)], fill=rgb(CY) if i == hover
                    else (255, 255, 255, 26),
                    outline=None if i == hover else (255, 255, 255, 52),
                    width=1)
        c.alpha_composite(kb, (x + bw // 2 - k // 2, by + 328))
        ot(d, (x + bw / 2, by + 347), str(i + 1), mono(21),
           "#03141A" if i == hover else TXT, "mm", sw=0)

    bracket(d, 48, 48, 1824, 984, CY, ln=30, bw=2)
    ot(d, (72, 66), "MAGNETO-DOME // GEAR BUS", mono(15), CY, sw=2)
    ot(d, (1848, 66), "RND 10", mono(15), DIM, "ra", sw=2)
    ot(d, (960, py + PH + 46),
       "BẤM 1–5 HOẶC CLICK ĐỂ MUA   ·   ESC ĐỂ ĐÓNG", mono(17), DIM, "mm",
       sw=3)
    return c


if __name__ == "__main__":
    radial().convert("RGB").save(f"{OUT}/10_radial_menu.png", quality=94)
    print("ok radial")
    shop().convert("RGB").save(f"{OUT}/11_shop.png", quality=94)
    print("ok shop")
