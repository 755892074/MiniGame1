using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 新界面 UI 预制体生成器 (v3)
/// 生成 Splash / Login / MainMenu / Settings / LevelSelect 五个面板预制体
/// 菜单：Tools/铲屎官疯了/生成新界面预制体(v3)
/// </summary>
public class UIPrefabGenV3
{
    const string PREFAB_DIR = "Assets/Resources/PrefabsV2";
    const string ART = "Assets/Art/PetGame";

    static readonly Font FONT = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // 配色
    static readonly Color COL_BG = new Color(0.96f, 0.94f, 0.91f);        // 暖米白
    static readonly Color COL_TOPBAR = new Color(0.94f, 0.92f, 0.88f);     // 顶栏
    static readonly Color COL_MAIN_BTN = new Color(0.89f, 0.48f, 0.32f);   // 珊瑚橙
    static readonly Color COL_SUB_BTN = new Color(0.50f, 0.45f, 0.87f);    // 紫色
    static readonly Color COL_TEXT_DARK = new Color(0.17f, 0.17f, 0.16f);  // 深色文字
    static readonly Color COL_TEXT_MUTED = new Color(0.53f, 0.53f, 0.50f); // 灰色文字
    static readonly Color COL_TEXT_GOLD = new Color(0.94f, 0.62f, 0.15f);  // 金色文字
    static readonly Color COL_WHITE = Color.white;
    static readonly Color COL_OVERLAY = new Color(0, 0, 0, 0.6f);          // 遮罩
    static readonly Color COL_CARD = new Color(0.98f, 0.97f, 0.95f);       // 卡片白
    static readonly Color COL_DANGER = new Color(0.79f, 0.24f, 0.24f);     // 危险红
    static readonly Color COL_LOCK = new Color(0.25f, 0.25f, 0.25f);       // 锁定灰
    static readonly Color COL_UNLOCKED = new Color(0.18f, 0.45f, 0.18f);   // 已通关绿
    static readonly Color COL_AVAILABLE = new Color(0.19f, 0.22f, 0.31f);  // 可玩蓝灰

    [MenuItem("Tools/铲屎官疯了/生成新界面预制体(v3)")]
    static void GenerateAll()
    {
        if (!System.IO.Directory.Exists(PREFAB_DIR))
            System.IO.Directory.CreateDirectory(PREFAB_DIR);

        CreateSplashPanel();
        CreateLoginPanel();
        CreateMainMenuPanel();
        CreateSettingsPanel();
        CreateLevelSelectPanel();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[PrefabGenV3] 5个新界面预制体生成完成</color>");
        EditorUtility.DisplayDialog("v3 预制体生成完成",
            "已生成 5 个新预制体:\n\n" +
            "1. SplashPanel (启动闪屏)\n" +
            "2. LoginPanel (登录/授权)\n" +
            "3. MainMenuPanel (主菜单)\n" +
            "4. SettingsPanel (设置面板)\n" +
            "5. LevelSelectPanel (选关界面)\n\n" +
            "路径: Resources/PrefabsV2/", "好的");
    }

    // ========================================
    // 公共工具方法
    // ========================================

    static Sprite LoadSprite(string subPath)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART}/{subPath}");
        if (s == null) Debug.LogWarning($"[PrefabGenV3] Sprite未找到: {subPath}");
        return s;
    }

    /// <summary>创建带 Image 背景的根面板</summary>
    static GameObject CreatePanelRoot(string name, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(750, 1334);
        go.GetComponent<Image>().color = bgColor;
        return go;
    }

    /// <summary>创建子节点 Image</summary>
    static GameObject AddImage(Transform parent, string name, Vector2 size, Vector2 anchoredPos,
        Vector2 anchorMin, Vector2 anchorMax, Sprite sprite = null, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color ?? COL_WHITE;
        if (sprite) { img.sprite = sprite; img.preserveAspect = true; }
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return go;
    }

    /// <summary>居中锚点的 Image</summary>
    static GameObject AddImageCenter(Transform parent, string name, Vector2 size, Vector2 pos,
        Sprite sprite = null, Color? color = null)
    {
        return AddImage(parent, name, size, pos,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), sprite, color);
    }

    /// <summary>全屏拉伸 Image</summary>
    static GameObject AddImageStretch(Transform parent, string name,
        Sprite sprite = null, Color? color = null)
    {
        return AddImage(parent, name, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.one, sprite, color);
    }

    /// <summary>创建子节点 Text</summary>
    static GameObject AddText(Transform parent, string name, string content, int fontSize,
        Vector2 size, Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax,
        Color? color = null, TextAnchor alignment = TextAnchor.MiddleCenter,
        FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = fontSize;
        t.color = color ?? COL_TEXT_DARK;
        t.alignment = alignment;
        t.font = FONT;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return go;
    }

    static GameObject AddTextCenter(Transform parent, string name, string content, int fontSize,
        Vector2 size, Vector2 pos, Color? color = null, FontStyle style = FontStyle.Normal)
    {
        return AddText(parent, name, content, fontSize, size, pos,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), color, TextAnchor.MiddleCenter, style);
    }

    /// <summary>创建按钮（Image+Button+Text 子物体）</summary>
    static GameObject AddButton(Transform parent, string name, string label,
        Vector2 size, Vector2 pos, Color bgColor, int fontSize = 20,
        Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = bgColor;
        img.sprite = LoadSprite("UI/ui05.png");
        img.type = Image.Type.Sliced;
        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = colors;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
        rt.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        // 文字子物体
        var txtGO = new GameObject("txt", typeof(RectTransform), typeof(Text));
        txtGO.transform.SetParent(go.transform, false);
        var t = txtGO.GetComponent<Text>();
        t.text = label;
        t.fontSize = fontSize;
        t.color = COL_WHITE;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = FONT;
        t.fontStyle = FontStyle.Bold;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        return go;
    }

    /// <summary>创建 Toggle</summary>
    static GameObject AddToggle(Transform parent, string name, string label, Vector2 pos, bool isOn = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(parent, false);
        var tog = go.GetComponent<Toggle>();
        tog.isOn = isOn;

        // 背景（方框）
        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(go.transform, false);
        bgGO.GetComponent<Image>().color = COL_WHITE;
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.5f);
        bgRT.anchorMax = new Vector2(0f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(28, 28);
        bgRT.anchoredPosition = new Vector2(14, 0);

        // 勾选标记
        var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkGO.transform.SetParent(bgGO.transform, false);
        checkGO.GetComponent<Image>().color = new Color(0.18f, 0.45f, 0.18f);
        var cRT = checkGO.GetComponent<RectTransform>();
        cRT.anchorMin = Vector2.zero;
        cRT.anchorMax = Vector2.one;
        cRT.sizeDelta = Vector2.zero;

        tog.targetGraphic = bgGO.GetComponent<Image>();
        tog.graphic = checkGO.GetComponent<Image>();

        // 标签文字
        var lblGO = AddText(go.transform, "Label", label, 14,
            new Vector2(200, 30), new Vector2(20, 0),
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            COL_TEXT_DARK, TextAnchor.MiddleLeft);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(360, 36);
        return go;
    }

    /// <summary>创建 Slider</summary>
    static GameObject AddSlider(Transform parent, string name, Vector2 pos, float defaultValue = 1f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultValue;

        // 背景轨道
        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(go.transform, false);
        bgGO.GetComponent<Image>().color = new Color(0.85f, 0.83f, 0.78f);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.5f);
        bgRT.anchorMax = new Vector2(1f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(0, 12);
        bgRT.anchoredPosition = Vector2.zero;

        // 填充区域
        var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGO.transform.SetParent(go.transform, false);
        var fArt = fillAreaGO.GetComponent<RectTransform>();
        fArt.anchorMin = new Vector2(0f, 0.5f);
        fArt.anchorMax = new Vector2(1f, 0.5f);
        fArt.pivot = new Vector2(0.5f, 0.5f);
        fArt.sizeDelta = new Vector2(-8, 12);

        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        fillGO.GetComponent<Image>().color = COL_MAIN_BTN;
        var fRT = fillGO.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;
        fRT.sizeDelta = Vector2.zero;

        // 手柄区域
        var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaGO.transform.SetParent(go.transform, false);
        var hArt = handleAreaGO.GetComponent<RectTransform>();
        hArt.anchorMin = new Vector2(0f, 0.5f);
        hArt.anchorMax = new Vector2(1f, 0.5f);
        hArt.pivot = new Vector2(0.5f, 0.5f);
        hArt.sizeDelta = new Vector2(-20, 20);

        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        handleGO.GetComponent<Image>().color = COL_WHITE;
        var hRT = handleGO.GetComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(20, 20);

        slider.targetGraphic = handleGO.GetComponent<Image>();
        slider.fillRect = fillGO.GetComponent<RectTransform>();
        slider.handleRect = hRT;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(200, 24);
        return go;
    }

    static void Save(GameObject go, string name)
    {
        string path = $"{PREFAB_DIR}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // ========================================
    // 1. Splash 面板
    // ========================================
    static void CreateSplashPanel()
    {
        var root = CreatePanelRoot("SplashPanel", COL_BG);

        // Logo（占位用宠物图）
        var logoSprite = LoadSprite("pets/cat/happy.png");
        AddImageCenter(root.transform, "imgLogo", new Vector2(240, 240),
            new Vector2(0, 180), logoSprite, COL_WHITE);

        // 标题
        AddTextCenter(root.transform, "txtTitle", "疯狂铲屎官", 42,
            new Vector2(500, 60), new Vector2(0, -60),
            COL_TEXT_DARK, FontStyle.Bold);

        // 副标题
        AddTextCenter(root.transform, "txtSubtitle", "Crazy Pooper", 18,
            new Vector2(400, 30), new Vector2(0, -110),
            COL_TEXT_MUTED);

        // 加载进度条背景
        AddImageCenter(root.transform, "imgBarBg", new Vector2(280, 8),
            new Vector2(0, -200), null, new Color(0.88f, 0.86f, 0.82f));

        // 加载进度条填充
        var barFill = AddImageCenter(root.transform, "imgBarFill", new Vector2(0, 8),
            new Vector2(-140, -200), null, COL_MAIN_BTN);
        // 左对齐锚点
        var brt = barFill.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0f, 0.5f);

        // 加载文字
        AddTextCenter(root.transform, "txtLoading", "加载中...", 14,
            new Vector2(200, 24), new Vector2(0, -230),
            COL_TEXT_MUTED);

        // 版本号
        AddText(root.transform, "txtVersion", "v1.0.0", 12,
            new Vector2(100, 24), new Vector2(-30, 30),
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            COL_TEXT_MUTED, TextAnchor.LowerRight);

        Save(root, "SplashPanel");
    }

    // ========================================
    // 2. Login 面板
    // ========================================
    static void CreateLoginPanel()
    {
        var root = CreatePanelRoot("LoginPanel", COL_BG);

        // 小Logo
        var logoSprite = LoadSprite("pets/cat/happy.png");
        AddImageCenter(root.transform, "imgMiniLogo", new Vector2(120, 120),
            new Vector2(0, 250), logoSprite, COL_WHITE);

        // 标题
        AddTextCenter(root.transform, "txtTitle", "疯狂铲屎官", 36,
            new Vector2(500, 50), new Vector2(0, 130),
            COL_TEXT_DARK, FontStyle.Bold);

        // 游客模式按钮
        AddButton(root.transform, "btnGuestLogin", "游客模式开始",
            new Vector2(320, 64), new Vector2(0, 0), COL_MAIN_BTN, 22);

        // 抖音授权按钮
        AddButton(root.transform, "btnDouyinLogin", "抖音授权登录",
            new Vector2(320, 64), new Vector2(0, -84), COL_SUB_BTN, 20);

        // 隐私 Toggle
        AddToggle(root.transform, "tgPrivacy", "已阅读并同意", new Vector2(0, -180));

        // 协议链接容器
        var linksPanel = new GameObject("pnlPrivacyLinks", typeof(RectTransform));
        linksPanel.transform.SetParent(root.transform, false);
        var lprt = linksPanel.GetComponent<RectTransform>();
        lprt.anchorMin = new Vector2(0.5f, 0.5f);
        lprt.anchorMax = new Vector2(0.5f, 0.5f);
        lprt.anchoredPosition = new Vector2(0, -215);
        lprt.sizeDelta = new Vector2(400, 24);

        var hLayout = linksPanel.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.spacing = 5;

        // 用户协议链接
        var uaGO = new GameObject("btnUserAgreement", typeof(RectTransform), typeof(Text));
        uaGO.transform.SetParent(linksPanel.transform, false);
        var uaT = uaGO.AddComponent<Text>();
        uaT.text = "《用户协议》";
        uaT.fontSize = 14;
        uaT.color = new Color(0.24f, 0.38f, 0.68f);
        uaT.font = FONT;
        uaT.alignment = TextAnchor.MiddleCenter;
        uaT.horizontalOverflow = HorizontalWrapMode.Overflow;
        uaT.raycastTarget = true;
        var uaRT = uaGO.GetComponent<RectTransform>();
        uaRT.sizeDelta = new Vector2(120, 24);
        uaGO.AddComponent<Button>();

        var andT = new GameObject("txtAnd", typeof(RectTransform), typeof(Text));
        andT.transform.SetParent(linksPanel.transform, false);
        var at = andT.GetComponent<Text>();
        at.text = "和";
        at.fontSize = 14;
        at.color = COL_TEXT_MUTED;
        at.font = FONT;
        at.alignment = TextAnchor.MiddleCenter;
        at.horizontalOverflow = HorizontalWrapMode.Overflow;
        andT.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 24);

        // 隐私政策链接
        var ppGO = new GameObject("btnPrivacyPolicy", typeof(RectTransform), typeof(Text));
        ppGO.transform.SetParent(linksPanel.transform, false);
        var ppT = ppGO.AddComponent<Text>();
        ppT.text = "《隐私政策》";
        ppT.fontSize = 14;
        ppT.color = new Color(0.24f, 0.38f, 0.68f);
        ppT.font = FONT;
        ppT.alignment = TextAnchor.MiddleCenter;
        ppT.horizontalOverflow = HorizontalWrapMode.Overflow;
        ppT.raycastTarget = true;
        var ppRT = ppGO.GetComponent<RectTransform>();
        ppRT.sizeDelta = new Vector2(120, 24);
        ppGO.AddComponent<Button>();

        // 隐私弹窗（默认隐藏）
        CreatePrivacyPopup(root.transform);

        Save(root, "LoginPanel");
    }

    static void CreatePrivacyPopup(Transform parent)
    {
        var overlay = AddImageStretch(parent, "pnlPrivacyPopup", null, COL_OVERLAY);

        var card = AddImageCenter(overlay.transform, "imgCardBg", new Vector2(580, 720),
            Vector2.zero, null, COL_CARD);

        // 卡片标题
        AddText(card.transform, "txtPopupTitle", "隐私政策概要", 24,
            new Vector2(500, 40), new Vector2(0, 320),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_DARK, TextAnchor.MiddleCenter, FontStyle.Bold);

        // 卡片内容（可滚动）
        var scrollGO = new GameObject("txtPopupScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(card.transform, false);
        var srt = scrollGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = new Vector2(0, 40);
        srt.sizeDelta = new Vector2(520, 440);

        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(Text));
        contentGO.transform.SetParent(scrollGO.transform, false);
        var ct = contentGO.GetComponent<Text>();
        ct.text = "感谢您使用「疯狂铲屎官」。\n\n" +
                  "我们重视您的隐私保护。本游戏仅收集必要的游戏数据（如关卡进度、设置偏好）用于提供游戏服务。\n\n" +
                  "1. 本地存储：您的游戏进度保存在设备本地。\n" +
                  "2. 云端同步：在抖音平台授权后，存档将同步至云端以防丢失。\n" +
                  "3. 我们不会收集您的个人敏感信息。\n" +
                  "4. 广告：本游戏包含广告，广告内容由广告平台提供。\n\n" +
                  "点击\"同意并继续\"表示您已了解并同意以上内容。";
        ct.fontSize = 15;
        ct.color = COL_TEXT_DARK;
        ct.font = FONT;
        ct.alignment = TextAnchor.UpperLeft;
        ct.horizontalOverflow = HorizontalWrapMode.Wrap;
        ct.verticalOverflow = VerticalWrapMode.Overflow;
        ct.lineSpacing = 1.4f;
        ct.raycastTarget = true;
        var crt = contentGO.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 1f);
        crt.anchorMax = new Vector2(0.5f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.sizeDelta = new Vector2(520, 600);

        // Content Size Fitter
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollGO.GetComponent<ScrollRect>();
        sr.content = crt;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 20;

        // 同意按钮
        AddButton(card.transform, "btnAgree", "同意并继续",
            new Vector2(240, 52), new Vector2(-70, -300), COL_MAIN_BTN, 18);

        // 不同意按钮
        AddButton(card.transform, "btnDisagree", "不同意",
            new Vector2(180, 52), new Vector2(110, -300), COL_LOCK, 16);

        overlay.SetActive(false);
    }

    // ========================================
    // 3. 主菜单面板
    // ========================================
    static void CreateMainMenuPanel()
    {
        var root = CreatePanelRoot("MainMenuPanel", COL_BG);

        // === 顶部玩家信息条 ===
        var playerBar = AddImage(root.transform, "pnlPlayerBar", Vector2.zero, Vector2.zero,
            new Vector2(0f, 1f), new Vector2(1f, 1f), null, COL_TOPBAR);
        var pbRT = playerBar.GetComponent<RectTransform>();
        pbRT.pivot = new Vector2(0.5f, 1f);
        pbRT.sizeDelta = new Vector2(0, 64);

        // 称号（左）
        AddText(playerBar.transform, "txtTitle", "初级铲屎官", 16,
            new Vector2(180, 30), new Vector2(-150, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_GOLD, TextAnchor.MiddleLeft, FontStyle.Bold);

        // 小鱼干（中右）
        AddText(playerBar.transform, "txtFish", "128", 16,
            new Vector2(120, 30), new Vector2(30, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Color(0.94f, 0.72f, 0.2f), TextAnchor.MiddleRight);

        // 总星数（右）
        AddText(playerBar.transform, "txtStars", "15", 16,
            new Vector2(120, 30), new Vector2(150, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Color(0.94f, 0.62f, 0.15f), TextAnchor.MiddleRight);

        // === 主视觉区域 ===
        var heroSprite = LoadSprite("UI/ui06.png");
        var hero = AddImageCenter(root.transform, "imgHeroArt", new Vector2(400, 300),
            new Vector2(0, 180), heroSprite, COL_WHITE);

        // === 继续游戏按钮（主按钮，大号）===
        AddButton(root.transform, "btnContinue", "继续游戏",
            new Vector2(340, 70), new Vector2(0, -70), COL_MAIN_BTN, 26);

        // === 第一行双按钮 ===
        AddButton(root.transform, "btnLevelSelect", "选择关卡",
            new Vector2(160, 56), new Vector2(-90, -170), COL_SUB_BTN, 18);
        AddButton(root.transform, "btnYard", "我的小院",
            new Vector2(160, 56), new Vector2(90, -170), COL_SUB_BTN, 18);

        // === 第二行双按钮 ===
        AddButton(root.transform, "btnSettings", "设置",
            new Vector2(160, 56), new Vector2(-90, -250), new Color(0.42f, 0.42f, 0.40f), 18);
        AddButton(root.transform, "btnAchievement", "成就",
            new Vector2(160, 56), new Vector2(90, -250), new Color(0.42f, 0.42f, 0.40f), 18);

        // === 右上角快捷设置 ===
        var gearSprite = LoadSprite("UI/ui04.png");
        var gearBtn = AddImage(root.transform, "btnQuickSettings", new Vector2(44, 44),
            new Vector2(-30, -30), new Vector2(1f, 1f), new Vector2(1f, 1f), gearSprite, COL_WHITE);
        gearBtn.AddComponent<Button>();

        Save(root, "MainMenuPanel");
    }

    // ========================================
    // 4. 设置面板
    // ========================================
    static void CreateSettingsPanel()
    {
        // 遮罩根
        var root = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        var rrt = root.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(750, 1334);
        root.GetComponent<Image>().color = COL_OVERLAY;

        // 卡片
        var card = AddImageCenter(root.transform, "imgCardBg", new Vector2(580, 760),
            Vector2.zero, null, COL_CARD);

        // 标题
        AddText(card.transform, "txtTitle", "设置", 28,
            new Vector2(500, 44), new Vector2(0, 340),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_DARK, TextAnchor.MiddleCenter, FontStyle.Bold);

        // 分隔线
        AddImageCenter(card.transform, "imgDivider1", new Vector2(500, 2),
            new Vector2(0, 300), null, new Color(0.85f, 0.83f, 0.78f));

        // === 音频区域 ===
        // 音乐
        AddText(card.transform, "txtMusicLabel", "音乐", 16,
            new Vector2(100, 30), new Vector2(-160, 250),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_DARK, TextAnchor.MiddleLeft);
        AddSlider(card.transform, "sldMusicVol", new Vector2(60, 250), 1f);

        // 音效
        AddText(card.transform, "txtSfxLabel", "音效", 16,
            new Vector2(100, 30), new Vector2(-160, 200),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_DARK, TextAnchor.MiddleLeft);
        AddSlider(card.transform, "sldSfxVol", new Vector2(60, 200), 1f);

        // 振动
        AddText(card.transform, "txtVibrationLabel", "振动", 16,
            new Vector2(100, 30), new Vector2(-160, 150),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_DARK, TextAnchor.MiddleLeft);
        var vibToggle = AddToggle(card.transform, "tgVibration", "", new Vector2(130, 150));

        // 分隔线
        AddImageCenter(card.transform, "imgDivider2", new Vector2(500, 2),
            new Vector2(0, 100), null, new Color(0.85f, 0.83f, 0.78f));

        // === 链接区域 ===
        AddButton(card.transform, "btnAccount", "账号管理",
            new Vector2(500, 48), new Vector2(0, 50), new Color(0.92f, 0.90f, 0.86f), 18);
        AddButton(card.transform, "btnPrivacy", "隐私政策",
            new Vector2(500, 48), new Vector2(0, -10), new Color(0.92f, 0.90f, 0.86f), 18);
        AddButton(card.transform, "btnAgreement", "用户协议",
            new Vector2(500, 48), new Vector2(0, -70), new Color(0.92f, 0.90f, 0.86f), 18);

        // 危险操作
        AddButton(card.transform, "btnResetSave", "重置存档",
            new Vector2(240, 48), new Vector2(0, -150), COL_DANGER, 16);

        // 关闭按钮
        var closeGO = new GameObject("btnClose", typeof(RectTransform), typeof(Image), typeof(Text), typeof(Button));
        closeGO.transform.SetParent(card.transform, false);
        closeGO.GetComponent<Image>().color = new Color(0.6f, 0.58f, 0.55f);
        var closeT = closeGO.GetComponent<Text>();
        closeT.text = "X";
        closeT.fontSize = 20;
        closeT.color = COL_WHITE;
        closeT.font = FONT;
        closeT.alignment = TextAnchor.MiddleCenter;
        closeT.fontStyle = FontStyle.Bold;
        var clRT = closeGO.GetComponent<RectTransform>();
        clRT.anchorMin = new Vector2(1f, 1f);
        clRT.anchorMax = new Vector2(1f, 1f);
        clRT.pivot = new Vector2(0.5f, 0.5f);
        clRT.anchoredPosition = new Vector2(-28, -28);
        clRT.sizeDelta = new Vector2(40, 40);

        Save(root, "SettingsPanel");
    }

    // ========================================
    // 5. 选关面板
    // ========================================
    static void CreateLevelSelectPanel()
    {
        var root = CreatePanelRoot("LevelSelectPanel", new Color(0.1f, 0.08f, 0.15f));

        // 标题
        AddText(root.transform, "txtTitle", "疯狂铲屎官", 40,
            new Vector2(500, 50), new Vector2(0, 560),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_WHITE, TextAnchor.MiddleCenter, FontStyle.Bold);

        // 玩家信息
        AddText(root.transform, "txtPlayerInfo", "", 18,
            new Vector2(600, 30), new Vector2(0, 500),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_GOLD, TextAnchor.MiddleCenter);

        // 返回按钮
        AddButton(root.transform, "btnBack", "返回",
            new Vector2(100, 44), new Vector2(-300, 580),
            new Color(0.3f, 0.3f, 0.35f), 16,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

        // ScrollView — 关卡网格
        var scrollGO = new GameObject("LevelScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(root.transform, false);
        scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        var sRT = scrollGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0f);
        sRT.anchorMax = new Vector2(1f, 1f);
        sRT.pivot = new Vector2(0.5f, 0.5f);
        sRT.offsetMin = new Vector2(20, 20);
        sRT.offsetMax = new Vector2(-20, -100);

        // Content
        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(scrollGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0.5f, 1f);
        contentRT.anchorMax = new Vector2(0.5f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;

        var grid = contentGO.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(150, 100);
        grid.spacing = new Vector2(15, 15);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;

        var csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.MinSize;

        var sr = scrollGO.GetComponent<ScrollRect>();
        sr.content = contentRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 20;

        Save(root, "LevelSelectPanel");
    }
}
