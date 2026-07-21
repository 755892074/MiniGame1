using System;
using UnityEngine;

/// <summary>
/// 广告管理层（IAA 激励视频统一出口）。
/// 当前用 mock 实现（延迟回调 success），预留真 TTSDK 接入点。
/// 后续接真抖音广告：把 useMock 置 false，并在 RealShow 里调用 TTAdManager 激励视频 API。
/// </summary>
public class AdManager : MonoBehaviour
{
    private static AdManager _inst;
    public static AdManager Instance
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("AdManager");
                _inst = go.AddComponent<AdManager>();
            }
            return _inst;
        }
    }

    [Header("开发期用 mock；接真抖音激励视频后置 false")]
    public bool useMock = true;

    void Awake() { _inst = this; }

    /// <summary>播放一次激励视频。成功回调 true，失败/关闭回调 false。</summary>
    public void ShowRewardedAd(Action<bool> onResult)
    {
        if (useMock)
            StartCoroutine(MockPlay(onResult));
        else
            RealShow(onResult);
    }

    /// <summary>看广告补充道具时调用，成功直接发奖（无额外 UI 分支）。</summary>
    public void ShowRewardedAdForGrant(Action<bool> onResult) => ShowRewardedAd(onResult);

    private System.Collections.IEnumerator MockPlay(Action<bool> cb)
    {
        // 模拟广告播放耗时（开发期短一点，不卡验收）
        yield return new WaitForSeconds(0.4f);
        cb?.Invoke(true);
    }

    // TODO: 接入真 TTSDK 激励视频
    // 参考 com.bytedance.starksdk@6.7.9，调用 TTAdManager / 激励视频广告位：
    //   1. 初始化广告 SDK（TTAdManager.init，传入抖音 appId）
    //   2. 加载激励视频广告（adId = 你的广告位 ID）
    //   3. 播放完成后在回调里 cb?.Invoke(rewardVerified)
    private void RealShow(Action<bool> cb)
    {
        Debug.LogWarning("[AdManager] 真抖音激励视频未接入，临时回退 mock。");
        StartCoroutine(MockPlay(cb));
    }
}
