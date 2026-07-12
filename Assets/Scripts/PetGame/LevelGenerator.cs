using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 关卡生成器 — 基于反向生成算法的可解性保证
/// 
/// 算法思路（类似水排序谜题）：
///   1. 创建"已解"状态：A 个碗各装满 B 个同类食物（对应 A 个宠物）
///   2. 加入 extra 个空碗
///   3. 执行 K 次合法前向倒水操作来打乱
///   4. 打乱后的状态保证可解（逆向执行即解法）
/// </summary>
public static class LevelGenerator
{
    private static System.Random rng;

    // 难度对应的额外空碗数
    private static readonly int[] ExtraByDifficulty = { 3, 2, 1 }; // 简单:3, 中等:2, 困难:1

    /// <summary>
    /// 生成一关
    /// </summary>
    /// <param name="pets">宠物队列（决定食物类型和数量）</param>
    /// <param name="capacity">每碗容量 B</param>
    /// <param name="difficulty">难度 0/1/2</param>
    /// <param name="seed">随机种子</param>
    /// <returns>碗初始化数据列表</returns>
    public static List<BowlInitData> Generate(PetType[] pets, int capacity, int difficulty, int seed)
    {
        rng = new System.Random(seed);
        int petCount = pets.Length;
        int extra = ExtraByDifficulty[Mathf.Clamp(difficulty, 0, 2)];
        int totalBowls = petCount + extra;

        if (petCount <= 0 || capacity <= 0)
        {
            Debug.LogError("[LevelGenerator] 参数非法");
            return null;
        }

        // 1. 每个宠物分配一种食物类型
        var usedFoods = new HashSet<FoodType>();
        var petFoodMap = new Dictionary<int, FoodType>(); // index -> food
        for (int i = 0; i < petCount; i++)
        {
            var candidates = FoodPetMap.GetFoodsForPet(pets[i]);
            FoodType chosen = FoodType.Apple; // fallback
            foreach (var c in candidates)
            {
                if (!usedFoods.Contains(c))
                {
                    chosen = c;
                    usedFoods.Add(chosen);
                    break;
                }
            }
            petFoodMap[i] = chosen;
        }

        // 2. 创建"已解"状态：前 A 个碗各装 B 个同类食物
        var bowls = new List<Bowl>();
        for (int i = 0; i < petCount; i++)
        {
            var bowl = new Bowl { bowlId = i, capacity = capacity };
            for (int j = 0; j < capacity; j++)
                bowl.foods.Add(petFoodMap[i]);
            bowls.Add(bowl);
        }
        // 加入 extra 个空碗
        for (int i = 0; i < extra; i++)
            bowls.Add(new Bowl { bowlId = petCount + i, capacity = capacity });

        // 3. 打乱：执行 K 次合法倒水
        int K = 10 + difficulty * 5; // 简单:10步, 中等:15步, 困难:20步
        int scrambles = 0;
        int maxAttempts = K * 10; // 防止死循环

        for (int attempt = 0; attempt < maxAttempts && scrambles < K; attempt++)
        {
            // 选源碗：有食物但未完成
            var candidates = new List<int>();
            for (int i = 0; i < totalBowls; i++)
                if (!bowls[i].IsEmpty && !bowls[i].isCompleted)
                    candidates.Add(i);
            if (candidates.Count == 0) break;

            int srcIdx = candidates[rng.Next(candidates.Count)];
            var src = bowls[srcIdx];
            FoodType srcTop = src.Top!.Value;

            // 数连续同种食物
            int pickCount = 0;
            for (int i = src.foods.Count - 1; i >= 0; i--)
            {
                if (src.foods[i] == srcTop) pickCount++;
                else break;
            }

            // 选目标碗：有空位，且为空或顶层匹配
            candidates.Clear();
            for (int i = 0; i < totalBowls; i++)
            {
                if (i == srcIdx) continue;
                var candidate = bowls[i];
                if (candidate.foods.Count >= capacity) continue;
                if (candidate.isCompleted) continue;
                if (!candidate.IsEmpty && candidate.Top != srcTop) continue;
                candidates.Add(i);
            }
            if (candidates.Count == 0) continue;

            int dstIdx = candidates[rng.Next(candidates.Count)];
            var dst = bowls[dstIdx];
            int space = capacity - dst.foods.Count;
            int pourCount = Mathf.Min(pickCount, space);
            if (pourCount <= 0) continue;

            // 执行倒水
            for (int i = 0; i < pourCount; i++)
            {
                src.Pop();
                dst.foods.Add(srcTop);
            }

            // 如果目标碗意外凑满了同种食物 → 回退（不允许打乱后出现已完成碗）
            if (dst.IsComplete)
            {
                for (int i = 0; i < pourCount; i++)
                {
                    dst.Pop();
                    src.foods.Add(srcTop);
                }
                continue;
            }

            scrambles++;
        }

        Debug.Log($"[LevelGenerator] seed={seed} pets={petCount} cap={capacity} extra={extra} scrambles={scrambles}/{K}");

        // 清除 isCompleted 标记
        foreach (var b in bowls) b.isCompleted = false;

        // 4. 转换为 BowlInitData
        var inits = new List<BowlInitData>();
        for (int i = 0; i < bowls.Count; i++)
            inits.Add(new BowlInitData { foodStack = bowls[i].foods.ToArray() });

        return inits;
    }

    /// <summary>计算推荐的目标分数</summary>
    public static int CalcTargetScore(int petCount, int difficulty)
    {
        // 最优分 = 第一个匹配100 + 第二个60 + 其余30
        int optimal = 100;
        if (petCount >= 2) optimal += 60;
        if (petCount >= 3) optimal += 30 * (petCount - 2);
        // 目标 = 最优的 60%~80%（留容错空间）
        float ratio = difficulty == 0 ? 0.6f : difficulty == 1 ? 0.7f : 0.8f;
        return Mathf.RoundToInt(optimal * ratio / 10) * 10;
    }

    /// <summary>自动生成关卡名称</summary>
    public static string GetLevelName(PetType[] pets, int levelId)
    {
        var cnNames = new Dictionary<PetType, string>
        {
            { PetType.Cat, "猫" }, { PetType.Dog, "狗" }, { PetType.Hamster, "仓鼠" },
            { PetType.Parrot, "鹦鹉" }, { PetType.Fish, "金鱼" }, { PetType.Rabbit, "兔" }
        };
        var names = pets.Distinct().Select(p => cnNames.GetValueOrDefault(p, "?")).ToArray();
        string petStr = string.Join("", names);
        if (petStr.Length > 4) petStr = petStr.Substring(0, 4);

        string[] suffixes = { "初体验", "入门", "热身", "进阶", "挑战", "高手", "大师", "地狱", "极限", "终极" };
        string suffix = levelId <= suffixes.Length ? suffixes[levelId - 1] : $"第{levelId}关";
        return $"{petStr}·{suffix}";
    }
}
