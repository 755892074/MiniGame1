using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成工具的"静默模式"开关。
///
/// 用途：AI（手机端 workbuddy）驱动编辑器自动跑生成菜单时，所有
/// EditorUtility.DisplayDialog 完成/错误提示会变成卡住流程的模态框
/// （手机端没人点）。开启静默后，提示改为写 Debug.Log/LogError，
/// 既不会卡住，又能通过 MCP 的 unity_console 读回来。
///
/// 默认 true（静默）——面向自动化。桌面端手动开发若想看弹窗提示，
/// 用菜单「铲屎官疯了 / 切换生成弹窗提示」翻转。
/// 通过 EditorPrefs 持久化，跨编辑器重编译保留。
/// </summary>
public static class PetGameGenUtil
{
    const string SILENT_KEY = "PetGameSilent";

    public static bool Silent
    {
        get { return EditorPrefs.GetBool(SILENT_KEY, true); }
        set { EditorPrefs.SetBool(SILENT_KEY, value); }
    }

    /// 成功提示：GUI 弹窗；静默时写日志（MCP 控制台可读到）
    public static void Success(string msg)
    {
        if (Silent) Debug.Log("[PetGameGen] " + msg);
        else EditorUtility.DisplayDialog("完成", msg, "好的");
    }

    /// 错误提示：GUI 弹窗；静默时写错误日志（MCP 能抓到，且不卡住）
    public static void Error(string title, string msg)
    {
        if (Silent) Debug.LogError("[" + title + "] " + msg);
        else EditorUtility.DisplayDialog(title, msg, "确定");
    }

    /// 信息/报告提示（如 CSV 检查）：GUI 弹窗；静默时写日志
    public static void Info(string title, string msg)
    {
        if (Silent) Debug.Log("[" + title + "] " + msg);
        else EditorUtility.DisplayDialog(title, msg, "确定");
    }

    /// 进度条：静默时不显示；返回 true 表示用户取消
    public static bool ShowProgress(string title, string info, float progress)
    {
        if (Silent) return false;
        return EditorUtility.DisplayCancelableProgressBar(title, info, progress);
    }

    public static void ClearProgress()
    {
        if (!Silent) EditorUtility.ClearProgressBar();
    }

    [MenuItem("铲屎官疯了/切换生成弹窗提示")]
    static void ToggleSilent()
    {
        Silent = !Silent;
        EditorUtility.DisplayDialog("提示",
            $"生成弹窗提示已{(Silent ? "关闭（静默，适合 AI 自动驱动）" : "开启（桌面手动开发可见弹窗）")}", "好的");
    }
}
