using UnityEngine;
using UnityEngine.UI;

/// <summary>全局轻量 toast，任意场景可用，自动找活动 Canvas 挂载。</summary>
public static class Toast
{
    public static void Show(string msg)
    {
        var canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogWarning("[Toast] 无 Canvas，跳过: " + msg); return; }

        var root = new GameObject("ToastRoot", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.15f, 0.80f);
        rt.anchorMax = new Vector2(0.85f, 0.88f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        var tgo = new GameObject("ToastText", typeof(RectTransform), typeof(SystemFontText));
        tgo.transform.SetParent(root.transform, false);
        var trt = tgo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var t = tgo.GetComponent<SystemFontText>();
        t.text = msg; t.fontSize = 18; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;

        GameObject.Destroy(root, 2.5f);
    }
}
