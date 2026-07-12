# ai-ui-asset-cutter

## 描述
AI美术资产自动切分与归档工作流。从AI生成UI合集图到自动切分、命名、归档到团结项目，完整自动化。

## 触发条件
当用户需要：
- 批量生成UI按钮、图标、面板等美术资产
- 自动切分AI生成的合集图
- 按命名规范归档到团结项目目录
- 设置9-Slice图集

## 依赖
- Python 3.x
- Pillow (`pip install Pillow`)
- 团结引擎项目（已配置 Assets/Art/UI/ 目录结构）

## 工具位置

脚本位于项目目录下：
```
MiniGame1_Project/
├── tools/
│   └── ai-ui-asset-cutter.py  ← 主脚本
├── Assets/Art/UI/
│   ├── Raw/
│   ├── Sliced/
│   ├── Icons/
│   └── Backgrounds/
```

## 目录结构
```
Assets/Art/UI/
├── Raw/           ← AI生成的原始合集图
├── Sliced/        ← 切分后的按钮、面板（需9-Slice）
├── Icons/         ← 道具图标
├── Backgrounds/  ← 背景图
└── Atlas/         ← 打图集后的最终资源
```

## 使用流程

### 1. 生成AI美术资产

复制对应的提示词到 Image2/SeedDance，生成合集图。

**推荐分辨率**（避免浪费）：
| 类型 | 推荐尺寸 | 说明 |
|------|---------|------|
| 按钮套装（5个） | 1024 x 512 | 横向排列，每个约200px |
| 面板套装（4个） | 1024 x 1024 | 2x2网格 |
| 道具图标（8个） | 1024 x 512 | 4x2网格 |
| 背景图 | 2048 x 2048 | 全屏 |

### 2. 保存到 Raw/ 目录

将AI生成的图片保存到：`Assets/Art/UI/Raw/`

### 3. 运行自动切分脚本

```bash
# 按钮套装（5个横排）
python tools/ai-ui-asset-cutter.py Raw/buttons.png --layout buttons

# 面板套装（4个2x2）
python tools/ai-ui-asset-cutter.py Raw/panels.png --layout panels

# 道具图标（8个4x2）
python tools/ai-ui-asset-cutter.py Raw/items.png --layout items

# 杂项UI（6个3x2）
python tools/ai-ui-asset-cutter.py Raw/misc.png --layout misc
```

### 4. 在团结中设置

1. **导入图片**：切分后的文件已自动放入对应目录
2. **设置9-Slice**（按钮/面板）：
   - 选中 `.png` → Sprite Editor → 设置 Border（Left/Right/Top/Bottom）
   - 按钮建议值：Left/Right=0.15, Top/Bottom=0.25
3. **打图集**：
   - Window → 2D → Sprite Atlas
   - 将 Icons/ 和 Sliced/ 中的资源拖到 Atlas

## 命名规范

### 按钮类 (btn_)
- `btn_main.png` — 主按钮（黄色木质）
- `btn_secondary.png` — 次按钮（淡绿）
- `btn_circle.png` — 圆形图标按钮
- `btn_pill.png` — 药丸标签按钮
- `btn_disabled.png` — 禁用按钮

### 面板类 (panel_)
- `panel_dialog.png` — 主弹窗面板
- `panel_toast.png` — 信息提示面板
- `panel_bottom.png` — 底部操作面板
- `panel_sidebar.png` — 侧边栏面板
- `panel_card.png` — 关卡选择卡片

### 背景类 (bg_)
- `bg_menu.png` — 主菜单背景
- `bg_levelselect.png` — 关卡选择背景
- `bg_overlay.png` — 深色遮罩
- `bg_share_card.png` — 分享卡片

### 道具图标类 (item_)
- `item_magnifier.png` — 放大镜（提示）
- `item_hourglass.png` — 沙漏（时间）
- `item_coin.png` — 星星币（货币）
- `item_gift.png` — 礼物盒
- `item_lightning.png` — 闪电（加速）
- `item_shield.png` — 护盾（保护）
- `item_key.png` — 钥匙（解锁）
- `item_heart.png` — 爱心（生命）

### 杂项类 (misc_)
- `misc_back_btn.png` — 返回按钮
- `misc_item_base.png` — 道具按钮底座
- `misc_lock.png` — 锁图标
- `misc_hot_tag.png` — "最热"标签

## 脚本参数

```bash
python ai-ui-asset-cutter.py <输入图> --layout <类型> [选项]

参数：
  input              输入图片路径
  --layout, -l       布局类型 (buttons/panels/items/misc/bg)
  --output, -o       输出目录（默认：项目Assets/Art/UI/）
  --no-auto          禁用智能检测，强制等分
  --names, -n        自定义文件名（空格分隔）
```

## 布局配置

| 布局类型 | 排列方式 | 元素数量 | 用途 |
|---------|---------|---------|------|
| `buttons` | 1x5横排 | 5个 | 按钮套装 |
| `panels` | 2x2网格 | 4个 | 面板套装 |
| `items` | 4x2网格 | 8个 | 道具图标 |
| `misc` | 3x2网格 | 6个 | 杂项UI |
| `bg` | 1x1 | 1个 | 背景图（不切分） |

## 注意事项

1. **生成时留出间距**：AI生成时要求"separated by gap"，确保元素之间有空白区域
2. **透明背景**：提示词中加入 `transparent background` 或 `clean solid background`
3. **分辨率控制**：合集图不需要太高分辨率，1024px宽通常足够
4. **手动调整**：自动切分后可能需要 Photoshop 微调，特别是9-Slice边距
5. **版本管理**：Raw/ 中的原始图建议提交Git，Sliced/ 和 Atlas/ 可由CI自动生成

## 扩展

如需添加新的布局类型，编辑 `ai-ui-asset-cutter.py` 中的 `LAYOUT_CONFIGS` 字典：

```python
"my_layout": {
    "desc": "描述",
    "rows": 2,
    "cols": 3,
    "margins": {"top": 20, "bottom": 20, "left": 20, "right": 20},
    "output_names": ["file1", "file2", "file3", "file4", "file5", "file6"],
}
```
