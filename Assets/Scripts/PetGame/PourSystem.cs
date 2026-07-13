using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 脑洞倒水核心逻辑 — 纯C#，管理碗、宠物和倒食物操作
/// </summary>
public class PourSystem
{
    public List<Bowl> bowls = new List<Bowl>();
    public Queue<PetType> petQueue = new Queue<PetType>();
    public List<PetType> originalPets = new List<PetType>(); // 保留原始顺序
    public List<PetType> fedPets = new List<PetType>();

    public FoodType? heldFood;
    public int score;
    public int comboCount;
    public int totalMoves;

    private Stack<PourAction> history = new Stack<PourAction>();

    public int TotalPets => originalPets.Count;
    public int FedCount => fedPets.Count;
    public PetType? CurrentPet => petQueue.Count > 0 ? petQueue.Peek() : (PetType?)null;
    public bool IsComplete => fedPets.Count >= originalPets.Count;

    struct PourAction
    {
        public int fromBowl, toBowl;
        public int count;                   // 倒了几个食物
        public FoodType? prevHeld;
        public int prevScore, prevCombo;
        public List<PetType> prevFedPets;
        public bool wasCompleted;
    }

    public void InitLevel(List<Bowl> initBowls, List<PetType> pets)
    {
        bowls = new List<Bowl>();
        foreach (var b in initBowls) bowls.Add(b.Clone());
        petQueue = new Queue<PetType>();
        originalPets = new List<PetType>(pets);
        foreach (var p in pets) petQueue.Enqueue(p);
        fedPets = new List<PetType>();
        heldFood = null;
        score = 0;
        comboCount = 0;
        totalMoves = 0;
        history.Clear();
        Debug.Log($"[PourSystem] Init: {bowls.Count}碗, {pets.Count}宠物排队: [{string.Join(",", pets)}]");
    }

    /// <summary>点击碗：取顶层所有同种食物</summary>
    public int PickUpAll(int bowlId)
    {
        var bowl = GetBowl(bowlId);
        if (bowl == null || bowl.IsEmpty) return 0;
        if (bowl.isCompleted) return 0;

        var top = bowl.Top!.Value;
        int count = 0;
        for (int i = bowl.foods.Count - 1; i >= 0; i--)
        {
            if (bowl.foods[i] == top) count++;
            else break;
        }

        // 移除 count 个
        for (int i = 0; i < count; i++)
            heldFood = bowl.Pop();
        return count;
    }

    /// <summary>点击目标碗：倒入所有手上的食物（count个）</summary>
    public PourResult PourInto(int targetBowlId, int count)
    {
        var result = new PourResult();
        if (heldFood == null) { result.reason = "手上没有食物"; return result; }
        var target = GetBowl(targetBowlId);
        if (target == null) { result.reason = "碗不存在"; return result; }
        if (target.isCompleted) { result.reason = "碗已完成"; return result; }
        if (target.foods.Count >= target.capacity) { result.reason = "碗已满"; return result; }
        if (target.foods.Count + count > target.capacity) { result.reason = "目标碗容量不足"; return result; }
        if (!target.IsEmpty && target.Top != heldFood) { result.reason = $"不能倒入：碗顶层是{target.Top}，手上是{heldFood}"; return result; }

        var f = heldFood.Value;
        for (int i = 0; i < count; i++)
            target.Push(f);
        heldFood = null;
        totalMoves++;
        result.success = true;

        if (target.IsComplete)
        {
            target.isCompleted = true;
            comboCount++;
            result.bowlCompleted = true;
        }
        return result;
    }

    /// <summary>处理满碗匹配宠物，返回得分和匹配的宠物</summary>
    public (int points, PetType fedPet, bool isFirst) OnBowlComplete(int bowlId)
    {
        var bowl = GetBowl(bowlId);
        if (bowl == null || !bowl.isCompleted || !bowl.Top.HasValue)
            return (0, PetType.Cat, false);

        var foodType = bowl.Top.Value;
        var matchedPet = FoodPetMap.GetPet(foodType);

        int pos = FindPetPosition(matchedPet);
        int points;
        bool isFirst;

        if (pos == 0)
        {
            points = ScoreConfig.MatchFirst + comboCount * ScoreConfig.ComboBonus;
            isFirst = true;
        }
        else if (pos == 1)
        {
            points = ScoreConfig.MatchSecond;
            isFirst = false;
        }
        else
        {
            points = ScoreConfig.MatchThird;
            isFirst = false;
        }

        score += points;
        fedPets.Add(matchedPet);

        // 从队列中移除宠物
        var newQueue = new Queue<PetType>();
        bool removed = false;
        while (petQueue.Count > 0)
        {
            var p = petQueue.Dequeue();
            if (!removed && p == matchedPet) { removed = true; continue; }
            newQueue.Enqueue(p);
        }
        petQueue = newQueue;

        Debug.Log($"[PourSystem] OnBowlComplete: 碗{bowlId}→{foodType}→宠物{matchedPet}(队列位置{pos}), +{points}分, 总分{score}, combo={comboCount}, isFirst={isFirst}");
        return (points, matchedPet, isFirst);
    }

    /// <summary>撤销一步</summary>
    public bool Undo()
    {
        if (history.Count == 0) { Debug.Log("[PourSystem] Undo: 无历史"); return false; }
        var act = history.Pop();
        var from = GetBowl(act.fromBowl);
        var to = GetBowl(act.toBowl);
        if (from == null || to == null) return false;

        // 从目标碗顶端取回 count 个食物
        var moved = new FoodType[act.count];
        for (int i = 0; i < act.count && to.foods.Count > 0; i++)
            moved[i] = to.Pop();
        // 逆序放回源碗（保持原来顺序）
        for (int i = act.count - 1; i >= 0; i--)
            from.Push(moved[i]);
        to.isCompleted = act.wasCompleted;

        heldFood = act.prevHeld;
        score = act.prevScore;
        comboCount = act.prevCombo;
        totalMoves = Mathf.Max(0, totalMoves - 1);
        if (act.prevFedPets != null) { fedPets = act.prevFedPets; RebuildQueue(); }
        Debug.Log($"[PourSystem] Undo: 回退{act.count}个食物, 当前分={score}");
        return true;
    }

    /// <summary>IAA：增加一个空碗</summary>
    public Bowl AddEmptyBowl()
    {
        var bowl = new Bowl { bowlId = bowls.Count, capacity = bowls.Count > 0 ? bowls[0].capacity : 3, gridPos = FindFreeGridPos() };
        bowls.Add(bowl);
        Debug.Log($"[PourSystem] AddBowl: 新增碗{bowl.bowlId} 位置({bowl.gridPos.x},{bowl.gridPos.y})");
        return bowl;
    }

    /// <summary>IAA：打乱指定碗的食物</summary>
    public bool ShuffleBowl(int bowlId)
    {
        var bowl = GetBowl(bowlId);
        if (bowl == null || bowl.foods.Count < 2) return false;
        for (int i = bowl.foods.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (bowl.foods[i], bowl.foods[j]) = (bowl.foods[j], bowl.foods[i]);
        }
        Debug.Log($"[PourSystem] Shuffle: 碗{bowlId}食物已打乱, 新顶={bowl.Top}");
        return true;
    }

    #region 工具
    public Bowl GetBowl(int id) => bowls.Find(b => b.bowlId == id);

    int FindBowlHeldFrom()
    {
        foreach (var b in bowls) if (!b.isCompleted && b.foods.Count > 0) return b.bowlId;
        return -1;
    }

    int FindPetPosition(PetType pet)
    {
        int i = 0;
        foreach (var p in originalPets)
        {
            if (!fedPets.Contains(p) && p == pet) return i;
            if (!fedPets.Contains(p)) i++;
        }
        return 99;
    }

    void RebuildQueue()
    {
        petQueue = new Queue<PetType>();
        foreach (var p in originalPets)
            if (!fedPets.Contains(p))
                petQueue.Enqueue(p);
    }

    Vector2Int FindFreeGridPos()
    {
        var used = new HashSet<Vector2Int>();
        foreach (var b in bowls) used.Add(b.gridPos);
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                if (!used.Contains(new Vector2Int(x, y)))
                    return new Vector2Int(x, y);
        return new Vector2Int(Random.Range(0, 5), Random.Range(0, 5));
    }

    public void SaveHistory(int fromBowl, int toBowl, int count)
    {
        history.Push(new PourAction
        {
            fromBowl = fromBowl,
            toBowl = toBowl,
            count = count,
            prevHeld = heldFood,
            prevScore = score,
            prevCombo = comboCount,
            prevFedPets = new List<PetType>(fedPets),
            wasCompleted = GetBowl(toBowl)?.isCompleted ?? false, // 倒入前的状态
        });
    }

    /// <summary>撤销最后一条历史记录（PourInto 失败时用）</summary>
    public void CancelLastHistory()
    {
        if (history.Count > 0) history.Pop();
    }

    /// <summary>BFS 检测当前局面是否为死局（无法完成所有宠物喂养）</summary>
    public bool IsDeadlock(int requiredCompletes)
    {
        // 轻量 BFS：限制深度 12，状态数 2000，足够覆盖大部分死局
        int maxDepth = 12;
        int maxStates = 2000;
        var visited = new HashSet<string>();
        var queue = new Queue<(List<Bowl> state, int depth)>();
        string initialCanon = CanonicalizeRuntime(bowls);
        visited.Add(initialCanon);
        queue.Enqueue((CloneBowlsRuntime(bowls), 0));

        while (queue.Count > 0 && visited.Count < maxStates)
        {
            var (state, depth) = queue.Dequeue();

            int completeCount = 0;
            foreach (var b in state) if (b.IsComplete) completeCount++;
            if (completeCount >= requiredCompletes)
                return false; // 能解，不是死局

            if (depth >= maxDepth) continue;

            int bowlCount = state.Count;
            for (int from = 0; from < bowlCount; from++)
            {
                var src = state[from];
                if (src.IsEmpty || src.isCompleted) continue;
                if (!src.Top.HasValue) continue;

                FoodType topFood = src.Top.Value;
                int pickCount = 0;
                for (int i = src.foods.Count - 1; i >= 0; i--)
                {
                    if (src.foods[i] == topFood) pickCount++;
                    else break;
                }

                for (int to = 0; to < bowlCount; to++)
                {
                    if (from == to) continue;
                    var dst = state[to];
                    if (dst.isCompleted) continue;
                    if (dst.foods.Count >= dst.capacity) continue;
                    if (!dst.IsEmpty && dst.Top != topFood) continue;

                    int pourCount = Mathf.Min(pickCount, dst.capacity - dst.foods.Count);
                    if (pourCount <= 0) continue;

                    var newState = CloneBowlsRuntime(state);
                    for (int k = 0; k < pourCount; k++)
                        newState[from].Pop();
                    for (int k = 0; k < pourCount; k++)
                        newState[to].foods.Add(topFood);

                    string canon = CanonicalizeRuntime(newState);
                    if (visited.Add(canon))
                        queue.Enqueue((newState, depth + 1));
                }
            }
        }

        // BFS 耗尽仍未找到解 → 死局
        return true;
    }

    private string CanonicalizeRuntime(List<Bowl> bowls)
    {
        var parts = bowls.Select(b =>
            b.foods.Count == 0 ? "_" : string.Join(",", b.foods.Select(f => ((int)f).ToString()))
        ).ToList();
        parts.Sort();
        return string.Join("|", parts);
    }

    private List<Bowl> CloneBowlsRuntime(List<Bowl> src)
    {
        var clone = new List<Bowl>();
        foreach (var b in src)
        {
            var cb = new Bowl { bowlId = b.bowlId, capacity = b.capacity, isCompleted = b.isCompleted };
            cb.foods.AddRange(b.foods);
            clone.Add(cb);
        }
        return clone;
    }
    #endregion
}
