# sync_native_to_package.ps1 - build the Android native plugin and copy it into
# the UPM package, so the package a consumer downloads matches this source tree.
#
# The .so lives INSIDE the package (consumers need no C++ toolchain), which means
# it must be re-synced whenever native code changes. Doing that by hand is how
# source and shipped binary drift apart.
#
#   .\sync_native_to_package.ps1              # build + copy
#   .\sync_native_to_package.ps1 -SkipBuild   # copy an existing build
#   .\sync_native_to_package.ps1 -Verify      # report drift, change nothing

param(
    [switch]$SkipBuild,
    [switch]$Verify
)

$ErrorActionPreference = "Stop"

$repo      = Split-Path $PSScriptRoot -Parent
$buildDir  = Join-Path $repo "code\appImmUnity\android-build"
$built     = Join-Path $buildDir "arm64-v8a\libImmUnityPlugin.so"
$package   = Join-Path $repo "code\ImmUnitySampleProject\Packages\com.immersive-foundation.imm-unity"
$shipped   = Join-Path $package "Plugins\Android\libs\arm64-v8a\libImmUnityPlugin.so"

function Get-Sha([string]$path) {
    if (-not (Test-Path $path)) { return $null }
    return (Get-FileHash $path -Algorithm SHA256).Hash
}

if ($Verify) {
    $a = Get-Sha $built; $b = Get-Sha $shipped
    "built  : $(if ($a) { "$($a.Substring(0,16))  $((Get-Item $built).LastWriteTime)" } else { 'MISSING (not built yet)' })"
    "shipped: $(if ($b) { "$($b.Substring(0,16))  $((Get-Item $shipped).LastWriteTime)" } else { 'MISSING' })"
    if ($a -and $b -and $a -eq $b) { Write-Host "IN SYNC" -ForegroundColor Green }
    elseif ($a -and $b)            { Write-Host "DRIFTED - run without -Verify to re-sync" -ForegroundColor Yellow }
    else                           { Write-Host "cannot compare" -ForegroundColor Yellow }
    return
}

if (-not $SkipBuild) {
    $ninja = "$env:LOCALAPPDATA\Android\Sdk\cmake\3.22.1\bin\ninja.exe"
    if (-not (Test-Path $ninja)) { throw "ninja not found at $ninja" }
    if (-not (Test-Path (Join-Path $buildDir "build.ninja"))) {
        throw "No configured build in $buildDir - run code/appImmUnity/build_android.bat once first"
    }
    Write-Host "Building libImmUnityPlugin.so..." -ForegroundColor Cyan
    & $ninja -C $buildDir ImmUnityPlugin
    if ($LASTEXITCODE -ne 0) { throw "native build failed" }
}

if (-not (Test-Path $built)) { throw "Built .so not found at $built" }

$before = Get-Sha $shipped
Copy-Item $built $shipped -Force
$after = Get-Sha $shipped

Write-Host ("Synced {0:N0} bytes -> {1}" -f (Get-Item $shipped).Length, `
            $shipped.Replace($repo, "")) -ForegroundColor Green
if ($before -eq $after) {
    Write-Host "(binary unchanged - nothing new to commit)" -ForegroundColor DarkGray
} else {
    Write-Host "Binary CHANGED - commit the package, and bump its version if the API moved." -ForegroundColor Yellow
}
