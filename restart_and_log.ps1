$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"
$package = "com.DefaultCompany.IMMUnityTest"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RESTART APP & CAPTURE STARTUP LOGS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Stop the app
Write-Host "[1/5] Stopping app..." -ForegroundColor Yellow
& $adb -s $device shell "am force-stop $package"
Start-Sleep -Seconds 1
Write-Host "      Stopped" -ForegroundColor Green
Write-Host ""

# Step 2: Clear logcat
Write-Host "[2/5] Clearing logcat buffer..." -ForegroundColor Yellow
& $adb -s $device logcat -c
Write-Host "      Cleared" -ForegroundColor Green
Write-Host ""

# Step 3: Start the app
Write-Host "[3/5] Starting app..." -ForegroundColor Yellow
& $adb -s $device shell "am start -n $package/com.unity3d.player.UnityPlayerActivity"
Write-Host "      Started" -ForegroundColor Green
Write-Host ""

# Step 4: Wait for initialization
Write-Host "[4/5] Waiting 5 seconds for app to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
Write-Host "      Ready" -ForegroundColor Green
Write-Host ""

# Step 5: Capture logs
Write-Host "[5/5] Capturing startup logs..." -ForegroundColor Yellow
& $adb -s $device logcat -d > "startup_log.txt"
$lineCount = (Get-Content "startup_log.txt" | Measure-Object -Line).Lines
Write-Host "      Captured $lineCount lines" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  LOG ANALYSIS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get app PID
$appPid = & $adb -s $device shell "pidof $package"
$appPid = $appPid.Trim()
Write-Host "App PID: $appPid" -ForegroundColor Cyan
Write-Host ""

# Check for Unity startup
Write-Host "--- Unity Startup ---" -ForegroundColor Magenta
$unityStartup = Get-Content "startup_log.txt" | Select-String "Unity|OpenXR" | Select-Object -First 10
if ($unityStartup) {
    $unityStartup | ForEach-Object { Write-Host $_ }
} else {
    Write-Host "  (No Unity startup logs)" -ForegroundColor Red
}
Write-Host ""

# Check for ImmPlayerManager
Write-Host "--- ImmPlayerManager ---" -ForegroundColor Magenta
$immManager = Get-Content "startup_log.txt" | Select-String "ImmPlayerManager"
if ($immManager) {
    $immManager | ForEach-Object { Write-Host $_ -ForegroundColor Green }
} else {
    Write-Host "  (No ImmPlayerManager logs - Awake() not called!)" -ForegroundColor Red
}
Write-Host ""

# Check for ImmFeatureExamples
Write-Host "--- ImmFeatureExamples ---" -ForegroundColor Magenta
$immExamples = Get-Content "startup_log.txt" | Select-String "ImmFeatureExamples"
if ($immExamples) {
    $immExamples | ForEach-Object { Write-Host $_ -ForegroundColor Green }
} else {
    Write-Host "  (No ImmFeatureExamples logs - Awake() not called!)" -ForegroundColor Red
}
Write-Host ""

# Check for native plugin
Write-Host "--- Native Plugin ---" -ForegroundColor Magenta
$native = Get-Content "startup_log.txt" | Select-String "ImmUnityPlugin|UnityPluginLoad"
if ($native) {
    $native | ForEach-Object { Write-Host $_ -ForegroundColor Green }
} else {
    Write-Host "  (No native plugin logs - Init() not called!)" -ForegroundColor Red
}
Write-Host ""

# Check for errors
Write-Host "--- Errors & Exceptions ---" -ForegroundColor Magenta
$errors = Get-Content "startup_log.txt" | Select-String "Exception|Error.*$package|FATAL|crash" | Select-Object -First 20
if ($errors) {
    $errors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
} else {
    Write-Host "  (No errors found)" -ForegroundColor Green
}
Write-Host ""

# Check for Debug.Log from Unity
Write-Host "--- Unity Debug.Log Output ---" -ForegroundColor Magenta
$debugLogs = Get-Content "startup_log.txt" | Select-String "$appPid.*Unity" | Select-Object -First 30
if ($debugLogs) {
    $debugLogs | ForEach-Object { Write-Host $_ -ForegroundColor Cyan }
} else {
    Write-Host "  (No Unity Debug.Log output - scripts may not be running!)" -ForegroundColor Red
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Full log saved to: startup_log.txt" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

