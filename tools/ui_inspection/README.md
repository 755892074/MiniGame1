# 🎮 铲屎官疯了 - UI视觉验收自动测试方案

一套完整的自动化UI视觉验收工具，支持自动截图、AI视觉分析、生成验收报告。

## 功能特性

- **🖼️ 自动截图**: Unity编辑器一键截取所有游戏界面
- **🎨 色彩分析**: 提取主色调、分析对比度、亮度、饱和度
- **📐 布局分析**: 检测元素对齐、间距、留白、对称性
- **🔤 文字可读性**: 评估文字对比度、大小、可读性
- **📊 AI深度分析**: 支持Kimi多模态模型深度分析
- **📋 验收报告**: 生成美观的HTML报告，含截图对比和优化建议

## 文件结构

```
tools/ui_inspection/
├── README.md                   # 本文档
├── ui_analyzer.py             # 基础视觉分析器 (PIL分析)
├── ai_visual_analyzer.py      # AI深度分析器 (Kimi多模态)
├── report_generator.py        # HTML报告生成器
├── run_ui_inspection.py       # 一键执行脚本
├── screenshots/               # 截图输出目录 (自动生成)
└── reports/                   # 报告输出目录 (自动生成)
```

## 使用方式

### 方式一：Unity编辑器内截图

1. 打开Unity编辑器，加载项目
2. 菜单栏 → **铲屎官疯了** → **UI视觉验收** → **执行完整截图**
3. 等待截图完成，结果保存在 `tools/ui_inspection/screenshots/`

### 方式二：命令行一键执行

```bash
# 进入工具目录
cd tools/ui_inspection

# 一键执行（分析已有截图并生成报告）
python run_ui_inspection.py --skip-screenshot

# 完整流程（截图 + 分析 + 报告）
python run_ui_inspection.py

# 指定目录
python run_ui_inspection.py --screenshots-dir ./screenshots --output-dir ./reports
```

### 方式三：AI深度分析（推荐）

```bash
# 设置API Key (Kimi)
export KIMI_API_KEY="your-api-key"

# 单图分析
python ai_visual_analyzer.py --image ./screenshots/scene_MenuScene.png

# 批量分析所有截图
python ai_visual_analyzer.py --screenshots-dir ./screenshots --output ./ai_analysis.json
```

### 方式四：分步执行

```bash
# 步骤1: 截图（在Unity中操作）
# 菜单栏 → 铲屎官疯了 → UI视觉验收 → 执行完整截图

# 步骤2: 基础分析（纯本地，无需API）
python ui_analyzer.py --screenshots-dir ./screenshots --output ./reports

# 步骤3: 生成HTML报告
python report_generator.py --input ./reports/analysis_results.json --output ./reports/ui_inspection_report.html

# 步骤4: AI深度分析（需要Kimi API Key）
python ai_visual_analyzer.py --screenshots-dir ./screenshots --output ./reports/ai_analysis.json
```

## 分析维度

### 1. 布局分析
- **元素对齐**: 检测UI元素是否对齐
- **间距检查**: 元素之间的间距是否均匀
- **安全边距**: 内容是否过于贴边
- **留白分析**: 页面留白比例是否合适
- **对称性**: 布局是否对称

### 2. 色彩分析
- **主色调**: 提取5种主色调
- **对比度**: 文字与背景的对比度
- **亮度**: 整体亮度分布
- **饱和度**: 色彩饱和度分析
- **色彩和谐度**: 暖色/冷色/中性色比例

### 3. 文字可读性
- **对比度**: 文字与背景的对比度
- **文字大小**: 是否过小或过大
- **字体一致性**: 字体是否统一
- **遮挡检测**: 文字是否被遮挡

### 4. 资源一致性
- **风格统一**: 所有UI元素的视觉风格是否一致
- **分辨率一致性**: 是否有模糊或低分辨率资源
- **色彩风格**: 是否保持统一的色彩风格

## 输出报告

执行后会生成以下文件：

```
tools/ui_inspection/reports/
├── ui_inspection_report.html    # HTML视觉验收报告
└── analysis_results.json         # 原始分析数据
```

HTML报告包含：
- 📊 综合得分概览
- 📸 截图详情（含色彩分析图表）
- ⚠️ 问题列表
- 💡 优化建议
- 🎨 色彩分析
- 📐 布局分析

## 环境要求

### Python依赖

```bash
pip install Pillow requests
```

### Unity依赖

- Unity 2022.3+ / Tuanjie 2022.3.62t11+
- 项目需包含 `UIVisualInspector.cs` 脚本

## Kimi API配置

1. 获取API Key: [Kimi开放平台](https://platform.moonshot.cn/)
2. 设置环境变量:
   ```bash
   # Windows
   set KIMI_API_KEY=your-api-key
   
   # macOS/Linux
   export KIMI_API_KEY=your-api-key
   ```

## 截图覆盖范围

工具会自动截取以下界面：

| 场景 | 状态 | 说明 |
|------|------|------|
| BootScene | 启动画面 | 游戏启动时的加载画面 |
| MenuScene | 登录面板 | 用户登录/隐私协议 |
| MenuScene | 主菜单 | 游戏主界面 |
| MenuScene | 设置面板 | 游戏设置界面 |
| MenuScene | 选关面板 | 关卡选择界面 |
| PetGameScene | 游戏进行中 | 核心玩法界面 |
| PetGameScene | 暂停界面 | 暂停菜单 |
| PetGameScene | 胜利结算 | 关卡完成界面 |
| PetGameScene | 失败结算 | 关卡失败界面 |

## 工作流程

```
┌─────────────────────────────────────────────────────────┐
│                    UI视觉验收流程                        │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  1. Unity截图 (UIVisualInspector.cs)                   │
│     ├── 打开各场景                                       │
│     ├── 切换不同UI状态                                   │
│     └── 截取GameView画面                                 │
│                                                         │
│  2. 基础分析 (ui_analyzer.py)                            │
│     ├── 色彩分析                                         │
│     ├── 布局分析                                         │
│     ├── 文字检测                                         │
│     └── 资源一致性检查                                   │
│                                                         │
│  3. AI深度分析 (ai_visual_analyzer.py)                   │
│     ├── 调用Kimi多模态API                                │
│     ├── 专业UI/UX评估                                    │
│     └── 深度问题挖掘                                     │
│                                                         │
│  4. 报告生成 (report_generator.py)                     │
│     ├── 汇总分析结果                                     │
│     ├── 生成HTML报告                                     │
│     └── 输出优化建议                                     │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## 常见问题

### Q1: 截图为空或黑屏？
- 确保Unity编辑器处于激活状态
- 尝试在Game View处于激活状态时截图
- 检查是否有弹窗遮挡

### Q2: AI分析失败？
- 检查API Key是否正确
- 确认网络连接正常
- 检查图片是否过大（建议不超过2MB）

### Q3: 报告中的截图不显示？
- 确保HTML报告与截图在同一台机器上查看
- 截图路径使用绝对路径，跨机器查看会失效

### Q4: 如何添加更多截图场景？
- 编辑 `UIVisualInspector.cs` 中的 `ScenePaths`、`MenuPanelStates`、`GameStates` 数组
- 重新编译Unity项目

## 扩展开发

### 接入其他视觉模型

在 `ai_visual_analyzer.py` 中添加新的分析器：

```python
class GPT4Analyzer:
    """GPT-4V分析器示例"""
    
    def __init__(self, api_key: str):
        self.api_key = api_key
    
    def analyze_image(self, image_path: str) -> AIAnalysisResult:
        # 实现你的分析逻辑
        pass
```

### 自定义分析规则

在 `ui_analyzer.py` 中修改分析器：

```python
class CustomAnalyzer:
    def analyze(self, image_path: str) -> Dict:
        # 添加你的自定义分析逻辑
        pass
```

## 技术栈

- **Unity/C#**: 截图引擎 (`UIVisualInspector.cs`)
- **Python/PIL**: 基础视觉分析 (`ui_analyzer.py`)
- **Python/Requests**: AI API调用 (`ai_visual_analyzer.py`)
- **HTML/CSS**: 报告展示 (`report_generator.py`)

## 更新日志

### v1.0 (2025-07-22)
- 初始版本
- 支持自动截图、基础视觉分析、HTML报告生成
- 支持Kimi多模态AI深度分析
- 支持一键执行和分步执行
