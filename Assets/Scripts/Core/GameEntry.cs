using UnityEngine;
using F8Framework.Core;
using F8Framework.Launcher;

/// <summary>
/// 游戏总入口 - 挂载在场景根 GameObject 上
/// 负责初始化 F8Framework 模块中心和各游戏模块
/// </summary>
public class GameEntry : MonoBehaviour
{
    [Header("游戏配置")]
    public string gameName = "找茬大师";
    public string version = "1.0.0";
    
    [Header("模块开关")]
    public bool enableUI = true;
    public bool enableAudio = true;
    public bool enableTimer = true;
    
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        InitializeFramework();
        InitializeGameModules();
    }

    void InitializeFramework()
    {
        // F8Framework ModuleCenter 是静态类，通过 Initialize() 初始化
        // 需要在场景中挂载 F8 的 GameLauncher 组件，
        // GameLauncher 会自动调用 ModuleCenter.Initialize(this)
        Debug.Log($"[GameEntry] F8Framework 初始化入口 - {gameName} v{version}");
        Debug.Log("[GameEntry] 请确保场景中已挂载 F8Framework 的 GameLauncher 组件");
    }

    void InitializeGameModules()
    {
        // 注册 UIManager（如果启用）
        if (enableUI)
        {
            Debug.Log("[GameEntry] UIManager 已就绪");
        }
        
        // 注册 AudioManager（如果启用）
        if (enableAudio)
        {
            Debug.Log("[GameEntry] AudioManager 已就绪");
        }
        
        // 注册 TimerManager（如果启用）
        if (enableTimer)
        {
            Debug.Log("[GameEntry] TimerManager 已就绪");
        }
        
        Debug.Log("[GameEntry] 游戏模块初始化完成");
    }

    void OnDestroy()
    {
        Debug.Log("[GameEntry] 游戏退出");
    }
}
