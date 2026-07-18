using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 新界面 UI 预制体生成器 (v3)
/// 生成 Splash / Login / MainMenu / Settings / LevelSelect 五个面板预制体
/// 菜单：铲屎官疯了/生成新界面预制体(v3)
/// </summary>
public class UIPrefabGenV3
{
    const string PREFAB_DIR = "Assets/Resources/PrefabsV2";
    const string ART = "Assets/Art/PetGame";

    static Font _font;
    static Font FONT
    {
        get
        {
            if (_font == null)
                _font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/AlibabaPuHuiTi-Regular.ttf");
            return _font;
        }
    }

    // 配色
    static readonly Color COL_BG = new Color(0.96f, 0.94f, 0.91f);
    static readonly Color COL_TOPBAR = new Color(0.94f, 0.92f, 0.88f);
    static readonly Color COL_MAIN_BTN = new Color(0.89f, 0.48f, 0.32f);
    static readonly Color COL_SUB_BTN = new Color(0.50f, 0.45f, 0.87f);
    static readonly Color COL_TEXT_DARK = new Color(0.17f, 0.17f, 0.16f);
    static readonly Color COL_TEXT_MUTED = new Color(0.53f, 0.53f, 0.50f);
    static readonly Color COL_TEXT_GOLD = new Color(0.94f, 0.62f, 0.15f);
    static readonly Color COL_WHITE = Color.white;
    static readonly Color COL_OVERLAY = new Color(0, 0, 0, 0.6f);
    static readonly Color COL_CARD = new Color(0.98f, 0.97f, 0.95f);
    static readonly Color COL_DANGER = new Color(0.79f, 0.24f, 0.24f);
    static readonly Color COL_LOCK = new Color(0.25f, 0.25f, 0.25f);

    [MenuItem("铲屎官疯了/生成新界面预制体(v3)")]
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
        PetGameGenUtil.Success(
            "已生成 5 个新预制体:\n\n" +
            "1. SplashPanel (启动闪屏)\n" +
            "2. LoginPanel (登录/授权)\n" +
            "3. MainMenuPanel (主菜单)\n" +
            "4. SettingsPanel (设置面板)\n" +
            "5. LevelSelectPanel (选关界面)\n\n" +
            "路径: Resources/PrefabsV2/");
    }

    // ========================================
    // 工具方法 — 统一用 new GameObject(name, typeof(RectTransform)) + AddComponent
    // ========================================

    static Sprite LoadSprite(string subPath)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART}/{subPath}");
        return s;
    }

    /// <summary>铺满拉伸的背景图（放到底层）</summary>
    static void AddBackground(Transform parent, string bgSubPath)
    {
        var go = NewGO("Background", parent);
        go.transform.SetAsFirstSibling();
        var img = go.AddComponent<Image>();
        var sp = LoadSprite(bgSubPath);
        if (sp) { img.sprite = sp; img.preserveAspect = false; }
        else img.color = COL_BG;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }

    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject CreatePanelRoot(string name, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.AddComponent<Image>().color = bgColor;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(750, 1334);
        return go;
    }

    static Image AddImage(Transform parent, string name, Vector2 size, Vector2 pos,
        Vector2 anchorMin, Vector2 anchorMax, Sprite sprite = null, Color? color = null)
    {
        var go = NewGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color = color ?? COL_WHITE;
        if (sprite) { img.sprite = sprite; img.preserveAspect = true; }
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    static Image AddImageCenter(Transform parent, string name, Vector2 size, Vector2 pos,
        Sprite sprite = null, Color? color = null)
    {
        return AddImage(parent, name, size, pos,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), sprite, color);
    }

    static Image AddImageStretch(Transform parent, string name,
        Sprite sprite = null, Color? color = null)
    {
        return AddImage(parent, name, Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.one, sprite, color);
    }

    static Text AddText(Transform parent, string name, string content, int fontSize,
        Vector2 size, Vector2 pos, Vector2 anchorMin, Vector2 anchorMax,
        Color? color = null, TextAnchor alignment = TextAnchor.MiddleCenter,
        FontStyle style = FontStyle.Normal)
    {
        var go = NewGO(name, parent);
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
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }

    static Text AddTextCenter(Transform parent, string name, string content, int fontSize,
        Vector2 size, Vector2 pos, Color? color = null, FontStyle style = FontStyle.Normal)
    {
        return AddText(parent, name, content, fontSize, size, pos,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), color, TextAnchor.MiddleCenter, style);
    }

    static Button AddButton(Transform parent, string name, string label,
        Vector2 size, Vector2 pos, Color bgColor, int fontSize = 20,
        Vector2? anchorMin = null, Vector2? anchorMax = null,
        Sprite bgSprite = null)
    {
        var go = NewGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        if (bgSprite)
        {
            img.sprite = bgSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true; // 防止按钮图被拉伸变形
        }
        var btn = go.AddComponent<Button>();
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

        // 子文字
        var txtGO = NewGO("txt", go.transform);
        var t = txtGO.AddComponent<Text>();
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
        return btn;
    }

    static Toggle AddToggle(Transform parent, string name, string label, Vector2 pos, bool isOn = false)
    {
        var go = NewGO(name, parent);
        var tog = go.AddComponent<Toggle>();
        tog.isOn = isOn;

        var bgGO = NewGO("Background", go.transform);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = COL_WHITE;
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.5f);
        bgRT.anchorMax = new Vector2(0f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(28, 28);
        bgRT.anchoredPosition = new Vector2(14, 0);

        var checkGO = NewGO("Checkmark", bgGO.transform);
        var checkImg = checkGO.AddComponent<Image>();
        checkImg.color = new Color(0.18f, 0.45f, 0.18f);
        var cRT = checkGO.GetComponent<RectTransform>();
        cRT.anchorMin = Vector2.zero;
        cRT.anchorMax = Vector2.one;
        cRT.sizeDelta = Vector2.zero;

        tog.targetGraphic = bgImg;
        tog.graphic = checkImg;

        if (!string.IsNullOrEmpty(label))
        {
            AddText(go.transform, "Label", label, 14,
                new Vector2(200, 30), new Vector2(20, 0),
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                COL_TEXT_DARK, TextAnchor.MiddleLeft);
        }

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(360, 36);
        return tog;
    }

    static Slider AddSlider(Transform parent, string name, Vector2 pos, float defaultValue = 1f)
    {
        var go = NewGO(name, parent);
        var slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultValue;

        // 背景轨道
        var bgGO = NewGO("Background", go.transform);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.85f, 0.83f, 0.78f);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.5f);
        bgRT.anchorMax = new Vector2(1f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(0, 12);
        bgRT.anchoredPosition = Vector2.zero;

        // 填充区域
        var fillAreaGO = NewGO("Fill Area", go.transform);
        var fArt = fillAreaGO.GetComponent<RectTransform>();
        fArt.anchorMin = new Vector2(0f, 0.5f);
        fArt.anchorMax = new Vector2(1f, 0.5f);
        fArt.pivot = new Vector2(0.5f, 0.5f);
        fArt.sizeDelta = new Vector2(-8, 12);

        var fillGO = NewGO("Fill", fillAreaGO.transform);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = COL_MAIN_BTN;
        var fRT = fillGO.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;
        fRT.sizeDelta = Vector2.zero;

        // 手柄
        var handleAreaGO = NewGO("Handle Slide Area", go.transform);
        var hArt = handleAreaGO.GetComponent<RectTransform>();
        hArt.anchorMin = new Vector2(0f, 0.5f);
        hArt.anchorMax = new Vector2(1f, 0.5f);
        hArt.pivot = new Vector2(0.5f, 0.5f);
        hArt.sizeDelta = new Vector2(-20, 20);

        var handleGO = NewGO("Handle", handleAreaGO.transform);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = COL_WHITE;
        var hRT = handleGO.GetComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(20, 20);

        slider.targetGraphic = handleImg;
        slider.fillRect = fRT;
        slider.handleRect = hRT;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(200, 24);
        return slider;
    }

    /// <summary>文字链接按钮（Text+Button，无背景图）</summary>
    static Button AddTextLink(Transform parent, string name, string label, Color color)
    {
        var go = NewGO(name, parent);
        var t = go.AddComponent<Text>();
        t.text = label;
        t.fontSize = 14;
        t.color = color;
        t.font = FONT;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = true;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 24);
        return go.AddComponent<Button>();
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
    // 1. Splash
    // ========================================
    static void CreateSplashPanel()
    {
        var root = CreatePanelRoot("SplashPanel", COL_BG);
        AddBackground(root.transform, "backgrounds/bg_room_cozy.png");

        var logoSprite = LoadSprite("pets/cat/happy.png");
        AddImageCenter(root.transform, "imgLogo", new Vector2(240, 240),
            new Vector2(0, 180), logoSprite, COL_WHITE);

        AddTextCenter(root.transform, "txtTitle", "疯狂铲屎官", 42,
            new Vector2(500, 60), new Vector2(0, -60), COL_TEXT_DARK, FontStyle.Bold);

        AddTextCenter(root.transform, "txtSubtitle", "Crazy Pooper", 18,
            new Vector2(400, 30), new Vector2(0, -110), COL_TEXT_MUTED);

        AddImageCenter(root.transform, "imgBarBg", new Vector2(280, 8),
            new Vector2(0, -200), null, new Color(0.88f, 0.86f, 0.82f));

        var barFill = AddImageCenter(root.transform, "imgBarFill", new Vector2(0, 8),
            new Vector2(-140, -200), null, COL_MAIN_BTN);
        var brt = barFill.rectTransform;
        brt.pivot = new Vector2(0f, 0.5f);

        AddTextCenter(root.transform, "txtLoading", "加载中...", 14,
            new Vector2(200, 24), new Vector2(0, -230), COL_TEXT_MUTED);

        AddText(root.transform, "txtVersion", "v1.0.0", 12,
            new Vector2(100, 24), new Vector2(-30, 30),
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            COL_TEXT_MUTED, TextAnchor.LowerRight);

        Save(root, "SplashPanel");
    }

    // ========================================
    // 2. Login
    // ========================================
    static void CreateLoginPanel()
    {
        var root = CreatePanelRoot("LoginPanel", COL_BG);
        AddBackground(root.transform, "backgrounds/bg_room_cozy.png");

        var logoSprite = LoadSprite("pets/cat/happy.png");
        AddImageCenter(root.transform, "imgMiniLogo", new Vector2(120, 120),
            new Vector2(0, 250), logoSprite, COL_WHITE);

        AddTextCenter(root.transform, "txtTitle", "疯狂铲屎官", 36,
            new Vector2(500, 50), new Vector2(0, 130), COL_TEXT_DARK, FontStyle.Bold);

        AddButton(root.transform, "btnGuestLogin", "游客模式开始",
            new Vector2(320, 64), new Vector2(0, 0), COL_MAIN_BTN, 22);

        AddButton(root.transform, "btnDouyinLogin", "抖音授权登录",
            new Vector2(320, 64), new Vector2(0, -84), COL_SUB_BTN, 20);

        AddToggle(root.transform, "tgPrivacy", "已阅读并同意", new Vector2(0, -180));

        // 协议链接
        var linksPanel = NewGO("pnlPrivacyLinks", root.transform);
        var lprt = linksPanel.GetComponent<RectTransform>();
        lprt.anchorMin = new Vector2(0.5f, 0.5f);
        lprt.anchorMax = new Vector2(0.5f, 0.5f);
        lprt.anchoredPosition = new Vector2(0, -215);
        lprt.sizeDelta = new Vector2(400, 24);
        var hLayout = linksPanel.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.spacing = 5;

        var linkColor = new Color(0.24f, 0.38f, 0.68f);
        AddTextLink(linksPanel.transform, "btnUserAgreement", "《用户协议》", linkColor);

        var andT = NewGO("txtAnd", linksPanel.transform);
        var at = andT.AddComponent<Text>();
        at.text = "和";
        at.fontSize = 14;
        at.color = COL_TEXT_MUTED;
        at.font = FONT;
        at.alignment = TextAnchor.MiddleCenter;
        at.horizontalOverflow = HorizontalWrapMode.Overflow;
        andT.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 24);

        AddTextLink(linksPanel.transform, "btnPrivacyPolicy", "《隐私政策》", linkColor);

        CreatePrivacyPopup(root.transform);

        Save(root, "LoginPanel");
    }

    static void CreatePrivacyPopup(Transform parent)
    {
        var overlay = AddImageStretch(parent, "pnlPrivacyPopup", null, COL_OVERLAY);
        var overlayGO = overlay.gameObject;

        var card = AddImageCenter(overlayGO.transform, "imgCardBg", new Vector2(580, 720),
            Vector2.zero, null, COL_CARD);
        var cardT = card.transform;

        AddText(cardT, "txtPopupTitle", "隐私政策概要", 24,
            new Vector2(500, 40), new Vector2(0, 320),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_DARK, TextAnchor.MiddleCenter, FontStyle.Bold);

        // 可滚动内容
        var scrollGO = NewGO("txtPopupScroll", cardT);
        var srt = scrollGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = new Vector2(0, 40);
        srt.sizeDelta = new Vector2(520, 440);

        var contentGO = NewGO("Content", scrollGO.transform);
        var ct = contentGO.AddComponent<Text>();
        ct.text = "感谢您使用「疯狂铲屎官」。\n\n" +
                  "我们重视您的隐私保护。本游戏仅收集必要的游戏数据" +
                  "（如关卡进度、设置偏好）用于提供游戏服务。\n\n" +
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
        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollGO.AddComponent<ScrollRect>();
        sr.content = crt;
        sr.horizontal = false;
        sr.vertical = true;

        AddButton(cardT, "btnAgree", "同意并继续",
            new Vector2(240, 52), new Vector2(-70, -300), COL_MAIN_BTN, 18);
        AddButton(cardT, "btnDisagree", "不同意",
            new Vector2(180, 52), new Vector2(110, -300), COL_LOCK, 16);

        overlayGO.SetActive(false);
    }

    // ========================================
    // 3. MainMenu
    // ========================================
    static void CreateMainMenuPanel()
    {
        var root = CreatePanelRoot("MainMenuPanel", COL_BG);
        AddBackground(root.transform, "backgrounds/bg_room_cozy.png");

        // 顶部信息条
        var playerBar = AddImage(root.transform, "pnlPlayerBar", Vector2.zero, Vector2.zero,
            new Vector2(0f, 1f), new Vector2(1f, 1f), null, COL_TOPBAR);
        playerBar.rectTransform.pivot = new Vector2(0.5f, 1f);
        playerBar.rectTransform.sizeDelta = new Vector2(0, 64);

        AddText(playerBar.transform, "txtTitle", "初级铲屎官", 16,
            new Vector2(180, 30), new Vector2(-150, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_GOLD, TextAnchor.MiddleLeft, FontStyle.Bold);

        AddText(playerBar.transform, "txtFish", "128", 16,
            new Vector2(120, 30), new Vector2(30, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Color(0.94f, 0.72f, 0.2f), TextAnchor.MiddleRight);

        AddText(playerBar.transform, "txtStars", "15", 16,
            new Vector2(120, 30), new Vector2(150, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Color(0.94f, 0.62f, 0.15f), TextAnchor.MiddleRight);

        // 主视觉
        var heroSprite = LoadSprite("UI/ui06.png");
        AddImageCenter(root.transform, "imgHeroArt", new Vector2(400, 300),
            new Vector2(0, 180), heroSprite, COL_WHITE);

        // 按钮：用对应元素图，避免拉伸进度条（ui05）
        var spStart = LoadSprite("UI/elements/item_00.png");
        AddButton(root.transform, "btnContinue", "继续游戏",
            new Vector2(340, 70), new Vector2(0, -70), COL_MAIN_BTN, 26, null, null, spStart);

        var spLevel = LoadSprite("UI/elements/item_01.png");
        AddButton(root.transform, "btnLevelSelect", "选择关卡",
            new Vector2(160, 56), new Vector2(-90, -170), COL_SUB_BTN, 18, null, null, spLevel);
        AddButton(root.transform, "btnYard", "我的小院",
            new Vector2(160, 56), new Vector2(90, -170), COL_SUB_BTN, 18);

        var spSettings = LoadSprite("UI/elements/item_02.png");
        AddButton(root.transform, "btnSettings", "设置",
            new Vector2(160, 56), new Vector2(-90, -250), new Color(0.42f, 0.42f, 0.40f), 18, null, null, spSettings);
        AddButton(root.transform, "btnAchievement", "成就",
            new Vector2(160, 56), new Vector2(90, -250), new Color(0.42f, 0.42f, 0.40f), 18);

        // 右上齿轮
        var gearSprite = LoadSprite("UI/ui04.png");
        var gearImg = AddImage(root.transform, "btnQuickSettings", new Vector2(44, 44),
            new Vector2(-30, -30), new Vector2(1f, 1f), new Vector2(1f, 1f), gearSprite, COL_WHITE);
        gearImg.gameObject.AddComponent<Button>();

        Save(root, "MainMenuPanel");
    }

    // ========================================
    // 4. Settings
    // ========================================
    static void CreateSettingsPanel()
    {
        var root = NewGO("SettingsPanel", null);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = COL_OVERLAY;
        var rrt = root.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(750, 1334);

        var card = AddImageCenter(root.transform, "imgCardBg", new Vector2(580, 760),
            Vector2.zero, null, COL_CARD);
        var cardT = card.transform;

        AddText(cardT, "txtTitle", "设置", 28,
            new Vector2(500, 44), new Vector2(0, 340),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_DARK, TextAnchor.MiddleCenter, FontStyle.Bold);

        AddImageCenter(cardT, "imgDivider1", new Vector2(500, 2),
            new Vector2(0, 300), null, new Color(0.85f, 0.83f, 0.78f));

        // 音频
        AddText(cardT, "txtMusicLabel", "音乐", 16,
            new Vector2(100, 30), new Vector2(-160, 250),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_DARK, TextAnchor.MiddleLeft);
        AddSlider(cardT, "sldMusicVol", new Vector2(60, 250), 1f);

        AddText(cardT, "txtSfxLabel", "音效", 16,
            new Vector2(100, 30), new Vector2(-160, 200),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_DARK, TextAnchor.MiddleLeft);
        AddSlider(cardT, "sldSfxVol", new Vector2(60, 200), 1f);

        AddText(cardT, "txtVibrationLabel", "振动", 16,
            new Vector2(100, 30), new Vector2(-160, 150),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_DARK, TextAnchor.MiddleLeft);
        AddToggle(cardT, "tgVibration", "", new Vector2(130, 150));

        AddImageCenter(cardT, "imgDivider2", new Vector2(500, 2),
            new Vector2(0, 100), null, new Color(0.85f, 0.83f, 0.78f));

        AddButton(cardT, "btnAccount", "账号管理",
            new Vector2(500, 48), new Vector2(0, 50), new Color(0.92f, 0.90f, 0.86f), 18);
        AddButton(cardT, "btnPrivacy", "隐私政策",
            new Vector2(500, 48), new Vector2(0, -10), new Color(0.92f, 0.90f, 0.86f), 18);
        AddButton(cardT, "btnAgreement", "用户协议",
            new Vector2(500, 48), new Vector2(0, -70), new Color(0.92f, 0.90f, 0.86f), 18);

        AddButton(cardT, "btnResetSave", "重置存档",
            new Vector2(240, 48), new Vector2(0, -150), COL_DANGER, 16);

        // 关闭按钮（Image 和 Text 不能在同一 GO，Text 放子物体）
        var closeGO = NewGO("btnClose", cardT);
        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(0.6f, 0.58f, 0.55f);
        closeGO.AddComponent<Button>();
        var closeTxtGO = NewGO("txt", closeGO.transform);
        var closeT = closeTxtGO.AddComponent<Text>();
        closeT.text = "X";
        closeT.fontSize = 20;
        closeT.color = COL_WHITE;
        closeT.font = FONT;
        closeT.alignment = TextAnchor.MiddleCenter;
        closeT.fontStyle = FontStyle.Bold;
        closeTxtGO.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        closeTxtGO.GetComponent<RectTransform>().anchorMax = Vector2.one;
        closeTxtGO.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        var clRT = closeGO.GetComponent<RectTransform>();
        clRT.anchorMin = new Vector2(1f, 1f);
        clRT.anchorMax = new Vector2(1f, 1f);
        clRT.pivot = new Vector2(0.5f, 0.5f);
        clRT.anchoredPosition = new Vector2(-28, -28);
        clRT.sizeDelta = new Vector2(40, 40);

        Save(root, "SettingsPanel");
    }

    // ========================================
    // 5. LevelSelect
    // ========================================
    static void CreateLevelSelectPanel()
    {
        var root = CreatePanelRoot("LevelSelectPanel", new Color(0.1f, 0.08f, 0.15f));

        AddText(root.transform, "txtTitle", "疯狂铲屎官", 40,
            new Vector2(500, 50), new Vector2(0, 560),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_WHITE, TextAnchor.MiddleCenter, FontStyle.Bold);

        AddText(root.transform, "txtPlayerInfo", "", 18,
            new Vector2(600, 30), new Vector2(0, 500),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            COL_TEXT_GOLD, TextAnchor.MiddleCenter);

        AddButton(root.transform, "btnBack", "返回",
            new Vector2(100, 44), new Vector2(-300, 580),
            new Color(0.3f, 0.3f, 0.35f), 16,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

        // ScrollView
        var scrollGO = NewGO("LevelScrollView", root.transform);
        var sImg = scrollGO.AddComponent<Image>();
        sImg.color = new Color(0, 0, 0, 0);
        var sRT = scrollGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0f);
        sRT.anchorMax = new Vector2(1f, 1f);
        sRT.pivot = new Vector2(0.5f, 0.5f);
        sRT.offsetMin = new Vector2(20, 20);
        sRT.offsetMax = new Vector2(-20, -100);

        var contentGO = NewGO("Content", scrollGO.transform);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0.5f, 1f);
        contentRT.anchorMax = new Vector2(0.5f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;

        var grid = contentGO.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(150, 100);
        grid.spacing = new Vector2(15, 15);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;

        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;

        var sr = scrollGO.AddComponent<ScrollRect>();
        sr.content = contentRT;
        sr.horizontal = false;
        sr.vertical = true;

        Save(root, "LevelSelectPanel");
    }
}
