using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换管理器 — 管理三个场景的加载和跳转
/// BootScene(0) → MenuScene(1) → PetGameScene(2)
/// </summary>
public static class GameSceneManager
{
    public const string BOOT = "BootScene";
    public const string MENU = "MenuScene";
    public const string GAME = "PetGameScene";

    /// <summary>标记：PetGameScene 加载后要自动开始哪一关。-1=不自动开始，交给 GameManager</summary>
    public static int pendingLevelId = -1;

    /// <summary>加载启动场景</summary>
    public static void LoadBoot()
    {
        Debug.Log("[GameSceneManager] → BootScene");
        SceneManager.LoadScene(BOOT);
    }

    /// <summary>加载主菜单场景（带防重入：若已是当前场景则跳过，避免重复重载把正在异步加载的 UI 干掉）</summary>
    public static void LoadMenu()
    {
        if (SceneManager.GetActiveScene().name == MENU)
        {
            Debug.LogWarning("[GameSceneManager] LoadMenu 跳过：当前已是 MenuScene");
            return;
        }
        Debug.Log("[GameSceneManager] → MenuScene");
        SceneManager.LoadScene(MENU);
    }

    /// <summary>加载游戏场景，可选指定关卡</summary>
    public static void LoadGame(int levelId = -1)
    {
        pendingLevelId = levelId;
        Debug.Log($"[GameSceneManager] → PetGameScene (关卡={levelId})");
        SceneManager.LoadScene(GAME);
    }

    /// <summary>当前场景名</summary>
    public static string CurrentScene => SceneManager.GetActiveScene().name;

    /// <summary>是否在游戏场景中</summary>
    public static bool IsInGame => CurrentScene == GAME;
}
