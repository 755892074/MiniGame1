using System.Collections.Generic;
using System.Linq;
using F8Framework.Core;
using F8Framework.Launcher;
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
    public PetLevelConfigV2 CurrentLevel => currentLevel;
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
    public UnityEvent<int, int, int> onPourAnim = new UnityEvent<int, int, int>(); // fromId, toId, count
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

        // 先销毁旧状态机，防止重复创建（切关/重开时会触发）
        FF8.FSM.DestoryFSM<PetGameManager>("PetGame");

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
            ClearHeldFood();
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
            ClearHeldFood();
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
        onPourAnim.Invoke(fromId, toId, count);
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
        // 不要在这里清空 heldFood，交给调用方处理
    }

    void ClearHeldFood() { pour.heldFood = null; }

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
    void LoadSavedLevels()
    {
        for (int id = 1; id <= 10; id++)
        {
            var lv = Resources.Load<PetLevelConfigV2>($"Levels/Level_{id:D2}");
            if (lv != null)
            {
                lv = Instantiate(lv); // clone 避免修改源资产
                levels.Add(lv);
                Debug.Log($"[PetGameManager] 加载已保存关卡{id}: {lv.levelName}");
            }
        }
    }

    void GenerateTestLevel()
    {
        if (levels.Count > 0) return;

        // 优先从磁盘加载已保存的关卡
        LoadSavedLevels();
        if (levels.Count > 0) return;

        // 磁盘没有 → 用算法自动生成
        Debug.Log("[PetGameManager] 未找到已保存关卡, 自动生成10关");

        // 难度曲线：宠物数↑ 容量↑ 额外碗合理
        PetType[][] petSets = {
            new[] { PetType.Cat, PetType.Dog },
            new[] { PetType.Cat, PetType.Dog },
            new[] { PetType.Cat, PetType.Dog, PetType.Hamster },
            new[] { PetType.Cat, PetType.Dog, PetType.Hamster },
            new[] { PetType.Dog, PetType.Parrot, PetType.Cat },
            new[] { PetType.Dog, PetType.Parrot, PetType.Rabbit, PetType.Cat },
            new[] { PetType.Fish, PetType.Rabbit, PetType.Dog, PetType.Hamster },
            new[] { PetType.Cat, PetType.Dog, PetType.Hamster, PetType.Parrot, PetType.Fish },
            new[] { PetType.Cat, PetType.Dog, PetType.Hamster, PetType.Parrot, PetType.Fish },
            new[] { PetType.Cat, PetType.Dog, PetType.Hamster, PetType.Parrot, PetType.Fish, PetType.Rabbit },
        };

        int[] caps  = { 3, 3, 3, 3, 4, 4, 4, 4, 5, 5 };
        int[] extra = { 0, 1, 0, 1, 1, 1, 2, 2, 2, 2 }; // 额外空碗数

        for (int i = 0; i < 10; i++)
        {
            int id = i + 1;
            var pets = petSets[i];
            int cap = caps[i];
            int ex = extra[i];
            int seed = id * 137 + (int)System.DateTime.Now.Ticks % 1000;

            string name = LevelGenerator.GetLevelName(pets, id);
            int target = LevelGenerator.CalcTargetScore(pets.Distinct().Count());

            var inits = LevelGenerator.Generate(pets, cap, ex, seed);
            if (inits == null) { Debug.LogError($"[PetGameManager] 关卡{id}生成失败!"); continue; }

            var lv = ScriptableObject.CreateInstance<PetLevelConfigV2>();
            lv.levelId = id;
            lv.levelName = name;
            lv.bowlCapacity = cap;
            lv.targetScore = target;
            lv.difficulty = i < 3 ? 0 : (i < 7 ? 1 : 2);
            lv.petQueue = pets;
            lv.bowlInits = inits.ToArray();
            levels.Add(lv);

            int totalFoods = pets.Length * cap;
            int bowlCount = inits.Count;
            Debug.Log($"[PetGameManager] 关卡{id}「{name}」: {pets.Distinct().Count()}宠×{cap}={totalFoods}食物, {bowlCount}碗(含{ex}空), 目标{target}分");
        }
    }
    #endregion
}
