#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
UI视觉验收一键执行脚本
整合：Unity截图 -> 视觉分析 -> 报告生成

用法（分步执行）：
    # 第1步：在Unity中运行截图（菜单栏 → 铲屎官疯了 → UI视觉验收 → 执行完整截图）
    
    # 第2步：执行分析+报告（或一键执行）：
    python run_ui_inspection.py

完整参数：
    python run_ui_inspection.py --screenshots-dir ./screenshots --output-dir ./reports

环境要求：
    - Python 3.8+
    - Pillow (pip install Pillow)
    - Unity 编辑器（用于截图）
"""

import os
import sys
import json
import argparse
import subprocess
import webbrowser
from pathlib import Path
from datetime import datetime

# 配置
DEFAULT_SCREENSHOTS_DIR = Path(__file__).parent / "screenshots"
DEFAULT_OUTPUT_DIR = Path(__file__).parent / "reports"
PROJECT_ROOT = Path(__file__).parent.parent.parent


def find_unity_editor():
    """查找Unity编辑器路径"""
    # 常见的Unity安装路径
    possible_paths = [
        r"C:\Program Files\Unity\Hub\Editor",
        r"C:\Program Files\Tuanjie\Hub\Editor",
        r"D:\Program Files\Unity\Hub\Editor",
        r"D:\Program Files\Tuanjie\Hub\Editor",
    ]

    for base_path in possible_paths:
        if not os.path.exists(base_path):
            continue

        # 查找最新版本
        versions = []
        for item in os.listdir(base_path):
            version_path = os.path.join(base_path, item)
            if os.path.isdir(version_path):
                editor_exe = os.path.join(version_path, "Editor", "Unity.exe")
                if os.path.exists(editor_exe):
                    versions.append((item, editor_exe))

        if versions:
            # 返回最新版本
            versions.sort(key=lambda x: x[0], reverse=True)
            return versions[0][1]

    return None


def run_unity_screenshot(project_path: str, editor_path: str = None):
    """通过命令行触发Unity截图（需要Unity支持-batchmode）"""
    if editor_path is None:
        editor_path = find_unity_editor()

    if editor_path is None:
        print("[警告] 未找到Unity编辑器，请手动在Unity中执行截图")
        print("       菜单栏: 铲屎官疯了 → UI视觉验收 → 执行完整截图")
        return False

    print(f"[Unity] 使用编辑器: {editor_path}")

    # 构建命令
    cmd = [
        editor_path,
        "-batchmode",
        "-nographics",
        "-projectPath", project_path,
        "-executeMethod", "UIVisualInspector.RunFullCapture",
        "-quit"
    ]

    print(f"[Unity] 执行: {' '.join(cmd)}")

    try:
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=120)
        if result.returncode != 0:
            print(f"[警告] Unity命令返回非零: {result.returncode}")
            print(f"       stderr: {result.stderr[:500]}")
            return False
        return True
    except subprocess.TimeoutExpired:
        print("[警告] Unity命令超时")
        return False
    except Exception as e:
        print(f"[警告] 执行Unity命令失败: {e}")
        return False


def analyze_screenshots(screenshots_dir: Path, output_dir: Path):
    """执行视觉分析"""
    print(f"[分析] 开始分析截图...")
    print(f"       截图目录: {screenshots_dir}")
    print(f"       输出目录: {output_dir}")

    # 确保目录存在
    output_dir.mkdir(parents=True, exist_ok=True)

    # 导入分析器
    sys.path.insert(0, str(Path(__file__).parent))
    from ui_analyzer import UIVisualAnalyzer

    # 执行分析
    analyzer = UIVisualAnalyzer(str(screenshots_dir), str(output_dir))
    results = analyzer.run()

    # 生成JSON报告数据
    report_data = []
    for r in results:
        report_data.append({
            "filename": r.filename,
            "filepath": r.filepath,
            "timestamp": r.timestamp,
            "color_analysis": r.color_analysis,
            "layout_analysis": r.layout_analysis,
            "text_analysis": r.text_analysis,
            "resource_analysis": r.resource_analysis,
            "overall_score": r.overall_score,
            "issues": r.issues,
            "suggestions": r.suggestions
        })

    # 保存JSON
    json_path = output_dir / "analysis_results.json"
    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(report_data, f, ensure_ascii=False, indent=2)

    print(f"[分析] JSON报告: {json_path}")
    return json_path, report_data


def generate_html_report(json_path: Path, output_dir: Path, screenshots_dir: Path):
    """生成HTML报告"""
    print(f"[报告] 生成HTML报告...")

    # 导入报告生成器
    from report_generator import ReportGenerator

    # 读取分析结果
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    # 生成HTML
    html_path = output_dir / "ui_inspection_report.html"
    generator = ReportGenerator()
    generator.generate(data, str(html_path), str(screenshots_dir))

    print(f"[报告] HTML报告: {html_path}")
    return html_path


def print_summary(report_data: list):
    """打印分析摘要"""
    print("\n" + "=" * 60)
    print("           UI视觉验收分析摘要")
    print("=" * 60)

    total_screenshots = len(report_data)
    total_issues = sum(len(item.get("issues", [])) for item in report_data)
    total_suggestions = sum(len(item.get("suggestions", [])) for item in report_data)

    avg_score = 0
    if total_screenshots > 0:
        avg_score = sum(item.get("overall_score", 0) for item in report_data) / total_screenshots

    print(f"\n  📸 截图数量: {total_screenshots} 张")
    print(f"  📊 平均得分: {avg_score:.1f}/100")
    print(f"  ⚠️  发现问题: {total_issues} 个")
    print(f"  💡 优化建议: {total_suggestions} 个")

    print("\n  各界面得分:")
    for item in report_data:
        score = item.get("overall_score", 0)
        status = "✅" if score >= 80 else "⚠️" if score >= 60 else "❌"
        print(f"    {status} {item['filename']}: {score:.0f}/100")

    print("\n" + "=" * 60)


def main():
    parser = argparse.ArgumentParser(description="UI视觉验收一键执行")
    parser.add_argument("--screenshots-dir", "-s",
                        default=str(DEFAULT_SCREENSHOTS_DIR),
                        help=f"截图目录 (默认: {DEFAULT_SCREENSHOTS_DIR})")
    parser.add_argument("--output-dir", "-o",
                        default=str(DEFAULT_OUTPUT_DIR),
                        help=f"输出目录 (默认: {DEFAULT_OUTPUT_DIR})")
    parser.add_argument("--skip-screenshot", action="store_true",
                        help="跳过Unity截图步骤（假设已有截图）")
    parser.add_argument("--open-report", action="store_true",
                        help="完成后自动打开报告")
    parser.add_argument("--unity-editor", "-u",
                        help="Unity编辑器可执行文件路径")
    args = parser.parse_args()

    screenshots_dir = Path(args.screenshots_dir)
    output_dir = Path(args.output_dir)

    print("=" * 60)
    print("    铲屎官疯了 - UI视觉验收工具")
    print("=" * 60)
    print(f"\n   截图目录: {screenshots_dir}")
    print(f"  📁 输出目录: {output_dir}")
    print(f"  🎯 项目路径: {PROJECT_ROOT}")
    print()

    # 步骤1: Unity截图
    if not args.skip_screenshot:
        print("[步骤1/3] Unity截图")
        print("-" * 40)
        success = run_unity_screenshot(str(PROJECT_ROOT), args.unity_editor)
        if not success:
            print("\n[提示] Unity截图失败或跳过，请确保:")
            print("  1. Unity编辑器已打开项目")
            print("  2. 手动执行: 菜单栏 → 铲屎官疯了 → UI视觉验收 → 执行完整截图")
            print("  3. 或使用 --skip-screenshot 跳过此步骤")
            response = input("\n是否继续执行分析? (y/n): ")
            if response.lower() != 'y':
                return
    else:
        print("[步骤1/3] 跳过Unity截图 (--skip-screenshot)")

    # 检查截图目录
    if not screenshots_dir.exists():
        print(f"[错误] 截图目录不存在: {screenshots_dir}")
        print("       请先在Unity中执行截图")
        return

    # 步骤2: 视觉分析
    print("\n[步骤2/3] 视觉分析")
    print("-" * 40)
    try:
        json_path, report_data = analyze_screenshots(screenshots_dir, output_dir)
    except ImportError as e:
        print(f"[错误] 导入分析模块失败: {e}")
        print("       请确保已安装依赖: pip install Pillow")
        return

    # 步骤3: 生成报告
    print("\n[步骤3/3] 生成HTML报告")
    print("-" * 40)
    html_path = generate_html_report(json_path, output_dir, screenshots_dir)

    # 打印摘要
    print_summary(report_data)

    # 打开报告
    if args.open_report:
        print(f"[打开] 正在打开报告...")
        webbrowser.open(f"file:///{html_path}")

    print(f"\n✅ 完成! 报告位置: {html_path}")


if __name__ == "__main__":
    main()
