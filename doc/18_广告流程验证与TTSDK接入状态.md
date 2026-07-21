# 广告流程验证与 TTSDK 接入状态

> 日期：2026-07-20 ｜ 关联任务：#54 / #63 ｜ 游戏：铲屎官疯了 v2

## 1. 真机验证结果（mock 模式）

`AdManager.useMock = true` 时，激励视频 = 0.4s 后回调 `success=true`（无真实广告）。
验证用 codely 两段式（reload-free 实例，避免 BindButtons 时序假阴性）。

| 广告触点 | 代码位置 | 验证方式 | 结论 |
|---|---|---|---|
| ① 道具用尽→看广告补充 | `PetGameUI.ShowToolAdSupply` L1158 | 耗尽 Hint→点 BtnHint→弹窗"道具用尽啦~"→点"📺 看广告补充"→mock 完成后 Hint 0→2、弹窗关闭 | **PASS**（端到端） |
| ② 死局救援→看广告+1碗 | `PetGameUI.ShowDeadlockRescue` L1186 | 代码结构同 ①（`AdManager.ShowRewardedAd`→`gm.AddBowl`） | 代码校验通过（同模式，未触发死局实跑） |
| ③ 结算→看广告金币+鱼干翻倍 | `PetGameUI.OnWatchAdForDouble` L961 | 代码结构同 ①（`AdManager.ShowRewardedAd`→`GrantTool`式发奖） | 代码校验通过（同模式，未实跑通关） |

**结论**：广告"成功发奖"闭环在 mock 下完整可用；① 已端到端真机验证，②/③ 与①同构可信。

## 2. TTSDK / 抖音接入状态盘点

| 项 | 状态 | 说明 |
|---|---|---|
| `com.bytedance.starksdk` | ✅ 已装 | 本地包 `com.bytedance.starksdk@6.7.9`（抖音小游戏运行时） |
| `com.bytedance.bgdt` | ✅ 已装 | `Assets/Plugins/ByteGame/com.bytedance.bgdt`（字节系包） |
| 宏 `TTSDK_MIX_ENGINE` | ✅ 已定义 | `manifest`/PlayerSettings defineSymbols 已开 |
| 小游戏 appId | ✅ 已配 | `StarkBuilderSetting.asset._appId = ttf09486296ef1dfbc07` |
| **激励视频真实调用** | ❌ 未接 | `AdManager.RealShow` 仅为 stub（`Debug.LogWarning` 后回退 mock） |
| 广告位 ID（adUnitId） | ❌ 未配 | 需在抖音开放平台注册激励视频广告位 |
| 服务端发奖校验 | ❌ 未接 | IAA 防作弊/收入保真关键，上线前必做 |

## 3. 真接入 TODO（上线前清单）

1. **注册广告位**：抖音开放平台 → 小游戏 `ttf09486296ef1dfbc07` → 创建「激励视频」广告位，拿到 `adUnitId`。
2. **改写 `AdManager.RealShow`**：删掉 mock 回退，接入抖音小游戏广告 API（Stark 封装或 `tt.createRewardedVideoAd({ adUnitId })`），在 `onClose(rewardVerified)` 回调里 `cb?.Invoke(verified)`。
3. **奖励校验**：客户端 `rewardVerified` 仅作前端发奖；收入结算应以**服务端回调**（抖音服务端通知）为准，防刷量。
4. **降级保护**：广告加载失败/用户中途关闭 → `cb(false)` → 不重复弹、提示"稍后再试"，与当前"稍后再说"按钮一致。
5. **埋点回传**：看广告次数、完播率、各触点转化、广告 ARPU，回传到运营追踪表（见 `运营分析/游戏生命周期追踪.md`）。
6. **真机联调**：`useMock=false` 后在真机/抖音开发者工具跑一遍 ①/②/③，确认发奖与界面正确。

## 4. 自动化验收注意

- 验证广告相关逻辑时保持 `useMock=true`（0.4s 内必 success），不要把真广告接入当验收前提。
- 弹窗按钮是运行时 `MakeBtn` 动态创建，真机查找需用 `GameObject.Find("Panel")` 取首个 `Button`（"看广告补充"先于"稍后再说"添加）。
- 看广告回调走协程（0.4s），断言发奖结果必须放在**下一次** codely 调用（让帧推进），不能在同一脚本内同步断言。
