$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "Getting PID of running app..."
$pidLine = & $adb -s $device shell "ps | grep IMMUnityTest"
Write-Host "PID line: $pidLine"

if ($pidLine -match "(\d+)") {
    $appPid = $matches[1]
    Write-Host "Found PID: $appPid"
    Write-Host ""
    Write-Host "Getting logs for PID $appPid..."
    & $adb -s $device logcat -d | Select-String " $appPid " | Select-Object -Last 200 | Out-File -FilePath "app_pid_logs.txt" -Encoding UTF8

    Write-Host ""
    Write-Host "Last 100 lines from app:"
    Write-Host "================================"
    Get-Content "app_pid_logs.txt" | Select-Object -Last 100
} else {
    Write-Host "Could not find app PID"
}

