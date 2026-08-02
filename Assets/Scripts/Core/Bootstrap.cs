using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// BootScene 入口脚本 — App 启动的第一个场景
/// 职责：读存档 → 检测SDK → 展示Splash → 跳转到 MenuScene
/// </summary>
public class Bootstrap : MonoBehaviour
{
    [Header("配置")]
    public float minSplashTime = 1.5f;  // 最小展示时间

    [Header("引用")]
    public RectTransform imgBarFill;    // 进度条填充，拖拽赋值

    void Awake()
    {
        // 确保只有一个 GameEntry 实例
        var entry = FindObjectOfType<GameEntry>();
        if (entry == null)
        {
            var go = new GameObject("[GameEntry]");
            go.AddComponent<GameEntry>();
        }
    }

    void Start()
    {
        Debug.Log("[Bootstrap] 启动流程开始");

        // 1. 加载存档
        SaveSystem.Load();
        Debug.Log($"[Bootstrap] 存档加载完成: 关卡{SaveSystem.Data.currentLevelId} / 已解锁{SaveSystem.Data.highestUnlockedLevel}");

        // 1.5 成就系统：启动期回溯判定（silent 不弹 toast）+ 订阅解锁提示
        AchievementSystem.EnsureToastWired();
        AchievementSystem.CheckAll(true);

        // 2. 检测SDK环境
        bool isDouyin = CloudSaveBridge.IsAvailable;
        Debug.Log($"[Bootstrap] 抖音环境: {isDouyin}");

        // 3. 异步加载 Splash 预制体（抖音小游戏禁止同步等待）
        var handle = ResLoader.LoadPrefab("Assets/Prefabs/UI/PrefabsV2/SplashPanel.prefab");
        bool callbackFired = false;
        handle.Completed += h =>
        {
            if (callbackFired) return;
            callbackFired = true;
            if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null)
            {
                var canvas = EnsureCanvas();
                var splashGO = Object.Instantiate(h.Result, canvas.transform);
                GameFont.ApplyAll(splashGO);
                splashGO.name = "SplashPanel";

                var barFill = FindChildRecursive(splashGO.transform, "imgBarFill");
                if (barFill != null) imgBarFill = barFill as RectTransform;
            }
            else
            {
                Debug.LogError("[Bootstrap] SplashPanel 加载失败，跳过 Splash 直接进菜单");
            }
            StartCoroutine(WaitAndGoMenu());
        };
        // 超时保护：如果 5 秒内 Addressables 没回调，直接进菜单
        StartCoroutine(TimeoutFallback(5f, () => {
            if (!callbackFired) { callbackFired = true; StartCoroutine(WaitAndGoMenu()); }
        }));
    }

    /// <summary>超时回调保护：seconds 秒后如果还未触发，执行 action</summary>
    IEnumerator TimeoutFallback(float seconds, System.Action action)
    {
        yield return new WaitForSeconds(seconds);
        action?.Invoke();
    }

    IEnumerator WaitAndGoMenu()
    {
        // 等待最小展示时间（模拟加载）
        float elapsed = 0f;
        while (elapsed < minSplashTime)
        {
            elapsed += Time.deltaTime;
            if (imgBarFill != null)
            {
                float progress = Mathf.Clamp01(elapsed / minSplashTime);
                imgBarFill.sizeDelta = new Vector2(280f * progress, 8f);
            }
            yield return null;
        }

        Debug.Log("[Bootstrap] → MenuScene");
        GameSceneManager.LoadMenu();
    }

    Canvas EnsureCanvas()
    {
        var existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas == null)
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = go.GetComponent<UnityEngine.UI.CanvasScaler>();
            sc.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(750, 1334);
            sc.matchWidthOrHeight = 1f;
            existingCanvas = c;
        }
        EnsureCamera();
        return existingCanvas;
    }

    void EnsureCamera()
    {
        if (Camera.main != null) return;
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.96f, 0.94f, 0.90f);
        cam.cullingMask = 0;
        cam.orthographic = true;
    }

    Transform FindChildRecursive(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
