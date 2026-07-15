using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 疯狂铲屎官 — 全局存档系统
/// 数据：JSON 序列化 → PlayerPrefs 存储（微信小游戏底层映射到 wx.setStorageSync）
/// 覆盖：关卡进度、星级、货币、铲屎官等级、宠物、建筑、设置
/// </summary>
public static class SaveSystem
{
    // ========== 常量 ==========
    private const string SAVE_KEY = "CrazyPooper_Save_v1";
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
        public int fishDiscount;          // 小鱼干（主货币，通关获得）
        public int rescueBadge;           // 救助徽章（成就货币，星级通关获得）
        public int rainbowBall;           // 彩虹毛球（稀有货币，看广告获得）

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

        // --- 设置 ---
        public bool bgmEnabled = true;
        public bool sfxEnabled = true;
        public float bgmVolume = 1f;
        public float sfxVolume = 1f;
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

    // ========== 核心 API ==========

    /// <summary>加载存档。首次游玩自动初始化。</summary>
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
                Debug.LogError($"[SaveSystem] 存档解析失败，使用新存档: {e.Message}");
                _cache = NewSave();
            }
        }
        else
        {
            _cache = NewSave();
            Debug.Log("[SaveSystem] 首次游玩，初始化新存档");
        }
    }

    /// <summary>保存到 PlayerPrefs</summary>
    public static void Save()
    {
        if (_cache == null) return;
        string json = JsonUtility.ToJson(_cache);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    /// <summary>重置所有存档</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        _cache = NewSave();
        Debug.Log("[SaveSystem] 存档已重置");
    }

    /// <summary>存档是否存在</summary>
    public static bool HasSave => PlayerPrefs.HasKey(SAVE_KEY);

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

    // ========== 设置 ==========

    public static void SetBgmEnabled(bool on) { Data.bgmEnabled = on; Save(); }
    public static void SetSfxEnabled(bool on) { Data.sfxEnabled = on; Save(); }
    public static void SetBgmVolume(float v) { Data.bgmVolume = v; Save(); }
    public static void SetSfxVolume(float v) { Data.sfxVolume = v; Save(); }

    // ========== 内部方法 ==========

    private static GameSave NewSave()
    {
        return new GameSave
        {
            version = SAVE_VERSION,
            firstPlayTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    /// <summary>存档版本迁移（后续版本升级时在这里加逻辑）</summary>
    private static GameSave Migrate(GameSave old)
    {
        // v0 → v1 示例：如果未来字段变更，在这里做迁移
        old.version = SAVE_VERSION;
        Debug.Log($"[SaveSystem] 存档迁移到 v{SAVE_VERSION}");
        return old;
    }
}
