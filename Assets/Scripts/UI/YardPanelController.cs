using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 小院面板控制器（P2 — 只读一览）
/// 展示：货币余额 / 建筑一览（4 种 + 等级 + 效果）/ 宠物一览（6 格 + 解锁状态）
/// 升级 / 喂养为后续阶段（P3 / P4），本面板不修改数据。
/// 面板为叠加层（不替换主菜单），关闭即销毁返回主菜单。
/// </summary>
public class YardPanelController : MonoBehaviour
{
    private MenuSceneController menuController;
    private Font font;

    // 建筑元数据（id / 图标 / 名称 / 效果说明）
    private static readonly (string id, string icon, string name, string effect)[] BUILDINGS = new[]
    {
        ("foodbowl", "🥣", "食盆", "全宠物亲密度增益"),
        ("toy",      "🧸", "玩具", "小鱼干产出速率"),
        ("medical",  "🏥", "医疗", "离线产出上限"),
        ("garden",   "🌻", "花园", "亲密度每日自然增长"),
    };

    // 宠物元数据（类型 / 图标 / 名称 / 解锁关）
    private static readonly (PetType type, string icon, string name, int unlockLevel)[] PETS = new[]
    {
        (PetType.Cat,    "🐱", "橘猫",   0),
        (PetType.Dog,    "🐶", "柴犬",   5),
        (PetType.Hamster,"🐹", "仓鼠",   10),
        (PetType.Parrot, "🦜", "鹦鹉",   15),
        (PetType.Fish,   "🐟", "金鱼",   20),
        (PetType.Rabbit, "🐰", "垂耳兔", 25),
    };

    private static readonly string[] STAGE_NAMES = { "", "脏兮兮", "干净", "幸福" };

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

        // 半透明遮罩（点击空白不关闭，避免误触）
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
        MakeText(panel.transform, "Currency", new Vector2(0.45f, 0.91f), new Vector2(0.93f, 0.99f),
            $"🪙{SaveSystem.Data.gold}  🐟{SaveSystem.Data.fishDiscount}  ⭐{SaveSystem.Data.rescueBadge}",
            18, new Color(0.50f, 0.35f, 0.15f), TextAnchor.MiddleRight);

        // 关闭按钮
        MakeBtn(panel.transform, new Vector2(0.86f, 0.915f), new Vector2(0.96f, 0.985f),
            "✕", new Color(0.79f, 0.24f, 0.24f), () => Close());

        // 分隔线
        MakeLine(panel.transform, 0.89f);

        // ===== 建筑一览 =====
        float y = 0.85f;
        MakeText(panel.transform, "SecBuild", new Vector2(0.06f, y - 0.05f), new Vector2(0.94f, y),
            "🏠 建筑", 26, new Color(0.40f, 0.28f, 0.18f), TextAnchor.MiddleLeft);
        y -= 0.07f;
        foreach (var b in BUILDINGS)
        {
            int lv = SaveSystem.GetBuildingLevel(b.id);
            string lvText = lv <= 0 ? "未建造" : $"Lv.{lv}";
            string eff = lv <= 0 ? "—" : EffectValue(b.id, lv);
            MakeRow(panel.transform, y,
                $"{b.icon} {b.name}",
                $"{lvText}  ·  {b.effect} {eff}");
            y -= 0.075f;
        }

        // 分隔线
        MakeLine(panel.transform, y + 0.01f);
        y -= 0.04f;

        // ===== 宠物一览 =====
        MakeText(panel.transform, "SecPet", new Vector2(0.06f, y - 0.05f), new Vector2(0.94f, y),
            "🐾 宠物", 26, new Color(0.40f, 0.28f, 0.18f), TextAnchor.MiddleLeft);
        y -= 0.07f;

        // 6 格网格（3 列 x 2 行）
        float colW = 0.30f, startX = 0.07f, rowH = 0.16f;
        for (int i = 0; i < PETS.Length; i++)
        {
            int col = i % 3, row = i / 3;
            float cx = startX + col * colW;
            float cy = y - row * rowH;
            MakePetSlot(panel.transform, cx, cy, PETS[i]);
        }

        // 底部提示
        MakeText(panel.transform, "Hint", new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.10f),
            "升级 / 喂养功能即将上线", 16, new Color(0.55f, 0.45f, 0.35f), TextAnchor.MiddleCenter);
    }

    // 建筑效果数值
    string EffectValue(string id, int level)
    {
        int add = level - 1;
        switch (id)
        {
            case "foodbowl": return add <= 0 ? "0%" : $"+{add * 5}%";
            case "toy":      return add <= 0 ? "0%" : $"+{add * 10}%";
            case "medical":  return add <= 0 ? "0h" : $"+{add * 2}h";
            case "garden":   return add <= 0 ? "0"  : $"+{add * 2}/天";
            default:         return "";
        }
    }

    // 单个宠物格
    void MakePetSlot(Transform parent, float x, float y, (PetType type, string icon, string name, int unlockLevel) pet)
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
            var rec = SaveSystem.Data.pets.Find(p => p.petType == pet.type);
            string stage = rec != null && rec.stage >= 1 && rec.stage <= 3 ? STAGE_NAMES[rec.stage] : "";
            string label = string.IsNullOrEmpty(rec?.nickname) ? pet.name : rec.nickname;
            MakeText(go.transform, "Icon", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.95f),
                pet.icon, 30, Color.black, TextAnchor.MiddleCenter);
            MakeText(go.transform, "Name", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.55f),
                label, 16, new Color(0.35f, 0.25f, 0.15f), TextAnchor.MiddleCenter);
            MakeText(go.transform, "Stage", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.28f),
                stage, 13, new Color(0.6f, 0.45f, 0.3f), TextAnchor.MiddleCenter);
        }
        else
        {
            if (pet.unlockLevel == 0)
            {
                // 初始伙伴：本应为已拥有，异常情况（如存档异常）下显示待领取
                MakeText(go.transform, "Icon", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.95f),
                    "🎁", 26, new Color(0.6f, 0.45f, 0.2f), TextAnchor.MiddleCenter);
                MakeText(go.transform, "Name", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.55f),
                    pet.name, 15, new Color(0.4f, 0.4f, 0.4f), TextAnchor.MiddleCenter);
                MakeText(go.transform, "Stage", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.28f),
                    "初始伙伴（待领取）", 12, new Color(0.6f, 0.45f, 0.2f), TextAnchor.MiddleCenter);
            }
            else
            {
                MakeText(go.transform, "Icon", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.95f),
                    "🔒", 26, new Color(0.5f, 0.5f, 0.5f), TextAnchor.MiddleCenter);
                MakeText(go.transform, "Name", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.55f),
                    pet.name, 15, new Color(0.4f, 0.4f, 0.4f), TextAnchor.MiddleCenter);
                MakeText(go.transform, "Stage", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.28f),
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

    void MakeRow(Transform parent, float y, string left, string right)
    {
        MakeText(parent, "L_" + left, new Vector2(0.08f, y - 0.06f), new Vector2(0.55f, y),
            left, 20, new Color(0.35f, 0.25f, 0.15f), TextAnchor.MiddleLeft);
        MakeText(parent, "R_" + left, new Vector2(0.55f, y - 0.06f), new Vector2(0.93f, y),
            right, 18, new Color(0.5f, 0.4f, 0.3f), TextAnchor.MiddleRight);
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
