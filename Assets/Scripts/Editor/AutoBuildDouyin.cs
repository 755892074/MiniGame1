using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Diagnostics;

/// <summary>
/// 抖音小游戏一键自动化构建
/// 菜单: Tools/铲屎官疯了/抖音小游戏一键出包
/// Batch mode: -executeMethod AutoBuildDouyin.BuildFromCommandLine
/// </summary>
public class AutoBuildDouyin
{
    const string APP_ID = "ttf09486296ef1dfbc07";
    const string OUT_DIR = "doc/douyin_package";
    const string PROJECT_NAME = "minigame-1";

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

    static void DoBuild()
    {
        var sw = Stopwatch.StartNew();
        UnityEngine.Debug.Log("[AutoBuild] ====== 开始抖音小游戏自动构建 ======");
        UnityEngine.Debug.Log("[AutoBuild] AppID: " + APP_ID);
        UnityEngine.Debug.Log("[AutoBuild] 输出: " + OUT_DIR);

        // 1. 清理旧输出
        string fullOut = Path.Combine(Application.dataPath, "..", OUT_DIR);
        if (Directory.Exists(fullOut))
        {
            UnityEngine.Debug.Log("[AutoBuild] 清理旧输出...");
            Directory.Delete(fullOut, true);
        }
        Directory.CreateDirectory(fullOut);

        // 2. 确保 ColorSpace = Gamma
        if (PlayerSettings.colorSpace != ColorSpace.Gamma)
        {
            UnityEngine.Debug.Log("[AutoBuild] 切换 ColorSpace → Gamma");
            PlayerSettings.colorSpace = ColorSpace.Gamma;
        }

        // 3. 通过反射调 TTSDK Builder
        bool ok = false;
        string err = "";

        try
        {
            var asm = System.AppDomain.CurrentDomain.GetAssemblies();
            System.Type settingsType = null;
            foreach (var a in asm)
            {
                var t = a.GetType("TTSDK.Tool.StarkBuilderSettings");
                if (t != null) { settingsType = t; break; }
            }

            if (settingsType == null)
            {
                UnityEngine.Debug.LogError("[AutoBuild] 找不到 StarkBuilderSettings 类型！TTSDK 未安装？");
                EditorUtility.DisplayDialog("错误", "找不到 TTSDK！请确认 ByteGame 已导入。", "确定");
                return;
            }

            // 从已保存的配置加载
            var existingSettings = AssetDatabase.LoadAssetAtPath<Object>(
                "Assets/Editor/StarkBuilderSetting.asset");
            var settings = existingSettings != null
                ? existingSettings
                : ScriptableObject.CreateInstance(settingsType);

            // 设置必填参数
            var st = settings.GetType();
            SetField(st, settings, "OutputDir",
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", OUT_DIR)));
            SetField(st, settings, "needCompress", true);
            SetField(st, settings, "isWebGL2", false);
            SetField(st, settings, "wasmMemorySize", 128);

            // AppID
            var appIdProp = st.GetProperty("appId");
            if (appIdProp != null)
                appIdProp.SetValue(settings, APP_ID);

            // 游戏方向: Portrait
            var orientProp = st.GetProperty("orientation");
            if (orientProp != null)
            {
                var orientEnum = System.Enum.ToObject(orientProp.PropertyType, 0); // Portrait=0
                orientProp.SetValue(settings, orientEnum);
            }

            // WasmSubFramework: WebGL
            var subFwField = st.GetField("wasmSubFramework",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (subFwField != null)
            {
                var subFwEnum = System.Enum.ToObject(subFwField.FieldType, 0); // WebGL=0
                subFwField.SetValue(settings, subFwEnum);
            }

            // Framework: Wasm
            var fwField = st.GetField("framework",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (fwField != null)
            {
                var fwEnum = System.Enum.ToObject(fwField.FieldType, 1); // Wasm=1
                fwField.SetValue(settings, fwEnum);
            }

            // 调用 Builder.Build
            System.Type builderType = null;
            foreach (var a in asm)
            {
                var t = a.GetType("TTSDK.Tool.StarkBuilder");
                if (t != null) { builderType = t; break; }
            }

            if (builderType == null)
            {
                UnityEngine.Debug.LogError("[AutoBuild] 找不到 StarkBuilder！");
                return;
            }

            var buildMethod = builderType.GetMethod("Build",
                BindingFlags.Public | BindingFlags.Static);
            if (buildMethod != null)
            {
                var args = new object[] { settings, false, "", false };
                buildMethod.Invoke(null, args);
                ok = (bool)args[1];
                err = (string)args[2];
            }
            else
            {
                UnityEngine.Debug.LogError("[AutoBuild] 找不到 Build 方法！");
            }

            // 保存设置
            if (existingSettings == null)
            {
                AssetDatabase.CreateAsset(settings, "Assets/Editor/StarkBuilderSetting.asset");
            }
            AssetDatabase.SaveAssets();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("[AutoBuild] 异常: " + e);
            err = e.Message;
        }

        sw.Stop();

        if (ok)
        {
            UnityEngine.Debug.Log($"[AutoBuild] ====== 构建成功！耗时 {sw.Elapsed.TotalSeconds:F0}s ======");

            // 4. 后处理：解压 zip → tt-minigame
            PostProcess(fullOut);
        }
        else
        {
            UnityEngine.Debug.LogError($"[AutoBuild] ====== 构建失败: {err} 耗时 {sw.Elapsed.TotalSeconds:F0}s ======");
            EditorUtility.DisplayDialog("构建失败", err, "确定");
        }
    }

    static void PostProcess(string outDir)
    {
        UnityEngine.Debug.Log("[AutoBuild] 后处理: 解压 zip + 补配置文件...");

        // 找到最新 zip
        var zipFiles = Directory.GetFiles(outDir, "webgl_package-*.zip");
        if (zipFiles.Length == 0)
        {
            UnityEngine.Debug.LogWarning("[AutoBuild] 未找到 webgl_package-*.zip");
            return;
        }

        var latest = zipFiles[zipFiles.Length - 1];
        UnityEngine.Debug.Log("[AutoBuild] 解压: " + Path.GetFileName(latest));

        string ttDir = Path.Combine(outDir, "tt-minigame");
        if (Directory.Exists(ttDir)) Directory.Delete(ttDir, true);
        Directory.CreateDirectory(ttDir);

        // 用 System.IO.Compression 解压
        try
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(latest, ttDir);

            // 补 game.json
            string gameJson = "{\"deviceOrientation\":\"portrait\",\"showStatusBar\":false," +
                "\"networkTimeout\":{\"request\":10000,\"connectSocket\":10000,\"uploadFile\":10000,\"downloadFile\":10000}," +
                "\"subpackages\":[]}";
            File.WriteAllText(Path.Combine(ttDir, "game.json"), gameJson);

            // 补 project.config.json
            string projJson = $"{{\"appid\":\"{APP_ID}\",\"projectname\":\"{PROJECT_NAME}\"," +
                "\"setting\":{\"urlCheck\":true,\"es6\":true,\"postcss\":false,\"minified\":false}}";
            File.WriteAllText(Path.Combine(ttDir, "project.config.json"), projJson);

            UnityEngine.Debug.Log($"[AutoBuild] tt-minigame 工程已就绪: {ttDir}");
            UnityEngine.Debug.Log("[AutoBuild] 文件清单:\n" + string.Join("\n", Directory.GetFiles(ttDir)));

            EditorUtility.RevealInFinder(ttDir);
            EditorUtility.DisplayDialog("构建成功",
                $"抖音小游戏工程已生成:\n{ttDir}\n\n请用抖音开发者工具打开此目录，点「预览」扫码运行。",
                "确定");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("[AutoBuild] 后处理失败: " + e);
        }
    }

    static void SetField(System.Type type, object obj, string name, object value)
    {
        var f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(obj, value);
    }
}
