import os
from PIL import Image, ImageDraw
from gen_lib import P, SS, panel, rgb, rr_mask, vgrad

OUT = "/mnt/user-data/outputs/lobby-ui-kit/sprites"
os.makedirs(OUT, exist_ok=True)

MANIFEST = []


def save(img, name, border, note):
    img.save(f"{OUT}/{name}.png")
    MANIFEST.append((name, img.size, border, note))


# --------------------------------------------------------------- surfaces ---
save(panel((120, 120), 20, P["surface_top"], P["surface"], P["stroke"], 2,
           pad=26, shadow=(18, 8, 130), highlight=(2, 26, 0.32)),
     "panel_main", (48, 48, 48, 48), "Panel chính của mỗi màn hình")

save(panel((96, 96), 14, P["raised_top"], P["raised"], P["stroke_soft"], 2,
           pad=14, shadow=(9, 4, 95), highlight=(2, 20, 0.35)),
     "card", (32, 32, 32, 32), "Card/khối con bên trong panel")

save(panel((96, 96), 14, "#171B23", "#12161D", P["stroke_soft"], 2,
           pad=0, inner_shade=70),
     "well", (18, 18, 18, 18), "Vùng lõm — nền scroll view, nền chứa list")

# ------------------------------------------------------------------ input ---
save(panel((88, 88), 10, "#161A22", "#12161D", P["stroke"], 2,
           pad=0, inner_shade=80),
     "input_normal", (14, 14, 14, 14), "InputField trạng thái thường")

save(panel((88, 88), 10, "#1A1F28", "#151A22", P["accent"], 2,
           pad=6, shadow=(7, 0, 0), inner_shade=60),
     "input_focus", (20, 20, 20, 20), "InputField khi focus (viền accent)")

# --------------------------------------------------------------- buttons ----
BTN_R, BTN_PAD = 12, 12


def button(name, top, bot, stroke, hi=(2, 40, 0.42), sh=(9, 5, 115),
           pad=BTN_PAD, shade=None):
    save(panel((92, 92), BTN_R, top, bot, stroke, 2, pad=pad,
               shadow=sh, highlight=hi, inner_shade=shade),
         name, (pad + 16, pad + 16, pad + 16, pad + 16), "")


button("btn_primary_normal", P["accent_top"], P["accent_bot"], "#E39B14")
button("btn_primary_hover", "#FFE29B", "#FFB730", "#F0A81C")
button("btn_primary_pressed", P["accent_press"], "#C8830F", "#B0740D",
       hi=None, sh=(5, 2, 60), shade=90)
button("btn_primary_disabled", "#2B303B", "#232833", "#333947",
       hi=(2, 12, 0.35), sh=(6, 3, 60))

button("btn_secondary_normal", "#2C323F", "#232935", P["stroke"])
button("btn_secondary_hover", "#363D4C", "#2B3240", "#4A5265")
button("btn_secondary_pressed", "#1E232D", "#191E27", "#333A48",
       hi=None, sh=(5, 2, 55), shade=80)
button("btn_secondary_disabled", "#22262F", "#1D212A", "#2C313C",
       hi=(2, 8, 0.3), sh=(5, 2, 50))

button("btn_danger_normal", "#F4676E", "#D33840", "#B92E36")
button("btn_danger_hover", "#FF848A", "#E24A52", "#C93A42")
button("btn_danger_pressed", "#C0323A", "#A62A31", "#8F232A",
       hi=None, sh=(5, 2, 60), shade=90)

# ghost: no fill, chỉ viền — dùng cho nút Back
g = panel((92, 92), BTN_R, "#00000000", "#00000000", None, 0, pad=0)
gd = Image.new("RGBA", (92 * SS, 92 * SS), (0, 0, 0, 0))
ImageDraw.Draw(gd).rounded_rectangle(
    [SS, SS, 92 * SS - SS - 1, 92 * SS - SS - 1], radius=BTN_R * SS,
    outline=rgb(P["stroke"]), width=2 * SS)
save(gd.resize((92, 92), Image.LANCZOS), "btn_ghost_normal",
     (16, 16, 16, 16), "Nút phụ dạng viền (Back / Cancel)")
button("btn_ghost_hover", "#272D39", "#1F2530", "#454C5C", pad=8, sh=(6, 3, 70))

# ------------------------------------------------------------- team slots ---
def team_slot(name, top, bot, rail, tint, note):
    W = H = 104
    pad = 14
    base = panel((W, H), 14, top, bot, P["stroke_soft"], 2, pad=pad,
                 shadow=(9, 4, 95), highlight=(2, 18, 0.3))
    railh = 5
    lay = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    d.rounded_rectangle([pad, pad, pad + W - 1, pad + railh * 2 + 14],
                        radius=14, fill=rgb(rail))
    m = Image.new("L", base.size, 0)
    ImageDraw.Draw(m).rectangle([pad, pad, pad + W - 1, pad + railh - 1],
                                fill=255)
    lay.putalpha(Image.composite(lay.getchannel("A"),
                                 Image.new("L", base.size, 0), m))
    base.alpha_composite(lay)
    save(base, name, (32, 32, 32, 32), note)


team_slot("slot_red", "#2E2530", "#241D24", P["red"], None,
          "Slot người chơi — team đỏ")
team_slot("slot_blue", "#232A3A", "#1C212E", P["blue"], None,
          "Slot người chơi — team xanh")
team_slot("slot_empty", "#232833", "#1C202A", "#39404F", None,
          "Slot trống (chưa có người)")

# ------------------------------------------------------------- list rows ----
save(panel((88, 88), 12, "#242A35", "#1E232D", P["stroke_soft"], 2, pad=0),
     "row_normal", (16, 16, 16, 16), "Dòng phòng trong RoomList")
save(panel((88, 88), 12, "#2E3543", "#262C38", "#454C5C", 2, pad=0),
     "row_hover", (16, 16, 16, 16), "Dòng phòng khi hover")
save(panel((88, 88), 12, "#332C21", "#2A2419", P["accent"], 2, pad=0),
     "row_selected", (16, 16, 16, 16), "Dòng phòng đang chọn")

# ------------------------------------------------------------- scrollbar ----
save(panel((16, 80), 8, "#14181F", "#14181F", None, 0, pad=0),
     "scroll_track", (8, 8, 8, 8), "Rãnh thanh cuộn (rộng 16)")
save(panel((16, 80), 8, "#454C5C", "#3A4150", None, 0, pad=0),
     "scroll_handle", (8, 8, 8, 8), "Tay cầm thanh cuộn")

# ------------------------------------------------------------ misc chrome ---
save(panel((56, 56), 28, "#2C323F", "#242A35", P["stroke"], 2, pad=0),
     "pill", (26, 26, 26, 26), "Badge/pill (số người, trạng thái)")
save(panel((56, 56), 28, "#3A2B2B", "#2E2222", "#6B3A3C", 2, pad=0),
     "pill_danger", (26, 26, 26, 26), "Pill lỗi / phòng đầy")
save(panel((96, 96), 14, "#3A2528", "#2C1D20", "#7A3B40", 2, pad=14,
           shadow=(10, 5, 110)),
     "toast_error", (32, 32, 32, 32), "Nền cho StatusErrorText")

d = Image.new("RGBA", (8, 8), rgb(P["stroke_soft"]))
save(d, "divider", (2, 2, 2, 2), "Đường kẻ ngăn (kéo giãn tuỳ ý)")

print(f"{len(MANIFEST)} sprites -> {OUT}")
with open("/home/claude/kit/manifest.txt", "w") as f:
    for n, s, b, note in MANIFEST:
        f.write(f"{n}\t{s[0]}x{s[1]}\t{b}\t{note}\n")
