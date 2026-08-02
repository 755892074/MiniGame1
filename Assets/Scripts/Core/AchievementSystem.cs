using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 成就系统（方向③）。
/// 成就定义全部基于现有存档字段，无需新增统计。
/// 在 通关 / 救助宠物 / 铲屎官升级 / 建筑升级 / 启动 时调用 CheckAll() 自动判定解锁。
/// 解锁后写入 SaveSystem.GameSave.unlockedAchievements，并发放救助徽章(rescueBadge)。
/// </summary>
public static class AchievementSystem
{
    [Serializable]
    public class AchievementDef
    {
        public string id;
        public string name;
        public string desc;
        public int badgeReward;
        public Func<bool> isMet;
    }

    public static event Action<AchievementDef> OnUnlocked;

    private static bool _toastWired = false;
    /// <summary>订阅 OnUnlocked → Toast（幂等，仅首次生效）。</summary>
    public static void EnsureToastWired()
    {
        if (_toastWired) return;
        _toastWired = true;
        OnUnlocked += d => Toast.Show("成就解锁: " + d.name);
    }

    private static List<AchievementDef> _defs;
    public static IReadOnlyList<AchievementDef> Defs
    {
        get { if (_defs == null) _defs = BuildDefs(); return _defs; }
    }

    private static AchievementDef Def(string id, string name, string desc, int badge, Func<bool> met)
    {
        return new AchievementDef { id = id, name = name, desc = desc, badgeReward = badge, isMet = met };
    }

    private static List<AchievementDef> BuildDefs()
    {
        var list = new List<AchievementDef>
        {
            Def("first_clear", "初次通关", "完成你的第一关", 1, () => SaveSystem.Data.totalLevelsCompleted >= 1),
            Def("clear_10", "渐入佳境", "累计通关 10 关", 2, () => SaveSystem.Data.highestUnlockedLevel >= 11),
            Def("clear_30", "铲屎老兵", "累计通关 30 关", 3, () => SaveSystem.Data.highestUnlockedLevel >= 31),
            Def("star_30", "完美主义", "累计收集 30 颗星", 3, () => SaveSystem.TotalStars >= 30),
            Def("pet_all", "猫狗双全", "集齐全部 6 只宠物", 5, () => SaveSystem.RescuedPetCount >= 6),
            Def("cleaner_5", "资深铲屎官", "铲屎官等级达到 5 级", 3, () => SaveSystem.Data.cleanerLevel >= 5),
            Def("build_max", "基建狂魔", "所有建筑升至当前上限", 3, AllBuildingsMaxed),
            Def("rich", "腰缠万贯", "金币≥5000 或获得彩虹毛球", 2, () => SaveSystem.Data.gold >= 5000 || SaveSystem.Data.rainbowBall >= 1),
        };
        return list;
    }

    private static bool AllBuildingsMaxed()
    {
        int cap = YardDefs.MaxLevelFor(SaveSystem.Data.houseLevel);
        foreach (var id in new[] { "foodbowl", "toy", "medical", "garden" })
        {
            if (SaveSystem.GetBuildingLevel(id) < cap) return false;
        }
        return true;
    }

    public static bool IsUnlocked(string id)
    {
        var list = SaveSystem.Data.unlockedAchievements;
        return list != null && list.Contains(id);
    }

    public static int UnlockedCount
    {
        get { int n = 0; foreach (var d in Defs) if (IsUnlocked(d.id)) n++; return n; }
    }

    /// <summary>检查并解锁所有已达成但未记录的成就。silent=true 时不弹 toast（用于启动期回溯）。</summary>
    public static void CheckAll(bool silent = false)
    {
        var list = SaveSystem.Data.unlockedAchievements;
        if (list == null) { list = new List<string>(); SaveSystem.Data.unlockedAchievements = list; }
        bool changed = false;
        foreach (var d in Defs)
        {
            if (IsUnlocked(d.id)) continue;
            if (d.isMet != null && d.isMet())
            {
                list.Add(d.id);
                if (d.badgeReward > 0) SaveSystem.AddBadge(d.badgeReward);
                changed = true;
                if (!silent) OnUnlocked?.Invoke(d);
            }
        }
        if (changed) SaveSystem.Save();
    }
}
