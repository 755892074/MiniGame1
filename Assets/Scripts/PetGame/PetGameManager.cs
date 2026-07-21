using System.Collections.Generic;
using System.Linq;
using F8Framework.Core;
using F8Framework.Launcher;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

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

    /// <summary>最近一次喂食是否为"首位匹配"（用于不公平气泡反馈）。false=非首位，喂错了顺序</summary>
    public bool lastFedIsFirst;

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
        LoadSaveData();
        LoadConfig();

        // 多场景模式：检查 GameSceneManager 是否指定了关卡
        int pendingLevel = GameSceneManager.pendingLevelId;
        if (pendingLevel > 0)
        {
            currentLevelId = pendingLevel;
            GameSceneManager.pendingLevelId = -1;  // 清除标记
        }
    }

    void Update()
    {
        if (fsm != null && fsm.IsRunning)
            elapsedTime += Time.deltaTime;
    }

    #region 关卡管理
    void LoadSaveData()
    {
        SaveSystem.Load();
        currentLevelId = SaveSystem.Data.currentLevelId;
        if (currentLevelId > SaveSystem.Data.highestUnlockedLevel)
            currentLevelId = SaveSystem.Data.highestUnlockedLevel;
        Debug.Log($"[PetGameManager] 读档: 关卡{currentLevelId} / 已解锁{SaveSystem.Data.highestUnlockedLevel} / 小鱼干{SaveSystem.Data.fishDiscount}");
    }

    void LoadConfig()
    {
        levels.Clear();
        var handle = ResLoader.LoadAll<PetLevelConfigV2>("Levels");
        handle.Completed += h =>
        {
            if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null && h.Result.Count > 0)
            {
                levels = new List<PetLevelConfigV2>(h.Result);
                levels.Sort((a, b) => a.levelId.CompareTo(b.levelId));
            }
            AutoStartLevel();
        };
    }

    void AutoStartLevel()
    {
        if (levels.Count == 0)
        {
            Debug.LogError("[PetGameManager] 无关卡数据! 请用菜单 铲屎官疯了 → 批量生成全部关卡 来预生成。");
            return;
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

        SaveSystem.RefillTools();   // 本关道具次数刷新为基数（撤销/加碗/洗牌/提示）

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

    #region 通关结算
    /// <summary>最近一次通关结算结果（供UI取用）</summary>
    public LevelResult lastResult { get; private set; }

    public LevelResult GetCurrentLevelResult() => lastResult;

    /// <summary>
    /// 通关结算：计算星级、发奖励、存档。
    /// 在 WinState 进入时调用。
    /// 返回结算结果供 UI 显示。
    /// </summary>
    public LevelResult OnLevelWin()
    {
        int stars = CalcStars();
        int score = pour.score;

        // 奖励计算
        // 金币：基础 50 × 星级（建造货币，见设计文档 §1.2）
        int goldReward = 50 * stars;
        // 小鱼干：基础50 + 每星20
        int fishReward = 50 + stars * 20;
        // 救助徽章：3星通关才给1枚
        int badgeReward = stars >= 3 ? 1 : 0;
        // 经验：每星30
        int expReward = stars * 30;

        // 存档
        SaveSystem.RecordLevelComplete(currentLevelId, stars, score);
        SaveSystem.AddGold(goldReward);
        SaveSystem.AddFish(fishReward);
        if (badgeReward > 0) SaveSystem.AddBadge(badgeReward);
        bool leveledUp = SaveSystem.AddExp(expReward);
        SaveSystem.Data.currentLevelId = currentLevelId + 1;
        SaveSystem.Save();

        lastResult = new LevelResult
        {
            levelId = currentLevelId,
            stars = stars,
            score = score,
            goldReward = goldReward,
            fishReward = fishReward,
            badgeReward = badgeReward,
            expReward = expReward,
            leveledUp = leveledUp,
            newTitle = leveledUp ? SaveSystem.GetCurrentTitle() : "",
        };

        Debug.Log($"[结算] 关卡{currentLevelId} 通关! {stars}星 / 得分{score} / 金币+{goldReward} / 小鱼干+{fishReward} / 徽章+{badgeReward} / 经验+{expReward}{(leveledUp ? " ★升级!" : "")}");

        return lastResult;
    }
    #endregion

    #region 核心逻辑
    /// <summary>从 fromId 倒食物到 toId，处理数据、触发动画、管理状态转换</summary>
    public void PourFromTo(int fromId, int toId, IFSM<PetGameManager> fsmRef)
    {
        selectedBowlId = -1;
        // onSelectionChanged 延迟到动画事件发出后再调用，避免提前重建 UI 销毁碗/宠物 GO

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

        // 在 PourInto 之前保存历史，确保 wasCompleted 记录的是倒入前的状态
        pour.SaveHistory(fromId, toId, count);

        var result = pour.PourInto(toId, count);
        if (!result.success)
        {
            // 倒入失败，撤销历史记录
            pour.CancelLastHistory();
            ReturnFood(fromId, count, pour.heldFood);
            ClearHeldFood();
            onPour.Invoke(result);
            onMistake.Invoke();
            fsmRef.ChangeState<IdleState>();
            return;
        }

        onPour.Invoke(result);
        fsmRef.ChangeState<PouringState>();

        // 处理满碗
        if (result.bowlCompleted)
        {
            onBowlCompleted.Invoke();
            var (points, fedPet, isFirst) = pour.OnBowlComplete(toId);
            lastFedIsFirst = isFirst;
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

        // 成功路径不在这里重建 UI：FeedAnimation 末尾会自己 BuildBowls/BuildPets/UpdateHUD。
        // 如果提前 Invoke onSelectionChanged，BuildBowls 会立即把碗/宠物 GO 从字典里清掉，
        // 导致刚启动的 FeedAnimation 协程 0.7s 后再查时找不到对象而 yield break。
        // 错误路径会 ChangeState<IdleState>，IdleState.OnStateEnter 里会触发 onSelectionChanged。

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

    /// <summary>IAA 提示：返回当前局面一步可解的倒食物步骤（只读求解，不修改运行时）。无解返回 null。</summary>
    public PetGameSolver.Step? Hint()
    {
        var level = GetCurrentLevel();
        if (level == null || level.petQueue == null) return null;
        var sol = PetGameSolver.Solve(pour, level.petQueue.Length);
        if (sol == null || sol.Count == 0) return null;
        return sol[0];
    }

    public void CheckWin()
    {
        if (pour.petQueue.Count == 0 && pour.bowls.FindAll(b => b.isCompleted).Count >= GetCurrentLevel()?.petQueue.Length)
            fsm?.ChangeState<WinState>();
    }

    /// <summary>检测死局：当前局面是否还能完成所有宠物喂养</summary>
    public bool CheckDeadlock()
    {
        var level = GetCurrentLevel();
        if (level == null || level.petQueue == null) return false;
        int required = level.petQueue.Length;
        int completed = pour.bowls.FindAll(b => b.isCompleted).Count;
        int remaining = required - completed;
        if (remaining <= 0) return false;
        return pour.IsDeadlock(remaining);
    }

    public int CalcStars()
    {
        float pct = (float)pour.score / targetScore;
        return pct >= 1.0f ? 3 : pct >= 0.7f ? 2 : 1;
    }
    #endregion
}
