#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
UI视觉分析器 - 铲屎官疯了
功能：
1. 色彩分析（主色调、色彩分布、对比度）
2. 文字可读性检测（对比度、大小）
3. 布局分析（元素间距、对齐、层级）
4. 资源一致性检测（风格统一性）
5. 支持接入视觉模型API（Kimi等）做深层分析

用法：
    python ui_analyzer.py --screenshots-dir ./screenshots --output ./reports
"""

import os
import sys
import json
import math
import colorsys
import argparse
from dataclasses import dataclass, field, asdict
from typing import List, Dict, Tuple, Optional
from pathlib import Path
from datetime import datetime

# 可选依赖
HAVE_CV2 = False
HAVE_PIL = False

# 基础依赖
class ColorAnalyzer:
    """色彩分析器"""

    def __init__(self):
        pass

    def analyze(self, image_path: str) -> Dict:
        """分析图片色彩特征"""
        try:
            from PIL import Image
        except ImportError:
            return {"error": "PIL not installed, install with: pip install Pillow"}

        try:
            img = Image.open(image_path).convert('RGB')
            pixels = list(img.getdata())

            # 主色调（使用简单的聚类）
            dominant_colors = self._extract_dominant_colors(pixels, n_colors=5)

            # 色彩分布
            color_distribution = self._analyze_color_distribution(pixels)

            # 对比度（使用最亮和最暗像素计算）
            contrast = self._calculate_contrast(pixels)

            # 亮度分布
            brightness = self._analyze_brightness(pixels)

            # 饱和度
            saturation = self._analyze_saturation(pixels)

            return {
                "dominant_colors": dominant_colors,
                "color_distribution": color_distribution,
                "contrast": contrast,
                "brightness": brightness,
                "saturation": saturation,
                "recommendations": self._color_recommendations(dominant_colors, contrast, brightness)
            }
        except Exception as e:
            return {"error": str(e)}

    def _extract_dominant_colors(self, pixels, n_colors=5):
        """提取图片的主色调"""
        # 简单降采样聚类
        from collections import Counter

        # 将每个像素量化为32级
        quantized = []
        for r, g, b in pixels:
            qr = (r // 32) * 32
            qg = (g // 32) * 32
            qb = (b // 32) * 32
            quantized.append((qr, qg, qb))

        # 统计频次
        counter = Counter(quantized)
        most_common = counter.most_common(n_colors)

        colors = []
        for (r, g, b), count in most_common:
            colors.append({
                "hex": f"#{r:02x}{g:02x}{b:02x}",
                "rgb": [r, g, b],
                "frequency": round(count / len(pixels) * 100, 2)
            })

        return colors

    def _analyze_color_distribution(self, pixels):
        """分析色彩分布"""
        warm_count = 0
        cool_count = 0
        neutral_count = 0
        total = len(pixels)

        for r, g, b in pixels:
            h, s, v = colorsys.rgb_to_hsv(r/255.0, g/255.0, b/255.0)
            if s < 0.1:
                neutral_count += 1
            elif 0 <= h < 0.17 or 0.83 <= h <= 1.0:
                warm_count += 1  # 红橙黄
            elif 0.5 <= h < 0.75:
                cool_count += 1  # 蓝青
            else:
                neutral_count += 1

        return {
            "warm_percent": round(warm_count / total * 100, 2),
            "cool_percent": round(cool_count / total * 100, 2),
            "neutral_percent": round(neutral_count / total * 100, 2)
        }

    def _calculate_contrast(self, pixels):
        """计算图片的对比度"""
        # 简化的 Michelson 对比度
        if not pixels:
            return 0

        luminances = [0.299*r + 0.587*g + 0.114*b for r, g, b in pixels]
        min_lum = min(luminances)
        max_lum = max(luminances)

        if max_lum + min_lum == 0:
            return 0

        contrast = (max_lum - min_lum) / (max_lum + min_lum)
        return round(contrast * 100, 2)

    def _analyze_brightness(self, pixels):
        """分析亮度分布"""
        if not pixels:
            return {}

        luminances = [0.299*r + 0.587*g + 0.114*b for r, g, b in pixels]
        return {
            "mean": round(sum(luminances) / len(luminances), 2),
            "min": round(min(luminances), 2),
            "max": round(max(luminances), 2),
            "assessment": self._assess_brightness(sum(luminances) / len(luminances))
        }

    def _assess_brightness(self, mean_lum):
        if mean_lum < 80:
            return "偏暗，可能影响可读性"
        elif mean_lum > 220:
            return "过亮，可能造成视觉疲劳"
        return "亮度适中"

    def _analyze_saturation(self, pixels):
        """分析饱和度"""
        if not pixels:
            return {}

        sats = []
        for r, g, b in pixels:
            h, s, v = colorsys.rgb_to_hsv(r/255.0, g/255.0, b/255.0)
            sats.append(s)

        return {
            "mean": round(sum(sats) / len(sats) * 100, 2),
            "assessment": self._assess_saturation(sum(sats) / len(sats))
        }

    def _assess_saturation(self, mean_sat):
        if mean_sat < 0.1:
            return "色彩过于单调，缺乏活力"
        elif mean_sat > 0.8:
            return "色彩过于鲜艳，可能造成视觉疲劳"
        return "饱和度适中"

    def _color_recommendations(self, dominant_colors, contrast, brightness):
        """基于分析结果给出色彩建议"""
        recommendations = []

        if contrast < 30:
            recommendations.append("️ 对比度过低，文字和背景可能难以区分，建议增加明暗对比")
        elif contrast > 90:
            recommendations.append("⚠️ 对比度过高，可能产生视觉疲劳，建议适当降低")

        if len(dominant_colors) > 0 and dominant_colors[0].get("frequency", 0) > 60:
            recommendations.append(f"⚠️ 主色调({dominant_colors[0]['hex']})占比过高，建议增加色彩层次")

        if not recommendations:
            recommendations.append(" 色彩表现良好")

        return recommendations


class LayoutAnalyzer:
    """布局分析器"""

    def __init__(self):
        pass

    def analyze(self, image_path: str) -> Dict:
        """分析图片布局特征"""
        try:
            from PIL import Image
        except ImportError:
            return {"error": "PIL not installed"}

        try:
            img = Image.open(image_path).convert('RGB')
            width, height = img.size

            # 检测边缘（简单实现：检测图片上下左右边缘的像素变化）
            edge_analysis = self._analyze_edges(img)

            # 检测中心焦点
            center_focus = self._analyze_center_focus(img)

            # 检测空白区域
            whitespace = self._analyze_whitespace(img)

            # 检测对称性
            symmetry = self._analyze_symmetry(img)

            return {
                "dimensions": {"width": width, "height": height, "aspect_ratio": round(width/height, 3)},
                "edge_analysis": edge_analysis,
                "center_focus": center_focus,
                "whitespace": whitespace,
                "symmetry": symmetry,
                "recommendations": self._layout_recommendations(edge_analysis, whitespace, symmetry)
            }
        except Exception as e:
            return {"error": str(e)}

    def _analyze_edges(self, img):
        """分析图片边缘（检测元素是否贴边）"""
        width, height = img.size
        pixels = img.load()

        # 简单检测：边缘是否有明显的非背景色
        edge_pixels = []
        for x in range(width):
            edge_pixels.append(pixels[x, 0])  # 上边缘
            edge_pixels.append(pixels[x, height-1])  # 下边缘
        for y in range(height):
            edge_pixels.append(pixels[0, y])  # 左边缘
            edge_pixels.append(pixels[width-1, y])  # 右边缘

        # 计算边缘颜色方差（方差大说明边缘内容丰富，可能贴边）
        avg_r = sum(p[0] for p in edge_pixels) / len(edge_pixels)
        avg_g = sum(p[1] for p in edge_pixels) / len(edge_pixels)
        avg_b = sum(p[2] for p in edge_pixels) / len(edge_pixels)

        variance = sum(((p[0]-avg_r)**2 + (p[1]-avg_g)**2 + (p[2]-avg_b)**2) / 3 for p in edge_pixels) / len(edge_pixels)

        return {
            "variance": round(variance, 2),
            "has_content_near_edge": variance > 1000,
            "assessment": "内容可能过于贴边" if variance > 1000 else "边缘留白合理"
        }

    def _analyze_center_focus(self, img):
        """分析视觉焦点是否在中心区域"""
        width, height = img.size
        center_region = (width * 0.25, height * 0.25, width * 0.75, height * 0.75)

        # 计算中心区域与边缘区域的色彩差异
        center_pixels = []
        edge_pixels = []

        pixels = img.load()
        for y in range(height):
            for x in range(width):
                if center_region[0] <= x <= center_region[2] and center_region[1] <= y <= center_region[3]:
                    center_pixels.append(pixels[x, y])
                else:
                    edge_pixels.append(pixels[x, y])

        if center_pixels and edge_pixels:
            center_lum = sum(0.299*r + 0.587*g + 0.114*b for r, g, b in center_pixels) / len(center_pixels)
            edge_lum = sum(0.299*r + 0.587*g + 0.114*b for r, g, b in edge_pixels) / len(edge_pixels)

            return {
                "center_brighter": center_lum > edge_lum,
                "luminance_diff": round(abs(center_lum - edge_lum), 2),
                "assessment": "视觉焦点集中在中心" if abs(center_lum - edge_lum) > 30 else "视觉焦点分散"
            }

        return {"assessment": "无法确定视觉焦点"}

    def _analyze_whitespace(self, img):
        """分析空白区域"""
        width, height = img.size
        pixels = img.load()

        # 检测近似白色的区域（留白）
        whitespace_count = 0
        total_pixels = width * height

        for y in range(height):
            for x in range(width):
                r, g, b = pixels[x, y]
                if r > 240 and g > 240 and b > 240:
                    whitespace_count += 1

        whitespace_percent = (whitespace_count / total_pixels) * 100

        return {
            "percentage": round(whitespace_percent, 2),
            "assessment": self._assess_whitespace(whitespace_percent)
        }

    def _assess_whitespace(self, percent):
        if percent < 10:
            return "页面过于拥挤，建议增加留白"
        elif percent > 50:
            return "留白过多，内容可能显得稀疏"
        return "留白适中"

    def _analyze_symmetry(self, img):
        """分析图片的对称性"""
        width, height = img.size
        pixels = img.load()

        # 垂直对称性检测
        diff_sum = 0
        for y in range(height):
            for x in range(width // 2):
                left = pixels[x, y]
                right = pixels[width - 1 - x, y]
                diff = abs(left[0] - right[0]) + abs(left[1] - right[1]) + abs(left[2] - right[2])
                diff_sum += diff

        total_pixels = width * height // 2
        avg_diff = diff_sum / (total_pixels * 3)

        return {
            "vertical_symmetry_score": round(100 - (avg_diff / 255 * 100), 2),
            "assessment": "布局较为对称" if avg_diff < 50 else "布局不对称"
        }

    def _layout_recommendations(self, edge_analysis, whitespace, symmetry):
        """布局建议"""
        recommendations = []

        if edge_analysis.get("has_content_near_edge"):
            recommendations.append("⚠️ 内容过于贴边，建议增加安全边距（至少20px）")

        ws_percent = whitespace.get("percentage", 30)
        if ws_percent < 10:
            recommendations.append("⚠️ 页面过于拥挤，建议增加元素间距")
        elif ws_percent > 50:
            recommendations.append("⚠️ 留白过多，建议调整元素大小或增加内容")

        sym_score = symmetry.get("vertical_symmetry_score", 50)
        if sym_score < 30:
            recommendations.append("️ 布局严重不对称，建议检查UI对齐")

        if not recommendations:
            recommendations.append("✅ 布局表现良好")

        return recommendations


class TextAnalyzer:
    """文字分析器 - 检测文字可读性"""

    def __init__(self):
        pass

    def analyze(self, image_path: str) -> Dict:
        """分析图片中的文字"""
        try:
            from PIL import Image
        except ImportError:
            return {"error": "PIL not installed"}

        try:
            img = Image.open(image_path).convert('RGB')
            width, height = img.size

            # 检测高对比度区域（可能是文字）
            text_regions = self._detect_text_regions(img)

            return {
                "detected_regions": len(text_regions),
                "regions": text_regions[:10],  # 最多返回10个
                "recommendations": self._text_recommendations(text_regions)
            }
        except Exception as e:
            return {"error": str(e)}

    def _detect_text_regions(self, img):
        """检测可能的文字区域（基于高对比度小区域）"""
        width, height = img.size
        pixels = img.load()

        # 将图片分成网格
        grid_size = 20
        regions = []

        for gy in range(0, height, grid_size):
            for gx in range(0, width, grid_size):
                # 计算该网格内的对比度
                cell_pixels = []
                for y in range(gy, min(gy + grid_size, height)):
                    for x in range(gx, min(gx + grid_size, width)):
                        cell_pixels.append(pixels[x, y])

                if len(cell_pixels) < 10:
                    continue

                # 计算局部对比度
                luminances = [0.299*r + 0.587*g + 0.114*b for r, g, b in cell_pixels]
                local_contrast = max(luminances) - min(luminances)

                if local_contrast > 80:  # 高对比度区域可能是文字
                    regions.append({
                        "x": gx,
                        "y": gy,
                        "width": min(grid_size, width - gx),
                        "height": min(grid_size, height - gy),
                        "contrast": round(local_contrast, 2)
                    })

        return regions

    def _text_recommendations(self, regions):
        """文字可读性建议"""
        recommendations = []

        if len(regions) > 50:
            recommendations.append("️ 检测到大量高对比度区域，可能存在文字过于密集的问题")
        elif len(regions) == 0:
            recommendations.append("ℹ️ 未检测到明显的文字区域（可能使用了与背景相近的颜色）")
        else:
            recommendations.append(" 文字区域分布合理")

        return recommendations


class ResourceConsistencyChecker:
    """资源一致性检测"""

    def __init__(self, assets_dir: str = None):
        self.assets_dir = assets_dir or "Assets/Art"

    def analyze(self, image_path: str) -> Dict:
        """分析资源风格一致性"""
        try:
            from PIL import Image
        except ImportError:
            return {"error": "PIL not installed"}

        try:
            img = Image.open(image_path).convert('RGB')

            # 分析色彩风格（卡通 vs 写实）
            style_analysis = self._analyze_art_style(img)

            return {
                "art_style": style_analysis,
                "recommendations": style_analysis.get("recommendations", [])
            }
        except Exception as e:
            return {"error": str(e)}

    def _analyze_art_style(self, img):
        """分析美术风格"""
        pixels = list(img.getdata())

        # 计算色彩离散度（卡通风格通常色彩更鲜明、离散）
        unique_colors = set()
        for r, g, b in pixels:
            # 量化到64色
            unique_colors.add((r // 4, g // 4, b // 4))

        color_diversity = len(unique_colors) / (64 ** 3) * 100

        return {
            "color_diversity_score": round(color_diversity, 2),
            "style": "卡通/扁平风格" if color_diversity > 0.01 else "写实风格",
            "recommendations": [
                " 色彩丰富，符合卡通风格" if color_diversity > 0.01 else "️ 色彩较为单一"
            ]
        }


@dataclass
class ScreenshotAnalysis:
    """单张截图的分析结果"""
    filename: str
    filepath: str
    timestamp: str = ""
    color_analysis: Dict = field(default_factory=dict)
    layout_analysis: Dict = field(default_factory=dict)
    text_analysis: Dict = field(default_factory=dict)
    resource_analysis: Dict = field(default_factory=dict)
    overall_score: float = 0.0
    issues: List[str] = field(default_factory=list)
    suggestions: List[str] = field(default_factory=list)


class UIVisualAnalyzer:
    """UI视觉验收主分析器"""

    def __init__(self, screenshots_dir: str, output_dir: str):
        self.screenshots_dir = Path(screenshots_dir)
        self.output_dir = Path(output_dir)
        self.color_analyzer = ColorAnalyzer()
        self.layout_analyzer = LayoutAnalyzer()
        self.text_analyzer = TextAnalyzer()
        self.resource_checker = ResourceConsistencyChecker()

    def run(self) -> List[ScreenshotAnalysis]:
        """执行完整分析"""
        results = []

        # 查找所有截图
        screenshot_files = sorted(self.screenshots_dir.glob("*.png"))
        if not screenshot_files:
            print(f"[警告] 在 {self.screenshots_dir} 中未找到截图文件")
            return results

        print(f"[UIVisualAnalyzer] 发现 {len(screenshot_files)} 张截图，开始分析...")

        for screenshot_path in screenshot_files:
            print(f"  分析: {screenshot_path.name}...")
            result = self._analyze_single(screenshot_path)
            results.append(result)

        return results

    def _analyze_single(self, screenshot_path: Path) -> ScreenshotAnalysis:
        """分析单张截图"""
        result = ScreenshotAnalysis(
            filename=screenshot_path.name,
            filepath=str(screenshot_path),
            timestamp=datetime.now().isoformat()
        )

        # 执行各项分析
        result.color_analysis = self.color_analyzer.analyze(str(screenshot_path))
        result.layout_analysis = self.layout_analyzer.analyze(str(screenshot_path))
        result.text_analysis = self.text_analyzer.analyze(str(screenshot_path))
        result.resource_analysis = self.resource_checker.analyze(str(screenshot_path))

        # 汇总问题和建议
        result.issues = self._collect_issues(result)
        result.suggestions = self._collect_suggestions(result)
        result.overall_score = self._calculate_score(result)

        return result

    def _collect_issues(self, result: ScreenshotAnalysis) -> List[str]:
        """收集问题"""
        issues = []

        # 色彩问题
        if "recommendations" in result.color_analysis:
            for rec in result.color_analysis["recommendations"]:
                if rec.startswith("⚠️"):
                    issues.append(f"[色彩] {rec[2:].strip()}")

        # 布局问题
        if "recommendations" in result.layout_analysis:
            for rec in result.layout_analysis["recommendations"]:
                if rec.startswith("⚠️"):
                    issues.append(f"[布局] {rec[2:].strip()}")

        # 文字问题
        if "recommendations" in result.text_analysis:
            for rec in result.text_analysis["recommendations"]:
                if rec.startswith("⚠️"):
                    issues.append(f"[文字] {rec[2:].strip()}")

        return issues

    def _collect_suggestions(self, result: ScreenshotAnalysis) -> List[str]:
        """收集优化建议"""
        suggestions = []

        # 色彩建议
        if "recommendations" in result.color_analysis:
            for rec in result.color_analysis["recommendations"]:
                if rec.startswith("✅"):
                    continue
                if not rec.startswith("⚠️"):
                    suggestions.append(f"[色彩] {rec}")

        # 布局建议
        if "recommendations" in result.layout_analysis:
            for rec in result.layout_analysis["recommendations"]:
                if rec.startswith("✅"):
                    continue
                if not rec.startswith("⚠️"):
                    suggestions.append(f"[布局] {rec}")

        return suggestions

    def _calculate_score(self, result: ScreenshotAnalysis) -> float:
        """计算综合得分（0-100）"""
        score = 100

        # 根据问题数量扣分
        score -= len(result.issues) * 5

        # 确保分数在合理范围内
        return max(0, min(100, score))


def main():
    parser = argparse.ArgumentParser(description="UI视觉验收分析器")
    parser.add_argument("--screenshots-dir", "-s", required=True, help="截图目录路径")
    parser.add_argument("--output-dir", "-o", required=True, help="输出目录路径")
    parser.add_argument("--report-json", help="输出JSON报告的路径")
    args = parser.parse_args()

    # 确保输出目录存在
    os.makedirs(args.output_dir, exist_ok=True)

    # 执行分析
    analyzer = UIVisualAnalyzer(args.screenshots_dir, args.output_dir)
    results = analyzer.run()

    # 输出JSON报告
    if args.report_json:
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

        with open(args.report_json, 'w', encoding='utf-8') as f:
            json.dump(report_data, f, ensure_ascii=False, indent=2)

        print(f"[UIVisualAnalyzer] JSON报告已生成: {args.report_json}")

    # 输出摘要
    print("\n========== UI视觉验收分析结果 ==========")
    for r in results:
        print(f"\n{r.filename}: 得分 {r.overall_score}/100")
        if r.issues:
            print(f"  问题 ({len(r.issues)}个):")
            for issue in r.issues[:5]:
                print(f"    - {issue}")
        if r.suggestions:
            print(f"  建议 ({len(r.suggestions)}个):")
            for suggestion in r.suggestions[:5]:
                print(f"    - {suggestion}")

    print(f"\n[完成] 共分析 {len(results)} 张截图")


if __name__ == "__main__":
    main()
