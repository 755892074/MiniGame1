using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;

/// <summary>
/// 抖音小游戏一键自动化构建 (TTSDK 6.7.9 / Tuanjie 1.5+)
/// 所有 TTSDK API 通过反射调用，无编译时依赖
/// </summary>
public class AutoBuildDouyin
{
    const string OUT_DIR = "doc/douyin_package";

    [MenuItem("Tools/铲屎官疯了/抖音小游戏一键出包")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog("确认构建",
            "将清理旧包并重新构建抖音小游戏。\n耗时约 3-5 分钟。",
            "确定", "取消"))
            return;

        DoBuild();
    }

    public static void BuildFromCommandLine()
    {
        DoBuild();
    }

    // ---- helpers ----

    static System.Type FindType(string fullName)
    {
        foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = a.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }

    static object InvokeStatic(System.Type type, string methodName, object[] args = null)
    {
        var m = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        return m?.Invoke(null, args);
    }

    static void SetField(System.Type type, object obj, string name, object value)
    {
        var f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(obj, value);
    }

    static void SetProperty(System.Type type, object obj, string name, object value)
    {
        var p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null && p.CanWrite) p.SetValue(obj, value);
    }

    // ---- build ----

    static void DoBuild()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Debug.Log("[AutoBuild] ====== 开始抖音小游戏自动构建 ======");

        // 1. 切换平台到 MiniGame
        var mgTarget = (BuildTarget)36;   // MiniGame enum value
        var mgGroup = (BuildTargetGroup)34; // MiniGame enum value
        if (EditorUserBuildSettings.activeBuildTarget != mgTarget)
        {
            Debug.Log("[AutoBuild] 切换平台到 MiniGame...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(mgGroup, mgTarget);
        }

        // 2. Gamma
        if (PlayerSettings.colorSpace != ColorSpace.Gamma)
        {
            Debug.Log("[AutoBuild] ColorSpace -> Gamma");
            PlayerSettings.colorSpace = ColorSpace.Gamma;
        }

        // 3. 清理旧输出
        string fullOut = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OUT_DIR));
        if (Directory.Exists(fullOut))
        {
            Debug.Log("[AutoBuild] 清理旧输出...");
            Directory.Delete(fullOut, true);
        }

        // 4. 场景
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length == 0)
        {
            Debug.LogError("[AutoBuild] Build Settings 中没有场景！");
            EditorUtility.DisplayDialog("错误", "请先在 Build Settings 中添加场景。", "确定");
            return;
        }

        var scenePaths = new string[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            scenePaths[i] = scenes[i].path;

        // 5. 构建
        string error = null;
        try
        {
            // 先同步 StarkBuilderSettings
            SyncBuildProfileToStark();

            // 调 TTSDK BuildManager.BuildForTuanjie
            var bmType = FindType("TTSDK.Tool.API.BuildManager");
            if (bmType != null)
            {
                var method = bmType.GetMethod("BuildForTuanjie",
                    BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    // Load PlayerSettings as Object
                    object psObj = null;
                    var psType = FindType("UnityEditor.PlayerSettings");
                    if (psType != null)
                    {
                        var loadAtPath = typeof(AssetDatabase).GetMethod("LoadAssetAtPath",
                            new[] { typeof(string), typeof(System.Type) });
                        psObj = loadAtPath?.Invoke(null,
                            new object[] { "ProjectSettings/ProjectSettings.asset", psType });
                    }

                    var result = method.Invoke(null, new[] { fullOut, psObj });
                    if (result == null || string.IsNullOrEmpty(result.ToString()))
                    {
                        Debug.LogWarning("[AutoBuild] TTSDK 返回空，回退 BuildPipeline...");
                        BuildViaPipelineDirect(scenePaths, fullOut, mgTarget, mgGroup);
                    }
                    else
                    {
                        Debug.Log($"[AutoBuild] TTSDK 构建完成: {result}");
                    }
                }
                else
                {
                    Debug.LogWarning("[AutoBuild] BuildForTuanjie 不存在，回退 BuildPipeline...");
                    BuildViaPipelineDirect(scenePaths, fullOut, mgTarget, mgGroup);
                }
            }
            else
            {
                Debug.LogWarning("[AutoBuild] BuildManager 不存在，回退 BuildPipeline...");
                BuildViaPipelineDirect(scenePaths, fullOut, mgTarget, mgGroup);
            }
        }
        catch (System.Exception e)
        {
            error = e.ToString();
        }

        sw.Stop();

        if (error == null)
        {
            Debug.Log($"[AutoBuild] ====== 构建成功！耗时 {sw.Elapsed.TotalSeconds:F0}s ======");
            EditorUtility.RevealInFinder(fullOut);
            EditorUtility.DisplayDialog("构建成功",
                $"抖音小游戏工程已生成:\n{fullOut}\n\n请用抖音开发者工具打开此目录。",
                "确定");
        }
        else
        {
            Debug.LogError($"[AutoBuild] ====== 构建失败 ({sw.Elapsed.TotalSeconds:F0}s) ======\n{error}");
            EditorUtility.DisplayDialog("构建失败", error, "确定");
        }
    }

    static void BuildViaPipelineDirect(string[] scenes, string outPath, BuildTarget target, BuildTargetGroup group)
    {
        Debug.Log("[AutoBuild] 使用 BuildPipeline.BuildPlayer 直构...");
        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outPath,
            target = target,
            targetGroup = group,
            options = BuildOptions.None
        };

        var reportType = FindType("UnityEditor.Build.Reporting.BuildReport");
        // BuildPipeline.BuildPlayer returns BuildReport
        var bpType = typeof(BuildPipeline);
        var buildMethod = bpType.GetMethod("BuildPlayer",
            new[] { typeof(BuildPlayerOptions) });
        if (buildMethod == null)
        {
            throw new System.Exception("找不到 BuildPipeline.BuildPlayer(BuildPlayerOptions)");
        }

        var report = buildMethod.Invoke(null, new object[] { opts });
        if (report == null)
        {
            throw new System.Exception("BuildPlayer 返回 null");
        }

        // report.summary.result
        var summaryProp = report.GetType().GetProperty("summary");
        if (summaryProp == null)
            throw new System.Exception("BuildReport 无 summary 属性");

        var summary = summaryProp.GetValue(report);
        var resultProp = summary.GetType().GetProperty("result");
        var result = resultProp.GetValue(summary);
        var resultStr = result.ToString();

        if (resultStr != "Succeeded")
        {
            var errProp = summary.GetType().GetProperty("totalErrors");
            var errs = errProp?.GetValue(summary);
            throw new System.Exception($"Build failed: {resultStr} errors={errs}");
        }

        Debug.Log("[AutoBuild] BuildPipeline 构建完成");
    }

    static void SyncBuildProfileToStark()
    {
        // 找 DouYin BuildProfile
        var guids = AssetDatabase.FindAssets("t:ScriptableObject");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath(path, typeof(ScriptableObject));
            if (obj == null) continue;

            var type = obj.GetType();
            if (type.Name != "BuildProfile") continue;

            var platformId = type.GetProperty("platformId");
            if (platformId == null) continue;

            var platform = platformId.GetValue(obj) as string;
            if (platform == null || (!platform.ToLower().Contains("douyin") && !platform.ToLower().Contains("dou_yin")))
                continue;

            Debug.Log($"[AutoBuild] 找到 DouYin BuildProfile: {path}");

            // 调用 DouYinSubplatformInterface.SyncBuildSettingsToStark
            var subType = FindType("TTSDK.Tool.DouYinSubplatformInterface");
            if (subType == null) break;

            var syncMethod = subType.GetMethod("SyncBuildSettingsToStark",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (syncMethod == null) break;

            var miniSettings = type.GetProperty("miniGameSettings")?.GetValue(obj);
            if (miniSettings == null) break;

            syncMethod.Invoke(null, new[] { miniSettings });
            Debug.Log("[AutoBuild] StarkBuilderSettings 已从 BuildProfile 同步");
            return;
        }

        Debug.Log("[AutoBuild] 未找到 DouYin BuildProfile，使用现有 StarkBuilderSettings");
    }
}
