# build_unitypackage.ps1 - produce the self-contained IMM .unitypackage release
# artifact by driving Unity in batch mode.
#
# Output: code/ImmUnitySampleProject/dist/IMM-Unity-<version>.unitypackage
# (dist/ is gitignored - a release artifact should not become another large
#  blob in this repo's history; attach it to a GitHub Release instead.)
#
#   .\build_unitypackage.ps1
#   .\build_unitypackage.ps1 -SyncNative        # rebuild + sync the .so first
#
# NOTE: Unity cannot open a project that is already open in the Editor. Close
# the Editor first, or - with it open - just use the menu item
# "IMM > Export Unity Package (release artifact)", which does the same thing.

param(
    [switch]$SyncNative,
    [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"

$repo    = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo "code\ImmUnitySampleProject"
$dist    = Join-Path $project "dist"
$logFile = Join-Path $env:TEMP "imm_unitypackage_build.log"

if ($SyncNative) {
    & (Join-Path $PSScriptRoot "sync_native_to_package.ps1")
}

if (-not (Test-Path $UnityExe)) { throw "Unity not found at $UnityExe (pass -UnityExe)" }

Write-Host "Exporting .unitypackage (batch mode; log: $logFile)..." -ForegroundColor Cyan

& $UnityExe -batchmode -quit -nographics `
    -projectPath $project `
    -executeMethod ImmPlayer.Editor.ImmPackageExporter.ExportBatch `
    -logFile $logFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "Unity exited $LASTEXITCODE - tail of log:" -ForegroundColor Red
    Get-Content $logFile -Tail 25 | ForEach-Object { $_ }
    throw "unitypackage export failed"
}

$artifact = Get-ChildItem $dist -Filter "IMM-Unity-*.unitypackage" |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $artifact) { throw "No artifact produced in $dist" }

Write-Host ("OK  {0}  ({1:N1} MB)" -f $artifact.Name, ($artifact.Length / 1MB)) -ForegroundColor Green
Write-Host "Attach this file to a GitHub Release (see PACKAGING.md)." -ForegroundColor DarkGray
