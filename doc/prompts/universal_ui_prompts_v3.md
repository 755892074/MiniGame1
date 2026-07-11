# 通用UI资产生成提示词（2D扁平卡通 + 木质纸质感）
# 参考：抖音/微信小游戏主流风格（木质感UI + 暖色系扁平卡通）
# 策略：同类资产合集生成，一次出图，自动切分
# 工具：ai-ui-asset-cutter.py（项目根目录 tools/ 下）

# ============================================================
# 生成建议与分辨率说明
# ============================================================

## 分辨率建议

| 生成批次 | 建议尺寸 | 原因 |
|---------|---------|------|
| **按钮套装（5个）** | **1024 x 512** | 横向5个按钮，每个约200px宽，足够 |
| 面板套装（4个） | 1024 x 1024 | 2x2网格，每个约450px |
| 道具图标（8个） | 1024 x 512 | 4x2网格，每个约200px |
| 杂项UI（6个） | 1024 x 1024 | 3x2网格，元素大小不一 |
| 背景图 | 2048 x 2048 | 全屏背景，需要高分辨率 |

## 为什么不需要太大？

1. **最终显示尺寸**：UI按钮在手机屏幕上通常只有 150-300px 宽
2. **图集压缩**：团结导入后会自动压缩到图集中，大图不会浪费
3. **AI生成成本**：分辨率越低，生成越快，费用越低
4. **切分后处理**：脚本会自动裁剪空白区域，最终尺寸是精确的

## 生成流程

1. **Image2 生成** → 复制提示词 → 设置分辨率 → 生成
2. **保存到 Raw/** → `Assets/Art/UI/Raw/`
3. **自动切分** → 运行脚本 → 自动裁剪空白 → 归档到对应目录
4. **团结导入** → 在团结中设置 9-Slice、打图集

## 自动切分命令

```bash
# 按钮套装
python tools/ai-ui-asset-cutter.py Raw/buttons.png --layout buttons

# 面板套装
python tools/ai-ui-asset-cutter.py Raw/panels.png --layout panels

# 道具图标
python tools/ai-ui-asset-cutter.py Raw/items.png --layout items

# 杂项UI
python tools/ai-ui-asset-cutter.py Raw/misc.png --layout misc

# 背景图（直接复制，不切分）
cp Raw/bg_menu.png Assets/Art/UI/Backgrounds/
```

# ============================================================
# 合计生成次数：7 次（原来 26 次 → 现在 7 次）
# ============================================================

# ============================================================
# 第1次生成：按钮套装（5个按钮横向排列在一张图上）
# 生成后手动切分为：btn_main.png, btn_secondary.png, btn_circle.png, btn_pill.png, btn_disabled.png
# ============================================================

## 按钮套装
```
2D flat cartoon illustration style, warm muted color palette,
subtle wood-grain and paper-texture details, soft matte finish,
clean vector-like shapes with gentle anti-aliased edges,
cozy and inviting atmosphere, no harsh shadows,
premium casual mobile game UI quality, no text, no watermark, high resolution,
a set of 5 game buttons arranged in a single horizontal row with equal spacing,
each button separated by a small gap, uniform soft lighting across all,
transparent or clean solid neutral background:

1. large yellow wooden main button - rounded corners, golden-yellow with subtle wood-grain horizontal lines, slight raised bevel effect, darker brown border, soft highlight on top edge, empty center for text overlay

2. medium green secondary button - rounded corners, soft mint-green with subtle paper texture, flat understated look, minimal border, empty center for text

3. small blue circular icon button - white left-pointing arrow in center, sky-blue fill, thick rounded border, subtle drop shadow, empty center for icon overlay

4. long peach pill-shaped tag button - fully rounded long edges, soft warm peach with subtle paper texture, gentle highlight on top, horizontal shape, empty center for text

5. large gray disabled button - rounded corners, desaturated muted gray, flat surface with no depth, disabled and inactive appearance, subtle paper texture.
```

---

# ============================================================
# 第2次生成：主弹窗面板（独立，因为要9-Slice切分）
# 存为：panel_dialog.png
# ============================================================

## 主弹窗面板（带标题横幅）
```
2D flat cartoon illustration style, warm muted color palette,
subtle wood-grain and paper-texture details, soft matte finish,
clean vector-like shapes with gentle anti-aliased edges,
cozy and inviting atmosphere, no harsh shadows,
premium casual mobile game UI quality, no text, no watermark, high resolution, transparent background,
a single rectangular game dialog panel with rounded corners,
outer frame in warm light-wood brown with visible wood-grain texture,
inner fill in soft cream-paper color with subtle paper texture,
top section has a decorative ribbon banner in slightly darker wood,
all corners have identical radius, generous flat border region for 9-slice,
subtle inner shadow on the paper area, soft outer shadow,
center area large and empty for content.
```

---

# ============================================================
# 第3次生成：小程序面板套装（4个面板排列在一张图上）
# 存为：切分后 panel_toast.png, panel_bottom.png, panel_sidebar.png, panel_card.png
# ============================================================

## 小程序面板套装
```
2D flat cartoon illustration style, warm muted color palette,
subtle wood-grain and paper-texture details, soft matte finish,
clean vector-like shapes with gentle anti-aliased edges,
cozy and inviting atmosphere, no harsh shadows,
premium casual mobile game UI quality, no text, no watermark, high resolution, transparent background,
a set of 4 small game UI panels arranged in a 2x2 grid with equal spacing,
each panel separated by a gap, uniform soft lighting across all:

1. toast notification panel - small wide horizontal rounded rectangle, border in warm tan-brown with wood texture, inner fill in soft warm yellow-cream with paper texture, soft highlight on top, flat empty center for text

2. bottom sheet panel - wide rectangular panel with rounded top corners only, bottom edge straight, border in warm wood-brown, inner fill in soft cream-paper, empty center area, designed for bottom-sheet style mobile UI

3. side panel - tall vertical panel with rounded corners on the right side only, left side straight, border in warm dusty rose-brown, inner fill in soft cream, empty center for content

4. level-select card frame - rectangular card with rounded corners, outer frame in warm light-wood brown with visible wood-grain, inner area with subtle cream-paper background, small decorative corner accents, generous inner area for overlaying a level thumbnail image, subtle shadow.
```

---

# ============================================================
# 第4次生成：主菜单背景（独立，全屏素材）
# 存为：bg_menu.png
# ============================================================

## 主菜单背景
```
2D flat cartoon illustration style, warm muted color palette,
subtle wood-grain and paper-texture details, soft matte finish,
clean vector-like shapes with gentle anti-aliased edges,
cozy and inviting atmosphere, no harsh shadows,
premium casual mobile game UI quality, no text, no watermark, high resolution,
a seamless soft background for a mobile game main menu,
warm beige-cream base with very subtle paper texture overlay,
small scattered decorative elements like faint leaf silhouettes
or soft watercolor-like organic shapes in muted colors,
flat and evenly lit, no harsh shadows, suitable as a game menu backdrop.
```

---

# ============================================================
# 第5次生成：关卡选择背景
# 存为：bg_levelselect.png
# ============================================================

## 关卡选择背景
```
2D flat cartoon illustration style, warm muted color palette,
subtle wood-grain and paper-texture details, soft matte finish,
clean vector-like shapes with gentle anti-aliased edges,
cozy and inviting atmosphere, no harsh shadows,
premium casual mobile game UI quality, no text, no watermark, high resolution,
a soft abstract background for a level selection screen,
warm cream-beige with subtle wood-paneled floor lines,
soft warm tones, minimal decorative elements,
leaves center area relatively open for UI and level node placement.
```

---

# ============================================================
# 第6次生成：道具图标套装（8个图标排列在一张图上，4x2网格）
# 生成后手动切分为8张独立PNG，透明背景
# ============================================================

## 道具图标套装
```
2D flat cartoon illustration style, warm muted color palette,
subtle wood-grain and paper-texture details, soft matte finish,
clean vector-like shapes with gentle anti-aliased edges,
cozy and inviting atmosphere, no harsh shadows,
premium casual mobile game UI quality, no text, no watermark, high resolution, transparent background,
a set of 8 cute hand-drawn game item icons arranged in a 4x2 grid with equal spacing,
each icon centered in its own cell with transparent background between them,
uniform soft lighting and consistent Q-bomb cartoon style across all:
row 1: magnifying glass (golden brass frame with wooden handle), hourglass (glass body with golden sand, wooden caps), gold star coin (embossed star, soft metallic luster), gift box (soft pink box with teal ribbon)
row 2: lightning bolt (glowing yellow with blue edge), shield (silver-blue metallic, embossed border), skeleton key (aged brass, simple flat shading), heart (soft glossy red-pink, rounded chunky shape)
simple flat shading style, cute and readable at small sizes.
```

---

# ============================================================
# 第7次生成：特殊杂项UI套装
# 生成后切分为：misc_back_btn.png, misc_item_base.png, misc_lock.png, misc_hot_tag.png, bg_overlay.png, bg_share_card.png
# ============================================================

## 杂项UI套装
```
2D flat cartoon illustration style, warm muted color palette,
subtle wood-grain and paper-texture details, soft matte finish,
clean vector-like shapes with gentle anti-aliased edges,
cozy and inviting atmosphere, no harsh shadows,
premium casual mobile game UI quality, no text, no watermark, high resolution, transparent background,
a set of 6 miscellaneous game UI elements arranged in a 3x2 grid with equal spacing:

row 1:
1. circular sky-blue return/back button with white left-pointing arrow in center, subtle raised effect with soft shadow
2. rounded rectangular button base with a small circular gold video-ad badge (play icon) in the top-right corner, base in warm cream with wood texture
3. red-orange "hot" badge tag, small rounded-corner label with decorative flame shape, empty center for "最热" text

row 2:
4. golden-yellow padlock icon, simple flat shading, small keyhole detail
5. dark desaturated brown-gray overlay panel with rounded corners, semi-transparent feel, soft edge vignette, designed as dimming overlay for popups
6. vertical 9:16 ratio social-share card background, warm cream base with subtle gold foil border accents, ornate but clean corner decorations, center empty for screenshot overlay.
```

---

# ============================================================
# 导出后处理
# ============================================================

# 1. 按钮/面板/背景：
#    在 Image2 导出后，用 Photoshop/Photopea 手动切分，
#    面板类标记 9-Slice 边距，导入团结后 Sprite Editor 设 Border

# 2. 道具图标：
#    从合集图上切分，每张保存为独立透明背景 PNG，
#    在团结中打 Sprite Atlas 图集

# 3. 切分后命名：
#    按钮: btn_main.png, btn_secondary.png, btn_circle.png, btn_pill.png, btn_disabled.png
#    面板: panel_dialog.png, panel_toast.png, panel_bottom.png, panel_sidebar.png, panel_card.png
#    背景: bg_menu.png, bg_levelselect.png, bg_overlay.png, bg_share_card.png
#    道具: item_magnifier.png, item_hourglass.png, item_coin.png, item_gift.png, item_lightning.png, item_shield.png, item_key.png, item_heart.png
#    杂项: misc_back_btn.png, misc_item_base.png, misc_lock.png, misc_hot_tag.png
