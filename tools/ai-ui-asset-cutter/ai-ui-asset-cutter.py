#!/usr/bin/env python3
"""
ai-ui-asset-cutter.py
自动切分AI生成的UI合集图，按命名规范归档
用法：
    python ai-ui-asset-cutter.py <输入图路径> --layout <layout_name>
示例：
    python ai-ui-asset-cutter.py buttons.png --layout buttons
    python ai-ui-asset-cutter.py panels.png --layout panels
    python ai-ui-asset-cutter.py items.png --layout items
"""

import os
import sys
import json
import argparse
from pathlib import Path
from PIL import Image
from typing import List, Tuple

# ============================================================
# 布局配置：预定义每种合集图的切分参数
# ============================================================
LAYOUT_CONFIGS = {
    # 横向排列的按钮套装（5个按钮横排）
    "buttons": {
        "desc": "按钮套装（5个横排）",
        "rows": 1,
        "cols": 5,
        "margins": {"top": 50, "bottom": 50, "left": 50, "right": 50},
        "output_names": ["btn_main", "btn_secondary", "btn_circle", "btn_pill", "btn_disabled"],
    },
    # 2x2网格面板（4个面板）
    "panels": {
        "desc": "面板套装（4个2x2）",
        "rows": 2,
        "cols": 2,
        "margins": {"top": 30, "bottom": 30, "left": 30, "right": 30},
        "output_names": ["panel_toast", "panel_bottom", "panel_sidebar", "panel_card"],
    },
    # 4x2网格道具图标（8个图标）
    "items": {
        "desc": "道具图标套装（8个4x2）",
        "rows": 4,
        "cols": 2,
        "margins": {"top": 20, "bottom": 20, "left": 20, "right": 20},
        "output_names": ["item_magnifier", "item_hourglass", "item_coin", "item_gift",
                          "item_lightning", "item_shield", "item_key", "item_heart"],
    },
    # 3x2网格杂项UI（6个元素）
    "misc": {
        "desc": "杂项UI套装（6个3x2）",
        "rows": 3,
        "cols": 2,
        "margins": {"top": 20, "bottom": 20, "left": 20, "right": 20},
        "output_names": ["misc_back_btn", "misc_item_base", "misc_hot_tag",
                          "misc_lock", "bg_overlay", "bg_share_card"],
    },
    # 背景图（独立，无需切分）
    # 2x4食物图标（8个，1024×512）
    "foods": {
        "desc": "食物图标（8个4×2，256px/格，1024×512）",
        "rows": 2,
        "cols": 4,
        "margins": {"top": 4, "bottom": 4, "left": 4, "right": 4},
        "output_names": ["food01", "food02", "food03", "food04",
                          "food05", "food06", "food07", "food08"],
    },
    # 2x3宠物头像（6个，1536×1024）
    "pets": {
        "desc": "宠物头像（6个3×2，512px/格，1536×1024）",
        "rows": 2,
        "cols": 3,
        "margins": {"top": 4, "bottom": 4, "left": 4, "right": 4},
        "output_names": ["pet_cat", "pet_dog", "pet_hamster",
                          "pet_parrot", "pet_fish", "pet_rabbit"],
    },
    # 2x2碗（4个，1024×1024）
    "bowls": {
        "desc": "碗套装（4个2×2，512px/格，1024×1024）",
        "rows": 2,
        "cols": 2,
        "margins": {"top": 4, "bottom": 4, "left": 4, "right": 4},
        "output_names": ["bowl_01", "bowl_02", "bowl_03", "bowl_04"],
    },
    # 3x2 UI图标（6个，384×256）
    "ui_icons": {
        "desc": "UI图标（6个3×2，128px/格，384×256）",
        "rows": 2,
        "cols": 3,
        "margins": {"top": 4, "bottom": 4, "left": 4, "right": 4},
        "output_names": ["ui_star", "ui_undo", "ui_add", "ui_shuffle", "ui_restart", "ui_next"],
    },
}

# ============================================================
# 项目路径
# ============================================================
# 默认输出到团结项目的 Assets/Art/UI/ 目录下
DEFAULT_PROJECT_ROOT = Path(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
DEFAULT_OUTPUT_DIR = DEFAULT_PROJECT_ROOT / "Assets/Art/UI"

# ============================================================
# 自动检测空白区域（智能切分）
# ============================================================

def is_blank_pixel(pixel, threshold=240):
    """判断像素是否为空白（接近白色）"""
    if len(pixel) == 4:
        # RGBA
        r, g, b, a = pixel
        return a < 10 or (r > threshold and g > threshold and b > threshold)
    else:
        # RGB
        r, g, b = pixel[:3]
        return r > threshold and g > threshold and b > threshold


def find_content_bounds(img: Image.Image, threshold=240, min_gap=10):
    """
    自动检测图片中内容区域的边界（基于空白区域分割）
    返回: [(x, y, w, h), ...] 每个内容区域的坐标和尺寸
    """
    pixels = img.load()
    width, height = img.size
    
    # 1. 找到非空白像素的行和列
    non_blank_rows = set()
    non_blank_cols = set()
    
    for y in range(height):
        for x in range(width):
            pixel = pixels[x, y]
            if not is_blank_pixel(pixel, threshold):
                non_blank_rows.add(y)
                non_blank_cols.add(x)
    
    if not non_blank_rows or not non_blank_cols:
        return []
    
    # 2. 按列分组，找到连续的非空白列（水平方向的内容块）
    sorted_cols = sorted(non_blank_cols)
    col_groups = []
    current_group = [sorted_cols[0]]
    
    for col in sorted_cols[1:]:
        if col - current_group[-1] <= min_gap:
            current_group.append(col)
        else:
            col_groups.append(current_group)
            current_group = [col]
    if current_group:
        col_groups.append(current_group)
    
    # 3. 对每个列组，找到对应的行范围
    bounds = []
    for col_group in col_groups:
        x1 = min(col_group)
        x2 = max(col_group)
        # 找到这个列组内所有行的范围
        y_min, y_max = height, 0
        for y in non_blank_rows:
            for x in range(x1, min(x2 + 1, width)):
                if not is_blank_pixel(pixels[x, y], threshold):
                    y_min = min(y_min, y)
                    y_max = max(y_max, y)
        if y_min < y_max:
            # 扩展一点边距
            margin = 5
            x1 = max(0, x1 - margin)
            y1 = max(0, y_min - margin)
            x2 = min(width, x2 + margin)
            y2 = min(height, y_max + margin)
            bounds.append((x1, y1, x2 - x1, y2 - y1))
    
    return bounds


def auto_split_grid(img: Image.Image, expected_count: int, threshold=240):
    """
    智能检测网格布局，返回每个元素的边界框
    """
    bounds = find_content_bounds(img, threshold)
    
    # 如果自动检测到的元素数与预期不符，回退到等分
    if len(bounds) != expected_count:
        print(f"  [警告] 自动检测到 {len(bounds)} 个元素，预期 {expected_count} 个")
        print(f"  [回退] 使用等分方案")
        return None
    
    return bounds


# ============================================================
# 主切分逻辑
# ============================================================

def split_image(input_path: str, layout_name: str, output_dir: str = None,
                 auto_detect: bool = True, custom_names: List[str] = None):
    """
    切分图片并保存
    
    Args:
        input_path: 输入图片路径
        layout_name: 布局名称 (buttons/panels/items/misc/bg)
        output_dir: 输出目录（默认使用项目Assets路径）
        auto_detect: 是否启用智能空白检测
        custom_names: 自定义输出文件名（可选）
    """
    img = Image.open(input_path)
    width, height = img.size
    print(f"\n{'='*60}")
    print(f"处理图片: {os.path.basename(input_path)}")
    print(f"原始尺寸: {width} x {height} | 模式: {img.mode}")
    print(f"{'='*60}")
    
    # 确定输出目录
    if output_dir is None:
        output_dir = DEFAULT_OUTPUT_DIR
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)
    
    # 获取布局配置
    if layout_name not in LAYOUT_CONFIGS:
        print(f"[错误] 未知布局 '{layout_name}'。可用布局: {list(LAYOUT_CONFIGS.keys())}")
        return False
    
    config = LAYOUT_CONFIGS[layout_name]
    print(f"布局类型: {config['desc']}")
    
    # 确定输出文件名
    names = custom_names if custom_names else config.get("output_names", [])
    
    # 尝试智能检测
    if auto_detect:
        bounds = auto_split_grid(img, config["rows"] * config["cols"])
        if bounds:
            print(f"[智能检测] 发现 {len(bounds)} 个内容区域")
            for i, (x, y, w, h) in enumerate(bounds):
                name = names[i] if i < len(names) else f"asset_{i}"
                # 裁切
                cropped = img.crop((x, y, x + w, y + h))
                # 食物/宠物图标去白底
                if name.startswith("food") or name.startswith("pet_"):
                    cropped = remove_white_bg(cropped)
                # 保存到对应子目录
                target_dir = get_target_dir(name, DEFAULT_PROJECT_ROOT)
                target_path = target_dir / f"{name}.png"
                target_path.parent.mkdir(parents=True, exist_ok=True)
                cropped.save(target_path, "PNG")
                print(f"  [{i+1}] {name}.png -> {target_dir.relative_to(DEFAULT_PROJECT_ROOT)}\\ ({w}x{h})")
            return True
    
    # 等分切分
    rows, cols = config["rows"], config["cols"]
    margins = config["margins"]
    
    # 计算有效区域（去掉边距）
    effective_width = width - margins["left"] - margins["right"]
    effective_height = height - margins["top"] - margins["bottom"]
    cell_w = effective_width // cols
    cell_h = effective_height // rows
    
    print(f"[等分切分] 网格: {cols}x{rows}, 单元格: {cell_w}x{cell_h}")
    
    count = 0
    for row in range(rows):
        for col in range(cols):
            idx = row * cols + col
            if idx >= len(names):
                break
            
            # 计算裁切区域
            x1 = margins["left"] + col * cell_w
            y1 = margins["top"] + row * cell_h
            x2 = min(margins["left"] + (col + 1) * cell_w, width - margins["right"])
            y2 = min(margins["top"] + (row + 1) * cell_h, height - margins["bottom"])
            
            # 裁切
            cropped = img.crop((x1, y1, x2, y2))
            
            # 智能裁剪空白边距（去除多余空白），仅自动检测模式启用
            if auto_detect:
                bbox = find_content_bounds(cropped)
                if bbox:
                    cx, cy, cw, ch = bbox[0]
                    cropped = cropped.crop((cx, cy, cx + cw, cy + ch))
            
            # 保存
            name = names[idx]
            cropped = img.crop((x1, y1, x2, y2))
            # 食物/宠物图标去白底
            if name.startswith("food") or name.startswith("pet_"):
                cropped = remove_white_bg(cropped)
            target_dir = get_target_dir(name, DEFAULT_PROJECT_ROOT)
            target_path = target_dir / f"{name}.png"
            target_path.parent.mkdir(parents=True, exist_ok=True)
            cropped.save(target_path, "PNG")
            print(f"  [{idx+1}] {name}.png -> {target_dir.relative_to(DEFAULT_PROJECT_ROOT)}\\ ({cropped.size[0]}x{cropped.size[1]})")
            count += 1
    
    print(f"\n[完成] 共生成 {count} 个切分文件")
    print(f"项目根目录: {DEFAULT_PROJECT_ROOT}")
    return True


def remove_white_bg(img: Image.Image, threshold=245) -> Image.Image:
    """将接近纯白的背景转为透明，保留图标主体"""
    if img.mode != "RGBA":
        img = img.convert("RGBA")
    data = img.getdata()
    new_data = []
    for r, g, b, a in data:
        if r > threshold and g > threshold and b > threshold:
            new_data.append((255, 255, 255, 0))
        else:
            new_data.append((r, g, b, a))
    img.putdata(new_data)
    return img


def get_target_dir(name: str, project_root: Path) -> Path:
    """根据文件名前缀决定存储子目录（绝对路径）"""
    if name.startswith("food"):
        return project_root / "Assets/Art/PetGame/foods"
    elif name.startswith("pet_"):
        return project_root / "Assets/Art/PetGame/pets"
    elif name.startswith("btn_") or name.startswith("panel_"):
        return project_root / "Assets/Art/UI/Sliced"
    elif name.startswith("bg_"):
        return project_root / "Assets/Art/UI/Backgrounds"
    elif name.startswith("bowl_"):
        return project_root / "Assets/Art/PetGame/bowls/empty"
    elif name.startswith("ui_"):
        return project_root / "Assets/Art/PetGame/UI"
    elif name.startswith("item_") or name.startswith("misc_"):
        return project_root / "Assets/Art/UI/Icons"
    else:
        return project_root / "Assets/Art/UI/Raw"


# ============================================================
# 9-Slice 标注（生成JSON元数据，供Unity读取）
# ============================================================

def generate_slice_metadata(name: str, img: Image.Image) -> dict:
    """
    生成9-Slice切分元数据
    返回包含 border.left, border.right, border.top, border.bottom 的JSON
    """
    width, height = img.size
    # 默认切分比例（假设按钮的圆角和边距约占总尺寸的10-15%）
    border = {
        "left": int(width * 0.15),
        "right": int(width * 0.15),
        "top": int(height * 0.25),
        "bottom": int(height * 0.25),
    }
    metadata = {
        "name": name,
        "type": "9-slice",
        "border": border,
        "original_size": [width, height],
    }
    return metadata


# ============================================================
# 命令行入口
# ============================================================

def main():
    parser = argparse.ArgumentParser(description="AI UI资产自动切分工具")
    parser.add_argument("input", help="输入图片路径")
    parser.add_argument("--layout", "-l", required=True,
                       choices=list(LAYOUT_CONFIGS.keys()),
                       help="布局类型")
    parser.add_argument("--output", "-o", default=None,
                       help="输出目录（默认使用项目Assets路径）")
    parser.add_argument("--no-auto", action="store_true",
                       help="禁用智能检测，强制使用等分")
    parser.add_argument("--names", "-n", nargs="+",
                       help="自定义输出文件名（空格分隔）")
    
    args = parser.parse_args()
    
    split_image(
        args.input,
        args.layout,
        output_dir=args.output,
        auto_detect=not args.no_auto,
        custom_names=args.names
    )


if __name__ == "__main__":
    main()
