"""Render each lobby screen at 1920x1080 using the real sprites, so the
mockup shows exactly what Unity will draw with Image(type=Sliced)."""
import os
from PIL import Image, ImageDraw, ImageFilter
from gen_lib import P, rgb, nineslice, font, mono, text

V = os.environ.get("KIT_V", "")
SPR = "/mnt/user-data/outputs/lobby-ui-kit/sprites" + V
ICO = "/mnt/user-data/outputs/lobby-ui-kit/icons"
OUT = "/mnt/user-data/outputs/lobby-ui-kit/mockups" + V
os.makedirs(OUT, exist_ok=True)
MODERN = bool(V)
os.makedirs(OUT, exist_ok=True)

# content padding baked into each sprite for its soft shadow
PAD = {"panel_main": 26, "card": 14, "well": 0, "input_normal": 0,
       "input_focus": 6, "btn_ghost_normal": 0, "btn_ghost_hover": 8,
       "slot_red": 14, "slot_blue": 14, "slot_empty": 14,
       "row_normal": 0, "row_hover": 0, "row_selected": 0,
       "toast_error": 14, "pill": 0, "pill_danger": 0,
       "scroll_track": 0, "scroll_handle": 0, "divider": 0}
BORDER = {"panel_main": 48, "card": 32, "well": 18, "input_normal": 14,
          "input_focus": 20, "btn_ghost_normal": 16, "btn_ghost_hover": 24,
          "slot_red": 32, "slot_blue": 32, "slot_empty": 32,
          "row_normal": 16, "row_hover": 16, "row_selected": 16,
          "toast_error": 32, "pill": 26, "pill_danger": 26,
          "scroll_track": 8, "scroll_handle": 8, "divider": 2}
for k in ("primary", "secondary", "danger"):
    for st in ("normal", "hover", "pressed", "disabled"):
        n = f"btn_{k}_{st}"
        PAD[n], BORDER[n] = 12, 28

_cache = {}


def spr(name):
    if name not in _cache:
        _cache[name] = Image.open(f"{SPR}/{name}.png").convert("RGBA")
    return _cache[name]


def icon(name, size, color):
    im = Image.open(f"{ICO}/ic_{name}.png").convert("RGBA")
    im = im.resize((size, size), Image.LANCZOS)
    tint = Image.new("RGBA", im.size, rgb(color))
    tint.putalpha(im.getchannel("A"))
    return tint


def box(c, name, x, y, w, h):
    """Place a sprite by its CONTENT rect; shadow padding bleeds outside."""
    p = PAD.get(name, 0)
    b = BORDER.get(name, 16)
    s = nineslice(spr(name), w + p * 2, h + p * 2, (b, b, b, b))
    c.alpha_composite(s, (int(x - p), int(y - p)))


def bg(w=1920, h=1080):
    c = Image.new("RGBA", (w, h), rgb(P["bg_deep"]))
    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(glow)
    d.ellipse([w * 0.5 - 780, -420, w * 0.5 + 780, 620],
              fill=(38, 46, 70, 150))
    d.ellipse([w * 0.5 - 300, h - 340, w * 0.5 + 300, h + 300],
              fill=(46, 36, 20, 60))
    c.alpha_composite(glow.filter(ImageFilter.GaussianBlur(190)))
    return c


def btn(c, x, y, w, h, label, kind="secondary", state="normal",
        ic=None, fsize=26):
    name = f"btn_{kind}_{state}" if kind != "ghost" else f"btn_ghost_{state}"
    box(c, name, x, y, w, h)
    d = ImageDraw.Draw(c)
    col = {"primary": P["on_accent"], "danger": "#FFFFFF",
           "secondary": P["text"], "ghost": P["text_dim"]}[kind]
    if state == "disabled":
        col = P["disabled_txt"]
    f = font("bold" if kind == "primary" else "medium", fsize)
    tw = d.textlength(label, font=f)
    ix = 0
    if ic:
        ix = int(fsize * 1.25)
        c.alpha_composite(icon(ic, ix, col),
                          (int(x + w / 2 - (tw + ix + 12) / 2),
                           int(y + h / 2 - ix / 2)))
    d.text((x + w / 2 + (ix + 12) / 2 if ic else x + w / 2, y + h / 2 + 1),
           label, font=f, fill=rgb(col), anchor="mm")


def icon_btn(c, x, y, s, ic, state="normal"):
    box(c, f"btn_secondary_{state}", x, y, s, s)
    c.alpha_composite(icon(ic, int(s * 0.46), P["text_dim"]),
                      (int(x + s * 0.27), int(y + s * 0.27)))


def eyebrow(d, xy, s, anchor="lm"):
    f = mono(14) if MODERN else font("medium", 15)
    text(d, xy, s.upper(), f, rgb(P["text_faint"]), anchor=anchor,
         spacing=3.2 if MODERN else 2.6)


def rule(c, x, y, wd=44):
    """v2 signature: short accent bar under a title."""
    if MODERN:
        ImageDraw.Draw(c).rectangle([x, y, x + wd, y + 3], rgb(P["accent"]))


# ============================================================ 01 name entry
def screen_name():
    c = bg()
    d = ImageDraw.Draw(c)
    PW, PH = 760, 452
    px, py = (1920 - PW) // 2, (1080 - PH) // 2
    box(c, "panel_main", px, py, PW, PH)

    eyebrow(d, (px + 56, py + 62), "bước 1 / 2")
    text(d, (px + 56, py + 88), "Bạn tên là gì?", font("bold", 46),
         rgb(P["text"]))
    rule(c, px + 56, py + 152 - 14)
    text(d, (px + 56, py + 152),
         "Tên này hiển thị với người chơi khác trong phòng.",
         font("regular", 21), rgb(P["text_dim"]))

    box(c, "input_focus", px + 56, py + 210, PW - 112, 68)
    text(d, (px + 82, py + 245), "Player_2947", font("regular", 25),
         rgb(P["text"]), anchor="lm")
    d.rectangle([px + 82 + 148, py + 226, px + 82 + 150, py + 264],
                fill=rgb(P["accent"]))
    text(d, (px + PW - 82, py + 245), "11 / 16", font("regular", 18),
         rgb(P["text_faint"]), anchor="rm")

    btn(c, px + 56, py + 308, PW - 112, 74, "Tiếp tục", "primary",
        ic="chevron_right", fsize=27)
    return c


# ============================================================= 02 main menu
def screen_main():
    c = bg()
    d = ImageDraw.Draw(c)

    text(d, (960, 118), "ARENA", font("bold", 74), rgb(P["text"]),
         anchor="mm")
    if MODERN:
        text(d, (960, 168), "MULTIPLAYER LOBBY", mono(16),
             rgb(P["text_faint"]), anchor="mm", spacing=6)
        ImageDraw.Draw(c).rectangle([938, 196, 982, 199], rgb(P["accent"]))
    else:
        text(d, (960, 176), "M U L T I P L A Y E R   L O B B Y",
             font("light", 18), rgb(P["text_faint"]), anchor="mm")

    box(c, "pill", 1560, 56, 300, 60)
    c.alpha_composite(icon("crown", 26, P["accent"]), (1584, 73))
    text(d, (1622, 87), "Player_2947", font("medium", 22), rgb(P["text"]),
         anchor="lm")

    PW, PH = 680, 620
    px, py = (1920 - PW) // 2, 252
    box(c, "panel_main", px, py, PW, PH)

    bx, bw = px + 56, PW - 112
    btn(c, bx, py + 52, bw, 78, "Tìm trận nhanh", "primary", ic="play",
        fsize=28)
    btn(c, bx, py + 148, bw, 66, "Tạo phòng", "secondary", ic="plus")
    btn(c, bx, py + 230, bw, 66, "Danh sách phòng", "secondary", ic="globe")

    box(c, "divider", bx, py + 330, bw, 2)
    eyebrow(d, (px + PW / 2, py + 366), "hoặc vào bằng mã phòng", anchor="mm")

    box(c, "input_normal", bx, py + 396, bw - 190, 66)
    text(d, (bx + 26, py + 430), "NHẬP MÃ", mono(24), rgb(P["text_faint"]),
         anchor="lm")
    btn(c, bx + bw - 172, py + 396, 172, 66, "Vào", "secondary",
        state="hover")

    box(c, "pill", 830, 962, 260, 52)
    d.ellipse([856, 982, 868, 994], fill=rgb(P["success"]))
    text(d, (880, 989), "1.284 người online", font("regular", 19),
         rgb(P["text_dim"]), anchor="lm")
    return c


# =========================================================== 03 create room
def screen_create():
    c = bg()
    d = ImageDraw.Draw(c)
    PW, PH = 760, 600
    px, py = (1920 - PW) // 2, (1080 - PH) // 2
    box(c, "panel_main", px, py, PW, PH)

    icon_btn(c, px + 40, py + 40, 56, "back")
    text(d, (px + PW / 2, py + 68), "Tạo phòng mới", font("bold", 36),
         rgb(P["text"]), anchor="mm")
    rule(c, px + PW / 2 - 22, py + 96)

    eyebrow(d, (px + 56, py + 148), "mã phòng của bạn")
    box(c, "well", px + 56, py + 172, PW - 112, 118)
    text(d, (px + PW / 2 - 26, py + 231), "K7 M2 Q4", mono(58),
         rgb(P["accent"]), anchor="mm")
    c.alpha_composite(icon("copy", 32, P["text_dim"]),
                      (px + PW - 116, py + 215))
    text(d, (px + PW / 2, py + 312),
         "Gửi mã này cho bạn bè để họ vào thẳng phòng.",
         font("regular", 20), rgb(P["text_dim"]), anchor="mm")

    box(c, "card", px + 56, py + 356, PW - 112, 76)
    c.alpha_composite(icon("lock", 28, P["text_dim"]), (px + 88, py + 380))
    text(d, (px + 134, py + 394), "Phòng riêng tư", font("medium", 22),
         rgb(P["text"]), anchor="lm")
    box(c, "pill", px + PW - 152, py + 380, 68, 36)
    d.ellipse([px + PW - 146, py + 386, px + PW - 122, py + 410],
              fill=rgb(P["text_faint"]))

    btn(c, px + 56, py + 468, PW - 112, 74, "Tạo phòng", "primary",
        fsize=27)
    return c


# ============================================================= 04 room list
def screen_list():
    c = bg()
    d = ImageDraw.Draw(c)
    PW, PH = 980, 760
    px, py = (1920 - PW) // 2, (1080 - PH) // 2
    box(c, "panel_main", px, py, PW, PH)

    icon_btn(c, px + 40, py + 40, 56, "back")
    text(d, (px + PW / 2, py + 68), "Danh sách phòng", font("bold", 36),
         rgb(P["text"]), anchor="mm")
    rule(c, px + PW / 2 - 22, py + 96)
    icon_btn(c, px + PW - 96, py + 40, 56, "refresh")

    lx, ly, lw, lh = px + 44, py + 132, PW - 88, PH - 196
    box(c, "well", lx, ly, lw, lh)

    rooms = [("Phòng của Minh", "3 / 8", "row_normal", False),
             ("Sân tập tân thủ", "6 / 8", "row_hover", False),
             ("no noob pls", "2 / 8", "row_normal", True),
             ("Phòng của Linh", "8 / 8", "row_normal", False),
             ("Đấu nhanh 4v4", "5 / 8", "row_selected", False),
             ("Chill lobby", "1 / 8", "row_normal", False)]
    ry = ly + 18
    for name, cnt, style, locked in rooms:
        rw = lw - 62
        box(c, style, lx + 18, ry, rw, 88)
        tx = lx + 46
        if locked:
            c.alpha_composite(icon("lock", 24, P["text_faint"]),
                              (tx, ry + 32))
            tx += 38
        text(d, (tx, ry + 33), name, font("medium", 25), rgb(P["text"]),
             anchor="lm")
        text(d, (tx, ry + 62), "chủ phòng · ping 24ms", font("regular", 17),
             rgb(P["text_faint"]), anchor="lm")

        full = cnt.startswith("8")
        box(c, "pill_danger" if full else "pill", lx + rw - 116, ry + 26, 96, 40)
        c.alpha_composite(
            icon("users", 20, P["danger"] if full else P["text_dim"]),
            (lx + rw - 102, ry + 36))
        text(d, (lx + rw - 40, ry + 47), cnt, font("medium", 19),
             rgb(P["danger"] if full else P["text_dim"]), anchor="mm")
        c.alpha_composite(icon("chevron_right", 22, P["text_faint"]),
                          (lx + rw + 2, ry + 34))
        ry += 100

    box(c, "scroll_track", lx + lw - 26, ly + 14, 14, lh - 28)
    box(c, "scroll_handle", lx + lw - 26, ly + 14, 14, 300)
    return c


# ============================================================ 05 room lobby
def screen_lobby():
    c = bg()
    d = ImageDraw.Draw(c)
    PW, PH = 1280, 800
    px, py = (1920 - PW) // 2, (1080 - PH) // 2
    box(c, "panel_main", px, py, PW, PH)

    icon_btn(c, px + 44, py + 44, 56, "back")
    text(d, (px + PW / 2, py + 62), "Phòng của Minh", font("bold", 38),
         rgb(P["text"]), anchor="mm")
    box(c, "pill", px + PW / 2 - 118, py + 96, 236, 46)
    text(d, (px + PW / 2 - 14, py + 120), "K7M2Q4", mono(24),
         rgb(P["accent"]), anchor="mm")
    c.alpha_composite(icon("copy", 22, P["text_dim"]),
                      (int(px + PW / 2 + 62), py + 108))

    tw, th, ty = 552, 452, py + 180
    teams = [(px + 60, "slot_red", P["red"], "Đội đỏ",
              [("Minh", True), ("Trung", False), ("Bảo", False),
               (None, False)]),
             (px + PW - 60 - tw, "slot_blue", P["blue"], "Đội xanh",
              [("Linh", False), ("An", False), (None, False), (None, False)])]

    for tx, slot, col, label, members in teams:
        box(c, "card", tx, ty, tw, th)
        d.rounded_rectangle([tx + 28, ty + 30, tx + 34, ty + 62], radius=3,
                            fill=rgb(col))
        text(d, (tx + 52, ty + 47), label, font("bold", 28), rgb(col),
             anchor="lm")
        filled = sum(1 for m, _ in members if m)
        text(d, (tx + tw - 32, ty + 47), f"{filled} / 4", font("medium", 22),
             rgb(P["text_dim"]), anchor="rm")

        sy = ty + 86
        for nm, host in members:
            box(c, slot if nm else "slot_empty", tx + 28, sy, tw - 56, 76)
            if nm:
                d.ellipse([tx + 48, sy + 16, tx + 92, sy + 60], fill=rgb(col))
                text(d, (tx + 70, sy + 39), nm[0].upper(), font("bold", 24),
                     rgb("#1A1218"), anchor="mm")
                text(d, (tx + 110, sy + 39), nm, font("medium", 24),
                     rgb(P["text"]), anchor="lm")
                if host:
                    c.alpha_composite(icon("crown", 24, P["accent"]),
                                      (int(tx + 118 +
                                           d.textlength(nm, font=font("medium", 24))), sy + 27))
                rw_ = d.textlength("sẵn sàng", font=font("regular", 17))
                text(d, (tx + tw - 54, sy + 39), "sẵn sàng",
                     font("regular", 17), rgb(P["success"]), anchor="rm")
                c.alpha_composite(icon("check", 22, P["success"]),
                                  (int(tx + tw - 84 - rw_), sy + 28))
            else:
                text(d, (tx + tw / 2, sy + 39), "Trống",
                     font("regular", 21), rgb(P["text_faint"]), anchor="mm")

            sy += 88

    by = py + PH - 116
    btn(c, px + 60, by, 216, 68, "Rời phòng", "danger", ic="logout",
        fsize=23)
    btn(c, px + PW / 2 - 150, by, 300, 68, "Đổi đội", "secondary",
        ic="swap", fsize=23)
    btn(c, px + PW - 60 - 320, by, 320, 68, "Bắt đầu trận", "primary",
        ic="play", fsize=26)

    box(c, "toast_error", px + PW / 2 - 290, py + PH + 34, 580, 66)
    c.alpha_composite(icon("warning", 26, P["danger"]),
                      (int(px + PW / 2 - 254), py + PH + 54))
    text(d, (px + PW / 2 - 214, py + PH + 67),
         "Cần ít nhất 2 người mỗi đội để bắt đầu.", font("regular", 21),
         rgb(P["danger"]), anchor="lm")
    return c


SCREENS = [("01_name_entry", screen_name), ("02_main_menu", screen_main),
           ("03_create_room", screen_create), ("04_room_list", screen_list),
           ("05_room_lobby", screen_lobby)]

if __name__ == "__main__":
    for name, fn in SCREENS:
        fn().convert("RGB").save(f"{OUT}/{name}.png", quality=95)
        print("ok", name)
