using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 食物堆（栈结构）— 只能从顶部取食物
/// </summary>
public class FoodStack
{
    private List<FoodType> stack = new List<FoodType>();
    
    public int Count => stack.Count;
    public bool IsEmpty => stack.Count == 0;
    
    /// <summary>查看顶部食物（不取出）</summary>
    public FoodType Peek()
    {
        return stack[stack.Count - 1];
    }
    
    /// <summary>取出顶部食物</summary>
    public FoodType Pop()
    {
        var food = stack[stack.Count - 1];
        stack.RemoveAt(stack.Count - 1);
        return food;
    }
    
    /// <summary>放入食物到顶部</summary>
    public void Push(FoodType food)
    {
        stack.Add(food);
    }
    
    /// <summary>获取所有食物列表（用于显示）</summary>
    public List<FoodType> GetAll()
    {
        return new List<FoodType>(stack);
    }
    
    /// <summary>初始化食物堆</summary>
    public void Init(List<FoodType> foods)
    {
        stack = new List<FoodType>(foods);
    }
    
    /// <summary>克隆</summary>
    public FoodStack Clone()
    {
        var fs = new FoodStack();
        fs.stack = new List<FoodType>(stack);
        return fs;
    }
    
    /// <summary>生成状态哈希（用于DFS去重）</summary>
    public string GetStateHash()
    {
        return string.Join(",", stack);
    }
}

/// <summary>
/// 宠物排队系统
/// </summary>
public class PetQueue
{
    private Queue<PetType> queue = new Queue<PetType>();
    private PetType? currentPet = null;
    
    public int Count => queue.Count;
    public bool IsEmpty => queue.Count == 0 && currentPet == null;
    public PetType? CurrentPet => currentPet;
    
    /// <summary>加入排队</summary>
    public void Enqueue(PetType pet)
    {
        queue.Enqueue(pet);
        if (currentPet == null)
        {
            currentPet = queue.Dequeue();
        }
    }
    
    /// <summary>当前宠物吃饱离开，下一个上前</summary>
    public PetType? Next()
    {
        if (queue.Count > 0)
        {
            currentPet = queue.Dequeue();
            return currentPet;
        }
        currentPet = null;
        return null;
    }
    
    /// <summary>获取所有排队的宠物</summary>
    public List<PetType> GetWaitingPets()
    {
        var list = new List<PetType>(queue);
        if (currentPet != null)
            list.Insert(0, currentPet.Value);
        return list;
    }
    
    public PetQueue Clone()
    {
        var pq = new PetQueue();
        pq.queue = new Queue<PetType>(queue);
        pq.currentPet = currentPet;
        return pq;
    }

    public string GetStateHash()
    {
        var parts = new List<string>();
        if (currentPet != null) parts.Add($"C{(int)currentPet}");
        foreach (var p in queue) parts.Add($"{(int)p}");
        return string.Join(",", parts);
    }
}

/// <summary>
/// 暂存碗 — 用于临时存放不匹配的食物
/// </summary>
public class TempBowl
{
    public int capacity = 4;
    private List<FoodType> foods = new List<FoodType>();
    
    public int Count => foods.Count;
    public bool IsEmpty => foods.Count == 0;
    public bool IsFull => foods.Count >= capacity;
    
    public FoodType Peek()
    {
        return foods[foods.Count - 1];
    }
    
    public FoodType Pop()
    {
        var f = foods[foods.Count - 1];
        foods.RemoveAt(foods.Count - 1);
        return f;
    }
    
    public void Push(FoodType food)
    {
        if (!IsFull) foods.Add(food);
    }
    
    public List<FoodType> GetAll() => new List<FoodType>(foods);
    
    public TempBowl Clone()
    {
        var tb = new TempBowl { capacity = capacity };
        tb.foods = new List<FoodType>(foods);
        return tb;
    }
    
    public string GetStateHash()
    {
        return string.Join(",", foods);
    }
}
