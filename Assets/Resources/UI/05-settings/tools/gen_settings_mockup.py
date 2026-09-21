import sys
sys.path.insert(0, "/home/claude/kit")
from PIL import Image, ImageDraw, ImageFilter
from gen_lib import font, mono, text

ROOT = "/mnt/user-data/outputs/lobby-ui-kit"
SP, RS = f"{ROOT}/settings_sprites", f"{ROOT}/radial_sprites"
OUT = f"{ROOT}/mockups_v2"

CY, CY_HI = "#2BE8FF", "#B8FBFF"
TXT, DIM, FAINT, RED = "#EDEFF4", "#7F8CA0", "#4E5A6B", "#FF6B72"


def rgb(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def ot(d, xy, s, f, fill, anchor="la", sw=3):
    d.text(xy, s, font=f, fill=rgb(fill), anchor=anchor,
           stroke_width=sw, stroke_fill=(3, 7, 12, 215))


_c = {}


def spr(n, folder=SP):
    k = folder + n
    if k not in _c:
        _c[k] = Image.open(f"{folder}/{n}.png").convert("RGBA")
    return _c[k]


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


def hbar(n, w, h, b=8):
    src = spr(n)
    sw, sh = src.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out.alpha_composite(src.crop((0, 0, b, sh)).resize((b, h),
                                                       Image.BILINEAR), (0, 0))
    out.alpha_composite(src.crop((sw - b, 0, sw, sh)).resize(
        (b, h), Image.BILINEAR), (w - b, 0))
    out.alpha_composite(src.crop((b, 0, sw - b, sh)).resize(
        (w - 2 * b, h), Image.BILINEAR), (b, 0))
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
    for rx, ry, rw in [(420, 760, 92), (1180, 700, 66), (1640, 850, 112)]:
        d.ellipse([rx - rw, ry - rw * .5, rx + rw, ry + rw * .5],
                  fill=(80, 84, 78))
    return c.filter(ImageFilter.GaussianBlur(1.6))


def bracket(d, x, y, w, h, col, ln=30, bw=2):
    for sx, sy, ax, ay in ((0, 0, 1, 1), (w, 0, -1, 1), (0, h, 1, -1),
                           (w, h, -1, -1)):
        d.line([(x + sx, y + sy), (x + sx + ax * ln, y + sy)], fill=rgb(col),
               width=bw)
        d.line([(x + sx, y + sy), (x + sx, y + sy + ay * ln)], fill=rgb(col),
               width=bw)


SLIDERS = [("Âm lượng tổng", "MASTER", 0.78),
           ("Nhạc", "MUSIC", 0.45),
           ("Hiệu ứng", "SFX", 0.62),
           ("Độ nhạy chuột", "SENS", 0.34)]


def slider_row(c, d, x, y, w, label, code, val, hover=False):
    H = 84
    c.alpha_composite(nine(spr("row_hover" if hover else "row"), w, H, 26),
                      (x, y))
    ot(d, (x + 30, y + 22), code, mono(13), CY if hover else DIM, sw=2)
    ot(d, (x + 30, y + 42), label, font("medium", 24), TXT, sw=2)

    tw = 420
    tx = x + w - tw - 118
    ty = y + H // 2 - 7
    c.alpha_composite(hbar("slider_track", tw, 14), (tx, ty))
    fw = max(int(tw * val), 14)
    c.alpha_composite(hbar("slider_fill", fw, 14), (tx, ty))
    hx = tx + fw - 28
    c.alpha_composite(spr("slider_handle"), (hx, ty - 21))
    ot(d, (x + w - 34, y + H // 2), f"{int(val * 100)}", mono(28), CY_HI,
       "rm", sw=3)


def settings(hover_row=0, confirm=False):
    c = scene()
    c.alpha_composite(Image.open(f"{RS}/screen_scrim.png").convert(
        "RGBA").resize(c.size, Image.LANCZOS))
    d = ImageDraw.Draw(c)

    PW, PH = 1180, 800
    px, py = (1920 - PW) // 2, 140
    c.alpha_composite(nine(spr("panel"), PW, PH, 44), (px, py))

    ot(d, (960, py + 54), "CÀI ĐẶT", font("bold", 46), TXT, "mm")
    ot(d, (960, py + 92), "SYS // CONFIG", mono(14), CY, "mm", sw=2)
    d.rectangle([932, py + 114, 988, py + 117], fill=rgb(CY))

    rx, rw = px + 46, PW - 92
    ry = py + 150
    for i, (label, code, val) in enumerate(SLIDERS):
        slider_row(c, d, rx, ry, rw, label, code, val, hover=(i == hover_row))
        ry += 96

    # dropdown row
    c.alpha_composite(nine(spr("row"), rw, 84, 26), (rx, ry))
    ot(d, (rx + 30, ry + 22), "RES", mono(13), DIM, sw=2)
    ot(d, (rx + 30, ry + 42), "Độ phân giải", font("medium", 24), TXT, sw=2)
    ddw = 320
    ddx = rx + rw - ddw - 34
    c.alpha_composite(nine(spr("dd_normal"), ddw, 56, 26), (ddx, ry + 14))
    ot(d, (ddx + 26, ry + 42), "1920 × 1080", mono(23), TXT, sw=2)
    c.alpha_composite(spr("dd_arrow"), (ddx + ddw - 48, ry + 22))
    ry += 96

    # toggle row
    c.alpha_composite(nine(spr("row"), rw, 84, 26), (rx, ry))
    ot(d, (rx + 30, ry + 22), "FULL", mono(13), DIM, sw=2)
    ot(d, (rx + 30, ry + 42), "Toàn màn hình", font("medium", 24), TXT, sw=2)
    tgx = rx + rw - 86
    c.alpha_composite(spr("toggle_on"), (tgx, ry + 16))
    c.alpha_composite(spr("toggle_check"), (tgx + 6, ry + 22))
    ry += 112

    # buttons
    bw2, bh2 = 300, 72
    c.alpha_composite(nine(spr("btn_danger_normal"), bw2 + 20, bh2 + 20, 36),
                      (px + 46 - 10, ry - 10))
    ot(d, (px + 46 + bw2 / 2, ry + bh2 / 2), "RỜI PHÒNG", font("bold", 25),
       RED, "mm")
    c.alpha_composite(nine(spr("btn_hover"), bw2 + 20, bh2 + 20, 36),
                      (px + PW - 46 - bw2 - 10, ry - 10))
    ot(d, (px + PW - 46 - bw2 / 2, ry + bh2 / 2), "ĐÓNG", font("bold", 25),
       CY, "mm")

    bracket(d, 48, 48, 1824, 984, CY)
    ot(d, (72, 66), "MAGNETO-DOME // SETTINGS", mono(15), CY, sw=2)
    ot(d, (1848, 66), "v0.4.1", mono(15), DIM, "ra", sw=2)

    if confirm:
        c.alpha_composite(Image.open(f"{RS}/screen_scrim.png").convert(
            "RGBA").resize(c.size, Image.LANCZOS))
        d = ImageDraw.Draw(c)
        CW, CH = 660, 340
        cxp, cyp = (1920 - CW) // 2, (1080 - CH) // 2
        c.alpha_composite(nine(spr("panel"), CW, CH, 44), (cxp, cyp))
        ot(d, (960, cyp + 62), "RỜI PHÒNG?", font("bold", 40), TXT, "mm")
        ot(d, (960, cyp + 104), "WARN // LEAVE MATCH", mono(14), RED, "mm",
           sw=2)
        ot(d, (960, cyp + 158),
           "Bạn sẽ mất toàn bộ vật phẩm và tiền của vòng này.",
           font("regular", 21), DIM, "mm", sw=2)
        ot(d, (960, cyp + 196), "Tự đóng sau 8s", mono(19), FAINT, "mm", sw=2)

        bw3 = 250
        c.alpha_composite(nine(spr("btn_normal"), bw3 + 20, 84, 36),
                          (cxp + 46 - 10, cyp + CH - 106))
        ot(d, (cxp + 46 + bw3 / 2, cyp + CH - 74), "Ở LẠI", font("bold", 24),
           TXT, "mm")
        c.alpha_composite(nine(spr("btn_danger_hover"), bw3 + 20, 84, 36),
                          (cxp + CW - 46 - bw3 - 10, cyp + CH - 106))
        ot(d, (cxp + CW - 46 - bw3 / 2, cyp + CH - 74), "RỜI ĐI",
           font("bold", 24), RED, "mm")
    return c


if __name__ == "__main__":
    settings().convert("RGB").save(f"{OUT}/12_settings.png", quality=94)
    print("ok settings")
    settings(confirm=True).convert("RGB").save(
        f"{OUT}/13_leave_confirm.png", quality=94)
    print("ok confirm")
