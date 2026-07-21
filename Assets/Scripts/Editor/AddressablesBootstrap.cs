#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.IO;
using System.Linq;

/// <summary>
/// Addressables 抖音双轨初始化（doc/16 阶段1）。
/// 打开编辑器自动初始化（try/catch 保护，失败仅警告），或菜单 Tools → 初始化 Addressables (抖音双轨) 手动触发。
/// 作用：创建 Settings + Local-MVP / Remote-Heavy 两组，把首启常用资源标记 Addressable 进本地组。
/// Remote-Heavy 留待阶段3（AutoStreaming）填充；Remote.LoadPath 的 dummy 替换由阶段2 运行时脚本处理。
/// </summary>
public class AddressablesBootstrap
{
    const string ConfigFolder = "Assets/AddressableAssetsData";
    const string SettingsName = "AddressableAssetSettings";
    const string LocalGroup = "Local-MVP";
    const string RemoteGroup = "Remote-Heavy";
    const string LevelsLabel = "Levels";

    [InitializeOnLoadMethod]
    static void AutoInit()
    {
        EditorApplication.delayCall += () =>
        {
            try { Setup(); }
            catch (System.Exception e) { Debug.LogWarning("[AddressablesBootstrap] 自动初始化跳过: " + e.Message); }
        };
    }

    [MenuItem("Tools/初始化 Addressables (抖音双轨)")]
    public static void Setup()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            settings = AddressableAssetSettings.Create(ConfigFolder, SettingsName, true, true);
            AddressableAssetSettingsDefaultObject.Settings = settings; // 注册为默认对象，构建系统才能识别
        }
        if (settings == null) { Debug.LogError("[AddressablesBootstrap] 无法创建 Addressable settings"); return; }

        var local = EnsureGroup(settings, LocalGroup, false);
        var remote = EnsureGroup(settings, RemoteGroup, true);

        // 首启常用 → 本地分包 (doc/16 资源分类矩阵)
        MarkFolder(settings, local, "Assets/Art/PetGame/pets", true);
        MarkFolder(settings, local, "Assets/Art/PetGame/foods", true);
        MarkFolder(settings, local, "Assets/Art/PetGame/bowls", true);
        MarkFolder(settings, local, "Assets/Art/PetGame/UI", true);
        MarkFolder(settings, local, "Assets/Prefabs/UI/PrefabsV2", false);
        // 关卡 → 本地 + Label "Levels"（供 Resources.LoadAll 改造后按 Label 批量加载）
        // 先清理旧 .csv 的 Levels Label，避免与 PetLevelConfigV2 类型冲突
        foreach (var entry in local.entries.ToList())
        {
            var entryPath = AssetDatabase.GUIDToAssetPath(entry.guid);
            if (entryPath.EndsWith(".csv") && entry.labels.Contains(LevelsLabel))
                entry.SetLabel(LevelsLabel, false, true);
        }
        MarkFolder(settings, local, "Assets/Data/Levels", true, LevelsLabel);

        settings.DefaultGroup = local;

        AssetDatabase.SaveAssets();
        Debug.Log("[AddressablesBootstrap] ✅ 初始化完成: Local-MVP 含 pets/foods/bowls/UI/PrefabsV2/Levels；Remote-Heavy 待阶段3填充");
    }

    static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings, string name, bool remote)
    {
        var g = settings.FindGroup(name);
        if (g != null) return g;
        // 1.22.3 签名: CreateGroup(name, setAsDefault, readOnly, postEvent, List<AddressableAssetGroupSchema> schemasToCopy, params Type[] types)
        g = settings.CreateGroup(name, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        var bundled = g.GetSchema<BundledAssetGroupSchema>();
        if (bundled != null)
        {
            bundled.BuildPath.SetVariableByName(settings, remote ? AddressableAssetSettings.kRemoteBuildPath : AddressableAssetSettings.kLocalBuildPath);
            bundled.LoadPath.SetVariableByName(settings, remote ? AddressableAssetSettings.kRemoteLoadPath : AddressableAssetSettings.kLocalLoadPath);
            bundled.BundleMode = remote
                ? BundledAssetGroupSchema.BundlePackingMode.PackSeparately
                : BundledAssetGroupSchema.BundlePackingMode.PackTogether;
        }
        return g;
    }

    static void MarkFolder(AddressableAssetSettings settings, AddressableAssetGroup group, string folder, bool recursive, string label = null)
    {
        if (!AssetDatabase.IsValidFolder(folder)) { Debug.LogWarning("[AddressablesBootstrap] 跳过不存在目录: " + folder); return; }
        var guids = AssetDatabase.FindAssets("", new[] { folder });
        int n = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Directory.Exists(path)) continue;
            if (path.EndsWith(".cs") || path.EndsWith(".meta") || path.EndsWith(".unity") || path.EndsWith(".csv")) continue;
            var entry = settings.CreateOrMoveEntry(guid, group, true, true);
            if (entry != null)
            {
                if (label != null) entry.SetLabel(label, true, true);
                n++;
            }
        }
        Debug.Log($"[AddressablesBootstrap] 标记 {n} 个资源进 {group.Name} ({(label != null ? "label=" + label : "path-key")})");
    }

    [MenuItem("Tools/Addressables PlayMode = Use Asset Database")]
    public static void SetPlayModeToUseAssetDatabase()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) { Debug.LogError("[AddressablesBootstrap] no settings"); return; }
        var field = typeof(AddressableAssetSettings).GetField("m_DataBuilders", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field == null) { Debug.LogError("[AddressablesBootstrap] no m_DataBuilders"); return; }
        var builders = (System.Collections.IList)field.GetValue(settings);
        int target = -1;
        for (int i = 0; i < builders.Count; i++)
        {
            var b = builders[i];
            var np = b.GetType().GetProperty("Name");
            string name = np != null ? (np.GetValue(b)?.ToString() ?? "?") : b.GetType().Name;
            if (name.Contains("Asset Database")) target = i;
        }
        if (target >= 0) { settings.ActivePlayModeDataBuilderIndex = target; AssetDatabase.SaveAssets(); Debug.Log("[AddressablesBootstrap] PlayMode -> Use Asset Database (index " + target + ")"); }
        else Debug.LogError("[AddressablesBootstrap] no Asset Database builder");
    }
}
#endif
