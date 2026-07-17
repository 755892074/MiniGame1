#!/usr/bin/env python3
"""
美术资源合规性检查脚本
检查项目内所有美术资源的尺寸是否统一

用法:
    python check_art_compliance.py [项目路径]
    如果不传参数，默认检查 F:/WorkBuddy/H5MiniGame/MiniGame1/MiniGame1_Project/Assets/Art/PetGame

输出:
    - 列出每个资源目录下所有图片的尺寸
    - 标识出尺寸不一致的资源
    - 给出合规性评分和建议
"""

import os
import sys
from pathlib import Path
from collections import defaultdict

try:
    from PIL import Image
except ImportError:
    print("错误：缺少 Pillow 库。请先安装：pip install Pillow")
    sys.exit(1)

# 默认项目路径
DEFAULT_PROJECT_PATH = "F:/WorkBuddy/H5MiniGame/MiniGame1/MiniGame1_Project/Assets/Art/PetGame"

# 资源规范定义
RESOURCE_SPECS = {
    "pets": {
        "description": "宠物表情",
        "expected_size": (256, 256),
        "tolerance": 0,  # 严格匹配
        "importance": "P0",
    },
    "animations": {
        "description": "宠物动画帧",
        "expected_size": (128, 256),
        "tolerance": 0,
        "importance": "P0",
    },
    "bowls": {
        "description": "碗",
        "expected_size": (256, 256),
        "tolerance": 0,
        "importance": "P1",
    },
    "foods": {
        "description": "食物图标",
        "expected_size": (128, 128),
        "tolerance": 0,
        "importance": "P1",
    },
    "ui": {
        "description": "UI元素",
        "expected_size": None,  # UI元素可能有多种尺寸
        "tolerance": 0,
        "importance": "P2",
    },
}

def get_image_info(path):
    """获取图片尺寸和文件大小"""
    try:
        with Image.open(path) as img:
            return {
                "size": img.size,
                "mode": img.mode,
                "path": path,
                "file_size": os.path.getsize(path),
            }
    except Exception as e:
        return {
            "size": None,
            "mode": None,
            "path": path,
            "error": str(e),
        }

def check_directory(directory, label=""):
    """检查目录下所有图片"""
    images = []
    for root, dirs, files in os.walk(directory):
        for file in sorted(files):
            if file.lower().endswith(('.png', '.jpg', '.jpeg')):
                path = os.path.join(root, file)
                info = get_image_info(path)
                rel_path = os.path.relpath(path, directory)
                info["rel_path"] = rel_path
                images.append(info)
    return images

def analyze_group(images, expected_size=None):
    """分析一组图片的合规性"""
    if not images:
        return {"compliant": True, "message": "无图片", "sizes": set()}
    
    sizes = set()
    for img in images:
        if img["size"]:
            sizes.add(img["size"])
    
    if expected_size is None:
        # 如果没有期望尺寸，只要所有图片尺寸一致即可
        return {
            "compliant": len(sizes) <= 1,
            "sizes": sizes,
            "message": f"共 {len(images)} 张，尺寸一致" if len(sizes) <= 1 else f"尺寸不一致！发现 {len(sizes)} 种尺寸",
        }
    
    # 检查是否符合期望尺寸
    non_compliant = []
    for img in images:
        if img["size"] and img["size"] != expected_size:
            non_compliant.append(img)
    
    return {
        "compliant": len(non_compliant) == 0,
        "sizes": sizes,
        "non_compliant": non_compliant,
        "message": f"共 {len(images)} 张，全部合规" if len(non_compliant) == 0 else f"共 {len(images)} 张，{len(non_compliant)} 张不合规",
    }

def print_header(text):
    print(f"\n{'='*60}")
    print(f" {text}")
    print(f"{'='*60}")

def print_section(text):
    print(f"\n{'─'*60}")
    print(f" {text}")
    print(f"{'─'*60}")

def main():
    project_path = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_PROJECT_PATH
    
    if not os.path.exists(project_path):
        print(f"错误：项目路径不存在: {project_path}")
        sys.exit(1)
    
    print_header("美术资源合规性检查报告")
    print(f"项目路径: {project_path}")
    from datetime import datetime
    print(f"检查时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    
    # 检查各目录
    all_compliant = True
    results = {}
    
    # 1. 检查宠物表情
    pets_path = os.path.join(project_path, "pets")
    if os.path.exists(pets_path):
        print_section("宠物表情 (pets/)")
        for pet_dir in sorted(os.listdir(pets_path)):
            pet_path = os.path.join(pets_path, pet_dir)
            if os.path.isdir(pet_path):
                images = check_directory(pet_path, pet_dir)
                result = analyze_group(images, (256, 256))
                results[f"pets/{pet_dir}"] = result
                
                status = "✅" if result["compliant"] else "❌"
                print(f"  {status} {pet_dir}: {result['message']}")
                if not result["compliant"] and "non_compliant" in result:
                    for img in result["non_compliant"][:3]:  # 最多显示3个
                        print(f"     └─ {img['rel_path']}: {img['size']}")
                    if len(result["non_compliant"]) > 3:
                        print(f"     └─ ... 等共 {len(result['non_compliant'])} 张")
                    all_compliant = False
    
    # 2. 检查动画
    animations_path = os.path.join(project_path, "Animations")
    if os.path.exists(animations_path):
        print_section("动画帧 (Animations/)")
        for pet_anim_dir in sorted(os.listdir(animations_path)):
            pet_anim_path = os.path.join(animations_path, pet_anim_dir)
            if os.path.isdir(pet_anim_path):
                for anim_type in ["idle", "walk", "eat"]:
                    anim_type_path = os.path.join(pet_anim_path, anim_type)
                    if os.path.exists(anim_type_path):
                        images = check_directory(anim_type_path, f"{pet_anim_dir}/{anim_type}")
                        result = analyze_group(images, (128, 256))
                        results[f"animations/{pet_anim_dir}/{anim_type}"] = result
                        
                        status = "✅" if result["compliant"] else "❌"
                        print(f"  {status} {pet_anim_dir}/{anim_type}: {result['message']}")
                        if not result["compliant"] and "non_compliant" in result:
                            for img in result["non_compliant"][:3]:
                                print(f"     └─ {img['rel_path']}: {img['size']}")
                            if len(result["non_compliant"]) > 3:
                                print(f"     └─ ... 等共 {len(result['non_compliant'])} 张")
                            all_compliant = False
    
    # 3. 检查碗
    bowls_path = os.path.join(project_path, "bowls")
    if os.path.exists(bowls_path):
        print_section("碗 (bowls/)")
        for bowl_type in ["empty", "full"]:
            bowl_type_path = os.path.join(bowls_path, bowl_type)
            if os.path.exists(bowl_type_path):
                images = check_directory(bowl_type_path, bowl_type)
                result = analyze_group(images, (256, 256))
                results[f"bowls/{bowl_type}"] = result
                
                status = "✅" if result["compliant"] else "❌"
                print(f"  {status} {bowl_type}: {result['message']}")
                if not result["compliant"] and "non_compliant" in result:
                    for img in result["non_compliant"][:3]:
                        print(f"     └─ {img['rel_path']}: {img['size']}")
                    all_compliant = False
    
    # 4. 检查食物
    foods_path = os.path.join(project_path, "foods")
    if os.path.exists(foods_path):
        print_section("食物 (foods/)")
        images = check_directory(foods_path, "foods")
        result = analyze_group(images, (128, 128))
        results["foods"] = result
        
        status = "✅" if result["compliant"] else "❌"
        print(f"  {status} foods: {result['message']}")
        if not result["compliant"] and "non_compliant" in result:
            for img in result["non_compliant"][:5]:
                print(f"     └─ {img['rel_path']}: {img['size']}")
            if len(result["non_compliant"]) > 5:
                print(f"     └─ ... 等共 {len(result['non_compliant'])} 张")
            all_compliant = False
    
    # 5. 检查UI
    ui_path = os.path.join(project_path, "UI")
    if os.path.exists(ui_path):
        print_section("UI元素 (UI/)")
        for ui_subdir in sorted(os.listdir(ui_path)):
            ui_subdir_path = os.path.join(ui_path, ui_subdir)
            if os.path.isdir(ui_subdir_path):
                images = check_directory(ui_subdir_path, ui_subdir)
                result = analyze_group(images, None)  # UI不强制统一尺寸
                results[f"ui/{ui_subdir}"] = result
                
                status = "✅" if result["compliant"] else "❌"
                print(f"  {status} {ui_subdir}: {result['message']}")
                if result["sizes"]:
                    print(f"     尺寸分布: {result['sizes']}")
    
    # 总结
    print_header("检查结果总结")
    
    compliant_count = sum(1 for r in results.values() if r["compliant"])
    total_count = len(results)
    
    print(f"合规目录: {compliant_count}/{total_count}")
    print(f"不合规目录: {total_count - compliant_count}")
    
    if all_compliant:
        print("\n🎉 恭喜！所有资源都符合规范！")
    else:
        print("\n⚠️ 发现不合规资源，请按以下优先级处理：")
        print("  1. 宠物表情 (P0) - 影响游戏内表情切换体验")
        print("  2. 动画帧 (P0) - 影响动画播放效果")
        print("  3. 碗和食物 (P1) - 影响关卡内显示")
        print("  4. UI元素 (P2) - 等UI设计确定后处理")
        print("\n建议：使用简化版提示词重新生成，确保尺寸统一。")
        print("参考文档: doc/美术资源规范与重新生成清单.md")

if __name__ == "__main__":
    main()
