#!/usr/bin/env python3
"""
宠物表情合集图切图脚本 v3
智能检测内容区域，不依赖颜色阈值

用法：
    python cut_expressions_v3.py <input.png> <output_dir> [--target-size 256]
"""

import sys
import os
from PIL import Image
import numpy as np

EXPRESSION_NAMES = ['neutral', 'happy', 'angry', 'begging', 'disgust', 'eating', 'sleepy']

def find_content_columns(image):
    """
    检测图片中包含内容的列（非纯白区域）
    通过计算每列的颜色方差来判断是否有内容
    """
    arr = np.array(image)
    height, width = arr.shape[:2]
    
    # 计算每列的颜色方差（标准差）
    # 背景是纯白色，方差很小；内容区域颜色变化大，方差大
    col_std = np.std(arr[:, :, :3], axis=(0, 2))  # 每列RGB的标准差
    
    # 标准差大于阈值的列视为有内容
    # 背景RGB几乎都是(254,254,254)，标准差接近0
    # 内容区域标准差较大
    threshold = 5.0  # 标准差阈值
    has_content = col_std > threshold
    
    return has_content

def find_boundaries_by_content(image, num_frames=7):
    """
    根据内容区域找到7个表情的边界
    """
    has_content = find_content_columns(image)
    
    # 找内容区域的边界（从有内容到无内容的过渡点）
    transitions = []
    in_content = False
    
    for i, content in enumerate(has_content):
        if content and not in_content:
            # 进入内容区域
            in_content = True
            transitions.append(('start', i))
        elif not content and in_content:
            # 离开内容区域
            in_content = False
            transitions.append(('end', i))
    
    # 处理最后一个区域
    if in_content:
        transitions.append(('end', len(has_content)))
    
    # 如果找到了7个区域，直接返回
    if len(transitions) >= num_frames * 2:
        regions = []
        for i in range(0, len(transitions), 2):
            if i+1 < len(transitions):
                start = transitions[i][1]
                end = transitions[i+1][1]
                regions.append((start, end))
        
        if len(regions) >= num_frames:
            return regions[:num_frames]
    
    # 如果没找到7个区域，使用基于间隙的切分
    return find_boundaries_by_gaps(image, num_frames)

def find_boundaries_by_gaps(image, num_frames=7):
    """
    基于间隙检测找到7个表情的边界
    """
    arr = np.array(image)
    height, width = arr.shape[:2]
    
    # 计算每列的平均颜色
    mean_color = np.mean(arr[:, :, :3], axis=(0, 2))
    
    # 找到最可能是背景的列（颜色最亮且方差最小）
    col_std = np.std(arr[:, :, :3], axis=(0, 2))
    
    # 综合评分：颜色亮 + 方差小 = 背景
    # 颜色越接近白色(255)，且方差越小，越可能是背景
    brightness = np.mean(arr[:, :, :3], axis=(0, 2))
    gap_score = brightness - col_std * 10  # 亮度高且方差小的地方分数高
    
    # 找连续的间隙区域
    is_gap = gap_score > 250
    
    # 找间隙的中心点
    gaps = []
    in_gap = False
    gap_start = 0
    
    for i, gap in enumerate(is_gap):
        if gap and not in_gap:
            in_gap = True
            gap_start = i
        elif not gap and in_gap:
            in_gap = False
            center = (gap_start + i) // 2
            gaps.append(center)
    
    if in_gap:
        center = (gap_start + len(is_gap)) // 2
        gaps.append(center)
    
    # 使用间隙点切分
    if len(gaps) >= num_frames - 1:
        boundaries = [0] + gaps[:num_frames-1] + [width]
        frames = []
        for i in range(num_frames):
            left = boundaries[i]
            right = boundaries[i+1]
            frames.append((left, right))
        return frames
    
    # 如果还是不行，就均分
    frame_width = width // num_frames
    frames = []
    for i in range(num_frames):
        left = i * frame_width
        right = (i + 1) * frame_width if i < num_frames - 1 else width
        frames.append((left, right))
    return frames

def remove_background(image, bg_threshold=253):
    """
    去除接近白色的背景
    只去除非常白的像素（>253），保留浅色的猫/狗
    """
    arr = np.array(image)
    height, width = arr.shape[:2]
    
    # 创建RGBA图像
    if len(arr.shape) == 3 and arr.shape[2] == 4:
        rgba = arr.copy()
    else:
        rgba = np.zeros((height, width, 4), dtype=np.uint8)
        rgba[:, :, :3] = arr[:, :, :3]
        rgba[:, :, 3] = 255
    
    # 只去除非常白的像素（接近255）
    # 猫的颜色虽然浅，但RGB一般在240-250左右，不会全部>253
    white_mask = (rgba[:, :, 0] >= bg_threshold) & (rgba[:, :, 1] >= bg_threshold) & (rgba[:, :, 2] >= bg_threshold)
    
    # 只有当三个通道都>=阈值时才设为透明
    rgba[white_mask, 3] = 0
    
    return Image.fromarray(rgba, 'RGBA')

def crop_to_content(image, padding=10):
    """
    裁剪到内容区域
    """
    arr = np.array(image)
    
    if len(arr.shape) != 3 or arr.shape[2] != 4:
        return image
    
    alpha = arr[:, :, 3]
    non_transparent = alpha > 0
    
    if not np.any(non_transparent):
        return image
    
    rows = np.any(non_transparent, axis=1)
    cols = np.any(non_transparent, axis=0)
    
    top = np.argmax(rows)
    bottom = len(rows) - np.argmax(rows[::-1]) - 1
    left = np.argmax(cols)
    right = len(cols) - np.argmax(cols[::-1]) - 1
    
    top = max(0, top - padding)
    bottom = min(arr.shape[0] - 1, bottom + padding)
    left = max(0, left - padding)
    right = min(arr.shape[1] - 1, right + padding)
    
    return image.crop((left, top, right, bottom))

def process_expressions(input_path, output_dir, target_size=256):
    print(f"处理: {input_path}")
    print(f"输出到: {output_dir}")
    
    os.makedirs(output_dir, exist_ok=True)
    
    image = Image.open(input_path)
    print(f"原图尺寸: {image.size}")
    
    # 找到7个表情的边界
    frames = find_boundaries_by_content(image, num_frames=7)
    
    # 如果没有找到7个，尝试用间隙检测
    if len(frames) < 7:
        print(f"内容检测只找到 {len(frames)} 个区域，尝试间隙检测...")
        frames = find_boundaries_by_gaps(image, num_frames=7)
    
    print(f"检测到 {len(frames)} 个表情区域")
    
    for i, (left, right) in enumerate(frames):
        if i >= len(EXPRESSION_NAMES):
            break
        
        name = EXPRESSION_NAMES[i]
        
        # 裁剪出表情
        frame = image.crop((left, 0, right, image.height))
        
        # 去白底（使用更高的阈值，只去除非常白的背景）
        frame = remove_background(frame, bg_threshold=253)
        
        # 裁剪到内容
        frame = crop_to_content(frame, padding=5)
        
        # 统一缩放到目标尺寸（保持比例）
        frame.thumbnail((target_size, target_size), Image.LANCZOS)
        
        # 创建新的画布，居中放置
        new_frame = Image.new('RGBA', (target_size, target_size), (0, 0, 0, 0))
        
        x = (target_size - frame.width) // 2
        y = (target_size - frame.height) // 2
        
        new_frame.paste(frame, (x, y), frame)
        
        # 保存
        output_path = os.path.join(output_dir, f"{name}.png")
        new_frame.save(output_path, 'PNG')
        print(f"  ✓ {name}.png -> {new_frame.size}")
    
    print(f"完成！共处理 {min(len(frames), len(EXPRESSION_NAMES))} 个表情")

def main():
    if len(sys.argv) < 3:
        print("用法: python cut_expressions_v3.py <input.png> <output_dir> [--target-size 256]")
        sys.exit(1)
    
    input_path = sys.argv[1]
    output_dir = sys.argv[2]
    
    target_size = 256
    if '--target-size' in sys.argv:
        idx = sys.argv.index('--target-size')
        target_size = int(sys.argv[idx + 1])
    
    process_expressions(input_path, output_dir, target_size)

if __name__ == '__main__':
    main()
