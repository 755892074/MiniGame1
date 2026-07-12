using System.Collections.Generic;
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

        if (target.IsFull)
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

        // 恢复目标碗
        if (to.foods.Count > 0) to.foods.RemoveAt(to.foods.Count - 1);
        to.isCompleted = act.wasCompleted;
        // 恢复来源碗
        var f = to.Pop(); // the food we just put
        from.Push(f);
        heldFood = act.prevHeld;
        score = act.prevScore;
        comboCount = act.prevCombo;
        if (act.prevFedPets != null) { fedPets = act.prevFedPets; RebuildQueue(); }
        Debug.Log($"[PourSystem] Undo: 回退, 当前分={score}");
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

    void SaveHistory(int fromBowl, int toBowl)
    {
        history.Push(new PourAction
        {
            fromBowl = fromBowl,
            toBowl = toBowl,
            prevHeld = heldFood,
            prevScore = score,
            prevCombo = comboCount,
            prevFedPets = new List<PetType>(fedPets),
            wasCompleted = GetBowl(toBowl)?.isCompleted ?? false,
        });
    }

    /// <summary>BFS 验证关卡可解（简化版）</summary>
    public static bool IsSolvable(List<Bowl> bowls, List<PetType> pets, int capacity, int maxSteps = 50)
    {
        // 简化：只要食物数足够喂所有宠物即可
        int totalNeeded = pets.Count * capacity;
        int totalAvailable = 0;
        foreach (var b in bowls) totalAvailable += b.foods.Count;
        Debug.Log($"[PourSystem] IsSolvable: 需要{totalNeeded}, 可用{totalAvailable}, → {(totalAvailable >= totalNeeded ? "YES" : "NO")}");
        return totalAvailable >= totalNeeded;
    }
    #endregion
}
