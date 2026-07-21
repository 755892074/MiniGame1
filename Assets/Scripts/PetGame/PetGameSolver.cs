using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 铲屎官疯了 v2 — 通关步骤求解器（GM 调试用）。
/// 在不借助任何 IAA 工具（撤销 / 加空碗 / 洗牌 / 提示）的前提下，
/// 用 BFS 求一条能完成全部宠物喂养的倒食物步骤序列。
/// 求解过程只读当前局面、克隆状态，不会修改运行时 PourSystem。
/// </summary>
public static class PetGameSolver
{
    /// <summary>一步倒食物操作（用于 UI 展示）</summary>
    public struct Step
    {
        public int fromId;     // 源碗 bowlId
        public int toId;       // 目标碗 bowlId
        public int count;      // 倒几个
        public FoodType food;  // 倒的食物类型
    }

    class SNode
    {
        public List<SBowl> bowls;
        public List<Step> path;
    }

    class SBowl
    {
        public int id;
        public int capacity;
        public List<FoodType> foods = new List<FoodType>();
        public bool isCompleted;
    }

    /// <summary>
    /// 求通关步骤。
    /// </summary>
    /// <param name="pour">当前 PourSystem（只读）</param>
    /// <param name="requiredCompletes">需要完成的碗数（= 宠物队列长度）</param>
    /// <returns>步骤列表；无解/超时返回 null</returns>
    public static List<Step> Solve(PourSystem pour, int requiredCompletes)
    {
        if (pour == null || requiredCompletes <= 0) return null;

        var start = new SNode
        {
            bowls = CloneBowls(pour.bowls),
            path = new List<Step>(),
        };

        var visited = new HashSet<string> { Canonicalize(start.bowls) };
        var queue = new Queue<SNode>();
        queue.Enqueue(start);

        int maxDepth = 50;
        long maxStates = 60000;

        while (queue.Count > 0)
        {
            if (visited.Count >= maxStates) break;

            var node = queue.Dequeue();

            if (CountComplete(node.bowls) >= requiredCompletes)
                return node.path;

            if (node.path.Count >= maxDepth) continue;

            int n = node.bowls.Count;
            for (int from = 0; from < n; from++)
            {
                var src = node.bowls[from];
                if (src.isCompleted || src.foods.Count == 0) continue;

                FoodType top = src.foods[src.foods.Count - 1];
                int topCount = 0;
                for (int i = src.foods.Count - 1; i >= 0; i--)
                {
                    if (src.foods[i] == top) topCount++;
                    else break;
                }
                if (topCount == 0) continue;

                for (int to = 0; to < n; to++)
                {
                    if (from == to) continue;
                    var dst = node.bowls[to];
                    if (dst.isCompleted) continue;
                    if (dst.foods.Count >= dst.capacity) continue;
                    if (dst.foods.Count > 0 && dst.foods[dst.foods.Count - 1] != top) continue;

                    int pourCount = Mathf.Min(topCount, dst.capacity - dst.foods.Count);
                    if (pourCount <= 0) continue;

                    var nb = CloneBowls(node.bowls);
                    for (int k = 0; k < pourCount; k++)
                        nb[from].foods.RemoveAt(nb[from].foods.Count - 1);
                    for (int k = 0; k < pourCount; k++)
                        nb[to].foods.Add(top);
                    // 满碗且同色 → 完成（与 Bowl.IsComplete 语义一致）
                    if (nb[to].foods.Count >= nb[to].capacity && nb[to].foods.All(f => f == top))
                        nb[to].isCompleted = true;

                    string canon = Canonicalize(nb);
                    if (!visited.Add(canon)) continue;

                    var np = new List<Step>(node.path)
                    {
                        new Step { fromId = src.id, toId = dst.id, count = pourCount, food = top }
                    };
                    queue.Enqueue(new SNode { bowls = nb, path = np });
                }
            }
        }

        return null; // 未找到（死局或超出搜索上限）
    }

    static int CountComplete(List<SBowl> bowls)
    {
        int c = 0;
        foreach (var b in bowls) if (b.isCompleted) c++;
        return c;
    }

    static List<SBowl> CloneBowls(List<Bowl> src)
    {
        var list = new List<SBowl>(src.Count);
        foreach (var b in src)
            list.Add(new SBowl { id = b.bowlId, capacity = b.capacity, isCompleted = b.isCompleted, foods = new List<FoodType>(b.foods) });
        return list;
    }

    static List<SBowl> CloneBowls(List<SBowl> src)
    {
        var list = new List<SBowl>(src.Count);
        foreach (var b in src)
            list.Add(new SBowl { id = b.id, capacity = b.capacity, isCompleted = b.isCompleted, foods = new List<FoodType>(b.foods) });
        return list;
    }

    /// <summary>规范化（按食物多重集排序，忽略碗序，仅用于访问去重）</summary>
    static string Canonicalize(List<SBowl> bowls)
    {
        var parts = bowls.Select(b =>
            b.foods.Count == 0 ? "_" :
            string.Join(",", b.foods.Select(f => ((int)f).ToString()))
        ).ToList();
        parts.Sort();
        return string.Join("|", parts);
    }
}
