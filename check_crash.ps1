$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CHECKING FOR CRASHES & PROCESS STATE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if app is still running
Write-Host "[1] Checking if app is still running..." -ForegroundColor Yellow
$currentPid = & $adb -s $device shell "pidof com.DefaultCompany.IMMUnityTest"
$currentPid = $currentPid.Trim()

if ($currentPid) {
    Write-Host "    App IS running with PID: $currentPid" -ForegroundColor Green
} else {
    Write-Host "    App is NOT running (crashed or stopped)" -ForegroundColor Red
}
Write-Host ""

# Check for crashes in startup log
Write-Host "[2] Checking for crash logs..." -ForegroundColor Yellow
$crashes = Get-Content 'startup_log.txt' | Select-String "32586.*(crash|FATAL|signal|segfault|SIGSEGV|tombstone)"
if ($crashes) {
    Write-Host "    FOUND CRASH LOGS:" -ForegroundColor Red
    $crashes | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
} else {
    Write-Host "    No crash logs found" -ForegroundColor Green
}
Write-Host ""

# Check for native library loading
Write-Host "[3] Checking for native library loading..." -ForegroundColor Yellow
$libLoading = Get-Content 'startup_log.txt' | Select-String "32586.*(\.so|dlopen|loading library)"
if ($libLoading) {
    Write-Host "    Found library loading logs:" -ForegroundColor Green
    $libLoading | ForEach-Object { Write-Host "    $_" }
} else {
    Write-Host "    NO library loading logs found!" -ForegroundColor Red
    Write-Host "    This means Unity's native libraries never loaded!" -ForegroundColor Red
}
Write-Host ""

# Check for Unity activity lifecycle
Write-Host "[4] Checking for Unity Activity lifecycle..." -ForegroundColor Yellow
$activity = Get-Content 'startup_log.txt' | Select-String "UnityPlayerActivity.*(onCreate|onStart|onResume)"
if ($activity) {
    Write-Host "    Found activity lifecycle:" -ForegroundColor Green
    $activity | ForEach-Object { Write-Host "    $_" }
} else {
    Write-Host "    NO activity lifecycle logs!" -ForegroundColor Red
}
Write-Host ""

# Check for ANR (Application Not Responding)
Write-Host "[5] Checking for ANR..." -ForegroundColor Yellow
$anr = Get-Content 'startup_log.txt' | Select-String "32586.*ANR"
if ($anr) {
    Write-Host "    FOUND ANR:" -ForegroundColor Red
    $anr | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
} else {
    Write-Host "    No ANR found" -ForegroundColor Green
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DIAGNOSIS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not $libLoading) {
    Write-Host "PROBLEM IDENTIFIED:" -ForegroundColor Red
    Write-Host "Unity's native libraries (libunity.so, libil2cpp.so) are NOT being loaded!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Possible causes:" -ForegroundColor Yellow
    Write-Host "  1. Missing native libraries in APK" -ForegroundColor Yellow
    Write-Host "  2. Wrong architecture (arm64-v8a vs armeabi-v7a)" -ForegroundColor Yellow
    Write-Host "  3. Unity build configuration issue" -ForegroundColor Yellow
    Write-Host "  4. AndroidManifest.xml issue" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Next step: Check APK contents to verify libraries exist" -ForegroundColor Cyan
}

