#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// 离线批量关卡生成工具
///
/// 用法:
///   菜单 → 铲屎官疯了 → 批量生成关卡
///   或   菜单 → 铲屎官疯了 → 生成单关
///
/// 流程:
///   1. 读取 Resources/LevelConfig.csv
///   2. 逐关生成 → BFS 验证 → 筛选 minSteps 在目标范围内
///   3. 保存为 Assets/Data/Levels/Level_XX.asset（与 AddressablesBootstrap 的 Levels Label 目录一致）
/// </summary>
public static class BatchLevelGenerator
{
    // 与 AddressablesBootstrap 标记的 "Assets/Data/Levels" + Levels Label 保持一致
    private const string CSV_PATH = "Assets/Data/Levels/LevelConfig.csv";
    private const string OUTPUT_DIR = "Assets/Data/Levels";

    [MenuItem("铲屎官疯了/批量生成全部关卡")]
    public static void GenerateAllLevels()
    {
        var configs = ParseCSV(CSV_PATH);
        if (configs.Count == 0)
        {
            PetGameGenUtil.Error("错误", $"未找到配置文件: {CSV_PATH}");
            return;
        }

        if (!Directory.Exists(OUTPUT_DIR))
            Directory.CreateDirectory(OUTPUT_DIR);

        int success = 0, fail = 0;
        string report = "";

        for (int i = 0; i < configs.Count; i++)
        {
            var cfg = configs[i];
            if (PetGameGenUtil.ShowProgress(
                "批量生成关卡",
                $"第 {cfg.id} 关 [{cfg.label}]  {cfg.pets.Length}宠 × {cfg.capacity}容量 × {cfg.extraBowls}空碗",
                (float)i / configs.Count)) break;

            var result = GenerateSingleLevel(cfg);
            if (result != null)
            {
                SaveLevelAsset(cfg, result);
                success++;
                report += $"✓ Lv{cfg.id} [{cfg.label}] minSteps={result.minSteps} (目标{cfg.minStepsMin}~{cfg.minStepsMax})\n";
            }
            else
            {
                fail++;
                report += $"✗ Lv{cfg.id} [{cfg.label}] 生成失败!\n";
            }
        }

        PetGameGenUtil.ClearProgress();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        PetGameGenUtil.Info("生成完成", $"成功: {success}  失败: {fail}\n\n详细:\n{report}");

        Debug.Log($"[BatchLevelGenerator] 完成! 成功{success}失败{fail}\n{report}");
    }

    [MenuItem("铲屎官疯了/重新生成当前选中关卡")]
    public static void RegenerateSelectedLevel()
    {
        // 获取选中的 .asset 文件
        var selected = Selection.activeObject as PetLevelConfigV2;
        if (selected == null)
        {
            // 尝试从选中的 asset 文件加载
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".asset"))
                selected = AssetDatabase.LoadAssetAtPath<PetLevelConfigV2>(path);
        }
        if (selected == null)
        {
            PetGameGenUtil.Info("提示", "请在 Project 窗口选中一个关卡 .asset 文件");
            return;
        }

        var configs = ParseCSV(CSV_PATH);
        var cfg = configs.FirstOrDefault(c => c.id == selected.levelId);
        if (cfg == null)
        {
            PetGameGenUtil.Error("错误", $"在 CSV 中未找到关卡 {selected.levelId} 的配置");
            return;
        }

        var result = GenerateSingleLevel(cfg);
        if (result != null)
        {
            SaveLevelAsset(cfg, result);
            AssetDatabase.SaveAssets();
            PetGameGenUtil.Success(
                $"关卡 {cfg.id} 已重新生成\nminSteps={result.minSteps}");
        }
        else
        {
            PetGameGenUtil.Error("失败", $"关卡 {cfg.id} 生成失败");
        }
    }

    [MenuItem("铲屎官疯了/检查CSV配置")]
    public static void CheckCSV()
    {
        var configs = ParseCSV(CSV_PATH);
        string report = $"共 {configs.Count} 关配置:\n\n";
        foreach (var c in configs)
        {
            report += $"Lv{c.id,2} [{c.label,-8}] {c.pets.Length}宠 cap={c.capacity} extra={c.extraBowls} steps={c.minStepsMin}~{c.minStepsMax}\n";
        }
        PetGameGenUtil.Info("CSV 配置检查", report);
    }

    // ===== 核心：生成单关，多 seed 尝试，筛 minSteps =====

    private static LevelData GenerateSingleLevel(LevelConfigEntry cfg)
    {
        // 尝试多个种子，找到 minSteps 在目标范围内的
        for (int seedOffset = 0; seedOffset < 20; seedOffset++)
        {
            int seed = cfg.id * 1000 + seedOffset;
            if (!LevelGenerator.Generate(cfg.pets, cfg.capacity, cfg.extraBowls, seed, out var data))
                continue;

            // 检查 minSteps 是否在目标范围
            if (data.minSteps >= cfg.minStepsMin && data.minSteps <= cfg.minStepsMax)
            {
                return data;
            }

            // 第一次尝试不在范围，继续找下一个种子
            Debug.Log($"[BatchLevelGenerator] Lv{cfg.id} seed={seed} minSteps={data.minSteps} 不在目标范围[{cfg.minStepsMin}~{cfg.minStepsMax}], 尝试下一个种子");
        }

        // 都不在范围，放宽要求，取第一个可解的
        int fallbackSeed = cfg.id * 1000;
        if (LevelGenerator.Generate(cfg.pets, cfg.capacity, cfg.extraBowls, fallbackSeed, out var fallback))
        {
            Debug.LogWarning($"[BatchLevelGenerator] Lv{cfg.id} 未找到目标范围minSteps的种子, 使用 fallback minSteps={fallback.minSteps}");
            return fallback;
        }

        return null;
    }

    // ===== 保存为 .asset =====

    private static void SaveLevelAsset(LevelConfigEntry cfg, LevelData data)
    {
        string path = $"{OUTPUT_DIR}/Level_{cfg.id:D2}.asset";

        // 加载已有或新建
        var existing = AssetDatabase.LoadAssetAtPath<PetLevelConfigV2>(path);
        var level = existing != null ? existing : ScriptableObject.CreateInstance<PetLevelConfigV2>();

        level.levelId = cfg.id;
        level.levelName = LevelGenerator.GetLevelName(cfg.pets, cfg.id);
        level.bowlCapacity = cfg.capacity;
        level.targetScore = LevelGenerator.CalcTargetScore(cfg.pets.Distinct().Count());
        level.difficulty = cfg.id <= 5 ? 0 : (cfg.id <= 20 ? 1 : 2);
        level.minSteps = data.minSteps;
        level.seed = data.seed;
        level.version = System.DateTime.Now.ToString("yyyyMMdd");
        level.petQueue = cfg.pets;
        level.bowlInits = data.bowlInits.ToArray();

        if (existing == null)
            AssetDatabase.CreateAsset(level, path);

        EditorUtility.SetDirty(level);
        MarkAddressable(path);
        Debug.Log($"[BatchLevelGenerator] 保存 {path}: minSteps={data.minSteps} seed={data.seed}");
    }

    /// <summary>生成后自动把关卡打进 Local-MVP 组并贴 Levels Label，使 ResLoader.LoadAll&lt;PetLevelConfigV2&gt;("Levels") 能直接加载。</summary>
    private static void MarkAddressable(string assetPath)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[BatchLevelGenerator] Addressables 未初始化，请先运行菜单 Tools/初始化 Addressables (抖音双轨)，否则关卡不会被打进资源包。");
            return;
        }
        var group = settings.FindGroup("Local-MVP") ?? settings.DefaultGroup;
        if (group == null) return;
        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid)) return;
        var entry = settings.CreateOrMoveEntry(guid, group, true, true);
        if (entry != null)
            entry.SetLabel("Levels", true, true);
    }

    // ===== CSV 解析 =====

    private static List<LevelConfigEntry> ParseCSV(string path)
    {
        var result = new List<LevelConfigEntry>();
        if (!File.Exists(path))
        {
            Debug.LogError($"[BatchLevelGenerator] CSV 文件不存在: {path}");
            return result;
        }

        var lines = File.ReadAllLines(path);
        // 跳过标题行
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var cols = ParseCSVLine(line);
            if (cols.Length < 7) continue;

            var entry = new LevelConfigEntry
            {
                id = int.Parse(cols[0]),
                pets = ParsePets(cols[1]),
                capacity = int.Parse(cols[2]),
                extraBowls = int.Parse(cols[3]),
                minStepsMin = int.Parse(cols[4]),
                minStepsMax = int.Parse(cols[5]),
                label = cols[6]
            };
            result.Add(entry);
        }

        return result;
    }

    // 处理带引号的 CSV 行（pet 字段含逗号所以用引号包裹）
    private static string[] ParseCSVLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        int start = 0;

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (line[i] == ',' && !inQuotes)
            {
                result.Add(line.Substring(start, i - start).Trim('"', ' '));
                start = i + 1;
            }
        }
        result.Add(line.Substring(start).Trim('"', ' '));
        return result.ToArray();
    }

    private static PetType[] ParsePets(string s)
    {
        var parts = s.Split(',');
        var pets = new List<PetType>();
        foreach (var p in parts)
        {
            var trimmed = p.Trim();
            if (System.Enum.TryParse<PetType>(trimmed, out var pet))
                pets.Add(pet);
        }
        return pets.ToArray();
    }

    private class LevelConfigEntry
    {
        public int id;
        public PetType[] pets;
        public int capacity;
        public int extraBowls;
        public int minStepsMin;
        public int minStepsMax;
        public string label;
    }
}
#endif
