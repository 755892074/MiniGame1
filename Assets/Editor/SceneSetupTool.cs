using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 一键创建 BootScene 和 MenuScene，并配置 BuildSettings
/// 菜单：Tools/铲屎官疯了/创建新场景(Boot+Menu)
/// </summary>
public class SceneSetupTool
{
    const string SCENE_DIR = "Assets/Scenes";

    [MenuItem("Tools/铲屎官疯了/创建新场景(Boot+Menu)")]
    static void CreateScenes()
    {
        if (!System.IO.Directory.Exists(SCENE_DIR))
            System.IO.Directory.CreateDirectory(SCENE_DIR);

        // 保存当前场景
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.isDirty)
            EditorSceneManager.SaveScene(currentScene);

        CreateBootScene();
        CreateMenuScene();

        // 更新 BuildSettings
        UpdateBuildSettings();

        EditorUtility.DisplayDialog("完成",
            "已创建/更新以下场景:\n\n" +
            "1. BootScene (index=0) — 启动场景\n" +
            "2. MenuScene (index=1) — 主菜单场景\n" +
            "3. PetGameScene (index=2) — 游戏场景(已有)\n\n" +
            "BuildSettings 已自动配置。\n" +
            "请在 BootScene 中点击 Play 开始测试。", "好的");

        // 打开 BootScene
        EditorSceneManager.OpenScene($"{SCENE_DIR}/BootScene.unity");
    }

    // ========================================
    // BootScene
    // ========================================
    static void CreateBootScene()
    {
        var path = $"{SCENE_DIR}/BootScene.unity";

        // 创建新场景
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Canvas
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = canvasGO.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(750, 1334);
        sc.matchWidthOrHeight = 1f;

        // Bootstrap 脚本挂在空 GameObject 上
        var bootGO = new GameObject("[Bootstrap]");
        bootGO.AddComponent<Bootstrap>();

        // 保存
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"<color=green>[SceneSetup] BootScene 已创建: {path}</color>");
    }

    // ========================================
    // MenuScene
    // ========================================
    static void CreateMenuScene()
    {
        var path = $"{SCENE_DIR}/MenuScene.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // MenuSceneController 挂在空 GameObject 上
        var menuGO = new GameObject("[MenuSceneController]");
        menuGO.AddComponent<MenuSceneController>();

        // 保存
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"<color=green>[SceneSetup] MenuScene 已创建: {path}</color>");
    }

    // ========================================
    // BuildSettings
    // ========================================
    static void UpdateBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

        // Boot
        string bootPath = $"{SCENE_DIR}/BootScene.unity";
        if (System.IO.File.Exists(bootPath))
            scenes.Add(new EditorBuildSettingsScene(bootPath, true));

        // Menu
        string menuPath = $"{SCENE_DIR}/MenuScene.unity";
        if (System.IO.File.Exists(menuPath))
            scenes.Add(new EditorBuildSettingsScene(menuPath, true));

        // Game (现有 PetGameScene)
        string gamePath = $"{SCENE_DIR}/PetGameScene.unity";
        if (System.IO.File.Exists(gamePath))
            scenes.Add(new EditorBuildSettingsScene(gamePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"<color=green>[SceneSetup] BuildSettings 已更新: {scenes.Count} 个场景</color>");
    }
}
