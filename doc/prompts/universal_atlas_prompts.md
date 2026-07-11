# 通用图集提示词包（3D 盲盒 Q 版风）

> 用途：先生成一套跨两款游戏通用的 UI / 图标 / IP 图集，锁定全局视觉基调。
> 工具：Image2 网页版，手动生成后导入 `Assets/Art/UI/`。
> **核心规则：每张图都必须带上下面的「风格锚点」前缀，这是整套图风格统一的关键。**

---

## 一、风格锚点（STYLE ANCHOR · 每张图必带）

把这段作为**固定前缀**，粘在每个具体元素描述前面：

```
3D rendered blind-box toy style, glossy soft plastic material like Popmart figures,
rounded chunky shapes, soft studio lighting with gentle rim light,
clean solid background, high-end mobile game UI asset,
dopamine color accents, smooth subsurface scattering,
centered composition, no text, no watermark, 4K quality,
```

配色主锚（点缀色统一从这里取）：
- 主色 紫 `#7F77DD`　辅色 粉 `#ED93B1`
- 点缀 暖黄 `#FAC775`　珊瑚 `#F0997B`　薄荷绿 `#5DCAA5`

> 生成建议：同一元素多出几张选最好的；背景统一用**纯色/柔和渐变**，方便抠图。

---

## 二、UI 按钮组

### 主按钮（开始/确认）
```
[风格锚点] + a glossy rounded rectangle button, purple-to-pink gradient body,
thick soft edges, subtle inner glow, floating slightly with soft shadow,
blank surface for text, playful premium feel.
```

### 次按钮（取消/返回）
```
[风格锚点] + a smaller rounded button in soft mint-green glossy plastic,
same chunky 3D style, blank surface, secondary action feel.
```

### 圆形图标按钮底（设置/暂停/音效）
```
[风格锚点] + a round glossy 3D button base, warm yellow plastic,
slightly domed top, soft shadow, empty center for an icon.
```

---

## 三、通用图标（统一 512×512，透明或纯色底）

一次生成一组，保持一致：

```
[风格锚点] + a set of cute 3D blind-box style game icons on separate solid backgrounds:
1. a shiny gold coin with a star emboss
2. a glossy pink heart (life)
3. a bright yellow five-point star
4. a purple gear (settings)
5. a pause icon (two rounded bars)
6. a left-pointing back arrow
7. a share icon (three connected dots)
8. a light bulb (hint)
9. a round clock (timer)
10. a magic wand sparkle (power-up)
each icon chunky, rounded, glossy plastic, consistent lighting and scale.
```

---

## 四、IAA 广告相关（重点·影响收入）

### 激励视频「翻倍」按钮
```
[风格锚点] + a glossy 3D button showing a bold "×2" multiplier symbol,
gold and purple, with a small play-triangle badge, coins bursting around,
inviting and rewarding feel.
```

### 「复活」按钮
```
[风格锚点] + a glossy 3D button with a pink heart and a circular refresh arrow,
warm hopeful glow, small play-triangle badge for video ad.
```

### 「看广告得道具」图标
```
[风格锚点] + a small glossy gift box with a play-triangle badge,
purple ribbon, sparkles, video-reward feel.
```

---

## 五、弹窗底板 / 面板

### 结算弹窗底板
```
[风格锚点] + a rounded 3D popup panel, soft cream-white glossy plastic frame,
purple rounded header bar on top, empty inner area,
floating with soft shadow, mobile game result panel.
```

### 商店/道具面板底
```
[风格锚点] + a wider rounded 3D shop panel, glossy white body with mint accents,
grid-friendly empty layout, premium casual game feel.
```

---

## 六、评价与进度

### 星级（空星 + 满星，成对生成）
```
[风格锚点] + two versions of a chunky 3D star side by side:
left = empty gray glossy star (unearned),
right = bright golden glowing star (earned),
same shape and lighting.
```

### 进度条（底槽 + 填充，成对生成）
```
[风格锚点] + a horizontal 3D progress bar:
an empty rounded gray glossy track, and a filled purple-pink glossy fill,
soft rounded caps, clean and simple.
```

---

## 七、IP 吉祥物（品牌核心资产 · 重点打磨）

### 主形象
```
[风格锚点] + an original mascot: a round chubby blind-box character,
big expressive glossy eyes, tiny arms, sitting pose,
soft purple body with pink cheeks, wearing a tiny detective magnifying glass,
adorable premium collectible toy look, front view.
```

### 表情变体（一次出一组，供两款游戏复用）
```
[风格锚点] + the same round mascot character in 6 expression variants:
happy, surprised, thinking, celebrating, confused, determined,
consistent design, same body, expression sheet layout.
```

---

## 八、主界面背景

```
[风格锚点] + a soft dreamy game main-menu background,
pastel purple-pink gradient sky with floating 3D blurred shapes (stars, coins, clouds),
depth of field blur, cozy premium mobile game atmosphere, vertical 1080x1920,
lower center left empty for UI and mascot.
```

---

## 生成与导入流程

1. 复制「风格锚点」+ 某个元素描述 → Image2 生成
2. 优先先出 **IP 吉祥物 + 通用图标 + 主按钮**（定调三件套）
3. 下载 → 存到 `Assets/Art/UI/`，命名如 `icon_coin.png` / `btn_primary.png` / `mascot_happy.png`
4. 生成后把成品路径发我，我用编辑器批量配置 Sprite 导入设置 + 打图集

---

## 命名规范

| 类型 | 命名 | 示例 |
|------|------|------|
| 图标 | `icon_{名}` | `icon_coin.png` |
| 按钮 | `btn_{名}` | `btn_primary.png` |
| 面板 | `panel_{名}` | `panel_result.png` |
| IP | `mascot_{表情}` | `mascot_happy.png` |
| 背景 | `bg_{场景}` | `bg_main.png` |
| 广告 | `ad_{名}` | `ad_double.png` |

---

*文档版本：v1.0 | 最后更新：2026-07-06*
