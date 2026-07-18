using UnityEditor;

/// <summary>
/// 一键串起所有预制体/场景生成步骤，避免手动跑 3 次菜单。
/// 顺序：界面预制体(v3) → UI预制体(v2) → 搭建游戏场景。
/// </summary>
public class GenerateAllV3
{
    [MenuItem("铲屎官疯了/一键生成全部(v3)")]
    static void GenerateAll()
    {
        EditorApplication.ExecuteMenuItem("铲屎官疯了/生成新界面预制体(v3)");
        EditorApplication.ExecuteMenuItem("铲屎官疯了/生成UI预制体(v2)");
        EditorApplication.ExecuteMenuItem("铲屎官疯了/搭建游戏场景");
        UnityEngine.Debug.Log("[GenerateAllV3] 一键生成全部完成：界面预制体 + UI预制体 + 游戏场景");
    }
}
