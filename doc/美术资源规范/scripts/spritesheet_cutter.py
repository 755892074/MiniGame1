# -*- coding: utf-8 -*-
"""
疯狂铲屎官 — Sprite Sheet 切图工具
功能：把一张大图（如 AI 生成的 4帧/6帧排列图）自动切分成单独的帧文件

用法：python spritesheet_cutter.py <图片路径> --cols 4 --rows 1 --prefix cat_idle_s2_frame
     python spritesheet_cutter.py <图片路径> --cols 2 --rows 2 --prefix dog_happy_s3_frame
"""

import sys
from pathlib import Path
from PIL import Image

OUTPUT_DIR = Path(__file__).parent.parent / "_raw"


def cut_spritesheet(image_path, cols, rows, prefix, output_dir=None):
    """切分 Sprite Sheet"""
    if output_dir is None:
        output_dir = OUTPUT_DIR / prefix
    output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    img = Image.open(image_path)
    if img.mode != "RGBA":
        img = img.convert("RGBA")

    cell_w = img.width // cols
    cell_h = img.height // rows

    print(f"原图: {img.width}×{img.height}")
    print(f"切分: {cols}列 × {rows}行 = {cols*rows}帧")
    print(f"单帧: {cell_w}×{cell_h}")
    print(f"输出: {output_dir}")
    print()

    frame_num = 1
    for row in range(rows):
        for col in range(cols):
            x = col * cell_w
            y = row * cell_h
            cell = img.crop((x, y, x + cell_w, y + cell_h))
            out_name = f"{prefix}_{frame_num:02d}.png"
            out_path = output_dir / out_name
            cell.save(out_path, "PNG")
            print(f"  [{frame_num:02d}/{cols*rows}] {out_name} ({cell_w}×{cell_h})")
            frame_num += 1

    print(f"\n完成！切分出 {frame_num - 1} 帧")
    print(f"接下来可以运行 batch_process.py 处理这些帧")


def main():
    if len(sys.argv) < 2:
        print("用法: python spritesheet_cutter.py <图片路径> [--cols N] [--rows N] [--prefix NAME]")
        print()
        print("示例:")
        print("  # 横向4帧")
        print('  python spritesheet_cutter.py cat_idle_sheet.png --cols 4 --rows 1 --prefix "cat_idle_s2_frame"')
        print()
        print("  # 2×2网格4帧")
        print('  python spritesheet_cutter.py dog_happy_sheet.png --cols 2 --rows 2 --prefix "dog_happy_s3_frame"')
        print()
        print("  # 横向6帧（run动画）")
        print('  python spritesheet_cutter.py cat_run_sheet.png --cols 6 --rows 1 --prefix "cat_run_s2_frame"')
        return

    image_path = sys.argv[1]
    cols = 4
    rows = 1
    prefix = "frame"

    for i, arg in enumerate(sys.argv[2:], 2):
        if arg == "--cols" and i + 1 < len(sys.argv):
            cols = int(sys.argv[i + 1])
        elif arg == "--rows" and i + 1 < len(sys.argv):
            rows = int(sys.argv[i + 1])
        elif arg == "--prefix" and i + 1 < len(sys.argv):
            prefix = sys.argv[i + 1]

    if not Path(image_path).exists():
        print(f"[!] 图片不存在: {image_path}")
        return

    cut_spritesheet(image_path, cols, rows, prefix)


if __name__ == "__main__":
    main()
