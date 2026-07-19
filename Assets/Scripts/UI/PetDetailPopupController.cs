using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 宠物详情弹窗（P3 — 喂养 / 互动）
/// 喂食：消耗小鱼干 +亲密度（受食盆加成）
/// 互动：每日限1次 +亲密度
/// </summary>
public class PetDetailPopupController : MonoBehaviour
{
    private MenuSceneController menuController;
    private PetType petType;

    private Text txtIntimacy;
    private Button btnInteract;
    private Text txtToast;

    public void Init(MenuSceneController controller, PetType type)
    {
        menuController = controller;
        petType = type;
        Build();
    }

    void Build()
    {
        var rec = SaveSystem.Data.pets.Find(p => p.petType == petType);
        var info = System.Array.Find(YardDefs.PETS, p => p.type == petType);
        string petName = (rec != null && !string.IsNullOrEmpty(rec.nickname)) ? rec.nickname : (info.name ?? petType.ToString());

        var root = transform as RectTransform;
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.sizeDelta = Vector2.zero;

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(transform, false);
        var drt = dim.GetComponent<RectTransform>();
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.sizeDelta = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        // 点遮罩关闭
        dim.AddComponent<Button>().onClick.AddListener(() => menuController.ClosePopup());

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.12f, 0.25f);
        prt.anchorMax = new Vector2(0.88f, 0.78f);
        prt.sizeDelta = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.98f, 0.93f, 0.84f, 0.99f);

        // 标题：图标 + 名称
        MakeText(panel.transform, "Title", new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.97f),
            $"{info.icon} {petName}", 30, new Color(0.40f, 0.28f, 0.18f), TextAnchor.MiddleCenter);

        // 关闭
        MakeBtn(panel.transform, new Vector2(0.85f, 0.88f), new Vector2(0.97f, 0.98f),
            "✕", new Color(0.79f, 0.24f, 0.24f), () => menuController.ClosePopup());

        // 成长阶段
        string stage = rec != null && rec.stage >= 1 && rec.stage <= 3 ? YardDefs.STAGE_NAMES[rec.stage] : "";
        MakeText(panel.transform, "Stage", new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.82f),
            $"状态：{stage}", 18, new Color(0.5f, 0.4f, 0.3f), TextAnchor.MiddleCenter);

        // 亲密度（动态）
        txtIntimacy = MakeText(panel.transform, "Intimacy", new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.68f),
            "", 18, new Color(0.35f, 0.25f, 0.15f), TextAnchor.MiddleCenter);

        // 喂食按钮
        MakeBtn(panel.transform, new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.46f),
            "🐟 喂食（-1 小鱼干，+亲密度）", new Color(0.36f, 0.66f, 0.45f),
            () => OnFeed());

        // 互动按钮
        btnInteract = MakeBtn(panel.transform, new Vector2(0.10f, 0.18f), new Vector2(0.90f, 0.30f),
            "🤝 互动（每日1次，+亲密度）", new Color(0.45f, 0.55f, 0.85f),
            () => OnInteract());

        // toast
        txtToast = MakeText(panel.transform, "Toast", new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.15f),
            "", 16, new Color(0.7f, 0.3f, 0.2f), TextAnchor.MiddleCenter);

        Refresh();
    }

    void Refresh()
    {
        var rec = SaveSystem.Data.pets.Find(p => p.petType == petType);
        if (rec == null) return;
        int intim = rec.intimacy;
        int lv = YardDefs.IntimacyLevel(intim);
        int toNext = YardDefs.IntimacyToNext(intim);
        txtIntimacy.text = toNext > 0
            ? $"亲密度：{YardDefs.INTIMACY_LEVEL_NAMES[lv - 1]} Lv{lv}  ({intim}/1000，还差 {toNext})"
            : $"亲密度：{YardDefs.INTIMACY_LEVEL_NAMES[lv - 1]} Lv{lv}  (已满)";

        bool canInteract = SaveSystem.CanInteractToday(petType);
        btnInteract.interactable = canInteract;
        if (!canInteract)
        {
            var t = btnInteract.GetComponentInChildren<Text>();
            if (t != null) t.text = "🤝 互动（今日已互动）";
        }
    }

    void OnFeed()
    {
        if (SaveSystem.FeedPet(petType))
        {
            var rec = SaveSystem.Data.pets.Find(p => p.petType == petType);
            float mul = YardDefs.IntimacyBonusMul("foodbowl", SaveSystem.GetBuildingLevel("foodbowl"));
            int gain = Mathf.RoundToInt(10 * mul);
            ShowToast($"喂食成功！亲密度 +{gain}");
            Refresh();
        }
        else
        {
            ShowToast("小鱼干不足，无法喂食");
        }
    }

    void OnInteract()
    {
        if (SaveSystem.InteractPet(petType))
        {
            float mul = YardDefs.IntimacyBonusMul("foodbowl", SaveSystem.GetBuildingLevel("foodbowl"));
            int gain = Mathf.RoundToInt(20 * mul);
            ShowToast($"互动成功！亲密度 +{gain}");
            Refresh();
        }
        else
        {
            ShowToast("今日已互动，明天再来~");
        }
    }

    void ShowToast(string msg)
    {
        if (txtToast != null) txtToast.text = msg;
    }

    // ===== 通用辅助 =====
    Text MakeText(Transform parent, string name, Vector2 min, Vector2 max, string text, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(SystemFontText));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max; rt.sizeDelta = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.text = text; t.fontSize = size; t.color = color;
        t.alignment = anchor; GameFont.Apply(t);
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
        var tg = new GameObject("T", typeof(RectTransform), typeof(SystemFontText));
        tg.transform.SetParent(go.transform, false);
        var trt = tg.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var t = tg.GetComponent<Text>();
        t.text = text; t.fontSize = 18; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter; GameFont.Apply(t);
        return btn;
    }
}
