# 自动运行第二步：使用 Unity 批处理模式执行一键配置
# 用法：在 PowerShell 中执行 .\运行一键配置.ps1 或右键“使用 PowerShell 运行”

$ErrorActionPreference = "Stop"
$ProjectPath = $PSScriptRoot

# 查找 Unity 可执行文件
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
    Write-Host "未找到 Unity。请手动在 Unity 中打开本项目，点击菜单：WarcraftReturn -> 一键配置工程与场景" -ForegroundColor Yellow
    exit 1
}

$LogDir = Join-Path $ProjectPath "Logs"
if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }
$LogFile = Join-Path $LogDir "SetupProject.log"

Write-Host "项目路径: $ProjectPath"
Write-Host "Unity: $UnityExe"
Write-Host "正在执行一键配置（批处理模式，无界面）..."
Write-Host ""

& $UnityExe -batchmode -projectPath $ProjectPath -executeMethod SetupWarcraftReturnProject.Execute -quit -logFile $LogFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "一键配置已完成。日志: $LogFile" -ForegroundColor Green
} else {
    Write-Host "请查看日志或直接在 Unity 中打开项目并点击菜单执行: $LogFile" -ForegroundColor Yellow
}
