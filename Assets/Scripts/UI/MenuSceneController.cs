using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// MenuScene 入口 — 管理主菜单场景中所有面板的加载和切换
/// 流程：判断首次 → LoginPanel 或 MainMenuPanel
/// </summary>
public class MenuSceneController : MonoBehaviour
{
    private Canvas canvas;
    private GameObject currentPanel;
    private bool _started = false;

    void Start()
    {
        if (_started) { Debug.Log("[MenuScene] Start() 已执行过，跳过重复初始化"); return; }
        _started = true;

        Debug.Log($"[MenuScene] Start() privacyAgreed={SaveSystem.Data.privacyAgreed} 当前场景={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        EnsureCamera();
        EnsureEventSystem();
        DisableTTSDKMockUI();
        canvas = EnsureCanvas();
        Debug.Log("[MenuScene] Canvas 就绪, 准备显示初始面板");
        ShowInitialPanel();
    }

    /// <summary>
    /// 关闭 TTSDK 自带的 MockUI（开发者调试 OnGUI 渲染，发布时不应出现在 UI 上）。
    /// MockUI 组件在 ttsdk.dll 中无法直接编辑，只能运行时禁用。
    /// </summary>
    void DisableTTSDKMockUI()
    {
        foreach (var m in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (m == null) continue;
            var tn = m.GetType().FullName;
            if (tn == "TTSDK.MockUIUtil" || tn == "TTSDK.TTSDKLog")
            {
                m.enabled = false;
                m.gameObject.SetActive(false);
            }
        }
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
        Debug.Log("[MenuScene] 新建 Canvas 完成");
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
            Debug.Log("[MenuScene] 显示 LoginPanel (首次)");
            ShowLoginPanel(true);
        }
        else
        {
            Debug.Log("[MenuScene] 显示 MainMenu (已同意隐私)");
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

    /// <summary>
    /// 带超时兜底的 prefab 加载（与 Bootstrap 一致）。
    /// 抖音小游戏环境若 LoadAssetAsync 回调迟迟不触发，6s 后强制报错，避免永久白屏。
    /// </summary>
    void LoadPanelWithTimeout(string key, string label, System.Action<GameObject> onReady)
    {
        Debug.Log($"[MenuScene] LoadPrefab 开始: {label} ({key})");
        var handle = ResLoader.LoadPrefab(key);
        bool done = false;

        handle.Completed += h =>
        {
            if (done) return;
            done = true;
            if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null)
            {
                Debug.Log($"[MenuScene] LoadPrefab 成功: {label}");
                onReady?.Invoke(h.Result);
            }
            else
            {
                Debug.LogError($"[MenuScene] {label} 预制体加载失败! status={h.Status} result={(h.Result == null ? "null" : h.Result.name)}");
            }
        };

        StartCoroutine(LoadTimeout(label, () => {
            if (done) return;
            done = true;
            Debug.LogError($"[MenuScene] LoadPrefab 超时(6s)未完成: {label} —— 可能 Addressables 在该环境挂起");
        }));
    }

    IEnumerator LoadTimeout(string label, System.Action onTimeout)
    {
        yield return new WaitForSeconds(6f);
        onTimeout?.Invoke();
    }

    // ========================================
    // 登录面板
    // ========================================

    /// <summary>显示登录面板</summary>
    /// <param name="firstTime">是否首次（影响隐私弹窗）</param>
    public void ShowLoginPanel(bool firstTime = false)
    {
        LoadPanelWithTimeout("Assets/Prefabs/UI/PrefabsV2/LoginPanel.prefab", "LoginPanel", go =>
        {
            var inst = Instantiate(go, canvas.transform);
            GameFont.ApplyAll(inst);
            inst.name = "LoginPanel";
            SwitchTo(inst);
            Debug.Log("[MenuScene] LoginPanel 已实例化并挂载到 Canvas");

            var ctrl = inst.AddComponent<LoginController>();
            ctrl.Init(this, firstTime);
        });
    }

    // ========================================
    // 主菜单
    // ========================================

    public void ShowMainMenu()
    {
        LoadPanelWithTimeout("Assets/Prefabs/UI/PrefabsV2/MainMenuPanel.prefab", "MainMenuPanel", go =>
        {
            var inst = Instantiate(go, canvas.transform);
            GameFont.ApplyAll(inst);
            inst.name = "MainMenuPanel";
            SwitchTo(inst);
            Debug.Log("[MenuScene] MainMenuPanel 已实例化并挂载到 Canvas");

            var ctrl = inst.AddComponent<MainMenuController>();
            ctrl.Init(this);
        });
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

        LoadPanelWithTimeout("Assets/Prefabs/UI/PrefabsV2/SettingsPanel.prefab", "SettingsPanel", go =>
        {
            settingsPanel = Instantiate(go, canvas.transform);
            GameFont.ApplyAll(settingsPanel);
            settingsPanel.name = "SettingsPanel";
            Debug.Log("[MenuScene] SettingsPanel 已实例化");

            var ctrl = settingsPanel.AddComponent<SettingsController>();
            ctrl.Init(this);
        });
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
        LoadPanelWithTimeout("Assets/Prefabs/UI/PrefabsV2/LevelSelectPanel.prefab", "LevelSelectPanel", go =>
        {
            var inst = Instantiate(go, canvas.transform);
            GameFont.ApplyAll(inst);
            inst.name = "LevelSelectPanel";
            SwitchTo(inst);
            Debug.Log("[MenuScene] LevelSelectPanel 已实例化");

            var ctrl = inst.AddComponent<LevelSelectController>();
            ctrl.Init(this);
        });
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
