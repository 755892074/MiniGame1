using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 关卡生成器 — 随机分布 + BFS 验证可解性
/// 
/// 算法：
///   1. 为每个宠物分配不同食物类型
///   2. Fisher-Yates 洗牌所有食物
///   3. 随机分配到碗中（每碗 0~B 个，无初始完成碗）
///   4. BFS 深度限制验证可解（深度12~15）
///   5. 不可解则重新洗牌，最多尝试 200 次
/// </summary>
public static class LevelGenerator
{
    private static System.Random rng;

    /// <param name="pets">宠物队列（去重，每种宠物对应一种食物）</param>
    /// <param name="capacity">单碗容量 B</param>
    /// <param name="extraBowls">额外空碗数</param>
    /// <param name="seed">随机种子</param>
    /// <param name="maxAttempts">最大重试次数</param>
    public static List<BowlInitData> Generate(PetType[] pets, int capacity, int extraBowls, int seed, int maxAttempts = 200)
    {
        rng = new System.Random(seed);
        int petCount = pets.Distinct().Count();
        int totalBowls = petCount + extraBowls;
        if (totalBowls < petCount) totalBowls = petCount; // 至少要有 petCount 个碗

        // 1. 为每个宠物分配食物（严格一对一映射）
        var petFoodMap = new Dictionary<PetType, FoodType>();
        foreach (var pet in pets)
        {
            if (petFoodMap.ContainsKey(pet)) continue;
            petFoodMap[pet] = FoodPetMap.GetFoodForPet(pet);
        }

        // 2. 生成食物列表：每个宠物 × B 个同类食物
        var allFoods = new List<FoodType>();
        foreach (var pet in pets)
            for (int i = 0; i < capacity; i++)
                allFoods.Add(petFoodMap[pet]);

        int foodCount = allFoods.Count;

        // 3. 多次尝试随机生成 + BFS 验证
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Fisher-Yates shuffle
            for (int i = allFoods.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = allFoods[i]; allFoods[i] = allFoods[j]; allFoods[j] = tmp;
            }

            // 随机分配到碗
            var bowls = new List<Bowl>();
            int foodCursor = 0;
            bool valid = true;

            for (int b = 0; b < totalBowls; b++)
            {
                var bowl = new Bowl { bowlId = b, capacity = capacity };
                int remaining = allFoods.Count - foodCursor;
                if (remaining <= 0) break;

                // 随机放 0~capacity 个，但确保最后几个碗能均匀分完
                int maxFill = Mathf.Min(capacity, remaining);
                int fill = rng.Next(0, maxFill + 1);

                // 不能全空：如果前期碗太空，后面食物装不下
                int bowlsLeft = totalBowls - b - 1;
                int maxAfter = bowlsLeft * capacity;
                int minNeeded = Mathf.Max(0, remaining - maxAfter);
                fill = Mathf.Clamp(fill, minNeeded, maxFill);

                for (int k = 0; k < fill; k++)
                    bowl.foods.Add(allFoods[foodCursor + k]);
                foodCursor += fill;

                // 不能有初始已完成碗
                if (bowl.IsComplete)
                {
                    valid = false;
                    break;
                }
                bowls.Add(bowl);
            }

            if (!valid || foodCursor < allFoods.Count) continue;

            // 补齐空碗
            while (bowls.Count < totalBowls)
                bowls.Add(new Bowl { bowlId = bowls.Count, capacity = capacity });

            // BFS 验证（限制深度避免卡死）
            int bfsDepth = Mathf.Clamp(petCount * 4, 10, 20);
            if (IsSolvable(bowls, capacity, petCount, bfsDepth))
            {
                var inits = new List<BowlInitData>();
                for (int i = 0; i < bowls.Count; i++)
                    inits.Add(new BowlInitData { foodStack = bowls[i].foods.ToArray() });
                Debug.Log($"[LevelGenerator] seed={seed} pets={petCount} cap={capacity} bowls={totalBowls} extra={extraBowls} foods={foodCount} attempt={attempt + 1} ✓可解");
                return inits;
            }
        }

        Debug.LogError($"[LevelGenerator] {maxAttempts}次尝试后仍不可解! seed={seed}");
        return null;
    }

    /// <summary>BFS 验证可解性（限制状态数）</summary>
    private static bool IsSolvable(List<Bowl> bowls, int capacity, int requiredCompletes, int maxDepth)
    {
        int maxStates = 8000;
        var visited = new HashSet<string>();
        var initialState = Canonicalize(bowls);
        visited.Add(initialState);

        var queue = new Queue<(List<Bowl> state, int depth)>();
        queue.Enqueue((CloneBowls(bowls), 0));

        while (queue.Count > 0 && visited.Count < maxStates)
        {
            var (state, depth) = queue.Dequeue();

            int completeCount = 0;
            foreach (var b in state) if (b.IsComplete) completeCount++;
            if (completeCount >= requiredCompletes)
                return true;

            if (depth >= maxDepth) continue;

            int bowlCount = state.Count;
            for (int from = 0; from < bowlCount; from++)
            {
                var src = state[from];
                if (src.IsEmpty || src.isCompleted) continue;

                int pickCount = 0;
                FoodType topFood = src.Top!.Value;
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
                    if (dst.foods.Count >= capacity) continue;
                    if (!dst.IsEmpty && dst.Top != topFood) continue;

                    int pourCount = Mathf.Min(pickCount, capacity - dst.foods.Count);
                    if (pourCount <= 0) continue;

                    var newState = CloneBowls(state);
                    for (int k = 0; k < pourCount; k++)
                        newState[from].Pop();
                    for (int k = 0; k < pourCount; k++)
                        newState[to].foods.Add(topFood);

                    newState[to].isCompleted = false; // BFS中不标记完成

                    var canon = Canonicalize(newState);
                    if (visited.Add(canon))
                        queue.Enqueue((newState, depth + 1));
                }
            }
        }

        return false;
    }

    private static List<Bowl> CloneBowls(List<Bowl> bowls)
    {
        var clone = new List<Bowl>();
        foreach (var b in bowls)
        {
            var cb = new Bowl { bowlId = b.bowlId, capacity = b.capacity };
            cb.foods.AddRange(b.foods);
            clone.Add(cb);
        }
        return clone;
    }

    private static string Canonicalize(List<Bowl> bowls)
    {
        var lists = bowls.Select(b =>
            b.foods.Count == 0 ? "_" : string.Join(",", b.foods.Select(f => ((int)f).ToString()))
        ).ToList();
        lists.Sort();
        return string.Join("|", lists);
    }

    /// <summary>计算目标分</summary>
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
}
