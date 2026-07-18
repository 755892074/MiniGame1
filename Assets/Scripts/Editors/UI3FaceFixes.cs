using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

/// <summary>
/// 三个界面统一修复脚本：背景遮罩 + 文字可读性 + 按钮尺寸/颜色
/// 菜单入口: Tools → 铲屎官疯了 → 三界面UI修复
/// 用法: 点击菜单后自动修复所有预制体，改完无需手动保存
/// </summary>
public class UI3FaceFixes
{
    [MenuItem("Tools/铲屎官疯了/三界面UI修复")]
    public static void FixAll3()
    {
        try { FixLoginPanel("Assets/Resources/PrefabsV2/LoginPanel.prefab"); } catch(System.Exception e) { Debug.LogError("[UI3F] LoginPanel error: " + e); }
        try { FixMainMenuPanel("Assets/Resources/PrefabsV2/MainMenuPanel.prefab"); } catch(System.Exception e) { Debug.LogError("[UI3F] MainMenuPanel error: " + e); }
        try { FixGameHUD("Assets/Resources/PrefabsV2/GameHUD.prefab"); } catch(System.Exception e) { Debug.LogError("[UI3F] GameHUD error: " + e); }
        AssetDatabase.Refresh();
        Debug.Log("[UI3F] 三界面UI修复完成 ✓");
    }

    static void FixLoginPanel(string path)
    {
        Debug.Log("[UI3F] === FixLoginPanel ===");
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform rt = root.transform;

        DarkenBackground(rt);
        AddOverlay(rt, 1);

        FixBtnText(rt, "btnUserAgreement", 20);
        FixBtnText(rt, "btnPrivacyPolicy", 20);
        FixBtnText(rt, "btnDisagree", 22);
        FixBtnText(rt, "btnAgree", 22);
        FixAnyText(rt, "txtAnd", 60, 20);
        FixAnyText(rt, "Label", 60, 20);
        FixBtnColor(rt, "btnDouyinLogin", new Color(0.89f, 0.48f, 0.32f, 1f));

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[UI3F] LoginPanel 修复完成");
    }

    static void FixMainMenuPanel(string path)
    {
        Debug.Log("[UI3F] === FixMainMenuPanel ===");
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform rt = root.transform;

        DarkenBackground(rt);
        AddOverlay(rt, 1);

        FixAnyText(rt, "txtFish", 40, 20);
        FixAnyText(rt, "txt", 40, 20);
        FixBtnColor(rt, "btnYard", new Color(0.89f, 0.48f, 0.32f, 1f));

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[UI3F] MainMenuPanel 修复完成");
    }

    static void FixGameHUD(string path)
    {
        Debug.Log("[UI3F] === FixGameHUD ===");
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform rt = root.transform;

        AddOverlay(rt, 0);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[UI3F] GameHUD 修复完成");
    }

    static void DarkenBackground(Transform rootT)
    {
        Transform bgT = FindAnyChild(rootT, "Background");
        if (bgT == null) return;
        Image img = bgT.GetComponent<Image>();
        if (img != null) img.color = new Color(0.55f, 0.55f, 0.55f, 1f);
    }

    // ── 工具函数 ──

    static Transform FindAnyChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name.Contains(name)) return parent.GetChild(i);
        return null;
    }

    static Transform FindExactChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
    }

    static void AddOverlay(Transform parent, int siblingIndex)
    {
        if (FindExactChild(parent, "~Overlay~") != null)
        {
            Debug.LogWarning("[UI3F] Overlay 已存在，跳过: " + parent.name);
            return;
        }

        GameObject overlay = new GameObject("~Overlay~", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(parent, false);
        overlay.transform.SetSiblingIndex(siblingIndex);

        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image img = overlay.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.25f);
        img.raycastTarget = false;
        Debug.Log("[UI3F] Overlay 已添加: " + parent.name);
    }

    /// <summary>修复按钮内的文字大小（按钮名为 btnXxx，文字子节点名为 txt）</summary>
    static void FixBtnText(Transform parent, string btnName, int fontSize)
    {
        Transform btnT = FindExactChild(parent, btnName);
        if (btnT == null) { Debug.LogWarning("[UI3F] 按钮未找到: " + btnName); return; }

        Transform txtT = FindExactChild(btnT, "txt");
        if (txtT == null) txtT = btnT; // Text 就在按钮上

        Text txt = txtT.GetComponent<Text>();
        if (txt != null)
        {
            txt.fontSize = fontSize;
            Debug.Log($"[UI3F] FixBtnText {btnName}: → {fontSize}");
        }
    }

    /// <summary>修复任意名字包含 keyword 的子节点文字大小（depth 限制递归层数）</summary>
    static void FixAnyText(Transform parent, string keyword, int maxDepth, int fontSize)
    {
        var all = parent.GetComponentsInChildren<Text>(true);
        int cnt = 0;
        foreach (var t in all)
        {
            if (t.name.Contains(keyword))
            {
                t.fontSize = fontSize;
                cnt++;
            }
        }
        Debug.Log($"[UI3F] FixAnyText '{keyword}': {cnt} found → {fontSize}");
    }

    /// <summary>修复按钮背景颜色</summary>
    static void FixBtnColor(Transform parent, string btnName, Color color)
    {
        Transform btnT = FindExactChild(parent, btnName);
        if (btnT == null) { Debug.LogWarning("[UI3F] 按钮未找到: " + btnName); return; }

        Image img = btnT.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
            Debug.Log($"[UI3F] FixBtnColor {btnName}: done");
        }
    }
}
