# -*- coding: utf-8 -*-
"""
疯狂铲屎官 — 批量图片处理工具
功能：去白底 → 裁切到目标尺寸 → 输出到 _processed/
用法：python batch_process.py <raw目录或文件> [--size 128]
"""

import os
import sys
import json
from pathlib import Path
from PIL import Image

RAW_DIR = Path(__file__).parent.parent / "_raw"
PROCESSED_DIR = Path(__file__).parent.parent / "_processed"

# 宠物名称映射
PET_MAP = {
    "cat": "Cat", "dog": "Dog", "hamster": "Hamster",
    "parrot": "Parrot", "fish": "Fish", "rabbit": "Rabbit",
}

# 动作映射
ACTION_MAP = {
    "idle": "idle", "happy": "happy", "cry": "cry",
    "run": "run", "cute": "cute",
}


def remove_white_bg(img, threshold=240):
    """去除白色/近白色背景，设为透明"""
    if img.mode != "RGBA":
        img = img.convert("RGBA")
    data = img.getdata()
    new_data = []
    for item in data:
        r, g, b, a = item
        if r >= threshold and g >= threshold and b >= threshold:
            new_data.append((255, 255, 255, 0))
        else:
            new_data.append((r, g, b, a))
    img.putdata(new_data)
    return img


def crop_to_content(img, padding=10):
    """裁切到非透明内容的边界"""
    bbox = img.getbbox()
    if bbox:
        bbox = (
            max(0, bbox[0] - padding),
            max(0, bbox[1] - padding),
            min(img.width, bbox[2] + padding),
            min(img.height, bbox[3] + padding),
        )
        img = img.crop(bbox)
    return img


def resize_to_square(img, target_size=128):
    """等比缩放到目标尺寸，居中放置在正方形画布上"""
    ratio = target_size / max(img.width, img.height)
    new_w = int(img.width * ratio)
    new_h = int(img.height * ratio)
    img = img.resize((new_w, new_h), Image.LANCZOS)

    canvas = Image.new("RGBA", (target_size, target_size), (0, 0, 0, 0))
    offset = ((target_size - new_w) // 2, (target_size - new_h) // 2)
    canvas.paste(img, offset, img)
    return canvas


def parse_filename(filename):
    """解析文件名，提取宠物/动作/阶段信息
    格式: {pet}_{action}_stage{N}_frame{XX}.png 或 {category}_{name}.png
    """
    name = Path(filename).stem.lower()
    parts = name.split("_")

    info = {"raw_name": name, "type": "unknown"}

    # 检测帧动画格式: cat_idle_stage2_frame01
    for part in parts:
        if part in PET_MAP:
            info["pet"] = PET_MAP[part]
            info["type"] = "animation"
        if part in ACTION_MAP:
            info["action"] = ACTION_MAP[part]
        if part.startswith("stage"):
            try:
                info["stage"] = int(part.replace("stage", ""))
            except ValueError:
                pass
        if part.startswith("frame"):
            try:
                info["frame"] = int(part.replace("frame", ""))
            except ValueError:
                pass

    # 检测住所: house_lv3
    if "house" in parts:
        info["type"] = "house"
        for part in parts:
            if part.startswith("lv"):
                info["level"] = part.replace("lv", "")

    # 检测建筑: foodbowl_lv2
    if any(k in name for k in ["foodbowl", "toy", "medical", "garden"]):
        info["type"] = "yard"
        for part in parts:
            if part.startswith("lv"):
                info["level"] = part.replace("lv", "")

    # 检测背景: yard_bg_lv3
    if "yard" in parts and "bg" in parts:
        info["type"] = "scene"
        for part in parts:
            if part.startswith("lv"):
                info["level"] = part.replace("lv", "")

    # 检测章节: chapter_01
    if "chapter" in parts:
        info["type"] = "chapter"
        for part in parts:
            if part.isdigit():
                info["chapter"] = int(part)

    # 检测UI图标: icon_fish, badge_lv3
    if "icon" in parts:
        info["type"] = "ui"
    if "badge" in parts:
        info["type"] = "badge"

    return info


def process_image(input_path, output_path, target_size=128, remove_bg=True):
    """处理单张图片"""
    img = Image.open(input_path)

    if remove_bg:
        img = remove_white_bg(img)

    img = crop_to_content(img, padding=8)
    img = resize_to_square(img, target_size)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    img.save(output_path, "PNG")
    return True


def main():
    print("=" * 60)
    print("疯狂铲屎官 — 图片批量处理工具")
    print("=" * 60)

    PROCESSED_DIR.mkdir(parents=True, exist_ok=True)

    # 扫描 _raw 目录
    files = list(RAW_DIR.glob("**/*.png")) + list(RAW_DIR.glob("**/*.jpg"))
    if not files:
        print(f"\n[!] 在 {RAW_DIR} 没有找到图片文件")
        print("请把 AI 生成的图片放到 _raw/ 目录")
        return

    print(f"\n找到 {len(files)} 个图片文件")

    success = 0
    failed = 0
    manifest = []

    for f in files:
        try:
            info = parse_filename(f.name)
            target_size = 128 if info["type"] == "animation" else 128
            if info["type"] in ("scene", "chapter"):
                target_size = 1024  # 场景背景不裁切

            # 构造输出路径
            if info["type"] == "animation":
                pet = info.get("pet", "unknown").lower()
                action = info.get("action", "idle")
                stage = info.get("stage", 2)
                frame = info.get("frame", 1)
                out_name = f"{pet}/{action}_s{stage}/frame_{frame:02d}.png"
            else:
                out_name = f.name

            out_path = PROCESSED_DIR / out_name

            if info["type"] in ("scene", "chapter"):
                # 场景图只去背景，不裁切
                img = Image.open(f)
                img = remove_white_bg(img)
                out_path.parent.mkdir(parents=True, exist_ok=True)
                img.save(out_path, "PNG")
            else:
                process_image(f, out_path, target_size)

            manifest.append({
                "raw": str(f.name),
                "output": str(out_path.relative_to(PROCESSED_DIR.parent)),
                "info": info,
            })
            success += 1
            print(f"  [OK] {f.name} → {out_path.relative_to(PROCESSED_DIR)}")

        except Exception as e:
            failed += 1
            print(f"  [FAIL] {f.name}: {e}")

    # 保存 manifest
    manifest_path = PROCESSED_DIR / "manifest.json"
    with open(manifest_path, "w", encoding="utf-8") as fp:
        json.dump(manifest, fp, ensure_ascii=False, indent=2)

    print(f"\n完成！成功 {success}，失败 {failed}")
    print(f"处理后的图片在: {PROCESSED_DIR}")
    print(f"清单文件: {manifest_path}")


if __name__ == "__main__":
    main()
