using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
        => Addressables.LoadAssetsAsync<T>(key, null, Addressables.MergeMode.Union, false);
}
