using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 铲屎官疯了 v2 — UI 交互 + 动画
/// </summary>
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
        if (gm == null) { Debug.LogError("[PetGameUI] Manager null!"); return; }

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

        if (hudPf == null) { Debug.LogError("[PetGameUI] GameHUD prefab missing!"); return; }
        gameHUD = Instantiate(hudPf, transform);
        gameHUD.name = "GameHUD";

        FindRefs();
        BindButtons();
        if (gm.isPlaying) BuildLevel();
    }

    void FindRefs()
    {
        txtLevel = FindT("LevelText");  txtScore = FindT("ScoreText");
        txtStep = FindT("StepText");    txtStars = FindT("Stars");
        txtResultTitle = FindT("Title");
        petArea = FindGO("PetArea")?.transform;
        bowlArea = FindGO("BowlArea")?.transform;
        resultOverlay = FindGO("ResultOverlay");
        btnUndo = FindB("btnUndo");     btnAddBowl = FindB("btnAddBowl");
        btnShuffle = FindB("btnShuffle"); btnRestart = FindB("btnRestart");
        btnNext = FindB("btnNext");
    }

    void BindButtons()
    {
        btnUndo?.onClick.AddListener(() => { gm.Undo(); BuildBowls(); BuildPets(); UpdateHUD(); });
        btnAddBowl?.onClick.AddListener(() => { gm.AddBowl(); BuildBowls(); });
        btnShuffle?.onClick.AddListener(() => BackToMenu());
        btnRestart?.onClick.AddListener(Restart);
        btnNext?.onClick.AddListener(NextLevel);
        gm.onScoreChanged.AddListener(_ => UpdateHUD());
        gm.onPour.AddListener(_ => { BuildBowls(); UpdateHUD(); });
        gm.onBowlCompleted.AddListener(() => BuildBowls());
        gm.onPetFed.AddListener((p, pts, f) => { BuildPets(); BuildBowls(); });
        gm.onLevelComplete.AddListener(OnWin);
        gm.onLevelFail.AddListener(OnFail);
        gm.onSelectionChanged.AddListener(BuildBowls);
        gm.onPourAnim.AddListener((fromId, toId) => StartCoroutine(PourAnimation(fromId, toId)));
        gm.onFeedAnim.AddListener((bowlId, petType) => StartCoroutine(FeedAnimation(bowlId, petType)));

        // 如果不在游戏中，显示关卡选择面板
        if (!gm.isPlaying) ShowLevelSelect();
        else BuildLevel();
    }

    void BuildLevel() { BuildPets(); BuildBowls(); UpdateHUD(); }

    #region 关卡选择
    GameObject levelSelectPanel;

    void ShowLevelSelect()
    {
        if (levelSelectPanel != null) Destroy(levelSelectPanel);

        levelSelectPanel = new GameObject("LevelSelect", typeof(RectTransform));
        levelSelectPanel.transform.SetParent(transform, false);
        var lrt = levelSelectPanel.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;

        var bg = levelSelectPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.08f, 0.15f, 0.95f);

        // 标题
        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(levelSelectPanel.transform, false);
        var tt = titleGO.AddComponent<Text>();
        tt.text = "铲屎官疯了"; tt.fontSize = 40; tt.color = Color.white;
        tt.alignment = TextAnchor.MiddleCenter; tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var trt = titleGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.1f, 0.75f); trt.anchorMax = new Vector2(0.9f, 0.9f);
        trt.sizeDelta = Vector2.zero;

        // 关卡按钮
        int cols = 4;
        float btnW = 150, btnH = 80, gap = 15;
        for (int i = 0; i < gm.LevelCount; i++)
        {
            int lid = i + 1;
            var btnGO = new GameObject($"Btn_Level{lid}", typeof(RectTransform));
            btnGO.transform.SetParent(levelSelectPanel.transform, false);
            var bimg = btnGO.AddComponent<Image>();
            bimg.color = (i < 5) ? new Color(0.3f, 0.5f, 0.3f) : new Color(0.3f, 0.35f, 0.5f);
            var btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(() => { Destroy(levelSelectPanel); levelSelectPanel = null; gm.currentLevelId = lid; gm.StartLevel(lid); BuildLevel(); });

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(btnGO.transform, false);
            var t = txtGO.AddComponent<Text>();
            t.text = $"{lid}\n{gm.GetLevelName(lid)}"; t.fontSize = 14; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var txtRT = txtGO.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one; txtRT.sizeDelta = Vector2.zero;

            var brt = btnGO.GetComponent<RectTransform>();
            int col = i % cols, row = i / cols;
            float startX = 375 - (cols * btnW + (cols - 1) * gap) / 2f + btnW / 2f;
            float startY = 700 - row * (btnH + gap);
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(0, 0);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(startX + col * (btnW + gap), startY);
            brt.sizeDelta = new Vector2(btnW, btnH);
        }
    }
    #endregion

    #region 碗布局 — 手动散布，带随机偏移
    Vector2 BowlPos(int index, int total)
    {
        var rt = bowlArea.GetComponent<RectTransform>();
        float w = rt.rect.width - 140;
        float h = rt.rect.height - 140;
        int cols = Mathf.Clamp(Mathf.Max(1, (int)(w / 170f)), 1, 4);
        int row = index / cols;
        int col = index % cols;
        float cellW = w / cols;
        float cellH = 140;

        float x = -w / 2 + cellW * (col + 0.5f);
        float y = h / 2 - 70 - row * cellH;

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
            if (face != null)
            {
                var spr = Resources.Load<Sprite>($"PetFaces/{pet.ToString().ToLower()}/neutral");
                if (spr != null) face.sprite = spr;
            }
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
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = BowlPos(i, bowls.Count);

            int bid = bowl.bowlId;
            bowlIdToGO[bid] = go;

            var btn = go.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                if (bowl.isCompleted)
                {
                    btn.interactable = false; // 已完成碗不可点击
                }
                else
                {
                    btn.interactable = true;
                    btn.onClick.AddListener(() => gm.OnBowlClicked(bid));
                }
            }

            float scale = (bid == gm.selectedBowlId) ? 1.3f : 1f;
            go.transform.localScale = Vector3.one * scale;

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

        var layout = stack.GetComponent<LayoutGroup>();
        if (layout) layout.enabled = false;

        Clear(stack);
        float overlap = 60f;
        float startY = -(bowl.foods.Count - 1) * overlap / 2f;

        for (int j = 0; j < bowl.foods.Count; j++)
        {
            var icon = Instantiate(foodIconPf, stack);
            var irt = icon.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = new Vector2(0, startY + j * overlap);
            irt.sizeDelta = new Vector2(100, 100);

            var le = icon.GetComponent<LayoutElement>();
            if (le != null) { le.preferredWidth = 100; le.preferredHeight = 100; }

            var img = icon.GetComponent<Image>();
            if (img != null)
            {
                var spr = GetFoodSprite(bowl.foods[j]);
                if (spr != null) img.sprite = spr;
            }
        }
    }

    Sprite GetFoodSprite(FoodType type)
    {
        int idx = (Mathf.Abs(type.GetHashCode()) % 15) + 1;
        return Resources.Load<Sprite>($"ArtFoods/food{idx:D2}");
    }

    #region 动画 — 倒食物
    IEnumerator PourAnimation(int fromId, int toId)
    {
        if (!bowlIdToGO.ContainsKey(fromId) || !bowlIdToGO.ContainsKey(toId)) yield break;
        var fromGO = bowlIdToGO[fromId];
        var toGO = bowlIdToGO[toId];
        var fromRT = fromGO.GetComponent<RectTransform>();
        var toRT = toGO.GetComponent<RectTransform>();
        var fromStack = FindGO(fromGO, "FoodStack")?.transform;
        var toStack = FindGO(toGO, "FoodStack")?.transform;

        if (fromStack == null || toStack == null) yield break;

        GameObject movingFood = null;
        if (fromStack.childCount > 0)
            movingFood = fromStack.GetChild(fromStack.childCount - 1).gameObject;

        Vector3 fromOrigPos = fromRT.anchoredPosition3D;
        Quaternion fromOrigRot = fromRT.localRotation;
        Vector3 targetPos = toRT.anchoredPosition3D + new Vector3(fromRT.anchoredPosition.x < toRT.anchoredPosition.x ? -60 : 60, 90, 0);

        // 1. 移到B旁
        float duration = 0.25f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            fromRT.anchoredPosition3D = Vector3.Lerp(fromOrigPos, targetPos, t / duration);
            yield return null;
        }
        fromRT.anchoredPosition3D = targetPos;

        // 2. 倾斜
        float tiltAngle = fromRT.anchoredPosition.x < toRT.anchoredPosition.x ? -100f : 100f;
        Quaternion targetRot = Quaternion.Euler(0, 0, tiltAngle);
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            fromRT.localRotation = Quaternion.Lerp(fromOrigRot, targetRot, t / 0.2f);
            yield return null;
        }
        fromRT.localRotation = targetRot;
        yield return new WaitForSeconds(0.15f);

        // 3. 食物从A消失，出现在B最上方
        if (movingFood != null)
        {
            movingFood.transform.SetParent(toStack, false);
            movingFood.transform.SetAsLastSibling();
            float overlap = 60f;
            float y = -(toStack.childCount - 1) * overlap / 2f + (toStack.childCount - 1) * overlap;
            var mrt = movingFood.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.5f, 0.5f);
            mrt.anchorMax = new Vector2(0.5f, 0.5f);
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.anchoredPosition = new Vector2(0, y);
        }

        yield return new WaitForSeconds(0.1f);

        // 4. 回正 + 回位
        for (float t = 0; t < 0.25f; t += Time.deltaTime)
        {
            fromRT.anchoredPosition3D = Vector3.Lerp(targetPos, fromOrigPos, t / 0.25f);
            fromRT.localRotation = Quaternion.Lerp(targetRot, fromOrigRot, t / 0.25f);
            yield return null;
        }
        fromRT.anchoredPosition3D = fromOrigPos;
        fromRT.localRotation = fromOrigRot;

        // 5. 同步真实数据
        BuildBowls();
    }
    #endregion

    #region 动画 — 宠物喂食
    IEnumerator FeedAnimation(int bowlId, PetType petType)
    {
        if (!bowlIdToGO.ContainsKey(bowlId)) yield break;
        var bowlGO = bowlIdToGO[bowlId];
        var bowlRT = bowlGO.GetComponent<RectTransform>();

        int petIdx = gm.GetFedPets().Count - 1;
        if (petIdx < 0 || petIdx >= petGOs.Count) petIdx = 0;
        var petGO = petGOs.Count > petIdx ? petGOs[petIdx] : null;
        if (petGO == null) { BuildBowls(); BuildPets(); yield break; }

        var petRT = petGO.GetComponent<RectTransform>();
        Vector3 bowlOrigPos = bowlRT.anchoredPosition3D;
        Vector3 petPos = petRT.anchoredPosition3D + new Vector3(0, 30, 0);

        // 1. 碗飞到宠物旁
        float duration = 0.35f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            bowlRT.anchoredPosition3D = Vector3.Lerp(bowlOrigPos, petPos, t / duration);
            yield return null;
        }

        // 2. 碗消失，头顶生成碗
        Destroy(bowlGO);
        var headBowl = Instantiate(bowlItemPf, petGO.transform);
        var hrt = headBowl.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0.5f, 0.5f);
        hrt.anchorMax = new Vector2(0.5f, 0.5f);
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = new Vector2(0, 80);
        hrt.sizeDelta = new Vector2(100, 120);
        var hbtn = headBowl.GetComponent<Button>();
        if (hbtn) hbtn.enabled = false;
        var dm = FindGO(headBowl, "DoneMark");
        if (dm) dm.SetActive(false);

        // 3. 宠物左移出屏幕
        Vector3 petOrig = petRT.anchoredPosition3D;
        Vector3 petOff = petOrig + new Vector3(-600, 0, 0);
        duration = 0.6f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            petRT.anchoredPosition3D = Vector3.Lerp(petOrig, petOff, t / duration);
            yield return null;
        }

        Destroy(petGO);
        Destroy(headBowl);
        // 不再调用 BuildBowls/BuildPets — onPetFed 事件已经重建过了
    }
    #endregion

    #region HUD
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

    public void BackToMenu()
    {
        gm.isPlaying = false;
        if (resultOverlay) resultOverlay.SetActive(false);
        Clear(bowlArea); Clear(petArea);
        bowlGOs.Clear(); petGOs.Clear(); bowlIdToGO.Clear();
        ShowLevelSelect();
    }
    #endregion

    #region helpers
    T FindC<T>(GameObject root, string n) where T : Component { foreach (var c in root.GetComponentsInChildren<T>(true)) if (c.name == n) return c; return null; }
    Text FindT(string n) => FindC<Text>(gameHUD, n);
    Button FindB(string n) => FindC<Button>(gameHUD, n);
    GameObject FindGO(GameObject root, string n) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t.gameObject; return null; }
    GameObject FindGO(string n) => FindGO(gameHUD, n);
    void Clear(Transform t) { foreach (Transform c in t) Destroy(c.gameObject); }
    string PetCN(PetType p) => p switch { PetType.Cat => "橘猫", PetType.Dog => "柴犬", PetType.Hamster => "仓鼠", PetType.Parrot => "鹦鹉", PetType.Fish => "金鱼", PetType.Rabbit => "垂耳兔", _ => p.ToString() };
    #endregion
}
