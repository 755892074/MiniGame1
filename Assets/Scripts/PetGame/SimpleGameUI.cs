using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 简化版游戏UI - 纯色底色 + 文字按钮，类似APP风格
/// 运行时动态创建，不依赖美术资源
/// </summary>
public class SimpleGameUI : MonoBehaviour
{
    [Header("配色方案")]
    public Color topBarColor = new Color(0.18f, 0.18f, 0.20f, 0.95f);      // 深灰底色
    public Color buttonColor = new Color(0.30f, 0.32f, 0.38f, 1.00f);        // 按钮底色
    public Color buttonHoverColor = new Color(0.40f, 0.42f, 0.48f, 1.00f);   // 按钮高亮
    public Color textColor = new Color(1.00f, 1.00f, 1.00f, 1.00f);          // 主文字
    public Color subTextColor = new Color(0.70f, 0.70f, 0.70f, 1.00f);       // 次要文字
    public Color accentColor = new Color(0.00f, 0.60f, 0.90f, 1.00f);        // 强调色(蓝)
    public Color dangerColor = new Color(0.90f, 0.30f, 0.30f, 1.00f);       // 危险操作(红)
    public Color successColor = new Color(0.20f, 0.70f, 0.40f, 1.00f);      // 成功(绿)

    private PetGameManager gm;
    private GameObject topBar, bottomBar;
    private Text txtLevel, txtScore, txtStep, txtHintCount;
    private Button btnUndo, btnAddBowl, btnShuffle, btnRestart, btnBack, btnHint;
    private Dictionary<string, Button> toolButtons = new Dictionary<string, Button>();

    public void Init(PetGameManager gameManager, Transform parent)
    {
        gm = gameManager;
        transform.SetParent(parent, false);
        BuildTopBar(parent);
        BuildBottomBar(parent);
        BindEvents();
    }

    // ================== 顶部信息栏 ==================
    void BuildTopBar(Transform parent)
    {
        topBar = new GameObject("TopBar", typeof(RectTransform));
        topBar.transform.SetParent(parent, false);
        var rt = topBar.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(1, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        // 背景图
        var bg = topBar.AddComponent<Image>();
        bg.color = topBarColor;

        // 创建 Canvas
        var canvas = topBar.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10;
        topBar.AddComponent<GraphicRaycaster>();

        // --- 左侧：关卡名 ---
        txtLevel = CreateText(topBar.transform, "LevelText", "关卡 1",
            new Vector2(0.02f, 0.92f), new Vector2(0.30f, 0.99f),
            20, textColor, TextAnchor.MiddleLeft);

        // --- 中间：分数 ---
        txtScore = CreateText(topBar.transform, "ScoreText", "得分: 0/100",
            new Vector2(0.25f, 0.92f), new Vector2(0.75f, 0.99f),
            22, textColor, TextAnchor.MiddleCenter);

        // --- 右侧：步数 ---
        txtStep = CreateText(topBar.transform, "StepText", "步数: 0",
            new Vector2(0.70f, 0.92f), new Vector2(0.98f, 0.99f),
            20, textColor, TextAnchor.MiddleRight);

        // 顶部细线装饰
        var line = new GameObject("TopLine", typeof(RectTransform));
        line.transform.SetParent(topBar.transform, false);
        var lineImg = line.AddComponent<Image>();
        lineImg.color = accentColor;
        var lrt = line.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0, 0.90f);
        lrt.anchorMax = new Vector2(1, 0.905f);
        lrt.anchoredPosition = Vector2.zero;
        lrt.sizeDelta = Vector2.zero;
    }

    // ================== 底部工具栏 ==================
    void BuildBottomBar(Transform parent)
    {
        bottomBar = new GameObject("BottomBar", typeof(RectTransform));
        bottomBar.transform.SetParent(parent, false);
        var rt = bottomBar.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(1, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        // 背景
        var bg = bottomBar.AddComponent<Image>();
        bg.color = topBarColor;

        var canvas = bottomBar.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10;
        bottomBar.AddComponent<GraphicRaycaster>();

        // 底部细线装饰
        var line = new GameObject("BottomLine", typeof(RectTransform));
        line.transform.SetParent(bottomBar.transform, false);
        var lineImg = line.AddComponent<Image>();
        lineImg.color = accentColor;
        var lrt = line.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0, 0.095f);
        lrt.anchorMax = new Vector2(1, 0.10f);
        lrt.anchoredPosition = Vector2.zero;
        lrt.sizeDelta = Vector2.zero;

        // 工具按钮区域 (底部居中)
        float btnYBtm = 0.02f;     // 按钮底部锚点
        float btnYTop = 0.09f;     // 按钮顶部锚点
        float btnW = 0.18f;        // 按钮宽度比例
        float btnGap = 0.02f;      // 按钮间距
        float startX = 0.5f - (4 * btnW + 3 * btnGap) / 2f; // 居中起始

        // 撤
        btnUndo = CreateToolButton(bottomBar.transform, "btnUndo", "撤",
            new Vector2(startX, btnYBtm), new Vector2(startX + btnW, btnYTop));
        toolButtons["undo"] = btnUndo;

        // 加碗
        btnAddBowl = CreateToolButton(bottomBar.transform, "btnAddBowl", "+碗",
            new Vector2(startX + btnW + btnGap, btnYBtm), new Vector2(startX + 2 * btnW + btnGap, btnYTop));
        toolButtons["addBowl"] = btnAddBowl;

        // 打乱
        btnShuffle = CreateToolButton(bottomBar.transform, "btnShuffle", "乱",
            new Vector2(startX + 2 * (btnW + btnGap), btnYBtm), new Vector2(startX + 3 * btnW + 2 * btnGap, btnYTop));
        toolButtons["shuffle"] = btnShuffle;

        // 提示 (右侧独立)
        btnHint = CreateToolButton(bottomBar.transform, "btnHint", "提示",
            new Vector2(0.78f, btnYBtm), new Vector2(0.96f, btnYTop));
        // 提示按钮用强调色
        var hintImg = btnHint.GetComponent<Image>();
        hintImg.color = accentColor;
        toolButtons["hint"] = btnHint;

        // 返回按钮 (左上角)
        btnBack = CreateToolButton(bottomBar.transform, "btnBack", "返回",
            new Vector2(0.02f, btnYBtm), new Vector2(0.15f, btnYTop));
        toolButtons["back"] = btnBack;

        // 重开按钮 (右上角)
        btnRestart = CreateToolButton(bottomBar.transform, "btnRestart", "重开",
            new Vector2(0.85f, btnYBtm), new Vector2(0.98f, btnYTop));
        // 重开用红色
        var restartImg = btnRestart.GetComponent<Image>();
        restartImg.color = dangerColor;
        toolButtons["restart"] = btnRestart;
    }

    // ================== 辅助方法 ==================
    Text CreateText(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax,
        int fontSize, Color color, TextAnchor alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = alignment;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;

        return txt;
    }

    Button CreateToolButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        // 按钮图片
        var img = go.AddComponent<Image>();
        img.color = buttonColor;
        img.sprite = null; // 纯色

        // 按钮组件
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        // 文字
        var txtGO = new GameObject("Label", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var txtRt = txtGO.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.anchoredPosition = Vector2.zero;
        txtRt.sizeDelta = Vector2.zero;

        var txt = txtGO.AddComponent<Text>();
        txt.text = label;
        txt.fontSize = 14;
        txt.color = textColor;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 悬停效果
        var colors = btn.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = new Color(buttonColor.r * 0.8f, buttonColor.g * 0.8f, buttonColor.b * 0.8f, 1);
        btn.colors = colors;

        return btn;
    }

    // ================== 事件绑定 ==================
    void BindEvents()
    {
        if (gm == null) return;

        gm.onScoreChanged.AddListener(_ => UpdateText());
        gm.onSelectionChanged.AddListener(() => UpdateText());
        gm.onPour.AddListener(_ => UpdateText());
        gm.onPetFed.AddListener((p, pts, f) => UpdateText());
    }

    public void UpdateText()
    {
        if (gm == null) return;
        if (txtLevel != null) txtLevel.text = $"关卡 {gm.currentLevelId}";
        if (txtScore != null) txtScore.text = $"得分: {gm.Score}/{gm.MaxScore}";
        if (txtStep != null) txtStep.text = $"步数: {gm.StepCount}";
    }

    public void SetLevel(int levelId)
    {
        if (txtLevel != null) txtLevel.text = $"关卡 {levelId}";
    }

    // 获取按钮引用
    public Button GetButton(string name)
    {
        if (toolButtons.TryGetValue(name, out var btn)) return btn;
        return null;
    }

    // 销毁
    void OnDestroy()
    {
        if (topBar != null) Destroy(topBar);
        if (bottomBar != null) Destroy(bottomBar);
    }
}
