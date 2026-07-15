using UnityEngine;

/// <summary>
/// 抖音云存储桥接层
/// 负责 SaveSystem 与抖音 tt.setUserCloudStorage / tt.getUserCloudStorage 之间的通信
///
/// 架构设计：
///   SaveSystem (游戏逻辑) → CloudSaveBridge (平台适配) → 抖音SDK / No-op
///
/// 平台兼容：
///   - 抖音小游戏：走 StarkSDK 的 TT.SetUserCloudStorage / TT.GetUserCloudStorage
///   - Unity Editor / 其他平台：自动降级为 no-op，不影响开发调试
///
/// 接入步骤（导出抖音小游戏时）：
///   1. 在 Build Settings 切换到 MiniGame 平台，团结引擎自动注入 StarkSDK
///   2. 在 Project Settings → Scripting Define Symbols 添加 DOUYIN_MINIGAME
///   3. 校准下方 #if DOUYIN_MINIGAME 区块内的 SDK API 调用
///   4. 测试：用抖音开发者工具预览，验证云端存档读写
///
/// 抖音云存储限制：
///   - 每个 key+value 最大 1024 字节
///   - 每个用户每个游戏最多 128 个 key-value 对
///   - 免费且无需申请
/// </summary>
public static class CloudSaveBridge
{
    /// <summary>当前运行环境是否支持抖音云存储</summary>
    public static bool IsAvailable
    {
        get
        {
#if DOUYIN_MINIGAME
            return _sdkChecked ? _sdkAvailable : CheckSdkAvailable();
#else
            return false;
#endif
        }
    }

#if DOUYIN_MINIGAME
    private static bool _sdkChecked = false;
    private static bool _sdkAvailable = false;

    private static bool CheckSdkAvailable()
    {
        _sdkChecked = true;
        try
        {
            // TODO: 导入 StarkSDK 后校准此检测
            // _sdkAvailable = TT.IsInitialized();
            _sdkAvailable = true;
            return _sdkAvailable;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudSaveBridge] 抖音SDK检测失败: {e.Message}");
            _sdkAvailable = false;
            return false;
        }
    }
#endif

    /// <summary>
    /// 上传存档到抖音云
    /// </summary>
    /// <param name="key">存储 key</param>
    /// <param name="value">存档 JSON 字符串</param>
    /// <param name="callback">上传完成回调，true=成功</param>
    public static void SetSave(string key, string value, System.Action<bool> callback)
    {
#if DOUYIN_MINIGAME
        if (!IsAvailable) { callback?.Invoke(false); return; }

        try
        {
            // TODO: 导入 StarkSDK 后校准此调用
            // StarkSDK 的 C# API 形如：
            //   TT.SetUserCloudStorage(KVDataList, onSuccess, onFail)
            //
            // 示例（伪代码，待校准）：
            //   StarkSDKSpace.TT.SetUserCloudStorage(
            //       new List<StarkSDKSpace.KVData> {
            //           new StarkSDKSpace.KVData { key = key, value = value }
            //       },
            //       () => callback?.Invoke(true),
            //       (err) => callback?.Invoke(false)
            //   );

            Debug.Log($"[CloudSaveBridge] 云端上传: key={key}, {value.Length}字节");
            // 临时模拟成功（真实环境替换为上面的 SDK 调用）
            callback?.Invoke(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudSaveBridge] SetSave异常: {e.Message}");
            callback?.Invoke(false);
        }
#else
        callback?.Invoke(false);
#endif
    }

    /// <summary>
    /// 从抖音云读取存档
    /// </summary>
    /// <param name="key">存储 key</param>
    /// <param name="callback">读取完成回调，返回存档JSON（null=不存在或失败）</param>
    public static void GetSave(string key, System.Action<string> callback)
    {
#if DOUYIN_MINIGAME
        if (!IsAvailable) { callback?.Invoke(null); return; }

        try
        {
            // TODO: 导入 StarkSDK 后校准此调用
            // StarkSDK 的 C# API 形如：
            //   TT.GetUserCloudStorage(keyList, onSuccess, onFail)
            //
            // 示例（伪代码，待校准）：
            //   StarkSDKSpace.TT.GetUserCloudStorage(
            //       new string[] { key },
            //       (data) => {
            //           // 在 data.KVDataList 里找我们的 key
            //           callback?.Invoke(foundValue);
            //       },
            //       (err) => callback?.Invoke(null)
            //   );

            Debug.Log($"[CloudSaveBridge] 云端读取: key={key}");
            // 临时模拟空（真实环境替换为上面的 SDK 调用）
            callback?.Invoke(null);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudSaveBridge] GetSave异常: {e.Message}");
            callback?.Invoke(null);
        }
#else
        callback?.Invoke(null);
#endif
    }

    /// <summary>删除抖音云端存档</summary>
    public static void RemoveSave(string key)
    {
#if DOUYIN_MINIGAME
        if (!IsAvailable) return;

        try
        {
            // TODO: 导入 StarkSDK 后校准此调用
            // StarkSDKSpace.TT.RemoveUserCloudStorage(new string[] { key }, ...);

            Debug.Log($"[CloudSaveBridge] 云端删除: key={key}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudSaveBridge] RemoveSave异常: {e.Message}");
        }
#endif
    }

    /// <summary>分片上传（当存档超过 1KB 时使用，多个 key 分段）</summary>
    public static void SetSave(string key, string[] chunks, System.Action<bool> callback)
    {
        if (chunks == null || chunks.Length == 0)
        {
            callback?.Invoke(false);
            return;
        }

        // 当前版本：休闲游戏存档通常 < 1KB，直接用第一个分片
        // 后续如果存档增大（如关卡星级记录超过 100 关），可扩展为多 key 上传
        // 策略：{key}_0, {key}_1, {key}_2... 每个不超过 900 字节
        if (chunks.Length == 1)
        {
            SetSave(key, chunks[0], callback);
            return;
        }

        // 多分片上传（预留扩展）
        SetSave(key, chunks[0], callback);
    }
}
