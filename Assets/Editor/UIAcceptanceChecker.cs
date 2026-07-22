using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// UI 视觉验收检查器（自动化跑测标准）
/// 检测：① 纯色块按钮（未用美术 sprite）② 越界 ③ 同父 Button 重叠 ④ 同排按钮尺寸不一致
/// 用法：菜单「铲屎官疯了/UI视觉验收检查」，或 codely 调用 UIAcceptanceChecker.CheckActiveUI()
/// 注意：本脚本只在 Editor 编译（UnityEditor 依赖）。
/// 规则经过收敛：跳过 inactive 对象（避免误报将被清掉的结算遮罩内容）；按钮 sprite 接受"自身或子物体有 sprite"（碗类容器按钮视觉在子物体）；重叠只查 Button↔Button（避免误报遮罩盖内容/叠放子物体）。
/// </summary>
public class UIAcceptanceChecker
{
    const float SCREEN_W = 750f, SCREEN_H = 1334f;
    const float OOB_MARGIN = 5f;     // 越界容差（像素）
    const float OVERLAP_MIN = 10f;   // 同父 Button 交集超过此尺寸视为重叠（像素）

    [MenuItem("铲屎官疯了/UI视觉验收检查")]
    public static void RunMenu()
    {
        var r = CheckActiveUI();
        Debug.Log(r);
        EditorUtility.DisplayDialog("UI 视觉验收",
            r.Contains("FAIL") ? "存在不合格项，详见 Console" : "全部通过 ✅", "OK");
    }

    /// <summary>检查当前激活的 UI 根（GameHUD 或 Canvas），返回报告文本</summary>
    public static string CheckActiveUI()
    {
        var sb = new StringBuilder();
        sb.AppendLine("==== UI 视觉验收 ====");

        var root = GameObject.Find("GameHUD");
        if (root == null) { var cv = Object.FindObjectOfType<Canvas>(); if (cv != null) root = cv.gameObject; }
        if (root == null) { sb.AppendLine("FAIL: 找不到 UI 根节点(GameHUD/Canvas)"); return sb.ToString(); }

        var rts = new List<RectTransform>();
        root.GetComponentsInChildren<RectTransform>(true, rts);

        int fail = 0, warn = 0;
        Vector3[] c = new Vector3[4];

        // 1. 纯色块按钮（自身或子物体均无美术 sprite）
        sb.AppendLine("--- 1. 按钮是否使用美术资源(自身或子物体有 sprite 即通过) ---");
        foreach (var rt in rts)
        {
            if (!rt.gameObject.activeInHierarchy) continue;
            var btn = rt.GetComponent<Button>();
            if (btn == null) continue;
            if (!HasSprite(rt))
            {
                sb.AppendLine("FAIL 按钮无 sprite(纯色块): " + FullName(rt));
                fail++;
            }
        }

        // 2. 越界（仅激活对象）
        sb.AppendLine("--- 2. 越界检查(屏幕 750x1334) ---");
        foreach (var rt in rts)
        {
            if (!rt.gameObject.activeInHierarchy) continue;
            try { rt.GetWorldCorners(c); } catch { continue; }
            float minX = Mathf.Min(c[0].x, c[2].x), maxX = Mathf.Max(c[0].x, c[2].x);
            float minY = Mathf.Min(c[0].y, c[2].y), maxY = Mathf.Max(c[0].y, c[2].y);
            if (minX < -OOB_MARGIN || minY < -OOB_MARGIN || maxX > SCREEN_W + OOB_MARGIN || maxY > SCREEN_H + OOB_MARGIN)
            {
                sb.AppendLine($"WARN 越界: {FullName(rt)} ({minX:F0},{minY:F0})-({maxX:F0},{maxY:F0})");
                warn++;
            }
        }

        // 3. 同父 Button 重叠（仅激活 Button，避免误报遮罩盖内容/叠放子物体）
        sb.AppendLine("--- 3. 同父 Button 重叠 ---");
        var btns = new List<RectTransform>();
        foreach (var rt in rts) { if (rt.gameObject.activeInHierarchy && rt.GetComponent<Button>() != null) btns.Add(rt); }
        for (int a = 0; a < btns.Count; a++)
        {
            for (int b = a + 1; b < btns.Count; b++)
            {
                if (btns[a].parent != btns[b].parent) continue;
                try { btns[a].GetWorldCorners(c); } catch { continue; }
                float ax0 = Mathf.Min(c[0].x, c[2].x), ax1 = Mathf.Max(c[0].x, c[2].x);
                float ay0 = Mathf.Min(c[0].y, c[2].y), ay1 = Mathf.Max(c[0].y, c[2].y);
                try { btns[b].GetWorldCorners(c); } catch { continue; }
                float bx0 = Mathf.Min(c[0].x, c[2].x), bx1 = Mathf.Max(c[0].x, c[2].x);
                float by0 = Mathf.Min(c[0].y, c[2].y), by1 = Mathf.Max(c[0].y, c[2].y);
                float ix = Mathf.Min(ax1, bx1) - Mathf.Max(ax0, bx0);
                float iy = Mathf.Min(ay1, by1) - Mathf.Max(ay0, by0);
                if (ix > OVERLAP_MIN && iy > OVERLAP_MIN)
                {
                    sb.AppendLine($"WARN 按钮重叠: {FullName(btns[a])} & {FullName(btns[b])} ({ix:F0}x{iy:F0})");
                    warn++;
                }
            }
        }

        // 4. 同排按钮尺寸一致性（同父按钮高度差异 > 20px）
        sb.AppendLine("--- 4. 同排按钮尺寸一致性 ---");
        var byParent = new Dictionary<Transform, List<float>>();
        foreach (var rt in btns)
        {
            if (!byParent.ContainsKey(rt.parent)) byParent[rt.parent] = new List<float>();
            byParent[rt.parent].Add(rt.sizeDelta.y);
        }
        foreach (var kv in byParent)
        {
            if (kv.Value.Count < 2) continue;
            float avg = 0; foreach (var v in kv.Value) avg += v; avg /= kv.Value.Count;
            foreach (var v in kv.Value)
            {
                if (Mathf.Abs(v - avg) > 20f)
                {
                    sb.AppendLine($"WARN 同排按钮高度不一致: 父 {kv.Key.name} 高={v:F0} 均值={avg:F0}");
                    warn++; break;
                }
            }
        }

        sb.AppendLine($"==== 结果: FAIL={fail} WARN={warn} ====");
        if (fail == 0 && warn == 0) sb.AppendLine("PASS ✅");
        return sb.ToString();
    }

    static bool HasSprite(Transform t)
    {
        var img = t.GetComponent<Image>();
        if (img != null && img.sprite != null) return true;
        // 子物体有 sprite（碗类容器按钮，视觉在 BowlBg 子物体）
        var childImgs = t.GetComponentsInChildren<Image>(true);
        foreach (var ci in childImgs) if (ci.sprite != null) return true;
        return false;
    }

    static string FullName(Transform t)
    {
        return t.parent != null ? t.parent.name + "/" + t.name : t.name;
    }
}
