using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置面板控制器
/// 处理：音乐/音效/振动开关与音量、账号、隐私政策、重置存档
/// </summary>
public class SettingsController : MonoBehaviour
{
    private MenuSceneController menuController;

    // UI 引用
    private Slider sldMusic, sldSfx;
    private Toggle tgVibration;
    private Button btnClose, btnResetSave, btnAccount, btnPrivacy, btnAgreement;
    private GameObject confirmDialog;

    public void Init(MenuSceneController controller)
    {
        menuController = controller;
        LoadCurrentSettings();
        BindUI();
    }

    void LoadCurrentSettings()
    {
        // 从存档读取当前设置
        var data = SaveSystem.Data;
        sldMusic = Find<Slider>("sldMusicVol");
        sldSfx = Find<Slider>("sldSfxVol");
        tgVibration = Find<Toggle>("tgVibration");

        if (sldMusic != null) sldMusic.value = data.bgmVolume;
        if (sldSfx != null) sldSfx.value = data.sfxVolume;
        // 振动默认关
        if (tgVibration != null) tgVibration.isOn = false;
    }

    void BindUI()
    {
        // 音量变化
        if (sldMusic != null)
            sldMusic.onValueChanged.AddListener(v =>
            {
                SaveSystem.SetBgmVolume(v);
                Debug.Log($"[Settings] 音乐音量={v:F2}");
            });

        if (sldSfx != null)
            sldSfx.onValueChanged.AddListener(v =>
            {
                SaveSystem.SetSfxVolume(v);
                Debug.Log($"[Settings] 音效音量={v:F2}");
            });

        if (tgVibration != null)
            tgVibration.onValueChanged.AddListener(v =>
                Debug.Log($"[Settings] 振动={v}"));

        // 链接按钮
        btnAccount = Find<Button>("btnAccount");
        btnPrivacy = Find<Button>("btnPrivacy");
        btnAgreement = Find<Button>("btnAgreement");

        if (btnAccount != null) btnAccount.onClick.AddListener(() =>
            Debug.Log("[Settings] 账号管理（待实现）"));
        if (btnPrivacy != null) btnPrivacy.onClick.AddListener(() =>
            Debug.Log("[Settings] 隐私政策（待实现）"));
        if (btnAgreement != null) btnAgreement.onClick.AddListener(() =>
            Debug.Log("[Settings] 用户协议（待实现）"));

        // 重置存档（带二次确认）
        btnResetSave = Find<Button>("btnResetSave");
        if (btnResetSave != null)
            btnResetSave.onClick.AddListener(ShowResetConfirm);

        // 关闭
        btnClose = Find<Button>("btnClose");
        if (btnClose != null)
            btnClose.onClick.AddListener(() => menuController.CloseSettings());
    }

    // ========================================
    // 重置存档二次确认
    // ========================================

    void ShowResetConfirm()
    {
        // 创建简易确认弹窗
        confirmDialog = new GameObject("ResetConfirmDialog", typeof(RectTransform), typeof(Image));
        confirmDialog.transform.SetParent(transform, false);
        var rt = confirmDialog.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.one * 0.5f;
        rt.sizeDelta = new Vector2(500, 300);
        var bg = confirmDialog.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.15f, 0.12f, 0.98f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 警告文字
        var warnGO = new GameObject("txtWarn", typeof(RectTransform), typeof(Text));
        warnGO.transform.SetParent(confirmDialog.transform, false);
        var warnRT = warnGO.GetComponent<RectTransform>();
        warnRT.anchorMin = new Vector2(0.1f, 0.6f);
        warnRT.anchorMax = new Vector2(0.9f, 0.9f);
        warnRT.sizeDelta = Vector2.zero;
        var wt = warnGO.GetComponent<Text>();
        wt.text = "确定要重置所有存档吗？\n\n所有进度、货币、宠物将丢失！\n此操作不可撤销。";
        wt.fontSize = 18;
        wt.color = Color.white;
        wt.alignment = TextAnchor.MiddleCenter;
        wt.font = font;

        // 确认按钮
        var btnConfirmGO = new GameObject("btnConfirm", typeof(RectTransform), typeof(Image), typeof(Button));
        btnConfirmGO.transform.SetParent(confirmDialog.transform, false);
        var cRT = btnConfirmGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.1f, 0.1f);
        cRT.anchorMax = new Vector2(0.45f, 0.3f);
        cRT.sizeDelta = Vector2.zero;
        btnConfirmGO.GetComponent<Image>().color = new Color(0.79f, 0.24f, 0.24f);
        btnConfirmGO.GetComponent<Button>().onClick.AddListener(OnConfirmReset);

        var ctGO = new GameObject("txt", typeof(RectTransform), typeof(Text));
        ctGO.transform.SetParent(btnConfirmGO.transform, false);
        var ctrt = ctGO.GetComponent<RectTransform>();
        ctrt.anchorMin = Vector2.zero; ctrt.anchorMax = Vector2.one; ctrt.sizeDelta = Vector2.zero;
        var ct = ctGO.GetComponent<Text>();
        ct.text = "确认重置";
        ct.fontSize = 18; ct.color = Color.white;
        ct.alignment = TextAnchor.MiddleCenter;
        ct.font = font; ct.fontStyle = FontStyle.Bold;

        // 取消按钮
        var btnCancelGO = new GameObject("btnCancel", typeof(RectTransform), typeof(Image), typeof(Button));
        btnCancelGO.transform.SetParent(confirmDialog.transform, false);
        var xRT = btnCancelGO.GetComponent<RectTransform>();
        xRT.anchorMin = new Vector2(0.55f, 0.1f);
        xRT.anchorMax = new Vector2(0.9f, 0.3f);
        xRT.sizeDelta = Vector2.zero;
        btnCancelGO.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f);
        btnCancelGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (confirmDialog != null) Destroy(confirmDialog);
        });

        var xtGO = new GameObject("txt", typeof(RectTransform), typeof(Text));
        xtGO.transform.SetParent(btnCancelGO.transform, false);
        var xtrt = xtGO.GetComponent<RectTransform>();
        xtrt.anchorMin = Vector2.zero; xtrt.anchorMax = Vector2.one; xtrt.sizeDelta = Vector2.zero;
        var xt = xtGO.GetComponent<Text>();
        xt.text = "取消";
        xt.fontSize = 18; xt.color = Color.white;
        xt.alignment = TextAnchor.MiddleCenter;
        xt.font = font; xt.fontStyle = FontStyle.Bold;
    }

    void OnConfirmReset()
    {
        Debug.Log("[Settings] 执行存档重置");
        SaveSystem.ResetAll();
        if (confirmDialog != null) Destroy(confirmDialog);
        menuController.CloseSettings();
        // 回到登录面板
        menuController.ShowLoginPanel(false);
    }

    T Find<T>(string name) where T : Component
    {
        foreach (var c in GetComponentsInChildren<T>(true))
            if (c.name == name) return c;
        return null;
    }
}
