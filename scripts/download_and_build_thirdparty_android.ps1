# PowerShell script to download and build third-party libraries for Android ARM64
# This script downloads source code and builds all required libraries

$ErrorActionPreference = "Stop"

# Configuration
$NDK_PATH = "C:\Users\$env:USERNAME\AppData\Local\Android\Sdk\ndk\24.0.8215888"
$CMAKE_PATH = "C:\Users\$env:USERNAME\AppData\Local\Android\Sdk\cmake\3.22.1\bin\cmake.exe"
$NINJA_PATH = "C:\Users\$env:USERNAME\AppData\Local\Android\Sdk\cmake\3.22.1\bin\ninja.exe"
$TOOLCHAIN_FILE = "$NDK_PATH\build\cmake\android.toolchain.cmake"
$ANDROID_ABI = "arm64-v8a"
$ANDROID_PLATFORM = "android-26"

$THIRDPARTY_SRC = "thirdparty-src"
$THIRDPARTY_INSTALL = "thirdparty\android\arm64-v8a"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Android ARM64 Third-Party Library Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Create directories
New-Item -ItemType Directory -Force -Path $THIRDPARTY_SRC | Out-Null
New-Item -ItemType Directory -Force -Path $THIRDPARTY_INSTALL | Out-Null

function Build-Library {
    param(
        [string]$Name,
        [string]$Url,
        [string]$ArchiveName,
        [string]$SourceDir,
        [hashtable]$CMakeOptions = @{}
    )
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Building $Name" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    
    $ArchivePath = Join-Path $THIRDPARTY_SRC $ArchiveName
    $ExtractPath = Join-Path $THIRDPARTY_SRC $SourceDir
    $BuildPath = Join-Path $ExtractPath "build-android"
    $InstallPath = Join-Path (Get-Location) $THIRDPARTY_INSTALL
    
    # Download if not exists
    if (-not (Test-Path $ArchivePath)) {
        Write-Host "Downloading $Name..." -ForegroundColor Yellow
        Invoke-WebRequest -Uri $Url -OutFile $ArchivePath
    } else {
        Write-Host "$Name already downloaded" -ForegroundColor Gray
    }
    
    # Extract if not exists
    if (-not (Test-Path $ExtractPath)) {
        Write-Host "Extracting $Name..." -ForegroundColor Yellow
        if ($ArchiveName -match "\.zip$") {
            Expand-Archive -Path $ArchivePath -DestinationPath $THIRDPARTY_SRC
        } else {
            # Use tar for .tar.gz files
            tar -xzf $ArchivePath -C $THIRDPARTY_SRC
        }
    } else {
        Write-Host "$Name already extracted" -ForegroundColor Gray
    }
    
    # Build
    Write-Host "Configuring $Name..." -ForegroundColor Yellow
    
    # Clean build directory
    if (Test-Path $BuildPath) {
        Remove-Item -Recurse -Force $BuildPath
    }
    New-Item -ItemType Directory -Force -Path $BuildPath | Out-Null
    
    # Build CMake options string
    $CMakeArgs = @(
        "-G", "Ninja",
        "-S", $ExtractPath,
        "-B", $BuildPath,
        "-DCMAKE_TOOLCHAIN_FILE=$TOOLCHAIN_FILE",
        "-DANDROID_ABI=$ANDROID_ABI",
        "-DANDROID_PLATFORM=$ANDROID_PLATFORM",
        "-DCMAKE_BUILD_TYPE=Release",
        "-DCMAKE_INSTALL_PREFIX=$InstallPath",
        "-DCMAKE_MAKE_PROGRAM=$NINJA_PATH"
    )
    
    # Add custom options
    foreach ($key in $CMakeOptions.Keys) {
        $CMakeArgs += "-D$key=$($CMakeOptions[$key])"
    }
    
    & $CMAKE_PATH @CMakeArgs
    if ($LASTEXITCODE -ne 0) { throw "CMake configuration failed for $Name" }
    
    Write-Host "Building $Name..." -ForegroundColor Yellow
    & $CMAKE_PATH --build $BuildPath --config Release
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $Name" }
    
    Write-Host "Installing $Name..." -ForegroundColor Yellow
    & $CMAKE_PATH --install $BuildPath
    if ($LASTEXITCODE -ne 0) { throw "Install failed for $Name" }
    
    Write-Host "$Name built successfully!" -ForegroundColor Green
}

try {
    # Build zlib first (required by others)
    Build-Library -Name "zlib" `
        -Url "https://github.com/madler/zlib/archive/refs/tags/v1.3.1.tar.gz" `
        -ArchiveName "zlib-1.3.1.tar.gz" `
        -SourceDir "zlib-1.3.1"
    
    # Build libpng (requires zlib)
    Build-Library -Name "libpng" `
        -Url "https://github.com/glennrp/libpng/archive/refs/tags/v1.6.40.tar.gz" `
        -ArchiveName "libpng-1.6.40.tar.gz" `
        -SourceDir "libpng-1.6.40" `
        -CMakeOptions @{
            "ZLIB_ROOT" = (Join-Path (Get-Location) "$THIRDPARTY_INSTALL")
        }
    
    # Build libjpeg-turbo
    Build-Library -Name "libjpeg-turbo" `
        -Url "https://github.com/libjpeg-turbo/libjpeg-turbo/archive/refs/tags/3.0.1.tar.gz" `
        -ArchiveName "libjpeg-turbo-3.0.1.tar.gz" `
        -SourceDir "libjpeg-turbo-3.0.1"
    
    # Build libogg
    Build-Library -Name "libogg" `
        -Url "https://github.com/xiph/ogg/archive/refs/tags/v1.3.5.tar.gz" `
        -ArchiveName "libogg-1.3.5.tar.gz" `
        -SourceDir "ogg-1.3.5"
    
    # Build libvorbis (requires libogg)
    $OggInstallPath = Join-Path (Get-Location) "$THIRDPARTY_INSTALL"
    Build-Library -Name "libvorbis" `
        -Url "https://github.com/xiph/vorbis/archive/refs/tags/v1.3.7.tar.gz" `
        -ArchiveName "libvorbis-1.3.7.tar.gz" `
        -SourceDir "vorbis-1.3.7" `
        -CMakeOptions @{
            "OGG_INCLUDE_DIR" = (Join-Path $OggInstallPath "include")
            "OGG_LIBRARY" = (Join-Path $OggInstallPath "lib\libogg.a")
            "CMAKE_PREFIX_PATH" = $OggInstallPath
        }
    
    # Build opus
    Build-Library -Name "opus" `
        -Url "https://github.com/xiph/opus/archive/refs/tags/v1.4.tar.gz" `
        -ArchiveName "opus-1.4.tar.gz" `
        -SourceDir "opus-1.4"
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "All libraries built successfully!" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Libraries installed to: $THIRDPARTY_INSTALL" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next step: Run build_android.bat in code\appImmUnity" -ForegroundColor Yellow
    
} catch {
    Write-Host ""
    Write-Host "ERROR: $_" -ForegroundColor Red
    exit 1
}

