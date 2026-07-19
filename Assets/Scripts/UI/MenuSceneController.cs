using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// MenuScene 入口 — 管理主菜单场景中所有面板的加载和切换
/// 流程：判断首次 → LoginPanel 或 MainMenuPanel
/// </summary>
public class MenuSceneController : MonoBehaviour
{
    private Canvas canvas;
    private GameObject currentPanel;

    void Start()
    {
        EnsureCamera();
        EnsureEventSystem();
        canvas = EnsureCanvas();
        ShowInitialPanel();
    }

    Canvas EnsureCanvas()
    {
        var existing = FindObjectOfType<Canvas>();
        if (existing != null) return existing;

        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(750, 1334);
        sc.matchWidthOrHeight = 1f;
        return c;
    }

    // ========================================
    // 必要的场景对象（运行时补齐，避免场景本身缺少这些导致 UI 不可见/不可交互）
    // ========================================

    /// <summary>确保场景有相机，消除 Game 视图的 "no cameras rendering" 提示</summary>
    Camera EnsureCamera()
    {
        var existing = FindObjectOfType<Camera>();
        if (existing != null) return existing;

        var go = new GameObject("Main Camera", typeof(Camera));
        go.tag = "MainCamera";
        var cam = go.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Depth;
        cam.cullingMask = 0;       // 不渲染世界对象；UI 由 overlay Canvas 渲染，相机仅用于消除 no-cameras 提示
        cam.depth = -1;
        return cam;
    }

    /// <summary>确保有 EventSystem + 输入模块，否则所有 UI 按钮点击无响应</summary>
    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
        go.AddComponent<StandaloneInputModule>();
    }

    void ShowInitialPanel()
    {
        // 首次游玩 → 登录面板
        // 已同意隐私 → 直接主菜单
        if (!SaveSystem.Data.privacyAgreed)
        {
            ShowLoginPanel(true);
        }
        else
        {
            ShowMainMenu();
        }
    }

    // ========================================
    // 面板切换
    // ========================================

    public void SwitchTo(GameObject newPanel)
    {
        if (currentPanel != null) Destroy(currentPanel);
        currentPanel = newPanel;
    }

    // ========================================
    // 登录面板
    // ========================================

    /// <summary>显示登录面板</summary>
    /// <param name="firstTime">是否首次（影响隐私弹窗）</param>
    public void ShowLoginPanel(bool firstTime = false)
    {
        var pf = Resources.Load<GameObject>("PrefabsV2/LoginPanel");
        if (pf == null)
        {
            Debug.LogError("[MenuScene] LoginPanel 预制体未找到! 请先执行 Tools → 生成新界面预制体(v3)");
            return;
        }
        var go = Instantiate(pf, canvas.transform);
        GameFont.ApplyAll(go);
        go.name = "LoginPanel";
        SwitchTo(go);

        // 绑定 LoginController
        var ctrl = go.AddComponent<LoginController>();
        ctrl.Init(this, firstTime);
    }

    // ========================================
    // 主菜单
    // ========================================

    public void ShowMainMenu()
    {
        var pf = Resources.Load<GameObject>("PrefabsV2/MainMenuPanel");
        if (pf == null)
        {
            Debug.LogError("[MenuScene] MainMenuPanel 预制体未找到!");
            return;
        }
        var go = Instantiate(pf, canvas.transform);
        GameFont.ApplyAll(go);
        go.name = "MainMenuPanel";
        SwitchTo(go);

        var ctrl = go.AddComponent<MainMenuController>();
        ctrl.Init(this);
    }

    // ========================================
    // 设置面板（弹窗叠加，不替换当前面板）
    // ========================================

    GameObject settingsPanel;

    // ========================================
    // 小院面板（弹窗叠加，不替换当前面板）— P2
    // ========================================

    GameObject yardPanel;

    /// <summary>显示小院面板（建筑+宠物只读一览）</summary>
    public void ShowYard()
    {
        if (yardPanel != null) return;  // 已打开

        var go = new GameObject("YardPanel", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(canvas.transform, false);
        yardPanel = go;

        var ctrl = go.AddComponent<YardPanelController>();
        ctrl.Init(this);
    }

    public void CloseYard()
    {
        if (yardPanel != null)
        {
            Destroy(yardPanel);
            yardPanel = null;
        }
    }

    // ========================================
    // 小院子弹窗（宠物详情 / 建筑升级）— P3 / P4
    // 同一时间仅一个弹窗，叠加在小院面板之上
    // ========================================

    GameObject currentPopup;

    public void ShowPetDetail(PetType type)
    {
        ClosePopup();
        var go = new GameObject("PetDetailPopup", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(canvas.transform, false);
        currentPopup = go;
        go.AddComponent<PetDetailPopupController>().Init(this, type);
    }

    public void ShowBuilding(string buildingId)
    {
        ClosePopup();
        var go = new GameObject("BuildingPopup", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(canvas.transform, false);
        currentPopup = go;
        go.AddComponent<BuildingPopupController>().Init(this, buildingId);
    }

    public void ClosePopup()
    {
        if (currentPopup != null)
        {
            Destroy(currentPopup);
            currentPopup = null;
        }
    }

    public void ShowSettings()
    {
        if (settingsPanel != null) return;  // 已打开

        var pf = Resources.Load<GameObject>("PrefabsV2/SettingsPanel");
        if (pf == null)
        {
            Debug.LogError("[MenuScene] SettingsPanel 预制体未找到!");
            return;
        }
        settingsPanel = Instantiate(pf, canvas.transform);
        GameFont.ApplyAll(settingsPanel);
        settingsPanel.name = "SettingsPanel";

        var ctrl = settingsPanel.AddComponent<SettingsController>();
        ctrl.Init(this);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            Destroy(settingsPanel);
            settingsPanel = null;
        }
    }

    // ========================================
    // 选关面板
    // ========================================

    public void ShowLevelSelect()
    {
        var pf = Resources.Load<GameObject>("PrefabsV2/LevelSelectPanel");
        if (pf == null)
        {
            Debug.LogError("[MenuScene] LevelSelectPanel 预制体未找到!");
            return;
        }
        var go = Instantiate(pf, canvas.transform);
        GameFont.ApplyAll(go);
        go.name = "LevelSelectPanel";
        SwitchTo(go);

        var ctrl = go.AddComponent<LevelSelectController>();
        ctrl.Init(this);
    }

    // ========================================
    // 进入游戏
    // ========================================

    public void EnterGame(int levelId = -1)
    {
        if (levelId < 0)
            levelId = SaveSystem.Data.currentLevelId;

        // 保存当前选择
        SaveSystem.Data.currentLevelId = levelId;
        SaveSystem.Save();

        // 加载游戏场景
        GameSceneManager.LoadGame(levelId);
    }
}
