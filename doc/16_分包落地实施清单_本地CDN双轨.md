# doc/16 分包落地实施清单（本地 + CDN 双轨）

> 版本：v1.0 | 日期：2026-07-19
> 目标：按 doc/15 策略，落地「**本地 + CDN 两套资源加载**」分包方案，先把包体压进抖音限制（主包 ≤4MB / 整体 ≤20MB），再做 P5 离线收益。
> 关联：`15_分包与远程资源策略.md`（策略）、`14_小院建造与宠物养成设计.md` §8 美术需求
> 前置结论（已确认）：MiniGame 平台支持；AutoStreaming 为**引擎内置模块**（标准 Unity 预览包 `3.0.1-pre.1` 已移除，与团结内置模块不兼容）。

---

## 0. 资源审计现状（2026-07-19 实测）

| 目录 | 体积 | 问题 |
|------|------|------|
| `Assets/Art/PetGame/Raw` | 22M | 最大头，疑似未压缩原图，必须走 CDN/分包 |
| `Assets/Art/UI/Raw` | 12M | 同上 |
| `Assets/Art/PetGame/backgrounds` | 8.1M | 背景大图，非首启必选 |
| `Assets/Art/PetGame/bowls` | 3.3M | 饭碗 8 套 |
| `Assets/Art/PetGame/foods` | 3.3M | 食物 15 种 |
| `Assets/Art/PetGame/pets` | 2.9M | 宠物 6×7 表情 |
| `Assets/Art/PetGame/UI` | 5.8M | 小院面板 UI |
| `Assets/Art/UI/Sliced` | 652K | UI 九宫 |
| `Assets/Art/UI/hamster` | 497K | 仓鼠素材 |
| `Assets/Art/PetGame/Animations` | 548K | 动画 |
| **`Assets/Resources/`** | **20M** | ⚠️ **致命**：Resources 下资源会被 Unity **强制打进主包**，直接撑爆 4MB。必须迁出 |
| **合计 `Assets/Art`** | **59M** | 远超主包，必须拆分 + 远程 |

> 结论：**59M 美术全部不能进主包**。首要动作 = 把 `Assets/Resources/` 的 20M 美术迁出（改用 Addressables / AutoStreaming），再按下面矩阵分流。

---

## 1. 双轨架构（本地 + CDN 两套资源加载）

```
┌─────────────────────────────────────────────────────────┐
│                    资源加载门面 Res.Load                  │
│  统一接口：Res.Load(key, mode)  →  屏蔽本地/CDN 差异      │
└───────────────┬───────────────────────┬─────────────────┘
                │                       │
       ┌────────▼────────┐     ┌────────▼────────┐
       │  本地轨 (Local)  │     │  CDN 轨 (Remote) │
       ├──────────────────┤     ├──────────────────┤
       │ • 主包 MVP ≤4MB  │     │ • UOS CDN        │
       │ • 抖音本地分包    │     │ • AutoStreaming  │
       │   (下载到本机)   │     │   原图/重资源     │
       │ • 弱网/首启可用  │     │ • 按需下载+缓存  │
       └──────────────────┘     └──────────────────┘
```

**两套的含义（落到实现）**：
- **同一资源两种存在形态**：本地轨放「精简/低清/占位」副本（进包或本地分包），CDN 轨放「高清/原图」副本（UOS CDN）。运行时门面按 `loadMode` 选源。
- **AutoStreaming 本身就是双轨**：开启后引擎自动把重资源拆成 `32×32 占位图`（随包本地）+ `原图`（CDN 按需），运行时先显示占位、后台下载原图替换——零代码即得「本地 + CDN」。
- **Addressables 控制逻辑内容归属**：常用/首需资源放 Local Group（本地分包），大体量/非首需放 Remote Group（CDN）。

**`loadMode` 策略（建议默认 Auto）**：
| mode | 行为 | 适用 |
|------|------|------|
| `Local` | 只从本地轨加载（包内 + 本地分包） | 弱网、首启保底、开发期 |
| `CDN` | 只从 CDN 轨加载（UOS） | 正常网络、省本地存储 |
| `Auto`（默认） | 先本地占位/精简，后台预取 CDN 高清并缓存 | 发布期最佳体验 |

---

## 2. 资源分类矩阵

| 资源 | 归属轨 | 加载方式 | 说明 |
|------|--------|----------|------|
| 首场景依赖（代码/WASM/首场景） | 主包 | 随包 | 启动即读，不开 Streaming |
| MVP 美术（货币×3、建筑×4、结算背景×1、爱心×1，约 9 张 <200KB） | 主包 | 随包 | doc/15 §3，必须本地 |
| 宠物 6×7 表情 | 本地分包 | Addressables Local Group | 首启常用 |
| 食物 15 / 饭碗 8 | 本地分包 | Addressables Local Group | 首启常用 |
| UI 基础（Sliced/hamster/小院面板） | 本地分包 | Addressables Local Group | 首启常用 |
| `PetGame/Raw` 22M / `UI/Raw` 12M | **CDN** | AutoStreaming + Addressables Remote | 原图，按需 |
| `backgrounds` 8.1M | **CDN** | AutoStreaming | 背景大图 |
| 变异换色/小窝外观/成长外观（未来） | **CDN** | Addressables Remote (Label) | 后续内容 |
| 关卡/新宠物/新建筑（后续） | **CDN** | Addressables Remote (Label) | 逻辑分包 |

> 本地分包 + 主包合计需 ≤20MB（开虚拟支付 30MB）；CDN 轨不计包体。

---

## 3. 实施步骤（分阶段动手清单）

### 阶段 0 — 迁出 `Assets/Resources/` ⚠️ 阻断项
- **目标**：消除 20M 强制入包。
- **操作**：
  1. 列 `Assets/Resources/Art*`（Art/ArtBowls/ArtFoods/ArtPets）内容，确认与 `Assets/Art/PetGame` 是否重复。
  2. 若重复 → 直接删除 Resources 下副本。
  3. 若不重复 → 移到 `Assets/Art/PetGame/{pets,foods,bowls}` 对应目录。
  4. 检查代码里是否有 `Resources.Load(...)` 调用，改为 Addressables 引用（配置驱动，改引用路径即可，不改逻辑）。
- **验证**：工程无 `Assets/Resources/` 美术；`grep -r "Resources.Load" Assets/Scripts` 为 0。
- **产出**：目录整理完成。

### 阶段 1 — Addressables 初始化 + 双轨 Profile
- **目标**：建立本地 + 远程两套 LoadPath。
- **操作**：
  1. 安装 Addressables 包（Window → Package Manager → Addressables，团结 registry 内置）。
  2. `Window → Asset Management → Addressables → Groups` 初始化，生成 `Assets/AddressableAssetsData`。
  3. Profiles 新建 `Douyin-Local` 与 `Douyin-CDN`（或单 Profile 用变量）：
     - Local：`Local.BuildPath`=`Build/Local`，`Local.LoadPath`=`{UnityBuiltInLocalLocation}`
     - CDN：`Remote.BuildPath`=`ServerData`，`Remote.LoadPath`=`https://a.unity.cn/client_api/v1/buckets/<BUCKET_ID>/release_by_badge/latest/content/`
  4. 建 Group：`Local-MVP`（Bundle Mode=Pack Together，打包进本地）、`Remote-Heavy`（Bundle Mode=Pack Remote，走 CDN）。
  5. 把矩阵中「本地分包」资源拖入 `Local-MVP`，「CDN」资源拖入 `Remote-Heavy` 并打 Label。
- **验证**：Addressables 构建无报错；`Addressables.BuildPlayerContent` 成功生成本地 + ServerData。
- **产出**：`Assets/AddressableAssetsData`、双 Profile、双 Group。

### 阶段 2 — dummy 占位符修复脚本（抖音必踩）
- **目标**：CDN 路径不被 `https://dummy.dummy.dummy/` 占位。
- **操作**：落地 doc/15 §5.2 的 `FixDouyinAddressablePath.cs` 到 `Assets/Scripts/Runtime/`，无需挂载，`[RuntimeInitializeOnLoadMethod]` 自动替换。把 `<BUCKET_ID>` 换成真实桶。
- **验证**：真机/模拟器加载远程资源无 404。
- **产出**：`FixDouyinAddressablePath.cs`。

### 阶段 3 — AutoStreaming 内置配置（零代码双轨）
- **目标**：重资源自动「本地占位 + CDN 原图」。
- **操作**：
  1. 确认内置模块窗口入口（`Window` / 团结菜单 → **Auto Streaming**；若内置模块无独立窗口，找团结官方兼容版，**不要**装标准 Unity `3.0.1-pre.1`）。
  2. 开启 AutoStreaming，首场景**不**开 Streaming（避免黑屏）。
  3. 非首场景重资源（Raw/backgrounds/UI.Raw）标记为 Streaming → 自动拆占位(本地)+原图(CDN)。
  4. 构建时选 MiniGame + 抖音环境，Instant Game/AutoStreaming 自动接管。
- **验证**：构建后 `ServerData` 含被拆出的原图；首屏先占位后变清晰。
- **产出**：AutoStreaming 配置（工程设置）。

### 阶段 4 — 资源门面 `Res.Load`（双轨统一接口）
- **目标**：逻辑代码不感知本地/CDN 差异。
- **操作**：新增 `Assets/Scripts/Runtime/ResLoader.cs`，封装 `Addressables.LoadAssetAsync`，按 `loadMode`（Local/CDN/Auto）选 Group/Label；AutoStreaming 资源由引擎层自动双轨，门面只管 Addressables 部分。
- **验证**：切换 loadMode 能从不同轨取同一资源。
- **产出**：`ResLoader.cs`。

### 阶段 5 — UOS CDN 开通 + 上传
- **目标**：远程资源上线 CDN。
- **操作**：团结后台开 UOS CDN → 拿 `<BUCKET_ID>`+secret → `uas` CLI：`auth login` → `entries sync --bucket <ID> <ServerData路径(无空格)>` → `releases create`（版本管理，防旧缓存）。抖音开发者工具填链接白名单（去 `https://`）。
- **验证**：CDN 上资源可公网访问；`uas releases` 有 latest。
- **产出**：CDN 桶就绪。
- **开通入口（澄清）**：UOS 是**云服务**，在 `uos.unity.cn` 网页后台开通（建项目→开通 CDN→建 Bucket），**不是** Tuanjie Hub 安装时的勾选项。Hub 勾选的是引擎模块（`Instant Game Package` / `WeixinMiniGameSupport` / Auto Streaming 模块），与 UOS 云服务是两回事。
- **收费（2026-07-19 核实）**：试用 **20GB 境内流量免费**（仅开发/测试，不支持商用）；正式商用 **¥0.15/GB** 无门槛非阶梯，按月流量计费、账户余额模式。小游戏资源几十 MB、DAU 不高时成本极低（例：1GB/日 ≈ ¥4.5/月）。

### 阶段 6 — 构建 + 真机验证
- **操作**：MiniGame 模式 Build → `ByteGameOutput` → 抖音开发者工具导入 → 真机预览。
- **验收**（见 §5）：主包 ≤4MB、首启本地轨可玩、远程无 404、双轨可切换。

---

## 4. 加载门面骨架（ResLoader，示意）

```csharp
// Assets/Scripts/Runtime/ResLoader.cs
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum ResLoadMode { Local, CDN, Auto }

public static class ResLoader
{
    public static ResLoadMode Mode = ResLoadMode.Auto;

    public static AsyncOperationHandle<T> Load<T>(string key)
    {
        // Auto/CDN 走 Remote Group；Local 走 Local Group
        // 同一资源在两组用不同 Label 区分（如 "mvp_local" / "hd_cdn"）
        string label = Mode == ResLoadMode.Local ? key + "_local" : key + "_cdn";
        if (Mode == ResLoadMode.Auto)
        {
            // 先本地占位，后台预取 CDN 高清（简化示意）
            Addressables.LoadAssetAsync<T>(key + "_local");
            return Addressables.LoadAssetAsync<T>(key + "_cdn");
        }
        return Addressables.LoadAssetAsync<T>(label);
    }
}
```

> AutoStreaming 资源无需进门面——引擎在资源层已完成「本地占位 + CDN 原图」双轨，门面只管 Addressables 逻辑内容。

---

## 5. 验收标准

| 项 | 标准 | 检查方式 |
|----|------|----------|
| 主包体积 | ≤4MB | 抖音开发者工具 → 代码包体积 |
| 整体包体 | ≤20MB（开虚拟支付 30MB） | 同上 |
| 首启可玩 | 本地轨即可进入首场景+小院 | 断网/飞行模式进游戏 |
| 远程无 404 | CDN 资源加载成功 | 真机日志无 dummy/404 |
| 双轨可切换 | loadMode 切换生效 | 改 Mode 重新加载 |

---

## 6. 关键坑（doc/15 §8 + 本次新增）

| 坑 | 对策 |
|----|------|
| `Assets/Resources/` 强制入包 | 阶段 0 迁出，改用 Addressables |
| 标准 Unity AutoStreaming 包不兼容 | 用内置模块，禁装 `3.0.1-pre.1` |
| 首场景开 Streaming 黑屏 | 首场景资源随包，非首场景才 Streaming |
| dummy 占位符 404 | 阶段 2 脚本替换 |
| 双轨缓存冲突 | 本地/远程用不同 key/label，避免同名覆盖 |
| WASM 体积 | 引擎代码剔除 + 不用 Resources + 精简托管代码 |
| 版本未对齐旧缓存 | `uas releases create` 做版本管理 |

---

## 7. 下一步

- 本清单执行完 → 包体达标、双轨加载可用。
- **之后做 P5 离线收益**（宠物离线产出/委托，依赖 P1 存档已就绪）。

---

*文档版本 v1.0 | 2026-07-19 | 关联 doc/15*
