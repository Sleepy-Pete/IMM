$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "Getting Unity logs from running app..."
& $adb -s $device logcat -d | Select-String "Unity" | Select-Object -Last 100 | Out-File -FilePath "unity_logs.txt" -Encoding UTF8

Write-Host ""
Write-Host "Last 50 Unity log lines:"
Write-Host "================================"
Get-Content "unity_logs.txt" | Select-Object -Last 50

