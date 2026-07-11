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
    public int selectedBowlId = -1;
    public float elapsedTime;
    public bool isPlaying;

    public PourSystem pour { get; private set; } = new PourSystem();
    public int targetScore => currentLevel?.targetScore ?? 200;

    private PetLevelConfigV2 currentLevel;

    #region 事件
    public UnityEvent<int> onScoreChanged = new UnityEvent<int>();
    public UnityEvent<PetType, int, bool> onPetFed = new UnityEvent<PetType, int, bool>();
    public UnityEvent onMistake = new UnityEvent();
    public UnityEvent<int> onLevelComplete = new UnityEvent<int>();
    public UnityEvent onLevelFail = new UnityEvent();
    public UnityEvent<FoodType> onPickUp = new UnityEvent<FoodType>();
    public UnityEvent<PourResult> onPour = new UnityEvent<PourResult>();
    public UnityEvent onBowlCompleted = new UnityEvent();
    public UnityEvent onHeldChanged = new UnityEvent();
    public UnityEvent onSelectionChanged = new UnityEvent();
    public UnityEvent<int, int> onPourAnim = new UnityEvent<int, int>(); // fromBowlId, toBowlId
    public UnityEvent<int, PetType> onFeedAnim = new UnityEvent<int, PetType>(); // bowlId, petType
    #endregion

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        LoadConfig();
        AutoStartLevel();
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
            }
        }
    }

    void AutoStartLevel()
    {
        if (levels.Count == 0)
            GenerateTestLevel();
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
        selectedBowlId = -1;
        elapsedTime = 0;
        isPlaying = true;
    }

    /// <summary>
    /// 点击碗：选中→取消选中→倒入
    /// </summary>
    public void OnBowlClicked(int bowlId)
    {
        if (!isPlaying) return;

        if (selectedBowlId == -1)
        {
            // 选中
            selectedBowlId = bowlId;
            onSelectionChanged.Invoke();
        }
        else if (selectedBowlId == bowlId)
        {
            // 取消选中
            selectedBowlId = -1;
            onSelectionChanged.Invoke();
        }
        else
        {
            // 倒入：selected → bowlId
            int fromId = selectedBowlId;
            selectedBowlId = -1;
            onSelectionChanged.Invoke();

            var err = pour.PickUp(fromId);
            if (err != null) { onMistake.Invoke(); return; }
            onPickUp.Invoke(pour.heldFood!.Value);

            var result = pour.PourInto(bowlId);
            onPour.Invoke(result);
            if (!result.success) { onMistake.Invoke(); return; }
            onHeldChanged.Invoke();

            // 通知 UI 播倒入动画
            onPourAnim.Invoke(fromId, bowlId);

            if (result.bowlCompleted)
            {
                onBowlCompleted.Invoke();
                var (points, fedPet, isFirst) = pour.OnBowlComplete(bowlId);
                onScoreChanged.Invoke(pour.score);
                onPetFed.Invoke(fedPet, points, isFirst);
                // 通知 UI 播喂食动画
                onFeedAnim.Invoke(bowlId, fedPet);
                CheckWin();
            }
        }
    }

    public void Undo()
    {
        if (pour.Undo())
        {
            selectedBowlId = -1;
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
        var lv = ScriptableObject.CreateInstance<PetLevelConfigV2>();
        lv.levelId = 1;
        lv.levelName = "鱼+骨头·初体验";
        lv.bowlCapacity = 3;
        lv.targetScore = 150;
        lv.petQueue = new[] { PetType.Cat, PetType.Dog };
        lv.bowlInits = new[]
        {
            new BowlInitData { gridPos = new Vector2Int(0, 0), foodStack = new[] { FoodType.DriedFish, FoodType.DriedFish } },
            new BowlInitData { gridPos = new Vector2Int(1, 0), foodStack = new[] { FoodType.DriedFish } },
            new BowlInitData { gridPos = new Vector2Int(2, 0), foodStack = new[] { FoodType.BoneTreat, FoodType.BoneTreat } },
            new BowlInitData { gridPos = new Vector2Int(0, 1), foodStack = new[] { FoodType.BoneTreat } },
            new BowlInitData { gridPos = new Vector2Int(1, 1), foodStack = new FoodType[] { } },
        };
        levels.Add(lv);
    }

    public PetLevelConfigV2 GetCurrentLevel() => currentLevel;
    public FoodType? GetHeldFood() => pour.heldFood;
    public int GetScore() => pour.score;
    public List<Bowl> GetBowls() => pour.bowls;
    public PetType? GetCurrentPet() => pour.CurrentPet;
    public List<PetType> GetPetQueue() => pour.originalPets;
    public List<PetType> GetFedPets() => pour.fedPets;
}
