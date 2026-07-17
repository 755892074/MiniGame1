using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 登录面板控制器
/// 处理：隐私政策弹窗、游客/抖音登录、Toggle 交互
/// </summary>
public class LoginController : MonoBehaviour
{
    private MenuSceneController menuController;
    private bool firstTime;

    // UI 引用
    private Toggle tgPrivacy;
    private Button btnGuest, btnDouyin;
    private GameObject privacyPopup;
    private Button btnAgree, btnDisagree;

    public void Init(MenuSceneController controller, bool isFirst)
    {
        menuController = controller;
        firstTime = isFirst;

        BindUI();
        UpdateButtonState();
    }

    void BindUI()
    {
        // Toggle
        tgPrivacy = FindComponent<Toggle>("tgPrivacy");
        if (tgPrivacy != null)
            tgPrivacy.onValueChanged.AddListener(_ => UpdateButtonState());

        // 按钮
        btnGuest = FindComponent<Button>("btnGuestLogin");
        btnDouyin = FindComponent<Button>("btnDouyinLogin");

        if (btnGuest != null)
            btnGuest.onClick.AddListener(OnGuestLogin);
        if (btnDouyin != null)
            btnDouyin.onClick.AddListener(OnDouyinLogin);

        // 隐私弹窗
        privacyPopup = FindChild("pnlPrivacyPopup");
        btnAgree = FindComponent<Button>("btnAgree", privacyPopup?.transform);
        btnDisagree = FindComponent<Button>("btnDisagree", privacyPopup?.transform);

        if (btnAgree != null)
            btnAgree.onClick.AddListener(OnAgreePrivacy);
        if (btnDisagree != null)
            btnDisagree.onClick.AddListener(OnDisagreePrivacy);

        // 协议链接
        var btnUA = FindComponent<Button>("btnUserAgreement");
        var btnPP = FindComponent<Button>("btnPrivacyPolicy");
        if (btnUA != null) btnUA.onClick.AddListener(() => Debug.Log("[Login] 用户协议（待实现）"));
        if (btnPP != null) btnPP.onClick.AddListener(() => Debug.Log("[Login] 隐私政策（待实现）"));

        // 抖音环境下隐藏抖音登录按钮（已自动登录）
        if (CloudSaveBridge.IsAvailable && btnDouyin != null)
        {
            btnDouyin.gameObject.SetActive(false);
        }

        // 首次进入显示隐私弹窗
        if (firstTime && privacyPopup != null)
            privacyPopup.SetActive(true);
    }

    void UpdateButtonState()
    {
        bool canStart = tgPrivacy != null && tgPrivacy.isOn;
        if (btnGuest != null) btnGuest.interactable = canStart;
        if (btnDouyin != null) btnDouyin.interactable = canStart;
    }

    // ========================================
    // 事件处理
    // ========================================

    void OnAgreePrivacy()
    {
        if (privacyPopup != null) privacyPopup.SetActive(false);

        // 勾选 Toggle
        if (tgPrivacy != null) tgPrivacy.isOn = true;
        UpdateButtonState();
    }

    void OnDisagreePrivacy()
    {
        if (privacyPopup != null) privacyPopup.SetActive(false);
        Debug.Log("[Login] 用户不同意隐私政策");
        // 可以退出应用或保持等待
    }

    void OnGuestLogin()
    {
        Debug.Log("[Login] 游客模式登录");
        SaveSystem.Data.privacyAgreed = true;
        SaveSystem.Save();
        menuController.ShowMainMenu();
    }

    void OnDouyinLogin()
    {
        Debug.Log("[Login] 抖音授权登录");
        // TODO: 调用 tt.login() 获取 code，后端换 openid
        // 这里先模拟成功
        SaveSystem.Data.privacyAgreed = true;
        SaveSystem.Save();
        menuController.ShowMainMenu();
    }

    // ========================================
    // 工具
    // ========================================

    T FindComponent<T>(string name, Transform parent = null) where T : Component
    {
        var target = parent ?? transform;
        foreach (var c in target.GetComponentsInChildren<T>(true))
            if (c.name == name || c.gameObject.name == name) return c;
        return null;
    }

    GameObject FindChild(string name)
    {
        foreach (var t in transform.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.gameObject;
        return null;
    }
}
