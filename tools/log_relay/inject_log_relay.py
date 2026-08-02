#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
把日志中继 shim 注入到抖音出包产物里（game.js / webgl.framework.js 顶部）。
可独立运行，也可被构建脚本调用。

用法:
    python inject_log_relay.py [package_dir]
    # 默认 package_dir = <项目>/doc/douyin_package
"""
import sys, os

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(os.path.dirname(HERE))
SHIM_PATH = os.path.join(HERE, "shim.js")
MARK = "DOUYIN LOG RELAY SHIM"

def _read(p):
    with open(p, "r", encoding="utf-8", errors="replace") as f:
        return f.read()

def _write(p, s):
    with open(p, "w", encoding="utf-8") as f:
        f.write(s)

def _strip_shim(content):
    end = content.find("/* === END DOUYIN LOG RELAY SHIM === */")
    if end == -1:
        return content
    nl = content.find("\n", end)
    if nl == -1:
        return ""
    return content[nl + 1:]

def inject_file(path, force=False):
    if not os.path.exists(path):
        print("skip (not found):", path)
        return
    content = _read(path)
    if MARK in content:
        if not force:
            print("already injected:", path)
            return
        content = _strip_shim(content)
    shim = _read(SHIM_PATH)
    _write(path, shim + "\n" + content)
    print(("re-injected" if force else "injected") + ":", path)

def patch_project_config(pkg):
    cfg = os.path.join(pkg, "tt-minigame", "project.config.json")
    if not os.path.exists(cfg):
        print("skip config (not found):", cfg)
        return
    try:
        import json
        with open(cfg, "r", encoding="utf-8") as f:
            data = json.load(f)
        if not data.get("urlCheck", True):
            print("config urlCheck already false:", cfg)
            return
        data["urlCheck"] = False
        with open(cfg, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        print("patched config urlCheck:false:", cfg)
    except Exception as e:
        print("patch config failed:", e)

def main():
    force = "--force" in sys.argv
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if args:
        pkg = args[0]
    else:
        pkg = os.path.join(PROJECT_ROOT, "doc", "douyin_package")
    tt_dir = os.path.join(pkg, "tt-minigame")
    if not os.path.isdir(tt_dir):
        print("tt-minigame dir not found under:", pkg)
        sys.exit(1)
    inject_file(os.path.join(tt_dir, "game.js"), force)
    inject_file(os.path.join(tt_dir, "webgl.framework.js"), force)
    patch_project_config(pkg)
    print("done.")

if __name__ == "__main__":
    main()
