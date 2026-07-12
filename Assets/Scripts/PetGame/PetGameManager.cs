using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 铲屎官疯了 v2 — 脑洞倒水游戏管理器
/// </summary>
public class PetGameManager : MonoBehaviour
{
    public static PetGameManager Instance { get; private set; }

    [Header("配置")]
    public PetGameSpriteConfig spriteConfig;
    public List<PetLevelConfigV2> levels = new List<PetLevelConfigV2>();

    [Header("状态")]
    public int currentLevelId = 1;
    public int selectedBowlId = -1;        // 当前选中的碗（拿出来源的碗）
    public float elapsedTime;
    public bool isPlaying;

    public PourSystem pour { get; private set; } = new PourSystem();
    public int targetScore => currentLevel?.targetScore ?? 200;

    private PetLevelConfigV2 currentLevel;
    private float levelTimer;

    #region 事件
    public UnityEvent<int> onScoreChanged = new UnityEvent<int>();
    public UnityEvent<PetType, int, bool> onPetFed = new UnityEvent<PetType, int, bool>(); // pet, points, isFirst
    public UnityEvent onMistake = new UnityEvent();
    public UnityEvent<int> onLevelComplete = new UnityEvent<int>();
    public UnityEvent onLevelFail = new UnityEvent();
    public UnityEvent<FoodType> onPickUp = new UnityEvent<FoodType>();
    public UnityEvent<PourResult> onPour = new UnityEvent<PourResult>();
    public UnityEvent onBowlCompleted = new UnityEvent();
    public UnityEvent onHeldChanged = new UnityEvent();
    #endregion

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        LoadConfig();
        AutoStartLevel();
        Debug.Log($"[PetGameManager] Awake: 关卡数={levels.Count}, config={(spriteConfig != null ? "OK" : "NULL")}");
    }

    void Update()
    {
        if (!isPlaying) return;
        elapsedTime += Time.deltaTime;
        if (currentLevel != null && currentLevel.maxMoves > 0 && pour.totalMoves >= currentLevel.maxMoves)
        {
            if (pour.score < targetScore) FailLevel("步数用尽");
        }
    }

    void LoadConfig()
    {
        if (spriteConfig == null)
            spriteConfig = Resources.Load<PetGameSpriteConfig>("PetGameSpriteConfig");

        if (levels.Count == 0)
        {
            var loaded = Resources.LoadAll<PetLevelConfigV2>("PetLevels");
            if (loaded != null && loaded.Length > 0)
            {
                levels = new List<PetLevelConfigV2>(loaded);
                levels.Sort((a, b) => a.levelId.CompareTo(b.levelId));
                Debug.Log($"[PetGameManager] 从 Resources 加载 {levels.Count} 关");
            }
        }
    }

    void AutoStartLevel()
    {
        if (levels.Count == 0)
        {
            Debug.LogWarning("[PetGameManager] 无关卡数据，生成测试关卡");
            GenerateTestLevel();
        }
        StartLevel(currentLevelId);
    }

    public void StartLevel(int id)
    {
        currentLevel = levels.Find(l => l.levelId == id);
        if (currentLevel == null)
        {
            Debug.LogError($"[PetGameManager] 找不到关卡{id}");
            return;
        }

        var bowls = new List<Bowl>();
        for (int i = 0; i < currentLevel.bowlInits.Length; i++)
        {
            var bi = currentLevel.bowlInits[i];
            bowls.Add(new Bowl
            {
                bowlId = i,
                capacity = currentLevel.bowlCapacity,
                foods = new List<FoodType>(bi.foodStack),
                gridPos = bi.gridPos,
            });
        }

        pour.InitLevel(bowls, new List<PetType>(currentLevel.petQueue));
        elapsedTime = 0;
        isPlaying = true;
        Debug.Log($"[PetGameManager] StartLevel: 第{id}关 '{currentLevel.levelName}', {bowls.Count}碗, {currentLevel.petQueue.Length}宠物, 目标{targetScore}分");
    }

    public void PickUpFromBowl(int bowlId)
    {
        if (!isPlaying) { Debug.LogWarning("[PetGameManager] PickUp: 游戏未开始"); return; }
        var err = pour.PickUp(bowlId);
        if (err != null) { onMistake.Invoke(); Debug.LogWarning($"[PetGameManager] PickUp 失败: {err}"); return; }
        selectedBowlId = bowlId; // 记录来源碗
        onPickUp.Invoke(pour.heldFood!.Value);
        onHeldChanged.Invoke();
    }

    public void PourToBowl(int bowlId)
    {
        if (!isPlaying) { Debug.LogWarning("[PetGameManager] PourTo: 游戏未开始"); return; }
        if (pour.heldFood == null) { PickUpFromBowl(bowlId); return; }

        var result = pour.PourInto(bowlId);
        onPour.Invoke(result);

        if (!result.success) { onMistake.Invoke(); Debug.LogWarning($"[PetGameManager] Pour 失败: {result.reason}"); return; }

        selectedBowlId = -1; // 清除选中
        onHeldChanged.Invoke();

        // 碗满了？
        if (result.bowlCompleted)
        {
            onBowlCompleted.Invoke();
            var (points, fedPet, isFirst) = pour.OnBowlComplete(bowlId);
            onScoreChanged.Invoke(pour.score);
            onPetFed.Invoke(fedPet, points, isFirst);

            if (!isFirst)
            {
                Debug.Log($"[PetGameManager] 其他宠物抱怨不公平！（匹配了{petQueueToString(fedPet)}而非队首{currentPetQueueStr()}）");
            }

            CheckWin();
        }
    }

    public void Undo()
    {
        if (pour.Undo())
        {
            onScoreChanged.Invoke(pour.score);
            onHeldChanged.Invoke();
        }
    }

    public void AddBowl()
    {
        var bowl = pour.AddEmptyBowl();
        Debug.Log($"[PetGameManager] IAA: 新增碗{bowl.bowlId}");
    }

    public void ShuffleBowl(int bowlId)
    {
        pour.ShuffleBowl(bowlId);
    }

    void CheckWin()
    {
        if (pour.score >= targetScore || pour.IsComplete)
        {
            isPlaying = false;
            int stars = pour.score >= targetScore * 2 ? 3 : pour.score >= targetScore * 1.5f ? 2 : 1;
            Debug.Log($"[PetGameManager] 通关! 得分{pour.score}/{targetScore}, ★{stars}");
            onLevelComplete.Invoke(stars);
        }
    }

    void FailLevel(string reason)
    {
        isPlaying = false;
        Debug.Log($"[PetGameManager] 失败: {reason}");
        onLevelFail.Invoke();
    }

    void GenerateTestLevel()
    {
        // 简单测试关：2个宠物（猫+狗），5个碗，容量3
        var lv = ScriptableObject.CreateInstance<PetLevelConfigV2>();
        lv.levelId = 1;
        lv.levelName = "鱼+骨头·初体验";
        lv.bowlCapacity = 3;
        lv.targetScore = 150;
        lv.petQueue = new[] { PetType.Cat, PetType.Dog }; // 猫在前，狗在后
        lv.bowlInits = new[]
        {
            new BowlInitData { gridPos = new Vector2Int(0, 0), foodStack = new[] { FoodType.DriedFish, FoodType.DriedFish } },
            new BowlInitData { gridPos = new Vector2Int(1, 0), foodStack = new[] { FoodType.DriedFish } },
            new BowlInitData { gridPos = new Vector2Int(2, 0), foodStack = new[] { FoodType.BoneTreat, FoodType.BoneTreat } },
            new BowlInitData { gridPos = new Vector2Int(0, 1), foodStack = new[] { FoodType.BoneTreat } },
            new BowlInitData { gridPos = new Vector2Int(1, 1), foodStack = new FoodType[] { } },
        };
        levels.Add(lv);
        Debug.Log("[PetGameManager] 生成测试关卡（5碗/2宠物）");
    }

    public PetLevelConfigV2 GetCurrentLevel() => currentLevel;
    public FoodType? GetHeldFood() => pour.heldFood;
    public int GetScore() => pour.score;
    public List<Bowl> GetBowls() => pour.bowls;
    public PetType? GetCurrentPet() => pour.CurrentPet;
    public List<PetType> GetPetQueue() => pour.originalPets;
    public List<PetType> GetFedPets() => pour.fedPets;

    string currentPetQueueStr() => pour.CurrentPet?.ToString() ?? "无";
    string petQueueToString(PetType p) => p.ToString();
}
