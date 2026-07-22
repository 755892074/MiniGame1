using UnityEngine;

/// <summary>
/// GM / 调试全局开关。
/// 开发期（UNITY_EDITOR）默认开启；发布构建默认关闭。
/// 运行时可读取 GM.Enabled，或用 GM.Toggle() / 直接赋值手动切换。
/// </summary>
public static class GM
{
    public static bool Enabled =
#if UNITY_EDITOR
        false;
#else
        false;
#endif

    /// <summary>运行时手动切换（例如预留的 GM 面板开关）</summary>
    public static void Toggle() => Enabled = !Enabled;
}
