from PIL import Image, ImageDraw, ImageFont
import os

SIZE = 256
PURPLE = (99, 102, 241, 255)      # AppTheme.AccentColor
PURPLE_DARK = (60, 63, 165, 255)  # darker shade for gradient
GRAY = (40, 44, 60, 255)          # AppTheme.CardBackColor (dark)

def rounded_square_gradient(size, radius, top_color, bottom_color):
    base = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    grad = Image.new("RGBA", (1, size), (0, 0, 0, 0))
    for y in range(size):
        t = y / (size - 1)
        r = int(top_color[0] + (bottom_color[0] - top_color[0]) * t)
        g = int(top_color[1] + (bottom_color[1] - top_color[1]) * t)
        b = int(top_color[2] + (bottom_color[2] - top_color[2]) * t)
        grad.putpixel((0, y), (r, g, b, 255))
    grad = grad.resize((size, size))

    mask = Image.new("L", (size, size), 0)
    mdraw = ImageDraw.Draw(mask)
    mdraw.rounded_rectangle([0, 0, size - 1, size - 1], radius=radius, fill=255)

    base.paste(grad, (0, 0), mask)
    return base

def find_font(size):
    candidates = [
        r"C:\Windows\Fonts\segoeuib.ttf",
        r"C:\Windows\Fonts\arialbd.ttf",
        r"C:\Windows\Fonts\seguisb.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    ]
    for c in candidates:
        if os.path.exists(c):
            return ImageFont.truetype(c, size)
    return ImageFont.load_default()

def build_icon():
    radius = int(SIZE * 0.22)
    img = rounded_square_gradient(SIZE, radius, PURPLE, PURPLE_DARK)

    # subtle inner border for depth, using the gray theme tone
    draw = ImageDraw.Draw(img)
    border_inset = 4
    draw.rounded_rectangle(
        [border_inset, border_inset, SIZE - 1 - border_inset, SIZE - 1 - border_inset],
        radius=radius - border_inset,
        outline=GRAY,
        width=3,
    )

    text = "FT"
    font = find_font(int(SIZE * 0.46))
    bbox = draw.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    pos = ((SIZE - tw) / 2 - bbox[0], (SIZE - th) / 2 - bbox[1])
    draw.text(pos, text, font=font, fill=(255, 255, 255, 255))

    return img

icon_img = build_icon()

sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
resized = [icon_img.resize((s, s), Image.LANCZOS) for s in sizes]

out_dir = os.path.dirname(os.path.abspath(__file__))
resized[-1].save(os.path.join(out_dir, "app_icon_preview.png"))

ico_path = os.path.join(out_dir, "AppIcon.ico")
resized[0].save(
    ico_path,
    format="ICO",
    sizes=[(s, s) for s in sizes],
    append_images=resized[1:],
)

print("Saved:", ico_path)
