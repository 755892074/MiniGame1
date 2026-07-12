using System.Collections.Generic;
using F8Framework.Core;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 铲屎官疯了 v2 — 脑洞倒水游戏管理器（FSM 驱动）
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

    public PourSystem pour { get; private set; } = new PourSystem();
    public int targetScore => currentLevel?.targetScore ?? 200;

    private PetLevelConfigV2 currentLevel;
    public IFSM<PetGameManager> fsm { get; private set; }

    /// <summary>碗按钮回调：FSM 状态决定行为</summary>
    public System.Action<int> OnBowlClicked;

    #region 事件
    public UnityEvent<int> onScoreChanged = new UnityEvent<int>();
    public UnityEvent<PetType, int, bool> onPetFed = new UnityEvent<PetType, int, bool>();
    public UnityEvent onMistake = new UnityEvent();
    public UnityEvent<int> onLevelComplete = new UnityEvent<int>();
    public UnityEvent onLevelFail = new UnityEvent();
    public UnityEvent<PourResult> onPour = new UnityEvent<PourResult>();
    public UnityEvent onBowlCompleted = new UnityEvent();
    public UnityEvent onSelectionChanged = new UnityEvent();
    public UnityEvent<int, int> onPourAnim = new UnityEvent<int, int>();
    public UnityEvent<int, PetType> onFeedAnim = new UnityEvent<int, PetType>();
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
        if (fsm != null && fsm.IsRunning)
            elapsedTime += Time.deltaTime;
    }

    #region 关卡管理
    void LoadConfig()
    {
        levels.Clear();
        var loaded = Resources.LoadAll<PetLevelConfigV2>("Levels");
        if (loaded != null && loaded.Length > 0)
        {
            levels = new List<PetLevelConfigV2>(loaded);
            levels.Sort((a, b) => a.levelId.CompareTo(b.levelId));
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
        // 防御：无关卡数据时直接返回
        if (levels == null || levels.Count == 0)
        {
            Debug.LogError("[PetGameManager] StartLevel: 无关卡数据!");
            return;
        }

        currentLevel = levels.Find(l => l.levelId == id);
        if (currentLevel == null)
        {
            currentLevel = levels[0];
            currentLevelId = 1;
            Debug.LogWarning($"[PetGameManager] 关卡{id}不存在, 回退到1");
        }
        else currentLevelId = id;

        var bowls = new List<Bowl>();
        if (currentLevel.bowlInits != null)
        {
            foreach (var init in currentLevel.bowlInits)
            {
                var b = new Bowl { bowlId = bowls.Count, capacity = currentLevel.bowlCapacity, gridPos = init.gridPos };
                if (init.foodStack != null)
                    foreach (var f in init.foodStack) b.foods.Add(f);
                bowls.Add(b);
            }
        }

        var pets = currentLevel.petQueue != null ? new List<PetType>(currentLevel.petQueue) : new List<PetType>();
        pour.InitLevel(bowls, pets);
        selectedBowlId = -1;
        elapsedTime = 0;

        // 创建状态机
        fsm = PetGameFSM.Create(this);
    }

    public void AddBowl()
    {
        var bowl = pour.AddEmptyBowl();
        Debug.Log($"[PetGameManager] 新增碗{bowl.bowlId}");
    }

    public PetLevelConfigV2 GetCurrentLevel() => currentLevel;
    public int LevelCount => levels.Count;
    public string GetLevelName(int id) => levels.Find(l => l.levelId == id)?.levelName ?? "???";
    public int GetScore() => pour.score;
    public List<Bowl> GetBowls() => pour.bowls;
    public List<PetType> GetPetQueue()
    {
        var q = new List<PetType>();
        foreach (var p in pour.petQueue) q.Add(p);
        return q;
    }
    public List<PetType> GetFedPets() => pour.fedPets;
    #endregion

    #region 核心逻辑
    /// <summary>从 fromId 倒食物到 toId，处理数据、触发动画、管理状态转换</summary>
    public void PourFromTo(int fromId, int toId, IFSM<PetGameManager> fsmRef)
    {
        selectedBowlId = -1;
        onSelectionChanged.Invoke();

        int count = pour.PickUpAll(fromId);
        if (count == 0) { onMistake.Invoke(); fsmRef.ChangeState<IdleState>(); return; }

        var target = pour.GetBowl(toId);
        int canFit = target != null ? target.capacity - target.foods.Count : 0;
        if (canFit <= 0)
        {
            ReturnFood(fromId, count, pour.heldFood);
            onPour.Invoke(new PourResult());
            onMistake.Invoke();
            fsmRef.ChangeState<IdleState>();
            return;
        }

        // 只倒能装下的数量
        int extra = count - canFit;
        if (extra > 0) { ReturnFood(fromId, extra, pour.heldFood!.Value); count = canFit; }

        var result = pour.PourInto(toId, count);
        if (!result.success)
        {
            ReturnFood(fromId, count, pour.heldFood);
            onPour.Invoke(result);
            onMistake.Invoke();
            fsmRef.ChangeState<IdleState>();
            return;
        }

        pour.SaveHistory(fromId, toId, count);
        onPour.Invoke(result);

        // 进入倒入动画状态
        fsmRef.ChangeState<PouringState>();

        // 处理满碗
        if (result.bowlCompleted)
        {
            onBowlCompleted.Invoke();
            var (points, fedPet, isFirst) = pour.OnBowlComplete(toId);
            onScoreChanged.Invoke(pour.score);
            onPetFed.Invoke(fedPet, points, isFirst);
        }

        // 触发动画事件
        onPourAnim.Invoke(fromId, toId);
        if (result.bowlCompleted)
        {
            var fedPet = pour.GetBowl(toId)?.Top != null
                ? FoodPetMap.GetPet(pour.GetBowl(toId)!.Top!.Value) : PetType.Cat;
            onFeedAnim.Invoke(toId, fedPet);
        }

        // 动画结束后状态由 PetGameUI 的回调切换（见动画协程末尾）
    }

    void ReturnFood(int bowlId, int count, FoodType? food)
    {
        if (!food.HasValue) return;
        var bowl = pour.GetBowl(bowlId);
        for (int i = 0; i < count && bowl != null; i++)
            bowl.Push(food.Value);
        pour.heldFood = null;
    }

    public void Undo()
    {
        if (pour.Undo())
        {
            selectedBowlId = -1;
            fsm?.ChangeState<IdleState>();
            onSelectionChanged.Invoke();
            onPour.Invoke(new PourResult());
            onScoreChanged.Invoke(pour.score);
        }
    }

    public void CheckWin()
    {
        if (pour.petQueue.Count == 0 && pour.bowls.FindAll(b => b.isCompleted).Count >= GetCurrentLevel()?.petQueue.Length)
            fsm?.ChangeState<WinState>();
    }

    public int CalcStars()
    {
        float pct = (float)pour.score / targetScore;
        return pct >= 1.0f ? 3 : pct >= 0.7f ? 2 : 1;
    }
    #endregion

    #region 关卡生成
    void GenerateTestLevel()
    {
        if (levels.Count > 0) return;
        GenerateLevel(1, "鱼+骨头·初体验", 3, 150, new[] { PetType.Cat, PetType.Dog }, new[] { FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat });
        GenerateLevel(2, "加入仓鼠", 3, 200, new[] { PetType.Cat, PetType.Dog, PetType.Hamster }, new[] { FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.SunflowerSeed });
        GenerateLevel(3, "鹦鹉来了", 3, 200, new[] { PetType.Dog, PetType.Parrot, PetType.Cat }, new[] { FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.Millet, FoodType.Millet, FoodType.Millet, FoodType.DriedFish, FoodType.DriedFish, FoodType.CatKibble });
        GenerateLevel(4, "金鱼池", 4, 250, new[] { PetType.Fish, PetType.Cat, PetType.Hamster }, new[] { FoodType.FishFlake, FoodType.FishFlake, FoodType.FishFlake, FoodType.FishFlake, FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.SunflowerSeed });
        GenerateLevel(5, "兔兔跳", 3, 250, new[] { PetType.Rabbit, PetType.Parrot, PetType.Dog }, new[] { FoodType.Carrot, FoodType.Carrot, FoodType.Carrot, FoodType.Millet, FoodType.Millet, FoodType.Millet, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat });
        GenerateLevel(6, "猫狗大战", 4, 300, new[] { PetType.Cat, PetType.Cat, PetType.Dog, PetType.Dog }, new[] { FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.CatKibble, FoodType.CatKibble, FoodType.CatKibble, FoodType.CatKibble, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.DogKibble, FoodType.DogKibble, FoodType.DogKibble, FoodType.DogKibble });
        GenerateLevel(7, "全员出动", 3, 350, new[] { PetType.Hamster, PetType.Fish, PetType.Rabbit, PetType.Parrot }, new[] { FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.SunflowerSeed, FoodType.FishFlake, FoodType.FishFlake, FoodType.FishFlake, FoodType.Carrot, FoodType.Carrot, FoodType.Carrot, FoodType.Millet, FoodType.Millet, FoodType.Millet });
        GenerateLevel(8, "猫咪天堂", 4, 350, new[] { PetType.Cat, PetType.Cat, PetType.Cat }, new[] { FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.DriedFish, FoodType.CatKibble, FoodType.CatKibble, FoodType.CatKibble, FoodType.CatKibble, FoodType.CatTreatStick, FoodType.CatTreatStick, FoodType.CatTreatStick, FoodType.CatTreatStick });
        GenerateLevel(9, "汪汪乐园", 4, 400, new[] { PetType.Dog, PetType.Dog, PetType.Dog }, new[] { FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.BoneTreat, FoodType.DogKibble, FoodType.DogKibble, FoodType.DogKibble, FoodType.DogKibble, FoodType.MeatJerky, FoodType.MeatJerky, FoodType.MeatJerky, FoodType.MeatJerky });
    }

    void GenerateLevel(int id, string name, int cap, int target, PetType[] pets, FoodType[] allFoods)
    {
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

        // 防止初始满碗
        for (int i = 0; i < inits.Count; i++)
        {
            var bowl = inits[i];
            if (bowl.foodStack.Length < cap) continue;
            bool allSame = true;
            for (int j = 1; j < bowl.foodStack.Length; j++)
                if (bowl.foodStack[j] != bowl.foodStack[0]) { allSame = false; break; }
            if (!allSame) continue;
            for (int k = 0; k < inits.Count; k++)
            {
                if (k == i || inits[k].foodStack.Length == 0) continue;
                if (inits[k].foodStack.Length > 0 && inits[k].foodStack[0] != bowl.foodStack[0])
                {
                    var tmp = bowl.foodStack[bowl.foodStack.Length - 1];
                    bowl.foodStack[bowl.foodStack.Length - 1] = inits[k].foodStack[inits[k].foodStack.Length - 1];
                    inits[k].foodStack[inits[k].foodStack.Length - 1] = tmp;
                    break;
                }
            }
        }

        var lv = ScriptableObject.CreateInstance<PetLevelConfigV2>();
        lv.levelId = id; lv.levelName = name; lv.bowlCapacity = cap; lv.targetScore = target;
        lv.petQueue = pets; lv.bowlInits = inits.ToArray();
        levels.Add(lv);
    }
    #endregion
}
