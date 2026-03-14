$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  IMM Unity Development Build Monitor" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get app PID
Write-Host "[1/4] Getting app PID..." -ForegroundColor Yellow
$appPid = & $adb -s $device shell "pidof com.DefaultCompany.IMMUnityTest"
$appPid = $appPid.Trim()

if (-not $appPid) {
    Write-Host "ERROR: App is not running!" -ForegroundColor Red
    Write-Host "Please launch the app on Quest and run this script again." -ForegroundColor Red
    exit
}

Write-Host "      Found PID: $appPid" -ForegroundColor Green
Write-Host ""

# Clear old logs
Write-Host "[2/4] Clearing old logcat buffer..." -ForegroundColor Yellow
& $adb -s $device logcat -c
Write-Host "      Cleared" -ForegroundColor Green
Write-Host ""

# Wait for app to initialize
Write-Host "[3/4] Waiting 3 seconds for app to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 3
Write-Host "      Ready" -ForegroundColor Green
Write-Host ""

# Capture logs
Write-Host "[4/4] Capturing logs..." -ForegroundColor Yellow
& $adb -s $device logcat -d > "dev_build_full_log.txt"
Write-Host "      Saved to: dev_build_full_log.txt" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  LOG ANALYSIS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check for Unity logs
Write-Host "--- Unity Debug.Log Output ---" -ForegroundColor Magenta
$unityLogs = Get-Content "dev_build_full_log.txt" | Select-String "Unity.*:" | Select-Object -Last 30
if ($unityLogs) {
    $unityLogs | ForEach-Object { Write-Host $_ }
} else {
    Write-Host "  (No Unity logs found)" -ForegroundColor DarkGray
}
Write-Host ""

# Check for ImmPlayerManager
Write-Host "--- ImmPlayerManager Logs ---" -ForegroundColor Magenta
$immManagerLogs = Get-Content "dev_build_full_log.txt" | Select-String "ImmPlayerManager"
if ($immManagerLogs) {
    $immManagerLogs | ForEach-Object { Write-Host $_ -ForegroundColor Green }
} else {
    Write-Host "  (No ImmPlayerManager logs found)" -ForegroundColor Red
}
Write-Host ""

# Check for ImmFeatureExamples
Write-Host "--- ImmFeatureExamples Logs ---" -ForegroundColor Magenta
$immExamplesLogs = Get-Content "dev_build_full_log.txt" | Select-String "ImmFeatureExamples"
if ($immExamplesLogs) {
    $immExamplesLogs | ForEach-Object { Write-Host $_ -ForegroundColor Green }
} else {
    Write-Host "  (No ImmFeatureExamples logs found)" -ForegroundColor Red
}
Write-Host ""

# Check for native plugin
Write-Host "--- Native Plugin Logs ---" -ForegroundColor Magenta
$nativeLogs = Get-Content "dev_build_full_log.txt" | Select-String "ImmUnityPlugin"
if ($nativeLogs) {
    $nativeLogs | ForEach-Object { Write-Host $_ -ForegroundColor Green }
} else {
    Write-Host "  (No ImmUnityPlugin logs found - Init() not called)" -ForegroundColor Red
}
Write-Host ""

# Check for errors
Write-Host "--- Errors & Exceptions ---" -ForegroundColor Magenta
$errors = Get-Content "dev_build_full_log.txt" | Select-String "Exception|Error|FATAL|DllNotFound" | Select-Object -Last 20
if ($errors) {
    $errors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
} else {
    Write-Host "  (No errors found)" -ForegroundColor Green
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Full log saved to: dev_build_full_log.txt" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

