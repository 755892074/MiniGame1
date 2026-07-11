using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宠物游戏 Sprite 配置表 — 集中管理所有美术资源引用
/// 运行时通过此表获取 Sprite，不改代码只改配置
/// </summary>
[CreateAssetMenu(fileName = "PetGameSpriteConfig", menuName = "铲屎官疯了/图片配置表")]
public class PetGameSpriteConfig : ScriptableObject
{
    [Header("=== 宠物表情（6宠 × 7种情绪）===")]
    public PetExpressionSet[] petExpressions;

    [Header("=== 宠物碗（空碗 + 满碗）===")]
    public BowlSpriteSet[] emptyBowls;
    public BowlSpriteSet[] fullBowls;

    [Header("=== 食物物品 ===")]
    public FoodSpriteEntry[] foods;

    [Header("=== UI元素 ===")]
    public Sprite btnPlay;
    public Sprite btnLevelSelect;
    public Sprite btnHint;
    public Sprite btnPause;
    public Sprite btnUndo;
    public Sprite btnRestart;
    public Sprite progressBarBg;
    public Sprite progressBarFill;
    public Sprite starIcon;
    public Sprite heartIcon;
    public Sprite gameLogo;

    // 快速查找字典（运行时构建）
    private Dictionary<PetType, PetExpressionSet> _petExprDict;
    private Dictionary<PetType, BowlSpriteSet> _emptyBowlDict;
    private Dictionary<PetType, BowlSpriteSet> _fullBowlDict;
    private Dictionary<FoodType, Sprite> _foodDict;

    void OnEnable()
    {
        BuildDicts();
    }

    void BuildDicts()
    {
        _petExprDict = new Dictionary<PetType, PetExpressionSet>();
        if (petExpressions != null)
            foreach (var e in petExpressions)
                if (!_petExprDict.ContainsKey(e.petType))
                    _petExprDict[e.petType] = e;

        _emptyBowlDict = new Dictionary<PetType, BowlSpriteSet>();
        if (emptyBowls != null)
            foreach (var b in emptyBowls)
                if (!_emptyBowlDict.ContainsKey(b.petType))
                    _emptyBowlDict[b.petType] = b;

        _fullBowlDict = new Dictionary<PetType, BowlSpriteSet>();
        if (fullBowls != null)
            foreach (var b in fullBowls)
                if (!_fullBowlDict.ContainsKey(b.petType))
                    _fullBowlDict[b.petType] = b;

        _foodDict = new Dictionary<FoodType, Sprite>();
        if (foods != null)
            foreach (var f in foods)
                if (!_foodDict.ContainsKey(f.foodType))
                    _foodDict[f.foodType] = f.sprite;
    }

    void EnsureDicts()
    {
        if (_petExprDict == null || _petExprDict.Count == 0) BuildDicts();
    }

    public Sprite GetPetExpression(PetType pet, PetMood mood)
    {
        EnsureDicts();
        if (_petExprDict.TryGetValue(pet, out var set))
            return set.GetExpression(mood);
        return null;
    }

    public Sprite GetEmptyBowl(PetType pet)
    {
        EnsureDicts();
        return _emptyBowlDict.TryGetValue(pet, out var b) ? b.bowlSprite : null;
    }

    public Sprite GetFullBowl(PetType pet)
    {
        EnsureDicts();
        return _fullBowlDict.TryGetValue(pet, out var b) ? b.bowlSprite : null;
    }

    public Sprite GetFoodSprite(FoodType food)
    {
        EnsureDicts();
        if (_foodDict.TryGetValue(food, out var s)) return s;
        // 回退：同宠物用同一Sprite
        var pet = FoodPetMap.GetPet(food);
        foreach (var kv in _foodDict)
            if (FoodPetMap.GetPet(kv.Key) == pet)
                return kv.Value;
        return null;
    }

    public Sprite GetTempBowlEmpty() => emptyBowls != null && emptyBowls.Length > 0 ? emptyBowls[emptyBowls.Length - 1].bowlSprite : null;
    public Sprite GetTempBowlPartial() => fullBowls != null && fullBowls.Length > 0 ? fullBowls[fullBowls.Length - 1].bowlSprite : null;
}

[Serializable]
public class PetExpressionSet
{
    public PetType petType;
    public string petName;
    [Header("情绪 0=Waiting 1=MyTurn 2=Eating 3=WrongFood 4=Full 5=Skipped 6=Bored")]
    public Sprite waiting;    // 😶 排队等
    public Sprite myTurn;     // 😋 该我了
    public Sprite eating;     // 😍 正在吃
    public Sprite wrongFood;  // 🤮 给错了
    public Sprite full;       // 😇 饱了
    public Sprite skipped;    // 😡 被插队
    public Sprite bored;      // 🙄 等太久

    public Sprite GetExpression(PetMood mood)
    {
        switch (mood)
        {
            case PetMood.Waiting: return waiting;
            case PetMood.MyTurn: return myTurn;
            case PetMood.Eating: return eating;
            case PetMood.WrongFood: return wrongFood;
            case PetMood.Full: return full;
            case PetMood.Skipped: return skipped;
            case PetMood.Bored: return bored;
            default: return waiting;
        }
    }

    public Sprite[] AllExpressions => new[] { waiting, myTurn, eating, wrongFood, full, skipped, bored };
}

[Serializable]
public class BowlSpriteSet
{
    public PetType petType;
    public string label;
    public Sprite bowlSprite;
}

[Serializable]
public class FoodSpriteEntry
{
    public FoodType foodType;
    public string foodName;
    public Sprite sprite;
}
