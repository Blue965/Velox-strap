from PIL import Image, ImageDraw

size = 256
img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)
for y in range(size):
    t = y / (size - 1)
    r = int((1 - t) * 0x63 + t * 0xa8)
    g = int((1 - t) * 0x66 + t * 0x55)
    b = int((1 - t) * 0xf1 + t * 0xf7)
    draw.line([(0, y), (size, y)], fill=(r, g, b, 255))

mask = Image.new('L', (size, size), 0)
mask_draw = ImageDraw.Draw(mask)
mask_draw.rounded_rectangle([(0, 0), (size - 1, size - 1)], radius=44, fill=255)
img.putalpha(mask)

poly = [
    (int(58 / 100 * size), int(10 / 100 * size)),
    (int(30 / 100 * size), int(52 / 100 * size)),
    (int(50 / 100 * size), int(52 / 100 * size)),
    (int(42 / 100 * size), int(90 / 100 * size)),
    (int(70 / 100 * size), int(48 / 100 * size)),
    (int(50 / 100 * size), int(48 / 100 * size)),
]
draw.polygon(poly, fill=(255, 255, 255, 255))

img.save('VeloxStrap.ico', sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])
print('Created VeloxStrap.ico')
