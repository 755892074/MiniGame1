# -*- coding: utf-8 -*-
# ⚠️ 已废弃 — 请使用 unity-auto-verify 技能替代
# 该脚本依赖 8765 HTTP MCP（codely serve unity-mcp），不稳定且截图工具不可用。
# 新的验证流程由 WorkBuddy 直连 MCP（mcp__codely-unity__*）驱动，
# 像素差分模块已独立为 pixel_diff.py。
"""
（废弃）一键验证闭环
"""
import json, re, subprocess, time, os, sys, shutil
from PIL import Image
import numpy as np

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
BASE = "http://127.0.0.1:8765/mcp"
OUT = os.path.join(SCRIPT_DIR, "screenshots")
os.makedirs(OUT, exist_ok=True)
CHANGE = sys.argv[1] if len(sys.argv) > 1 else "（未提供变更说明）"


def mcp(method, params=None, sid=None, timeout=8):
    headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
    if sid:
        headers["Mcp-Session-Id"] = sid
    body = {"jsonrpc": "2.0", "method": method}
    if params is not None:
        body["params"] = params
    if "notification" not in method:
        body["id"] = 1
    args = ["curl", "-s", "-D-", "--max-time", str(timeout), "-X", "POST", BASE]
    for k, v in headers.items():
        args += ["-H", f"{k}: {v}"]
    args += ["-d", json.dumps(body, ensure_ascii=False)]
    r = subprocess.run(args, capture_output=True, text=True, timeout=timeout + 5)
    out = r.stdout
    m = re.search(r"(?i)mcp-session-id:\s*(\S+)", out)
    ns = m.group(1) if m else sid
    parts = out.split("\r\n\r\n")
    if len(parts) < 2:
        parts = out.split("\n\n")
    bt = parts[-1] if len(parts) >= 2 else out
    if not bt.strip():
        return None, ns
    try:
        return json.loads(bt), ns
    except Exception:
        return {"parse_error": bt[:300]}, ns


def result_text(r):
    if not r:
        return ""
    return "".join(c.get("text", "") for c in r.get("result", {}).get("content", []))


_counter = [0]


def save_shot(r):
    txt = result_text(r)
    try:
        d = json.loads(txt) if txt and txt.strip().startswith("{") else {}
    except Exception:
        d = {}
    p = None
    if isinstance(d, dict):
        # 兼容多种返回格式：path 可能在顶层、data 内、或 result 内
        for key in ("path",):
            if key in d:
                p = d[key]
                break
        if not p and isinstance(d.get("data"), dict):
            p = d["data"].get("path")
        if not p and isinstance(d.get("result"), dict):
            p = d["result"].get("path")
    if p and os.path.exists(p):
        dst = f"{OUT}/verify_f{_counter[0]}.png"
        _counter[0] += 1
        shutil.copy(p, dst)
        return dst
    # 调试：失败时打印响应片段
    if txt:
        print(f"  [save_shot] 未找到path, 响应前200字: {txt[:200]}")
    return None


print(f"=== 验证变更: {CHANGE} ===")
data, sid = mcp("initialize", {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "wb", "version": "1.0"}})
print(f"Session: {sid}")
mcp("notifications/initialized", sid=sid, timeout=3)

print("\n[1] Save scene")
mcp("tools/call", {"name": "unity_scene", "arguments": {"action": "ensure_scene_saved"}}, sid=sid, timeout=20)

print("\n[2] Force silent mode (no confirm dialogs)")
mcp("tools/call", {"name": "execute_csharp_script", "arguments": {"script": "UnityEditor.EditorPrefs.SetBool(\"PetGameSilent\", true);"}}, sid=sid, timeout=20)

print("\n[3] Regenerate ALL (v3 UI + v2 game UI + scene)  -- 静默")
r = mcp("tools/call", {"name": "unity_menu", "arguments": {"menu_path": "铲屎官疯了/一键生成全部(v3)", "action": "execute"}}, sid=sid, timeout=240)
print(result_text(r[0])[:400])

print("\n[3.5] Clear console (avoid stale logs)")
mcp("tools/call", {"name": "execute_csharp_script", "arguments": {"script": "var t = System.Type.GetType(\"UnityEditor.LogEntries, UnityEditor\"); if (t != null) { var m = t.GetMethod(\"Clear\", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic); if (m != null) m.Invoke(null, null); }"}}, sid=sid, timeout=20)

print("\n[4] Wait for idle (compile + regen done)")
mcp("tools/call", {"name": "unity_editor", "arguments": {"action": "wait_for_idle"}}, sid=sid, timeout=180)

print("\n[5] Console log (compile errors only)")
r2 = mcp("tools/call", {"name": "unity_console", "arguments": {"action": "get"}}, sid=sid, timeout=25)
raw = result_text(r2[0])
try:
    obj = json.loads(raw)
    entries = obj.get("data", []) if isinstance(obj, dict) else []
except Exception:
    entries = []
cs_errs = []
missing = []
for e in entries:
    msg = e.get("message", "")
    typ = e.get("type", "")
    if typ == "Error" and (re.search(r"error CS\d+", msg, re.I) or re.search(r"\.cs\(\d+,\d+\):\s*error", msg, re.I)):
        cs_errs.append(msg)
    if "missing" in msg.lower():
        missing.append(msg)
print(f"  总日志 {len(entries)} 条 | CS编译错误 {len(cs_errs)} | missing脚本 {len(missing)}")
for e in cs_errs[:12]:
    print("  CS错误:", e[:200])
for e in missing[:12]:
    print("  missing(警告):", e[:160])
compile_ok = len(cs_errs) == 0

frames = []
if compile_ok or missing:
    print("\n[6] Enter Play Mode + burst 4 frames (missing 仅警告，仍验证运行时)")
    mcp("tools/call", {"name": "unity_editor", "arguments": {"action": "play"}}, sid=sid, timeout=30)
    time.sleep(3)
    for i in range(4):
        rr = mcp("tools/call", {"name": "unity_screenshot", "arguments": {"action": "capture_game_view"}}, sid=sid, timeout=30)
        f = save_shot(rr[0])
        if f:
            frames.append(f)
            print(f"  saved {f}")
        time.sleep(1.0)
    mcp("tools/call", {"name": "unity_editor", "arguments": {"action": "stop"}}, sid=sid, timeout=30)

    # 运行时是否新增 missing（区分历史残留 vs 真问题）
    r3 = mcp("tools/call", {"name": "unity_console", "arguments": {"action": "get"}}, sid=sid, timeout=25)
    try:
        obj3 = json.loads(result_text(r3[0]))
        ent3 = obj3.get("data", []) if isinstance(obj3, dict) else []
    except Exception:
        ent3 = []
    rt_missing = [e for e in ent3 if "missing" in e.get("message", "").lower()]
    print(f"\n[6.5] 运行时日志 {len(ent3)} 条，其中 missing {len(rt_missing)} 条")

    if frames:
        print("\n[7] Pixel-diff")
        imgs = [np.asarray(Image.open(f).convert("L")) for f in frames]
        max_diff = 0
        acc = np.zeros_like(imgs[0], dtype=int)
        for i in range(len(imgs) - 1):
            diff = np.abs(imgs[i].astype(int) - imgs[i + 1].astype(int))
            cnt = int((diff > 25).sum())
            max_diff = max(max_diff, cnt)
            acc += (diff > 25).astype(int)
        ys, xs = np.where(acc > 0)
        if len(xs):
            bbox = f"{xs.max()-xs.min()+1}x{ys.max()-ys.min()+1} @ ({xs.min()},{ys.min()})"
        else:
            bbox = "无变化"
        anim_ok = max_diff > 2000
    else:
        print("\n[7] Pixel-diff 跳过（8765 MCP 截图工具未注册或不可用，建议用 codely MCP 直连补拍）")
        max_diff = 0
        anim_ok = False
        bbox = "未捕获"
else:
    print("\n[7] CS 编译错误，跳过 Play 验证")

print("\n========== 结论 ==========")
print(f"变更: {CHANGE}")
print(f"编译: {'✅ 0 CS 错误' if compile_ok else '❌ 有 '+str(len(cs_errs))+' 处 CS 编译错误（见上）'}")
if (compile_ok or missing) and frames:
    print(f"动画: {'✅ 帧间有变化（'+str(max_diff)+' px），动画在播' if anim_ok else '⚠️ 帧间变化过小，动画可能未播，需检查'}")
    print(f"变化区域: {bbox}")
    print(f"运行时missing: {len(rt_missing)} 条 {'（为0=历史残留，已不影响运行）' if len(rt_missing)==0 else '（需排查）'}")
    print(f"截图: {', '.join(frames) if frames else '无'}")
print("==========================")
