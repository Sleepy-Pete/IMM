$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CHECKING BUILT APK MANIFEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get APK path
$apkPath = & $adb -s $device shell "pm path com.DefaultCompany.IMMUnityTest"
$apkPath = $apkPath -replace "package:", ""
$apkPath = $apkPath.Trim()

Write-Host "APK Path: $apkPath" -ForegroundColor Cyan
Write-Host ""

# Dump the AndroidManifest.xml
Write-Host "Extracting AndroidManifest.xml..." -ForegroundColor Yellow
& $adb -s $device shell "aapt dump xmltree '$apkPath' AndroidManifest.xml" > "built_manifest.txt"

Write-Host "Saved to: built_manifest.txt" -ForegroundColor Green
Write-Host ""

# Check for UnityPlayerActivity
Write-Host "Checking for UnityPlayerActivity..." -ForegroundColor Yellow
$unityActivity = Get-Content "built_manifest.txt" | Select-String "UnityPlayerActivity"
if ($unityActivity) {
    Write-Host "  Found UnityPlayerActivity" -ForegroundColor Green
} else {
    Write-Host "  UnityPlayerActivity NOT found!" -ForegroundColor Red
}

# Check for native library loading
Write-Host ""
Write-Host "Checking for native library configuration..." -ForegroundColor Yellow
$nativeLib = Get-Content "built_manifest.txt" | Select-String "extractNativeLibs"
if ($nativeLib) {
    Write-Host "  Found extractNativeLibs setting:" -ForegroundColor Cyan
    $nativeLib | ForEach-Object { Write-Host "    $_" }
} else {
    Write-Host "  No extractNativeLibs setting (default: true)" -ForegroundColor Cyan
}

# Check for VR mode
Write-Host ""
Write-Host "Checking for VR configuration..." -ForegroundColor Yellow
$vrMode = Get-Content "built_manifest.txt" | Select-String "vr|oculus|quest"
if ($vrMode) {
    Write-Host "  Found VR configuration:" -ForegroundColor Green
    $vrMode | Select-Object -First 5 | ForEach-Object { Write-Host "    $_" }
} else {
    Write-Host "  No VR configuration found!" -ForegroundColor Red
}

Write-Host ""
Write-Host "Full manifest saved to: built_manifest.txt" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

