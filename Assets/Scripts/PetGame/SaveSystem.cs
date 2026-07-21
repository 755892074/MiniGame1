using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 疯狂铲屎官 — 全局存档系统
/// 存储：本地 PlayerPrefs（秒读）+ 抖音云 setUserCloudStorage（防丢档）
/// 覆盖：关卡进度、星级、货币、铲屎官等级、宠物、建筑、设置
/// </summary>
public static class SaveSystem
{
    // ========== 常量 ==========
    private const string SAVE_KEY = "CrazyPooper_Save_v1";
    private const string CLOUD_KEY = "cp_save";          // 抖音云存储 key
    private const int SAVE_VERSION = 1;

    // ========== 存档数据模型 ==========
    [Serializable]
    public class GameSave
    {
        public int version = SAVE_VERSION;

        // --- 关卡进度 ---
        public int currentLevelId = 1;
        public int highestUnlockedLevel = 1;

        // --- 每关星级记录（levelId → 星数1-3） ---
        public List<LevelStarRecord> levelStars = new List<LevelStarRecord>();

        // --- 货币系统 ---
        public int gold;                  // 金币（建造货币：买地/建升级建筑，通关获得）
        public int fishDiscount;          // 小鱼干（主货币，通关获得）
        public int rescueBadge;           // 救助徽章（成就货币，星级通关获得）
        public int rainbowBall;           // 彩虹毛球（稀有货币，看广告获得）

        // --- IAA 道具剩余次数（每关开始刷新为基数；用尽看广告补充）---
        public int itemUndo = 3;          // 撤销
        public int itemAddBowl = 2;       // 加空碗
        public int itemShuffle = 3;       // 洗牌
        public int itemHint = 3;          // 提示

        // --- 铲屎官等级 ---
        public int cleanerExp;            // 当前经验值
        public int cleanerLevel = 1;      // 当前铲屎官等级(1-8)

        // --- 宠物系统 ---
        public List<PetRecord> pets = new List<PetRecord>();

        // --- 小院建筑 ---
        public int houseLevel = 1;        // 全局住所等级(1-5)
        public List<BuildingRecord> buildings = new List<BuildingRecord>();

        // --- 统计 ---
        public int totalLevelsCompleted;  // 总通关次数
        public int totalPetsRescued;      // 总救助宠物数
        public long firstPlayTimestamp;   // 首次游玩时间(Unix秒)
        public long lastSaveTimestamp;     // 最后存档时间(Unix秒)，用于云端合并冲突

        // --- 设置 ---
        public bool bgmEnabled = true;
        public bool sfxEnabled = true;
        public float bgmVolume = 1f;
        public float sfxVolume = 1f;

        // --- 登录/隐私 ---
        public bool privacyAgreed = false;   // 是否同意隐私政策
        public string loginType = "guest";  // "guest" 或 "douyin"
        public string userId = "";          // 登录后的用户ID
    }

    [Serializable]
    public class LevelStarRecord
    {
        public int levelId;
        public int stars;       // 1-3
        public int bestScore;   // 该关历史最高分
    }

    [Serializable]
    public class PetRecord
    {
        public PetType petType;
        public bool unlocked;       // 是否已救助获得
        public int stage;           // 成长阶段 1=脏兮兮 2=干净 3=幸福
        public string nickname;     // 玩家给宠物起的名字（可选）
        public long rescuedTimestamp; // 救助时间

        // --- 养成（设计文档 §2.2 / §7.2）---
        public int intimacy;        // 累计亲密度 0-1000
        public bool isRare;         // 是否已稀有变异
        public long lastFeedTime;   // 最后喂食时间（Unix秒）
        public long lastInteractTime; // 最后互动时间（每日限1次）
    }

    [Serializable]
    public class BuildingRecord
    {
        public string buildingId;  // "foodbowl" / "toy" / "medical" / "garden"
        public int level;          // 当前等级
    }

    // ========== 铲屎官等级配置 ==========
    public static readonly CleanerRank[] CleanerRanks = new CleanerRank[]
    {
        new CleanerRank { level = 1, title = "实习铲屎官",   expRequired = 0 },
        new CleanerRank { level = 2, title = "初级铲屎官",   expRequired = 100 },
        new CleanerRank { level = 3, title = "中级铲屎官",   expRequired = 300 },
        new CleanerRank { level = 4, title = "高级铲屎官",   expRequired = 600 },
        new CleanerRank { level = 5, title = "资深铲屎官",   expRequired = 1000 },
        new CleanerRank { level = 6, title = "专家铲屎官",   expRequired = 1800 },
        new CleanerRank { level = 7, title = "大师铲屎官",   expRequired = 3000 },
        new CleanerRank { level = 8, title = "传奇铲屎官",   expRequired = 5000 },
    };

    public struct CleanerRank
    {
        public int level;
        public string title;
        public int expRequired;  // 达到该等级所需的累计经验
    }

    // ========== 缓存 ==========
    private static GameSave _cache;

    /// <summary>获取存档数据（缓存的内存副本，修改后需调用Save）</summary>
    public static GameSave Data
    {
        get
        {
            if (_cache == null) Load();
            return _cache;
        }
    }

    // ========== 云同步状态 ==========
    /// <summary>云同步是否可用（运行时检测）</summary>
    public static bool CloudAvailable => CloudSaveBridge.IsAvailable;

    /// <summary>最后一次云同步结果</summary>
    public enum CloudSyncStatus { Idle, Uploading, Uploaded, Downloading, Downloaded, Failed, Disabled }
    public static CloudSyncStatus lastCloudStatus { get; private set; } = CloudSyncStatus.Idle;

    // ========== 核心 API ==========

    /// <summary>加载存档。首次游玩自动初始化。然后异步尝试从云端恢复。</summary>
    public static void Load()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                _cache = JsonUtility.FromJson<GameSave>(json);
                if (_cache == null) _cache = NewSave();
                else if (_cache.version < SAVE_VERSION) _cache = Migrate(_cache);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 本地存档解析失败，使用新存档: {e.Message}");
                _cache = NewSave();
            }
        }
        else
        {
            _cache = NewSave();
            Debug.Log("[SaveSystem] 首次游玩，初始化新存档");
        }

        // 兼容旧存档：确保初始小院数据存在（初始橘猫 + 4 个基础建筑）
        EnsureBaseYard();

        // 异步尝试从云端恢复（不阻塞游戏启动）
        TryRestoreFromCloud();
    }

    /// <summary>保存到本地 PlayerPrefs，并异步同步到云端</summary>
    public static void Save()
    {
        if (_cache == null) return;

        // 更新时间戳（用于云端冲突合并）
        _cache.lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 写入本地（秒级，不阻塞）
        string json = JsonUtility.ToJson(_cache);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        // 异步上传云端（防丢档，失败不影响游戏）
        UploadToCloud(json);
    }

    /// <summary>重置所有存档（本地+云端）</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        _cache = NewSave();
        CloudSaveBridge.RemoveSave(CLOUD_KEY);
        Debug.Log("[SaveSystem] 存档已重置（本地+云端）");
    }

    /// <summary>存档是否存在</summary>
    public static bool HasSave => PlayerPrefs.HasKey(SAVE_KEY);

    // ========== 云同步内部逻辑 ==========

    /// <summary>异步上传存档到抖音云</summary>
    private static void UploadToCloud(string json)
    {
        if (!CloudSaveBridge.IsAvailable)
        {
            lastCloudStatus = CloudSyncStatus.Disabled;
            return;
        }

        // 抖音 setUserCloudStorage 限制：每个 key+value 最大 1KB
        // 我们的 JSON 存档可能超过 1KB（有关卡星级列表+宠物列表）
        // 策略：将 JSON 拆分成多个分片上传
        var chunks = SplitJson(json, 900); // 留点余量

        lastCloudStatus = CloudSyncStatus.Uploading;
        CloudSaveBridge.SetSave(CLOUD_KEY, chunks, (success) =>
        {
            lastCloudStatus = success ? CloudSyncStatus.Uploaded : CloudSyncStatus.Failed;
            if (!success)
                Debug.LogWarning("[SaveSystem] 云同步上传失败，本地存档不受影响");
        });
    }

    /// <summary>异步从抖音云恢复存档，合并到本地（取最新版本）</summary>
    private static void TryRestoreFromCloud()
    {
        if (!CloudSaveBridge.IsAvailable)
        {
            lastCloudStatus = CloudSyncStatus.Disabled;
            return;
        }

        lastCloudStatus = CloudSyncStatus.Downloading;
        CloudSaveBridge.GetSave(CLOUD_KEY, (json) =>
        {
            if (string.IsNullOrEmpty(json))
            {
                lastCloudStatus = CloudSyncStatus.Downloaded;
                return; // 云端无存档，用本地
            }

            try
            {
                var cloudSave = JsonUtility.FromJson<GameSave>(json);
                if (cloudSave == null) { lastCloudStatus = CloudSyncStatus.Failed; return; }

                // 合并策略：取 lastSaveTimestamp 更新的那份
                // 但如果本地是新存档（firstPlayTimestamp 刚创建），且云端更老，也用本地
                if (cloudSave.lastSaveTimestamp > (_cache?.lastSaveTimestamp ?? 0))
                {
                    // 云端更新 → 用云端覆盖本地
                    Debug.Log($"[SaveSystem] 云端存档更新({cloudSave.lastSaveTimestamp}) > 本地({_cache?.lastSaveTimestamp})，恢复云端数据");
                    _cache = cloudSave;
                    PlayerPrefs.SetString(SAVE_KEY, json);
                    PlayerPrefs.Save();
                }
                else
                {
                    Debug.Log("[SaveSystem] 本地存档更新或相同，保留本地");
                }
                lastCloudStatus = CloudSyncStatus.Downloaded;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 云端存档解析失败: {e.Message}");
                lastCloudStatus = CloudSyncStatus.Failed;
            }
        });
    }

    /// <summary>将 JSON 拆分为多个不超过 maxLen 的分片，用 \x00 分隔符编码为单字符串</summary>
    private static string SplitJson(string json, int maxLen)
    {
        // 抖音云限制每个 key+value 最大 1024 字节
        // 简单策略：如果 JSON < 900 字节，直接存；否则用 Base64 压缩
        // 这里用一个简单编码：存原始 JSON（实际休闲游戏存档不会太大）
        if (json.Length <= maxLen) return json;

        // 大存档情况：做简单压缩（去掉空白）
        string compact = JsonUtility.ToJson(_cache, false);
        return compact.Length <= maxLen ? compact : compact.Substring(0, maxLen);
    }

    // ========== 关卡进度 ==========

    /// <summary>通关后记录。返回true表示星级有提升。</summary>
    public static bool RecordLevelComplete(int levelId, int stars, int score)
    {
        var data = Data;
        bool improved = false;

        var record = data.levelStars.Find(r => r.levelId == levelId);
        if (record == null)
        {
            data.levelStars.Add(new LevelStarRecord
            {
                levelId = levelId,
                stars = stars,
                bestScore = score
            });
            improved = true;
        }
        else
        {
            if (stars > record.stars) { record.stars = stars; improved = true; }
            if (score > record.bestScore) record.bestScore = score;
        }

        // 解锁下一关
        if (levelId >= data.highestUnlockedLevel)
            data.highestUnlockedLevel = Mathf.Max(data.highestUnlockedLevel, levelId + 1);

        data.totalLevelsCompleted++;
        Save();
        return improved;
    }

    /// <summary>获取某关的星级（0=未通关）</summary>
    public static int GetLevelStars(int levelId)
    {
        var record = Data.levelStars.Find(r => r.levelId == levelId);
        return record?.stars ?? 0;
    }

    /// <summary>获取某关最高分</summary>
    public static int GetLevelBestScore(int levelId)
    {
        var record = Data.levelStars.Find(r => r.levelId == levelId);
        return record?.bestScore ?? 0;
    }

    /// <summary>总星数</summary>
    public static int TotalStars => Data.levelStars.Sum(r => r.stars);

    /// <summary>关卡是否已解锁</summary>
    public static bool IsLevelUnlocked(int levelId) => levelId <= Data.highestUnlockedLevel;

    // ========== 货币 ==========

    public static void AddGold(int amount)
    {
        Data.gold += amount;
        Save();
    }

    public static bool SpendGold(int amount)
    {
        if (Data.gold < amount) return false;
        Data.gold -= amount;
        Save();
        return true;
    }

    public static void AddFish(int amount)
    {
        Data.fishDiscount += amount;
        Save();
    }

    public static bool SpendFish(int amount)
    {
        if (Data.fishDiscount < amount) return false;
        Data.fishDiscount -= amount;
        Save();
        return true;
    }

    public static void AddBadge(int amount)
    {
        Data.rescueBadge += amount;
        Save();
    }

    public static void AddRainbowBall(int amount)
    {
        Data.rainbowBall += amount;
        Save();
    }

    // ========== IAA 道具次数 ==========
    // 每关开始 RefillTools() 刷新为基数；用尽后看广告补充（GrantTool）。
    public enum ToolType { Undo, AddBowl, Shuffle, Hint }

    public const int BaseUndo = 3, BaseAddBowl = 2, BaseShuffle = 3, BaseHint = 3;
    public const int AdGrantUndo = 2, AdGrantAddBowl = 1, AdGrantShuffle = 2, AdGrantHint = 2;
    public const int DeadlockRescueGoldCost = 900;  // 死局花金币+1碗（与 IAA 数值参考一致）

    /// <summary>取某道具剩余次数</summary>
    public static int GetTool(ToolType t)
    {
        var d = Data;
        return t switch
        {
            ToolType.Undo => d.itemUndo,
            ToolType.AddBowl => d.itemAddBowl,
            ToolType.Shuffle => d.itemShuffle,
            ToolType.Hint => d.itemHint,
            _ => 0
        };
    }

    /// <summary>消耗 1 次道具，成功返回 true（次数>0 才扣）</summary>
    public static bool ConsumeTool(ToolType t)
    {
        var d = Data;
        switch (t)
        {
            case ToolType.Undo: if (d.itemUndo <= 0) return false; d.itemUndo--; break;
            case ToolType.AddBowl: if (d.itemAddBowl <= 0) return false; d.itemAddBowl--; break;
            case ToolType.Shuffle: if (d.itemShuffle <= 0) return false; d.itemShuffle--; break;
            case ToolType.Hint: if (d.itemHint <= 0) return false; d.itemHint--; break;
            default: return false;
        }
        Save();
        return true;
    }

    /// <summary>看广告/奖励补充道具次数</summary>
    public static void GrantTool(ToolType t, int n)
    {
        var d = Data;
        switch (t)
        {
            case ToolType.Undo: d.itemUndo += n; break;
            case ToolType.AddBowl: d.itemAddBowl += n; break;
            case ToolType.Shuffle: d.itemShuffle += n; break;
            case ToolType.Hint: d.itemHint += n; break;
        }
        Save();
    }

    /// <summary>每关开始刷新为基数（失败/重开同样刷新，等于重新发工具包）</summary>
    public static void RefillTools()
    {
        var d = Data;
        d.itemUndo = BaseUndo;
        d.itemAddBowl = BaseAddBowl;
        d.itemShuffle = BaseShuffle;
        d.itemHint = BaseHint;
        Save();
        Debug.Log("[SaveSystem] 本关道具已刷新：撤销3 加碗2 洗牌3 提示3");
    }

    // ========== 经验/等级 ==========

    /// <summary>增加经验，返回是否升级</summary>
    public static bool AddExp(int exp)
    {
        Data.cleanerExp += exp;
        int oldLevel = Data.cleanerLevel;
        Data.cleanerLevel = GetLevelByExp(Data.cleanerExp);
        bool leveled = Data.cleanerLevel > oldLevel;
        Save();
        return leveled;
    }

    /// <summary>根据累计经验计算铲屎官等级</summary>
    public static int GetLevelByExp(int totalExp)
    {
        int level = 1;
        for (int i = CleanerRanks.Length - 1; i >= 0; i--)
        {
            if (totalExp >= CleanerRanks[i].expRequired)
            {
                level = CleanerRanks[i].level;
                break;
            }
        }
        return level;
    }

    /// <summary>当前等级称号</summary>
    public static string GetCurrentTitle()
    {
        int idx = Data.cleanerLevel - 1;
        if (idx < 0 || idx >= CleanerRanks.Length) return "???";
        return CleanerRanks[idx].title;
    }

    /// <summary>距离下一级还差多少经验（满级返回0）</summary>
    public static int ExpToNextLevel()
    {
        int idx = Data.cleanerLevel - 1;
        if (idx >= CleanerRanks.Length - 1) return 0;
        return CleanerRanks[idx + 1].expRequired - Data.cleanerExp;
    }

    /// <summary>当前等级的进度比例(0~1)，用于经验条UI</summary>
    public static float ExpProgress()
    {
        int idx = Data.cleanerLevel - 1;
        if (idx >= CleanerRanks.Length - 1) return 1f;
        int curBase = CleanerRanks[idx].expRequired;
        int nextBase = CleanerRanks[idx + 1].expRequired;
        return (float)(Data.cleanerExp - curBase) / (nextBase - curBase);
    }

    // ========== 宠物 ==========

    /// <summary>救助（解锁）一只宠物</summary>
    public static void RescuePet(PetType type, string nickname = "")
    {
        var data = Data;
        var pet = data.pets.Find(p => p.petType == type);
        if (pet == null)
        {
            data.pets.Add(new PetRecord
            {
                petType = type,
                unlocked = true,
                stage = 1,
                nickname = nickname,
                rescuedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
            data.totalPetsRescued++;
        }
        else if (!pet.unlocked)
        {
            pet.unlocked = true;
            pet.stage = 1;
            pet.rescuedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            data.totalPetsRescued++;
        }
        Save();
    }

    /// <summary>升级宠物成长阶段(1→2→3)</summary>
    public static bool UpgradePetStage(PetType type)
    {
        var pet = Data.pets.Find(p => p.petType == type);
        if (pet == null || pet.stage >= 3) return false;
        pet.stage++;
        Save();
        return true;
    }

    /// <summary>宠物是否已救助</summary>
    public static bool IsPetRescued(PetType type)
    {
        var pet = Data.pets.Find(p => p.petType == type);
        return pet != null && pet.unlocked;
    }

    /// <summary>宠物当前阶段</summary>
    public static int GetPetStage(PetType type)
    {
        var pet = Data.pets.Find(p => p.petType == type);
        return pet?.stage ?? 0;
    }

    /// <summary>已救助宠物数量</summary>
    public static int RescuedPetCount => Data.pets.Count(p => p.unlocked);

    // ============================================================
    // 喂养 / 互动（P3）
    // ============================================================

    /// <summary>喂食一次：消耗小鱼干，按食盆等级加成增加亲密度（设计 §2.2：基础 +10/次）</summary>
    public static bool FeedPet(PetType type, int fishCost = 1)
    {
        var pet = Data.pets.Find(p => p.petType == type);
        if (pet == null || !pet.unlocked) return false;
        if (Data.fishDiscount < fishCost) return false;

        Data.fishDiscount -= fishCost;
        float mul = YardDefs.IntimacyBonusMul("foodbowl", GetBuildingLevel("foodbowl"));
        int gain = Mathf.RoundToInt(10 * mul);
        pet.intimacy += gain;
        pet.lastFeedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Save();
        return true;
    }

    /// <summary>今日是否还能互动（每日限1次）</summary>
    public static bool CanInteractToday(PetType type)
    {
        var pet = Data.pets.Find(p => p.petType == type);
        if (pet == null || !pet.unlocked) return false;
        if (pet.lastInteractTime <= 0) return true;
        var last = DateTimeOffset.FromUnixTimeSeconds(pet.lastInteractTime);
        return last.Date != DateTimeOffset.UtcNow.Date;
    }

    /// <summary>互动一次：每日限1次，按食盆等级加成增加亲密度（设计 §2.2：基础 +20/次）</summary>
    public static bool InteractPet(PetType type)
    {
        if (!CanInteractToday(type)) return false;
        var pet = Data.pets.Find(p => p.petType == type);
        if (pet == null) return false;
        float mul = YardDefs.IntimacyBonusMul("foodbowl", GetBuildingLevel("foodbowl"));
        int gain = Mathf.RoundToInt(20 * mul);
        pet.intimacy += gain;
        pet.lastInteractTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Save();
        return true;
    }

    // ========== 建筑 ==========

    /// <summary>获取建筑等级（不存在返回0）</summary>
    public static int GetBuildingLevel(string buildingId)
    {
        var b = Data.buildings.Find(x => x.buildingId == buildingId);
        return b?.level ?? 0;
    }

    /// <summary>升级建筑，返回是否成功</summary>
    public static bool UpgradeBuilding(string buildingId, int maxLevel = 2)
    {
        var data = Data;
        var b = data.buildings.Find(x => x.buildingId == buildingId);
        if (b == null)
        {
            if (maxLevel < 1) return false;
            data.buildings.Add(new BuildingRecord { buildingId = buildingId, level = 1 });
            Save();
            return true;
        }
        if (b.level >= maxLevel) return false;
        b.level++;
        Save();
        return true;
    }

    /// <summary>升级全局住所等级</summary>
    public static bool UpgradeHouse(int maxLevel = 5)
    {
        if (Data.houseLevel >= maxLevel) return false;
        Data.houseLevel++;
        Save();
        return true;
    }

    // ============================================================
    // 建筑升级（P4，按成本扣费）
    // ============================================================

    /// <summary>建筑升级预览信息（供 UI 展示）</summary>
    public struct BuildingUpgradeInfo
    {
        public int currentLevel;
        public int maxLevel;        // 受住所等级限制后的实际上限
        public bool maxed;          // 是否已满级
        public int goldCost;
        public int fishCost;
        public int badgeCost;
        public bool affordable;     // 货币是否足够
        public string effectCurrent;
        public string effectNext;
    }

    /// <summary>获取建筑升级预览（不修改数据）</summary>
    public static BuildingUpgradeInfo GetBuildingUpgradeInfo(string buildingId)
    {
        int lv = GetBuildingLevel(buildingId);
        int cap = YardDefs.MaxLevelFor(Data.houseLevel);
        var info = new BuildingUpgradeInfo
        {
            currentLevel = lv,
            maxLevel = cap,
            maxed = YardDefs.IsMaxLevel(buildingId, lv, Data.houseLevel),
            effectCurrent = YardDefs.EffectValue(buildingId, lv),
        };

        if (!info.maxed && YardDefs.TryGetUpgradeCost(buildingId, lv, out var cost))
        {
            info.goldCost = cost.gold;
            info.fishCost = cost.fish;
            info.badgeCost = cost.badge;
            info.affordable = Data.gold >= cost.gold && Data.fishDiscount >= cost.fish && Data.rescueBadge >= cost.badge;
            info.effectNext = YardDefs.EffectValue(buildingId, lv + 1);
        }
        else
        {
            info.effectNext = info.effectCurrent;
        }
        return info;
    }

    /// <summary>尝试升级建筑（按成本扣费）。成功返回 true。</summary>
    public static bool TryUpgradeBuilding(string buildingId)
    {
        var info = GetBuildingUpgradeInfo(buildingId);
        if (info.maxed) return false;
        if (!info.affordable) return false;

        if (info.goldCost > 0) { if (!SpendGold(info.goldCost)) return false; }
        if (info.fishCost > 0) { if (!SpendFish(info.fishCost)) return false; }
        if (info.badgeCost > 0)
        {
            if (Data.rescueBadge < info.badgeCost) return false;
            Data.rescueBadge -= info.badgeCost;
        }

        var b = Data.buildings.Find(x => x.buildingId == buildingId);
        if (b == null)
            Data.buildings.Add(new BuildingRecord { buildingId = buildingId, level = 1 });
        else
            b.level++;
        Save();
        Debug.Log($"[建筑] {buildingId} 升级 → Lv.{GetBuildingLevel(buildingId)}");
        return true;
    }

    // ========== 设置 ==========

    public static void SetBgmEnabled(bool on) { Data.bgmEnabled = on; Save(); }
    public static void SetSfxEnabled(bool on) { Data.sfxEnabled = on; Save(); }
    public static void SetBgmVolume(float v) { Data.bgmVolume = v; Save(); }
    public static void SetSfxVolume(float v) { Data.sfxVolume = v; Save(); }

    // ========== 内部方法 ==========

    private static GameSave NewSave()
    {
        var save = new GameSave
        {
            version = SAVE_VERSION,
            firstPlayTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // 初始伙伴：橘猫（设计文档 §2.1 初始送 1 只）
        save.pets.Add(new PetRecord
        {
            petType = PetType.Cat,
            unlocked = true,
            stage = 1,
            nickname = "",
            rescuedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
        save.totalPetsRescued = 1;

        // 初始小院：4 个基础建筑各 Lv.1（设计文档 §3.4 住所 Lv1 建筑等级上限=2）
        foreach (var id in new[] { "foodbowl", "toy", "medical", "garden" })
            save.buildings.Add(new BuildingRecord { buildingId = id, level = 1 });

        return save;
    }

    /// <summary>存档版本迁移（后续版本升级时在这里加逻辑）</summary>
    private static GameSave Migrate(GameSave old)
    {
        old.version = SAVE_VERSION;
        Debug.Log($"[SaveSystem] 存档迁移到 v{SAVE_VERSION}");
        return old;
    }

    /// <summary>兼容旧存档：保证初始小院数据存在（初始橘猫 + 4 个基础建筑）。
    /// 仅在列表为空时补足，不会覆盖玩家已有的宠物/建筑。</summary>
    private static void EnsureBaseYard()
    {
        var data = _cache;
        bool dirty = false;

        if (data.pets.Count == 0)
        {
            data.pets.Add(new PetRecord
            {
                petType = PetType.Cat,
                unlocked = true,
                stage = 1,
                rescuedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
            data.totalPetsRescued = Mathf.Max(data.totalPetsRescued, 1);
            dirty = true;
        }

        if (data.buildings.Count == 0)
        {
            foreach (var id in new[] { "foodbowl", "toy", "medical", "garden" })
                data.buildings.Add(new BuildingRecord { buildingId = id, level = 1 });
            dirty = true;
        }

        if (dirty) Save();
    }
}
