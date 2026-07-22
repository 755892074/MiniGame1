// ============================================================
// UIVisualInspector.cs
// 铲屎官疯了 - UI视觉验收自动截图工具
// 使用方式：菜单栏 → 铲屎官疯了 → UI视觉验收 → 执行完整截图
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIVisualInspector : EditorWindow
{
    // ----------------------- 配置 -----------------------
    const string OutputRoot = "../tools/ui_inspection/screenshots";
    const int CaptureWidth = 750;
    const int CaptureHeight = 1334;

    // 需要截图的场景列表
    static readonly string[] ScenePaths = new string[]
    {
        "Assets/Scenes/BootScene.unity",
        "Assets/Scenes/MenuScene.unity",
        "Assets/Scenes/PetGameScene.unity",
    };

    // MenuScene 中的面板状态机（通过 MenuSceneController 的方法名）
    static readonly string[] MenuPanelStates = new string[]
    {
        "LoginPanel",
        "MainMenuPanel",
        "SettingsPanel",
        "LevelSelectPanel",
    };

    // PetGameScene 中的游戏状态
    static readonly string[] GameStates = new string[]
    {
        "Gameplay",
        "Paused",
        "GameOver_Win",
        "GameOver_Lose",
    };

    // 当前执行的进度
    static int currentStep = 0;
    static int totalSteps = 0;
    static string currentAction = "";
    static bool isCapturing = false;

    [MenuItem("铲屎官疯了/UI视觉验收/执行完整截图 %&u")]
    static void RunFullCapture()
    {
        if (isCapturing)
        {
            EditorUtility.DisplayDialog("提示", "截图正在进行中，请等待完成", "确定");
            return;
        }
        isCapturing = true;
        currentStep = 0;

        // 计算总步数
        totalSteps = ScenePaths.Length + MenuPanelStates.Length + GameStates.Length + 2; // +2 for summary

        string outputDir = Path.Combine(Application.dataPath, OutputRoot);
        Directory.CreateDirectory(outputDir);
        CleanDirectory(outputDir);

        EditorCoroutine.Start(CaptureAll(outputDir));
    }

    [MenuItem("铲屎官疯了/UI视觉验收/仅截图当前场景")]
    static void CaptureCurrentScene()
    {
        string outputDir = Path.Combine(Application.dataPath, OutputRoot, "current");
        Directory.CreateDirectory(outputDir);
        string path = Path.Combine(outputDir, $"{SceneManager.GetActiveScene().name}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        CaptureScreenshot(path);
        Debug.Log($"[UIVisualInspector] 当前场景截图已保存: {path}");
        EditorUtility.DisplayDialog("截图完成", $"已保存到:\n{path}", "确定");
    }

    [MenuItem("铲屎官疯了/UI视觉验收/打开截图文件夹")]
    static void OpenScreenshotFolder()
    {
        string outputDir = Path.Combine(Application.dataPath, OutputRoot);
        Directory.CreateDirectory(outputDir);
        EditorUtility.RevealInFinder(outputDir);
    }

    // ============================================================
    // 核心截图逻辑
    // ============================================================

    static IEnumerator CaptureAll(string outputDir)
    {
        Debug.Log("[UIVisualInspector] ========== 开始 UI 视觉验收截图 ==========");

        // 1. 截图静态场景（BootScene, MenuScene, PetGameScene）
        foreach (var scenePath in ScenePaths)
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "..", scenePath))) continue;

            currentAction = $"打开场景: {Path.GetFileNameWithoutExtension(scenePath)}";
            UpdateProgress();

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            yield return WaitForSeconds(1.0f); // 等待场景加载

            // 等待一帧确保UI渲染完成
            yield return null;

            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            string path = Path.Combine(outputDir, $"scene_{sceneName}.png");
            CaptureScreenshot(path);
            Debug.Log($"[UIVisualInspector] 截图: {path}");

            currentStep++;
        }

        // 2. MenuScene 各面板截图（通过反射调用MenuSceneController的方法）
        if (File.Exists(Path.Combine(Application.dataPath, "..", "Assets/Scenes/MenuScene.unity")))
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity", OpenSceneMode.Single);
            yield return WaitForSeconds(1.0f);

            foreach (var panel in MenuPanelStates)
            {
                currentAction = $"MenuScene 面板: {panel}";
                UpdateProgress();

                // 通过反射调用显示方法
                var controller = FindObjectOfType<MenuSceneController>();
                if (controller != null)
                {
                    InvokeShowMethod(controller, panel);
                    yield return WaitForSeconds(0.8f);
                }

                string path = Path.Combine(outputDir, $"menu_{panel}.png");
                CaptureScreenshot(path);
                Debug.Log($"[UIVisualInspector] 截图: {path}");

                currentStep++;
            }
        }

        // 3. PetGameScene 各状态截图
        if (File.Exists(Path.Combine(Application.dataPath, "..", "Assets/Scenes/PetGameScene.unity")))
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PetGameScene.unity", OpenSceneMode.Single);
            yield return WaitForSeconds(1.5f);

            foreach (var state in GameStates)
            {
                currentAction = $"游戏状态: {state}";
                UpdateProgress();

                // 尝试设置游戏状态
                SimulateGameState(state);
                yield return WaitForSeconds(1.0f);

                string path = Path.Combine(outputDir, $"game_{state}.png");
                CaptureScreenshot(path);
                Debug.Log($"[UIVisualInspector] 截图: {path}");

                currentStep++;
            }
        }

        // 4. 生成元数据JSON
        currentAction = "生成元数据";
        UpdateProgress();
        GenerateMetadata(outputDir);
        currentStep++;

        // 5. 完成
        currentAction = "完成";
        UpdateProgress();
        isCapturing = false;

        string msg = $"UI视觉验收截图完成！\n共 {totalSteps} 张截图\n保存位置: {outputDir}";
        Debug.Log($"[UIVisualInspector] {msg}");
        EditorUtility.DisplayDialog("UI视觉验收", msg, "确定");

        // 打开文件夹
        EditorUtility.RevealInFinder(outputDir);
    }

    // 通过反射调用 MenuSceneController 的显示方法
    static void InvokeShowMethod(MenuSceneController controller, string panelName)
    {
        try
        {
            var method = controller.GetType().GetMethod($"Show{panelName}");
            if (method != null)
            {
                method.Invoke(controller, null);
            }
            else
            {
                // 尝试其他命名
                method = controller.GetType().GetMethod(panelName);
                if (method != null) method.Invoke(controller, null);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[UIVisualInspector] 调用 {panelName} 失败: {e.Message}");
        }
    }

    // 模拟游戏状态
    static void SimulateGameState(string state)
    {
        // 查找 PetGameManager
        var mgr = FindObjectOfType<PetGameManager>();
        if (mgr == null) return;

        switch (state)
        {
            case "Paused":
                // 查找暂停按钮或触发暂停
                var pauseBtn = FindUIObjectByName("PauseButton", "BtnPause", "PauseBtn");
                if (pauseBtn != null)
                {
                    var btn = pauseBtn.GetComponent<Button>();
                    if (btn != null) btn.onClick?.Invoke();
                }
                break;

            case "GameOver_Win":
                // 尝试通过反射触发胜利
                TryInvokeMethod(mgr, "OnLevelComplete");
                break;

            case "GameOver_Lose":
                TryInvokeMethod(mgr, "OnLevelFailed");
                break;
        }
    }

    static GameObject FindUIObjectByName(params string[] names)
    {
        foreach (var name in names)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;
        }
        return null;
    }

    static void TryInvokeMethod(object target, string methodName)
    {
        try
        {
            var method = target.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            if (method != null) method.Invoke(target, null);
        }
        catch { /* ignore */ }
    }

    // 实际截图方法
    static void CaptureScreenshot(string savePath)
    {
        // 使用 GameView 截图
        var gameView = GetGameView();
        if (gameView == null)
        {
            Debug.LogWarning("[UIVisualInspector] 无法获取 GameView，使用 ScreenCapture");
            ScreenCapture.CaptureScreenshot(savePath);
            return;
        }

        // 设置 GameView 分辨率
        SetGameViewResolution(CaptureWidth, CaptureHeight);

        // 使用 Unity 的 ScreenCapture API
        ScreenCapture.CaptureScreenshot(savePath);

        // 确保文件写入完成
        int retries = 0;
        while (!File.Exists(savePath) && retries < 60)
        {
            System.Threading.Thread.Sleep(100);
            retries++;
        }
    }

    static EditorWindow GetGameView()
    {
        var assembly = typeof(Editor).Assembly;
        var type = assembly.GetType("UnityEditor.GameView");
        if (type != null)
        {
            return EditorWindow.GetWindow(type);
        }
        return null;
    }

    static void SetGameViewResolution(int width, int height)
    {
        // 通过反射设置 GameView 大小
        var assembly = typeof(Editor).Assembly;
        var gameViewType = assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null) return;

        var gameView = EditorWindow.GetWindow(gameViewType);
        if (gameView == null) return;

        // 设置 GameView 的尺寸
        var rect = gameView.position;
        rect.width = width + 32;  // 加上边框
        rect.height = height + 56;
        gameView.position = rect;
    }

    static void GenerateMetadata(string outputDir)
    {
        var meta = new System.Text.StringBuilder();
        meta.AppendLine("{");
        meta.AppendLine($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
        meta.AppendLine($"  \"unityVersion\": \"{Application.unityVersion}\",");
        meta.AppendLine($"  \"platform\": \"{Application.platform}\",");
        meta.AppendLine($"  \"screenWidth\": {CaptureWidth},");
        meta.AppendLine($"  \"screenHeight\": {CaptureHeight},");
        meta.AppendLine("  \"screenshots\": [");

        var files = Directory.GetFiles(outputDir, "*.png");
        for (int i = 0; i < files.Length; i++)
        {
            var fi = new FileInfo(files[i]);
            string comma = (i < files.Length - 1) ? "," : "";
            meta.AppendLine($"    {{\"name\": \"{Path.GetFileNameWithoutExtension(files[i])}\", \"path\": \"{files[i]}\", \"size\": {fi.Length}}}{comma}");
        }

        meta.AppendLine("  ]");
        meta.AppendLine("}");

        File.WriteAllText(Path.Combine(outputDir, "metadata.json"), meta.ToString());
    }

    static void CleanDirectory(string dir)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir, "*.png"))
        {
            File.Delete(file);
        }
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            File.Delete(file);
        }
    }

    static void UpdateProgress()
    {
        float progress = (float)currentStep / totalSteps;
        EditorUtility.DisplayProgressBar("UI视觉验收", $"{currentAction} ({currentStep}/{totalSteps})", progress);
    }

    // 简单的协程实现（Editor模式下）
    public class EditorCoroutine
    {
        IEnumerator routine;
        EditorCoroutine(IEnumerator routine) { this.routine = routine; }

        public static EditorCoroutine Start(IEnumerator routine)
        {
            var coroutine = new EditorCoroutine(routine);
            coroutine.Start();
            return coroutine;
        }

        void Start()
        {
            EditorApplication.update += Update;
        }

        void Update()
        {
            if (!routine.MoveNext())
            {
                EditorApplication.update -= Update;
            }
        }
    }

    static IEnumerator WaitForSeconds(float seconds)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup < start + seconds)
        {
            yield return null;
        }
    }
}
