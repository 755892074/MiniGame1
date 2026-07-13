using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.Linq;

/// <summary>
/// 关卡生成器 v3 — 随机分配 + BFS 验证管线
///
/// 算法:
///   1. 为每种宠物生成 capacity 个食物，共 petCount×capacity 个
///   2. 随机分配到 (petCount + extraBowls) 个碗中，每碗 0~capacity 个
///   3. 确保没有初始已完成碗（满碗同色）
///   4. BFS 验证可解性 + 求最少步数
///   5. 不可解或 minSteps 不在范围则换种子重试
/// </summary>
public static class LevelGenerator
{
    public static bool Generate(PetType[] pets, int capacity, int extraBowls, int seed,
        out LevelData result, int timeoutMs = 5000, int maxAttempts = 200)
    {
        result = null;
        int petCount = pets.Distinct().Count();
        int totalBowls = petCount + extraBowls;
        int totalFoods = petCount * capacity;

        // 宠物→食物映射
        var petFoodMap = new Dictionary<PetType, FoodType>();
        foreach (var pet in pets.Distinct())
            petFoodMap[pet] = FoodPetMap.GetFoodForPet(pet);

        // 构造食物池：每种宠物对应 capacity 个同色食物
        var foodPool = new List<FoodType>();
        foreach (var pet in pets.Distinct())
            for (int i = 0; i < capacity; i++)
                foodPool.Add(petFoodMap[pet]);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int actualSeed = seed + attempt * 31;
            var rng = new System.Random(actualSeed);

            // 1. Fisher-Yates 洗牌食物池
            var shuffled = new List<FoodType>(foodPool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            // 2. 分配到碗中（完全随机，只看有无余量）
            var bowls = new List<List<FoodType>>();
            for (int b = 0; b < totalBowls; b++)
                bowls.Add(new List<FoodType>());

            bool allocOk = true;
            foreach (var food in shuffled)
            {
                // 随机选一个有余量的碗（不限制颜色）
                var candidates = new List<int>();
                for (int b = 0; b < bowls.Count; b++)
                    if (bowls[b].Count < capacity) candidates.Add(b);

                if (candidates.Count == 0) { allocOk = false; break; }

                int target = candidates[rng.Next(candidates.Count)];
                bowls[target].Add(food);
            }
            if (!allocOk) continue;

            // 3. 确保没有初始已完成碗
            bool hasInitialComplete = false;
            foreach (var b in bowls)
            {
                if (b.Count == capacity && b.All(f => f.Equals(b[0])))
                {
                    hasInitialComplete = true;
                    break;
                }
            }
            if (hasInitialComplete) continue;

            // 4. 确保所有食物都分配了
            int placedCount = bowls.Sum(b => b.Count);
            if (placedCount < totalFoods) continue;

            // 5. BFS 验证可解性 + 求最少步数
            var sw = Stopwatch.StartNew();
            int minSteps = BFSMinSteps(bowls, capacity, petCount, sw, timeoutMs);

            if (minSteps > 0)
            {
                var inits = new List<BowlInitData>();
                for (int i = 0; i < bowls.Count; i++)
                    inits.Add(new BowlInitData { foodStack = bowls[i].ToArray() });

                result = new LevelData
                {
                    bowlInits = inits,
                    minSteps = minSteps,
                    seed = actualSeed,
                    petCount = petCount,
                    capacity = capacity,
                    extraBowls = extraBowls,
                    attempt = attempt + 1
                };

                UnityEngine.Debug.Log($"[LevelGenerator] ✓ seed={actualSeed} pets={petCount} cap={capacity} extra={extraBowls} minSteps={minSteps} attempt={attempt + 1}");
                return true;
            }
        }

        UnityEngine.Debug.LogError($"[LevelGenerator] {maxAttempts}次尝试后仍不可解! seed={seed}");
        return false;
    }

    // 旧版兼容
    public static List<BowlInitData> Generate(PetType[] pets, int capacity, int extraBowls, int seed, int maxAttempts = 200)
    {
        if (Generate(pets, capacity, extraBowls, seed, out var data, maxAttempts: maxAttempts))
            return data.bowlInits;
        return null;
    }

    /// <summary>
    /// BFS 求最少步数解（带超时保护）
    /// 返回最少步数，-1=不可解或超时
    /// </summary>
    private static int BFSMinSteps(List<List<FoodType>> initialBowls, int capacity,
        int requiredCompletes, Stopwatch sw, int timeoutMs)
    {
        // 先检查初始状态是否已经满足（不应该发生，但安全起见）
        int initialComplete = CountComplete(initialBowls, capacity);
        if (initialComplete >= requiredCompletes)
            return 0;

        string initialState = Canonicalize(initialBowls);
        var visited = new HashSet<string> { initialState };
        var queue = new Queue<(List<List<FoodType>> state, int depth)>();
        queue.Enqueue((CloneState(initialBowls), 0));

        int maxDepth = 40;
        long maxStates = 100000;

        while (queue.Count > 0)
        {
            if (queue.Count % 100 == 0 && sw.ElapsedMilliseconds > timeoutMs)
                return -1;

            var (state, depth) = queue.Dequeue();

            int completeCount = CountComplete(state, capacity);
            if (completeCount >= requiredCompletes)
                return depth;

            if (depth >= maxDepth) continue;
            if (visited.Count >= maxStates) continue;

            int bowlCount = state.Count;
            for (int from = 0; from < bowlCount; from++)
            {
                var src = state[from];
                if (src.Count == 0) continue;

                // 跳过已满同色碗
                bool srcComplete = src.Count == capacity && src.All(f => f.Equals(src[0]));
                if (srcComplete) continue;

                FoodType topFood = src[src.Count - 1];
                int topCount = 0;
                for (int i = src.Count - 1; i >= 0; i--)
                {
                    if (src[i].Equals(topFood)) topCount++;
                    else break;
                }

                for (int to = 0; to < bowlCount; to++)
                {
                    if (from == to) continue;
                    var dst = state[to];
                    if (dst.Count >= capacity) continue;
                    if (dst.Count > 0 && !dst[dst.Count - 1].Equals(topFood)) continue;

                    int pourCount = Mathf.Min(topCount, capacity - dst.Count);
                    if (pourCount <= 0) continue;

                    var newState = CloneState(state);
                    for (int k = 0; k < pourCount; k++)
                        newState[from].RemoveAt(newState[from].Count - 1);
                    for (int k = 0; k < pourCount; k++)
                        newState[to].Add(topFood);

                    string canon = Canonicalize(newState);
                    if (visited.Add(canon))
                        queue.Enqueue((newState, depth + 1));
                }
            }
        }

        return -1;
    }

    private static int CountComplete(List<List<FoodType>> bowls, int capacity)
    {
        int count = 0;
        foreach (var b in bowls)
            if (b.Count == capacity && b.Count > 0 && b.All(f => f.Equals(b[0])))
                count++;
        return count;
    }

    private static string Canonicalize(List<List<FoodType>> bowls)
    {
        var parts = bowls.Select(b =>
            b.Count == 0 ? "_" : string.Join(",", b.Select(f => ((int)f).ToString()))
        ).ToList();
        parts.Sort();
        return string.Join("|", parts);
    }

    private static List<List<FoodType>> CloneState(List<List<FoodType>> src)
    {
        var clone = new List<List<FoodType>>(src.Count);
        foreach (var b in src)
            clone.Add(new List<FoodType>(b));
        return clone;
    }

    /// <summary>计算目标分（最优分的70%）</summary>
    public static int CalcTargetScore(int petCount)
    {
        int optimal = 100;
        if (petCount >= 2) optimal += 60;
        if (petCount >= 3) optimal += 30 * (petCount - 2);
        return Mathf.RoundToInt(optimal * 0.7f / 10) * 10;
    }

    /// <summary>自动关卡名称</summary>
    public static string GetLevelName(PetType[] pets, int levelId)
    {
        var cn = new Dictionary<PetType, string> {
            { PetType.Cat, "猫" }, { PetType.Dog, "狗" }, { PetType.Hamster, "仓" },
            { PetType.Parrot, "鹦" }, { PetType.Fish, "鱼" }, { PetType.Rabbit, "兔" }
        };
        var names = pets.Distinct().Select(p => cn.GetValueOrDefault(p, "?")).ToArray();
        string petStr = string.Join("", names);
        string suffix = levelId switch {
            1 => "初体验", 2 => "小试", 3 => "热身", 4 => "进阶", 5 => "挑战",
            6 => "高手", 7 => "大师", 8 => "地狱", 9 => "极限", _ => "终极"
        };
        return $"{petStr}·{suffix}";
    }

    public static string GetDifficultyLabel(int levelId)
    {
        if (levelId <= 5) return "新手";
        if (levelId <= 10) return "入门";
        if (levelId <= 15) return "进阶";
        if (levelId <= 20) return "中难";
        if (levelId <= 30) return "困难";
        if (levelId <= 50) return "挑战";
        return "地狱";
    }
}

/// <summary>
/// 关卡生成结果（含难度标注）
/// </summary>
public class LevelData
{
    public List<BowlInitData> bowlInits;
    public int minSteps;
    public int seed;
    public int petCount;
    public int capacity;
    public int extraBowls;
    public int attempt;
}
