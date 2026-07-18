using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单面板控制器
/// 绑定按钮：继续游戏/选关/设置/小院/成就
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private MenuSceneController menuController;

    public void Init(MenuSceneController controller)
    {
        menuController = controller;
        UpdatePlayerInfo();
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

        // 成就（暂未实现）
        var btnAch = Find<Button>("btnAchievement");
        if (btnAch != null)
            btnAch.onClick.AddListener(() => Debug.Log("[MainMenu] 成就（待实现）"));
    }

    T Find<T>(string name) where T : Component
    {
        foreach (var c in GetComponentsInChildren<T>(true))
            if (c.name == name || c.gameObject.name == name) return c;
        return null;
    }
}
