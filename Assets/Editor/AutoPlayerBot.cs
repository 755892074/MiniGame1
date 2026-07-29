using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 自动玩家机器人 — 模拟真实玩家完整游玩流程
/// 菜单入口：
///   铲屎官疯了/AutoPlayer/通关 1-10
///   铲屎官疯了/AutoPlayer/通关全部
///   铲屎官疯了/AutoPlayer/完整玩家流程
///   铲屎官疯了/AutoPlayer/经济系统测试
///   铲屎官疯了/AutoPlayer/菜单流程测试
/// </summary>
public class AutoPlayerBot : EditorWindow
{
    [MenuItem("铲屎官疯了/AutoPlayer/通关 1-10")]
    static void RunLevels1to10() => StartBot(RunScenario_PlayLevels(1, 10));

    [MenuItem("铲屎官疯了/AutoPlayer/通关全部")]
    static void RunAllLevels() => StartBot(RunScenario_PlayLevels(1, 999));

    [MenuItem("铲屎官疯了/AutoPlayer/完整玩家流程")]
    static void RunFullFlow() => StartBot(RunScenario_FullFlow());

    [MenuItem("铲屎官疯了/AutoPlayer/经济系统测试")]
    static void RunEconomy() => StartBot(RunScenario_Economy());

    [MenuItem("铲屎官疯了/AutoPlayer/菜单UI流程测试")]
    static void RunMenuUI() => StartBot(RunScenario_MenuUI());

    [MenuItem("铲屎官疯了/AutoPlayer/小院建筑系统测试")]
    static void RunYard() => StartBot(RunScenario_YardSystem());

    [MenuItem("铲屎官疯了/AutoPlayer/宠物养成系统测试")]
    static void RunPets() => StartBot(RunScenario_PetSystem());

    [MenuItem("铲屎官疯了/AutoPlayer/全量回归测试(50关)")]
    static void RunRegression() => StartBot(RunScenario_Regression50());

    static bool isRunning;

    static void StartBot(IEnumerator scenario)
    {
        if (isRunning) { Debug.LogWarning("[Bot] 已在运行中"); return; }
        isRunning = true;
        EditorCoroutine.Start(Run(scenario));
    }

    static IEnumerator Run(IEnumerator scenario)
    {
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorApplication.isPlaying = true;
            yield return WaitForSeconds(3f);
        }
        yield return scenario;
        Debug.Log("[Bot] === 流程结束 ===");
        isRunning = false;
        EditorApplication.isPlaying = false;
    }

    // ============================================================
    // 核心引擎：自动通关单个关卡
    // ============================================================
    static (bool solved, int steps, int finalScore) PlayCurrentLevel(float stepDelay = 2f)
    {
        var gm = PetGameManager.Instance;
        if (gm == null || gm.fsm == null) return (false, 0, 0);

        int steps = 0;
        while (steps < 80)
        {
            string stateName = gm.fsm?.CurrentState?.GetType().Name ?? "null";

            // Win check
            if (gm.GetPetQueue().Count == 0)
            {
                if (stateName == "WinState" || stateName == "IdleState") return (true, steps, gm.GetScore());
            }

            // Wait for Idle
            if (stateName != "IdleState") return (false, steps, gm.GetScore()); // timeout fallback

            // Deadlock
            if (gm.CheckDeadlock())
            {
                Debug.LogWarning($"[Bot] L{gm.currentLevelId} 死局! 加空碗");
                gm.AddBowl();
                continue;
            }

            // Solve step
            var step = gm.Hint();
            if (step == null) { Debug.LogWarning($"[Bot] L{gm.currentLevelId} 求解器null"); return (false, steps, gm.GetScore()); }

            var s = step.Value;
            var bowls = gm.GetBowls();
            if (s.fromId >= bowls.Count || s.toId >= bowls.Count) return (false, steps, gm.GetScore());

            Debug.Log($"[Bot] L{gm.currentLevelId} step{steps}: 碗{s.fromId}→碗{s.toId} ({s.count}x {s.food})");
            gm.PourFromTo(s.fromId, s.toId, gm.fsm);
            steps++;
            // stepDelay handled by caller's coroutine
            break; // Only do one step per call
        }
        return (false, steps, gm.GetScore());
    }

    // ============================================================
    // 场景 1：通关多个关卡
    // ============================================================
    static IEnumerator RunScenario_PlayLevels(int fromLevel, int toLevel)
    {
        var report = new StringBuilder();
        report.AppendLine("\n╔══ AutoPlayer 通关报告 ══╗");

        // Navigate to game
        var ctrl = Object.FindObjectOfType<MenuSceneController>();
        if (ctrl != null) { ctrl.EnterGame(fromLevel); yield return WaitForSeconds(4f); }

        int goldBefore = SaveSystem.Data.gold;
        int passCount = 0, failCount = 0;
        int totalLevels = Mathf.Min(toLevel, GetLevelCount());

        var gm = PetGameManager.Instance;
        if (gm == null) { Debug.LogWarning("[Bot] No PetGameManager"); yield break; }

        for (int currentTarget = fromLevel; currentTarget <= totalLevels; currentTarget++)
        {
            // Start level directly (avoids scene reload timing issues)
            gm.StartLevel(currentTarget);
            yield return WaitForSeconds(1.5f);

            Debug.Log($"[Bot] L{currentTarget}: levelId={gm.currentLevelId} pets={gm.GetPetQueue().Count} bowls={gm.GetBowls().Count}");

            int goldBeforeLvl = SaveSystem.Data.gold;
            int stepCount = 0;
            bool solved = false;

            while (stepCount < 80)
            {
                string stateName = gm.fsm?.CurrentState?.GetType().Name ?? "null";
                if (gm.GetPetQueue().Count == 0 && (stateName == "WinState" || stateName == "IdleState")) { solved = true; break; }
                if (stateName != "IdleState") { yield return WaitForSeconds(0.5f); continue; }
                if (gm.CheckDeadlock()) { gm.AddBowl(); yield return WaitForSeconds(1f); continue; }
                var step = gm.Hint();
                if (step == null) break;
                var s = step.Value;
                var bowls = gm.GetBowls();
                if (s.fromId >= bowls.Count || s.toId >= bowls.Count) break;
                gm.PourFromTo(s.fromId, s.toId, gm.fsm);
                stepCount++;
                yield return WaitForSeconds(2f);
            }

            yield return WaitForSeconds(1f);

            int goldEarned = SaveSystem.Data.gold - goldBeforeLvl;
            var sr = SaveSystem.Data.levelStars?.Find(x => x.levelId == currentTarget);
            int stars = sr?.stars ?? 0;

            if (solved) { passCount++; report.AppendLine($"L{currentTarget}: PASS ✅ {stars}★ {stepCount}步 得分{gm.GetScore()} 金币+{goldEarned}"); }
            else { failCount++; report.AppendLine($"L{currentTarget}: FAIL ❌ {stepCount}步"); }
        }

        report.AppendLine($"\n══ 总结: 通过{passCount} 失败{failCount} ══");
        report.AppendLine($"金币: {goldBefore}→{SaveSystem.Data.gold} (+{SaveSystem.Data.gold - goldBefore})");
        report.AppendLine($"小鱼干: {SaveSystem.Data.fishDiscount}  等级: Lv{SaveSystem.Data.cleanerLevel}");
        Debug.Log("[Bot]" + report.ToString());
    }

    // ============================================================
    // 场景 2：完整玩家流程
    // ============================================================
    static IEnumerator RunScenario_FullFlow()
    {
        var report = new StringBuilder();
        report.AppendLine("\n╔══ 完整玩家流程报告 ══╗");

        // Phase 1: 主菜单
        Debug.Log("[Bot] Phase 1: 主菜单");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MenuScene")
        {
            GameSceneManager.LoadMenu();
            yield return WaitForSeconds(3f);
        }
        yield return WaitForSeconds(2f);

        var activeBtns = new List<string>();
        foreach (var b in Object.FindObjectsOfType<Button>())
            if (b.gameObject.activeInHierarchy) activeBtns.Add(b.name);
        report.AppendLine($"Phase 1 主菜单按钮: {string.Join(", ", activeBtns)}");

        // Phase 2: 进游戏通关3关
        Debug.Log("[Bot] Phase 2: 通关3关");
        var ctrl = Object.FindObjectOfType<MenuSceneController>();
        if (ctrl != null) ctrl.EnterGame(1);
        yield return WaitForSeconds(4f);

        int gold0 = SaveSystem.Data.gold;
        for (int lvl = 1; lvl <= 3; lvl++)
        {
            yield return WaitForLevelReady(lvl);
            var gm = PetGameManager.Instance;
            if (gm == null) { report.AppendLine($"Phase2 L{lvl}: FAIL"); continue; }

            int steps = 0;
            while (steps < 80)
            {
                string st = gm.fsm?.CurrentState?.GetType().Name ?? "null";
                if (gm.GetPetQueue().Count == 0 && (st == "WinState" || st == "IdleState")) break;
                if (st != "IdleState") { yield return WaitForSeconds(0.5f); continue; }
                if (gm.CheckDeadlock()) { gm.AddBowl(); yield return WaitForSeconds(1f); continue; }
                var step = gm.Hint();
                if (step == null) break;
                gm.PourFromTo(step.Value.fromId, step.Value.toId, gm.fsm);
                steps++;
                yield return WaitForSeconds(2f);
            }
            report.AppendLine($"Phase2 L{lvl}: {(gm.GetPetQueue().Count == 0 ? "PASS ✅" : "FAIL ❌")} {steps}步");
            yield return WaitForSeconds(1.5f);
            if (lvl < 3) { var nb = GameObject.Find("btnNext")?.GetComponent<Button>(); if (nb != null) nb.onClick.Invoke(); yield return WaitForSeconds(3f); }
        }

        // Phase 3: 返回主菜单
        Debug.Log("[Bot] Phase 3: 返回主菜单");
        GameSceneManager.LoadMenu();
        yield return WaitForSeconds(3f);

        // Phase 4: 打开设置面板
        Debug.Log("[Bot] Phase 4: 测试设置面板");
        var ctrl2 = Object.FindObjectOfType<MenuSceneController>();
        if (ctrl2 != null) ctrl2.ShowSettings();
        yield return WaitForSeconds(1f);
        var settingsBtns = new List<string>();
        foreach (var b in Object.FindObjectsOfType<Button>())
            if (b.gameObject.activeInHierarchy) settingsBtns.Add(b.name);
        report.AppendLine($"Phase 4 设置面板按钮: {string.Join(", ", settingsBtns)}");

        // Close settings
        var closeBtn = GameObject.Find("btnClose")?.GetComponent<Button>();
        if (closeBtn != null) closeBtn.onClick.Invoke();
        yield return WaitForSeconds(0.5f);

        // Phase 5: 打开选关面板
        Debug.Log("[Bot] Phase 5: 测试选关面板");
        if (ctrl2 != null) ctrl2.ShowLevelSelect();
        yield return WaitForSeconds(2f);
        var levelBtns = new List<string>();
        foreach (var b in Object.FindObjectsOfType<Button>())
            if (b.gameObject.activeInHierarchy && (b.name.Contains("Btn") || b.name.Contains("btn"))) levelBtns.Add(b.name);
        report.AppendLine($"Phase 5 选关面板按钮数: {levelBtns.Count}");

        // Phase 6: 存档验证
        report.AppendLine($"\n--- 存档验证 ---");
        report.AppendLine($"金币: {gold0}→{SaveSystem.Data.gold} (+{SaveSystem.Data.gold - gold0})");
        report.AppendLine($"小鱼干: {SaveSystem.Data.fishDiscount}  徽章: {SaveSystem.Data.rescueBadge}");
        report.AppendLine($"等级: Lv{SaveSystem.Data.cleanerLevel}  通关数: {SaveSystem.Data.totalLevelsCompleted}");
        report.AppendLine($"宠物: {SaveSystem.Data.pets.Count}只  建筑: {SaveSystem.Data.buildings.Count}个");

        Debug.Log("[Bot]" + report.ToString());
    }

    // ============================================================
    // 场景 3：经济系统测试
    // ============================================================
    static IEnumerator RunScenario_Economy()
    {
        var report = new StringBuilder();
        report.AppendLine("\n╔══ 经济系统报告 ══╗");

        int gold0 = SaveSystem.Data.gold;
        int fish0 = SaveSystem.Data.fishDiscount;
        int badge0 = SaveSystem.Data.rescueBadge;
        report.AppendLine($"初始: 金币={gold0} 小鱼干={fish0} 徽章={badge0}");

        // 通关5关
        var ctrl = Object.FindObjectOfType<MenuSceneController>();
        if (ctrl != null) ctrl.EnterGame(1);
        else { GameSceneManager.LoadMenu(); yield return WaitForSeconds(3f); Object.FindObjectOfType<MenuSceneController>()?.EnterGame(1); }
        yield return WaitForSeconds(4f);

        for (int lvl = 1; lvl <= 5; lvl++)
        {
            yield return WaitForLevelReady(lvl);
            var gm = PetGameManager.Instance;
            if (gm == null) break;

            int goldBefore = SaveSystem.Data.gold;
            int fishBefore = SaveSystem.Data.fishDiscount;

            int steps = 0;
            while (steps < 80)
            {
                string st = gm.fsm?.CurrentState?.GetType().Name ?? "null";
                if (gm.GetPetQueue().Count == 0 && (st == "WinState" || st == "IdleState")) break;
                if (st != "IdleState") { yield return WaitForSeconds(0.5f); continue; }
                if (gm.CheckDeadlock()) { gm.AddBowl(); yield return WaitForSeconds(1f); continue; }
                var step = gm.Hint();
                if (step == null) break;
                gm.PourFromTo(step.Value.fromId, step.Value.toId, gm.fsm);
                steps++;
                yield return WaitForSeconds(2f);
            }
            yield return WaitForSeconds(1.5f);

            report.AppendLine($"L{lvl}: 金币{SaveSystem.Data.gold - goldBefore:+#;-#;0} 小鱼干{SaveSystem.Data.fishDiscount - fishBefore:+#;-#;0}");

            if (lvl < 5) { var nb = GameObject.Find("btnNext")?.GetComponent<Button>(); if (nb != null) nb.onClick.Invoke(); yield return WaitForSeconds(3f); }
        }

        report.AppendLine($"\n总变化: 金币+{SaveSystem.Data.gold - gold0} 小鱼干+{SaveSystem.Data.fishDiscount - fish0} 徽章+{SaveSystem.Data.rescueBadge - badge0}");
        report.AppendLine($"等级: Lv{SaveSystem.Data.cleanerLevel} Exp={SaveSystem.Data.cleanerExp}");

        // 建筑系统
        report.AppendLine("\n--- 建筑升级检查 ---");
        foreach (var b in YardDefs.BUILDINGS)
        {
            if (YardDefs.TryGetUpgradeCost(b.id, 1, out var cost))
                report.AppendLine($"  {b.name}: 需要 {cost.gold}金币/{cost.fish}鱼干/{cost.badge}徽章");
        }
        bool canUpgrade = SaveSystem.Data.gold >= 100;
        report.AppendLine($"  当前金币{SaveSystem.Data.gold} → 可升级食盆: {(canUpgrade ? "YES ✅" : "NO ❌")}");

        Debug.Log("[Bot]" + report.ToString());
    }

    // ============================================================
    // 场景 4：菜单 UI 流程测试
    // ============================================================
    static IEnumerator RunScenario_MenuUI()
    {
        var report = new StringBuilder();
        report.AppendLine("\n╔══ 菜单UI流程报告 ══╗");

        // 1. 主菜单
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MenuScene")
        {
            GameSceneManager.LoadMenu();
            yield return WaitForSeconds(3f);
        }
        report.AppendLine("Phase 1: 主菜单");
        var errors = new List<string>();
        var btns = Object.FindObjectsOfType<Button>();
        foreach (var b in btns)
        {
            if (!b.gameObject.activeInHierarchy) continue;
            var img = b.GetComponent<Image>();
            bool hasSprite = img != null && (img.sprite != null);
            // Check child sprites
            if (!hasSprite)
            {
                var childImgs = b.GetComponentsInChildren<Image>(true);
                foreach (var ci in childImgs) if (ci.sprite != null) { hasSprite = true; break; }
            }
            if (!hasSprite && b.GetComponent<Text>() == null)
                errors.Add($"  按钮无素材: {b.name}");
        }
        if (errors.Count == 0) report.AppendLine("  按钮素材: ALL OK ✅");
        else errors.ForEach(e => report.AppendLine(e));

        // 2. 测试设置面板
        report.AppendLine("\nPhase 2: 设置面板");
        var ctrl = Object.FindObjectOfType<MenuSceneController>();
        if (ctrl != null) { ctrl.ShowSettings(); yield return WaitForSeconds(1f); }
        var card = GameObject.Find("imgCardBg");
        report.AppendLine($"  卡片渲染: {(card != null && card.activeInHierarchy ? "OK ✅" : "FAIL ❌")}");
        var close = GameObject.Find("btnClose");
        if (close != null) { close.GetComponent<Button>().onClick.Invoke(); yield return WaitForSeconds(0.5f); }

        // 3. 测试选关面板
        report.AppendLine("\nPhase 3: 选关面板");
        if (ctrl != null) { ctrl.ShowLevelSelect(); yield return WaitForSeconds(2f); }
        var scroll = GameObject.Find("LevelScrollView");
        report.AppendLine($"  滚动视图: {(scroll != null ? "OK ✅" : "FAIL ❌")}");
        var content = GameObject.Find("Content");
        int childCount = content?.transform.childCount ?? 0;
        report.AppendLine($"  关卡按钮数: {childCount} {(childCount > 0 ? "✅" : "❌")}");

        // 4. 进游戏
        report.AppendLine("\nPhase 4: 进入游戏");
        var gameBtn = GameObject.Find("btnBack")?.GetComponent<Button>();
        if (gameBtn != null) gameBtn.onClick.Invoke();
        yield return WaitForSeconds(1f);
        if (ctrl != null) ctrl.EnterGame(1);
        yield return WaitForSeconds(4f);
        var gm = PetGameManager.Instance;
        report.AppendLine($"  游戏加载: {(gm != null && gm.fsm != null ? "OK ✅" : "FAIL ❌")}");
        if (gm != null)
        {
            report.AppendLine($"  关卡: {gm.currentLevelId}  碗: {gm.GetBowls().Count}  宠物: {gm.GetPetQueue().Count}");
        }

        Debug.Log("[Bot]" + report.ToString());
    }

    // ============================================================
    // 场景 5：小院建筑系统测试
    // ============================================================
    static IEnumerator RunScenario_YardSystem()
    {
        var report = new StringBuilder();
        report.AppendLine("\n╔══ 小院建筑系统报告 ══╗");

        int gold0 = SaveSystem.Data.gold;
        int fish0 = SaveSystem.Data.fishDiscount;
        int badge0 = SaveSystem.Data.rescueBadge;

        // Phase 1: 通关3关积累货币
        report.AppendLine("Phase 1: 通关3关积累货币...");
        var ctrl = Object.FindObjectOfType<MenuSceneController>();
        if (ctrl != null) { ctrl.EnterGame(1); yield return WaitForSeconds(4f); }

        var gm = PetGameManager.Instance;
        if (gm != null)
        {
            for (int lvl = 1; lvl <= 3; lvl++)
            {
                gm.StartLevel(lvl);
                yield return WaitForSeconds(1.5f);
                int steps = 0;
                while (steps < 80)
                {
                    string st = gm.fsm?.CurrentState?.GetType().Name ?? "null";
                    if (gm.GetPetQueue().Count == 0) break;
                    if (st != "IdleState") { yield return WaitForSeconds(0.5f); continue; }
                    if (gm.CheckDeadlock()) { gm.AddBowl(); yield return WaitForSeconds(1f); continue; }
                    var step = gm.Hint();
                    if (step == null) break;
                    gm.PourFromTo(step.Value.fromId, step.Value.toId, gm.fsm);
                    steps++;
                    yield return WaitForSeconds(2f);
                }
                yield return WaitForSeconds(1f);
            }
        }

        report.AppendLine($"  通关后: 金币={SaveSystem.Data.gold} 小鱼干={SaveSystem.Data.fishDiscount} 徽章={SaveSystem.Data.rescueBadge}");

        // Phase 2: 测试建筑升级
        report.AppendLine("\nPhase 2: 建筑升级测试");
        foreach (var b in YardDefs.BUILDINGS)
        {
            int lvBefore = SaveSystem.GetBuildingLevel(b.id);
            var info = SaveSystem.GetBuildingUpgradeInfo(b.id);

            report.AppendLine($"  {b.name}({b.id}): Lv{info.currentLevel}/{info.maxLevel} {(info.maxed ? "满级" : "")}");
            if (!info.maxed)
            {
                report.AppendLine($"    升级费: {info.goldCost}金/{info.fishCost}鱼/{info.badgeCost}徽章 可负担={info.affordable}");
                if (info.affordable)
                {
                    bool ok = SaveSystem.TryUpgradeBuilding(b.id);
                    int lvAfter = SaveSystem.GetBuildingLevel(b.id);
                    report.AppendLine($"    升级结果: {(ok ? "✅ 成功" : "❌ 失败")} Lv{lvBefore}→{lvAfter}");
                }
                else
                {
                    report.AppendLine($"    跳过(货币不足)");
                }
            }
        }

        // Phase 3: 测试住所升级
        report.AppendLine("\nPhase 3: 住所升级测试");
        int houseBefore = SaveSystem.Data.houseLevel;
        bool houseUp = SaveSystem.UpgradeHouse(5);
        report.AppendLine($"  住所 Lv{houseBefore}→{SaveSystem.Data.houseLevel} {(houseUp ? "✅" : "已满级或失败")}");

        // Phase 4: 验证货币扣除
        report.AppendLine($"\nPhase 4: 货币变化");
        report.AppendLine($"  金币: {gold0}→{SaveSystem.Data.gold} ({SaveSystem.Data.gold - gold0:+#;-#;0})");
        report.AppendLine($"  小鱼干: {fish0}→{SaveSystem.Data.fishDiscount} ({SaveSystem.Data.fishDiscount - fish0:+#;-#;0})");
        report.AppendLine($"  徽章: {badge0}→{SaveSystem.Data.rescueBadge} ({SaveSystem.Data.rescueBadge - badge0:+#;-#;0})");
        report.AppendLine($"  建筑数: {SaveSystem.Data.buildings.Count}");

        // Phase 5: 验证建筑效果
        report.AppendLine("\nPhase 5: 建筑效果验证");
        foreach (var b in YardDefs.BUILDINGS)
        {
            int lv = SaveSystem.GetBuildingLevel(b.id);
            string effect = YardDefs.EffectValue(b.id, lv);
            report.AppendLine($"  {b.name} Lv{lv}: {effect}");
        }

        Debug.Log("[Bot]" + report.ToString());
    }

    // ============================================================
    // 场景 6：宠物养成系统测试
    // ============================================================
    static IEnumerator RunScenario_PetSystem()
    {
        var report = new StringBuilder();
        report.AppendLine("\n╔══ 宠物养成系统报告 ══╗");

        int fish0 = SaveSystem.Data.fishDiscount;
        int petCount0 = SaveSystem.Data.pets.Count;

        // Phase 1: 救助所有6种宠物
        report.AppendLine("Phase 1: 救助宠物");
        foreach (PetType pt in System.Enum.GetValues(typeof(PetType)))
        {
            bool wasRescued = SaveSystem.IsPetRescued(pt);
            SaveSystem.RescuePet(pt);
            bool nowRescued = SaveSystem.IsPetRescued(pt);
            int stage = SaveSystem.GetPetStage(pt);
            report.AppendLine($"  {pt}: 救助前={wasRescued} 救助后={nowRescued} 阶段={stage} {(nowRescued ? "✅" : "❌")}");
        }
        report.AppendLine($"  已救助总数: {SaveSystem.RescuedPetCount}");

        // Phase 2: 喂食所有宠物
        report.AppendLine("\nPhase 2: 喂食宠物(消耗小鱼干)");
        foreach (PetType pt in System.Enum.GetValues(typeof(PetType)))
        {
            int intimacyBefore = 0;
            var pet = SaveSystem.Data.pets.Find(p => p.petType == pt);
            if (pet != null) intimacyBefore = pet.intimacy;

            bool fed = SaveSystem.FeedPet(pt, 1);
            int intimacyAfter = pet?.intimacy ?? 0;
            report.AppendLine($"  {pt}: 喂食={fed} 亲密度 {intimacyBefore}→{intimacyAfter} (+{intimacyAfter - intimacyBefore})");
        }
        report.AppendLine($"  小鱼干: {fish0}→{SaveSystem.Data.fishDiscount} ({SaveSystem.Data.fishDiscount - fish0:+#;-#;0})");

        // Phase 3: 互动(每日限1次)
        report.AppendLine("\nPhase 3: 互动(每日限1次)");
        foreach (PetType pt in System.Enum.GetValues(typeof(PetType)))
        {
            bool canInteract = SaveSystem.CanInteractToday(pt);
            if (canInteract)
            {
                int before = SaveSystem.Data.pets.Find(p => p.petType == pt)?.intimacy ?? 0;
                bool done = SaveSystem.InteractPet(pt);
                int after = SaveSystem.Data.pets.Find(p => p.petType == pt)?.intimacy ?? 0;
                report.AppendLine($"  {pt}: 互动={done} 亲密度 {before}→{after} (+{after - before})");
            }
            else
            {
                report.AppendLine($"  {pt}: 今日已互动");
            }
        }

        // Phase 4: 宠物成长阶段升级
        report.AppendLine("\nPhase 4: 宠物成长阶段");
        foreach (PetType pt in System.Enum.GetValues(typeof(PetType)))
        {
            int stage0 = SaveSystem.GetPetStage(pt);
            SaveSystem.UpgradePetStage(pt);
            int stage1 = SaveSystem.GetPetStage(pt);
            report.AppendLine($"  {pt}: 阶段 {stage0}→{stage1}");
        }

        // Phase 5: 统计
        report.AppendLine($"\n--- 统计 ---");
        report.AppendLine($"宠物总数: {petCount0}→{SaveSystem.Data.pets.Count}");
        report.AppendLine($"总救助数: {SaveSystem.Data.totalPetsRescued}");
        report.AppendLine($"小鱼干: {fish0}→{SaveSystem.Data.fishDiscount}");
        foreach (var p in SaveSystem.Data.pets)
            report.AppendLine($"  {p.petType}: stage={p.stage} intimacy={p.intimacy} rare={p.isRare}");

        Debug.Log("[Bot]" + report.ToString());
        yield return null;
    }

    // ============================================================
    // 场景 7：50关全量回归测试
    // ============================================================
    static IEnumerator RunScenario_Regression50()
    {
        var report = new StringBuilder();
        report.AppendLine("\n╔══ 50关全量回归测试 ══╗");

        var ctrl = Object.FindObjectOfType<MenuSceneController>();
        if (ctrl != null) { ctrl.EnterGame(1); yield return WaitForSeconds(4f); }

        var gm = PetGameManager.Instance;
        if (gm == null) { report.AppendLine("FAIL: 无 PetGameManager"); Debug.Log("[Bot]" + report.ToString()); yield break; }

        int totalLevels = gm.LevelCount;
        int passCount = 0, failCount = 0;
        int totalSteps = 0;
        int goldBefore = SaveSystem.Data.gold;
        var failedLevels = new List<int>();
        var deadlockLevels = new List<int>();
        float startTime = Time.realtimeSinceStartup;

        for (int lvl = 1; lvl <= totalLevels; lvl++)
        {
            gm.StartLevel(lvl);
            yield return WaitForSeconds(1f);

            int steps = 0;
            bool solved = false;
            bool hitDeadlock = false;

            while (steps < 80)
            {
                string st = gm.fsm?.CurrentState?.GetType().Name ?? "null";
                if (gm.GetPetQueue().Count == 0 && (st == "WinState" || st == "IdleState")) { solved = true; break; }
                if (st != "IdleState") { yield return WaitForSeconds(0.3f); continue; }
                if (gm.CheckDeadlock()) { hitDeadlock = true; gm.AddBowl(); yield return WaitForSeconds(0.5f); continue; }
                var step = gm.Hint();
                if (step == null) break;
                gm.PourFromTo(step.Value.fromId, step.Value.toId, gm.fsm);
                steps++;
                yield return WaitForSeconds(1.5f);
            }

            if (solved)
            {
                passCount++;
                totalSteps += steps;
                if (hitDeadlock) deadlockLevels.Add(lvl);
            }
            else
            {
                failCount++;
                failedLevels.Add(lvl);
                Debug.LogWarning($"[Bot] L{lvl} FAIL! steps={steps} deadlock={hitDeadlock} pets={gm.GetPetQueue().Count}");
            }

            // Progress report every 10 levels
            if (lvl % 10 == 0)
            {
                Debug.Log($"[Bot] 进度: {lvl}/{totalLevels} 通过={passCount} 失败={failCount}");
            }
        }

        float elapsed = Time.realtimeSinceStartup - startTime;

        report.AppendLine($"通过: {passCount}/{totalLevels}");
        report.AppendLine($"失败: {failCount} 关: [{string.Join(", ", failedLevels.ToArray())}]");
        report.AppendLine($"死局救援: {deadlockLevels.Count} 关: [{string.Join(", ", deadlockLevels.ToArray())}]");
        report.AppendLine($"总步数: {totalSteps}  平均: {(passCount > 0 ? (float)totalSteps / passCount : 0):F1}步/关");
        report.AppendLine($"金币: {goldBefore}→{SaveSystem.Data.gold} (+{SaveSystem.Data.gold - goldBefore})");
        report.AppendLine($"小鱼干: {SaveSystem.Data.fishDiscount}  等级: Lv{SaveSystem.Data.cleanerLevel}");
        report.AppendLine($"耗时: {elapsed:F0}秒");

        if (failCount == 0)
            report.AppendLine("\n🎉 全部通过！无回归！");
        else
            report.AppendLine($"\n⚠️ 有{failCount}关失败，需检查！");

        Debug.Log("[Bot]" + report.ToString());
    }

    // ============================================================
    // Helpers
    // ============================================================
    static IEnumerator WaitForLevelReady(int levelId)
    {
        float timeout = 8f;
        while (timeout > 0)
        {
            var gm = PetGameManager.Instance;
            if (gm != null && gm.fsm != null && gm.fsm.IsRunning && gm.currentLevelId == levelId) break;
            timeout -= 0.1f;
            yield return null;
        }
    }

    static int GetLevelCount()
    {
        var gm = PetGameManager.Instance;
        return gm != null ? gm.LevelCount : 50;
    }

    static IEnumerator WaitForSeconds(float seconds)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup < start + seconds) yield return null;
    }

    public class EditorCoroutine
    {
        IEnumerator routine;
        EditorCoroutine(IEnumerator r) { routine = r; }
        public static EditorCoroutine Start(IEnumerator r)
        {
            var c = new EditorCoroutine(r);
            EditorApplication.update += c.Tick;
            return c;
        }
        void Tick()
        {
            if (!routine.MoveNext()) EditorApplication.update -= Tick;
        }
    }
}
