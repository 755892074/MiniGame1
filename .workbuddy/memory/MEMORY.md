# MiniGame1 项目记忆

## 📱 手机端对话须知
- 桌面 WorkBuddy 已通过 MCP 直连团结编辑器，能改代码、调菜单、进 Play、截图、跑测。
- 手机端下达任务即可，桌面 WorkBuddy 收到后自主规划执行，结果（截图/结论）同步回项目。
- 不要说"帮我打开编辑器"——编辑器已在桌面运行。
- 功能：改代码、改关卡、生成美术、跑 pipleline 验证、出包、修 bug、改 UI。

## 基本信息
- 仓库: https://github.com/755892074/MiniGame1
- 本地: D:\WorkBuddy_WorkSpace\Projects\MiniGame1
- 引擎: 团结引擎 2022.3.62t11, URP 2D
- 框架: F8Framework (GitHub 源)
- 平台: 抖音/微信小游戏 + IAA 变现

## 双机开发 ⚠️ 铁律
- **先 pull → 再改 → 再 commit → 再 push**。绝对不允许不 pull 直接 push。
- 换机前必须把当前机代码 push 完。
- 看到合并冲突不要强行覆盖，找 WorkBuddy 处理。
- Packages/packages-lock.json, PackageManagerSettings.asset 已 gitignore（机器差异）
- F8Framework 用 GitHub 源，不用本地包

## 游戏: 铲屎官疯了
- 类型: 宠物排队分食解谜 (类似脑洞倒水)
- 核心: PourSystem 倒食物判定 + 满碗匹配宠物计分
- UI: 需 Resources/PrefabsV2/ 下 4 个预制体

## 美术资产规格（锁定）
- 基准: 750×1334, AI 2x 超采
- 食物: 128px/格, 4×2=512×256, 8种
- 宠物: 256px/格, 3×2=768×512, 6只
- 碗: 192px/格, 2×2=384×384
- UI图标: 80px/格, 3×2=240×160
- 提示词风格: cute cartoon, kawaii, hand-drawn, soft pastel, transparent/white bg
- 切图: `python tools/ai-ui-asset-cutter/ai-ui-asset-cutter.py <图> --layout <类型> --no-auto`
