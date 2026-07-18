using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 小院系统共享配置（P2/P3/P4 共用）
/// - 建筑：成本表 / 效果计算
/// - 宠物：亲密度等级 / 解锁表
/// 单一数据源，避免面板与弹窗各自硬编码导致不一致。
/// </summary>
public static class YardDefs
{
    // ============================================================
    // 建筑
    // ============================================================
    public struct BuildingInfo
    {
        public string id;
        public string icon;
        public string name;
        public string effectDesc;   // 效果说明文案
    }

    public static readonly BuildingInfo[] BUILDINGS = new[]
    {
        new BuildingInfo { id = "foodbowl", icon = "🥣", name = "食盆", effectDesc = "全宠物亲密度增益" },
        new BuildingInfo { id = "toy",      icon = "🧸", name = "玩具", effectDesc = "小鱼干产出速率" },
        new BuildingInfo { id = "medical",  icon = "🏥", name = "医疗", effectDesc = "离线产出上限" },
        new BuildingInfo { id = "garden",   icon = "🌻", name = "花园", effectDesc = "亲密度每日自然增长" },
    };

    public const int MAX_BUILDING_LEVEL = 6;

    /// <summary>建筑等级上限受住所等级限制（design §3.4）</summary>
    public static int MaxLevelFor(int houseLevel)
        => Mathf.Min(MAX_BUILDING_LEVEL, houseLevel + 1);

    // 升级到下一级的消耗（按当前等级 1..5 索引，共 5 段：1→2 ... 5→Max）
    // 设计文档 §3.3
    private static readonly Dictionary<string, (int gold, int fish, int badge)[]> UPGRADE_COST = new Dictionary<string, (int, int, int)[]>
    {
        ["foodbowl"] = new[] { (100,0,0), (300,0,0), (800,0,0), (2000,0,0), (5000,0,0) },
        ["toy"]      = new[] { (100,0,0), (300,0,0), (800,0,0), (2000,0,0), (5000,0,0) },
        ["medical"]  = new[] { (150,0,1), (400,0,2), (1000,0,3), (2500,0,5), (6000,0,10) },
        ["garden"]   = new[] { (150,50,0), (400,120,0), (1000,300,0), (2500,800,0), (6000,2000,0) },
    };

    /// <summary>获取从 currentLevel 升到下一级的消耗；已满级返回 false</summary>
    public static bool TryGetUpgradeCost(string id, int currentLevel, out (int gold, int fish, int badge) cost)
    {
        cost = default;
        if (currentLevel < 1 || currentLevel >= MAX_BUILDING_LEVEL) return false;
        if (!UPGRADE_COST.TryGetValue(id, out var arr)) return false;
        cost = arr[currentLevel - 1];
        return true;
    }

    /// <summary>建筑是否已满级（受 MAX_BUILDING_LEVEL 与住所等级双重限制）</summary>
    public static bool IsMaxLevel(string id, int currentLevel, int houseLevel)
        => currentLevel >= Mathf.Min(MAX_BUILDING_LEVEL, MaxLevelFor(houseLevel));

    // ----- 效果（显示用文案）-----
    public static string EffectValue(string id, int level)
    {
        int add = level - 1;
        switch (id)
        {
            case "foodbowl": return add <= 0 ? "0%" : $"+{add * 5}%";
            case "toy":      return add <= 0 ? "0%" : $"+{add * 10}%";
            case "medical":  return add <= 0 ? "0h" : $"+{add * 2}h";
            case "garden":   return add <= 0 ? "0"  : $"+{add * 2}/天";
            default:         return "";
        }
    }

    // ----- 效果（数值，供玩法计算）-----
    /// <summary>食盆：喂食/互动亲密度增益倍率（每级 +5%）</summary>
    public static float IntimacyBonusMul(string id, int level)
        => id == "foodbowl" ? 1f + (level - 1) * 0.05f : 1f;

    /// <summary>玩具：小鱼干产出速率加成（每级 +10%）</summary>
    public static float FishRateBonus(string id, int level)
        => id == "toy" ? (level - 1) * 0.10f : 0f;

    /// <summary>医疗：离线产出上限加成（小时，每级 +2h）</summary>
    public static int OfflineCapBonusHours(string id, int level)
        => id == "medical" ? (level - 1) * 2 : 0;

    /// <summary>花园：每日自然亲密度增长（每级 +2）</summary>
    public static int DailyIntimacyBonus(string id, int level)
        => id == "garden" ? (level - 1) * 2 : 0;

    // ============================================================
    // 宠物
    // ============================================================
    public struct PetInfo
    {
        public PetType type;
        public string icon;
        public string name;
        public int unlockLevel;   // 0 = 初始伙伴
    }

    public static readonly PetInfo[] PETS = new[]
    {
        new PetInfo { type = PetType.Cat,    icon = "🐱", name = "橘猫",   unlockLevel = 0 },
        new PetInfo { type = PetType.Dog,    icon = "🐶", name = "柴犬",   unlockLevel = 5 },
        new PetInfo { type = PetType.Hamster,icon = "🐹", name = "仓鼠",   unlockLevel = 10 },
        new PetInfo { type = PetType.Parrot, icon = "🦜", name = "鹦鹉",   unlockLevel = 15 },
        new PetInfo { type = PetType.Fish,   icon = "🐟", name = "金鱼",   unlockLevel = 20 },
        new PetInfo { type = PetType.Rabbit, icon = "🐰", name = "垂耳兔", unlockLevel = 25 },
    };

    public static readonly string[] STAGE_NAMES = { "", "脏兮兮", "干净", "幸福" };

    public static readonly string[] INTIMACY_LEVEL_NAMES = { "陌生", "认识", "熟悉", "亲近", "亲密", "挚友" };

    // 亲密度等级累计阈值（Lv1..Lv6），设计文档 §2.2
    private static readonly int[] INTIMACY_THRESHOLD = { 0, 50, 150, 300, 600, 1000 };

    /// <summary>根据累计亲密度计算等级（1-6）</summary>
    public static int IntimacyLevel(int intimacy)
    {
        int lv = 1;
        for (int i = INTIMACY_THRESHOLD.Length - 1; i >= 0; i--)
        {
            if (intimacy >= INTIMACY_THRESHOLD[i]) { lv = i + 1; break; }
        }
        return lv;
    }

    /// <summary>到下一级还差多少亲密度（满级返回 0）</summary>
    public static int IntimacyToNext(int intimacy)
    {
        int lv = IntimacyLevel(intimacy);
        if (lv >= 6) return 0;
        return INTIMACY_THRESHOLD[lv] - intimacy;
    }

    public static string IntimacyLevelName(int intimacy)
        => INTIMACY_LEVEL_NAMES[IntimacyLevel(intimacy) - 1];
}
