$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "Clearing old logs..."
& $adb -s $device logcat -c

Write-Host "Waiting 2 seconds..."
Start-Sleep -Seconds 2

Write-Host "Capturing fresh logcat..."
& $adb -s $device logcat -d | Out-File -FilePath "quest_fresh_logcat.txt" -Encoding UTF8

Write-Host ""
Write-Host "=== Fresh log saved to: quest_fresh_logcat.txt ==="
Write-Host ""
Write-Host "All IMM-related logs:"
Write-Host "================================"
Get-Content "quest_fresh_logcat.txt" | Select-String "ImmUnityPlugin|ImmPlayerManager|ImmFeatureExamples|Init|SIGSEGV|UnityMain"

