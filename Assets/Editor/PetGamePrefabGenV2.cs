using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用真实美术素材生成 UI 预制体 (v2)
/// </summary>
public class PetGamePrefabGenV2
{
    const string PREFAB_DIR = "Assets/Resources/PrefabsV2";
    const string ART = "Assets/Art/PetGame";

    [MenuItem("Tools/铲屎官疯了/生成UI预制体(v2)")]
    static void GenerateAll()
    {
        if (!System.IO.Directory.Exists(PREFAB_DIR)) System.IO.Directory.CreateDirectory(PREFAB_DIR);

        CreatePetItem();
        CreateBowlItem();
        CreateFoodIcon();
        CreateGameHUD();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[PrefabGen] v2 预制体生成完成</color>");
        EditorUtility.DisplayDialog("完成",
            "已生成 4 个预制体，路径 Resources/PrefabsV2/\n\nPetItem (宠物，表情+序号)\nBowlItem (碗，含宠物主题背景)\nFoodIcon (食物图标)\nGameHUD (完整界面)\n\n双击 GameHUD.prefab 预览整体效果", "好的");
    }

    static Sprite LoadSprite(string subPath)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART}/{subPath}");
        if (s == null) Debug.LogWarning($"[PrefabGen] Sprite未找到: {subPath}");
        return s;
    }

    // ========== 宠物 Item（表情+序号+碗在头顶）==========
    static void CreatePetItem()
    {
        var root = new GameObject("PetItem", typeof(RectTransform), typeof(LayoutElement));
        root.GetComponent<LayoutElement>().preferredWidth = 100;
        root.GetComponent<LayoutElement>().preferredHeight = 130;

        // 头顶的碗（最上层，初始隐藏）
        var topBowl = new GameObject("TopBowl", typeof(RectTransform), typeof(Image));
        topBowl.transform.SetParent(root.transform, false);
        var tbrt = topBowl.GetComponent<RectTransform>();
        tbrt.anchorMin = tbrt.anchorMax = new Vector2(0.5f, 1);
        tbrt.sizeDelta = new Vector2(70, 50);
        tbrt.anchoredPosition = new Vector2(0, -10);
        topBowl.GetComponent<Image>().preserveAspect = true;
        topBowl.GetComponent<Image>().sprite = LoadSprite("bowls/full/bowl01.png");
        topBowl.SetActive(false);

        // 宠物表情
        var face = new GameObject("PetFace", typeof(RectTransform), typeof(Image));
        face.transform.SetParent(root.transform, false);
        face.AddComponent<LayoutElement>().preferredHeight = 80;
        var faceImg = face.GetComponent<Image>();
        faceImg.sprite = LoadSprite("pets/cat/neutral.png");
        faceImg.preserveAspect = true;

        // 排队序号
        var label = new GameObject("QueueLabel", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(root.transform, false);
        label.AddComponent<LayoutElement>().preferredHeight = 20;
        var t = label.GetComponent<Text>();
        t.fontSize = 14; t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.3f, 0.2f, 0.1f);
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Save(root, "PetItem");
    }

    // ========== 碗 Item（背景是宠物主题碗+食物栈）==========
    static void CreateBowlItem()
    {
        var root = new GameObject("BowlItem", typeof(RectTransform), typeof(Button), typeof(LayoutElement));
        root.GetComponent<LayoutElement>().preferredWidth = 130;
        root.GetComponent<LayoutElement>().preferredHeight = 150;

        // 碗背景图（用cats专用bowl01）
        var bg = new GameObject("BowlBg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        bg.AddComponent<LayoutElement>().ignoreLayout = true;
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = LoadSprite("bowls/empty/bowl01.png");
        bgImg.preserveAspect = true;
        bgImg.raycastTarget = false;

        // 食物栈容器
        var stack = new GameObject("FoodStack", typeof(RectTransform));
        stack.transform.SetParent(root.transform, false);
        stack.AddComponent<LayoutElement>().ignoreLayout = true;
        var srt = stack.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.1f, 0.55f);
        srt.anchorMax = new Vector2(0.9f, 0.85f);
        srt.sizeDelta = Vector2.zero;
        var svl = stack.AddComponent<VerticalLayoutGroup>();
        svl.childAlignment = TextAnchor.MiddleCenter;
        svl.spacing = 0;

        // 完成标记（星星）
        var done = new GameObject("DoneMark", typeof(RectTransform), typeof(Image));
        done.transform.SetParent(root.transform, false);
        done.AddComponent<LayoutElement>().ignoreLayout = true;
        var drt = done.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.sizeDelta = new Vector2(50, 50);
        drt.anchoredPosition = new Vector2(0, 30);
        done.GetComponent<Image>().sprite = LoadSprite("UI/ui06.png");
        done.GetComponent<Image>().preserveAspect = true;
        done.SetActive(false);

        Save(root, "BowlItem");
    }

    // ========== 食物图标 ==========
    static void CreateFoodIcon()
    {
        var root = new GameObject("FoodIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        root.GetComponent<LayoutElement>().preferredWidth = 60;
        root.GetComponent<LayoutElement>().preferredHeight = 60;
        root.GetComponent<Image>().sprite = LoadSprite("foods/food01.png");
        root.GetComponent<Image>().preserveAspect = true;
        Save(root, "FoodIcon");
    }

    // ========== 完整游戏界面 ==========
    static void CreateGameHUD()
    {
        var root = new GameObject("GameHUD", typeof(RectTransform), typeof(Image));
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(750, 1334);
        root.GetComponent<Image>().color = new Color(0.97f, 0.95f, 0.9f);

        // ===== 顶栏 =====
        var topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(root.transform, false);
        var tbrt = topBar.GetComponent<RectTransform>();
        tbrt.anchorMin = new Vector2(0, 1); tbrt.anchorMax = new Vector2(1, 1);
        tbrt.pivot = new Vector2(0.5f, 1);
        tbrt.anchoredPosition = Vector2.zero; tbrt.sizeDelta = new Vector2(0, 60);
        topBar.GetComponent<Image>().color = new Color(0.94f, 0.92f, 0.88f, 0.95f);
        var tHL = topBar.AddComponent<HorizontalLayoutGroup>();
        tHL.childAlignment = TextAnchor.MiddleCenter; tHL.padding = new RectOffset(15, 15, 0, 0);

        NewText(topBar.transform, "LevelText", "第1关", 16);
        NewText(topBar.transform, "ScoreText", "得分:0/200", 18).color = new Color(0.8f, 0.4f, 0.2f);
        NewText(topBar.transform, "StepText", "步数:0", 14).color = new Color(0.5f, 0.5f, 0.5f);

        // ===== 按钮行（用UI素材）=====
        var btnRow = new GameObject("ButtonRow", typeof(RectTransform));
        btnRow.transform.SetParent(root.transform, false);
        var brrt = btnRow.GetComponent<RectTransform>();
        brrt.anchorMin = new Vector2(0, 1); brrt.anchorMax = new Vector2(1, 1);
        brrt.pivot = new Vector2(0.5f, 1);
        brrt.anchoredPosition = new Vector2(0, -60); brrt.sizeDelta = new Vector2(0, 50);
        var bHL = btnRow.AddComponent<HorizontalLayoutGroup>();
        bHL.childAlignment = TextAnchor.MiddleCenter; bHL.spacing = 10;
        bHL.childForceExpandWidth = false; bHL.padding = new RectOffset(10, 10, 0, 0);

        // ui03 提示、ui05(进度条作取消) → 改成ui04暂停 改用ui01开始
        // 撤回+加碗+打乱+重来：用小按钮 + emoji
        SmallIconButton(btnRow.transform, "btnUndo", "↩");
        SmallIconButton(btnRow.transform, "btnAddBowl", "🥣+");
        SmallIconButton(btnRow.transform, "btnShuffle", "🔀");
        SmallIconButton(btnRow.transform, "btnRestart", "🔄");

        // ===== 宠物区域 =====
        var petArea = new GameObject("PetArea", typeof(RectTransform));
        petArea.transform.SetParent(root.transform, false);
        var part = petArea.GetComponent<RectTransform>();
        part.anchorMin = new Vector2(0, 1); part.anchorMax = new Vector2(1, 1);
        part.pivot = new Vector2(0.5f, 1);
        part.anchoredPosition = new Vector2(0, -120); part.sizeDelta = new Vector2(0, 130);
        var pHL = petArea.AddComponent<HorizontalLayoutGroup>();
        pHL.childAlignment = TextAnchor.MiddleCenter; pHL.spacing = 15;
        pHL.childForceExpandWidth = false; pHL.padding = new RectOffset(20, 20, 5, 5);

        // ===== 碗网格 =====
        var bowlArea = new GameObject("BowlArea", typeof(RectTransform));
        bowlArea.transform.SetParent(root.transform, false);
        var bart = bowlArea.GetComponent<RectTransform>();
        bart.anchorMin = new Vector2(0, 0); bart.anchorMax = new Vector2(1, 1);
        bart.offsetMin = new Vector2(15, 100); bart.offsetMax = new Vector2(-15, -260);
        var gGL = bowlArea.AddComponent<GridLayoutGroup>();
        gGL.cellSize = new Vector2(140, 170);
        gGL.spacing = new Vector2(20, 25);
        gGL.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gGL.constraintCount = 4;
        gGL.childAlignment = TextAnchor.MiddleCenter;

        // ===== 手持食物（带半透明圈）=====
        var heldHolder = new GameObject("HeldFoodHolder", typeof(RectTransform), typeof(Image));
        heldHolder.transform.SetParent(root.transform, false);
        var hhrt = heldHolder.GetComponent<RectTransform>();
        hhrt.anchorMin = hhrt.anchorMax = new Vector2(0.5f, 0.5f);
        hhrt.sizeDelta = new Vector2(100, 100);
        hhrt.anchoredPosition = new Vector2(0, -490);
        heldHolder.GetComponent<Image>().color = new Color(1, 1, 1, 0.6f);
        heldHolder.GetComponent<Image>().sprite = LoadSprite("UI/ui07.png");
        heldHolder.GetComponent<Image>().preserveAspect = true;
        heldHolder.SetActive(false);

        // ===== 通关遮罩（用ui06星星）=====
        var overlay = new GameObject("ResultOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(root.transform, false);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.sizeDelta = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        overlay.SetActive(false);

        var oVL = overlay.AddComponent<VerticalLayoutGroup>();
        oVL.childAlignment = TextAnchor.MiddleCenter;
        oVL.spacing = 15;
        oVL.padding = new RectOffset(0, 0, 200, 0);

        // 标题
        var oTitle = NewText(overlay.transform, "Title", "通关!", 42);
        oTitle.color = Color.white;
        // 星星图标
        var starIcon = new GameObject("StarIcon", typeof(RectTransform), typeof(Image));
        starIcon.transform.SetParent(overlay.transform, false);
        starIcon.AddComponent<LayoutElement>().preferredWidth = 120;
        starIcon.AddComponent<LayoutElement>().preferredHeight = 120;
        starIcon.GetComponent<Image>().sprite = LoadSprite("UI/ui06.png");
        starIcon.GetComponent<Image>().preserveAspect = true;

        NewText(overlay.transform, "Stars", "★★★", 36).color = new Color(1, 0.8f, 0);
        BigButton(overlay.transform, "btnNext", "下一关", new Color(0.35f, 0.65f, 0.95f));

        Save(root, "GameHUD");
    }

    static Text NewText(Transform parent, string name, string content, int size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.3f, 0.2f, 0.1f);
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.AddComponent<LayoutElement>();
        return t;
    }

    /// <summary>小图标按钮（带背景）</summary>
    static GameObject SmallIconButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.8f, 0.7f, 0.5f, 0.95f);
        go.AddComponent<LayoutElement>().preferredWidth = 50;
        go.AddComponent<LayoutElement>().preferredHeight = 40;
        go.GetComponent<Image>().sprite = LoadSprite("UI/ui05.png");
        go.GetComponent<Image>().preserveAspect = false;
        go.GetComponent<Image>().type = Image.Type.Sliced;

        var txt = new GameObject("Text", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        var t = txt.AddComponent<Text>();
        t.text = label; t.fontSize = 16; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
        t.fontStyle = FontStyle.Bold;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        return go;
    }

    /// <summary>大按钮（带文字）</summary>
    static GameObject BigButton(Transform parent, string name, string label, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.AddComponent<LayoutElement>().preferredWidth = 200;
        go.AddComponent<LayoutElement>().preferredHeight = 60;

        var txt = new GameObject("Text", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        var t = txt.AddComponent<Text>();
        t.text = label; t.fontSize = 20; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
        t.fontStyle = FontStyle.Bold;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        return go;
    }

    static void Save(GameObject go, string name)
    {
        string path = $"{PREFAB_DIR}/{name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) AssetDatabase.DeleteAsset(path);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
