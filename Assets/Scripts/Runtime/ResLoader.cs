using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

/// <summary>
/// 资源加载门面（doc/16 阶段4）：统一封装 Addressables，隐藏本地/CDN 差异。
/// 抖音小游戏为 WebGL 类环境，禁止同步等待，所有加载均为异步（用 .Completed 回调或 await handle.Task）。
/// key 约定：优先用资源的 Addressable 地址（默认=资源路径，如 "Assets/Prefabs/UI/PrefabsV2/SplashPanel.prefab"）；
/// 关卡用 Label "Levels" 批量加载。
/// </summary>
public enum ResLoadMode { Local, CDN, Auto }

public static class ResLoader
{
    public static ResLoadMode Mode = ResLoadMode.Auto;

    public static AsyncOperationHandle<GameObject> LoadPrefab(string key)
        => Addressables.LoadAssetAsync<GameObject>(key);

    public static AsyncOperationHandle<Sprite> LoadSprite(string key)
        => Addressables.LoadAssetAsync<Sprite>(key);

    public static AsyncOperationHandle<T> Load<T>(string key) where T : Object
        => Addressables.LoadAssetAsync<T>(key);

    /// <summary>按 key/label 加载一组资产（如全部关卡）。Result 为 IList&lt;T&gt;。</summary>
    public static AsyncOperationHandle<IList<T>> LoadAll<T>(string key) where T : Object
    {
        // 两步加载：先解析 location，再按地址列表加载。绕开 Addressables 1.22.3 的 label 类型匹配 bug。
        var locHandle = Addressables.LoadResourceLocationsAsync(key, typeof(T));
        locHandle.WaitForCompletion();
        if (locHandle.Status != AsyncOperationStatus.Succeeded || locHandle.Result == null || locHandle.Result.Count == 0)
        {
            Addressables.Release(locHandle);
            Debug.LogWarning($"[ResLoader] LoadAll<{typeof(T).Name}>(\"{key}\"): 无匹配资源");
            return Addressables.ResourceManager.CreateCompletedOperation<IList<T>>(new List<T>(), "LoadAll: empty");
        }

        var keys = new List<object>(locHandle.Result.Select(l => (object)l.PrimaryKey));
        Addressables.Release(locHandle);
        return Addressables.LoadAssetsAsync<T>(keys, null, Addressables.MergeMode.Union, false);
    }
}
