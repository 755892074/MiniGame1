using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 小院面板控制器（P2 — 只读一览；P3/P4 入口）
/// 展示：货币余额 / 建筑一览（4 种 + 等级 + 效果）/ 宠物一览（6 格 + 解锁状态）
/// 点击建筑 → 建筑升级弹窗；点击已救助宠物 → 宠物详情弹窗（喂养/互动）。
/// 面板为叠加层（不替换主菜单），关闭即销毁返回主菜单。
/// </summary>
public class YardPanelController : MonoBehaviour
{
    private MenuSceneController menuController;
    private Font font;

    public void Init(MenuSceneController controller)
    {
        menuController = controller;
        font = Resources.Load<Font>("Fonts/SourceHanSans");
        Build();
    }

    void Build()
    {
        var root = transform as RectTransform;
        if (root == null) return;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.sizeDelta = Vector2.zero;

        // 半透明遮罩
        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(transform, false);
        var dimRT = dim.GetComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one; dimRT.sizeDelta = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);

        // 主面板容器
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.04f, 0.04f);
        prt.anchorMax = new Vector2(0.96f, 0.96f);
        prt.sizeDelta = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.96f, 0.90f, 0.80f, 0.98f);

        // 标题栏
        MakeText(panel.transform, "Title", new Vector2(0.06f, 0.91f), new Vector2(0.70f, 0.99f),
            "🏡 我的小院", 34, new Color(0.45f, 0.30f, 0.20f), TextAnchor.MiddleLeft);

        // 货币行（右上）
        MakeText(panel.transform, "Currency", new Vector2(0.40f, 0.91f), new Vector2(0.93f, 0.99f),
            $"🪙{SaveSystem.Data.gold}  🐟{SaveSystem.Data.fishDiscount}  ⭐{SaveSystem.Data.rescueBadge}",
            16, new Color(0.50f, 0.35f, 0.15f), TextAnchor.MiddleRight);

        // 关闭按钮
        MakeBtn(panel.transform, new Vector2(0.86f, 0.915f), new Vector2(0.96f, 0.985f),
            "✕", new Color(0.79f, 0.24f, 0.24f), () => Close());

        // 分隔线
        MakeLine(panel.transform, 0.89f);

        // ===== 建筑一览 =====
        float y = 0.85f;
        MakeText(panel.transform, "SecBuild", new Vector2(0.06f, y - 0.05f), new Vector2(0.94f, y),
            "🏠 建筑（点击升级）", 24, new Color(0.40f, 0.28f, 0.18f), TextAnchor.MiddleLeft);
        y -= 0.07f;
        foreach (var b in YardDefs.BUILDINGS)
        {
            int lv = SaveSystem.GetBuildingLevel(b.id);
            string lvText = lv <= 0 ? "未建造" : $"Lv.{lv}";
            string eff = lv <= 0 ? "—" : YardDefs.EffectValue(b.id, lv);
            MakeBuildingRow(panel.transform, y, b.id, $"{b.icon} {b.name}", $"{lvText}  ·  {b.effectDesc} {eff}");
            y -= 0.075f;
        }

        // 分隔线
        MakeLine(panel.transform, y + 0.01f);
        y -= 0.04f;

        // ===== 宠物一览 =====
        MakeText(panel.transform, "SecPet", new Vector2(0.06f, y - 0.05f), new Vector2(0.94f, y),
            "🐾 宠物（点击喂食/互动）", 24, new Color(0.40f, 0.28f, 0.18f), TextAnchor.MiddleLeft);
        y -= 0.07f;

        // 6 格网格（3 列 x 2 行）
        float colW = 0.30f, startX = 0.07f, rowH = 0.16f;
        for (int i = 0; i < YardDefs.PETS.Length; i++)
        {
            int col = i % 3, row = i / 3;
            float cx = startX + col * colW;
            float cy = y - row * rowH;
            MakePetSlot(panel.transform, cx, cy, YardDefs.PETS[i]);
        }

        // 底部提示
        MakeText(panel.transform, "Hint", new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.10f),
            "升级 / 喂养功能已开放", 16, new Color(0.55f, 0.45f, 0.35f), TextAnchor.MiddleCenter);
    }

    // 建筑行（可点击）
    void MakeBuildingRow(Transform parent, float y, string id, string left, string right)
    {
        var go = new GameObject("Row_" + id, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.06f, y - 0.065f);
        rt.anchorMax = new Vector2(0.94f, y);
        rt.sizeDelta = Vector2.zero;
        go.GetComponent<Image>().color = new Color(1f, 0.96f, 0.88f, 0.9f);
        go.GetComponent<Button>().onClick.AddListener(() => menuController.ShowBuilding(id));

        MakeText(go.transform, "L", new Vector2(0.02f, 0f), new Vector2(0.50f, 1f),
            left, 19, new Color(0.35f, 0.25f, 0.15f), TextAnchor.MiddleLeft);
        MakeText(go.transform, "R", new Vector2(0.50f, 0f), new Vector2(0.96f, 1f),
            right, 16, new Color(0.5f, 0.4f, 0.3f), TextAnchor.MiddleRight);
    }

    // 单个宠物格
    void MakePetSlot(Transform parent, float x, float y, YardDefs.PetInfo pet)
    {
        bool rescued = SaveSystem.IsPetRescued(pet.type);

        var go = new GameObject($"Pet_{pet.name}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x, y - 0.13f);
        rt.anchorMax = new Vector2(x + 0.27f, y);
        rt.sizeDelta = Vector2.zero;
        go.GetComponent<Image>().color = rescued
            ? new Color(1f, 0.95f, 0.82f, 1f)
            : new Color(0.82f, 0.78f, 0.74f, 0.8f);

        if (rescued)
        {
            // 整格可点击进入详情弹窗
            go.AddComponent<Button>().onClick.AddListener(() => menuController.ShowPetDetail(pet.type));

            var rec = SaveSystem.Data.pets.Find(p => p.petType == pet.type);
            string stage = rec != null && rec.stage >= 1 && rec.stage <= 3 ? YardDefs.STAGE_NAMES[rec.stage] : "";
            string intim = rec != null ? $"{YardDefs.IntimacyLevelName(rec.intimacy)} Lv{YardDefs.IntimacyLevel(rec.intimacy)}" : "";
            string sub = string.IsNullOrEmpty(intim) ? stage : $"{stage} · {intim}";
            string label = rec != null && !string.IsNullOrEmpty(rec.nickname) ? rec.nickname : pet.name;

            MakeText(go.transform, "Icon", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.95f),
                pet.icon, 30, Color.black, TextAnchor.MiddleCenter);
            MakeText(go.transform, "Name", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.55f),
                label, 16, new Color(0.35f, 0.25f, 0.15f), TextAnchor.MiddleCenter);
            MakeText(go.transform, "Sub", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.28f),
                sub, 12, new Color(0.6f, 0.45f, 0.3f), TextAnchor.MiddleCenter);
        }
        else
        {
            if (pet.unlockLevel == 0)
            {
                MakeText(go.transform, "Icon", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.95f),
                    "🎁", 26, new Color(0.6f, 0.45f, 0.2f), TextAnchor.MiddleCenter);
                MakeText(go.transform, "Name", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.55f),
                    pet.name, 15, new Color(0.4f, 0.4f, 0.4f), TextAnchor.MiddleCenter);
                MakeText(go.transform, "Sub", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.28f),
                    "初始伙伴（待领取）", 12, new Color(0.6f, 0.45f, 0.2f), TextAnchor.MiddleCenter);
            }
            else
            {
                MakeText(go.transform, "Icon", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.95f),
                    "🔒", 26, new Color(0.5f, 0.5f, 0.5f), TextAnchor.MiddleCenter);
                MakeText(go.transform, "Name", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.55f),
                    pet.name, 15, new Color(0.4f, 0.4f, 0.4f), TextAnchor.MiddleCenter);
                MakeText(go.transform, "Sub", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.28f),
                    $"通关第{pet.unlockLevel}关解锁", 12, new Color(0.55f, 0.5f, 0.45f), TextAnchor.MiddleCenter);
            }
        }
    }

    // ===== 通用 UI 辅助 =====
    void MakeText(Transform parent, string name, Vector2 min, Vector2 max, string text, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max; rt.sizeDelta = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.text = text; t.fontSize = size; t.color = color;
        t.alignment = anchor; t.font = font;
    }

    void MakeLine(Transform parent, float y)
    {
        var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.06f, y - 0.005f);
        rt.anchorMax = new Vector2(0.94f, y + 0.005f);
        rt.sizeDelta = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.7f, 0.6f, 0.5f, 0.6f);
    }

    void MakeBtn(Transform parent, Vector2 min, Vector2 max, string text, Color bg, Action onClick)
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
        t.text = text; t.fontSize = 22; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter; t.font = font;
    }

    void Close()
    {
        if (menuController != null) menuController.CloseYard();
        else Destroy(gameObject);
    }
}
