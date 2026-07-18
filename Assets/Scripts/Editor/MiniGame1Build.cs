using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

public class MiniGame1Build
{
    [MenuItem("Tools/铲屎官疯了/导出抖音小游戏包")]
    public static void BuildMiniGame()
    {
        string outPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "MiniGame1_Build");
        
        // Clean old
        if (Directory.Exists(outPath)) Directory.Delete(outPath, true);
        Directory.CreateDirectory(outPath);
        
        // Get enabled scenes
        var scenes = EditorBuildSettings.scenes;
        var scenePaths = new string[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            scenePaths[i] = scenes[i].path;
        
        Debug.Log("[BUILD] Scenes: " + string.Join(", ", scenePaths));
        Debug.Log("[BUILD] Output: " + outPath);
        Debug.Log("[BUILD] Target: MiniGame");
        
        var opts = new BuildPlayerOptions();
        opts.scenes = scenePaths;
        opts.locationPathName = outPath;
        opts.target = BuildTarget.MiniGame;
        opts.targetGroup = BuildTargetGroup.MiniGame;
        opts.options = BuildOptions.None;
        
        var report = BuildPipeline.BuildPlayer(opts);
        var result = report.summary.result;
        
        if (result == BuildResult.Succeeded)
        {
            Debug.Log("[BUILD] SUCCESS! Output: " + outPath);
            // Open folder
            System.Diagnostics.Process.Start("explorer.exe", outPath);
        }
        else
        {
            Debug.LogError("[BUILD] FAILED: " + result + " | errors=" + report.summary.totalErrors + " | warnings=" + report.summary.totalWarnings);
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error)
                        Debug.LogError("[BUILD] " + msg.content);
                }
            }
        }
    }
}
