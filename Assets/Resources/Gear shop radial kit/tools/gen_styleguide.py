from PIL import Image, ImageDraw
from gen_lib import P, rgb, font, mono, text
from gen_mockups import bg, box, btn, icon, icon_btn, OUT

W, H = 1680, 1360
c = Image.new("RGBA", (W, H), rgb(P["bg_deep"]))
d = ImageDraw.Draw(c)


def head(y, s, sub):
    text(d, (72, y), s.upper(), font("medium", 17), rgb(P["text_faint"]),
         spacing=3)
    text(d, (72, y + 30), sub, font("regular", 19), rgb(P["text_dim"]))
    d.rectangle([72, y + 66, W - 72, y + 67], fill=rgb("#242A35"))


text(d, (72, 56), "Lobby UI kit", font("bold", 52), rgb(P["text"]))
text(d, (72, 122), "Xám tối trung tính · accent hổ phách · viền mảnh, "
     "gradient nhẹ, bóng mềm", font("regular", 22), rgb(P["text_dim"]))

# ---------------------------------------------------------------- palette ---
head(200, "bảng màu", "Dán thẳng vào Color field trong Unity")
sw = [("bg_deep", "Nền canvas"), ("surface", "Panel"), ("raised", "Card"),
      ("stroke", "Viền"), ("text", "Chữ chính"), ("text_dim", "Chữ phụ"),
      ("accent", "Accent / CTA"), ("red", "Team đỏ"), ("blue", "Team xanh"),
      ("success", "Sẵn sàng"), ("danger", "Lỗi")]
x, y = 72, 296
for i, (k, lab) in enumerate(sw):
    cx = x + (i % 6) * 258
    cy = y + (i // 6) * 148
    d.rounded_rectangle([cx, cy, cx + 226, cy + 84], radius=14,
                        fill=rgb(P[k]), outline=rgb("#2C323F"), width=2)
    text(d, (cx, cy + 100), lab, font("medium", 19), rgb(P["text"]))
    text(d, (cx, cy + 124), P[k].upper(), mono(17), rgb(P["text_faint"]))

# ----------------------------------------------------------------- typo -----
head(608, "type scale", "Poppins cho UI · JetBrains Mono cho mã phòng")
rows = [("Tiêu đề màn hình", "bold", 46, "Bold 46"),
        ("Tiêu đề panel", "bold", 36, "Bold 36"),
        ("Nhãn nút chính", "bold", 27, "Bold 27"),
        ("Tên người chơi / nội dung", "medium", 24, "Medium 24"),
        ("Chữ phụ, mô tả", "regular", 20, "Regular 20"),
        ("EYEBROW / NHÃN NHỎ", "medium", 15, "Medium 15 · spacing 2.6")]
y = 706
for s, w, sz, note in rows:
    text(d, (72, y), s, font(w, sz), rgb(P["text"]))
    text(d, (1180, y + sz * 0.32), note, mono(17), rgb(P["text_faint"]))
    y += sz + 30
text(d, (72, y + 4), "K7M2Q4", mono(44), rgb(P["accent"]))
text(d, (1180, y + 22), "JetBrainsMono Bold 44", mono(17),
     rgb(P["text_faint"]))

# --------------------------------------------------------------- controls ---
head(1000, "trạng thái nút", "Gán vào Button > Transition: Sprite Swap")
y = 1096
for i, st in enumerate(["normal", "hover", "pressed", "disabled"]):
    btn(c, 72 + i * 214, y, 190, 62, st.capitalize(), "primary", state=st,
        fsize=21)
    btn(c, 72 + i * 214, y + 82, 190, 62, st.capitalize(), "secondary",
        state=st, fsize=21)

box(c, "input_normal", 968, y, 300, 62)
text(d, (994, y + 31), "Input normal", font("regular", 21),
     rgb(P["text_faint"]), anchor="lm")
box(c, "input_focus", 968, y + 82, 300, 62)
text(d, (994, y + 113), "Input focus", font("regular", 21), rgb(P["text"]),
     anchor="lm")

box(c, "slot_red", 1300, y, 308, 62)
d.ellipse([1318, y + 12, 1356, y + 50], fill=rgb(P["red"]))
text(d, (1370, y + 31), "Slot đỏ", font("medium", 21), rgb(P["text"]),
     anchor="lm")
box(c, "slot_blue", 1300, y + 82, 308, 62)
d.ellipse([1318, y + 94, 1356, y + 132], fill=rgb(P["blue"]))
text(d, (1370, y + 113), "Slot xanh", font("medium", 21), rgb(P["text"]),
     anchor="lm")

# icons strip
ics = ["back", "close", "check", "plus", "chevron_right", "play", "copy",
       "refresh", "search", "lock", "users", "logout", "swap", "crown",
       "warning", "globe"]
for i, n in enumerate(ics):
    c.alpha_composite(icon(n, 34, P["text_dim"]), (74 + i * 62, 1290))

c.convert("RGB").save(f"{OUT}/00_style_guide.png", quality=95)
print("style guide ok")
