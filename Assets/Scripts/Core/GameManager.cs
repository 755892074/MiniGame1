using System;
using UnityEngine;

/// <summary>
/// 游戏全局状态管理 - 管理游戏生命周期
/// 负责：游戏状态切换、场景加载、全局数据
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    #region 游戏状态
    public enum GameState
    {
        MainMenu,       // 主菜单
        LevelSelect,    // 关卡选择
        Playing,        // 游戏中
        Paused,         // 暂停
        LevelComplete,  // 关卡完成
        LevelFailed,    // 关卡失败
    }

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    #endregion

    #region 全局数据
    // 玩家进度
    public int currentLevelId { get; private set; } = 1;
    public int highestUnlockedLevel { get; private set; } = 1;
    
    // 关卡内数据
    public int foundDiffCount { get; set; } = 0;
    public int mistakeCount { get; set; } = 0;
    public float elapsedTime { get; set; } = 0f;
    public int earnedStars { get; set; } = 0;
    #endregion

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadProgress();
    }

    #region 状态切换
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        
        var oldState = CurrentState;
        CurrentState = newState;
        
        Debug.Log($"[GameManager] 状态切换: {oldState} → {newState}");
        
        OnStateChanged(oldState, newState);
    }

    void OnStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.MainMenu:
            case GameState.LevelSelect:
                Time.timeScale = 1f;
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                ResetLevelData();
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.LevelComplete:
            case GameState.LevelFailed:
                Time.timeScale = 1f;
                break;
        }
    }
    #endregion

    #region 关卡数据
    void ResetLevelData()
    {
        foundDiffCount = 0;
        mistakeCount = 0;
        elapsedTime = 0f;
        earnedStars = 0;
    }

    public void StartLevel(int levelId)
    {
        currentLevelId = levelId;
        ChangeState(GameState.Playing);
    }

    public void CompleteLevel()
    {
        earnedStars = 3; // 简单默认
        if (currentLevelId >= highestUnlockedLevel)
            highestUnlockedLevel = currentLevelId + 1;
        SaveProgress();
        ChangeState(GameState.LevelComplete);
    }

    public void FailLevel()
    {
        SaveProgress();
        ChangeState(GameState.LevelFailed);
    }
    #endregion

    #region 数据持久化
    void SaveProgress()
    {
        PlayerPrefs.SetInt("HighestUnlockedLevel", highestUnlockedLevel);
        PlayerPrefs.SetInt("CurrentLevel", currentLevelId);
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        highestUnlockedLevel = PlayerPrefs.GetInt("HighestUnlockedLevel", 1);
        currentLevelId = PlayerPrefs.GetInt("CurrentLevel", 1);
    }

    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        highestUnlockedLevel = 1;
        currentLevelId = 1;
    }
    #endregion
}
