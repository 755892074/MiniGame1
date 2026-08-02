#!/usr/bin/env python3
"""Generate the execute_code JSON args for the Douyin build and write to build_args.json.
Mirrors AutoBuildDouyin.DoBuild() but without blocking dialogs, and injects the
log-relay shim via reflection. Session handling is left to the working mcp_client.py.
"""
import json

CS_CODE = r'''
UnityEngine.Debug.Log("[AutoBuild] ====== 开始抖音小游戏自动构建 (no-dialog, 无内联 shim) ======");
var sw = System.Diagnostics.Stopwatch.StartNew();
var mgTarget = UnityEditor.BuildTarget.WeixinMiniGame;
var mgGroup = UnityEditor.BuildTargetGroup.WeixinMiniGame;
// 不在本脚本内切换平台：若当前 != WeixinMiniGame，SwitchActiveBuildTarget 会触发域重载，
// 直接打断本次 execute_code 调用（构建在 BuildForTuanjie 之前就死掉，且无任何报错）。
// 前置步骤已确保为 WeixinMiniGame；若不符则明确中止，避免静默失败。
if (UnityEditor.EditorUserBuildSettings.activeBuildTarget != mgTarget) {
    UnityEngine.Debug.LogError("[AutoBuild] 当前平台非 WeixinMiniGame（" + UnityEditor.EditorUserBuildSettings.activeBuildTarget + "），请先在编辑器内切换，再构建。跳过本次。");
    return "BUILD_ABORT_WRONG_TARGET";
}
if (UnityEditor.PlayerSettings.colorSpace != UnityEngine.ColorSpace.Gamma) {
    UnityEngine.Debug.Log("[AutoBuild] ColorSpace -> Gamma");
    UnityEditor.PlayerSettings.colorSpace = UnityEngine.ColorSpace.Gamma;
}
string fullOut = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "doc/douyin_package"));
if (System.IO.Directory.Exists(fullOut)) { UnityEngine.Debug.Log("[AutoBuild] 清理旧输出..."); System.IO.Directory.Delete(fullOut, true); }
UnityEngine.Debug.Log("[AutoBuild] 旧输出已清理");
var scenes = UnityEditor.EditorBuildSettings.scenes;
UnityEngine.Debug.Log("[AutoBuild] BuildSettings 场景数 = " + scenes.Length);
if (scenes.Length == 0) { UnityEngine.Debug.LogError("[AutoBuild] 无场景!"); return "BUILD_ABORT_NO_SCENES"; }
var abType = (System.Type)null;
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { var t = a.GetType("AutoBuildDouyin"); if (t != null) { abType = t; break; } }
UnityEngine.Debug.Log("[AutoBuild] AutoBuildDouyin 类型 " + (abType != null ? "已找到" : "未找到"));
if (abType != null) { var sync = abType.GetMethod("SyncBuildProfileToStark", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); if (sync != null) { try { UnityEngine.Debug.Log("[AutoBuild] SyncBuildProfileToStark..."); sync.Invoke(null, null); } catch (System.Exception ex) { UnityEngine.Debug.LogWarning("[AutoBuild] SyncBuildProfileToStark 异常(已忽略): " + ex.Message); } } }
UnityEngine.Debug.Log("[AutoBuild] 调用 TTSDK BuildForTuanjie...");
var bmType = (System.Type)null;
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { var t = a.GetType("TTSDK.Tool.API.BuildManager"); if (t != null) { bmType = t; break; } }
if (bmType == null) { UnityEngine.Debug.LogError("[AutoBuild] BuildManager 类型未找到"); return "BUILD_ABORT_NO_BUILDMGR"; }
var method = bmType.GetMethod("BuildForTuanjie", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
if (method == null) { UnityEngine.Debug.LogError("[AutoBuild] BuildForTuanjie 方法未找到"); return "BUILD_ABORT_NO_METHOD"; }
object psObj = null;
var psType = (System.Type)null;
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { var t = a.GetType("UnityEditor.PlayerSettings"); if (t != null) { psType = t; break; } }
if (psType != null) { var loadAtPath = typeof(UnityEditor.AssetDatabase).GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(System.Type) }); psObj = loadAtPath != null ? loadAtPath.Invoke(null, new object[] { "ProjectSettings/ProjectSettings.asset", psType }) : null; }
var result = method.Invoke(null, new object[] { fullOut, psObj });
UnityEngine.Debug.Log("[AutoBuild] TTSDK 返回: " + (result == null ? "null" : result.ToString()));
sw.Stop();
UnityEngine.Debug.Log("[AutoBuild] ====== 构建阶段完成 耗时 " + (int)sw.Elapsed.TotalSeconds + "s ======");
UnityEngine.Debug.Log("[AutoBuild] ====== 全部完成（shim 将单独注入）======");
return "BUILD_OK";
'''

args = {"action": "execute", "safety_checks": False, "code": CS_CODE}
with open("build_args.json", "w", encoding="utf-8") as f:
    json.dump(args, f, ensure_ascii=False)
print("wrote build_args.json, code length =", len(CS_CODE))
