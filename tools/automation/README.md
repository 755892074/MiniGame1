# 铲屎官疯了 · 自动化闭环（手机端无人值守开发）

本目录让 AI（手机端 WorkBuddy 小程序）能**自主驱动团结编辑器**：改代码 → 重跑生成 → 编译检查 → 进 Play 连拍 → 像素判断动画 → 出结论，**全程无需人工点确认框**。

## 一、原理

```
手机 WorkBuddy ──(局域网 HTTP/MCP)──> codely MCP (本机 127.0.0.1:8765)
                                         │
                                         ▼ 内部 TCP 桥
                                    团结 Tuanjie 编辑器 (本机, 打开本工程)
```

- 团结编辑器装了 `cn.tuanjie.codely.bridge` 包，内部监听 TCP `62767`。
- `codely serve unity-mcp --http --http-port 8765` 把编辑器能力暴露成 MCP（绑定 `0.0.0.0`，同局域网手机可直连）。
- `pipeline_verify.py` 通过 MCP 调用 `unity_menu` / `unity_editor`(play/stop) / `unity_screenshot` 等工具，完成「改完即验证」。
- `PetGameGenUtil.cs` 提供**静默开关**：所有生成菜单末尾不再弹 `DisplayDialog` 确认框（手机端没人点会卡死），改为写日志，AI 能从 MCP 控制台读回结果。桌面端想看提示可用菜单「铲屎官疯了 / 切换生成弹窗提示」翻转。

## 二、台式机一次性环境搭建

> 笔记本已验证可用。切到台式机按此步骤，clone 后基本零配置。

1. **安装团结引擎 Tuanjie 2022.3.62t11**
   - 必须同版本（`m_EditorVersion: 2022.3.62t11`），否则微信小游戏/PlayableAds 包不匹配会损坏工程。
   - 默认装到 `D:\Program Files\UnityEditors\2022.3.62t11\`。若装到别处，运行脚本时设置环境变量 `TUANJIE_EXE` 指向 `Tuanjie.exe`，或 `python ensure_env.py --tuanjie <路径>`。

2. **安装 Tuanjie Cowork（含 codely CLI）**
   - 提供 `codely serve unity-mcp` 命令。
   - 默认装到 `%LOCALAPPDATA%\Programs\Tuanjie Cowork\cli\bin\win32-x64\codely.exe`。
   - 若路径不同，设置环境变量 `CODELY_CLI` 或 `--codely <路径>`。
   - 脚本会自动搜索常见位置，找不到才需要手动指定。

3. **clone 工程**
   ```
   git clone https://github.com/755892074/MiniGame1.git
   cd MiniGame1
   ```
   （首次打开工程时团结会重新导入资源，TCP 桥要等几分钟才起来，属正常。）

4. **装 Python 依赖**（脚本用 pillow/numpy 做像素分析）
   ```
   python -m pip install -r tools/automation/requirements.txt
   ```

## 三、启动环境（每次开机/切机后跑一次）

```
cd MiniGame1
python tools/automation/ensure_env.py
```

它会：
- 检测 TCP 桥(62767) / MCP(8765) 是否已在监听 → 在跑就跳过（**幂等**）。
- 没跑就按需拉起 Tuanjie 编辑器 + codely MCP。
- 输出**本机局域网 IP** 和手机端要连的 MCP 地址，例如 `http://192.168.1.50:8765/mcp`。

> 卡死/换端口时：`python ensure_env.py --restart-mcp`

## 四、手机 WorkBuddy 驱动开发

1. 台式机跑完 `ensure_env.py`，把输出的 **MCP 地址**（如 `http://192.168.1.50:8765/mcp`）告诉手机端 WorkBuddy。
2. 在手机 WorkBuddy 里配置一个 MCP 连接器指向该地址（或把地址作为上下文提供给对话）。
3. 直接说需求即可，例如：
   - "把碗改成圆形"
   - "橘猫 idle 动画再快一点"
   - "顶部分数条背景换成花园图"
4. AI 会调用 `pipeline_verify.py` 走完整闭环并回传截图 + 结论。

> 也可在台式机本机直接跑验证：
> ```
> python tools/automation/pipeline_verify.py "本次改动说明"
> ```

## 五、文件清单

| 文件 | 作用 |
|---|---|
| `ensure_env.py` | 环境常驻：检查/拉起 Tuanjie + codely MCP，幂等，输出手机端 MCP 地址 |
| `pipeline_verify.py` | 一键验证闭环：重跑生成 → 编译检查 → 进 Play 连拍 → 像素判动画 → 结论 |
| `requirements.txt` | Python 依赖（pillow / numpy） |
| `README.md` | 本说明 |

## 六、常见问题

- **手机连不上 MCP**：确认台式机和手机在同一局域网；台式机防火墙放行 8765；`ensure_env.py` 输出的地址是 `0.0.0.0` 绑定，手机用局域网 IP 访问。
- **TCP 桥起不来**：首次打开工程团结在导入资源，等几分钟；或确认 `cn.tuanjie.codely.bridge` 包已在 `Packages/manifest.json`（已带）。
- **生成菜单卡住**：确认 `PetGameGenUtil` 静默开关开启（`EditorPrefs` 里 `PetGameSilent=true`），脚本 [2] 步会自动设。
- **像素判定说动画没播**：可能只是帧间变化小于阈值；可人肉进编辑器 Play 看一眼，或调大 `pipeline_verify.py` 里 `anim_ok = max_diff > 2000` 的阈值。
