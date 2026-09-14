"""Core drawing helpers for the lobby UI kit.

Everything is drawn at SS x resolution then downsampled with LANCZOS so the
thin 2px borders and 12-16px corner radii stay crisp instead of aliasing.
"""
from PIL import Image, ImageDraw, ImageFilter, ImageFont

SS = 4  # supersample factor

# ---------------------------------------------------------------- palette ---
P = {
    "bg_deep":      "#10121A",
    "bg_vignette":  "#0A0C12",
    "surface":      "#1A1E27",
    "surface_top":  "#222734",
    "raised":       "#252B37",
    "raised_top":   "#2E3543",
    "stroke":       "#39404F",
    "stroke_soft":  "#2B3140",
    "text":         "#E9ECF3",
    "text_dim":     "#8B94A7",
    "text_faint":   "#5C6577",

    "accent":       "#FFC24B",
    "accent_top":   "#FFD57E",
    "accent_bot":   "#F2A81E",
    "accent_press": "#DE9314",
    "on_accent":    "#231803",

    "red_top":      "#FF7178",
    "red":          "#EF525A",
    "red_bot":      "#D6373F",
    "blue_top":     "#7BA6FF",
    "blue":         "#4C86FF",
    "blue_bot":     "#2E63E0",

    "danger":       "#FF7B7B",
    "success":      "#3ECF8E",
    "disabled_txt": "#5A6273",
}


def rgb(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


# ------------------------------------------------------------- primitives ---
def vgrad(size, top, bottom):
    """Vertical linear gradient image (RGBA)."""
    w, h = size
    img = Image.new("RGBA", (1, h))
    t, b = rgb(top), rgb(bottom)
    px = img.load()
    for y in range(h):
        f = y / max(h - 1, 1)
        px[0, y] = tuple(int(t[i] + (b[i] - t[i]) * f) for i in range(4))
    return img.resize((w, h), Image.BILINEAR)


def rr_mask(size, radius, scale=1):
    """Anti-aliased rounded-rect alpha mask."""
    w, h = size
    m = Image.new("L", (w * scale, h * scale), 0)
    ImageDraw.Draw(m).rounded_rectangle(
        [0, 0, w * scale - 1, h * scale - 1], radius=radius * scale, fill=255)
    return m if scale == 1 else m.resize((w, h), Image.LANCZOS)


def soft_shadow(size, radius, blur, offset, alpha, pad):
    """Blurred rounded-rect shadow on a padded transparent canvas."""
    w, h = size
    cw, ch = w + pad * 2, h + pad * 2
    lay = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    sh = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    d = ImageDraw.Draw(sh)
    d.rounded_rectangle([pad, pad + offset, pad + w - 1, pad + offset + h - 1],
                        radius=radius, fill=(0, 0, 0, alpha))
    sh = sh.filter(ImageFilter.GaussianBlur(blur))
    lay.alpha_composite(sh)
    return lay


def top_highlight(size, radius, width, alpha, fade):
    """Thin bright line hugging the inner top edge, fading downward.

    This is what sells the 'soft gradient' look without a hard bevel.
    """
    w, h = size
    lay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    d.rounded_rectangle([0, 0, w - 1, h - 1], radius=radius,
                        outline=(255, 255, 255, alpha), width=width)
    ramp = Image.new("L", (1, h))
    rp = ramp.load()
    cut = max(int(h * fade), 2)
    for y in range(h):
        rp[0, y] = int(255 * max(0.0, 1.0 - y / cut))
    ramp = ramp.resize((w, h), Image.BILINEAR)
    a = lay.getchannel("A")
    lay.putalpha(Image.composite(a, Image.new("L", (w, h), 0), ramp).point(
        lambda v: v) if False else Image.eval(a, lambda v: v))
    # multiply alpha by ramp
    import PIL.ImageChops as C
    lay.putalpha(C.multiply(lay.getchannel("A"), ramp))
    return lay


def panel(size, radius, fill_top, fill_bot, stroke=None, stroke_w=2,
          pad=0, shadow=None, highlight=None, inner_shade=None):
    """Build one sprite. Returns RGBA image of size (w+2pad, h+2pad).

    shadow    = (blur, offset, alpha)
    highlight = (width, alpha, fade)   inner top light line
    inner_shade = alpha                subtle dark line on inner bottom edge
    """
    w, h = size
    W, H = (w + pad * 2) * SS, (h + pad * 2) * SS
    r, sw, p = radius * SS, stroke_w * SS, pad * SS
    bw, bh = w * SS, h * SS

    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    if shadow:
        blur, off, alp = shadow
        canvas.alpha_composite(
            soft_shadow((bw, bh), r, blur * SS, off * SS, alp, p))

    body = vgrad((bw, bh), fill_top, fill_bot)
    body.putalpha(rr_mask((bw, bh), r))

    if inner_shade:
        sh = Image.new("RGBA", (bw, bh), (0, 0, 0, 0))
        ImageDraw.Draw(sh).rounded_rectangle(
            [0, 0, bw - 1, bh - 1], radius=r,
            outline=(0, 0, 0, inner_shade), width=max(sw, 2))
        import PIL.ImageChops as C
        ramp = Image.new("L", (1, bh))
        rp = ramp.load()
        for y in range(bh):
            rp[0, y] = int(255 * max(0.0, (y - bh * 0.55) / (bh * 0.45)))
        ramp = ramp.resize((bw, bh), Image.BILINEAR)
        sh.putalpha(C.multiply(sh.getchannel("A"), ramp))
        body.alpha_composite(sh)

    if highlight:
        hw, ha, hf = highlight
        body.alpha_composite(top_highlight((bw, bh), r, hw * SS, ha, hf))

    if stroke:
        st = Image.new("RGBA", (bw, bh), (0, 0, 0, 0))
        ImageDraw.Draw(st).rounded_rectangle(
            [sw / 2, sw / 2, bw - 1 - sw / 2, bh - 1 - sw / 2],
            radius=max(r - sw / 2, 1), outline=rgb(stroke), width=sw)
        body.alpha_composite(st)

    canvas.alpha_composite(body, (p, p))
    return canvas.resize(((w + pad * 2), (h + pad * 2)), Image.LANCZOS)


# ---------------------------------------------------------------- 9-slice ---
def nineslice(sprite, w, h, border):
    """Stretch a sprite the way Unity's Image(type=Sliced) will.

    border = (left, right, top, bottom) in px, matching Sprite Editor values.
    Used so the mockups show the true result, not a naive scale.
    """
    l, r, t, b = border
    sw, sh = sprite.size
    w, h = max(w, l + r + 1), max(h, t + b + 1)
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    cw, ch = sw - l - r, sh - t - b
    mw, mh = w - l - r, h - t - b

    def crop(x0, y0, x1, y1):
        return sprite.crop((x0, y0, x1, y1))

    def put(im, x, y, tw, th):
        if tw <= 0 or th <= 0:
            return
        out.alpha_composite(im.resize((tw, th), Image.BILINEAR), (x, y))

    put(crop(0, 0, l, t), 0, 0, l, t)
    put(crop(sw - r, 0, sw, t), w - r, 0, r, t)
    put(crop(0, sh - b, l, sh), 0, h - b, l, b)
    put(crop(sw - r, sh - b, sw, sh), w - r, h - b, r, b)
    put(crop(l, 0, sw - r, t), l, 0, mw, t)
    put(crop(l, sh - b, sw - r, sh), l, h - b, mw, b)
    put(crop(0, t, l, sh - b), 0, t, l, mh)
    put(crop(sw - r, t, sw, sh - b), w - r, t, r, mh)
    put(crop(l, t, sw - r, sh - b), l, t, mw, mh)
    return out


# ------------------------------------------------------------------ fonts ---
GF = "/usr/share/fonts/truetype/google-fonts/"
MONO = "/mnt/skills/examples/canvas-design/canvas-fonts/JetBrainsMono-Bold.ttf"


def font(weight="regular", size=24):
    f = {"regular": "Poppins-Regular.ttf", "medium": "Poppins-Medium.ttf",
         "bold": "Poppins-Bold.ttf", "light": "Poppins-Light.ttf"}[weight]
    return ImageFont.truetype(GF + f, size)


def mono(size=24):
    return ImageFont.truetype(MONO, size)


def text(draw, xy, s, f, fill, anchor="la", spacing=0):
    """Draw text, with optional manual letter-spacing for eyebrow labels."""
    if not spacing:
        draw.text(xy, s, font=f, fill=fill, anchor=anchor)
        return
    widths = [draw.textlength(c, font=f) + spacing for c in s]
    total = sum(widths) - spacing
    x, y = xy
    if anchor[0] == "m":
        x -= total / 2
    elif anchor[0] == "r":
        x -= total
    for c, cw in zip(s, widths):
        draw.text((x, y), c, font=f, fill=fill, anchor="l" + anchor[1])
        x += cw
