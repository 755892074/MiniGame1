#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
AI视觉分析器 - 支持接入Kimi等多模态模型
对截图进行深度UI分析，识别布局、色彩、文字可读性等问题

用法：
    # 单图分析
    python ai_visual_analyzer.py --image ./screenshots/scene_MenuScene.png
    
    # 批量分析所有截图
    python ai_visual_analyzer.py --screenshots-dir ./screenshots --output ./ai_analysis.json
    
    # 使用特定模型
    python ai_visual_analyzer.py --image ./test.png --model kimi --api-key YOUR_API_KEY
"""

import os
import sys
import json
import base64
import argparse
import requests
from pathlib import Path
from datetime import datetime
from typing import List, Dict, Optional
from dataclasses import dataclass, field, asdict


@dataclass
class AIAnalysisResult:
    """AI分析结果"""
    filename: str = ""
    timestamp: str = ""
    model_used: str = ""
    
    # 布局分析
    layout_score: float = 0.0  # 0-100
    layout_issues: List[str] = field(default_factory=list)
    
    # 色彩分析
    color_score: float = 0.0
    color_issues: List[str] = field(default_factory=list)
    color_recommendations: List[str] = field(default_factory=list)
    
    # 文字分析
    text_readability: float = 0.0  # 0-100
    text_issues: List[str] = field(default_factory=list)
    
    # 资源一致性
    resource_consistency: float = 0.0
    resource_issues: List[str] = field(default_factory=list)
    
    # 综合
    overall_score: float = 0.0
    critical_issues: List[str] = field(default_factory=list)
    optimization_suggestions: List[str] = field(default_factory=list)
    raw_analysis: str = ""  # AI原始回复


class KimiAnalyzer:
    """Kimi多模态分析器"""
    
    API_URL = "https://api.moonshot.cn/v1/chat/completions"
    
    def __init__(self, api_key: str = None, model: str = "kimi-latest"):
        self.api_key = api_key or os.getenv("KIMI_API_KEY")
        if not self.api_key:
            raise ValueError("Kimi API Key未设置。请设置环境变量KIMI_API_KEY或通过--api-key参数传入")
        self.model = model
        
        # UI视觉验收分析提示词
        self.prompt_template = """你是一位资深的UI/UX设计专家和游戏美术总监，请对这张游戏界面截图进行专业的视觉验收分析。

请从以下几个维度进行详细分析，并给出具体的分数（0-100）和建议：

## 1. 布局与构图 (Layout & Composition)
- 元素排列是否合理？是否有对齐问题？
- 间距是否均匀？是否有过于拥挤或过于稀疏的区域？
- 视觉重心是否在合适的位置？
- 安全边距是否足够？

## 2. 色彩搭配 (Color & Visual Design)
- 主色调是否和谐？是否有突兀的颜色？
- 文字与背景的对比度是否足够？
- 色彩层次是否清晰？
- 是否符合2D卡通/休闲游戏的视觉风格？

## 3. 文字可读性 (Typography & Readability)
- 文字大小是否合适？是否有过大或过小的文字？
- 字体是否统一？
- 文字颜色与背景的对比度是否足够？
- 是否有文字被遮挡或显示不全？

## 4. 资源一致性 (Asset Consistency)
- 各种UI元素的视觉风格是否统一？
- 图标、按钮、背景等资源是否风格一致？
- 是否有明显的分辨率或清晰度差异？
- 是否有过时或风格不符的资源？

## 5. 交互元素 (Interactive Elements)
- 按钮大小是否合适？是否容易点击？
- 交互元素的视觉反馈是否清晰？
- 是否有足够的触控区域？

请按以下格式输出分析结果（JSON格式）：

```json
{{
  "layout_score": 85,
  "layout_issues": ["元素A未对齐", "间距不均匀"],
  "color_score": 78,
  "color_issues": ["主色调过于饱和", "对比度不足"],
  "color_recommendations": ["建议使用更柔和的主色", "增加文字对比度"],
  "text_readability": 82,
  "text_issues": ["小字难以辨认"],
  "resource_consistency": 75,
  "resource_issues": ["按钮风格不统一"],
  "overall_score": 80,
  "critical_issues": ["文字对比度不足影响可读性"],
  "optimization_suggestions": ["统一按钮风格", "增加文字大小"]
}}
```

请确保分析专业、具体、可操作。分数要客观，不要全是高分。"""

    def analyze_image(self, image_path: str) -> AIAnalysisResult:
        """分析单张图片"""
        # 读取图片并编码为base64
        with open(image_path, 'rb') as f:
            image_data = base64.b64encode(f.read()).decode('utf-8')
        
        # 构建请求
        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        }
        
        payload = {
            "model": self.model,
            "messages": [
                {
                    "role": "user",
                    "content": [
                        {"type": "text", "text": self.prompt_template},
                        {
                            "type": "image_url",
                            "image_url": {
                                "url": f"data:image/png;base64,{image_data}"
                            }
                        }
                    ]
                }
            ],
            "temperature": 0.3,
            "max_tokens": 2000
        }
        
        # 发送请求
        response = requests.post(self.API_URL, headers=headers, json=payload, timeout=60)
        response.raise_for_status()
        
        result = response.json()
        analysis_text = result["choices"][0]["message"]["content"]
        
        # 解析JSON结果
        return self._parse_result(image_path, analysis_text)
    
    def _parse_result(self, image_path: str, analysis_text: str) -> AIAnalysisResult:
        """解析AI分析结果"""
        import re
        
        # 尝试从文本中提取JSON
        json_match = re.search(r'```json\n(.*?)\n```', analysis_text, re.DOTALL)
        if not json_match:
            json_match = re.search(r'\{.*?\}', analysis_text, re.DOTALL)
        
        result = AIAnalysisResult(
            filename=Path(image_path).name,
            timestamp=datetime.now().isoformat(),
            model_used=self.model,
            raw_analysis=analysis_text
        )
        
        if json_match:
            try:
                data = json.loads(json_match.group(1) if json_match.group(1) else json_match.group(0))
                result.layout_score = data.get("layout_score", 0)
                result.layout_issues = data.get("layout_issues", [])
                result.color_score = data.get("color_score", 0)
                result.color_issues = data.get("color_issues", [])
                result.color_recommendations = data.get("color_recommendations", [])
                result.text_readability = data.get("text_readability", 0)
                result.text_issues = data.get("text_issues", [])
                result.resource_consistency = data.get("resource_consistency", 0)
                result.resource_issues = data.get("resource_issues", [])
                result.overall_score = data.get("overall_score", 0)
                result.critical_issues = data.get("critical_issues", [])
                result.optimization_suggestions = data.get("optimization_suggestions", [])
            except json.JSONDecodeError:
                pass
        
        return result


def analyze_all_screenshots(screenshots_dir: str, api_key: str, model: str = "kimi-latest") -> List[AIAnalysisResult]:
    """批量分析所有截图"""
    analyzer = KimiAnalyzer(api_key=api_key, model=model)
    
    results = []
    screenshot_path = Path(screenshots_dir)
    
    if not screenshot_path.exists():
        print(f"[错误] 截图目录不存在: {screenshots_dir}")
        return results
    
    screenshots = list(screenshot_path.glob("*.png"))
    
    if not screenshots:
        print(f"[警告] 未找到截图文件")
        return results
    
    print(f"[AI分析] 共发现 {len(screenshots)} 张截图")
    print(f"[AI分析] 使用模型: {model}")
    print()
    
    for i, screenshot in enumerate(screenshots, 1):
        print(f"  [{i}/{len(screenshots)}] 分析: {screenshot.name}...")
        try:
            result = analyzer.analyze_image(str(screenshot))
            results.append(result)
            print(f"         得分: {result.overall_score}/100")
        except Exception as e:
            print(f"         失败: {e}")
            # 添加一个空结果
            results.append(AIAnalysisResult(
                filename=screenshot.name,
                timestamp=datetime.now().isoformat(),
                model_used=model,
                overall_score=0,
                raw_analysis=f"分析失败: {e}"
            ))
    
    return results


def save_results(results: List[AIAnalysisResult], output_path: str):
    """保存分析结果到JSON"""
    data = [asdict(r) for r in results]
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    print(f"[保存] 结果已保存: {output_path}")


def print_summary(results: List[AIAnalysisResult]):
    """打印分析摘要"""
    print("\n" + "=" * 60)
    print("           AI视觉验收分析摘要")
    print("=" * 60)
    
    if not results:
        print("  无分析结果")
        return
    
    total_score = sum(r.overall_score for r in results)
    avg_score = total_score / len(results) if results else 0
    
    print(f"\n   平均得分: {avg_score:.1f}/100")
    print(f"  📸 分析截图: {len(results)} 张")
    
    # 问题统计
    total_critical = sum(len(r.critical_issues) for r in results)
    total_suggestions = sum(len(r.optimization_suggestions) for r in results)
    
    print(f"  ️  关键问题: {total_critical} 个")
    print(f"  💡 优化建议: {total_suggestions} 个")
    
    print("\n  各界面得分:")
    for r in results:
        status = "✅" if r.overall_score >= 80 else "⚠️" if r.overall_score >= 60 else ""
        print(f"    {status} {r.filename}: {r.overall_score}/100")
    
    # 列出关键问题
    if total_critical > 0:
        print("\n  🔴 关键问题:")
        for r in results:
            for issue in r.critical_issues:
                print(f"    - [{r.filename}] {issue}")
    
    # 列出优化建议
    if total_suggestions > 0:
        print("\n   优化建议:")
        for r in results:
            for suggestion in r.optimization_suggestions[:3]:  # 每个最多显示3条
                print(f"    - [{r.filename}] {suggestion}")
    
    print("\n" + "=" * 60)


def main():
    parser = argparse.ArgumentParser(description="AI视觉验收分析器 (Kimi)")
    parser.add_argument("--image", "-i", help="单张图片路径")
    parser.add_argument("--screenshots-dir", "-d", help="截图目录")
    parser.add_argument("--output", "-o", help="输出JSON文件路径")
    parser.add_argument("--api-key", "-k", help="API Key (或设置KIMI_API_KEY环境变量)")
    parser.add_argument("--model", "-m", default="kimi-latest", help="模型名称 (默认: kimi-latest)")
    args = parser.parse_args()
    
    # 检查API Key
    api_key = args.api_key or os.getenv("KIMI_API_KEY")
    if not api_key:
        print("[错误] 未设置API Key")
        print("  请设置环境变量 KIMI_API_KEY")
        print("  或通过 --api-key 参数传入")
        return
    
    # 单图分析
    if args.image:
        print(f"[AI分析] 分析图片: {args.image}")
        analyzer = KimiAnalyzer(api_key=api_key, model=args.model)
        result = analyzer.analyze_image(args.image)
        
        print(f"\n{'='*60}")
        print(f"  文件: {result.filename}")
        print(f"  综合得分: {result.overall_score}/100")
        print(f"  布局得分: {result.layout_score}/100")
        print(f"  色彩得分: {result.color_score}/100")
        print(f"  文字可读性: {result.text_readability}/100")
        print(f"  资源一致性: {result.resource_consistency}/100")
        print(f"{'='*60}\n")
        
        if result.critical_issues:
            print("🔴 关键问题:")
            for issue in result.critical_issues:
                print(f"  - {issue}")
        
        if result.optimization_suggestions:
            print("\n💡 优化建议:")
            for suggestion in result.optimization_suggestions:
                print(f"  - {suggestion}")
        
        # 保存结果
        if args.output:
            save_results([result], args.output)
    
    # 批量分析
    elif args.screenshots_dir:
        results = analyze_all_screenshots(args.screenshots_dir, api_key, args.model)
        print_summary(results)
        
        if args.output:
            save_results(results, args.output)
    
    else:
        print("请指定 --image 或 --screenshots-dir 参数")
        print("使用 --help 查看完整用法")


if __name__ == "__main__":
    main()
