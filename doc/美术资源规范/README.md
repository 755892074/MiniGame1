# 疯狂铲屎官 — 美术资源规范总纲

> 本目录包含所有美术资源的命名规范、尺寸要求、AI生成提示词和自动化工具。
> 用法：按 `prompts/` 里的提示词去 AI 生成 → 产出的图丢到 `_raw/` → 跑 `scripts/` 里的脚本自动切图、命名、放到项目对应路径。

---

## 目录结构

```
美术资源规范/
├── README.md                      ← 你在这里（总规范）
├── prompts/                       ← AI 提示词（按类别分文件）
│   ├── 01_宠物帧动画.md            ← 6只宠物 × 5动作 × 3阶段
│   ├── 02_住所与建筑.md            ← 宠物窝 + 小院建筑
│   ├── 03_场景背景.md              ← 小院/章节封面
│   ├── 04_UI图标与按钮.md          ← 货币/称号/道具图标
│   └── 05_音效说明.md             ← 音效需求（非AI生成）
├── scripts/                       ← 自动化工具
│   ├── spritesheet_cutter.py      ← 切 Sprite Sheet
│   ├── auto_deploy.py             ← 自动命名+部署到项目
│   └── batch_process.py           ← 批量处理（去背景+裁切+重命名）
└── _raw/                          ← AI生成的原始图片（临时目录）
    └── (把AI生成的图丢这里)
```

---

## 项目美术路径对照

| 类别 | 项目路径 | 命名规则 |
|---|---|---|
| 宠物帧动画 | `Assets/Resources/ArtPets/{pet}/{action}/` | `frame_01.png` ~ `frame_06.png` |
| 宠物头像 | `Assets/Resources/PetFaces/{pet}/` | `neutral.png`, `happy.png` 等 |
| 食物图标 | `Assets/Resources/ArtFoods/` | `food01.png` ~ `food29.png` |
| 碗 | `Assets/Art/PetGame/bowls/{empty\|full}/` | `bowl01.png` ~ `bowl08.png` |
| 住所 | `Assets/Resources/ArtHouses/` | `house_lv1.png` ~ `house_lv5.png` |
| 小院建筑 | `Assets/Resources/ArtYard/` | `foodbowl_1.png`, `toy_1.png` 等 |
| 场景背景 | `Assets/Resources/ArtScenes/` | `yard_bg_01.png` 等 |
| UI 图标 | `Assets/Resources/ArtUI/` | `icon_fish.png`, `icon_medal.png` 等 |
| 章节封面 | `Assets/Resources/ArtChapters/` | `chapter_01.png` ~ `chapter_05.png` |

---

## 全局风格锚点

所有美术资源必须遵守以下风格规则，保持视觉一致性：

| 属性 | 规范 |
|---|---|
| **画风** | Q版扁平 (flat design / chibi) |
| **线条** | 粗线条描边 (thick outline, 2-3px) |
| **配色** | 明亮柔和色调 (pastel color palette) |
| **渲染** | 纯平涂，无渐变/无光影 |
| **背景** | 纯白背景 `#FFFFFF`（便于自动去背景） |
| **帧动画尺寸** | 单帧 256×256 px |
| **图标尺寸** | 128×128 px |
| **场景尺寸** | 750×1334 px（竖屏全屏） |
| **帧率** | 8-10 fps |
| **格式** | PNG（带透明通道） |

---

## 6只宠物设定（风格锚点）

每只宠物先生成 1 张「设定图」，后续所有帧动画都以这张为参考。

| 编号 | 宠物 | 名字 | 主色调 | 体型 | 特征 |
|---|---|---|---|---|---|
| 01 | 橘猫 | 小橘 | 橙色 #FF8C00 | 胖圆 | 条纹尾巴、圆脸 |
| 02 | 柴犬 | 阿黄 | 金棕 #D4A017 | 健壮 | 立耳、卷尾 |
| 03 | 仓鼠 | 球球 | 浅棕 #C4A062 | 超圆 | 大腮帮、小耳 |
| 04 | 鹦鹉 | 翠翠 | 翠绿 #2ED573 | 小巧 | 长尾羽、红嘴 |
| 05 | 金鱼 | 泡泡 | 橙红 #FF6348 | 椭圆 | 大眼睛、飘逸尾鳍 |
| 06 | 垂耳兔 | 麻薯 | 灰白 #D8D8D8 | 圆润 | 垂耳、粉鼻 |

---

## 制作流程（3步出图）

```
第1步：生成
  打开 prompts/ 目录下对应的 .md 文件
  复制提示词 → 粘贴到 AI 绘图工具（推荐 Midjourney / DALL-E / Stable Diffusion）
  把生成的图保存到 _raw/ 目录

第2步：处理
  运行 scripts/batch_process.py
  自动：去白底 → 裁切到目标尺寸 → 输出到 _processed/

第3步：部署
  运行 scripts/auto_deploy.py
  自动：按命名规则重命名 → 复制到项目 Assets/ 对应路径
  如果同名文件已存在则覆盖
```

---

## 优先级排序

| 优先级 | 资源 | 数量 | 阻塞 |
|---|---|---|---|
| **P0** | 小院背景 | 1 | 阶段A开发 |
| **P0** | 小橘 idle 4帧 | 4 | 阶段A开发 |
| **P0** | 住所 Lv1-2 图标 | 2 | 阶段A开发 |
| **P1** | 其余5只 idle | 20 | 阶段B |
| **P1** | happy/cry 动画 | 48 | 阶段B |
| **P1** | 住所 Lv3-5 图标 | 3 | 阶段B |
| **P2** | run/cute 动画 | 48 | 阶段B |
| **P2** | 脏版本变体 | 120 | 阶段B |
| **P2** | 小院建筑 | 8 | 阶段B |
| **P3** | 章节封面 | 5 | 阶段C |
| **P3** | UI 图标 | 15 | 阶段C |
