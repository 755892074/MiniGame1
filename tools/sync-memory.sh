#!/bin/bash
# WorkBuddy 记忆同步脚本 — 将本地工作日志推送到 GitHub
# 用法: bash sync-memory.sh

set -e

PROJECT_DIR="F:/WorkBuddy/H5MiniGame/MiniGame1/MiniGame1_Project"
MEMORY_SRC="$HOME/WorkBuddy/2026-07-06-10-41-14/.workbuddy/memory"
MEMORY_DST="$PROJECT_DIR/doc/memory"

echo "[Sync] 复制记忆文件..."
mkdir -p "$MEMORY_DST"
cp "$MEMORY_SRC"/*.md "$MEMORY_DST/" 2>/dev/null

cd "$PROJECT_DIR"
git pull origin master --no-rebase 2>/dev/null || true
git add doc/memory/
git commit -m "记忆同步 $(date +%m-%d)" 2>/dev/null || echo "  无新变化"
git push origin master
echo "[Sync] 完成"
