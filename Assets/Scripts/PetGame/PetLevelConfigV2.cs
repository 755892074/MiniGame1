using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// v2关卡配置 ScriptableObject
/// </summary>
[CreateAssetMenu(menuName = "铲屎官疯了/关卡配置v2")]
public class PetLevelConfigV2 : ScriptableObject
{
    public int levelId;
    public string levelName;
    public int bowlCapacity = 3;
    public int targetScore = 200;
    public int maxMoves = 0; // 0=无限
    public int difficulty; // 0=简单, 1=中等, 2=困难

    public PetType[] petQueue;
    public BowlInitData[] bowlInits;
}

[System.Serializable]
public struct BowlInitData
{
    public Vector2Int gridPos;
    public FoodType[] foodStack; // 底到顶
}
