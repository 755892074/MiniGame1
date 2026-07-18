---
name: unity-auto-verify
description: Unity/Tuanjie 自动验证闭环。触发条件：用户要求"跑测试""验证""自动跑测""check一下""检查编译""截图看效果"。通过直连 MCP（mcp__codely-unity__*）驱动编辑器完成：保存场景 → 静默生成 → 等编译 → 抓控制台错误 → 进 Play 连拍 4 帧 → 像素差分分析 → 中文结论。
---

# Unity 自动验证闭环

通过直连 MCP 驱动团结编辑器，完成一键验证闭环。

## 前置条件

- 团结编辑器已打开本工程（桌面 WorkBuddy 不做启动/关闭编辑器操作）
- MCP 直连通路可用（`mcp__codely-unity__unity_editor`、`mcp__codely-unity__unity_console`、`mcp__codely-unity__unity_screenshot`、`mcp__codely-unity__unity_menu`）
- Python 环境已装 `pillow` + `numpy`

## 流程

### Step 1: 保存场景

调用 `mcp__codely-unity__unity_editor` → `{"action": "save_scene"}`

### Step 2: 静默模式

调用 `mcp__codely-unity__unity_menu` → `{"action": "execute", "menuPath": "Tools/铲屎官疯了/静默模式"}`

关闭所有确认弹窗，避免 Play 模式卡住。

### Step 3: 重跑全部生成

调用 `mcp__codely-unity__unity_menu` → `{"action": "execute", "menuPath": "Tools/铲屎官疯了/重新生成全部"}`

包括 v3 UI + v2 游戏 UI + 场景布局。

### Step 4: 等待编译

调用 `mcp__codely-unity__unity_editor` → `{"action": "wait_for_idle"}`，超时 180s。

### Step 5: 检查控制台

调用 `mcp__codely-unity__unity_console` → `{"action": "get"}`。

筛选：
- CS 编译错误：type=Error 且消息匹配 `error CS\d+` 或 `.cs(\d+,\d+):\s*error`
- Missing 脚本：消息含 "missing"

0 条 CS 错误 = 编译通过。

### Step 6: Play 模式连拍

1. 调用 `mcp__codely-unity__unity_editor` → `{"action": "play"}`
2. 等待 3 秒
3. 循环 4 次，每次：
   - 调用 `mcp__codely-unity__unity_screenshot` → `{"action": "capture_game_view"}`
   - 从返回的 `path` 获取截图路径
   - 复制到 `tools/automation/screenshots/verify_f{i}.png`
   - 等待 1 秒
4. 再次查控制台，看是否有运行时 missing
5. 调用 `mcp__codely-unity__unity_editor` → `{"action": "stop"}`

### Step 7: 像素差分

运行 `python tools/automation/pixel_diff.py verify_f0.png verify_f1.png verify_f2.png verify_f3.png`。

输出：
- 帧间变化像素数
- 最大变化像素
- 变化区域（bbox）
- 动画结论：✅ 在播（>2000px） 或 ⚠️ 变化偏小

## 结论格式

输出简洁中文结论：

```
[验证结论]
编译: ✅ 0 CS 错误 / ❌ N 处错误
运行时: ✅ 0 异常 / ⚠️ N 条 missing
动画: ✅ 在播 (N px) / ⚠️ 变化偏小
变化区域: WxH @ (left,top)
```

## 注意事项

- 不启动/关闭团结编辑器（用户自行管理）
- 截图失败时跳过像素差分，不影响编译结论
- 静默模式菜单不存在时跳过，能继续
