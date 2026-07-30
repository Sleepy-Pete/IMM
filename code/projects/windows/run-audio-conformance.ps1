# Builds and runs the audio conformance tests.
#
# Neither test needs a device, an SDK or a game engine - the spatializer and the
# mixer policy are both pure math - so this runs on any host with MSVC. Any
# backend that does its own mixing (Quest today, Apple next) must agree with
# these numbers.
#
# Usage:  .\run-audio-conformance.ps1
[CmdletBinding()]
param(
    [string]$OutputDir = "$PSScriptRoot\..\..\..\build\tests\audio"
)

$ErrorActionPreference = 'Stop'

$repoCode = Resolve-Path "$PSScriptRoot\..\.."

# name -> source files (first is the one with main())
$suites = [ordered]@{
    'test_piSoundSpatializer' = @(
        (Join-Path $repoCode 'libImmCore\src\libSound\tests\test_piSoundSpatializer.cpp'),
        (Join-Path $repoCode 'libImmCore\src\libSound\piSoundSpatializer.cpp')
    )
    'test_soundMix' = @(
        (Join-Path $repoCode 'libImmPlayer\src\tests\test_soundMix.cpp')
    )
}

foreach ($suite in $suites.GetEnumerator()) {
    foreach ($file in $suite.Value) {
        if (-not (Test-Path $file)) { throw "Missing source file: $file" }
    }
}

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) { throw "vswhere.exe not found - Visual Studio is required" }

$vsPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $vsPath) { throw "No Visual Studio installation with the C++ toolset was found" }

$vcvars = Join-Path $vsPath 'VC\Auxiliary\Build\vcvars64.bat'
if (-not (Test-Path $vcvars)) { throw "vcvars64.bat not found at $vcvars" }

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Go through a batch file rather than `cmd /c "..."`: cmd eats the outer quotes
# when a command both starts and ends with one, which mangles the paths.
$batch = Join-Path $OutputDir 'build-audio-tests.bat'
$lines = @('@echo off', "call `"$vcvars`" >nul 2>nul", 'if errorlevel 1 exit /b 1')

foreach ($suite in $suites.GetEnumerator()) {
    $exe = Join-Path $OutputDir "$($suite.Key).exe"
    $sources = ($suite.Value | ForEach-Object { "`"$_`"" }) -join ' '
    # /EHsc for the standard library, /W4 because this is math nobody re-reads
    # often. The doubled backslash on /Fo matters: MSVC treats \" as an escaped
    # quote, so a path ending in a single backslash swallows the command line.
    $lines += "cl /nologo /EHsc /W4 /O2 /std:c++17 /Fe:`"$exe`" /Fo:`"$OutputDir\\`" $sources"
    $lines += 'if errorlevel 1 exit /b 1'
}

$lines | Set-Content -Path $batch -Encoding ascii

Write-Host "Building audio conformance tests..." -ForegroundColor Cyan
& cmd.exe /c $batch
if ($LASTEXITCODE -ne 0) { throw "Compilation failed with exit code $LASTEXITCODE" }

$failed = 0
foreach ($suite in $suites.GetEnumerator()) {
    Write-Host "`n--- $($suite.Key) ---" -ForegroundColor Cyan
    & (Join-Path $OutputDir "$($suite.Key).exe")
    if ($LASTEXITCODE -ne 0) { $failed++ }
}

if ($failed -eq 0) {
    Write-Host "`nAudio conformance PASSED" -ForegroundColor Green
} else {
    Write-Host "`nAudio conformance FAILED ($failed suite(s))" -ForegroundColor Red
}

exit $failed
