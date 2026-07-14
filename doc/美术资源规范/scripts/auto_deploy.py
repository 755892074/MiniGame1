# -*- coding: utf-8 -*-
"""
疯狂铲屎官 — 自动部署工具
功能：读取 _processed/ 目录和 manifest.json，按命名规则把图片
     复制到 Unity 项目 Assets/ 对应路径，同名则覆盖。

用法：python auto_deploy.py [--project 项目路径] [--dry-run]
"""

import os
import sys
import json
import shutil
from pathlib import Path

# 默认项目路径（自动检测：往上找到含 Assets/ 的目录）
def _find_project_root():
    p = Path(__file__).resolve().parent
    for _ in range(10):
        if (p / "Assets").exists():
            return str(p)
        p = p.parent
    return r"F:\WorkBuddy\H5MiniGame\MiniGame1\MiniGame1_Project"

DEFAULT_PROJECT = _find_project_root()

PROCESSED_DIR = Path(__file__).parent.parent / "_processed"
MANIFEST_PATH = PROCESSED_DIR / "manifest.json"

# 部署规则：类型 → (目标路径模板, 命名模板)
DEPLOY_RULES = {
    "animation": {
        "path": "Assets/Resources/ArtPets/{pet}/{action}_s{stage}/",
        "name": "frame_{frame:02d}.png",
    },
    "house": {
        "path": "Assets/Resources/ArtHouses/",
        "name": "house_lv{level}.png",
    },
    "yard": {
        "path": "Assets/Resources/ArtYard/",
        "name": None,  # 用原始文件名
    },
    "scene": {
        "path": "Assets/Resources/ArtScenes/",
        "name": None,
    },
    "chapter": {
        "path": "Assets/Resources/ArtChapters/",
        "name": None,
    },
    "ui": {
        "path": "Assets/Resources/ArtUI/",
        "name": None,
    },
    "badge": {
        "path": "Assets/Resources/ArtUI/",
        "name": None,
    },
    "unknown": {
        "path": "Assets/Resources/ArtMisc/",
        "name": None,
    },
}


def deploy(manifest, project_path, dry_run=False):
    deployed = 0
    skipped = 0
    overwritten = 0
    errors = 0

    for entry in manifest:
        info = entry["info"]
        source = PROCESSED_DIR.parent / entry["output"]

        if not source.exists():
            # 尝试直接从 output 字段找
            source = Path(entry["output"])
            if not source.exists():
                print(f"  [SKIP] 源文件不存在: {entry['output']}")
                skipped += 1
                continue

        # 确定部署规则
        res_type = info.get("type", "unknown")
        rule = DEPLOY_RULES.get(res_type, DEPLOY_RULES["unknown"])

        # 构造目标路径
        try:
            target_dir_rel = rule["path"].format(**info)
        except KeyError as e:
            # 缺少必要字段，用原始文件名
            target_dir_rel = "Assets/Resources/ArtMisc/"

        target_dir = Path(project_path) / target_dir_rel
        target_dir.mkdir(parents=True, exist_ok=True)

        # 构造目标文件名
        if rule["name"]:
            try:
                target_name = rule["name"].format(**info)
            except KeyError:
                target_name = source.name
        else:
            target_name = source.name

        target_path = target_dir / target_name

        if dry_run:
            action = "覆盖" if target_path.exists() else "新建"
            print(f"  [{action}] {target_path.relative_to(project_path)}")
            if target_path.exists():
                overwritten += 1
            else:
                deployed += 1
            continue

        # 实际复制
        if target_path.exists():
            overwritten += 1
        else:
            deployed += 1

        shutil.copy2(source, target_path)
        print(f"  [OK] → {target_path.relative_to(project_path)}")

    return deployed, overwritten, skipped, errors


def main():
    print("=" * 60)
    print("疯狂铲屎官 — 自动部署工具")
    print("=" * 60)

    # 解析参数
    project_path = DEFAULT_PROJECT
    dry_run = "--dry-run" in sys.argv

    for arg in sys.argv[1:]:
        if not arg.startswith("--") and not arg.startswith("-"):
            project_path = arg

    # 检查 manifest
    if not MANIFEST_PATH.exists():
        print(f"\n[!] 未找到 manifest.json: {MANIFEST_PATH}")
        print("请先运行 batch_process.py 处理图片")
        return

    with open(MANIFEST_PATH, "r", encoding="utf-8") as f:
        manifest = json.load(f)

    print(f"\n项目路径: {project_path}")
    print(f"待部署资源: {len(manifest)} 个")
    print(f"模式: {'预览(不实际复制)' if dry_run else '实际部署'}")
    print()

    if not Path(project_path).exists():
        print(f"[!] 项目路径不存在: {project_path}")
        return

    deployed, overwritten, skipped, errors = deploy(manifest, project_path, dry_run)

    print(f"\n{'='*40}")
    print(f"完成！")
    print(f"  新建部署: {deployed}")
    print(f"  覆盖更新: {overwritten}")
    print(f"  跳过: {skipped}")

    if not dry_run:
        print(f"\n提示：请回到 Unity 编辑器，等待 AssetDatabase 刷新")
        print(f"或手动执行：Assets → Reimport All")


if __name__ == "__main__":
    main()
