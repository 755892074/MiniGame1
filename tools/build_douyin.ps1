# 抖音小游戏一键出包（PowerShell 包装）
# 用法: .\build_douyin.ps1
# 也可通过 Unity batch mode 自动调用

$ErrorActionPreference = "Stop"

$PROJECT = "D:\WorkBuddy_WorkSpace\Projects\MiniGame1"
$TUANJIE = "C:\Program Files\Unity\Hub\Editor\2022.3.62t11\Editor\Tuanjie.exe"
$LOG = "$PROJECT\build_douyin.log"
$TOOL_DIR = "$PROJECT\tools"

Write-Host "===== 抖音小游戏自动出包 =====" -ForegroundColor Cyan

# 1. 清理旧日志
if (Test-Path $LOG) { Remove-Item $LOG }

# 2. Unity Batch Build
Write-Host "[1/3] Unity Batch Build..." -ForegroundColor Yellow
& $TUANJIE -batchmode -quit -nographics `
    -projectPath $PROJECT `
    -executeMethod AutoBuildDouyin.BuildFromCommandLine `
    -logFile $LOG

if ($LASTEXITCODE -ne 0) {
    Write-Host "构建失败，查看日志: $LOG" -ForegroundColor Red
    Get-Content $LOG -Tail 50
    exit 1
}

Write-Host "[2/3] 构建完成" -ForegroundColor Green

# 3. 检查输出
$TT_DIR = "$PROJECT\doc\douyin_package\tt-minigame"
if (-not (Test-Path $TT_DIR)) {
    Write-Host "tt-minigame 目录不存在，构建可能失败" -ForegroundColor Red
    Get-Content $LOG -Tail 30
    exit 1
}

Write-Host "[3/3] 生成二维码..." -ForegroundColor Yellow
Write-Host ""

$files = Get-ChildItem $TT_DIR | ForEach-Object { "  $_" }
Write-Host "=== 构建产物 ===" -ForegroundColor Green
Write-Host $files
Write-Host ""
Write-Host "抖音小游戏工程: $TT_DIR" -ForegroundColor Cyan
Write-Host "用抖音开发者工具打开此目录 → 点「预览」→ 扫码运行" -ForegroundColor Cyan

# 提示
Write-Host ""
Write-Host "完成！" -ForegroundColor Green
