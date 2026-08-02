using UnityEngine;
using UnityEngine.UI;

/// <summary>成就面板：运行时构建，纯文本列表，不依赖任何图标 sprite（规避字形缺失坑）。</summary>
public static class AchievementUI
{
    public static void Show()
    {
        var canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogWarning("[AchievementUI] 无 Canvas，无法显示成就面板"); return; }

        // 遮罩根
        var root = new GameObject("AchievementPanel", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        var rrt = root.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
        var bg = root.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);   // 半透明遮罩，挡住背后输入
        bg.raycastTarget = true;

        // 内容容器
        var panel = new GameObject("Content", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.08f, 0.06f); prt.anchorMax = new Vector2(0.92f, 0.94f);
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;

        AddText(panel.transform, "成就", 30, Color.white,
            new Vector2(0.1f, 0.93f), new Vector2(0.9f, 0.99f), TextAnchor.MiddleCenter);
        AddText(panel.transform,
            "已解锁 " + AchievementSystem.UnlockedCount + " / " + AchievementSystem.Defs.Count,
            16, new Color(1f, 0.85f, 0.3f),
            new Vector2(0.1f, 0.87f), new Vector2(0.9f, 0.92f), TextAnchor.MiddleCenter);

        float top = 0.85f;
        float rowH = 0.092f;
        float step = 0.102f;
        foreach (var d in AchievementSystem.Defs)
        {
            bool unlocked = AchievementSystem.IsUnlocked(d.id);
            Color titleCol = unlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            Color descCol  = unlocked ? new Color(0.82f, 0.82f, 0.82f) : new Color(0.5f, 0.5f, 0.5f);
            AddText(panel.transform, d.name, 18, titleCol,
                new Vector2(0.06f, top - rowH), new Vector2(0.94f, top), TextAnchor.MiddleLeft);
            AddText(panel.transform, d.desc, 12, descCol,
                new Vector2(0.06f, top - rowH - 0.012f), new Vector2(0.62f, top - 0.012f), TextAnchor.MiddleLeft);
            string right = unlocked ? "已达成" : ("+" + d.badgeReward + " 徽章");
            Color rightCol = unlocked ? Color.green : new Color(1f, 0.85f, 0.3f);
            AddText(panel.transform, right, 13, rightCol,
                new Vector2(0.64f, top - rowH), new Vector2(0.94f, top), TextAnchor.MiddleRight);
            top -= step;
        }

        // 关闭按钮（用 X，避开 ✕ 字形缺失）
        var close = new GameObject("btnClose", typeof(RectTransform), typeof(SystemFontText), typeof(Button));
        close.transform.SetParent(panel.transform, false);
        var crt = close.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.86f, 0.93f); crt.anchorMax = new Vector2(0.96f, 0.99f);
        crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
        var ct = close.GetComponent<SystemFontText>();
        ct.text = "X"; ct.fontSize = 26; ct.color = Color.white; ct.alignment = TextAnchor.MiddleCenter;
        close.GetComponent<Button>().onClick.AddListener(() => GameObject.Destroy(root));
    }

    static void AddText(Transform parent, string text, int size, Color color, Vector2 min, Vector2 max, TextAnchor align)
    {
        var go = new GameObject("txt", typeof(RectTransform), typeof(SystemFontText));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<SystemFontText>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
        t.raycastTarget = false;
    }
}
