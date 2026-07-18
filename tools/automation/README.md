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

> **跨机器零配置**：`ensure_env.py` / `pipeline_verify.py` 的工程根目录从脚本自己所在位置（上两级）自动推算；Tuanjie / codely 路径支持环境变量 `TUANJIE_EXE` / `CODELY_CLI` 覆盖，找不到就自动搜索常见位置。所以**换机器 / 换盘符都基本不用改任何配置**。

---

## 二、台式机已经在同步开发 → 直接 pull（主路径）

你台式机本来就在跑团结 + codely、工程一直开着同步，**不需要重装任何东西**，pull 下来就能用：

```bash
cd <你的工程目录>/MiniGame1          # 就是你现在一直在用的那个目录
git pull origin master

# 确保本机自动化环境在跑（幂等，已在跑就跳过，不重复拉起）
python tools/automation/ensure_env.py
```

`ensure_env.py` 会：
- 检测 TCP 桥(62767) / MCP(8765) 是否已在监听 → 在跑就跳过。
- 没跑才按需拉起 Tuanjie 编辑器 + codely MCP（用自动搜索到的路径，不挑盘符）。
- 输出**本机局域网 IP** 和手机端要连的 MCP 地址，例如 `http://192.168.1.50:8765/mcp`。

> 之后每次只是 `git pull` 拿新改动，环境常驻不用反复起。换端口/卡死时：`python ensure_env.py --restart-mcp`。

**装一次 Python 依赖即可**（脚本用 pillow/numpy 做像素分析，和 git 仓库无关，换机器才需要重装）：
```bash
python -m pip install -r tools/automation/requirements.txt
```

---

## 三、手机 WorkBuddy 驱动开发

1. 台式机跑完 `ensure_env.py`，把输出的 **MCP 地址**（如 `http://192.168.1.50:8765/mcp`）告诉手机端 WorkBuddy。
2. 在手机 WorkBuddy 里配置一个 MCP 连接器指向该地址（或把地址作为上下文提供给对话）。
3. 直接说需求即可，例如：
   - "把碗改成圆形"
   - "橘猫 idle 动画再快一点"
   - "顶部分数条背景换成花园图"
4. AI 会调用 `pipeline_verify.py` 走完整闭环并回传截图 + 结论。

> 也可在台式机本机直接跑验证：
> ```bash
> python tools/automation/pipeline_verify.py "本次改动说明"
> ```

---

## 四、文件清单

| 文件 | 作用 |
|---|---|
| `ensure_env.py` | 环境常驻：检查/拉起 Tuanjie + codely MCP，幂等，输出手机端 MCP 地址 |
| `pipeline_verify.py` | 一键验证闭环：重跑生成 → 编译检查 → 进 Play 连拍 → 像素判动画 → 结论 |
| `requirements.txt` | Python 依赖（pillow / numpy） |
| `README.md` | 本说明 |

---

## 附录 A：全新机器 / 新同事才需要（你台式机跳过）

仅当在一台**从没装过**团结引擎的机器上首次建立环境时用。

1. **安装团结引擎 Tuanjie 2022.3.62t11**
   - 必须同版本（`m_EditorVersion: 2022.3.62t11`），否则微信小游戏/PlayableAds 包不匹配会损坏工程。
   - 默认装到 `D:\Program Files\UnityEditors\2022.3.62t11\`。若装到别处，运行脚本时设 `TUANJIE_EXE` 指向 `Tuanjie.exe`，或 `python ensure_env.py --tuanjie <路径>`。

2. **安装 Tuanjie Cowork（含 codely CLI）**
   - 提供 `codely serve unity-mcp` 命令。
   - 默认装到 `%LOCALAPPDATA%\Programs\Tuanjie Cowork\cli\bin\win32-x64\codely.exe`。
   - 若路径不同，设 `CODELY_CLI` 或 `--codely <路径>`；脚本也会自动搜索常见位置。

3. **clone 工程**
   ```bash
   git clone https://github.com/755892074/MiniGame1.git
   cd MiniGame1
   ```
   （首次打开工程时团结会重新导入资源，TCP 桥要等几分钟才起来，属正常。）

4. 然后回到「二、直接 pull」的 `ensure_env.py` 步骤即可。

---

## 附录 B：常见问题

- **手机连不上 MCP**：确认台式机和手机在同一局域网；台式机防火墙放行 8765；`ensure_env.py` 输出的是 `0.0.0.0` 绑定，手机用局域网 IP 访问。
- **TCP 桥起不来**：首次打开工程团结在导入资源，等几分钟；或确认 `cn.tuanjie.codely.bridge` 包已在 `Packages/manifest.json`（已带）。
- **生成菜单卡住**：确认 `PetGameGenUtil` 静默开关开启（`EditorPrefs` 里 `PetGameSilent=true`），脚本 [2] 步会自动设。
- **像素判定说动画没播**：可能只是帧间变化小于阈值；可人肉进编辑器 Play 看一眼，或调大 `pipeline_verify.py` 里 `anim_ok = max_diff > 2000` 的阈值。
