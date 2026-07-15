$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "Getting PID of running app..."
$appPid = & $adb -s $device shell "pidof com.DefaultCompany.IMMUnityTest"
$appPid = $appPid.Trim()
Write-Host "Found PID: $appPid"

if ($appPid) {
    Write-Host ""
    Write-Host "Getting logs for PID $appPid..."
    & $adb -s $device logcat -d | Select-String " $appPid " | Out-File -FilePath "app_pid_logs.txt" -Encoding UTF8
    
    Write-Host ""
    Write-Host "Total lines: $((Get-Content 'app_pid_logs.txt').Count)"
    Write-Host ""
    Write-Host "Last 50 lines from app:"
    Write-Host "================================"
    Get-Content "app_pid_logs.txt" | Select-Object -Last 50
} else {
    Write-Host "Could not find app PID - app may not be running"
}

