using System.Collections;
using System.Collections.Generic;
using System.Text;
using F8Framework.Core;
using PetGame;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PetGameUI : MonoBehaviour
{
    private PetGameManager gm;
    private GameObject gameHUD;
    private GameObject petItemPf, bowlItemPf, foodIconPf, gameHUDPf;
    private Dictionary<FoodType, Sprite> foodCache = new Dictionary<FoodType, Sprite>();
    private Dictionary<string, Sprite> faceCache = new Dictionary<string, Sprite>();

    private Transform petArea, bowlArea;
    private Text txtLevel, txtScore, txtStep, txtStars, txtResultTitle;
    private GameObject resultOverlay;
    private Button btnUndo, btnAddBowl, btnShuffle, btnRestart, btnNext, btnBack;

    // GM 调试：通关步骤按钮 + 步骤面板
    private Button btnGM;
    private GameObject gmPanel;

    // IAA 提示按钮（正式功能，常显）+ 提示高亮/文字的临时对象
    private Button btnHint;
    private GameObject hintRoot;

    // IAA 道具剩余次数角标（显示在 4 个工具按钮上）
    private Text txtUndoCount, txtAddBowlCount, txtShuffleCount, txtHintCount;

    // 死局救援弹窗
    private GameObject deadlockPanel;

    // 结算页动态元素
    private LevelResult lastResult;
    private GameObject rewardPanel;
    private Button btnWatchAd, btnBackMenu;
    private bool adRewardClaimed;

    private List<GameObject> petGOs = new List<GameObject>();
    private List<GameObject> bowlGOs = new List<GameObject>();
    private Dictionary<int, GameObject> bowlIdToGO = new Dictionary<int, GameObject>();

    private Dictionary<int, Vector2> bowlPositions = new Dictionary<int, Vector2>();
    // 每个碗固定占一个格子 slot（按 bowlId 分配，绝不因其他碗完成/移除而变），用于稳定定位、避免重排
    private Dictionary<int, int> bowlSlot = new Dictionary<int, int>();
    private int nextSlot = 0;

    void Start()
    {
        gm = PetGameManager.Instance;
        if (gm == null) return;

        EnsureCamera();   // 没有相机会导致 Game View 无法渲染

        gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(750, 1334);
        sc.matchWidthOrHeight = 1f;
        gameObject.AddComponent<GraphicRaycaster>();

        // 异步加载 4 个 prefab（抖音小游戏禁止同步等待）
        int pendingCore = 4;
        System.Action coreDec = () => { if (--pendingCore == 0) OnCoreReady(); };
        ResLoader.LoadPrefab("Assets/Prefabs/UI/PrefabsV2/PetItem.prefab").Completed += h => { if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null) petItemPf = h.Result; coreDec(); };
        ResLoader.LoadPrefab("Assets/Prefabs/UI/PrefabsV2/BowlItem.prefab").Completed += h => { if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null) bowlItemPf = h.Result; coreDec(); };
        ResLoader.LoadPrefab("Assets/Prefabs/UI/PrefabsV2/FoodIcon.prefab").Completed += h => { if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null) foodIconPf = h.Result; coreDec(); };
        ResLoader.LoadPrefab("Assets/Prefabs/UI/PrefabsV2/GameHUD.prefab").Completed += h => { if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null) gameHUDPf = h.Result; coreDec(); };
    }

    /// <summary>兜底相机：没有 Camera 时 Unity Game View 无法渲染，PetGameScene 里可能没挂相机</summary>
    void EnsureCamera()
    {
        if (Camera.main != null) return;
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.96f, 0.94f, 0.90f); // 木质纸感底色
        cam.cullingMask = 0;
        cam.orthographic = true;
    }

    void OnCoreReady()
    {
        if (gameHUDPf == null) { Debug.LogError("[PetGameUI] GameHUD 加载失败"); return; }
        gameHUD = Instantiate(gameHUDPf, transform);
        GameFont.ApplyAll(gameHUD);
        gameHUD.name = "GameHUD";

        // 预加载 15 食物 + 6 宠物 neutral 脸到缓存
        int pending = 15 + 6;
        System.Action dec = () => { if (--pending == 0) OnAssetsReady(); };
        for (int i = 0; i < 15; i++)
        {
            var type = (FoodType)i;
            string path = $"Assets/Art/PetGame/foods/food{i + 1:D2}.png";
            ResLoader.LoadSprite(path).Completed += h => { if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null) foodCache[type] = h.Result; dec(); };
        }
        foreach (PetType pt in System.Enum.GetValues(typeof(PetType)))
        {
            string key = pt.ToString().ToLower();
            string path = $"Assets/Art/PetGame/pets/{key}/neutral.png";
            ResLoader.LoadSprite(path).Completed += h => { if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null) faceCache[key] = h.Result; dec(); };
        }
    }

    void OnAssetsReady()
    {
        FindRefs();
        BindButtons();
        if (gm.fsm != null) BuildLevel();
        else ShowLevelSelect();

        // GM 调试按钮（仅开发期显示，由 GM.Enabled 控制）
        if (GM.Enabled) BuildGMButton();
        BuildHintButton();   // IAA 提示按钮（正式功能，始终显示）
    }

    void FindRefs()
    {
        txtLevel = FindT("LevelText"); txtScore = FindT("ScoreText");
        txtStep = FindT("StepText"); txtStars = FindT("Stars");
        txtResultTitle = FindT("Title");
        petArea = FindGO("PetArea")?.transform;
        bowlArea = FindGO("BowlArea")?.transform;
        resultOverlay = FindGO("ResultOverlay");

        // 关闭 BowlArea 上的 GridLayoutGroup：碗用 anchoredPosition 手动定位（按 bowlId 固定 slot）。
        // 若保留网格布局组，移除/完成一只碗会触发网格重排，其余碗位置全变（用户反馈“少一个碗就重排”）。
        if (bowlArea != null)
        {
            var glg = bowlArea.GetComponent<GridLayoutGroup>();
            if (glg != null) glg.enabled = false;
        }
        btnUndo = FindB("btnUndo"); btnAddBowl = FindB("btnAddBowl");
        btnShuffle = FindB("btnShuffle"); btnRestart = FindB("btnRestart");
        btnNext = FindB("btnNext"); btnBack = FindB("btnBack");
    }

    void BindButtons()
    {
        btnUndo?.onClick.AddListener(() => TryUseTool(SaveSystem.ToolType.Undo, () => { gm.Undo(); RebuildAll(); }));
        btnAddBowl?.onClick.AddListener(() => TryUseTool(SaveSystem.ToolType.AddBowl, () => { gm.AddBowl(); BuildBowls(); }));
        btnShuffle?.onClick.AddListener(() => TryUseTool(SaveSystem.ToolType.Shuffle, () => { ShuffleCurrentBowl(); }));
        InitItemCountBadges();
        btnRestart?.onClick.AddListener(Restart);
        btnNext?.onClick.AddListener(NextLevel);
        btnBack?.onClick.AddListener(BackToMenu);
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
    void BuildLevel() { ResetBowlLayout(); BuildPets(); BuildBowls(); UpdateHUD(); }
    /// <summary>关卡开始/重开时清空碗位置缓存，重新按 bowlId 分配稳定格子</summary>
    void ResetBowlLayout()
    {
        bowlPositions.Clear();
        bowlSlot.Clear();
        nextSlot = 0;
    }

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

        // --- 顶部标题 + 玩家信息 ---
        var tt = new GameObject("Title", typeof(RectTransform)).AddComponent<SystemFontText>();
        tt.transform.SetParent(levelSelectPanel.transform, false);
        tt.text = "疯狂铲屎官"; tt.fontSize = 40; tt.color = Color.white;
        tt.alignment = TextAnchor.MiddleCenter; GameFont.Apply(tt);
        var trt = tt.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.1f, 0.85f); trt.anchorMax = new Vector2(0.9f, 0.93f);
        trt.sizeDelta = Vector2.zero;

        // 玩家状态栏：称号 + 小鱼干 + 总星数
        var info = new GameObject("PlayerInfo", typeof(RectTransform));
        info.transform.SetParent(levelSelectPanel.transform, false);
        var irt = info.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.05f, 0.78f); irt.anchorMax = new Vector2(0.95f, 0.84f);
        irt.sizeDelta = Vector2.zero;
        var it = info.AddComponent<SystemFontText>();
        int totalStars = SaveSystem.TotalStars;
        it.text = $"{SaveSystem.GetCurrentTitle()}  |  🪙{SaveSystem.Data.gold}  🐟{SaveSystem.Data.fishDiscount}  徽章:{SaveSystem.Data.rescueBadge}  |  总星数:{totalStars}";
        it.fontSize = 18;
        it.color = new Color(1f, 0.84f, 0f);
        it.alignment = TextAnchor.MiddleCenter;
        GameFont.Apply(it);

        // --- 关卡按钮 ---
        int cols = 4; float bw = 150, bh = 90, gap = 15;
        int highest = SaveSystem.Data.highestUnlockedLevel;
#if UNITY_EDITOR
        highest = gm.LevelCount; // 开发期：全部解锁，方便测试任意关卡
#endif
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
            var t = tgo.AddComponent<SystemFontText>();
            string lockIcon = unlocked ? "" : "\n[锁定]";
            string starStr = stars > 0 ? "\n" + new string((char)9733, stars) : "";
            t.text = $"{lid}\n{gm.GetLevelName(lid)}{starStr}{lockIcon}";
            t.fontSize = 13; t.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            t.alignment = TextAnchor.MiddleCenter; GameFont.Apply(t);
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
    /// <summary>
    /// 按固定 slot（格子序号）计算位置。随机偏移只依赖 slot，因此某只碗的位置
    /// 完全由它自己的 bowlId 决定，其他碗完成/移除都不会让它移动 —— 实现"不重排"。
    /// </summary>
    Vector2 BowlPos(int slot)
    {
        var rt = bowlArea.GetComponent<RectTransform>();
        float w = rt.rect.width - 140, h = rt.rect.height - 140;
        int cols = Mathf.Clamp(Mathf.Max(1, (int)(w / 170f)), 1, 4);
        int row = slot / cols, col = slot % cols;
        float x = -w / 2 + (w / cols) * (col + 0.5f);
        float y = h / 2 - 70 - row * 140;
        var rng = new System.Random(slot * 137 + 7919);
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
            GameFont.ApplyAll(go);
            var label = FindC<Text>(go, "QueueLabel");
            if (label) label.text = PetCN(pet);
            var face = FindC<Image>(go, "PetFace");
            var animPlayer = FindC<AnimationPlayer>(go, "PetFace");
            if (pet == PetType.Cat)
            {
                // 如果预制体尚未生成 AnimationPlayer（需重跑 生成UI预制体(v2)），
                // 运行时动态挂上去，这样不依赖预制体是否已重建。
                if (animPlayer == null && face != null)
                {
                    animPlayer = face.gameObject.AddComponent<AnimationPlayer>();
                    animPlayer.uiImage = face;
                    animPlayer.petName = "cat_orange";
                    animPlayer.autoPlay = false;
                }
                if (animPlayer)
                {
                    animPlayer.enabled = true;
                    animPlayer.petName = "cat_orange";
                    animPlayer.Play("Idle");
                    animPlayer.frameRate = 8f; // 加快 idle，动作更明显
                    StartCoroutine(CatBreath(go)); // 排队时轻微呼吸缩放，强化"在动"的观感
                }
            }
            else
            {
                if (animPlayer) animPlayer.enabled = false;
                if (face) { if (faceCache.TryGetValue(pet.ToString().ToLower(), out var s) && s != null) face.sprite = s; }
            }
            petGOs.Add(go);
        }
    }

    /// <summary>排队橘猫的"呼吸"缩放，让 idle 动画更明显（petGO 销毁后自动停止）</summary>
    IEnumerator CatBreath(GameObject catGO)
    {
        if (catGO == null) yield break;
        float t = Random.Range(0f, 6.28f); // 错开相位
        while (catGO != null)
        {
            t += Time.deltaTime;
            float s = 1.08f + Mathf.Sin(t * 3f) * 0.06f; // 1.02 ~ 1.14
            catGO.transform.localScale = new Vector3(s, s, 1);
            yield return null;
        }
    }

    void BuildBowls()
    {
        if (bowlArea == null || bowlItemPf == null) return;

        var visible = new List<Bowl>();
        foreach (var b in gm.GetBowls()) if (!b.isCompleted) visible.Add(b);

        var visibleIds = new HashSet<int>();
        foreach (var b in visible) visibleIds.Add(b.bowlId);

        // 只移除已达成/消失的碗，保留其他碗的 GameObject 和位置
        var idsToRemove = new List<int>();
        foreach (var kv in bowlIdToGO)
        {
            if (!visibleIds.Contains(kv.Key))
            {
                if (kv.Value != null) Destroy(kv.Value);
                idsToRemove.Add(kv.Key);
            }
        }
        foreach (var id in idsToRemove)
        {
            bowlIdToGO.Remove(id);
            bowlPositions.Remove(id);
        }
        bowlGOs.RemoveAll(go => go == null);

        // 更新或创建碗
        var newBowlGOs = new List<GameObject>();
        for (int i = 0; i < visible.Count; i++)
        {
            var bowl = visible[i];
            // 给每只碗分配一个固定 slot（首次出现时分配，之后不变）
            if (!bowlSlot.ContainsKey(bowl.bowlId)) bowlSlot[bowl.bowlId] = nextSlot++;

            GameObject go;
            if (!bowlIdToGO.TryGetValue(bowl.bowlId, out go) || go == null)
            {
                go = Instantiate(bowlItemPf, bowlArea);
                GameFont.ApplyAll(go);
                Vector2 pos;
                if (!bowlPositions.TryGetValue(bowl.bowlId, out pos))
                {
                    pos = BowlPos(bowlSlot[bowl.bowlId]);
                    bowlPositions[bowl.bowlId] = pos;
                }
                var brt = go.GetComponent<RectTransform>();
                brt.anchoredPosition = pos;
                // 关掉网格布局组后，碗尺寸不再被 Grid 强制，这里显式给定原 cell 尺寸保持一致观感
                brt.sizeDelta = new Vector2(140f, 170f);
                bowlIdToGO[bowl.bowlId] = go;
            }

            var btn = go.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                int bid = bowl.bowlId;
                btn.onClick.AddListener(() => gm.OnBowlClicked(bid));
            }
            go.transform.localScale = (bowl.bowlId == gm.selectedBowlId) ? Vector3.one * 1.3f : Vector3.one;
            BuildFoodStack(go, bowl);
            newBowlGOs.Add(go);
        }
        bowlGOs = newBowlGOs;
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
            GameFont.ApplyAll(icon);
            var irt = icon.GetComponent<RectTransform>();
            irt.anchoredPosition = new Vector2(0, startY + j * overlap);
            irt.sizeDelta = new Vector2(sz, sz);
            var le = icon.GetComponent<LayoutElement>();
            if (le != null) { le.preferredWidth = sz; le.preferredHeight = sz; }
            var img = icon.GetComponent<Image>();
            if (img) { var s = GetFoodSprite(bowl.foods[j]); if (s) img.sprite = s; }
        }
    }

    Sprite GetFoodSprite(FoodType type) { foodCache.TryGetValue(type, out var s); return s; }

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
        // 兜底：petGOs 列表可能已被提前重建，直接从 PetArea 按标签再找一个能匹配的宠物 GO
        if (petGO == null)
        {
            var pa = GameObject.Find("PetArea")?.transform;
            if (pa != null)
            {
                foreach (Transform t in pa)
                {
                    var label = FindC<Text>(t.gameObject, "QueueLabel");
                    if (label != null && label.text == targetLabel) { petGO = t.gameObject; break; }
                }
            }
        }
        if (petGO == null)
        {
            // 即使找不到被喂宠物，也要给排前面的宠物弹不公平气泡
            if (!gm.lastFedIsFirst && petGOs.Count > 0 && petGOs[0] != null)
            {
                var frontRT = petGOs[0].GetComponent<RectTransform>();
                if (frontRT != null) ShowUnfairBubble(frontRT);
            }
            BuildBowls(); BuildPets(); gm.fsm?.ChangeState<IdleState>(); yield break;
        }

        var bowlRT = bowlGO.GetComponent<RectTransform>();
        var petRT = petGO.GetComponent<RectTransform>();
        var petAnim = FindC<AnimationPlayer>(petGO, "PetFace");

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

        // 碗到嘴边 -> 宠物吃动画
        if (petAnim != null && petAnim.enabled)
            petAnim.Play("Eat");

        // 不公平气泡：非首位匹配（喂了后排宠物，但更靠前的宠物还没吃到）时，
        // 由队列最前、仍没吃到食的宠物吐槽（设计核心爽点反馈，时机=碗移到被喂宠物时）
        if (!gm.lastFedIsFirst)
        {
            var frontGO = (petGOs.Count > 0 && petGOs[0] != null) ? petGOs[0] : null;
            if (frontGO == null)
            {
                var pa = GameObject.Find("PetArea")?.transform;
                if (pa != null && pa.childCount > 0) frontGO = pa.GetChild(0).gameObject;
            }
            if (frontGO != null)
            {
                var frontRT = frontGO.GetComponent<RectTransform>();
                if (frontRT != null) ShowUnfairBubble(frontRT);
            }
        }

        // 2. 宠物头顶复制碗（含食物）
        var headBowl = Instantiate(bowlItemPf, petGO.transform);
        GameFont.ApplyAll(headBowl);
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

        // 吃完准备离场 -> 走路动画，2倍速
        if (petAnim != null && petAnim.enabled)
        {
            petAnim.Play("Walk");
            petAnim.frameRate *= 2f;
        }

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
                Debug.Log("[PetGameUI] 检测到死局! 弹出救援弹窗");
                ShowDeadlockRescue();   // 弹窗处理后续（看广告+1碗/花金币+1碗/重来），不在此切状态
            }
            else
            {
                gm.fsm?.ChangeState<IdleState>();
            }
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
        UpdateItemCounts();
    }

    /// <summary>更新铲屎官等级/货币 HUD（叠加显示在 HUD 顶部）</summary>
    private GameObject cleanerHUD;
    private Text txtTitle, txtFish, txtExp;
    void UpdateCleanerHUD()
    {
        if (cleanerHUD == null) BuildCleanerHUD();
        if (txtTitle) txtTitle.text = $"{SaveSystem.GetCurrentTitle()}";
        if (txtFish) txtFish.text = $"🪙{SaveSystem.Data.gold}  🐟{SaveSystem.Data.fishDiscount}";
        if (txtExp)
        {
            int toNext = SaveSystem.ExpToNextLevel();
            txtExp.text = toNext > 0 ? $"下一级:{toNext}" : "MAX";
        }
    }

    void BuildCleanerHUD()
    {
        cleanerHUD = new GameObject("CleanerHUD", typeof(RectTransform));
        cleanerHUD.transform.SetParent(transform, false);
        var rt = cleanerHUD.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.9f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = Vector2.zero;
        // 半透明木质底色，让称号/小鱼干文字在场景背景上更清晰
        var chBg = cleanerHUD.AddComponent<Image>();
        chBg.color = new Color(0.20f, 0.14f, 0.10f, 0.35f);

        // 称号（左上）
        var titleGO = new GameObject("CleanerTitle", typeof(RectTransform));
        titleGO.transform.SetParent(cleanerHUD.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.02f, 0.5f);
        titleRT.anchorMax = new Vector2(0.3f, 0.95f);
        titleRT.sizeDelta = Vector2.zero;
        txtTitle = titleGO.AddComponent<SystemFontText>();
        txtTitle.fontSize = 16;
        txtTitle.color = new Color(1f, 0.84f, 0f);
        txtTitle.alignment = TextAnchor.MiddleLeft;
        GameFont.Apply(txtTitle);

        // 小鱼干（右上，含金币）
        var fishGO = new GameObject("FishDisplay", typeof(RectTransform));
        fishGO.transform.SetParent(cleanerHUD.transform, false);
        var fishRT = fishGO.GetComponent<RectTransform>();
        fishRT.anchorMin = new Vector2(0.5f, 0.5f);
        fishRT.anchorMax = new Vector2(0.98f, 0.95f);
        fishRT.sizeDelta = Vector2.zero;
        txtFish = fishGO.AddComponent<SystemFontText>();
        txtFish.fontSize = 16;
        txtFish.color = new Color(1f, 0.8f, 0.3f);
        txtFish.alignment = TextAnchor.MiddleRight;
        GameFont.Apply(txtFish);

        // 经验（右上下方）
        var expGO = new GameObject("ExpDisplay", typeof(RectTransform));
        expGO.transform.SetParent(cleanerHUD.transform, false);
        var expRT = expGO.GetComponent<RectTransform>();
        expRT.anchorMin = new Vector2(0.7f, 0.05f);
        expRT.anchorMax = new Vector2(0.98f, 0.45f);
        expRT.sizeDelta = Vector2.zero;
        txtExp = expGO.AddComponent<SystemFontText>();
        txtExp.fontSize = 12;
        txtExp.color = new Color(0.7f, 0.7f, 0.7f);
        txtExp.alignment = TextAnchor.MiddleRight;
        GameFont.Apply(txtExp);
    }

    void OnWin(int stars)
    {
        // 从 PetGameManager 获取完整结算结果
        lastResult = gm.GetCurrentLevelResult();
        adRewardClaimed = false;

        if (resultOverlay)
        {
            resultOverlay.SetActive(true);
            // 清掉 prefab 旧内容，完全用动态生成
            Clear(resultOverlay.transform);
            // 关键：关闭 ResultOverlay 上的 VerticalLayoutGroup。否则布局组会接管动态子物体的
            // 位置与尺寸（padding top=200 + 子物体 preferred 高度=0），导致结算内容全部塌陷不可见，
            // 只剩黑色背景图（用户反馈“结算只剩透明黑底”）。
            var vlg = resultOverlay.GetComponent<LayoutGroup>();
            if (vlg != null) vlg.enabled = false;
        }

        // 隐藏游戏内按钮，避免与结算面板的“下一关/回主菜单/看广告”按钮重叠
        if (gameHUD != null)
        {
            foreach (var btn in gameHUD.GetComponentsInChildren<Button>(true))
            {
                if (resultOverlay != null && btn.transform.IsChildOf(resultOverlay.transform)) continue;
                btn.gameObject.SetActive(false);
            }
        }
        foreach (var n in new[] { "BtnHint", "BtnGM" })
        {
            var go = GameObject.Find(n);
            if (go != null) go.SetActive(false);
        }

        BuildResultPanel(stars);
    }

    /// <summary>直接在 resultOverlay 内构建完整结算面板</summary>
    void BuildResultPanel(int stars)
    {
        var root = resultOverlay?.transform;
        if (root == null) return;

        // 标题
        {
            var tgo = new GameObject("ResultTitle", typeof(RectTransform));
            tgo.transform.SetParent(root, false);
            var trt = tgo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.1f, 0.75f); trt.anchorMax = new Vector2(0.9f, 0.88f);
            trt.sizeDelta = Vector2.zero;
            var t = tgo.AddComponent<SystemFontText>();
            t.text = $"🎉 第{lastResult.levelId}关 通关!";
            t.fontSize = 32; t.color = new Color(1f, 0.92f, 0.4f);
            t.alignment = TextAnchor.MiddleCenter; GameFont.Apply(t);
        }

        // 星级
        {
            var sgo = new GameObject("ResultStars", typeof(RectTransform));
            sgo.transform.SetParent(root, false);
            var srt = sgo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.1f, 0.62f); srt.anchorMax = new Vector2(0.9f, 0.74f);
            srt.sizeDelta = Vector2.zero;
            var st = sgo.AddComponent<SystemFontText>();
            st.text = new string((char)9733, stars) + new string((char)9734, 3 - stars);
            st.fontSize = 48; st.color = new Color(1f, 0.84f, 0f);
            st.alignment = TextAnchor.MiddleCenter; GameFont.Apply(st);
        }

        // 奖励行（金币 / 小鱼干 / 徽章(三星) / 经验）
        float yBase = 0.55f;
        float step = 0.08f;
        float rowH = 0.07f;
        var rewards = new System.Collections.Generic.List<(string label, int val, Color color)>
        {
            ("🪙 金币", lastResult.goldReward, new Color(1f, 0.84f, 0.2f)),
            ("🐟 小鱼干", lastResult.fishReward, new Color(1f, 0.8f, 0.3f)),
            ("⭐ 徽章", lastResult.badgeReward, new Color(0.9f, 0.75f, 0.3f)),
            ("📈 经验", lastResult.expReward, new Color(0.4f, 0.8f, 1f)),
        };
        int ri = 0;
        foreach (var r in rewards)
        {
            if (r.val <= 0) continue;
            var rgo = new GameObject($"Reward{ri}", typeof(RectTransform));
            rgo.transform.SetParent(root, false);
            var rrt = rgo.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.15f, yBase - ri * step);
            rrt.anchorMax = new Vector2(0.85f, yBase - ri * step + rowH);
            rrt.sizeDelta = Vector2.zero;
            var rt = rgo.AddComponent<SystemFontText>();
            rt.text = $"{r.label}  +{r.val}";
            rt.fontSize = 24; rt.color = r.color;
            rt.alignment = TextAnchor.MiddleCenter; GameFont.Apply(rt);
            ri++;
        }

        // 升级提示
        if (lastResult.leveledUp)
        {
            var upGO = new GameObject("LevelUpBanner", typeof(RectTransform));
            upGO.transform.SetParent(root, false);
            var urt = upGO.GetComponent<RectTransform>();
            urt.anchorMin = new Vector2(0.05f, 0.20f); urt.anchorMax = new Vector2(0.95f, 0.28f);
            urt.sizeDelta = Vector2.zero;
            var ut = upGO.AddComponent<SystemFontText>();
            ut.text = $"🎉 恭喜晋升 → {lastResult.newTitle}!";
            ut.fontSize = 24; ut.color = new Color(1f, 0.84f, 0f);
            ut.alignment = TextAnchor.MiddleCenter; GameFont.Apply(ut);
        }

        // 已翻倍提示（看完广告后重建面板时显示，放在 ad 按钮原位置）
        if (adRewardClaimed)
        {
            float tipY = 0.07f;
            var tipGO = new GameObject("AdDone", typeof(RectTransform));
            tipGO.transform.SetParent(root, false);
            var trt = tipGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.15f, tipY + 0.06f);
            trt.anchorMax = new Vector2(0.85f, tipY + 0.14f);
            trt.sizeDelta = Vector2.zero;
            var t = tipGO.AddComponent<SystemFontText>();
            t.text = $"✅ 已翻倍! 金币+{lastResult.goldReward} 小鱼干+{lastResult.fishReward}";
            t.fontSize = 20; t.color = new Color(0.4f, 1f, 0.4f);
            t.alignment = TextAnchor.MiddleCenter; GameFont.Apply(t);
        }

        // 看广告翻倍按钮
        float btnBaseY = 0.07f;
        if (lastResult.fishReward > 0 && !adRewardClaimed)
        {
            var adGO = new GameObject("BtnWatchAd", typeof(RectTransform));
            adGO.transform.SetParent(root, false);
            var art = adGO.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.15f, btnBaseY + 0.06f);
            art.anchorMax = new Vector2(0.85f, btnBaseY + 0.14f);
            art.sizeDelta = Vector2.zero;
            var adImg = adGO.AddComponent<Image>();
            adImg.color = new Color(0.95f, 0.6f, 0.1f);
            btnWatchAd = adGO.AddComponent<Button>();
            var adText = new GameObject("T", typeof(RectTransform));
            adText.transform.SetParent(adGO.transform, false);
            var adtrt = adText.GetComponent<RectTransform>();
            adtrt.anchorMin = Vector2.zero; adtrt.anchorMax = Vector2.one; adtrt.sizeDelta = Vector2.zero;
            var at = adText.AddComponent<SystemFontText>();
            at.text = "📺 看广告 金币+鱼干翻倍";
            at.fontSize = 20; at.color = Color.white;
            at.alignment = TextAnchor.MiddleCenter; GameFont.Apply(at);
            btnWatchAd.onClick.AddListener(OnWatchAdForDouble);
        }

        // 下一关 / 回主菜单 按钮
        {
            float bw = 0.38f, bh = 0.08f, by = 0.03f;

            MakeBtn(root, 0.05f, bw, by, bh, "▶ 下一关", new Color(0.89f, 0.48f, 0.32f), NextLevel);
            MakeBtn(root, 0.57f, bw, by, bh, "🏠 回主菜单", new Color(0.5f, 0.35f, 0.7f), BackToMenu);
        }
    }

    void MakeBtn(Transform parent, float x, float w, float y, float h, string label, Color color, UnityEngine.Events.UnityAction cb)
    {
        var bgo = new GameObject("Btn_" + label, typeof(RectTransform));
        bgo.transform.SetParent(parent, false);
        var brt = bgo.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(x, y); brt.anchorMax = new Vector2(x + w, y + h);
        brt.sizeDelta = Vector2.zero;
        var bimg = bgo.AddComponent<Image>();
        bimg.color = color;
        var bb = bgo.AddComponent<Button>();
        var btgo = new GameObject("T", typeof(RectTransform));
        btgo.transform.SetParent(bgo.transform, false);
        var btrt = btgo.GetComponent<RectTransform>();
        btrt.anchorMin = Vector2.zero; btrt.anchorMax = Vector2.one; btrt.sizeDelta = Vector2.zero;
        var bt = btgo.AddComponent<SystemFontText>();
        bt.text = label; bt.fontSize = 22; bt.color = Color.white;
        bt.alignment = TextAnchor.MiddleCenter; GameFont.Apply(bt);
        bb.onClick.AddListener(cb);
    }

    /// <summary>看广告翻倍奖励（走 AdManager；mock 或真 TTSDK）</summary>
    void OnWatchAdForDouble()
    {
        if (adRewardClaimed) return;
        AdManager.Instance.ShowRewardedAd(success =>
        {
            if (!success) return;
            adRewardClaimed = true;
            int bonusFish = lastResult.fishReward;   // 小鱼干翻倍 = 再给一份
            int bonusGold = lastResult.goldReward;   // 金币翻倍 = 再给一份
            SaveSystem.AddFish(bonusFish);
            SaveSystem.AddGold(bonusGold);
            Debug.Log($"[结算] 看广告翻倍! 金币+{bonusGold} 小鱼干+{bonusFish}");

            // 重建面板：先清空再生成，避免重复叠加
            if (resultOverlay != null)
            {
                Clear(resultOverlay.transform);
                BuildResultPanel(lastResult.stars);
            }
        });
    }
    void OnFail() { if (resultOverlay) resultOverlay.SetActive(true); if (txtResultTitle) txtResultTitle.text = "失败..."; }
    void ShowHUDButtons()
    {
        // OnWin 会隐藏游戏内 HUD 按钮（撤销/加空碗/洗牌/提示/GM），
        // 进下一关/重开时必须恢复，否则后续关卡永久丢失 IAA 按钮。
        if (gameHUD != null)
            foreach (var btn in gameHUD.GetComponentsInChildren<Button>(true))
                btn.gameObject.SetActive(true);
        foreach (var n in new[] { "BtnHint", "BtnGM" })
        {
            var go = GameObject.Find(n);
            if (go != null) go.SetActive(true);
        }
    }

    void Restart()
    {
        StopAnimations();
        if (rewardPanel != null) { Destroy(rewardPanel); rewardPanel = null; }
        if (resultOverlay) resultOverlay.SetActive(false);
        ShowHUDButtons();
        gm.StartLevel(gm.currentLevelId);
        BuildLevel();
    }
    void ShuffleCurrentBowl() { if (gm.selectedBowlId >= 0) { gm.pour.ShuffleBowl(gm.selectedBowlId); gm.selectedBowlId = -1; gm.onSelectionChanged.Invoke(); BuildBowls(); } }
    void NextLevel()
    {
        StopAnimations();
        if (rewardPanel != null) { Destroy(rewardPanel); rewardPanel = null; }
        if (resultOverlay) resultOverlay.SetActive(false);
        ShowHUDButtons();
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

    #region IAA 提示
    /// <summary>底部工具栏的「提示」按钮（正式功能，常显）：点击高亮一步可解的倒食物操作</summary>
    void BuildHintButton()
    {
        var go = new GameObject("BtnHint", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.62f, 0.02f);
        rt.anchorMax = new Vector2(0.80f, 0.09f);
        rt.sizeDelta = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.95f, 0.6f, 0.1f, 0.9f);
        btnHint = go.AddComponent<Button>();
        var tgo = new GameObject("T", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        var trt = tgo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var t = tgo.AddComponent<SystemFontText>();
        t.text = "💡 提示"; t.fontSize = 24; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter; GameFont.Apply(t);
        btnHint.onClick.AddListener(() => TryUseTool(SaveSystem.ToolType.Hint, ShowHint));
        // 次数角标
        var cgo = new GameObject("Count", typeof(RectTransform));
        cgo.transform.SetParent(go.transform, false);
        var crt = cgo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.55f, 0.04f); crt.anchorMax = new Vector2(0.97f, 0.46f); crt.sizeDelta = Vector2.zero;
        txtHintCount = cgo.AddComponent<SystemFontText>();
        txtHintCount.fontSize = 18; txtHintCount.color = Color.white; txtHintCount.alignment = TextAnchor.MiddleCenter; GameFont.Apply(txtHintCount);
    }

    /// <summary>提示回调：用求解器取一步可解操作，高亮源/目标碗并弹出文字</summary>
    /// <summary>在宠物上方显示"不公平"吐槽气泡（转换到主 Canvas 坐标，避免受 petArea 布局影响）</summary>
    void ShowUnfairBubble(RectTransform targetPetRT)
    {
        if (targetPetRT == null) return;
        // 把宠物在屏幕上的位置换算到本 Canvas 的局部坐标（不依赖 Camera，兼容 ScreenSpaceOverlay）
        var canvasRT = GetComponent<RectTransform>();
        Vector2 localPos = canvasRT.InverseTransformPoint(targetPetRT.position);
        localPos += new Vector2(0, 60);

        var bubble = new GameObject("UnfairBubble", typeof(RectTransform));
        bubble.transform.SetParent(transform, false);
        bubble.transform.SetAsLastSibling();
        var brt = bubble.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = localPos;
        brt.sizeDelta = new Vector2(180, 60);
        var bimg = bubble.AddComponent<Image>();
        bimg.color = new Color(1f, 0.95f, 0.8f, 0.95f);
        var outline = bubble.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.2f, 0.15f); outline.effectDistance = new Vector2(2, 2);

        var btgo = new GameObject("T", typeof(RectTransform));
        btgo.transform.SetParent(bubble.transform, false);
        var btrt = btgo.GetComponent<RectTransform>();
        btrt.anchorMin = Vector2.zero; btrt.anchorMax = Vector2.one; btrt.sizeDelta = Vector2.zero;
        var bt = btgo.AddComponent<SystemFontText>();
        bt.text = "不公平!"; bt.fontSize = 24; bt.color = new Color(0.85f, 0.2f, 0.15f);
        bt.alignment = TextAnchor.MiddleCenter; GameFont.Apply(bt);
        StartCoroutine(DestroyAfter(bubble, 1.6f));
    }

    void ShowHint()
    {
        // 先清掉上一轮高亮（HL 子物体挂在碗 GO 上，不归属 hintRoot），避免反复点击叠加
        ClearHighlights();
        if (hintRoot != null) { Destroy(hintRoot); hintRoot = null; }
        hintRoot = new GameObject("HintRoot", typeof(RectTransform));
        hintRoot.transform.SetParent(transform, false);
        var hrt = hintRoot.GetComponent<RectTransform>();
        hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one; hrt.sizeDelta = Vector2.zero;

        var step = gm.Hint();
        if (step == null)
        {
            ShowHintText("暂时没可提示的步骤（可能已快通关，或已是死局）");
            StartCoroutine(ClearHintAfter(2.0f));
            return;
        }

        HighlightBowl(step.Value.fromId, new Color(0.2f, 0.9f, 0.3f)); // 源：绿
        HighlightBowl(step.Value.toId, new Color(1f, 0.85f, 0.1f));    // 目标：黄
        ShowHintText($"把碗{step.Value.fromId}的 {step.Value.count} 个 {FoodCN(step.Value.food)} → 碗{step.Value.toId}");
        StartCoroutine(ClearHintAfter(2.5f));
    }

    #region IAA 道具次数 + 广告补充 + 死局救援
    /// <summary>给 HUD 上的工具按钮挂一个次数角标（只建一次）</summary>
    Text EnsureCountBadge(Button btn, Text existing)
    {
        if (btn == null) return existing;
        if (existing != null) return existing;
        var go = new GameObject("Count", typeof(RectTransform));
        go.transform.SetParent(btn.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.55f, 0.04f); rt.anchorMax = new Vector2(0.97f, 0.46f); rt.sizeDelta = Vector2.zero;
        var tx = go.AddComponent<SystemFontText>();
        tx.fontSize = 18; tx.color = Color.white; tx.alignment = TextAnchor.MiddleCenter; GameFont.Apply(tx);
        return tx;
    }

    void InitItemCountBadges()
    {
        txtUndoCount = EnsureCountBadge(btnUndo, txtUndoCount);
        txtAddBowlCount = EnsureCountBadge(btnAddBowl, txtAddBowlCount);
        txtShuffleCount = EnsureCountBadge(btnShuffle, txtShuffleCount);
        UpdateItemCounts();
    }

    void UpdateItemCounts()
    {
        if (txtUndoCount) txtUndoCount.text = SaveSystem.GetTool(SaveSystem.ToolType.Undo).ToString();
        if (txtAddBowlCount) txtAddBowlCount.text = SaveSystem.GetTool(SaveSystem.ToolType.AddBowl).ToString();
        if (txtShuffleCount) txtShuffleCount.text = SaveSystem.GetTool(SaveSystem.ToolType.Shuffle).ToString();
        if (txtHintCount) txtHintCount.text = SaveSystem.GetTool(SaveSystem.ToolType.Hint).ToString();
    }

    /// <summary>使用道具：有次数则消耗并执行 action；无次数则弹"看广告补充"</summary>
    void TryUseTool(SaveSystem.ToolType t, System.Action action)
    {
        if (SaveSystem.GetTool(t) > 0)
        {
            SaveSystem.ConsumeTool(t);
            action();
            UpdateItemCounts();
        }
        else
        {
            ShowToolAdSupply(t);
        }
    }

    int GrantAmount(SaveSystem.ToolType t) => t switch
    {
        SaveSystem.ToolType.Undo => SaveSystem.AdGrantUndo,
        SaveSystem.ToolType.AddBowl => SaveSystem.AdGrantAddBowl,
        SaveSystem.ToolType.Shuffle => SaveSystem.AdGrantShuffle,
        SaveSystem.ToolType.Hint => SaveSystem.AdGrantHint,
        _ => 1
    };

    /// <summary>道具用尽 → 看广告补充弹窗</summary>
    void ShowToolAdSupply(SaveSystem.ToolType t)
    {
        CloseDeadlock();
        deadlockPanel = MakePanel(out var box);
        AddPanelTitle(box, "道具用尽啦~");
        float y = 0.5f;
        MakeBtn(box.transform, 0.1f, 0.8f, y, 0.13f, $"📺 看广告补充 ×{GrantAmount(t)}", new Color(0.95f, 0.6f, 0.1f), () =>
        {
            AdManager.Instance.ShowRewardedAd(success =>
            {
                if (!success) return;
                SaveSystem.GrantTool(t, GrantAmount(t));
                UpdateItemCounts();
                CloseDeadlock();
                Toast($"已补充 ×{GrantAmount(t)}，再点一次道具即可使用");
            });
        });
        y -= 0.17f;
        MakeBtn(box.transform, 0.1f, 0.8f, y, 0.13f, "稍后再说", new Color(0.5f, 0.5f, 0.5f), CloseDeadlock);
    }

    /// <summary>死局救援弹窗：看广告+1碗 / 花金币+1碗 / 放弃重来</summary>
    void ShowDeadlockRescue()
    {
        CloseDeadlock();
        deadlockPanel = MakePanel(out var box);
        AddPanelTitle(box, "😿 当前无解了!");
        float y = 0.5f;
        MakeBtn(box.transform, 0.1f, 0.8f, y, 0.13f, "📺 看广告 +1 碗", new Color(0.95f, 0.6f, 0.1f), () =>
        {
            AdManager.Instance.ShowRewardedAd(success =>
            {
                if (!success) return;
                gm.AddBowl(); BuildBowls(); CloseDeadlock(); gm.fsm?.ChangeState<IdleState>();
            });
        });
        y -= 0.17f;
        MakeBtn(box.transform, 0.1f, 0.8f, y, 0.13f, $"🪙 花{SaveSystem.DeadlockRescueGoldCost}金币 +1碗", new Color(0.3f, 0.5f, 0.8f), () =>
        {
            if (SaveSystem.SpendGold(SaveSystem.DeadlockRescueGoldCost))
            {
                gm.AddBowl(); BuildBowls(); CloseDeadlock(); gm.fsm?.ChangeState<IdleState>();
            }
            else { Toast("金币不足，试试看广告~"); }
        });
        y -= 0.17f;
        MakeBtn(box.transform, 0.1f, 0.8f, y, 0.13f, "🔄 放弃重来", new Color(0.6f, 0.3f, 0.3f), () => { CloseDeadlock(); Restart(); });
    }

    /// <summary>半透明遮罩 + 居中盒子，返回 panel（box 通过 out 返回）</summary>
    GameObject MakePanel(out GameObject box)
    {
        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.sizeDelta = Vector2.zero;
        var bg = panel.AddComponent<Image>(); bg.color = new Color(0f, 0f, 0f, 0.6f);
        box = new GameObject("Box", typeof(RectTransform));
        box.transform.SetParent(panel.transform, false);
        var brt = box.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.15f, 0.33f); brt.anchorMax = new Vector2(0.85f, 0.72f); brt.sizeDelta = Vector2.zero;
        var bimg = box.AddComponent<Image>(); bimg.color = new Color(0.2f, 0.14f, 0.1f, 0.98f);
        return panel;
    }

    void AddPanelTitle(GameObject box, string title)
    {
        var tt = new GameObject("Title", typeof(RectTransform));
        tt.transform.SetParent(box.transform, false);
        var trt = tt.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.08f, 0.78f); trt.anchorMax = new Vector2(0.92f, 0.98f); trt.sizeDelta = Vector2.zero;
        var t = tt.AddComponent<SystemFontText>();
        t.text = title; t.fontSize = 26; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter; GameFont.Apply(t);
    }

    void CloseDeadlock()
    {
        if (deadlockPanel != null) { Destroy(deadlockPanel); deadlockPanel = null; }
    }

    /// <summary>短暂提示文字</summary>
    void Toast(string msg)
    {
        var go = new GameObject("Toast", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.08f); rt.anchorMax = new Vector2(0.8f, 0.16f); rt.sizeDelta = Vector2.zero;
        var img = go.AddComponent<Image>(); img.color = new Color(0f, 0f, 0f, 0.72f);
        var t = go.AddComponent<SystemFontText>(); t.text = msg; t.fontSize = 20; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter; GameFont.Apply(t);
        StartCoroutine(DestroyAfter(go, 1.6f));
    }
    #endregion

    // [已移除] 开发期自动演示 EditorAutoDemo：避免“未操作就强制弹出不公平气泡”误导验收。
    // 验证提示/不公平：在编辑器手动点「提示」按钮，或真实喂食一个非首位宠物即可触发。

    /// <summary>在指定碗上加一个高亮边框 + 放大，提示结束后随 hintRoot 一起销毁</summary>
    void HighlightBowl(int bowlId, Color c)
    {
        if (!bowlIdToGO.TryGetValue(bowlId, out var go) || go == null) return;
        var hl = new GameObject("HL", typeof(RectTransform));
        hl.transform.SetParent(go.transform, false);
        var hlr = hl.GetComponent<RectTransform>();
        hlr.anchorMin = new Vector2(-0.15f, -0.15f);
        hlr.anchorMax = new Vector2(1.15f, 1.15f);
        hlr.sizeDelta = Vector2.zero;
        var img = hl.AddComponent<Image>();
        img.color = new Color(c.r, c.g, c.b, 0.18f);
        img.raycastTarget = false;
        var outline = hl.AddComponent<Outline>();
        outline.effectColor = c; outline.effectDistance = new Vector2(5, 5);
        go.transform.localScale = Vector3.one * 1.18f;
    }

    /// <summary>清掉所有碗上残留的高亮（HL 子物体）并复位缩放，避免反复点提示叠加</summary>
    void ClearHighlights()
    {
        foreach (var kv in bowlIdToGO)
        {
            if (kv.Value == null) continue;
            var go = kv.Value;
            var hls = new List<GameObject>();
            foreach (Transform t in go.transform)
                if (t.name == "HL") hls.Add(t.gameObject);
            foreach (var h in hls) Destroy(h);
            go.transform.localScale = Vector3.one;
        }
    }

    /// <summary>画布顶部居中显示提示文字（背景父节点 + 文字子节点，避免同一 GO 上两个 Graphic）</summary>
    void ShowHintText(string msg)
    {
        var go = new GameObject("HintText", typeof(RectTransform));
        go.transform.SetParent(hintRoot.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.82f); rt.anchorMax = new Vector2(0.9f, 0.88f); rt.sizeDelta = Vector2.zero;
        // 强制渲染在最前，避免被 HUD 覆盖
        var topCanvas = go.AddComponent<Canvas>();
        topCanvas.overrideSorting = true;
        topCanvas.sortingOrder = 100;
        go.AddComponent<GraphicRaycaster>();
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

        var tgo = new GameObject("T", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        var trt = tgo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var t = tgo.AddComponent<SystemFontText>();
        t.text = msg; t.fontSize = 26; t.color = new Color(1f, 0.95f, 0.2f);
        t.alignment = TextAnchor.MiddleCenter; GameFont.Apply(t);
    }

    IEnumerator ClearHintAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        ClearHighlights();
        if (hintRoot != null) { Destroy(hintRoot); hintRoot = null; }
    }

    IEnumerator DestroyAfter(GameObject go, float sec) { yield return new WaitForSeconds(sec); if (go != null) Destroy(go); }
    #endregion

    #region GM 调试 — 通关步骤
    /// <summary>右上角 GM 按钮：点击后弹出本局"不用道具"的通关步骤</summary>
    void BuildGMButton()
    {
        var go = new GameObject("BtnGM", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.86f, 0.02f);
        rt.anchorMax = new Vector2(0.98f, 0.09f);
        rt.sizeDelta = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.9f, 0.2f, 0.2f, 0.85f);
        btnGM = go.AddComponent<Button>();
        var tgo = new GameObject("T", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        var trt = tgo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var t = tgo.AddComponent<SystemFontText>();
        t.text = "GM"; t.fontSize = 28; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter; GameFont.Apply(t);
        btnGM.onClick.AddListener(ShowGMSolution);
    }

    /// <summary>弹出 GM 通关步骤面板（BFS 求解，不修改运行时状态）</summary>
    void ShowGMSolution()
    {
        if (gmPanel != null) { Destroy(gmPanel); gmPanel = null; }
        gmPanel = new GameObject("GMPanel", typeof(RectTransform));
        gmPanel.transform.SetParent(transform, false);
        var prt = gmPanel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.sizeDelta = Vector2.zero;
        var bg = gmPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

        // 关闭按钮
        MakeBtn(gmPanel.transform, 0.82f, 0.15f, 0.9f, 0.07f, "✕ 关闭", new Color(0.6f, 0.2f, 0.2f),
            () => { Destroy(gmPanel); gmPanel = null; });

        // 标题
        {
            var titleGO = new GameObject("GMTitle", typeof(RectTransform));
            titleGO.transform.SetParent(gmPanel.transform, false);
            var trt2 = titleGO.GetComponent<RectTransform>();
            trt2.anchorMin = new Vector2(0.05f, 0.83f); trt2.anchorMax = new Vector2(0.8f, 0.91f); trt2.sizeDelta = Vector2.zero;
            var tt = titleGO.AddComponent<SystemFontText>();
            tt.text = "🛠 GM · 通关步骤（无需道具）"; tt.fontSize = 24; tt.color = new Color(1f, 0.84f, 0.4f);
            tt.alignment = TextAnchor.MiddleLeft; GameFont.Apply(tt);
        }

        // 滚动容器
        var scrollGO = new GameObject("GMScroll", typeof(RectTransform));
        scrollGO.transform.SetParent(gmPanel.transform, false);
        var srt = scrollGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.03f, 0.05f); srt.anchorMax = new Vector2(0.97f, 0.8f); srt.sizeDelta = Vector2.zero;
        var scroll = scrollGO.AddComponent<ScrollRect>();
        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(scrollGO.transform, false);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);

        // 计算解
        int req = gm.GetCurrentLevel()?.petQueue?.Length ?? 0;
        var sol = PetGameSolver.Solve(gm.pour, req);

        var sb = new StringBuilder();
        if (sol == null || sol.Count == 0)
        {
            sb.AppendLine("⚠ 当前局面未找到通关步骤（可能超出搜索深度，或已是死局）。");
            sb.AppendLine("提示：可尝试撤销 / 加空碗 / 洗牌后重试，或点「重开」。");
        }
        else
        {
            sb.AppendLine($"共 {sol.Count} 步，全程不使用任何道具即可通关：\n");
            int idx = 1;
            foreach (var s in sol)
            {
                string foodCN = FoodCN(s.food);
                sb.AppendLine($"{idx}. 碗{s.fromId} → 碗{s.toId}：倒 {s.count} 个 {foodCN}");
                idx++;
            }
            sb.AppendLine($"\n目标：完成 {req} 个满碗（各喂一种宠物）。");
        }

        var txtGO = new GameObject("GMText", typeof(RectTransform));
        txtGO.transform.SetParent(content.transform, false);
        // 关键：用拉伸锚点铺满 content，否则默认居中锚点 + sizeDelta.x=0 会让文字零宽不可见
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one; txtRT.sizeDelta = Vector2.zero;
        var txt = txtGO.AddComponent<SystemFontText>();
        txt.text = sb.ToString(); txt.fontSize = 20; txt.color = Color.white;
        txt.alignment = TextAnchor.UpperLeft; GameFont.Apply(txt);

        int lines = sb.ToString().Split('\n').Length;
        float h = Mathf.Max(120, lines * 26f);
        crt.sizeDelta = new Vector2(0, h);
        txtGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0, h);

        scroll.content = content.GetComponent<RectTransform>();
        scroll.vertical = true; scroll.horizontal = false;
    }

    /// <summary>食物→中文（复用食物→宠物的关系，展示为对应宠物粮）</summary>
    string FoodCN(FoodType f) => PetCN(FoodPetMap.GetPet(f)) + "粮";
    #endregion

    T FindC<T>(GameObject root, string n) where T : Component { foreach (var c in root.GetComponentsInChildren<T>(true)) if (c.name == n) return c; return null; }
    Text FindT(string n) => FindC<Text>(gameHUD, n);
    Button FindB(string n) => FindC<Button>(gameHUD, n);
    GameObject FindGO(GameObject root, string n) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t.gameObject; return null; }
    GameObject FindGO(string n) => FindGO(gameHUD, n);
    void Clear(Transform t) { foreach (Transform c in t) Destroy(c.gameObject); }
    string PetCN(PetType p) => p switch { PetType.Cat => "橘猫", PetType.Dog => "柴犬", PetType.Hamster => "仓鼠", PetType.Parrot => "鹦鹉", PetType.Fish => "金鱼", PetType.Rabbit => "垂耳兔", _ => p.ToString() };
}
