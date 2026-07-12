using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PetGameUI : MonoBehaviour
{
    private PetGameManager gm;
    private GameObject gameHUD;
    private GameObject petItemPf, bowlItemPf, foodIconPf;

    private Transform petArea, bowlArea;
    private Text txtLevel, txtScore, txtStep, txtStars, txtResultTitle;
    private GameObject resultOverlay;
    private Button btnUndo, btnAddBowl, btnShuffle, btnRestart, btnNext;

    private List<GameObject> petGOs = new List<GameObject>();
    private List<GameObject> bowlGOs = new List<GameObject>();
    private Dictionary<int, GameObject> bowlIdToGO = new Dictionary<int, GameObject>();

    void Start()
    {
        gm = PetGameManager.Instance;
        if (gm == null) return;

        gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(750, 1334);
        sc.matchWidthOrHeight = 1f;
        gameObject.AddComponent<GraphicRaycaster>();

        petItemPf = Resources.Load<GameObject>("PrefabsV2/PetItem");
        bowlItemPf = Resources.Load<GameObject>("PrefabsV2/BowlItem");
        foodIconPf = Resources.Load<GameObject>("PrefabsV2/FoodIcon");
        var hudPf = Resources.Load<GameObject>("PrefabsV2/GameHUD");
        if (hudPf == null) return;
        gameHUD = Instantiate(hudPf, transform);
        gameHUD.name = "GameHUD";

        FindRefs();
        BindButtons();
        if (gm.isPlaying) BuildLevel();
        else ShowLevelSelect();
    }

    void FindRefs()
    {
        txtLevel = FindT("LevelText"); txtScore = FindT("ScoreText");
        txtStep = FindT("StepText"); txtStars = FindT("Stars");
        txtResultTitle = FindT("Title");
        petArea = FindGO("PetArea")?.transform;
        bowlArea = FindGO("BowlArea")?.transform;
        resultOverlay = FindGO("ResultOverlay");
        btnUndo = FindB("btnUndo"); btnAddBowl = FindB("btnAddBowl");
        btnShuffle = FindB("btnShuffle"); btnRestart = FindB("btnRestart");
        btnNext = FindB("btnNext");
    }

    void BindButtons()
    {
        btnUndo?.onClick.AddListener(() => { gm.Undo(); RebuildAll(); });
        btnAddBowl?.onClick.AddListener(() => { gm.AddBowl(); BuildBowls(); });
        btnShuffle?.onClick.AddListener(() => BackToMenu());
        btnRestart?.onClick.AddListener(Restart);
        btnNext?.onClick.AddListener(NextLevel);
        gm.onScoreChanged.AddListener(_ => UpdateHUD());
        gm.onSelectionChanged.AddListener(BuildBowls);
        gm.onPour.AddListener(_ => RebuildAll());
        gm.onPetFed.AddListener((p, pts, f) => RebuildAll());
        gm.onBowlCompleted.AddListener(() => BuildBowls());
        gm.onLevelComplete.AddListener(OnWin);
        gm.onLevelFail.AddListener(OnFail);
    }

    void RebuildAll() { BuildBowls(); BuildPets(); UpdateHUD(); }
    void BuildLevel() { BuildPets(); BuildBowls(); UpdateHUD(); }

    #region 关卡选择
    GameObject levelSelectPanel;
    void ShowLevelSelect()
    {
        if (levelSelectPanel != null) Destroy(levelSelectPanel);
        levelSelectPanel = new GameObject("LevelSelect", typeof(RectTransform));
        levelSelectPanel.transform.SetParent(transform, false);
        var lrt = levelSelectPanel.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        var bg = levelSelectPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.08f, 0.15f, 0.95f);

        var tt = new GameObject("Title", typeof(RectTransform)).AddComponent<Text>();
        tt.transform.SetParent(levelSelectPanel.transform, false);
        tt.text = "铲屎官疯了"; tt.fontSize = 40; tt.color = Color.white;
        tt.alignment = TextAnchor.MiddleCenter;
        tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var trt = tt.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.1f, 0.75f); trt.anchorMax = new Vector2(0.9f, 0.9f);
        trt.sizeDelta = Vector2.zero;

        int cols = 4; float bw = 150, bh = 80, gap = 15;
        for (int i = 0; i < gm.LevelCount; i++)
        {
            int lid = i + 1;
            var bgo = new GameObject($"Btn{lid}", typeof(RectTransform));
            bgo.transform.SetParent(levelSelectPanel.transform, false);
            var bimg = bgo.AddComponent<Image>();
            bimg.color = i < 5 ? new Color(0.3f, 0.5f, 0.3f) : new Color(0.3f, 0.35f, 0.5f);
            var btn = bgo.AddComponent<Button>();
            btn.onClick.AddListener(() => { Destroy(levelSelectPanel); levelSelectPanel = null; gm.currentLevelId = lid; gm.StartLevel(lid); BuildLevel(); });
            var tgo = new GameObject("T", typeof(RectTransform));
            tgo.transform.SetParent(bgo.transform, false);
            var t = tgo.AddComponent<Text>();
            t.text = $"{lid}\n{gm.GetLevelName(lid)}"; t.fontSize = 14; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var trt2 = tgo.GetComponent<RectTransform>();
            trt2.anchorMin = Vector2.zero; trt2.anchorMax = Vector2.one; trt2.sizeDelta = Vector2.zero;
            var brt = bgo.GetComponent<RectTransform>();
            int col = i % cols, row = i / cols;
            float sx = 375 - (cols * bw + (cols - 1) * gap) / 2f + bw / 2f;
            float sy = 700 - row * (bh + gap);
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(sx + col * (bw + gap), sy);
            brt.sizeDelta = new Vector2(bw, bh);
        }
    }
    #endregion

    #region 碗布局
    Vector2 BowlPos(int index, int total)
    {
        var rt = bowlArea.GetComponent<RectTransform>();
        float w = rt.rect.width - 140, h = rt.rect.height - 140;
        int cols = Mathf.Clamp(Mathf.Max(1, (int)(w / 170f)), 1, 4);
        int row = index / cols, col = index % cols;
        float x = -w / 2 + (w / cols) * (col + 0.5f);
        float y = h / 2 - 70 - row * 140;
        var rng = new System.Random(index * 137 + total * 73);
        x += (float)(rng.NextDouble() - 0.5) * 30;
        y += (float)(rng.NextDouble() - 0.5) * 20;
        return new Vector2(x, y);
    }
    #endregion

    void BuildPets()
    {
        if (petArea == null) return;
        Clear(petArea); petGOs.Clear();
        foreach (var pet in gm.GetPetQueue())
        {
            var go = Instantiate(petItemPf, petArea);
            var label = FindC<Text>(go, "QueueLabel");
            if (label) label.text = PetCN(pet);
            var face = FindC<Image>(go, "PetFace");
            if (face) { var s = Resources.Load<Sprite>($"PetFaces/{pet.ToString().ToLower()}/neutral"); if (s) face.sprite = s; }
            petGOs.Add(go);
        }
    }

    void BuildBowls()
    {
        if (bowlArea == null || bowlItemPf == null) return;
        Clear(bowlArea); bowlGOs.Clear(); bowlIdToGO.Clear();
        var bowls = gm.GetBowls();
        for (int i = 0; i < bowls.Count; i++)
        {
            var bowl = bowls[i];
            var go = Instantiate(bowlItemPf, bowlArea);
            go.GetComponent<RectTransform>().anchoredPosition = BowlPos(i, bowls.Count);
            int bid = bowl.bowlId;
            bowlIdToGO[bid] = go;
            var btn = go.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.interactable = !bowl.isCompleted;
                if (!bowl.isCompleted) btn.onClick.AddListener(() => gm.OnBowlClicked(bid));
            }
            go.transform.localScale = (bid == gm.selectedBowlId) ? Vector3.one * 1.3f : Vector3.one;
            BuildFoodStack(go, bowl);
            var done = FindGO(go, "DoneMark");
            if (done) done.SetActive(bowl.isCompleted);
            bowlGOs.Add(go);
        }
    }

    void BuildFoodStack(GameObject bowlGO, Bowl bowl)
    {
        var stack = FindGO(bowlGO, "FoodStack")?.transform;
        if (stack == null || foodIconPf == null) return;
        var lg = stack.GetComponent<LayoutGroup>();
        if (lg) lg.enabled = false;
        Clear(stack);

        // 动态尺寸：碗容量决定食物图标大小
        float sz = bowl.capacity <= 3 ? 50f : 38f;
        float overlap = sz * 0.45f;

        // 从下往上堆叠，不超过碗内区域
        for (int j = 0; j < bowl.foods.Count; j++)
        {
            var icon = Instantiate(foodIconPf, stack);
            var irt = icon.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = new Vector2(0, j * overlap);
            irt.sizeDelta = new Vector2(sz, sz);
            var le = icon.GetComponent<LayoutElement>();
            if (le != null) { le.preferredWidth = sz; le.preferredHeight = sz; }
            var img = icon.GetComponent<Image>();
            if (img) { var s = GetFoodSprite(bowl.foods[j]); if (s) img.sprite = s; }
        }
    }

    Sprite GetFoodSprite(FoodType type) { int i = (Mathf.Abs(type.GetHashCode()) % 15) + 1; return Resources.Load<Sprite>($"ArtFoods/food{i:D2}"); }

    void UpdateHUD()
    {
        if (txtLevel) txtLevel.text = $"第{gm.currentLevelId}关";
        if (txtScore) txtScore.text = $"得分:{gm.GetScore()}/{gm.targetScore}";
        if (txtStep) txtStep.text = $"步数:{gm.pour.totalMoves}";
    }

    void OnWin(int s) { if (resultOverlay) resultOverlay.SetActive(true); if (txtResultTitle) txtResultTitle.text = "通关!"; if (txtStars) txtStars.text = new string((char)9733, s) + new string((char)9734, 3 - s); }
    void OnFail() { if (resultOverlay) resultOverlay.SetActive(true); if (txtResultTitle) txtResultTitle.text = "失败..."; }
    void Restart() { if (resultOverlay) resultOverlay.SetActive(false); gm.StartLevel(gm.currentLevelId); BuildLevel(); }
    void NextLevel() { if (resultOverlay) resultOverlay.SetActive(false); gm.currentLevelId = gm.currentLevelId >= gm.LevelCount ? 1 : gm.currentLevelId + 1; gm.StartLevel(gm.currentLevelId); BuildLevel(); }
    public void BackToMenu() { gm.isPlaying = false; if (resultOverlay) resultOverlay.SetActive(false); Clear(bowlArea); Clear(petArea); bowlGOs.Clear(); petGOs.Clear(); bowlIdToGO.Clear(); ShowLevelSelect(); }

    T FindC<T>(GameObject root, string n) where T : Component { foreach (var c in root.GetComponentsInChildren<T>(true)) if (c.name == n) return c; return null; }
    Text FindT(string n) => FindC<Text>(gameHUD, n);
    Button FindB(string n) => FindC<Button>(gameHUD, n);
    GameObject FindGO(GameObject root, string n) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t.gameObject; return null; }
    GameObject FindGO(string n) => FindGO(gameHUD, n);
    void Clear(Transform t) { foreach (Transform c in t) Destroy(c.gameObject); }
    string PetCN(PetType p) => p switch { PetType.Cat => "橘猫", PetType.Dog => "柴犬", PetType.Hamster => "仓鼠", PetType.Parrot => "鹦鹉", PetType.Fish => "金鱼", PetType.Rabbit => "垂耳兔", _ => p.ToString() };
}
