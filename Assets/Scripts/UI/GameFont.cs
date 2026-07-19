using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 统一字体入口。
/// 抖音小游戏：走 TTSDK.TT.GetSystemFont（系统字，异步回调）——官方推荐的解决中文不显示方案。
/// 其余平台（编辑器/独立包）：走 Font.CreateDynamicFontFromOSFont。
/// 背景：打包抖音小游戏后，内置默认字体(Arial→团结改名 LegacyRuntime.ttf)不含中文且已失效，
/// 且 Resources.Load 动态 TTF 在 WebView 沙箱里常被重定向失败，导致中文全空白。
/// </summary>
public static class GameFont
{
    private static Font _font;
    private static bool _requested;
    private static readonly List<Text> _pending = new List<Text>();

    /// <summary>给单个 Text 应用系统字体；若尚未就绪（抖音异步）则登记，加载后自动刷新。</summary>
    public static void Apply(Text t)
    {
        if (t == null) return;
        if (_font != null)
        {
            t.font = _font;
            return;
        }
        _pending.Add(t);
        EnsureLoaded();
    }

    /// <summary>给 GameObject 下所有 Text（含未激活）统一应用系统字体，用于 prefab 实例化后。</summary>
    public static void ApplyAll(GameObject root)
    {
        if (root == null) return;
        var texts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
            Apply(texts[i]);
    }

    private static void EnsureLoaded()
    {
        if (_requested) return;
        _requested = true;

#if TTSDK_MIX_ENGINE
        try
        {
            TTSDK.TT.GetSystemFont((Font f) =>
            {
                if (f != null)
                {
                    _font = f;
                    Flush();
                }
                else
                {
                    _font = CreateOSFont();
                    Flush();
                }
            });
        }
        catch (Exception)
        {
            _font = CreateOSFont();
            Flush();
        }
#else
        _font = CreateOSFont();
        Flush();
#endif
    }

    private static Font CreateOSFont()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string[] names = { "Microsoft YaHei", "微软雅黑", "SimHei", "黑体", "Arial" };
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        string[] names = { "PingFang SC", "苹方", "Heiti SC", "STHeiti", "Arial" };
#else
        string[] names = { "Arial" };
#endif
        foreach (var n in names)
        {
            var f = Font.CreateDynamicFontFromOSFont(n, 24);
            if (f != null) return f;
        }
        // 兜底：已无内置中文字体，统一依赖系统字体（抖音 TT.GetSystemFont / 其他平台 OS 字体）
        return null;
    }

    private static void Flush()
    {
        if (_font == null) return;
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            var t = _pending[i];
            if (t != null) t.font = _font;
            _pending.RemoveAt(i);
        }
    }
}
