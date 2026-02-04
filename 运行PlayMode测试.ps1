# 自动运行：使用 Unity 批处理模式执行 PlayMode 测试
# 用法：在 PowerShell 中执行 .\运行PlayMode测试.ps1 或右键“使用 PowerShell 运行”

$ErrorActionPreference = "Stop"
$ProjectPath = $PSScriptRoot

# 查找 Unity 可执行文件（与 运行一键配置.ps1 保持一致）
$possiblePaths = @(
    "C:\Program Files\Unity\Hub\Editor\2022.3.47f1\Editor\Unity.exe",
    "C:\Program Files\Unity\Hub\Editor\2022.3.47f1\Editor\Unity.exe",
    "C:\Program Files\Unity\Hub\Editor\2022.3.36f1\Editor\Unity.exe",
    "C:\Program Files\Unity\Editor\Unity.exe"
)

$UnityExe = $null
foreach ($p in $possiblePaths) {
    if (Test-Path $p) { $UnityExe = $p; break }
}

if (-not $UnityExe) {
    $hub = "C:\Program Files\Unity\Hub\Editor"
    if (Test-Path $hub) {
        $ver = Get-ChildItem $hub -Directory | Where-Object { $_.Name -match "2022" } | Select-Object -First 1
        if ($ver) {
            $exe = Join-Path $ver.FullName "Editor\Unity.exe"
            if (Test-Path $exe) { $UnityExe = $exe }
        }
    }
}

if (-not $UnityExe) {
    Write-Host "未找到 Unity.exe。请确认 Unity Hub 已安装或手动在 Unity 中运行 Test Runner。" -ForegroundColor Yellow
    exit 1
}

$LogDir = Join-Path $ProjectPath "Logs"
if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }

$TimeStamp = Get-Date -Format "yyyyMMdd-HHmmss"
$LogFile = Join-Path $LogDir ("BatchMode_PlayModeTests_" + $TimeStamp + ".log")
$ResultFile = Join-Path $LogDir ("PlayModeTests_" + $TimeStamp + ".xml")

Write-Host "项目路径: $ProjectPath"
Write-Host "Unity: $UnityExe"
Write-Host "正在执行 PlayMode 测试（批处理模式，无界面）..."
Write-Host "结果: $ResultFile"
Write-Host "日志: $LogFile"
Write-Host ""

& $UnityExe -batchmode -projectPath $ProjectPath -runTests -testPlatform PlayMode -testResults $ResultFile -quit -logFile $LogFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "PlayMode 测试通过 ✅" -ForegroundColor Green
} else {
    Write-Host "PlayMode 测试失败 ❌（请查看日志与测试结果）" -ForegroundColor Red
}

