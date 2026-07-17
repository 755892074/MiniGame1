using System.Collections;
using UnityEngine;

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

    IEnumerator Start()
    {
        Debug.Log("[Bootstrap] 启动流程开始");

        // 1. 加载存档
        SaveSystem.Load();
        Debug.Log($"[Bootstrap] 存档加载完成: 关卡{SaveSystem.Data.currentLevelId} / 已解锁{SaveSystem.Data.highestUnlockedLevel}");

        // 2. 检测SDK环境
        bool isDouyin = CloudSaveBridge.IsAvailable;
        Debug.Log($"[Bootstrap] 抖音环境: {isDouyin}");

        // 3. 加载 Splash 预制体
        var splashPf = Resources.Load<GameObject>("PrefabsV2/SplashPanel");
        if (splashPf != null)
        {
            // 需要 Canvas
            var canvas = EnsureCanvas();
            var splashGO = Object.Instantiate(splashPf, canvas.transform);
            splashGO.name = "SplashPanel";

            // 找进度条
            var barFill = FindChildRecursive(splashGO.transform, "imgBarFill");
            if (barFill != null) imgBarFill = barFill as RectTransform;
        }

        // 4. 等待最小展示时间（模拟加载）
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

        // 5. 跳转到主菜单
        Debug.Log("[Bootstrap] → MenuScene");
        GameSceneManager.LoadMenu();
    }

    Canvas EnsureCanvas()
    {
        var existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null) return existingCanvas;

        var go = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.GetComponent<UnityEngine.UI.CanvasScaler>();
        sc.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(750, 1334);
        sc.matchWidthOrHeight = 1f;
        return c;
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
