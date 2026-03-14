$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "Checking if app is running..." -ForegroundColor Yellow
$appPid = & $adb -s $device shell "pidof com.DefaultCompany.IMMUnityTest"
$appPid = $appPid.Trim()

if ($appPid) {
    Write-Host "App is running with PID: $appPid" -ForegroundColor Green
    Write-Host ""
    Write-Host "Sending SIGQUIT to generate thread dump..." -ForegroundColor Yellow
    & $adb -s $device shell "kill -3 $appPid"
    
    Write-Host "Waiting 2 seconds for dump to be written..." -ForegroundColor Yellow
    Start-Sleep -Seconds 2
    
    Write-Host "Capturing logs..." -ForegroundColor Yellow
    & $adb -s $device logcat -d > "thread_dump.txt"
    
    Write-Host ""
    Write-Host "Checking for thread dump..." -ForegroundColor Cyan
    $threadDump = Get-Content "thread_dump.txt" | Select-String "$appPid.*Thread"
    if ($threadDump) {
        Write-Host "Found thread information:" -ForegroundColor Green
        $threadDump | Select-Object -First 20 | ForEach-Object { Write-Host $_ }
    } else {
        Write-Host "No thread dump found" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "Full dump saved to: thread_dump.txt" -ForegroundColor Cyan
} else {
    Write-Host "App is not running!" -ForegroundColor Red
}

