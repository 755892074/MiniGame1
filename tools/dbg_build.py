#!/usr/bin/env python3
"""Debug: send the build CS code and print the RAW MCP HTTP response."""
import json, urllib.request, urllib.error, sys

MCP = "http://127.0.0.1:8080/mcp"

CS_CODE = r'''
UnityEngine.Debug.Log("[AutoBuild] ====== 开始抖音小游戏自动构建 (no-dialog, shim-injected) ======");
var sw = System.Diagnostics.Stopwatch.StartNew();
var mgTarget = UnityEditor.BuildTarget.WeixinMiniGame;
var mgGroup = UnityEditor.BuildTargetGroup.WeixinMiniGame;
if (UnityEditor.EditorUserBuildSettings.activeBuildTarget != mgTarget) {
    UnityEngine.Debug.Log("[AutoBuild] 切换平台到 WeixinMiniGame...");
    UnityEditor.EditorUserBuildSettings.SwitchActiveBuildTarget(mgGroup, mgTarget);
}
if (UnityEditor.PlayerSettings.colorSpace != UnityEditor.ColorSpace.Gamma) {
    UnityEngine.Debug.Log("[AutoBuild] ColorSpace -> Gamma");
    UnityEditor.PlayerSettings.colorSpace = UnityEditor.ColorSpace.Gamma;
}
string fullOut = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "doc/douyin_package"));
if (System.IO.Directory.Exists(fullOut)) { UnityEngine.Debug.Log("[AutoBuild] 清理旧输出..."); System.IO.Directory.Delete(fullOut, true); }
var scenes = UnityEditor.EditorBuildSettings.scenes;
if (scenes.Length == 0) { UnityEngine.Debug.LogError("[AutoBuild] 无场景!"); return; }
var abType = (System.Type)null;
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { var t = a.GetType("AutoBuildDouyin"); if (t != null) { abType = t; break; } }
if (abType != null) { var sync = abType.GetMethod("SyncBuildProfileToStark", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); if (sync != null) { UnityEngine.Debug.Log("[AutoBuild] SyncBuildProfileToStark..."); sync.Invoke(null, null); } }
UnityEngine.Debug.Log("[AutoBuild] 调用 TTSDK BuildForTuanjie...");
var bmType = (System.Type)null;
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { var t = a.GetType("TTSDK.Tool.API.BuildManager"); if (t != null) { bmType = t; break; } }
if (bmType == null) { UnityEngine.Debug.LogError("[AutoBuild] BuildManager 类型未找到"); return; }
var method = bmType.GetMethod("BuildForTuanjie", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
if (method == null) { UnityEngine.Debug.LogError("[AutoBuild] BuildForTuanjie 方法未找到"); return; }
object psObj = null;
var psType = (System.Type)null;
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { var t = a.GetType("UnityEditor.PlayerSettings"); if (t != null) { psType = t; break; } }
if (psType != null) { var loadAtPath = typeof(UnityEditor.AssetDatabase).GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(System.Type) }); psObj = loadAtPath != null ? loadAtPath.Invoke(null, new object[] { "ProjectSettings/ProjectSettings.asset", psType }) : null; }
var result = method.Invoke(null, new object[] { fullOut, psObj });
UnityEngine.Debug.Log("[AutoBuild] TTSDK 返回: " + (result == null ? "null" : result.ToString()));
sw.Stop();
UnityEngine.Debug.Log("[AutoBuild] ====== 构建阶段完成 耗时 " + (int)sw.Elapsed.TotalSeconds + "s ======");
if (abType != null) { var inj = abType.GetMethod("InjectLogRelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); if (inj != null) { UnityEngine.Debug.Log("[AutoBuild] 注入日志中继 shim..."); inj.Invoke(null, new object[] { fullOut }); } }
UnityEngine.Debug.Log("[AutoBuild] ====== 全部完成 ======");
'''


def rpc_raw(method, params, req_id=1):
    payload = json.dumps({
        "jsonrpc": "2.0", "id": req_id, "method": method, "params": params
    }).encode("utf-8")
    req = urllib.request.Request(MCP, data=payload, headers={
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream",
    })
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            status = resp.status
            body = resp.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        status = e.code
        body = e.read().decode("utf-8", "replace")
    except Exception as e:
        print("EXCEPTION:", e)
        return
    print("=== HTTP STATUS:", status, "===")
    print("=== BODY (first 3000 chars) ===")
    print(body[:3000])


if __name__ == "__main__":
    rpc_raw("initialize", {
        "protocolVersion": "2024-11-05",
        "capabilities": {},
        "clientInfo": {"name": "dbg_build", "version": "1.0"}
    })
    print("\n\n=== NOW tools/call execute_code ===\n")
    rpc_raw("tools/call", {
        "name": "execute_code",
        "arguments": {"action": "execute", "safety_checks": False, "code": CS_CODE}
    }, req_id=2)
