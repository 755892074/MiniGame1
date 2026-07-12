using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#region 枚举
public enum PetType
{
    Cat = 0, Dog = 1, Hamster = 2, Parrot = 3, Fish = 4, Rabbit = 5,
}

public enum FoodType
{
    CannedCatFood = 0, CatKibble = 1, DriedFish = 2, CatTreatStick = 3, Catnip = 4, Milk = 5,
    DogKibble = 6, BoneTreat = 7, MeatJerky = 8, DentalChew = 9, DogBiscuit = 10, Sausage = 11,
    SunflowerSeed = 12, Corn = 13, Mealworm = 14,
    Millet = 15, Cuttlebone = 16, SeedBag = 17,
    FishFlake = 18, Bloodworm = 19, AlgaeWafer = 20,
    Carrot = 21, Hay = 22,
    Apple = 23, Pellet = 24, PeanutButter = 25, TunaChunk = 26, SalmonSlice = 27, TreatBall = 28,
}

public enum PetMood
{
    Waiting, MyTurn, Eating, WrongFood, Full, Skipped, Bored,
}
#endregion

#region 碗
/// <summary>碗：食物栈（index 0=底, Count-1=顶）</summary>
[Serializable]
public class Bowl
{
    public int bowlId;
    public int capacity = 3;
    public List<FoodType> foods = new List<FoodType>();
    public bool isCompleted;
    public Vector2Int gridPos;

    public bool IsEmpty => foods.Count == 0;
    public bool IsFull => foods.Count >= capacity;
    public bool IsComplete => IsFull && foods.All(f => f == foods[0]);
    public FoodType? Top => foods.Count > 0 ? foods[foods.Count - 1] : (FoodType?)null;

    public void Push(FoodType f) { foods.Add(f); }
    public FoodType Pop() { var f = foods[foods.Count - 1]; foods.RemoveAt(foods.Count - 1); return f; }

    public Bowl Clone()
    {
        return new Bowl { bowlId = bowlId, capacity = capacity, foods = new List<FoodType>(foods), isCompleted = isCompleted, gridPos = gridPos };
    }
}
#endregion

#region 操作结果
public struct PourResult
{
    public bool success;
    public bool bowlCompleted;
    public string reason;
}
#endregion

#region 计分
public static class ScoreConfig
{
    public const int MatchFirst = 100;
    public const int MatchSecond = 60;
    public const int MatchThird = 30;
    public const int ComboBonus = 20;
}

/// <summary>食物→宠物映射</summary>
public static class FoodPetMap
{
    public static PetType GetPet(FoodType f)
    {
        int v = (int)f;
        if (v <= 5) return PetType.Cat;
        if (v <= 11) return PetType.Dog;
        if (v <= 14) return PetType.Hamster;
        if (v <= 17) return PetType.Parrot;
        if (v <= 20) return PetType.Fish;
        if (v <= 22) return PetType.Rabbit;
        return PetType.Cat;
    }

    /// <summary>宠物→食物（严格一对一映射）</summary>
    public static FoodType GetFoodForPet(PetType p)
    {
        return p switch
        {
            PetType.Cat => FoodType.DriedFish,
            PetType.Dog => FoodType.BoneTreat,
            PetType.Hamster => FoodType.SunflowerSeed,
            PetType.Parrot => FoodType.Millet,
            PetType.Fish => FoodType.FishFlake,
            PetType.Rabbit => FoodType.Carrot,
            _ => FoodType.Apple,
        };
    }
}
#endregion
