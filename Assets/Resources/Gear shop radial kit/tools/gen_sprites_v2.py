"""v2 sprites. Same filenames, sizes and 9-slice borders as v1 so the
mockup layout code and the Unity scene work unchanged — only the surface
treatment differs."""
import os, shutil
from PIL import Image, ImageDraw
from gen_lib import SS, rgb
from gen_lib2 import P2, HAIR, HAIR_HI, slab

ROOT = "/mnt/user-data/outputs/lobby-ui-kit"
S2, H2 = f"{ROOT}/sprites_v2", f"{ROOT}/hud_sprites_v2"
for p in (S2, H2):
    os.makedirs(p, exist_ok=True)

A = P2["accent"]


def w(img, name, d=S2):
    img.save(f"{d}/{name}.png")


# ------------------------------------------------------------- surfaces ----
w(slab((120, 120), 6, P2["surface"], border=HAIR, ch_tl=18, pad=26,
       sh=(16, 7, 120)), "panel_main")
w(slab((96, 96), 4, P2["raised"], border=HAIR, ch_tl=12, pad=14,
       sh=(9, 4, 80)), "card")
w(slab((96, 96), 4, P2["sunken"], border=(0, 0, 0, 90), pad=0), "well")

# ---------------------------------------------------------------- input ----
w(slab((88, 88), 4, P2["sunken"], border=HAIR, pad=0), "input_normal")
w(slab((88, 88), 4, "#151A24", border=rgb(A), bw=1.5, pad=6,
       accent_edge=(A, 2)), "input_focus")

# --------------------------------------------------------------- buttons ---
def btn(name, fill, border, ch=14, edge=None, pad=12, sh=(9, 4, 90), a=1.0):
    w(slab((92, 92), 4, fill, border=border, ch_tl=ch, pad=pad, sh=sh,
           accent_edge=edge, alpha_f=a), name)


btn("btn_primary_normal", A, (0, 0, 0, 0), edge=(P2["accent_dim"], 3))
btn("btn_primary_hover", "#FFD275", (0, 0, 0, 0),
    edge=(P2["accent_dim"], 3))
btn("btn_primary_pressed", P2["accent_dim"], (0, 0, 0, 0), sh=(4, 1, 60))
btn("btn_primary_disabled", "#232833", HAIR, sh=(5, 2, 50))

btn("btn_secondary_normal", P2["raised"], HAIR)
btn("btn_secondary_hover", P2["raised_hi"], HAIR_HI)
btn("btn_secondary_pressed", "#171C24", HAIR, sh=(4, 1, 50))
btn("btn_secondary_disabled", "#1A1E26", (255, 255, 255, 14), sh=(4, 2, 45))

btn("btn_danger_normal", "#D6414A", (0, 0, 0, 0), edge=("#9E2B33", 3))
btn("btn_danger_hover", "#E85860", (0, 0, 0, 0), edge=("#9E2B33", 3))
btn("btn_danger_pressed", "#A63039", (0, 0, 0, 0), sh=(4, 1, 60))

w(slab((92, 92), 4, "#00000000", border=HAIR_HI, ch_tl=14, pad=0),
  "btn_ghost_normal")
btn("btn_ghost_hover", "#1E232C", HAIR_HI, pad=8, sh=(6, 3, 60))

# ------------------------------------------------------------ team slots ---
for name, col, fill in (("slot_red", P2["red"], "#221A1E"),
                        ("slot_blue", P2["blue"], "#181E2B"),
                        ("slot_empty", "#39404F", "#1A1E26")):
    im = slab((104, 104), 4, fill, border=HAIR, pad=14, sh=(8, 3, 70))
    d = ImageDraw.Draw(im)
    d.rectangle([14, 14, 17, 117], fill=rgb(col))
    w(im, name)

# -------------------------------------------------------------- list rows --
w(slab((88, 88), 4, "#1C2029", border=HAIR, pad=0), "row_normal")
w(slab((88, 88), 4, P2["raised_hi"], border=HAIR_HI, pad=0), "row_hover")
im = slab((88, 88), 4, "#241E12", border=rgb(A), pad=0)
ImageDraw.Draw(im).rectangle([0, 0, 2, 87], fill=rgb(A))
w(im, "row_selected")

# -------------------------------------------------------------- scrollbar --
w(slab((16, 80), 2, "#0D1016", border=None, pad=0), "scroll_track")
w(slab((16, 80), 2, "#3F4756", border=None, pad=0), "scroll_handle")

# ------------------------------------------------------------------ misc ---
w(slab((56, 56), 4, P2["raised"], border=HAIR, pad=0), "pill")
w(slab((56, 56), 4, "#2A1C1E", border=(239, 82, 90, 90), pad=0),
  "pill_danger")
w(slab((96, 96), 4, "#2A1B1E", border=(239, 82, 90, 110), ch_tl=12, pad=14,
       sh=(9, 4, 90), accent_edge=("#B03840", 3)), "toast_error")
w(Image.new("RGBA", (8, 8), (255, 255, 255, 22)), "divider")

# =============================================================== HUD v2 =====
w(slab((72, 72), 3, "#0B0E14", border=(255, 255, 255, 18), pad=0,
       alpha_f=0.66), "hud_scrim", H2)
w(slab((72, 72), 3, "#0B0E14", border=HAIR_HI, pad=0, alpha_f=0.76),
  "hud_scrim_edge", H2)
w(slab((60, 60), 3, "#0B0E14", border=(255, 255, 255, 18), pad=0,
       alpha_f=0.66), "hud_pill", H2)
w(slab((60, 60), 3, "#1F1809", border=rgb(A), bw=1.5, pad=0,
       accent_edge=(A, 2), alpha_f=0.94), "hud_pill_active", H2)

w(slab((40, 40), 1, "#080A0F", border=(255, 255, 255, 22), pad=0,
       alpha_f=0.9), "bar_track", H2)
w(slab((40, 40), 1, "#FFFFFF", border=None, pad=0), "bar_fill", H2)
w(slab((28, 28), 1, "#FFFFFF", border=None, pad=0), "bar_fill_thin", H2)

for n in ("crosshair", "announce_bar", "dead_overlay"):
    shutil.copy(f"{ROOT}/hud_sprites/{n}.png", f"{H2}/{n}.png")

# polarity: chamfered square reads more technical than a plain circle
for n, fill, edge in (("polarity_ring", "#0E1119", "#EDEFF4"),
                      ("polarity_ring_pos", "#1F1809", A),
                      ("polarity_ring_neg", "#101828", "#4C86FF")):
    w(slab((96, 96), 3, fill, border=rgb(edge), bw=2.5, ch_tl=22, ch_br=22,
           pad=0, alpha_f=0.86), n, H2)

print("v2:", len(os.listdir(S2)), "lobby sprites,", len(os.listdir(H2)), "hud")
