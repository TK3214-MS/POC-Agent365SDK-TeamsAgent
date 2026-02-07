#!/usr/bin/env python3
from PIL import Image, ImageDraw, ImageFont

# カラーアイコン (192x192)
color_img = Image.new('RGB', (192, 192), color='#0078D4')
draw = ImageDraw.Draw(color_img)
try:
    font = ImageFont.truetype("/System/Library/Fonts/ヒラギノ角ゴシック W6.ttc", 60)
except:
    try:
        font = ImageFont.truetype("/System/Library/Fonts/Hiragino Sans GB.ttc", 60)
    except:
        font = ImageFont.load_default()

text = "営業\nBot"
bbox = draw.textbbox((0, 0), text, font=font)
text_width = bbox[2] - bbox[0]
text_height = bbox[3] - bbox[1]
x = (192 - text_width) / 2
y = (192 - text_height) / 2
draw.multiline_text((x, y), text, fill='white', font=font, align='center')
color_img.save('color.png')
print("✅ color.png created (192x192)")

# アウトラインアイコン (32x32)
outline_img = Image.new('RGBA', (32, 32), color=(0, 0, 0, 0))
draw = ImageDraw.Draw(outline_img)
try:
    font_small = ImageFont.truetype("/System/Library/Fonts/ヒラギノ角ゴシック W6.ttc", 20)
except:
    try:
        font_small = ImageFont.truetype("/System/Library/Fonts/Hiragino Sans GB.ttc", 20)
    except:
        font_small = ImageFont.load_default()

text = "営"
bbox = draw.textbbox((0, 0), text, font=font_small)
text_width = bbox[2] - bbox[0]
text_height = bbox[3] - bbox[1]
x = (32 - text_width) / 2
y = (32 - text_height) / 2
draw.text((x, y), text, fill='white', font=font_small)
outline_img.save('outline.png')
print("✅ outline.png created (32x32)")
