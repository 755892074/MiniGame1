#!/usr/bin/env python3
"""Trigger a Douyin (TTSDK BuildForTuanjie) package build via the MCP endpoint.
Avoids shell JSON-escaping hell by building the payload with json.dumps and
POSTing to the MCP Streamable-HTTP/SSE endpoint.
"""
import json, sys, time, urllib.request, urllib.error

MCP = "http://127.0.0.1:8080/mcp"

CS_CODE = r'''
UnityEngine.Debug.Log("[AutoBuild][VERIFY] start active=" + UnityEditor.EditorUserBuildSettings.activeBuildTarget);
var fullOut = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "doc/douyin_package"));
if (System.IO.Directory.Exists(fullOut)) System.IO.Directory.Delete(fullOut, true);
var psType = typeof(UnityEditor.PlayerSettings);
var loadAtPath = typeof(UnityEditor.AssetDatabase).GetMethod("LoadAssetAtPath", new System.Type[]{ typeof(string), typeof(System.Type) });
object psObj = loadAtPath.Invoke(null, new object[]{ "ProjectSettings/ProjectSettings.asset", psType });
System.Type bmType = null;
foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { var t = a.GetType("TTSDK.Tool.API.BuildManager"); if (t != null) { bmType = t; break; } }
if (bmType == null) { UnityEngine.Debug.Log("[AutoBuild][VERIFY] ERR BuildManager type not found"); return; }
var method = bmType.GetMethod("BuildForTuanjie", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
if (method == null) { UnityEngine.Debug.Log("[AutoBuild][VERIFY] ERR BuildForTuanjie method not found"); return; }
var sw = System.Diagnostics.Stopwatch.StartNew();
var result = method.Invoke(null, new object[]{ fullOut, psObj });
sw.Stop();
UnityEngine.Debug.Log("[AutoBuild][VERIFY] result=" + result + " afterPlatform=" + UnityEditor.EditorUserBuildSettings.activeBuildTarget + "(" + ((int)UnityEditor.EditorUserBuildSettings.activeBuildTarget) + ") elapsedSec=" + (int)sw.Elapsed.TotalSeconds);
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
        with urllib.request.urlopen(req, timeout=30) as resp:
            body = resp.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", "replace")
    # SSE: lines like "data: {...}"
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
    # initialize
    rpc("initialize", {
        "protocolVersion": "2024-11-05",
        "capabilities": {},
        "clientInfo": {"name": "build_douyin", "version": "1.0"}
    })
    args = {"action": "execute", "safety_checks": False, "code": CS_CODE}
    print("[build_douyin] calling execute_code (build may take ~11 min in editor)...")
    res = rpc("tools/call", {
        "name": "execute_code",
        "arguments": args
    }, req_id=2)
    print(json.dumps(res, ensure_ascii=False)[:500])
    print("[build_douyin] request dispatched. Poll editor console for [AutoBuild][VERIFY] result=...")
