#if !UNITY_EDITOR
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.Scripting;

/// <summary>
/// 抖音小游戏 Addressables CDN 路径修复
/// 构建后自动替换 dummy 占位符为真实 UOS CDN 地址。
/// 放工程 Assets 目录下即可，无需挂载，BeforeSceneLoad 自动执行。
/// </summary>
[Preserve]
public static class FixDouyinAddressablePath
{
    // TODO: 开通 UOS CDN 后替换为真实 BUCKET_ID
    private const string BUCKET_ID = "<YOUR_BUCKET_ID>";
    private const string DUMMY = "https://dummy.dummy.dummy/";
    private static readonly string RealCdn = $"https://a.unity.cn/client_api/v1/buckets/{BUCKET_ID}/release_by_badge/latest/content/";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Fix()
    {
        if (BUCKET_ID == "<YOUR_BUCKET_ID>")
        {
            Debug.LogWarning("[FixDouyin] BUCKET_ID 未配置，跳过 CDN 路径替换。开通 UOS CDN 后在此填入真实 ID。");
            return;
        }

        Addressables.InternalIdTransformFunc = location =>
        {
            string id = location.InternalId;
            if (id.StartsWith(DUMMY))
                id = id.Replace(DUMMY, RealCdn);
            // 修复 StreamingAssets 路径双写 bug
            if (id.Contains("StreamingAssets/StreamingAssets/"))
                id = id.Replace("StreamingAssets/StreamingAssets/", "StreamingAssets/");
            return id;
        };

        Debug.Log($"[FixDouyin] CDN 路径修复已启用: {RealCdn}");
    }
}
#endif
