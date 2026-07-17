using System.Collections;
using System.Collections.Generic;
using F8Framework.Core;
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

    // 结算页动态元素
    private LevelResult lastResult;
    private GameObject rewardPanel;
    private Button btnWatchAd;
    private bool adRewardClaimed;

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
        if (gm.fsm != null) BuildLevel();
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
        btnShuffle?.onClick.AddListener(() => { ShuffleCurrentBowl(); });
        btnRestart?.onClick.AddListener(Restart);
        btnNext?.onClick.AddListener(NextLevel);
        gm.onScoreChanged.AddListener(_ => UpdateHUD());
        gm.onSelectionChanged.AddListener(BuildBowls);
        gm.onPour.AddListener(_ => UpdateHUD());
        gm.onPetFed.AddListener((p, pts, f) => UpdateHUD());
        gm.onBowlCompleted.AddListener(() => { }); // 不做任何事，等动画结束统一重建
        gm.onLevelComplete.AddListener(OnWin);
        gm.onLevelFail.AddListener(OnFail);
        gm.onPourAnim.AddListener((f, t, c) => StartCoroutine(PourAnimation(f, t, c)));
        gm.onFeedAnim.AddListener((bid, pet) => StartCoroutine(FeedAnimation(bid, pet)));
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

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // --- 顶部标题 + 玩家信息 ---
        var tt = new GameObject("Title", typeof(RectTransform)).AddComponent<Text>();
        tt.transform.SetParent(levelSelectPanel.transform, false);
        tt.text = "疯狂铲屎官"; tt.fontSize = 40; tt.color = Color.white;
        tt.alignment = TextAnchor.MiddleCenter; tt.font = font;
        var trt = tt.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.1f, 0.85f); trt.anchorMax = new Vector2(0.9f, 0.93f);
        trt.sizeDelta = Vector2.zero;

        // 玩家状态栏：称号 + 小鱼干 + 总星数
        var info = new GameObject("PlayerInfo", typeof(RectTransform));
        info.transform.SetParent(levelSelectPanel.transform, false);
        var irt = info.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.05f, 0.78f); irt.anchorMax = new Vector2(0.95f, 0.84f);
        irt.sizeDelta = Vector2.zero;
        var it = info.AddComponent<Text>();
        int totalStars = SaveSystem.TotalStars;
        it.text = $"{SaveSystem.GetCurrentTitle()}  |  小鱼干:{SaveSystem.Data.fishDiscount}  徽章:{SaveSystem.Data.rescueBadge}  |  总星数:{totalStars}";
        it.fontSize = 18;
        it.color = new Color(1f, 0.84f, 0f);
        it.alignment = TextAnchor.MiddleCenter;
        it.font = font;

        // --- 关卡按钮 ---
        int cols = 4; float bw = 150, bh = 90, gap = 15;
        int highest = SaveSystem.Data.highestUnlockedLevel;
        for (int i = 0; i < gm.LevelCount; i++)
        {
            int lid = i + 1;
            bool unlocked = lid <= highest;
            int stars = SaveSystem.GetLevelStars(lid);

            var bgo = new GameObject($"Btn{lid}", typeof(RectTransform));
            bgo.transform.SetParent(levelSelectPanel.transform, false);
            var bimg = bgo.AddComponent<Image>();
            // 已通关=绿, 可玩=蓝, 锁定=灰
            bimg.color = stars > 0 ? new Color(0.2f, 0.5f, 0.25f)
                       : unlocked ? new Color(0.3f, 0.35f, 0.5f)
                       : new Color(0.25f, 0.25f, 0.25f);
            var btn = bgo.AddComponent<Button>();
            btn.interactable = unlocked;
            btn.onClick.AddListener(() => {
                Destroy(levelSelectPanel); levelSelectPanel = null;
                gm.currentLevelId = lid;
                SaveSystem.Data.currentLevelId = lid;
                SaveSystem.Save();
                gm.StartLevel(lid);
                BuildLevel();
            });

            // 关卡号 + 名称
            var tgo = new GameObject("T", typeof(RectTransform));
            tgo.transform.SetParent(bgo.transform, false);
            var t = tgo.AddComponent<Text>();
            string lockIcon = unlocked ? "" : "\n[锁定]";
            string starStr = stars > 0 ? "\n" + new string((char)9733, stars) : "";
            t.text = $"{lid}\n{gm.GetLevelName(lid)}{starStr}{lockIcon}";
            t.fontSize = 13; t.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            t.alignment = TextAnchor.MiddleCenter; t.font = font;
            var trt2 = tgo.GetComponent<RectTransform>();
            trt2.anchorMin = Vector2.zero; trt2.anchorMax = Vector2.one; trt2.sizeDelta = Vector2.zero;

            var brt = bgo.GetComponent<RectTransform>();
            int col = i % cols, row = i / cols;
            float sx = 375 - (cols * bw + (cols - 1) * gap) / 2f + bw / 2f;
            float sy = 680 - row * (bh + gap);
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
        var visible = new List<Bowl>();
        foreach (var b in bowls) if (!b.isCompleted) visible.Add(b);
        for (int i = 0; i < visible.Count; i++)
        {
            var bowl = visible[i];
            var go = Instantiate(bowlItemPf, bowlArea);
            go.GetComponent<RectTransform>().anchoredPosition = BowlPos(i, visible.Count);
            int bid = bowl.bowlId;
            bowlIdToGO[bid] = go;
            var btn = go.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => gm.OnBowlClicked(bid));
            }
            go.transform.localScale = (bid == gm.selectedBowlId) ? Vector3.one * 1.3f : Vector3.one;
            BuildFoodStack(go, bowl);
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
        float startY = -(bowl.foods.Count - 1) * overlap / 2f;

        for (int j = 0; j < bowl.foods.Count; j++)
        {
            var icon = Instantiate(foodIconPf, stack);
            var irt = icon.GetComponent<RectTransform>();
            irt.anchoredPosition = new Vector2(0, startY + j * overlap);
            irt.sizeDelta = new Vector2(sz, sz);
            var le = icon.GetComponent<LayoutElement>();
            if (le != null) { le.preferredWidth = sz; le.preferredHeight = sz; }
            var img = icon.GetComponent<Image>();
            if (img) { var s = GetFoodSprite(bowl.foods[j]); if (s) img.sprite = s; }
        }
    }

    Sprite GetFoodSprite(FoodType type) { int i = ((int)type % 15) + 1; return Resources.Load<Sprite>($"ArtFoods/food{i:D2}"); }

    #region 动画 — 倒食物
    IEnumerator PourAnimation(int fromId, int toId, int count)
    {
        yield return null;

        if (!bowlIdToGO.ContainsKey(fromId) || !bowlIdToGO.ContainsKey(toId))
            { gm.fsm?.ChangeState<IdleState>(); yield break; }
        var fromGO = bowlIdToGO[fromId];
        var toGO = bowlIdToGO[toId];
        if (fromGO == null || toGO == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }
        var fromRT = fromGO.GetComponent<RectTransform>();
        var toRT = toGO.GetComponent<RectTransform>();
        var fromStack = FindGO(fromGO, "FoodStack")?.transform;
        var toStack = FindGO(toGO, "FoodStack")?.transform;
        if (fromStack == null || toStack == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }

        // 收集要移动的 count 个食物图标（从顶往下数）
        int moveCount = Mathf.Min(count, fromStack.childCount);
        var movingFoods = new List<GameObject>();
        for (int i = 0; i < moveCount; i++)
        {
            int idx = fromStack.childCount - 1 - i;
            movingFoods.Add(fromStack.GetChild(idx).gameObject);
        }

        // 判断目标碗是否已达成
        var targetBowl = gm.pour.GetBowl(toId);
        bool bowlCompleted = targetBowl != null && targetBowl.isCompleted;

        Vector3 fromOrigPos = fromRT.anchoredPosition3D;
        Vector3 targetPos = toRT.anchoredPosition3D + new Vector3(
            fromRT.anchoredPosition.x < toRT.anchoredPosition.x ? -50 : 50, 70, 0);

        // 1. 源碗移到目标碗旁
        float dur = 0.2f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            if (fromRT == null || toRT == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }
            fromRT.anchoredPosition3D = Vector3.Lerp(fromOrigPos, targetPos, t / dur);
            yield return null;
        }
        if (fromRT == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }
        fromRT.anchoredPosition3D = targetPos;

        // 2. 倾斜
        float tilt = fromRT.anchoredPosition.x < toRT.anchoredPosition.x ? -80f : 80f;
        Quaternion fromOrigRot = fromRT.localRotation;
        Quaternion targetRot = Quaternion.Euler(0, 0, tilt);
        for (float t = 0; t < 0.15f; t += Time.deltaTime)
        {
            if (fromRT == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }
            fromRT.localRotation = Quaternion.Lerp(fromOrigRot, targetRot, t / 0.15f);
            yield return null;
        }
        if (fromRT == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }
        fromRT.localRotation = targetRot;

        // 3. 食物全部飞过去
        if (toStack != null)
        {
            for (int i = movingFoods.Count - 1; i >= 0; i--)
            {
                movingFoods[i].transform.SetParent(toStack, false);
                movingFoods[i].transform.SetAsLastSibling();
            }
        }
        yield return new WaitForSeconds(0.15f);

        // 4. 回正 + 回位
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            if (fromRT == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }
            fromRT.anchoredPosition3D = Vector3.Lerp(targetPos, fromOrigPos, t / 0.2f);
            fromRT.localRotation = Quaternion.Lerp(targetRot, fromOrigRot, t / 0.2f);
            yield return null;
        }
        if (fromRT == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }
        fromRT.anchoredPosition3D = fromOrigPos;
        fromRT.localRotation = fromOrigRot;

        // 碗未达成：重建UI
        if (!bowlCompleted)
        {
            BuildBowls();
            gm.fsm?.ChangeState<IdleState>();
        }
    }
    #endregion

    #region 动画 — 宠物喂食
    IEnumerator FeedAnimation(int bowlId, PetType petType)
    {
        // 等倒入动画先结束（PourAnimation 约 0.55s + 容错）
        yield return new WaitForSeconds(0.7f);

        gm.fsm?.ChangeState<FeedingState>();

        if (!bowlIdToGO.ContainsKey(bowlId)) { gm.fsm?.ChangeState<IdleState>(); yield break; }
        var bowlGO = bowlIdToGO[bowlId];
        if (bowlGO == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }

        // 找到匹配的宠物 GO（根据标签文字）
        var targetLabel = PetCN(petType);
        GameObject petGO = null;
        foreach (var go in petGOs)
        {
            if (go == null) continue;
            var label = FindC<Text>(go, "QueueLabel");
            if (label != null && label.text == targetLabel) { petGO = go; break; }
        }
        if (petGO == null) { BuildBowls(); BuildPets(); gm.fsm?.ChangeState<IdleState>(); yield break; }

        var bowlRT = bowlGO.GetComponent<RectTransform>();
        var petRT = petGO.GetComponent<RectTransform>();

        // CRITICAL: 碗和宠物在不同父节点（bowlArea / petArea）
        // 必须通过世界坐标转换
        Vector3 bowlOrigLocal = bowlRT.anchoredPosition3D;
        Vector3 petWorldPos = petRT.position;
        Vector3 bowlTargetWorld = petWorldPos + new Vector3(0, 50, 0);
        Vector3 bowlTargetLocal = bowlArea.InverseTransformPoint(bowlTargetWorld);

        // 1. 碗飞到宠物旁（世界坐标转换）
        float dur = 0.35f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            if (bowlRT == null || petRT == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }
            bowlRT.anchoredPosition3D = Vector3.Lerp(bowlOrigLocal, bowlTargetLocal, t / dur);
            yield return null;
        }
        if (bowlRT == null || petRT == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }
        bowlRT.anchoredPosition3D = bowlTargetLocal;

        // 2. 宠物头顶复制碗（含食物）
        var headBowl = Instantiate(bowlItemPf, petGO.transform);
        headBowl.name = "HeadBowl";
        var hrt = headBowl.GetComponent<RectTransform>();
        hrt.anchoredPosition = new Vector2(0, 80);
        hrt.sizeDelta = new Vector2(80, 90);
        var srcStack = FindGO(bowlGO, "FoodStack");
        var dstStack = FindGO(headBowl, "FoodStack");
        if (srcStack != null && dstStack != null)
        {
            foreach (Transform child in srcStack.transform)
                Instantiate(child.gameObject, dstStack.transform);
        }
        var hbtn = headBowl.GetComponent<Button>();
        if (hbtn) hbtn.enabled = false;
        var dm = FindGO(headBowl, "DoneMark");
        if (dm) dm.SetActive(false);

        // 3. 原碗缩小消失
        float shrink = 0.25f;
        for (float t = 0; t < shrink; t += Time.deltaTime)
        {
            if (bowlGO == null) break;
            bowlGO.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t / shrink);
            yield return null;
        }
        if (bowlGO != null) Destroy(bowlGO);
        bowlIdToGO.Remove(bowlId);

        yield return new WaitForSeconds(0.1f);

        // 4. 宠物弹一下
        Vector3 petOrigLocal = petRT.anchoredPosition3D;
        Vector3 petBounceLocal = petOrigLocal + new Vector3(0, 25, 0);
        float bounce = 0.2f;
        for (float t = 0; t < bounce; t += Time.deltaTime)
        {
            if (petRT == null) break;
            petRT.anchoredPosition3D = Vector3.Lerp(petOrigLocal, petBounceLocal, t / bounce);
            yield return null;
        }
        if (petRT == null) { gm.fsm?.ChangeState<IdleState>(); yield break; }

        // 5. 宠物+头顶碗一起慢左移出屏
        float slide = 0.7f;
        Vector3 petOffLocal = petBounceLocal + new Vector3(-650, 30, 0);
        for (float t = 0; t < slide; t += Time.deltaTime)
        {
            if (petGO == null) break;
            petRT.anchoredPosition3D = Vector3.Lerp(petBounceLocal, petOffLocal, t / slide);
            yield return null;
        }

        // 隐藏并销毁旧宠物GO
        if (petGO != null)
        {
            petGOs.Remove(petGO);
            Destroy(petGO);
        }

        // 6. 重建UI（BuildBowls会基于数据重建所有碗，BuildPets基于队列重建宠物）
        BuildBowls();
        BuildPets();
        UpdateHUD();

        gm.CheckWin();
        // CheckWin 通关会切 WinState，否则回 Idle
        if (gm.fsm?.CurrentState is not WinState)
        {
            // 检测死局
            if (gm.CheckDeadlock())
            {
                Debug.Log("[PetGameUI] 检测到死局! 可触发看广告+1碗");
                // TODO: 接入广告 SDK 后，这里弹出死局救援弹窗
                // 暂时自动加一个碗方便测试
                gm.AddBowl();
                BuildBowls();
            }
            gm.fsm?.ChangeState<IdleState>();
        }
    }
    #endregion

    /// <summary>停止所有动画协程（切关/重开时调用）</summary>
    public void StopAnimations() { StopAllCoroutines(); }

    void UpdateHUD()
    {
        if (txtLevel) txtLevel.text = $"第{gm.currentLevelId}关";
        if (txtScore) txtScore.text = $"得分:{gm.GetScore()}/{gm.targetScore}";
        if (txtStep) txtStep.text = $"步数:{gm.pour.totalMoves}";
        UpdateCleanerHUD();
    }

    /// <summary>更新铲屎官等级/货币 HUD（叠加显示在 HUD 顶部）</summary>
    private GameObject cleanerHUD;
    private Text txtTitle, txtFish, txtExp;
    void UpdateCleanerHUD()
    {
        if (cleanerHUD == null) BuildCleanerHUD();
        if (txtTitle) txtTitle.text = $"{SaveSystem.GetCurrentTitle()}";
        if (txtFish) txtFish.text = $"{SaveSystem.Data.fishDiscount}";
        if (txtExp)
        {
            int toNext = SaveSystem.ExpToNextLevel();
            txtExp.text = toNext > 0 ? $"下一级:{toNext}" : "MAX";
        }
    }

    void BuildCleanerHUD()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cleanerHUD = new GameObject("CleanerHUD", typeof(RectTransform));
        cleanerHUD.transform.SetParent(transform, false);
        var rt = cleanerHUD.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.9f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = Vector2.zero;

        // 称号（左上）
        var titleGO = new GameObject("CleanerTitle", typeof(RectTransform));
        titleGO.transform.SetParent(cleanerHUD.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.02f, 0.5f);
        titleRT.anchorMax = new Vector2(0.3f, 0.95f);
        titleRT.sizeDelta = Vector2.zero;
        txtTitle = titleGO.AddComponent<Text>();
        txtTitle.fontSize = 16;
        txtTitle.color = new Color(1f, 0.84f, 0f);
        txtTitle.alignment = TextAnchor.MiddleLeft;
        txtTitle.font = font;

        // 小鱼干（右上）
        var fishGO = new GameObject("FishDisplay", typeof(RectTransform));
        fishGO.transform.SetParent(cleanerHUD.transform, false);
        var fishRT = fishGO.GetComponent<RectTransform>();
        fishRT.anchorMin = new Vector2(0.7f, 0.5f);
        fishRT.anchorMax = new Vector2(0.98f, 0.95f);
        fishRT.sizeDelta = Vector2.zero;
        txtFish = fishGO.AddComponent<Text>();
        txtFish.fontSize = 16;
        txtFish.color = new Color(1f, 0.8f, 0.3f);
        txtFish.alignment = TextAnchor.MiddleRight;
        txtFish.font = font;

        // 经验（右上下方）
        var expGO = new GameObject("ExpDisplay", typeof(RectTransform));
        expGO.transform.SetParent(cleanerHUD.transform, false);
        var expRT = expGO.GetComponent<RectTransform>();
        expRT.anchorMin = new Vector2(0.7f, 0.05f);
        expRT.anchorMax = new Vector2(0.98f, 0.45f);
        expRT.sizeDelta = Vector2.zero;
        txtExp = expGO.AddComponent<Text>();
        txtExp.fontSize = 12;
        txtExp.color = new Color(0.7f, 0.7f, 0.7f);
        txtExp.alignment = TextAnchor.MiddleRight;
        txtExp.font = font;
    }

    void OnWin(int stars)
    {
        // 从 PetGameManager 获取完整结算结果
        lastResult = gm.GetCurrentLevelResult();
        adRewardClaimed = false;

        if (resultOverlay) resultOverlay.SetActive(true);
        if (txtResultTitle) txtResultTitle.text = "通关!";
        if (txtStars) txtStars.text = new string((char)9733, stars) + new string((char)9734, 3 - stars);

        BuildRewardPanel();
    }

    /// <summary>构建结算页奖励面板（小鱼干/徽章/经验 + 看广告翻倍按钮）</summary>
    void BuildRewardPanel()
    {
        if (rewardPanel != null) Destroy(rewardPanel);

        rewardPanel = new GameObject("RewardPanel", typeof(RectTransform));
        rewardPanel.transform.SetParent(resultOverlay?.transform ?? transform, false);
        var rt = rewardPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.15f, 0.15f);
        rt.anchorMax = new Vector2(0.85f, 0.6f);
        rt.sizeDelta = Vector2.zero;
        var bg = rewardPanel.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.12f, 0.2f, 0.95f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // --- 奖励行 ---
        float yPos = 0.35f;
        var rewards = new (string, int, Color)[]
        {
            ("+ " + lastResult.fishReward, lastResult.fishReward, new Color(1f, 0.8f, 0.3f)),
            ("+ " + lastResult.badgeReward, lastResult.badgeReward, new Color(0.9f, 0.75f, 0.3f)),
            ("+ " + lastResult.expReward + " EXP", lastResult.expReward, new Color(0.4f, 0.8f, 1f)),
        };
        string[] labels = { "小鱼干", "救助徽章", "经验" };

        for (int i = 0; i < rewards.Length; i++)
        {
            if (rewards[i].Item2 <= 0 && i == 1) continue; // 徽章为0不显示

            var row = new GameObject($"Reward{i}", typeof(RectTransform));
            row.transform.SetParent(rewardPanel.transform, false);
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.1f, yPos - i * 0.12f);
            rrt.anchorMax = new Vector2(0.9f, yPos - i * 0.12f + 0.1f);
            rrt.sizeDelta = Vector2.zero;
            var t = row.AddComponent<Text>();
            t.text = $"{labels[i]}  {rewards[i].Item1}";
            t.fontSize = 22;
            t.color = rewards[i].Item3;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = font;
        }

        // --- 升级提示 ---
        if (lastResult.leveledUp)
        {
            var upGO = new GameObject("LevelUpBanner", typeof(RectTransform));
            upGO.transform.SetParent(rewardPanel.transform, false);
            var urt = upGO.GetComponent<RectTransform>();
            urt.anchorMin = new Vector2(0.05f, 0.85f);
            urt.anchorMax = new Vector2(0.95f, 0.98f);
            urt.sizeDelta = Vector2.zero;
            var ut = upGO.AddComponent<Text>();
            ut.text = $"恭喜晋升 → {lastResult.newTitle}!";
            ut.fontSize = 26;
            ut.color = new Color(1f, 0.84f, 0f);
            ut.alignment = TextAnchor.MiddleCenter;
            ut.fontStyle = FontStyle.Bold;
            ut.font = font;
        }

        // --- 看广告翻倍按钮 ---
        if (lastResult.fishReward > 0)
        {
            var adGO = new GameObject("BtnWatchAd", typeof(RectTransform));
            adGO.transform.SetParent(rewardPanel.transform, false);
            var art = adGO.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.2f, 0.02f);
            art.anchorMax = new Vector2(0.8f, 0.14f);
            art.sizeDelta = Vector2.zero;
            var adImg = adGO.AddComponent<Image>();
            adImg.color = new Color(0.95f, 0.6f, 0.1f);
            btnWatchAd = adGO.AddComponent<Button>();
            var adText = new GameObject("T", typeof(RectTransform));
            adText.transform.SetParent(adGO.transform, false);
            var adtrt = adText.GetComponent<RectTransform>();
            adtrt.anchorMin = Vector2.zero; adtrt.anchorMax = Vector2.one; adtrt.sizeDelta = Vector2.zero;
            var at = adText.AddComponent<Text>();
            at.text = $"看广告 小鱼干x{lastResult.fishReward * 2}";
            at.fontSize = 18;
            at.color = Color.white;
            at.alignment = TextAnchor.MiddleCenter;
            at.font = font;
            btnWatchAd.onClick.AddListener(OnWatchAdForDouble);
        }
    }

    /// <summary>看广告翻倍奖励回调（目前无广告SDK，先模拟）</summary>
    void OnWatchAdForDouble()
    {
        if (adRewardClaimed) return;
        adRewardClaimed = true;

        // TODO: 接入真实广告SDK：SDK.ShowRewardedAd(() => { ... })
        int bonus = lastResult.fishReward; // 翻倍 = 再给一份
        SaveSystem.AddFish(bonus);
        Debug.Log($"[结算] 看广告翻倍! 小鱼干+{bonus}");

        if (btnWatchAd != null) btnWatchAd.gameObject.SetActive(false);

        // 更新面板显示
        BuildRewardPanel();
        adRewardClaimed = true; // 标记已领取
        if (rewardPanel != null)
        {
            // 简单提示已翻倍
            var tip = new GameObject("AdDone", typeof(RectTransform));
            tip.transform.SetParent(rewardPanel.transform, false);
            var trt = tip.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.2f, 0.02f);
            trt.anchorMax = new Vector2(0.8f, 0.14f);
            trt.sizeDelta = Vector2.zero;
            var t = tip.AddComponent<Text>();
            t.text = $"已翻倍! +{bonus} 小鱼干";
            t.fontSize = 20;
            t.color = new Color(0.4f, 1f, 0.4f);
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
    void OnFail() { if (resultOverlay) resultOverlay.SetActive(true); if (txtResultTitle) txtResultTitle.text = "失败..."; }
    void Restart()
    {
        StopAnimations();
        if (rewardPanel != null) { Destroy(rewardPanel); rewardPanel = null; }
        if (resultOverlay) resultOverlay.SetActive(false);
        gm.StartLevel(gm.currentLevelId);
        BuildLevel();
    }
    void ShuffleCurrentBowl() { if (gm.selectedBowlId >= 0) { gm.pour.ShuffleBowl(gm.selectedBowlId); gm.selectedBowlId = -1; gm.onSelectionChanged.Invoke(); BuildBowls(); } }
    void NextLevel()
    {
        StopAnimations();
        if (rewardPanel != null) { Destroy(rewardPanel); rewardPanel = null; }
        if (resultOverlay) resultOverlay.SetActive(false);
        // 跟随存档的解锁进度，不越界
        int next = gm.currentLevelId + 1;
        if (next > gm.LevelCount) next = gm.LevelCount;
        gm.currentLevelId = next;
        SaveSystem.Data.currentLevelId = next;
        SaveSystem.Save();
        gm.StartLevel(next);
        BuildLevel();
    }
    public void BackToMenu()
    {
        StopAnimations();
        if (rewardPanel != null) { Destroy(rewardPanel); rewardPanel = null; }
        if (resultOverlay) resultOverlay.SetActive(false);
        // 多场景模式：返回主菜单场景
        GameSceneManager.LoadMenu();
    }

    T FindC<T>(GameObject root, string n) where T : Component { foreach (var c in root.GetComponentsInChildren<T>(true)) if (c.name == n) return c; return null; }
    Text FindT(string n) => FindC<Text>(gameHUD, n);
    Button FindB(string n) => FindC<Button>(gameHUD, n);
    GameObject FindGO(GameObject root, string n) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t.gameObject; return null; }
    GameObject FindGO(string n) => FindGO(gameHUD, n);
    void Clear(Transform t) { foreach (Transform c in t) Destroy(c.gameObject); }
    string PetCN(PetType p) => p switch { PetType.Cat => "橘猫", PetType.Dog => "柴犬", PetType.Hamster => "仓鼠", PetType.Parrot => "鹦鹉", PetType.Fish => "金鱼", PetType.Rabbit => "垂耳兔", _ => p.ToString() };
}
