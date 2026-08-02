using UnityEngine;
using UnityEngine.UI;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 主菜单面板控制器
/// 绑定按钮：继续游戏/选关/设置/小院/成就
/// 显示主页 Hero 宠物（当前主角宠物 + 阶段对应表情）
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private MenuSceneController menuController;

    public void Init(MenuSceneController controller)
    {
        menuController = controller;
        UpdatePlayerInfo();
        UpdateHeroArt();
        BindButtons();
    }

    void UpdatePlayerInfo()
    {
        var txtTitle = Find<Text>("txtTitle");
        var txtFish = Find<Text>("txtFish");
        var txtStars = Find<Text>("txtStars");

        if (txtTitle != null)
            txtTitle.text = SaveSystem.GetCurrentTitle();
        if (txtFish != null)
            txtFish.text = SaveSystem.Data.fishDiscount.ToString();
        if (txtStars != null)
            txtStars.text = SaveSystem.TotalStars.ToString();
    }

    /// <summary>
    /// 主页 Hero 区宠物展示：取 SaveSystem 第一个已救助宠物（无则默认 Cat 中性表情），
    /// 阶段 1=脏兮兮→disgust / 2=干净→neutral / 3=幸福→happy，加载到 imgHeroArt。
    /// </summary>
    void UpdateHeroArt()
    {
        var img = Find<Image>("imgHeroArt");
        if (img == null) return;

        PetType activePet = PetType.Cat;
        int stage = 2;
        foreach (var p in SaveSystem.Data.pets)
        {
            if (p.unlocked) { activePet = p.petType; stage = p.stage; break; }
        }

        string key = activePet.ToString().ToLower();
        string expr = stage == 1 ? "disgust" : (stage == 3 ? "happy" : "neutral");
        string path = $"Assets/Art/PetGame/pets/{key}/{expr}.png";

        var handle = ResLoader.LoadSprite(path);
        handle.Completed += h =>
        {
            if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null)
            {
                img.sprite = h.Result;
                img.color = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                Debug.LogWarning($"[MainMenu] 加载 Hero 宠物 sprite 失败: {path} (status={h.Status})");
            }
        };
    }

    void BindButtons()
    {
        // 继续游戏
        var btnContinue = Find<Button>("btnContinue");
        if (btnContinue != null)
            btnContinue.onClick.AddListener(() => menuController.EnterGame(-1));

        // 选关
        var btnLevelSelect = Find<Button>("btnLevelSelect");
        if (btnLevelSelect != null)
            btnLevelSelect.onClick.AddListener(() => menuController.ShowLevelSelect());

        // 设置
        var btnSettings = Find<Button>("btnSettings");
        if (btnSettings != null)
            btnSettings.onClick.AddListener(() => menuController.ShowSettings());

        // 快捷设置
        var btnQuick = Find<Button>("btnQuickSettings");
        if (btnQuick != null)
            btnQuick.onClick.AddListener(() => menuController.ShowSettings());

        // 小院（P2 已接入）
        var btnYard = Find<Button>("btnYard");
        if (btnYard != null)
            btnYard.onClick.AddListener(() => menuController.ShowYard());

        // 成就
        var btnAch = Find<Button>("btnAchievement");
        if (btnAch != null)
            btnAch.onClick.AddListener(() => { AchievementSystem.CheckAll(); AchievementUI.Show(); });
    }

    T Find<T>(string name) where T : Component
    {
        foreach (var c in GetComponentsInChildren<T>(true))
            if (c.name == name || c.gameObject.name == name) return c;
        return null;
    }
}
