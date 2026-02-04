param(
    [string]$TargetDir = "e:\AIProject\MT\WarcraftReturn\Assets\Resources\Environment\Textures",
    [switch]$Force = $false
)

$ErrorActionPreference = "Stop"

function DownloadAndExtractZip($url, $zipName, $destDir)
{
    $zipPath = Join-Path $env:TEMP $zipName
    if ((Test-Path $zipPath) -and (-not $Force)) { Remove-Item -Force $zipPath -ErrorAction SilentlyContinue }

    Write-Host "[CC0] Download: $url"
    Invoke-WebRequest -Uri $url -OutFile $zipPath -UseBasicParsing

    if (!(Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }

    Write-Host "[CC0] Extract: $zipName -> $destDir"
    Expand-Archive -Path $zipPath -DestinationPath $destDir -Force
}

if (!(Test-Path $TargetDir)) { New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null }

# Source: ambientCG (CC0) — https://ambientcg.com/
# We use 1K-JPG to keep import size reasonable.
$assets = @(
    @{ id = "Ground003"; file = "Ground003_1K-JPG.zip"; url = "https://ambientcg.com/get?file=Ground003_1K-JPG.zip" },
    @{ id = "Ground029"; file = "Ground029_1K-JPG.zip"; url = "https://ambientcg.com/get?file=Ground029_1K-JPG.zip" },
    @{ id = "PavingStones142"; file = "PavingStones142_1K-JPG.zip"; url = "https://ambientcg.com/get?file=PavingStones142_1K-JPG.zip" }
)

foreach ($a in $assets)
{
    $dest = Join-Path $TargetDir ($a.id + "_1K")
    if ((Test-Path $dest) -and (-not $Force))
    {
        Write-Host "[CC0] Skip existing: $dest"
        continue
    }
    DownloadAndExtractZip $a.url $a.file $dest
}

# Normalize folder structure: ambientCG usually extracts into a nested folder.
# Flatten one level if needed.
Get-ChildItem -Path $TargetDir -Directory | ForEach-Object {
    $sub = Get-ChildItem -Path $_.FullName -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($sub -and ($sub.Name -like "*_1K-JPG"))
    {
        Write-Host "[CC0] Flatten: $($_.Name) <- $($sub.Name)"
        Get-ChildItem -Path $sub.FullName | ForEach-Object {
            Move-Item -Force -Path $_.FullName -Destination $_.Directory.Parent.FullName
        }
        Remove-Item -Recurse -Force $sub.FullName
    }
}

Write-Host "[CC0] Done. Textures in: $TargetDir"

