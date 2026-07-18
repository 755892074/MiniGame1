# -*- coding: utf-8 -*-
"""
环境常驻一键启动 / 守护（手机端 AI 无人值守开发的基座）

依赖链：
  Tuanjie 编辑器(打开工程) --内部 bridge 包--> 监听 TCP 62767
  codely serve unity-mcp --http --http-port 8765 --> 对外 MCP，内部连 62767
  MCP 绑定 0.0.0.0 => 同一局域网内手机可直连  http://<本机IP>:8765/mcp

特性：幂等（已在跑就跳过）、按需拉起、健康检查、超时不死、输出局域网访问地址。
跨机器：路径优先从脚本位置推算 + 环境变量覆盖 + 自动搜索候选路径，台式机 clone 后零配置。

用法：
  python ensure_env.py                  # 确保环境就绪
  python ensure_env.py --restart-mcp    # 仅重启 MCP 服务（换端口/卡死时）
  python ensure_env.py --project X --tuanjie Y --codely Z   # 显式指定路径
"""
import subprocess, socket, time, json, re, os, sys, glob, argparse

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
# 脚本位于 <工程>/tools/automation/ensure_env.py -> 工程根 = 上两级
DEFAULT_PROJECT = os.path.dirname(os.path.dirname(SCRIPT_DIR))
MCP_PORT = 8765
BRIDGE_PORT = 62767
LOG_DIR = os.path.join(SCRIPT_DIR, "logs")
os.makedirs(LOG_DIR, exist_ok=True)

DETACHED = 0x00000008 | 0x00000200  # DETACHED_PROCESS | CREATE_NEW_PROCESS_GROUP


def find_tuanjie():
    env = os.environ.get("TUANJIE_EXE")
    if env and os.path.exists(env):
        return env
    roots = [r"C:\Program Files\UnityEditors",
             r"D:\Program Files\UnityEditors",
             r"E:\Program Files\UnityEditors",
             os.path.join(os.path.expanduser("~"), "UnityEditors")]
    for r in roots:
        hits = sorted(glob.glob(os.path.join(r, "*", "Editor", "Tuanjie.exe")))
        if hits:
            return hits[-1]  # 取版本最高
    for d in ["D:/", "C:/", "E:/"]:
        hits = glob.glob(d + "**/Tuanjie.exe", recursive=True)
        if hits:
            return hits[0]
    return None


def find_codely():
    env = os.environ.get("CODELY_CLI")
    if env and os.path.exists(env):
        return env
    cand = os.path.join(os.path.expanduser("~"), "AppData", "Local", "Programs",
                        "Tuanjie Cowork", "cli", "bin", "win32-x64", "codely.exe")
    if os.path.exists(cand):
        return cand
    local = glob.glob(os.path.join(DEFAULT_PROJECT, ".codely-cli", "**", "codely.exe"), recursive=True)
    if local:
        return local[0]
    for d in ["C:/", "D:/", "E:/"]:
        hits = glob.glob(d + "**/codely.exe", recursive=True)
        if hits:
            return hits[0]
    return None


def port_listening(port):
    for fam, addr in [(socket.AF_INET, ("127.0.0.1", port)),
                      (socket.AF_INET6, ("::1", port))]:
        try:
            s = socket.socket(fam, socket.SOCK_STREAM)
            s.settimeout(1)
            s.connect(addr)
            s.close()
            return True
        except Exception:
            pass
    return False


def proc_running(image):
    try:
        r = subprocess.run(["tasklist", "/FI", f"IMAGENAME eq {image}"],
                           capture_output=True, text=True, timeout=15)
        return image.lower() in r.stdout.lower()
    except Exception:
        return False


def lan_ip():
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except Exception:
        return "127.0.0.1"


def mcp_health():
    """对 8765 做一次 MCP initialize，确认真正可用（不只是端口开着）。"""
    body = json.dumps({"jsonrpc": "2.0", "id": 1, "method": "initialize",
                       "params": {"protocolVersion": "2024-11-05", "capabilities": {},
                                  "clientInfo": {"name": "ensure", "version": "1.0"}}})
    try:
        r = subprocess.run(
            ["curl", "-s", "-D-", "--max-time", "8", "-X", "POST",
             f"http://127.0.0.1:{MCP_PORT}/mcp",
             "-H", "Content-Type: application/json",
             "-H", "Accept: application/json, text/event-stream",
             "-d", body],
            capture_output=True, text=True, timeout=15)
        return bool(re.search(r"(?i)mcp-session-id:\s*\S+", r.stdout)) or '"result"' in r.stdout
    except Exception:
        return False


def start_tuanjie(project, tuanjie):
    log = open(os.path.join(LOG_DIR, "tuanjie.log"), "ab")
    subprocess.Popen([tuanjie, "-projectPath", project],
                     creationflags=DETACHED, close_fds=True,
                     stdout=log, stderr=log)


def start_mcp(project, codely):
    log = open(os.path.join(LOG_DIR, "mcp.log"), "ab")
    subprocess.Popen([codely, "serve", "unity-mcp", "--http",
                      "--http-port", str(MCP_PORT),
                      "--unity-project-path", project],
                     creationflags=DETACHED, close_fds=True,
                     stdout=log, stderr=log)


def wait_for(cond, timeout, interval, label):
    t0 = time.time()
    while time.time() - t0 < timeout:
        if cond():
            return True
        print(f"    ...等待 {label} ({int(time.time()-t0)}s/{timeout}s)")
        time.sleep(interval)
    return False


def kill_mcp():
    subprocess.run(["taskkill", "/F", "/IM", "codely.exe"],
                   capture_output=True, text=True)
    time.sleep(2)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--restart-mcp", action="store_true")
    ap.add_argument("--project", default=DEFAULT_PROJECT)
    ap.add_argument("--tuanjie", default=None)
    ap.add_argument("--codely", default=None)
    args = ap.parse_args()

    project = os.path.abspath(args.project)
    tuanjie = args.tuanjie or find_tuanjie()
    codely = args.codely or find_codely()

    print("=" * 46)
    print("  铲屎官疯了 · 环境常驻检查")
    print("=" * 46)
    print(f"  工程    : {project}")
    print(f"  Tuanjie : {tuanjie or '(未找到!)'}")
    print(f"  codely  : {codely or '(未找到!)'}")

    if not tuanjie or not os.path.exists(tuanjie):
        print("\n  ❌ 找不到 Tuanjie 编辑器。请安装团结引擎 2022.3.62t11，或 "
              "设置环境变量 TUANJIE_EXE 指向 Tuanjie.exe。")
        return 1
    if not codely or not os.path.exists(codely):
        print("\n  ❌ 找不到 codely CLI。请安装 Tuanjie Cowork，或 "
              "设置环境变量 CODELY_CLI 指向 codely.exe。")
        return 1

    # ---- 1. Tuanjie 编辑器 + TCP 桥(62767) ----
    print("\n[1/2] Tuanjie 编辑器 + TCP 桥(62767)")
    if port_listening(BRIDGE_PORT):
        print("  ✅ TCP 桥已在监听，编辑器就绪")
    else:
        if proc_running("Tuanjie.exe"):
            print("  ⏳ 编辑器进程在，但桥未起（可能仍在导入/编译），等待中...")
        else:
            print("  ▶ 编辑器未运行，正在启动并打开工程...")
            start_tuanjie(project, tuanjie)
        ok = wait_for(lambda: port_listening(BRIDGE_PORT), 360, 10, "TCP桥")
        if ok:
            print("  ✅ TCP 桥已就绪")
        else:
            print("  ❌ 6分钟内 TCP 桥未就绪（首次导入可能更久）。请稍后重跑本脚本。")

    # ---- 2. codely MCP(8765) ----
    print("\n[2/2] codely MCP 服务(8765)")
    if args.restart_mcp and proc_running("codely.exe"):
        print("  ♻ --restart-mcp：先杀掉现有 codely...")
        kill_mcp()

    if not args.restart_mcp and port_listening(MCP_PORT) and mcp_health():
        print("  ✅ MCP 已在监听且健康")
    else:
        if port_listening(MCP_PORT) and not mcp_health():
            print("  ⚠ 端口开着但健康检查失败，重启 MCP...")
            kill_mcp()
        print("  ▶ 启动 codely serve unity-mcp ...")
        start_mcp(project, codely)
        ok = wait_for(lambda: port_listening(MCP_PORT), 60, 5, "MCP端口")
        if ok and wait_for(mcp_health, 30, 5, "MCP健康"):
            print("  ✅ MCP 已就绪且健康")
        else:
            print("  ❌ MCP 未能就绪，请查看日志：", os.path.join(LOG_DIR, "mcp.log"))

    # ---- 汇总 ----
    ip = lan_ip()
    bridge_ok = port_listening(BRIDGE_PORT)
    mcp_ok = port_listening(MCP_PORT) and mcp_health()
    print("\n" + "=" * 46)
    print("  环境状态汇总")
    print("=" * 46)
    print(f"  TCP 桥(62767)   : {'✅ 就绪' if bridge_ok else '❌ 未就绪'}")
    print(f"  MCP 服务(8765)  : {'✅ 就绪' if mcp_ok else '❌ 未就绪'}")
    print(f"  本机局域网 IP   : {ip}")
    print(f"  手机端 MCP 地址 : http://{ip}:{MCP_PORT}/mcp")
    print(f"  日志目录        : {LOG_DIR}")
    if bridge_ok and mcp_ok:
        print("\n  🎉 全部就绪，可以开始远程开发。")
        return 0
    else:
        print("\n  ⚠ 环境未完全就绪，请按上面提示处理后重跑。")
        return 1


if __name__ == "__main__":
    sys.exit(main())
