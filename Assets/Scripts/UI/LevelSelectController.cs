using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 选关面板控制器
/// 从存档读取关卡解锁状态和星级，动态生成关卡按钮网格
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    private MenuSceneController menuController;

    public void Init(MenuSceneController controller)
    {
        menuController = controller;
        UpdatePlayerInfo();
        BindButtons();
        BuildLevelGrid();
    }

    void UpdatePlayerInfo()
    {
        var txtInfo = Find<Text>("txtPlayerInfo");
        if (txtInfo != null)
        {
            int totalStars = SaveSystem.TotalStars;
            txtInfo.text = $"{SaveSystem.GetCurrentTitle()}  |  小鱼干:{SaveSystem.Data.fishDiscount}  徽章:{SaveSystem.Data.rescueBadge}  |  总星数:{totalStars}";
        }
    }

    void BindButtons()
    {
        var btnBack = Find<Button>("btnBack");
        if (btnBack != null)
            btnBack.onClick.AddListener(() => menuController.ShowMainMenu());
    }

    void BuildLevelGrid()
    {
        // 找到 ScrollView Content
        Transform content = null;
        var scroll = Find<ScrollRect>("LevelScrollView");
        if (scroll != null) content = scroll.content;
        if (content == null)
        {
            Debug.LogError("[LevelSelect] 找不到 ScrollView Content!");
            return;
        }

        // 加载关卡数据获取总数
        var levels = Resources.LoadAll<PetLevelConfigV2>("Levels");
        int levelCount = levels != null ? levels.Length : 50;
        int highest = SaveSystem.Data.highestUnlockedLevel;
#if UNITY_EDITOR
        highest = levelCount; // 开发期：全部解锁，方便测试任意关卡
#endif

        Debug.Log($"[LevelSelect] 生成关卡网格: {levelCount}关, 已解锁{highest}");

        var font = Resources.Load<Font>("Fonts/AlibabaPuHuiTi-Regular");

        for (int i = 0; i < levelCount; i++)
        {
            int lid = i + 1;
            bool unlocked = lid <= highest;
            int stars = SaveSystem.GetLevelStars(lid);

            // 关卡按钮
            var btnGO = new GameObject($"Btn{lid}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(content, false);

            var img = btnGO.GetComponent<Image>();
            // 颜色：已通关绿 / 可玩蓝灰 / 锁定灰
            img.color = stars > 0 ? new Color(0.18f, 0.45f, 0.18f)
                       : unlocked ? new Color(0.19f, 0.22f, 0.31f)
                       : new Color(0.25f, 0.25f, 0.25f);

            var btn = btnGO.GetComponent<Button>();
            btn.interactable = unlocked;

            int capturedId = lid;  // 闭包捕获
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[LevelSelect] 选择关卡 {capturedId}");
                menuController.EnterGame(capturedId);
            });

            // 关卡文字
            var txtGO = new GameObject("txtLabel", typeof(RectTransform), typeof(Text));
            txtGO.transform.SetParent(btnGO.transform, false);
            var trt = txtGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            string lockIcon = unlocked ? "" : "\n[锁定]";
            string starStr = stars > 0 ? "\n" + new string('\u2605', stars) : "";
            string levelName = "";
            var levelData = System.Array.Find(levels, l => l.levelId == lid);
            if (levelData != null) levelName = levelData.levelName;

            var t = txtGO.GetComponent<Text>();
            t.text = $"{lid}\n{levelName}{starStr}{lockIcon}";
            t.fontSize = 13;
            t.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            t.alignment = TextAnchor.MiddleCenter;
            t.font = font;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
    }

    T Find<T>(string name) where T : Component
    {
        foreach (var c in GetComponentsInChildren<T>(true))
            if (c.name == name) return c;
        return null;
    }
}
