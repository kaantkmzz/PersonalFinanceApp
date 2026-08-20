from PIL import Image, ImageDraw, ImageFont, ImageFilter
import os, math, random

SIZE = 512
CENTER = SIZE / 2

METAL_LIGHT = (172, 172, 182)
METAL_MID = (108, 108, 120)
METAL_DARK = (58, 58, 68)
PURPLE = (139, 122, 255)
PURPLE_DEEP = (99, 102, 241)
PURPLE_GLOW = (180, 155, 255)

def find_font(size, bold=True):
    candidates = [
        r"C:\Windows\Fonts\segoeuib.ttf" if bold else r"C:\Windows\Fonts\segoeui.ttf",
        r"C:\Windows\Fonts\arialbd.ttf" if bold else r"C:\Windows\Fonts\arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    ]
    for c in candidates:
        if os.path.exists(c):
            return ImageFont.truetype(c, size)
    return ImageFont.load_default()

def radial_metal_disc(size):
    """Brushed-metal looking disc: base radial gradient + faint concentric brush streaks + noise."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    r_max = size / 2
    cx = cy = size / 2
    rnd = random.Random(42)
    for y in range(size):
        for x in range(size):
            dx, dy = x - cx, y - cy
            dist = math.hypot(dx, dy)
            if dist > r_max:
                continue
            t = dist / r_max
            # base radial shading: lighter near a light source (upper-left), darker at rim
            angle = math.atan2(dy, dx)
            light_bias = max(0.0, math.cos(angle - math.radians(-135))) * 0.35
            base_t = min(1.0, t * 0.85 + (1 - light_bias) * 0.15)
            r = int(METAL_LIGHT[0] + (METAL_DARK[0] - METAL_LIGHT[0]) * base_t)
            g = int(METAL_LIGHT[1] + (METAL_DARK[1] - METAL_LIGHT[1]) * base_t)
            b = int(METAL_LIGHT[2] + (METAL_DARK[2] - METAL_LIGHT[2]) * base_t)
            # circular brush streaks (concentric rings), more pronounced
            streak = math.sin(dist * 0.55) * 14
            r = max(0, min(255, r + int(streak)))
            g = max(0, min(255, g + int(streak)))
            b = max(0, min(255, b + int(streak)))
            # subtle grain
            noise = rnd.randint(-6, 6)
            r = max(0, min(255, r + noise))
            g = max(0, min(255, g + noise))
            b = max(0, min(255, b + noise))
            px[x, y] = (r, g, b, 255)
    return img

def build_icon():
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))

    # dark backdrop circle (slightly larger, gives depth behind the metal disc)
    draw = ImageDraw.Draw(img)
    pad = 6
    draw.ellipse([pad, pad, SIZE - pad, SIZE - pad], fill=(14, 14, 20, 255))

    # brushed metal disc, inset from the outer ring
    disc_inset = 26
    disc = radial_metal_disc(SIZE - disc_inset * 2)
    img.paste(disc, (disc_inset, disc_inset), disc)

    # inner shadow ring for depth at the metal disc's edge
    shadow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.ellipse([disc_inset, disc_inset, SIZE - disc_inset, SIZE - disc_inset], outline=(0, 0, 0, 140), width=10)
    shadow = shadow.filter(ImageFilter.GaussianBlur(4))
    img.alpha_composite(shadow)

    # glowing purple ring around the rim
    ring = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    rd = ImageDraw.Draw(ring)
    ring_w = 16
    ring_pad = pad + 2
    for i in range(8):
        glow_w = ring_w + (7 - i) * 5
        alpha = int(110 - i * 12)
        rd.ellipse([ring_pad - i, ring_pad - i, SIZE - ring_pad + i, SIZE - ring_pad + i],
                   outline=(*PURPLE_GLOW, max(0, alpha)), width=glow_w)
    ring = ring.filter(ImageFilter.GaussianBlur(7))
    img.alpha_composite(ring)

    # crisp purple ring line on top of the glow
    draw = ImageDraw.Draw(img)
    draw.ellipse([ring_pad, ring_pad, SIZE - ring_pad, SIZE - ring_pad], outline=PURPLE_DEEP, width=6)
    # thin bright highlight arc (upper-left) for a glossy look
    highlight = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    hd = ImageDraw.Draw(highlight)
    hd.arc([ring_pad + 3, ring_pad + 3, SIZE - ring_pad - 3, SIZE - ring_pad - 3],
           start=200, end=320, fill=(220, 210, 255, 180), width=4)
    highlight = highlight.filter(ImageFilter.GaussianBlur(1.5))
    img.alpha_composite(highlight)

    # "FT" wordmark with a metallic-purple bevel: dark drop shadow, deep-purple base, bright top highlight
    font = find_font(int(SIZE * 0.40))
    text = "FT"
    tmp = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    td = ImageDraw.Draw(tmp)
    bbox = td.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    tx = (SIZE - tw) / 2 - bbox[0]
    ty = (SIZE - th) / 2 - bbox[1] - 6

    shadow_layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    ImageDraw.Draw(shadow_layer).text((tx + 3, ty + 5), text, font=font, fill=(0, 0, 0, 200))
    shadow_layer = shadow_layer.filter(ImageFilter.GaussianBlur(3))
    img.alpha_composite(shadow_layer)

    ImageDraw.Draw(img).text((tx, ty), text, font=font, fill=PURPLE_DEEP)

    highlight_text = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    ImageDraw.Draw(highlight_text).text((tx, ty - 2), text, font=font, fill=(*PURPLE_GLOW, 210))
    mask = Image.new("L", (SIZE, SIZE), 0)
    ImageDraw.Draw(mask).rectangle([0, 0, SIZE, SIZE * 0.5], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(8))
    highlight_text.putalpha(Image.composite(highlight_text.split()[3], Image.new("L", (SIZE, SIZE), 0), mask))
    img.alpha_composite(highlight_text)

    return img

def build_simple_icon():
    """Flat, high-contrast variant for small sizes (16-32px) where the brushed-metal
    texture and soft glow of the detailed badge turn into illegible mush."""
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    pad = 10
    draw.ellipse([pad, pad, SIZE - pad, SIZE - pad], fill=(46, 46, 56, 255))
    ring_pad = pad + 6
    draw.ellipse([ring_pad, ring_pad, SIZE - ring_pad, SIZE - ring_pad], outline=PURPLE_GLOW, width=16)

    font = find_font(int(SIZE * 0.46))
    text = "FT"
    bbox = draw.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    tx = (SIZE - tw) / 2 - bbox[0]
    ty = (SIZE - th) / 2 - bbox[1] - 4
    draw.text((tx, ty), text, font=font, fill=(255, 255, 255, 255))
    return img

icon_img = build_icon()
simple_img = build_simple_icon()

detailed_sizes = [48, 64, 128, 256]
simple_sizes = [16, 20, 24, 32, 40]

resized = [simple_img.resize((s, s), Image.LANCZOS) for s in simple_sizes] + \
          [icon_img.resize((s, s), Image.LANCZOS) for s in detailed_sizes]
all_sizes = simple_sizes + detailed_sizes

out_dir = os.path.dirname(os.path.abspath(__file__))
icon_img.resize((256, 256), Image.LANCZOS).save(os.path.join(out_dir, "app_icon_preview.png"))
simple_img.resize((32, 32), Image.LANCZOS).save(os.path.join(out_dir, "app_icon_preview_small.png"))

ico_path = os.path.join(out_dir, "AppIcon.ico")
resized[0].save(
    ico_path,
    format="ICO",
    sizes=[(s, s) for s in all_sizes],
    append_images=resized[1:],
)

print("Saved:", ico_path)
