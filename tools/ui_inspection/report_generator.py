#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
UI视觉验收报告生成器
生成美观的HTML报告，包含：
- 截图对比
- 色彩分析图表
- 布局分析
- 问题列表
- 优化建议

用法：
    python report_generator.py --input ./analysis_results.json --output ./report.html
"""

import json
import os
import sys
from pathlib import Path
from datetime import datetime
from typing import List, Dict, Optional


class ReportGenerator:
    """HTML报告生成器"""

    def __init__(self, template_dir: Optional[str] = None):
        self.template_dir = Path(template_dir) if template_dir else Path(__file__).parent

    def generate(self, analysis_data: List[Dict], output_path: str, screenshot_dir: str = None):
        """生成HTML报告"""
        html = self._build_html(analysis_data, screenshot_dir)

        # 确保输出目录存在
        os.makedirs(os.path.dirname(output_path) or ".", exist_ok=True)

        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(html)

        return output_path

    def _build_html(self, data: List[Dict], screenshot_dir: str = None) -> str:
        """构建完整HTML"""
        total_screenshots = len(data)
        total_issues = sum(len(item.get("issues", [])) for item in data)
        total_suggestions = sum(len(item.get("suggestions", [])) for item in data)

        # 计算平均分
        avg_score = 0
        if total_screenshots > 0:
            avg_score = sum(item.get("overall_score", 0) for item in data) / total_screenshots

        # 构建截图卡片
        screenshot_cards = ""
        for item in data:
            screenshot_cards += self._build_screenshot_card(item, screenshot_dir)

        html = f"""<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>铲屎官疯了 - UI视觉验收报告</title>
    <style>
        :root {{
            --primary: #FF8C42;
            --primary-dark: #E67E22;
            --secondary: #4ECDC4;
            --bg-dark: #1a1a2e;
            --bg-card: #16213e;
            --bg-light: #0f3460;
            --text: #e0e0e0;
            --text-muted: #a0a0a0;
            --success: #4CAF50;
            --warning: #FF9800;
            --danger: #F44336;
            --border: rgba(255,255,255,0.1);
        }}

        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
            background: var(--bg-dark);
            color: var(--text);
            line-height: 1.6;
        }}

        .header {{
            background: linear-gradient(135deg, var(--bg-card) 0%, var(--bg-light) 100%);
            padding: 40px 0;
            text-align: center;
            border-bottom: 1px solid var(--border);
        }}

        .header h1 {{
            font-size: 2.5em;
            margin-bottom: 10px;
            background: linear-gradient(135deg, var(--primary) 0%, var(--secondary) 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
        }}

        .header .subtitle {{
            color: var(--text-muted);
            font-size: 1.1em;
        }}

        .summary {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            padding: 30px;
            max-width: 1200px;
            margin: 0 auto;
        }}

        .summary-card {{
            background: var(--bg-card);
            border-radius: 16px;
            padding: 24px;
            text-align: center;
            border: 1px solid var(--border);
            transition: transform 0.2s;
        }}

        .summary-card:hover {{
            transform: translateY(-2px);
        }}

        .summary-card .number {{
            font-size: 2.5em;
            font-weight: bold;
            margin-bottom: 8px;
        }}

        .summary-card .label {{
            color: var(--text-muted);
            font-size: 0.9em;
        }}

        .score-good {{ color: var(--success); }}
        .score-warning {{ color: var(--warning); }}
        .score-danger {{ color: var(--danger); }}

        .content {{
            max-width: 1200px;
            margin: 0 auto;
            padding: 0 30px 30px;
        }}

        .screenshot-section {{
            margin-bottom: 40px;
        }}

        .section-title {{
            font-size: 1.5em;
            margin-bottom: 20px;
            padding-bottom: 10px;
            border-bottom: 2px solid var(--primary);
            display: inline-block;
        }}

        .screenshot-card {{
            background: var(--bg-card);
            border-radius: 16px;
            overflow: hidden;
            border: 1px solid var(--border);
            margin-bottom: 24px;
        }}

        .screenshot-header {{
            padding: 16px 24px;
            background: rgba(255,255,255,0.03);
            border-bottom: 1px solid var(--border);
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}

        .screenshot-header h3 {{
            font-size: 1.1em;
            font-weight: 600;
        }}

        .screenshot-header .score-badge {{
            padding: 4px 16px;
            border-radius: 20px;
            font-size: 0.85em;
            font-weight: bold;
        }}

        .screenshot-body {{
            display: grid;
            grid-template-columns: 300px 1fr;
            gap: 24px;
            padding: 24px;
        }}

        @media (max-width: 768px) {{
            .screenshot-body {{
                grid-template-columns: 1fr;
            }}
        }}

        .screenshot-preview {{
            background: rgba(0,0,0,0.3);
            border-radius: 12px;
            overflow: hidden;
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 200px;
        }}

        .screenshot-preview img {{
            max-width: 100%;
            max-height: 500px;
            object-fit: contain;
            display: block;
        }}

        .screenshot-preview .placeholder {{
            color: var(--text-muted);
            text-align: center;
            padding: 40px;
        }}

        .analysis-details {{
            display: flex;
            flex-direction: column;
            gap: 16px;
        }}

        .detail-section {{
            background: rgba(255,255,255,0.03);
            border-radius: 12px;
            padding: 16px;
        }}

        .detail-section h4 {{
            font-size: 0.9em;
            color: var(--text-muted);
            margin-bottom: 12px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }}

        .color-palette {{
            display: flex;
            gap: 8px;
            flex-wrap: wrap;
        }}

        .color-swatch {{
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 4px;
        }}

        .color-swatch .swatch {{
            width: 40px;
            height: 40px;
            border-radius: 8px;
            border: 2px solid rgba(255,255,255,0.1);
        }}

        .color-swatch .hex {{
            font-size: 0.75em;
            color: var(--text-muted);
        }}

        .metrics-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 12px;
        }}

        .metric {{
            background: rgba(255,255,255,0.05);
            padding: 12px;
            border-radius: 8px;
            text-align: center;
        }}

        .metric .value {{
            font-size: 1.5em;
            font-weight: bold;
            color: var(--primary);
        }}

        .metric .label {{
            font-size: 0.8em;
            color: var(--text-muted);
            margin-top: 4px;
        }}

        .issues-list {{
            list-style: none;
        }}

        .issues-list li {{
            padding: 8px 0;
            border-bottom: 1px solid rgba(255,255,255,0.05);
            display: flex;
            align-items: flex-start;
            gap: 8px;
        }}

        .issues-list li:last-child {{
            border-bottom: none;
        }}

        .issues-list .issue-icon {{
            color: var(--danger);
            flex-shrink: 0;
        }}

        .suggestions-list {{
            list-style: none;
        }}

        .suggestions-list li {{
            padding: 8px 0;
            border-bottom: 1px solid rgba(255,255,255,0.05);
            display: flex;
            align-items: flex-start;
            gap: 8px;
        }}

        .suggestions-list .suggestion-icon {{
            color: var(--secondary);
            flex-shrink: 0;
        }}

        .no-issues {{
            color: var(--success);
            text-align: center;
            padding: 16px;
        }}

        .footer {{
            text-align: center;
            padding: 40px;
            color: var(--text-muted);
            border-top: 1px solid var(--border);
            margin-top: 40px;
        }}

        .collapsible {{ cursor: pointer; }}
        .collapsible:hover {{ opacity: 0.8; }}
    </style>
</head>
<body>
    <div class="header">
        <h1>🎮 铲屎官疯了</h1>
        <p class="subtitle">UI视觉验收报告</p>
        <p style="color: var(--text-muted); margin-top: 8px;">生成时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}</p>
    </div>

    <div class="summary">
        <div class="summary-card">
            <div class="number">{total_screenshots}</div>
            <div class="label">截图数量</div>
        </div>
        <div class="summary-card">
            <div class="number {'score-good' if avg_score >= 80 else 'score-warning' if avg_score >= 60 else 'score-danger'}">{avg_score:.1f}</div>
            <div class="label">平均得分</div>
        </div>
        <div class="summary-card">
            <div class="number score-danger">{total_issues}</div>
            <div class="label">发现问题</div>
        </div>
        <div class="summary-card">
            <div class="number score-warning">{total_suggestions}</div>
            <div class="label">优化建议</div>
        </div>
    </div>

    <div class="content">
        <h2 class="section-title">📸 截图详情</h2>
        {screenshot_cards}
    </div>

    <div class="footer">
        <p>UI视觉验收自动测试工具 · 铲屎官疯了</p>
        <p style="font-size: 0.85em; margin-top: 8px;">本报告由自动化工具生成，仅供参考</p>
    </div>

    <script>
        // 简单的图片加载错误处理
        document.querySelectorAll('img').forEach(img => {{
            img.onerror = function() {{
                this.style.display = 'none';
                this.parentElement.innerHTML = '<div class="placeholder">图片加载失败<br><small>' + this.alt + '</small></div>';
            }}
        }});
    </script>
</body>
</html>"""
        return html

    def _build_screenshot_card(self, item: Dict, screenshot_dir: str = None) -> str:
        """构建单个截图的分析卡片"""
        filename = item.get("filename", "unknown.png")
        score = item.get("overall_score", 0)
        score_class = "score-good" if score >= 80 else "score-warning" if score >= 60 else "score-danger"

        # 截图路径
        img_path = item.get("filepath", "")
        if screenshot_dir and not os.path.isabs(img_path):
            img_path = os.path.join(screenshot_dir, filename)

        # 色彩分析
        color_html = self._build_color_section(item.get("color_analysis", {}))
        layout_html = self._build_layout_section(item.get("layout_analysis", {}))
        issues_html = self._build_issues_section(item.get("issues", []))
        suggestions_html = self._build_suggestions_section(item.get("suggestions", []))

        return f"""
        <div class="screenshot-card">
            <div class="screenshot-header">
                <h3>📱 {filename}</h3>
                <span class="score-badge {score_class}">得分: {score:.0f}/100</span>
            </div>
            <div class="screenshot-body">
                <div class="screenshot-preview">
                    <img src="file:///{img_path.replace('\\', '/')}" alt="{filename}" loading="lazy">
                </div>
                <div class="analysis-details">
                    {color_html}
                    {layout_html}
                    {issues_html}
                    {suggestions_html}
                </div>
            </div>
        </div>
        """

    def _build_color_section(self, color_data: Dict) -> str:
        """构建色彩分析部分"""
        if not color_data or "error" in color_data:
            error_msg = color_data.get("error", "无法分析") if isinstance(color_data, dict) else "无法分析"
            return f"""
            <div class="detail-section">
                <h4>🎨 色彩分析</h4>
                <p style="color: var(--text-muted)">{error_msg}</p>
            </div>
            """

        dominant = color_data.get("dominant_colors", [])
        contrast = color_data.get("contrast", 0)
        brightness = color_data.get("brightness", {})

        # 构建色板
        color_swatch_html = ""
        for color in dominant[:5]:
            hex_color = color.get("hex", "#000000")
            freq = color.get("frequency", 0)
            color_swatch_html += f"""
            <div class="color-swatch">
                <div class="swatch" style="background: {hex_color};"></div>
                <span class="hex">{hex_color}</span>
                <span class="hex">{freq}%</span>
            </div>
            """

        return f"""
        <div class="detail-section">
            <h4>🎨 色彩分析</h4>
            <div class="color-palette">{color_swatch_html}</div>
            <div class="metrics-grid" style="margin-top: 12px;">
                <div class="metric">
                    <div class="value">{contrast:.1f}</div>
                    <div class="label">对比度</div>
                </div>
                <div class="metric">
                    <div class="value">{brightness.get('mean', 'N/A'):.1f}</div>
                    <div class="label">平均亮度</div>
                </div>
                <div class="metric">
                    <div class="value">{brightness.get('assessment', 'N/A')}</div>
                    <div class="label">亮度评估</div>
                </div>
            </div>
        </div>
        """

    def _build_layout_section(self, layout_data: Dict) -> str:
        """构建布局分析部分"""
        if not layout_data or "error" in layout_data:
            error_msg = layout_data.get("error", "无法分析") if isinstance(layout_data, dict) else "无法分析"
            return f"""
            <div class="detail-section">
                <h4>📐 布局分析</h4>
                <p style="color: var(--text-muted)">{error_msg}</p>
            </div>
            """

        dimensions = layout_data.get("dimensions", {})
        whitespace = layout_data.get("whitespace", {})
        symmetry = layout_data.get("symmetry", {})

        return f"""
        <div class="detail-section">
            <h4>📐 布局分析</h4>
            <div class="metrics-grid">
                <div class="metric">
                    <div class="value">{dimensions.get('width', '?')}x{dimensions.get('height', '?')}</div>
                    <div class="label">分辨率</div>
                </div>
                <div class="metric">
                    <div class="value">{whitespace.get('percentage', 'N/A'):.1f}%</div>
                    <div class="label">留白占比</div>
                </div>
                <div class="metric">
                    <div class="value">{symmetry.get('vertical_symmetry_score', 'N/A'):.1f}</div>
                    <div class="label">对称性</div>
                </div>
            </div>
        </div>
        """

    def _build_issues_section(self, issues: List[str]) -> str:
        """构建问题列表部分"""
        if not issues:
            return """
            <div class="detail-section">
                <h4>⚠️ 问题列表</h4>
                <div class="no-issues">✅ 未发现问题</div>
            </div>
            """

        issues_html = ""
        for issue in issues:
            issues_html += f"<li><span class=\"issue-icon\">🔴</span>{issue}</li>"

        return f"""
        <div class="detail-section">
            <h4>⚠️ 问题列表 ({len(issues)}个)</h4>
            <ul class="issues-list">
                {issues_html}
            </ul>
        </div>
        """

    def _build_suggestions_section(self, suggestions: List[str]) -> str:
        """构建建议列表部分"""
        if not suggestions:
            return """
            <div class="detail-section">
                <h4> 优化建议</h4>
                <div class="no-issues">✅ 无需优化</div>
            </div>
            """

        suggestions_html = ""
        for suggestion in suggestions:
            suggestions_html += f"<li><span class=\"suggestion-icon\">💡</span>{suggestion}</li>"

        return f"""
        <div class="detail-section">
            <h4>💡 优化建议 ({len(suggestions)}个)</h4>
            <ul class="suggestions-list">
                {suggestions_html}
            </ul>
        </div>
        """


def main():
    import argparse
    parser = argparse.ArgumentParser(description="UI视觉验收报告生成器")
    parser.add_argument("--input", "-i", required=True, help="JSON分析结果路径")
    parser.add_argument("--output", "-o", required=True, help="HTML报告输出路径")
    parser.add_argument("--screenshot-dir", "-s", help="截图目录（用于图片引用）")
    args = parser.parse_args()

    # 读取分析结果
    with open(args.input, 'r', encoding='utf-8') as f:
        data = json.load(f)

    # 生成报告
    generator = ReportGenerator()
    output_path = generator.generate(data, args.output, args.screenshot_dir)

    print(f"[ReportGenerator] HTML报告已生成: {output_path}")
    return output_path


if __name__ == "__main__":
    main()
