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

    [Header("生成元数据（离线工具填写，运行时只读）")]
    public int minSteps;        // BFS 最少步数解
    public int seed;            // 随机种子
    public string version = ""; // 生成版本号

    public PetType[] petQueue;
    public BowlInitData[] bowlInits;
}

[System.Serializable]
public struct BowlInitData
{
    public Vector2Int gridPos;
    public FoodType[] foodStack; // 底到顶
}
