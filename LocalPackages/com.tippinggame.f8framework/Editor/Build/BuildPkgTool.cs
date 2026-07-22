using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace F8Framework.Core.Editor
{
    public class BuildPkgTool : ScriptableObject
    {
        private static readonly GUILayoutOption NormalWidth = GUILayout.Width(100);
        private static readonly GUILayoutOption ButtonHeight = GUILayout.Height(20);
        private static readonly GUILayoutOption BigNormalWidth = GUILayout.Width(140);
        private static readonly GUILayoutOption BigButtonHeight = GUILayout.Height(35);
        private static string _prefBuildPathKey = "PrefBuildPathKey";
        private static string _exportPlatformKey = "ExportPlatformKey";
        private static string _exportCurrentPlatformKey = "ExportCurrentPlatformKey";
        private static string _toVersionKey = "ToVersionKey";
        private static string _codeVersionKey = "CodeVersionKey";
        private static string _enableHotUpdateKey = "EnableHotUpdateKey";
        private static string _enableFullPackageKey = "EnableFullPackageKey";
        private static string _enableOptionalPackageKey = "EnableOptionalPackageKey";
        private static string _enableNullPackageKey = "EnableNullPackageKey";
        private static string _optionalPackageKey = "OptionalPackageKey";
        private static string _assetRemoteAddressKey = "AssetRemoteAddressKey";
        private static string _enablePackageKey = "EnablePackageKey";
        private static string _locationPathNameKey = "LocationPathNameKey";
        private static string _androidBuildAppBundleKey = "AndroidBuildAppBundleKey";
        private static string _androidUseKeystoreKey = "AndroidUseKeystoreKey";
        private static string _androidKeystoreNameKey = "AndroidKeystoreNameKey";
        private static string _androidKeystorePassKey = "AndroidKeystorePassKey";
        private static string _androidKeyAliasNameKey = "AndroidKeyAliasNameKey";
        private static string _androidKeyAliasPassKey = "AndroidKeyAliasPassKey";
        public static string EnableFullPathAssetLoadingKey = "FullPathAssetLoadingKey";
        public static string EnableFullPathExtensionAssetLoadingKey = "FullPathExtensionAssetLoadingKey";
        public static string ExcelPathKey = "ExcelPath";
        public static string ConvertExcelToOtherFormatsKey = "ConvertExcelToOtherFormatsKey";
        public static string ForceRebuildAssetBundleKey = "ForceRebuildAssetBundleKey";
        public static string CleanBuildCacheKey = "CleanBuildCacheKey";
        public static string ExcelBinDataFolderKey = "ExcelBinDataFolderKey";

        private static string _buildPath = "";
        private static string _toVersion = "1.0.0";
        private static string _codeVersion = "1";
        private static bool _enableHotUpdate = false;
        private static bool _enableFullPackage = true;
        private static bool _enableOptionalPackage = false;
        private static bool _enableNullPackage = false;
        private static string _optionalPackage = "0_1_2_3";
        private static string _optionalPackagePassword = "";
        private static string _assetRemoteAddress = "";
        private static bool _enablePackage = false;
        private static bool _enableFullPathAssetLoading = false;
        private static bool _enableFullPathExtensionAssetLoading = false;
        private static string _excelPath = "";
        private static string _convertExcelToOtherFormats = "binary";
        public static string[] ExcelToOtherFormats = { "json", "binary" };
        private static bool _forceRebuildAssetBundle = false;
        private static bool _cleanBuildCache = false;
        private static bool _appendHashToAssetBundleName = false;
        private static bool _forceRemoteAssetBundle = false;
        private static bool _disableUnityCacheOnWebGL = false;
        private static int _assetBundleOffset = 0;
        private static int _assetBundleXorKey = 0;
        private static string _excelBinDataFolder = "";
        private static string _assetManifestEncryptKey = "";

        private static BuildTarget _buildTarget = BuildTarget.NoTarget;

        private static int _index = 0;
        private static BuildTarget[] _options = Enum.GetValues(typeof(BuildTarget))
            .Cast<BuildTarget>()
            .Select(option => (BuildTarget)Enum.Parse(typeof(BuildTarget), option.ToString()))
            .ToArray();
        private static string[] _optionNames = Array.ConvertAll(_options, option => option.ToString());

        private static bool _exportCurrentPlatform = true;
        private static bool _androidBuildAppBundle = false;
        private static bool _androidUseKeystore = false;
        private static string _androidKeystoreName = "";
        private static string _androidKeystorePass = "";
        private static string _androidKeyAliasName = "";
        private static string _androidKeyAliasPass = "";

        public static string BuildPath => URLSetting.AddRootPath(F8EditorPrefs.GetString(_prefBuildPathKey, null)) ?? _buildPath;
        public static string ToVersion => F8EditorPrefs.GetString(_toVersionKey, null) ?? _toVersion;

        private static string ChangeVersionLastNumber(string version, int delta, int minValue)
        {
            if (string.IsNullOrEmpty(version))
            {
                return version;
            }

            string[] versionParts = version.Split('.');
            if (versionParts.Length == 0)
            {
                return version;
            }

            string lastPart = versionParts[versionParts.Length - 1];
            if (!int.TryParse(lastPart, out int lastNumber))
            {
                return version;
            }

            versionParts[versionParts.Length - 1] = Mathf.Max(minValue, lastNumber + delta).ToString();
            return string.Join(".", versionParts);
        }

        private static string ChangeBuildCount(string buildCount, int delta, int minValue)
        {
            if (!int.TryParse(buildCount, out int count))
            {
                return buildCount;
            }

            return Mathf.Max(minValue, count + delta).ToString();
        }

        private static bool GetBoolArg(string[] args, string argName)
        {
            return string.Equals(GetArgValue(args, argName), "true", StringComparison.OrdinalIgnoreCase);
        }

        public static void JenkinsBuild()
        {
            string[] args = Environment.GetCommandLineArgs();

            string platformStr = GetArgValue(args, "Platform-");
            BuildTarget platform = BuildTarget.NoTarget;
            if (Enum.TryParse<BuildTarget>(platformStr, out platform))
            {
                LogF8.Log($"转换成功: {platform}");
            }
            string buildPath = GetArgValue(args, "BuildPath-");
            string version = GetArgValue(args, "Version-");
            string codeVersion = GetArgValue(args, "CodeVersion-");
            string assetRemoteAddress = GetArgValue(args, "AssetRemoteAddress-");
            bool enableHotUpdate = GetBoolArg(args, "EnableHotUpdate-");
            bool enablePackage = GetBoolArg(args, "EnablePackage-");
            bool enableFullPackage = GetBoolArg(args, "EnableFullPackage-");
            bool enableOptionalPackage = GetBoolArg(args, "EnableOptionalPackage-");
            string optionalPackage = GetArgValue(args, "OptionalPackage-");
            bool enableNullPackage = GetBoolArg(args, "EnableNullPackage-");
            bool androidBuildAppBundle = GetBoolArg(args, "AndroidBuildAppBundle-");
            bool androidUseKeystore = GetBoolArg(args, "AndroidUseKeystore-");
            string androidKeystoreName = GetArgValue(args, "AndroidKeystoreName-");
            string androidKeystorePass = GetArgValue(args, "AndroidKeystorePass-");
            string androidKeyAliasName = GetArgValue(args, "AndroidKeyAliasName-");
            string androidKeyAliasPass = GetArgValue(args, "AndroidKeyAliasPass-");
            bool cleanBuildCache = GetBoolArg(args, "CleanBuildCache-");
            string optionalPackagePassword = GetArgValue(args, "OptionalPackagePassword-") ?? "";
            string assetManifestEncryptKey = GetArgValue(args, "AssetManifestEncryptKey-") ?? "";

            F8EditorPrefs.SetBool(_exportCurrentPlatformKey, false);
            F8EditorPrefs.SetString(_exportPlatformKey, platformStr);
            _buildTarget = platform;
            F8EditorPrefs.SetString(_prefBuildPathKey, URLSetting.RemoveRootPath(buildPath));
            _buildPath = buildPath;
            F8EditorPrefs.SetString(_toVersionKey, version);
            _toVersion = version;
            F8EditorPrefs.SetString(_codeVersionKey, codeVersion);
            _codeVersion = codeVersion;
            F8EditorPrefs.SetBool(_enableHotUpdateKey, enableHotUpdate);
            _enableHotUpdate = enableHotUpdate;
            F8EditorPrefs.SetBool(_enableFullPackageKey, enableFullPackage);
            _enableFullPackage = enableFullPackage;
            F8EditorPrefs.SetBool(_enableOptionalPackageKey, enableOptionalPackage);
            _enableOptionalPackage = enableOptionalPackage;
            F8EditorPrefs.SetBool(_enableNullPackageKey, enableNullPackage);
            _enableNullPackage = enableNullPackage;
            F8EditorPrefs.SetString(_optionalPackageKey, optionalPackage);
            _optionalPackage = optionalPackage;
            F8EditorPrefs.SetBool(_enablePackageKey, enablePackage);
            _enablePackage = enablePackage;
            F8EditorPrefs.SetString(_assetRemoteAddressKey, assetRemoteAddress);
            _assetRemoteAddress = assetRemoteAddress;

            F8EditorPrefs.SetBool(_androidBuildAppBundleKey, androidBuildAppBundle);
            _androidBuildAppBundle = androidBuildAppBundle;
            F8EditorPrefs.SetBool(_androidUseKeystoreKey, androidUseKeystore);
            _androidUseKeystore = androidUseKeystore;
            F8EditorPrefs.SetString(_androidKeystoreNameKey, URLSetting.RemoveRootPath(androidKeystoreName));
            _androidKeystoreName = androidKeystoreName;
            F8EditorPrefs.SetString(_androidKeystorePassKey, androidKeystorePass);
            _androidKeystorePass = androidKeystorePass;
            F8EditorPrefs.SetString(_androidKeyAliasNameKey, androidKeyAliasName);
            _androidKeyAliasName = androidKeyAliasName;
            F8EditorPrefs.SetString(_androidKeyAliasPassKey, androidKeyAliasPass);
            _androidKeyAliasPass = androidKeyAliasPass;
            F8EditorPrefs.SetBool(CleanBuildCacheKey, cleanBuildCache);
            F8GamePrefs.SetString(nameof(F8GameConfig.OptionalPackagePassword), optionalPackagePassword);
            _optionalPackagePassword = optionalPackagePassword;
            F8GamePrefs.SetString(nameof(F8GameConfig.AssetManifestEncryptKey), assetManifestEncryptKey);
            _assetManifestEncryptKey = assetManifestEncryptKey;

            WriteGameVersion();
            Build();
            WriteAssetVersion();
        }

        public static string GetArgValue(string[] args, string argName)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == argName && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        public static void BuildUpdate()
        {
            string buildPath = URLSetting.AddRootPath(F8EditorPrefs.GetString(_prefBuildPathKey, ""));
            string toVersion = F8EditorPrefs.GetString(_toVersionKey, "");

            string gameVersionPath = buildPath + HotUpdateManager.RemoteDirName + "/" + nameof(GameVersion) + ".json";
            string assetBundleMapPath = buildPath + HotUpdateManager.RemoteDirName + "/" + nameof(AssetBundleMap) + ".json";
            string hotUpdateMapPath = buildPath + HotUpdateManager.RemoteDirName + HotUpdateManager.HotUpdateDirName +
                                      HotUpdateManager.Separator + nameof(AssetBundleMap) + ".json";
            if (!File.Exists(gameVersionPath) || !File.Exists(assetBundleMapPath))
            {
                EditorUtility.DisplayDialog("注意！！！", "\n请先构建一个游戏版本，再构建热更新文件！~", "确定");
                LogF8.LogError("请先构建一个游戏版本，再构建热更新文件！~");
                return;
            }

            string assetManifestEncryptKey = F8GamePrefs.GetString(nameof(F8GameConfig.AssetManifestEncryptKey), "");
            GameVersion remoteGameVersion = Util.LitJson.ToObject<GameVersion>(F8JsonEncryption.ReadJsonFromFile(gameVersionPath, assetManifestEncryptKey));
            int result = GameConfig.CompareVersions(toVersion, remoteGameVersion.Version);
            if (result <= 0)
            {
                EditorUtility.DisplayDialog("注意！！！", "\n热更新版本必须大于当前游戏版本！~", "确定");
                LogF8.LogError("热更新版本必须大于当前游戏版本！~");
                return;
            }

            var resAssetBundleMappings = Util.LitJson.ToObject<Dictionary<string, AssetBundleMap.AssetMapping>>(F8JsonEncryption.ReadJsonFromTextAsset(Resources.Load<TextAsset>(nameof(AssetBundleMap))));
            var assetBundleMappings = Util.LitJson.ToObject<Dictionary<string, AssetBundleMap.AssetMapping>>(F8JsonEncryption.ReadJsonFromFile(assetBundleMapPath, assetManifestEncryptKey));
            Dictionary<string, AssetBundleMap.AssetMapping> hotUpdateAssetBundleMappings = new Dictionary<string, AssetBundleMap.AssetMapping>();
            if (File.Exists(hotUpdateMapPath))
            {
                hotUpdateAssetBundleMappings = Util.LitJson.ToObject<Dictionary<string, AssetBundleMap.AssetMapping>>(F8JsonEncryption.ReadJsonFromFile(hotUpdateMapPath, assetManifestEncryptKey)) ?? new Dictionary<string, AssetBundleMap.AssetMapping>();
            }

            Dictionary<string, AssetBundleMap.AssetMapping> generateAssetBundleMappings = new Dictionary<string, AssetBundleMap.AssetMapping>();
            foreach (var resAssetMapping in resAssetBundleMappings)
            {
                assetBundleMappings.TryGetValue(resAssetMapping.Key, out AssetBundleMap.AssetMapping assetMapping);
                if (assetMapping == null || resAssetMapping.Value.MD5 != assetMapping.MD5)
                {
                    if (F8Helper.AOTDllList.Contains(resAssetMapping.Key + "by"))
                    {
                        continue;
                    }
                    generateAssetBundleMappings.TryAdd(resAssetMapping.Key, resAssetMapping.Value);
                    assetBundleMappings[resAssetMapping.Key] = resAssetMapping.Value;
                    assetBundleMappings[resAssetMapping.Key].Updated = "1";
                }
            }

            string hotUpdatePath = buildPath + HotUpdateManager.RemoteDirName + HotUpdateManager.HotUpdateDirName + HotUpdateManager.Separator + toVersion;
            FileTools.CheckDirAndCreateWhenNeeded(hotUpdatePath);
            FileTools.SafeClearDir(hotUpdatePath);
            CopyHotUpdateAb(URLSetting.GetAssetBundlesStreamPath(), generateAssetBundleMappings, hotUpdatePath);

            remoteGameVersion.Version = toVersion;
            if (!remoteGameVersion.HotUpdateVersion.Contains(toVersion))
                remoteGameVersion.HotUpdateVersion.Add(toVersion);
            F8JsonEncryption.WriteJsonToFile(gameVersionPath, Util.LitJson.ToJson(remoteGameVersion), assetManifestEncryptKey);

            foreach (var assetMapping in generateAssetBundleMappings)
            {
                hotUpdateAssetBundleMappings[assetMapping.Key] = assetMapping.Value;
            }

            F8JsonEncryption.WriteJsonToFile(assetBundleMapPath, Util.LitJson.ToJson(assetBundleMappings), assetManifestEncryptKey);
            F8JsonEncryption.WriteJsonToFile(hotUpdateMapPath, Util.LitJson.ToJson(hotUpdateAssetBundleMappings), assetManifestEncryptKey);

            LogF8.LogVersion("构建热更新包版本成功！版本：" + toVersion);

            SyncForceRemoteAssetBundles(buildPath);
            AssetDatabase.Refresh();
        }

        public static void RunExportedGame()
        {
        }

        public static void Build()
        {
            string appName = Application.productName;
            string buildPath = URLSetting.AddRootPath(F8EditorPrefs.GetString(_prefBuildPathKey, ""));

            Array enumValues = Enum.GetValues(typeof(BuildTarget));
            int index = Array.FindIndex((BuildTarget[])enumValues, target =>
                target.ToString() == F8EditorPrefs.GetString(_exportPlatformKey, ""));

            BuildTarget buildTarget = F8EditorPrefs.GetBool(_exportCurrentPlatformKey, true) ? EditorUserBuildSettings.activeBuildTarget : _options[index];

            string toVersion = F8EditorPrefs.GetString(_toVersionKey, "");
            string codeVersion = F8EditorPrefs.GetString(_codeVersionKey, "");
            string optionalPackage = F8EditorPrefs.GetString(_optionalPackageKey, "");
            string optionalPackagePassword = F8GamePrefs.GetString(nameof(F8GameConfig.OptionalPackagePassword), "");

            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.WSAPlayer:
                    appName += ".exe";
                    break;
                case BuildTarget.Android:
                    appName += F8EditorPrefs.GetBool(_androidBuildAppBundleKey, false) ? ".aab" : ".apk";
                    EditorUserBuildSettings.buildAppBundle = F8EditorPrefs.GetBool(_androidBuildAppBundleKey, false);
                    PlayerSettings.Android.useCustomKeystore = F8EditorPrefs.GetBool(_androidUseKeystoreKey, false);
                    PlayerSettings.Android.keystoreName = URLSetting.AddRootPath(F8EditorPrefs.GetString(_androidKeystoreNameKey, ""));
                    PlayerSettings.Android.keystorePass = F8EditorPrefs.GetString(_androidKeystorePassKey, "");
                    PlayerSettings.Android.keyaliasName = F8EditorPrefs.GetString(_androidKeyAliasNameKey, "");
                    PlayerSettings.Android.keyaliasPass = F8EditorPrefs.GetString(_androidKeyAliasPassKey, "");
                    break;
            }

            PlayerSettings.bundleVersion = toVersion;
            PlayerSettings.Android.bundleVersionCode = int.Parse(codeVersion);
            PlayerSettings.iOS.buildNumber = codeVersion;
            PlayerSettings.macOS.buildNumber = codeVersion;
            PlayerSettings.tvOS.buildNumber = codeVersion;

            bool enableFullPackage = F8EditorPrefs.GetBool(_enableFullPackageKey, true);
            bool enableOptionalPackage = F8EditorPrefs.GetBool(_enableOptionalPackageKey, false);
            bool enableNullPackage = F8EditorPrefs.GetBool(_enableNullPackageKey, false);

            BuildOptions buildOptions = BuildOptions.None;
            if (SessionState.GetBool("compilationFinishedBuildRun", false))
            {
                buildOptions |= BuildOptions.AutoRunPlayer;
            }
            if (F8EditorPrefs.GetBool(CleanBuildCacheKey))
            {
                buildOptions |= BuildOptions.CleanBuildCache;
            }

            if (enableFullPackage)
            {
                string locationPathName = buildPath + "/" + buildTarget.ToString() + "_Full_" + toVersion + "/" + appName;
                locationPathName = FileTools.FormatToUnityPath(locationPathName);
                F8EditorPrefs.SetString(_locationPathNameKey, locationPathName);
                FileTools.CheckFileAndCreateDirWhenNeeded(locationPathName);

                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = GetBuildScenes(),
                    locationPathName = locationPathName,
                    target = buildTarget,
                    options = buildOptions,
                };
                BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
                if (buildReport.summary.result != BuildResult.Succeeded)
                {
                    LogF8.LogError($"导出失败了，检查一下 Unity 内置的 Build Settings 导出的路径是否存在，并使用 Unity 内置打包工具打包一次，或 Unity 没有给我清理缓存，尝试使用 Clean 打包模式！: {buildReport.summary.result}");
                }

                LogF8.LogVersion("游戏全量包打包成功! " + locationPathName);
            }

            SyncForceRemoteAssetBundles(buildPath);
            AssetDatabase.Refresh();
        }

        public static void SetBuildTarget() { }
        public static void DrawRootDirectory() { }
        public static void DrawVersion() { }
        public static void DrawAssetSetting() { }
        public static void DrawHotUpdate() { }
        public static void DrawBuildPkg() { }

        private static void CopyHotUpdateAb(string assetBundlesOutPath, Dictionary<string, AssetBundleMap.AssetMapping> mappings, string toPath)
        {
            Dictionary<string, AssetBundleMap.AssetMapping> temp_mappings = new Dictionary<string, AssetBundleMap.AssetMapping>();
            foreach (var mapping in mappings)
            {
                temp_mappings.TryAdd(mapping.Value.AbName, mapping.Value);
            }

            Stack<string> stack = new Stack<string>();
            stack.Push(assetBundlesOutPath);

            while (stack.Count > 0)
            {
                string currentPath = stack.Pop();
                string[] directories = Directory.GetDirectories(currentPath);
                foreach (string directory in directories)
                {
                    stack.Push(directory);
                }

                string[] files = Directory.GetFiles(currentPath);
                foreach (string file in files)
                {
                    string extension = Path.GetExtension(file).ToLower();
                    if (extension != ".meta" && extension != ".manifest" && extension != ".ds_store")
                    {
                        string filePath = FileTools.FormatToUnityPath(file);
                        string filePathManifest = FileTools.FormatToUnityPath(file) + ".manifest";
                        string abName = filePath.Replace(assetBundlesOutPath + "/", "");
                        if (temp_mappings.TryGetValue(abName, out AssetBundleMap.AssetMapping assetMapping))
                        {
                            FileTools.SafeCopyFile(filePath,
                                FileTools.FormatToUnityPath(toPath + "/" + ABBuildTool.GetAssetBundlesPath(filePath)));
                            FileTools.SafeCopyFile(filePathManifest,
                                FileTools.FormatToUnityPath(toPath + "/" + ABBuildTool.GetAssetBundlesPath(filePathManifest)));
                        }
                    }
                }
            }
            AssetDatabase.Refresh();
        }

        private static void SyncForceRemoteAssetBundles(string buildPath)
        {
            if (!F8GamePrefs.GetBool(nameof(F8GameConfig.ForceRemoteAssetBundle)))
            {
                return;
            }

            if (string.IsNullOrEmpty(buildPath))
            {
                LogF8.LogError("强制远程资产加载模式下，同步AssetBundles到CDN目录失败：打包输出目录为空。");
                return;
            }

            string assetBundlesPath = FileTools.FormatToUnityPath(Application.streamingAssetsPath + "/" + URLSetting.AssetBundlesName);
            if (!Directory.Exists(assetBundlesPath))
            {
                LogF8.LogError("强制远程资产加载模式下，同步AssetBundles到CDN目录失败，源目录不存在：" + assetBundlesPath);
                return;
            }

            string remoteAssetBundlesPath = FileTools.FormatToUnityPath(buildPath + HotUpdateManager.RemoteDirName + "/" + URLSetting.AssetBundlesName);
            FileTools.SafeClearDir(remoteAssetBundlesPath);
            if (FileTools.SafeCopyDirectory(assetBundlesPath, remoteAssetBundlesPath, true, new[] { ".meta", ".DS_Store" }))
            {
                LogF8.LogVersion("强制远程资产加载模式：已同步全量AssetBundles到CDN目录：" + remoteAssetBundlesPath);
            }
        }

        public static void WriteAssetVersion()
        {
            string buildPath = URLSetting.AddRootPath(F8EditorPrefs.GetString(_prefBuildPathKey, ""));
            string assetBundleMapPath = Application.dataPath + "/F8Framework/AssetMap/Resources/" + nameof(AssetBundleMap) + ".json";
            FileTools.SafeCopyFile(assetBundleMapPath, buildPath + HotUpdateManager.RemoteDirName + "/" + nameof(AssetBundleMap) + ".json");

            string hotUpdateMapPath = buildPath + HotUpdateManager.RemoteDirName + HotUpdateManager.HotUpdateDirName + HotUpdateManager.Separator + nameof(AssetBundleMap) + ".json";
            FileTools.CheckFileAndCreateDirWhenNeeded(hotUpdateMapPath);
            F8JsonEncryption.WriteJsonToFile(hotUpdateMapPath, Util.LitJson.ToJson(new Dictionary<string, AssetBundleMap.AssetMapping>()));
            UnityEditor.AssetDatabase.Refresh();
        }

        public static void WriteGameVersion()
        {
            string optionalPackage = F8EditorPrefs.GetString(_optionalPackageKey, "");
            string toVersion = F8EditorPrefs.GetString(_toVersionKey, "");
            string assetRemoteAddress = GameConfig.BuildAssetRemoteAddress(F8EditorPrefs.GetString(_assetRemoteAddressKey, ""));
            bool enableHotUpdate = F8EditorPrefs.GetBool(_enableHotUpdateKey, false);
            bool _enablePackage = F8EditorPrefs.GetBool(_enablePackageKey, false);
            string buildPath = URLSetting.AddRootPath(F8EditorPrefs.GetString(_prefBuildPathKey, ""));

            string gameVersionPath = FileTools.FormatToUnityPath(FileTools.TruncatePath(GetScriptPath(), 3)) + "/AssetMap/Resources/" + nameof(GameVersion) + ".json";
            FileTools.SafeDeleteFile(gameVersionPath);
            FileTools.SafeDeleteFile(gameVersionPath + ".meta");
            FileTools.CheckFileAndCreateDirWhenNeeded(gameVersionPath);
            AssetDatabase.Refresh();

            List<string> packageList;
            if (!string.IsNullOrEmpty(optionalPackage))
            {
                packageList = new List<string>(optionalPackage.Split(HotUpdateManager.Separator));
            }
            else
            {
                packageList = new List<string>();
            }

            Dictionary<string, (long size, string md5)> subPackageInfo = new Dictionary<string, (long size, string md5)>();
            if (_enablePackage && packageList.Count > 0)
            {
                string remotePackageDir = buildPath + HotUpdateManager.RemoteDirName + HotUpdateManager.PackageDirName;
                foreach (var package in packageList)
                {
                    if (string.IsNullOrEmpty(package)) continue;
                    string packageZipPath = remotePackageDir + HotUpdateManager.Separator + package + ".zip";
                    if (File.Exists(packageZipPath))
                    {
                        string md5 = FileTools.CreateMd5ForFile(packageZipPath);
                        long size = new FileInfo(packageZipPath).Length;
                        subPackageInfo[package] = (size, md5);
                    }
                }
            }

            GameVersion gameVersion = new GameVersion(toVersion, assetRemoteAddress, enableHotUpdate, new List<string>(), _enablePackage, packageList, subPackageInfo);
            string gameVersionResourcesPath = Application.dataPath + "/F8Framework/AssetMap/Resources/" + nameof(GameVersion) + ".json";
            string json = Util.LitJson.ToJson(gameVersion);
            FileTools.SafeDeleteFile(gameVersionResourcesPath);
            FileTools.SafeDeleteFile(gameVersionResourcesPath + ".meta");
            UnityEditor.AssetDatabase.Refresh();
            FileTools.CheckFileAndCreateDirWhenNeeded(gameVersionResourcesPath);
            F8JsonEncryption.WriteJsonToFile(gameVersionResourcesPath, json);
            FileTools.CheckDirAndCreateWhenNeeded(buildPath + HotUpdateManager.RemoteDirName);
            FileTools.SafeCopyFile(gameVersionResourcesPath, buildPath + HotUpdateManager.RemoteDirName + "/" + nameof(GameVersion) + ".json");
            LogF8.LogVersion("写入游戏版本： " + gameVersion.Version);
            UnityEditor.AssetDatabase.Refresh();
        }

        private static BuildTargetGroup GetBuildTargetGroup(BuildTarget target)
        {
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            if (targetGroup != BuildTargetGroup.Unknown)
            {
                return targetGroup;
            }
            else
            {
                LogF8.LogError($"Could not find BuildTargetGroup for BuildTarget {target}");
                return default;
            }
        }

        private static string GetScriptPath()
        {
            MonoScript monoScript = MonoScript.FromScriptableObject(CreateInstance<BuildPkgTool>());
            string scriptRelativePath = AssetDatabase.GetAssetPath(monoScript);
            string scriptPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", scriptRelativePath));
            return scriptPath;
        }

        private static string[] GetBuildScenes()
        {
            List<string> names = new List<string>();
            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            {
                if (e != null && e.enabled)
                {
                    names.Add(e.path);
                }
            }
            return names.ToArray();
        }

        public static int StringLen(string str)
        {
            int realLength = 0;
            foreach (char c in str)
            {
                if (c >= 0 && c <= 128)
                    realLength += 1;
                else
                    realLength += 2;
            }
            return realLength;
        }
    }
}
