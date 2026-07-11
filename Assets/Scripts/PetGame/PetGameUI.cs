using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 铲屎官疯了 v2 — 从预制体加载 UI
/// </summary>
public class PetGameUI : MonoBehaviour
{
    private PetGameManager gm;
    private GameObject gameHUD;
    private GameObject petItemPf, bowlItemPf, foodIconPf;

    private Transform petArea, bowlArea;
    private Text txtLevel, txtScore, txtStep, txtStars, txtResultTitle;
    private GameObject heldFoodHolder, resultOverlay;
    private Button btnUndo, btnAddBowl, btnShuffle, btnRestart, btnNext;

    private List<GameObject> petGOs = new List<GameObject>();
    private List<GameObject> bowlGOs = new List<GameObject>();

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
        heldFoodHolder = FindGO("HeldFoodHolder");
        resultOverlay = FindGO("ResultOverlay");
        btnUndo = FindB("btnUndo");     btnAddBowl = FindB("btnAddBowl");
        btnShuffle = FindB("btnShuffle"); btnRestart = FindB("btnRestart");
        btnNext = FindB("btnNext");
        Debug.Log($"[PetGameUI] petArea:{petArea!=null} bowlArea:{bowlArea!=null} hudOK:{gameHUD!=null}");
    }

    void BindButtons()
    {
        btnUndo?.onClick.AddListener(() => gm.Undo());
        btnAddBowl?.onClick.AddListener(() => gm.AddBowl());
        btnShuffle?.onClick.AddListener(() => { /* TODO */ });
        btnRestart?.onClick.AddListener(Restart);
        btnNext?.onClick.AddListener(Restart);
        gm.onScoreChanged.AddListener(_ => UpdateHUD());
        gm.onPickUp.AddListener(_ => UpdateHeld());
        gm.onPour.AddListener(_ => { BuildBowls(); UpdateHUD(); });
        gm.onBowlCompleted.AddListener(() => BuildBowls());
        gm.onPetFed.AddListener((p, pts, f) => { BuildPets(); BuildBowls(); });
        gm.onHeldChanged.AddListener(UpdateHeld);
        gm.onLevelComplete.AddListener(OnWin);
        gm.onLevelFail.AddListener(OnFail);
    }

    void BuildLevel() { BuildPets(); BuildBowls(); UpdateHUD(); }

    void BuildPets()
    {
        if (petArea == null) return;
        Clear(petArea); petGOs.Clear();

        foreach (var pet in gm.GetPetQueue())
        {
            var go = Instantiate(petItemPf, petArea);
            var label = FindC<Text>(go, "QueueLabel");
            if (label) label.text = PetCN(pet);

            // 切换宠物表情
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
        Clear(bowlArea); bowlGOs.Clear();

        foreach (var bowl in gm.GetBowls())
        {
            var go = Instantiate(bowlItemPf, bowlArea);
            int bid = bowl.bowlId;
            var btn = go.GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => gm.PourToBowl(bid));

            // 选中效果：缩放1.3倍
            if (bid == gm.selectedBowlId)
            {
                go.transform.localScale = Vector3.one * 1.3f;
            }

            var stack = FindGO(go, "FoodStack")?.transform;
            if (stack != null && foodIconPf != null)
            {
                Clear(stack);
                foreach (var f in bowl.foods)
                {
                    var icon = Instantiate(foodIconPf, stack);
                    // 缩小食物图标适配碗内
                    var le = icon.GetComponent<LayoutElement>();
                    if (le != null) { le.preferredWidth = 30; le.preferredHeight = 30; }
                }
            }

            var done = FindGO(go, "DoneMark");
            if (done) done.SetActive(bowl.isCompleted);

            bowlGOs.Add(go);
        }
    }

    void UpdateHUD()
    {
        if (txtLevel) txtLevel.text = $"第{gm.currentLevelId}关";
        if (txtScore) txtScore.text = $"得分:{gm.GetScore()}/{gm.targetScore}";
        if (txtStep) txtStep.text = $"步数:{gm.pour.totalMoves}";
    }

    void UpdateHeld() { if (heldFoodHolder) heldFoodHolder.SetActive(gm.GetHeldFood() != null); }

    void OnWin(int s) { if (resultOverlay) resultOverlay.SetActive(true); if (txtResultTitle) txtResultTitle.text = "通关!"; if (txtStars) txtStars.text = new string('★', s) + new string('☆', 3 - s); }
    void OnFail() { if (resultOverlay) resultOverlay.SetActive(true); if (txtResultTitle) txtResultTitle.text = "失败..."; }
    void Restart() { if (resultOverlay) resultOverlay.SetActive(false); gm.StartLevel(gm.currentLevelId); BuildLevel(); }

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
