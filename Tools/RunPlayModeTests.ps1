param(
    [Parameter(Mandatory = $true)]
    [string]$UnityPath,

    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$ResultsPath,

    [Parameter(Mandatory = $true)]
    [string]$RawLogPath,

    [Parameter(Mandatory = $true)]
    [string]$CleanLogPath,

    [switch]$NoGraphics = $true
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $UnityPath)) { throw "UnityPath not found: $UnityPath" }
if (!(Test-Path $ProjectPath)) { throw "ProjectPath not found: $ProjectPath" }

$resultsDir = Split-Path -Parent $ResultsPath
if ($resultsDir) { New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null }

$rawLogDir = Split-Path -Parent $RawLogPath
if ($rawLogDir) { New-Item -ItemType Directory -Force -Path $rawLogDir | Out-Null }

$cleanLogDir = Split-Path -Parent $CleanLogPath
if ($cleanLogDir) { New-Item -ItemType Directory -Force -Path $cleanLogDir | Out-Null }

$args = @(
    "-batchmode"
)

if ($NoGraphics) { $args += "-nographics" }

$args += @(
    "-projectPath", $ProjectPath,
    "-executeMethod", "CommandLineTestRunner.RunPlayMode",
    "-testResults", $ResultsPath,
    "-logFile", $RawLogPath
)

Write-Host "[RunPlayModeTests] UnityPath=$UnityPath"
Write-Host "[RunPlayModeTests] ProjectPath=$ProjectPath"
Write-Host "[RunPlayModeTests] ResultsPath=$ResultsPath"
Write-Host "[RunPlayModeTests] RawLogPath=$RawLogPath"
Write-Host "[RunPlayModeTests] CleanLogPath=$CleanLogPath"

$p = Start-Process -FilePath $UnityPath -ArgumentList $args -Wait -PassThru
$exitCode = $p.ExitCode

if (!(Test-Path $ResultsPath))
{
    Write-Error "[RunPlayModeTests] Missing test results xml: $ResultsPath"
    exit 2
}

if (!(Test-Path $RawLogPath))
{
    Write-Error "[RunPlayModeTests] Missing raw log file: $RawLogPath"
    exit 3
}

$suppressed = @(
    "GfxDevice renderer is null. Unity cannot update the Ambient Probe and Reflection Probes",
    "Shader Hidden/ProbeVolume/VoxelizeScene is not supported: GPU does not support conservative rasterization"
)

# 过滤：只移除已知无害噪音行，保留其余日志（含中文日志）
$lines = Get-Content -Path $RawLogPath -ErrorAction Stop
$filtered = New-Object System.Collections.Generic.List[string]

foreach ($line in $lines)
{
    $skip = $false
    foreach ($s in $suppressed)
    {
        if ($line -like "*$s*") { $skip = $true; break }
    }
    if (!$skip) { [void]$filtered.Add($line) }
}

Set-Content -Path $CleanLogPath -Value $filtered -Encoding UTF8

try
{
    $markerStarted = $ResultsPath + ".started"
    $markerFinished = $ResultsPath + ".finished"
    if (Test-Path $markerStarted) { Remove-Item -Force $markerStarted -ErrorAction SilentlyContinue }
    if (Test-Path $markerFinished) { Remove-Item -Force $markerFinished -ErrorAction SilentlyContinue }
}
catch { }

Write-Host "[RunPlayModeTests] Done. Unity exitCode=$exitCode"
exit $exitCode

