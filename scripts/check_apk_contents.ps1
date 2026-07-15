$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CHECKING APK CONTENTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Find the APK path on device
Write-Host "[1] Finding APK path on device..." -ForegroundColor Yellow
$apkPath = & $adb -s $device shell "pm path com.DefaultCompany.IMMUnityTest"
$apkPath = $apkPath -replace "package:", ""
$apkPath = $apkPath.Trim()
Write-Host "    APK path: $apkPath" -ForegroundColor Green
Write-Host ""

# List native libraries in APK
Write-Host "[2] Listing native libraries in APK..." -ForegroundColor Yellow
$libs = & $adb -s $device shell "unzip -l '$apkPath' | grep '\.so$'"

if ($libs) {
    Write-Host "    Found native libraries:" -ForegroundColor Green
    $libs | ForEach-Object { 
        if ($_ -match "lib.*\.so") {
            Write-Host "    $_" -ForegroundColor Cyan
        }
    }
    
    # Check for Unity-specific libraries
    Write-Host ""
    Write-Host "[3] Checking for Unity libraries..." -ForegroundColor Yellow
    
    $hasUnity = $libs | Select-String "libunity\.so"
    $hasIl2cpp = $libs | Select-String "libil2cpp\.so"
    $hasMain = $libs | Select-String "libmain\.so"
    $hasImmPlugin = $libs | Select-String "libImmUnityPlugin\.so"
    
    if ($hasUnity) {
        Write-Host "    ✓ libunity.so found" -ForegroundColor Green
    } else {
        Write-Host "    ✗ libunity.so MISSING!" -ForegroundColor Red
    }
    
    if ($hasIl2cpp) {
        Write-Host "    ✓ libil2cpp.so found" -ForegroundColor Green
    } else {
        Write-Host "    ✗ libil2cpp.so MISSING!" -ForegroundColor Red
    }
    
    if ($hasMain) {
        Write-Host "    ✓ libmain.so found" -ForegroundColor Green
    } else {
        Write-Host "    ✗ libmain.so MISSING!" -ForegroundColor Red
    }
    
    if ($hasImmPlugin) {
        Write-Host "    ✓ libImmUnityPlugin.so found" -ForegroundColor Green
    } else {
        Write-Host "    ✗ libImmUnityPlugin.so MISSING!" -ForegroundColor Red
    }
    
    # Check architecture
    Write-Host ""
    Write-Host "[4] Checking architecture..." -ForegroundColor Yellow
    $arm64 = $libs | Select-String "arm64-v8a"
    $armv7 = $libs | Select-String "armeabi-v7a"
    
    if ($arm64) {
        Write-Host "    ✓ arm64-v8a libraries found (CORRECT for Quest)" -ForegroundColor Green
    }
    if ($armv7) {
        Write-Host "    ✓ armeabi-v7a libraries found" -ForegroundColor Cyan
    }
    if (-not $arm64 -and -not $armv7) {
        Write-Host "    ✗ No architecture-specific libraries found!" -ForegroundColor Red
    }
    
} else {
    Write-Host "    ERROR: Could not list APK contents!" -ForegroundColor Red
    Write-Host "    The APK may be corrupted or inaccessible." -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

