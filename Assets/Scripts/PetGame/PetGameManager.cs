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
    public UnityEvent<int, int> onPourAnim = new UnityEvent<int, int>();     // fromBowlId, toBowlId
    public UnityEvent<int, PetType> onFeedAnim = new UnityEvent<int, PetType>(); // bowlId, petType
    public bool isAnimating;
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
        // 不自动开始，等待玩家选关
        isPlaying = false;
        levels.Sort((a, b) => a.levelId.CompareTo(b.levelId));
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
        if (!isPlaying || isAnimating) return;

        var bowl = pour.GetBowl(bowlId);
        if (bowl != null && bowl.isCompleted) return; // 已完成碗不能操作

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
            // 批量倒入：selected → bowlId，只倒能装下的数量
            int fromId = selectedBowlId;
            selectedBowlId = -1;
            onSelectionChanged.Invoke();

            int count = pour.PickUpAll(fromId);
            if (count == 0) { onMistake.Invoke(); return; }

            // 目标碗还能装几个
            var target = pour.GetBowl(bowlId);
            int canFit = target != null ? target.capacity - target.foods.Count : 0;
            if (canFit <= 0)
            {
                // 装不下，全部还回源碗
                var held = pour.heldFood;
                var src = pour.GetBowl(fromId);
                for (int i = 0; i < count && held.HasValue && src != null; i++)
                    src.Push(held.Value);
                pour.heldFood = null;
                onPour.Invoke(new PourResult());
                onMistake.Invoke(); return;
            }

            int extra = count - canFit;
            if (extra > 0)
            {
                // 拿多了，多出来的还回去
                var held = pour.heldFood!.Value;
                var src = pour.GetBowl(fromId);
                for (int i = 0; i < extra && src != null; i++)
                    src.Push(held);
                count = canFit;
            }

            var result = pour.PourInto(bowlId, count);
            if (!result.success)
            {
                // 类型不匹配，全部还回源碗
                var held = pour.heldFood;
                var src2 = pour.GetBowl(fromId);
                for (int i = 0; i < count && held.HasValue && src2 != null; i++)
                    src2.Push(held.Value);
                pour.heldFood = null;
                onPour.Invoke(result);
                onMistake.Invoke(); return;
            }

            onPour.Invoke(result);

            // 记录历史
            pour.SaveHistory(fromId, bowlId, count);

            if (result.bowlCompleted)
            {
                // 先触发碗完成 + 宠物喂食逻辑
                onBowlCompleted.Invoke();
                var (points, fedPet, isFirst) = pour.OnBowlComplete(bowlId);
                onScoreChanged.Invoke(pour.score);
                onPetFed.Invoke(fedPet, points, isFirst);
                // 通知 UI 播喂食动画（动画结束由 UI 重建界面）
                onFeedAnim.Invoke(bowlId, fedPet);
            }
            else
            {
                // 通知 UI 播倒入动画
                onPourAnim.Invoke(fromId, bowlId);
            }
            CheckWin();
    }

    public void Undo()
    {
        if (pour.Undo())
        {
            selectedBowlId = -1;
            onPour.Invoke(new PourResult());   // 刷新UI
            onScoreChanged.Invoke(pour.score);
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
        GenerateLevel(1, "鱼+骨头·初体验", 3, 150, new[] { PetType.Cat, PetType.Dog }, new[] { FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat });
        GenerateLevel(2, "加入仓鼠", 3, 200, new[] { PetType.Cat, PetType.Dog, PetType.Hamster }, new[] { FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.SunflowerSeed });
        GenerateLevel(3, "鹦鹉来了", 3, 200, new[] { PetType.Dog, PetType.Parrot, PetType.Cat }, new[] { FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.Millet, FoodType.Millet, FoodType.Millet, FoodType.DriedFish, FoodType.DriedFish, FoodType.CatKibble });
        GenerateLevel(4, "金鱼池", 4, 250, new[] { PetType.Fish, PetType.Cat, PetType.Hamster }, new[] { FoodType.FishFlake, FoodType.FishFlake, FoodType.FishFlake, FoodType.FishFlake, FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.SunflowerSeed });
        GenerateLevel(5, "兔兔跳", 3, 250, new[] { PetType.Rabbit, PetType.Parrot, PetType.Dog }, new[] { FoodType.Carrot, FoodType.Carrot, FoodType.Carrot, FoodType.Millet, FoodType.Millet, FoodType.Millet, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat });
        GenerateLevel(6, "猫狗大战", 4, 300, new[] { PetType.Cat, PetType.Cat, PetType.Dog, PetType.Dog }, new[] { FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.CatKibble, FoodType.CatKibble, FoodType.CatKibble, FoodType.CatKibble, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.DogKibble, FoodType.DogKibble, FoodType.DogKibble, FoodType.DogKibble });
        GenerateLevel(7, "全员出动", 3, 350, new[] { PetType.Hamster, PetType.Fish, PetType.Rabbit, PetType.Parrot }, new[] { FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.FishFlake, FoodType.FishFlake, FoodType.FishFlake, FoodType.Carrot, FoodType.Carrot, FoodType.Carrot, FoodType.Millet, FoodType.Millet, FoodType.Millet });
        GenerateLevel(8, "猫咪天堂", 4, 350, new[] { PetType.Cat, PetType.Cat, PetType.Cat }, new[] { FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.CatKibble, FoodType.CatKibble, FoodType.CatKibble, FoodType.CatKibble, FoodType.CatTreatStick, FoodType.CatTreatStick, FoodType.CatTreatStick, FoodType.CatTreatStick });
        GenerateLevel(9, "汪汪乐园", 4, 400, new[] { PetType.Dog, PetType.Dog, PetType.Dog }, new[] { FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.DogKibble, FoodType.DogKibble, FoodType.DogKibble, FoodType.DogKibble, FoodType.MeatJerky, FoodType.MeatJerky, FoodType.MeatJerky, FoodType.MeatJerky });
        GenerateLevel(10, "终极挑战", 5, 500, new[] { PetType.Cat, PetType.Dog, PetType.Hamster, PetType.Parrot, PetType.Fish }, new[] { FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.Millet, FoodType.Millet, FoodType.Millet, FoodType.Millet, FoodType.Millet, FoodType.FishFlake, FoodType.FishFlake, FoodType.FishFlake, FoodType.FishFlake, FoodType.FishFlake });
    }

    void GenerateLevel(int id, string name, int cap, int target, PetType[] pets, FoodType[] allFoods)
    {
        // 打乱食物顺序，防止同类食物聚在同一碗
        var rng = new System.Random(id * 137 + name.Length * 73);
        for (int i = allFoods.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var tmp = allFoods[i]; allFoods[i] = allFoods[j]; allFoods[j] = tmp;
        }

        int bowlCount = Mathf.Max(pets.Length + 1, Mathf.CeilToInt(allFoods.Length / (float)cap));
        var inits = new List<BowlInitData>();
        int fi = 0;
        for (int i = 0; i < bowlCount; i++)
        {
            var foods = new List<FoodType>();
            for (int j = 0; j < cap && fi < allFoods.Length; j++)
                foods.Add(allFoods[fi++]);
            inits.Add(new BowlInitData { gridPos = new Vector2Int(i % 4, i / 4), foodStack = foods.ToArray() });
        }

        // 防止初始就出现满碗：检测到全同种→交换一个食物到其他碗
        for (int i = 0; i < inits.Count; i++)
        {
            var bowl = inits[i];
            if (bowl.foodStack.Length < cap) continue;
            bool allSame = true;
            for (int j = 1; j < bowl.foodStack.Length; j++)
                if (bowl.foodStack[j] != bowl.foodStack[0]) { allSame = false; break; }
            if (!allSame) continue;

            // 找一个食物类型不同的碗交换最后一个食物
            for (int k = 0; k < inits.Count; k++)
            {
                if (k == i || inits[k].foodStack.Length == 0) continue;
                if (inits[k].foodStack.Length > 0 && inits[k].foodStack[0] != bowl.foodStack[0])
                {
                    var tmp = bowl.foodStack[bowl.foodStack.Length - 1];
                    bowl.foodStack[bowl.foodStack.Length - 1] = inits[k].foodStack[inits[k].foodStack.Length - 1];
                    inits[k].foodStack[inits[k].foodStack.Length - 1] = tmp;
                    Debug.Log($"[GenerateLevel] 第{id}关碗{i}全相同，与碗{k}交换");
                    break;
                }
            }
        }
        var lv = ScriptableObject.CreateInstance<PetLevelConfigV2>();
        lv.levelId = id; lv.levelName = name; lv.bowlCapacity = cap; lv.targetScore = target;
        lv.petQueue = pets; lv.bowlInits = inits.ToArray();
        levels.Add(lv);
    }

    public int LevelCount => levels.Count;
    public string GetLevelName(int id) => levels.Find(l => l.levelId == id)?.levelName ?? "???";
    public PetLevelConfigV2 GetCurrentLevel() => currentLevel;
    public FoodType? GetHeldFood() => pour.heldFood;
    public int GetScore() => pour.score;
    public List<Bowl> GetBowls() => pour.bowls;
    public PetType? GetCurrentPet() => pour.CurrentPet;
    public List<PetType> GetPetQueue() => pour.originalPets;
    public List<PetType> GetFedPets() => pour.fedPets;
}
