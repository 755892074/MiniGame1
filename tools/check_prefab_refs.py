#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
检查 Unity/团结工程里所有 .prefab / .unity 对 精灵(sprite)/字体(font) 的引用
是否能在项目的 .meta 文件中找到对应资源。
找不到且不是 Unity 内置资源的，判定为【断链/缺失】。

用法:
    python check_prefab_refs.py [工程Assets绝对路径] [--quiet]
"""
import os
import re
import sys

# Unity 内置资源 guid 白名单（不在项目 .meta 中，但引擎自带，属正常）
# 9c1e3046... 是 UGUI 内置白色方块 sprite（Knob/Background 系列）
BUILTIN_GUIDS = {
    "9c1e3046480fbce4fb79e99295a3b2b7",  # UGUI default white sprite
    "0000000000000000e000000000000000",  # Library/unity default resources
}


def collect_all_guids(assets_root):
    """收集工程内所有 .meta 的 guid -> 相对路径"""
    guids = {}
    for root, _, files in os.walk(assets_root):
        for f in files:
            if f.endswith(".meta"):
                p = os.path.join(root, f)
                try:
                    with open(p, "r", encoding="utf-8", errors="ignore") as fh:
                        m = re.search(r"guid:\s*([A-Za-z0-9+/=]+)", fh.read())
                        if m:
                            rel = os.path.relpath(p[:-5], assets_root)
                            guids[m.group(1)] = rel
                except Exception:
                    pass
    return guids


def scan_targets(assets_root):
    targets = []
    for root, _, files in os.walk(assets_root):
        for f in files:
            if f.lower().endswith((".prefab", ".unity")):
                targets.append(os.path.join(root, f))
    return sorted(targets)


def main():
    assets_root = sys.argv[1] if len(sys.argv) > 1 else r"F:/WorkBuddy/H5MiniGame/MiniGame1/MiniGame1_Project/Assets"
    quiet = "--quiet" in sys.argv

    all_guids = collect_all_guids(assets_root)
    targets = scan_targets(assets_root)

    print(f"资源 guid 总数: {len(all_guids)}")
    print(f"待检查 prefab/unity 数: {len(targets)}")
    print("=" * 70)

    missing = []
    for t in targets:
        rel = os.path.relpath(t, assets_root)
        try:
            with open(t, "r", encoding="utf-8", errors="ignore") as fh:
                content = fh.read()
        except Exception:
            continue
        # 匹配 m_Sprite: { fileID: x, guid: X, type: y }  或 m_Font
        for m in re.finditer(
            r"(m_Sprite|m_Font):\s*\{[^}]*?guid:\s*([A-Za-z0-9+/=]+)", content, re.S
        ):
            field, g = m.group(1), m.group(2)
            if g in BUILTIN_GUIDS:
                continue
            if g not in all_guids:
                # 找到该引用所在行的上下文（前面一个 m_Name 或组件名）
                start = max(0, m.start() - 400)
                ctx = content[start : m.start()]
                name_m = re.findall(r"m_Name:\s*([^\n]+)", ctx)
                ctx_name = name_m[-1].strip() if name_m else "(未知节点)"
                missing.append((rel, field, g, ctx_name))
                if not quiet:
                    print(f"[缺失] {rel}")
                    print(f"       字段={field}  节点≈{ctx_name}  guid={g}")

    print("=" * 70)
    if not missing:
        print("✅ 未发现精灵/字体断链引用")
    else:
        print(f"❌ 共发现 {len(missing)} 处疑似断链引用")
        # 按文件聚合
        by_file = {}
        for rel, field, g, name in missing:
            by_file.setdefault(rel, []).append((field, g, name))
        print("\n--- 按文件汇总 ---")
        for rel, items in by_file.items():
            print(f"\n{rel}  ({len(items)} 处)")
            for field, g, name in items:
                print(f"   - {field} @ {name}  ->  guid={g}")


if __name__ == "__main__":
    main()
