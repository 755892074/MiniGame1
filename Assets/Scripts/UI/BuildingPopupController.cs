using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 建筑升级弹窗（P4）
/// 展示当前/下一级效果与升级成本，按金币(+鱼干/徽章)扣费升级。
/// 建筑等级上限受住所等级限制（design §3.4）。
/// </summary>
public class BuildingPopupController : MonoBehaviour
{
    private MenuSceneController menuController;
    private string buildingId;
    private Font font;

    private Text txtInfo;
    private Text txtCost;
    private Button btnUpgrade;
    private Text txtToast;

    public void Init(MenuSceneController controller, string id)
    {
        menuController = controller;
        buildingId = id;
        font = Resources.Load<Font>("Fonts/SourceHanSans");
        Build();
    }

    void Build()
    {
        var info = System.Array.Find(YardDefs.BUILDINGS, b => b.id == buildingId);

        var root = transform as RectTransform;
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.sizeDelta = Vector2.zero;

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(transform, false);
        var drt = dim.GetComponent<RectTransform>();
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.sizeDelta = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        dim.AddComponent<Button>().onClick.AddListener(() => menuController.ClosePopup());

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.12f, 0.28f);
        prt.anchorMax = new Vector2(0.88f, 0.75f);
        prt.sizeDelta = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.98f, 0.93f, 0.84f, 0.99f);

        MakeText(panel.transform, "Title", new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.97f),
            $"{info.icon} {info.name}", 30, new Color(0.40f, 0.28f, 0.18f), TextAnchor.MiddleCenter);

        MakeBtn(panel.transform, new Vector2(0.85f, 0.88f), new Vector2(0.97f, 0.98f),
            "✕", new Color(0.79f, 0.24f, 0.24f), () => menuController.ClosePopup());

        MakeText(panel.transform, "Desc", new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.82f),
            info.effectDesc, 18, new Color(0.5f, 0.4f, 0.3f), TextAnchor.MiddleCenter);

        // 等级 + 效果（动态）
        txtInfo = MakeText(panel.transform, "Info", new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.70f),
            "", 18, new Color(0.35f, 0.25f, 0.15f), TextAnchor.MiddleCenter);

        // 成本（动态）
        txtCost = MakeText(panel.transform, "Cost", new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.48f),
            "", 18, new Color(0.45f, 0.35f, 0.2f), TextAnchor.MiddleCenter);

        // 升级按钮
        btnUpgrade = MakeBtn(panel.transform, new Vector2(0.10f, 0.18f), new Vector2(0.90f, 0.30f),
            "⬆ 升级", new Color(0.85f, 0.55f, 0.12f), () => OnUpgrade());

        // toast
        txtToast = MakeText(panel.transform, "Toast", new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.15f),
            "", 16, new Color(0.7f, 0.3f, 0.2f), TextAnchor.MiddleCenter);

        Refresh();
    }

    void Refresh()
    {
        var ui = SaveSystem.GetBuildingUpgradeInfo(buildingId);
        txtInfo.text = ui.maxed
            ? $"等级：Lv.{ui.currentLevel}（已满级，上限受住所等级限制）\n当前效果：{ui.effectCurrent}"
            : $"等级：Lv.{ui.currentLevel} → Lv.{ui.currentLevel + 1}\n当前：{ui.effectCurrent}  →  下一级：{ui.effectNext}";

        if (ui.maxed)
        {
            txtCost.text = "已达当前上限";
            btnUpgrade.interactable = false;
            var t = btnUpgrade.GetComponentInChildren<Text>();
            if (t != null) t.text = "已满级";
        }
        else
        {
            string costStr = $"🪙{ui.goldCost}";
            if (ui.fishCost > 0) costStr += $"  🐟{ui.fishCost}";
            if (ui.badgeCost > 0) costStr += $"  ⭐{ui.badgeCost}";
            txtCost.text = $"升级消耗：{costStr}";
            btnUpgrade.interactable = ui.affordable;
            var t = btnUpgrade.GetComponentInChildren<Text>();
            if (t != null) t.text = ui.affordable ? "⬆ 升级" : "⬆ 货币不足";
        }
    }

    void OnUpgrade()
    {
        if (SaveSystem.TryUpgradeBuilding(buildingId))
        {
            ShowToast("升级成功！效果已提升");
            Refresh();
        }
        else
        {
            ShowToast("货币不足或已满级");
        }
    }

    void ShowToast(string msg)
    {
        if (txtToast != null) txtToast.text = msg;
    }

    // ===== 通用辅助 =====
    Text MakeText(Transform parent, string name, Vector2 min, Vector2 max, string text, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max; rt.sizeDelta = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.text = text; t.fontSize = size; t.color = color;
        t.alignment = anchor; t.font = font;
        return t;
    }

    Button MakeBtn(Transform parent, Vector2 min, Vector2 max, string text, Color bg, Action onClick)
    {
        var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max; rt.sizeDelta = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());
        var tg = new GameObject("T", typeof(RectTransform), typeof(Text));
        tg.transform.SetParent(go.transform, false);
        var trt = tg.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var t = tg.GetComponent<Text>();
        t.text = text; t.fontSize = 18; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter; t.font = font;
        return btn;
    }
}
