# 铲屎官疯了 — UI 预制体实现指南

> 在 Unity 编辑器中操作，预制体路径: `Assets/Resources/PrefabsV2/`

---

## 环境准备

1. 团结引擎打开项目 (2022.3.62t11 / URP 2D)
2. 确认 `Assets/Art/PetGame/` 下精灵图已正确导入
3. 创建目录 `Assets/Resources/PrefabsV2/`

---

## 1. GameHUD.prefab（主 HUD）

Canvas 下创建 750×1334 根节点，包含：

```
HUD_Root (RectTransform, 750×1334)
├─ LevelText (Text, y=600)         → "第1关"
├─ ScoreText (Text, y=560)         → "得分:0/150"
├─ StepText (Text, y=520)          → "步数:0"
├─ Stars (Text, y=480, 隐藏)       → ★★★
├─ PetArea (Empty, y=300~350)      → 横向 LayoutGroup
├─ BowlArea (Empty, y=0~200)       → GridLayoutGroup 3列
├─ HeldFoodHolder (Empty, 底部, 隐藏) → "手持食物"占位
├─ ResultOverlay (Panel, 全屏覆盖, 隐藏)
│   ├─ Title (Text, "通关!")
│   ├─ Stars (Text)
│   ├─ btnRestart (Button, "重新开始")
│   └─ btnNext (Button, "下一关")
├─ btnUndo (Button, 左下)          → "↩撤回"
├─ btnAddBowl (Button, 中下)       → "🥣+碗"
└─ btnShuffle (Button, 右下)       → "🔀打乱"
```

**关键: 所有命名必须与 PetGameUI.FindRefs() 一致**

---

## 2. PetItem.prefab（宠物项）

```
PetItem (Image, 120×180)
├─ QueueLabel (Text, 顶部)         → "橘猫"等
└─ PetFace (Image, 主体 100×120)   → 宠物表情
```

- PetFace 的 Sprite 从 `Assets/Art/PetGame/pets/` 加载
- 含 LayoutElement (minWidth=120, minHeight=160)

---

## 3. BowlItem.prefab（碗项）

```
BowlItem (Button, 140×180)
├─ BowlBg (Image, 碗底图)          → Art/PetGame/bowls/
├─ FoodStack (VerticalLayoutGroup, 碗内)
│   └─ (动态生成 FoodIcon)
└─ DoneMark (Image, 右上角, 隐藏) → 星星/对勾图标
```

- Button 的 onClick 由 PetGameUI 动态绑定
- FoodStack 需 constrained content

---

## 4. FoodIcon.prefab（食物图标）

```
FoodIcon (Image, 30×30)
└─ 含 LayoutElement (preferred 30×30)
```

- 极其简单，只需 Image + LayoutElement
- Sprite 由代码动态设置

---

## 验证

在 PetGameScene 中：
1. 创建空 GameObject，挂载 PetGameUI.cs
2. 确保 PetGameManager 已挂到场景中
3. 运行 → 应自动生成测试关卡 UI
