#!/usr/bin/env python3
"""Trigger a Douyin package build via MCP, mirroring AutoBuildDouyin.DoBuild()
but WITHOUT any blocking EditorUtility.DisplayDialog / RevealInFinder calls,
and WITH the log-relay shim injection (InjectLogRelay).

The build runs synchronously inside the Tuanjie editor (~11 min). This script
dispatches the request and returns quickly; monitor the package directory +
Editor.log for completion.
"""
import json, urllib.request, urllib.error

MCP = "http://127.0.0.1:8080/mcp"

CS_CODE = r'''
UnityEngine.Debug.Log("[AutoBuild] ====== 开始抖音小游戏自动构建 (no-dialog, shim-injected) ======");
var sw = System.Diagnostics.Stopwatch.StartNew();

// 1. platform
var mgTarget = UnityEditor.BuildTarget.WeixinMiniGame;
var mgGroup = UnityEditor.BuildTargetGroup.WeixinMiniGame;
if (UnityEditor.EditorUserBuildSettings.activeBuildTarget != mgTarget) {
    UnityEngine.Debug.Log("[AutoBuild] 切换平台到 WeixinMiniGame...");
    UnityEditor.EditorUserBuildSettings.SwitchActiveBuildTarget(mgGroup, mgTarget);
}

// 2. Gamma
if (UnityEditor.PlayerSettings.colorSpace != UnityEditor.ColorSpace.Gamma) {
    UnityEngine.Debug.Log("[AutoBuild] ColorSpace -> Gamma");
    UnityEditor.PlayerSettings.colorSpace = UnityEditor.ColorSpace.Gamma;
}

// 3. clean old output
string fullOut = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "doc/douyin_package"));
if (System.IO.Directory.Exists(fullOut)) { UnityEngine.Debug.Log("[AutoBuild] 清理旧输出..."); System.IO.Directory.Delete(fullOut, true); }

// 4. scenes
var scenes = UnityEditor.EditorBuildSettings.scenes;
if (scenes.Length == 0) { UnityEngine.Debug.LogError("[AutoBuild] Build Settings 中没有场景！"); return; }
var scenePaths = new string[scenes.Length];
for (int i = 0; i < scenes.Length; i++) scenePaths[i] = scenes[i].path;

// helper: find type across assemblies
System.Func<string, System.Type> FindType = (name) => {
    foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) {
        var t = a.GetType(name);
        if (t != null) return t;
    }
    return (System.Type)null;
};

// 5. SyncBuildProfileToStark (if available)
var abType = FindType("AutoBuildDouyin");
if (abType != null) {
    var sync = abType.GetMethod("SyncBuildProfileToStark", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    if (sync != null) { UnityEngine.Debug.Log("[AutoBuild] SyncBuildProfileToStark..."); sync.Invoke(null, null); }
}

// 6. build via TTSDK BuildManager.BuildForTuanjie
UnityEngine.Debug.Log("[AutoBuild] 调用 TTSDK BuildForTuanjie...");
var bmType = FindType("TTSDK.Tool.API.BuildManager");
if (bmType == null) { UnityEngine.Debug.LogError("[AutoBuild] BuildManager 类型未找到"); return; }
var method = bmType.GetMethod("BuildForTuanjie", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
if (method == null) { UnityEngine.Debug.LogError("[AutoBuild] BuildForTuanjie 方法未找到"); return; }

object psObj = null;
var psType = FindType("UnityEditor.PlayerSettings");
if (psType != null) {
    var loadAtPath = typeof(UnityEditor.AssetDatabase).GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(System.Type) });
    psObj = loadAtPath?.Invoke(null, new object[] { "ProjectSettings/ProjectSettings.asset", psType });
}

var result = method.Invoke(null, new object[] { fullOut, psObj });
UnityEngine.Debug.Log("[AutoBuild] TTSDK 返回: " + (result == null ? "null" : result.ToString()));

sw.Stop();
UnityEngine.Debug.Log("[AutoBuild] ====== 构建阶段完成 耗时 " + (int)sw.Elapsed.TotalSeconds + "s ======");

// 7. inject log relay shim
if (abType != null) {
    var inj = abType.GetMethod("InjectLogRelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    if (inj != null) { UnityEngine.Debug.Log("[AutoBuild] 注入日志中继 shim..."); inj.Invoke(null, new object[] { fullOut }); }
}
UnityEngine.Debug.Log("[AutoBuild] ====== 全部完成 ======");
'''


def rpc(method, params, req_id=1):
    payload = json.dumps({
        "jsonrpc": "2.0", "id": req_id, "method": method, "params": params
    }).encode("utf-8")
    req = urllib.request.Request(MCP, data=payload, headers={
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream",
    })
    try:
        with urllib.request.urlopen(req, timeout=90) as resp:
            body = resp.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", "replace")
    except Exception as e:
        # timeout / client disconnect: build still runs in editor
        print("[build_douyin_now] dispatch note:", e)
        return None
    for line in body.splitlines():
        line = line.strip()
        if line.startswith("data:"):
            data = line[5:].strip()
            try:
                return json.loads(data)
            except Exception:
                pass
    return None


if __name__ == "__main__":
    rpc("initialize", {
        "protocolVersion": "2024-11-05",
        "capabilities": {},
        "clientInfo": {"name": "build_douyin_now", "version": "1.0"}
    })
    print("[build_douyin_now] dispatching build (~11 min in editor)...")
    res = rpc("tools/call", {
        "name": "execute_code",
        "arguments": {"action": "execute", "safety_checks": False, "code": CS_CODE}
    }, req_id=2)
    if res is not None:
        print(json.dumps(res, ensure_ascii=False)[:800])
    else:
        print("[build_douyin_now] no immediate response (build running async in editor). Monitor package dir + Editor.log.")
