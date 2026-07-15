$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "Clearing old logs..."
& $adb -s $device logcat -c

Write-Host "Launching app..."
& $adb -s $device shell am start -n com.DefaultCompany.IMMUnityTest/com.unity3d.player.UnityPlayerActivity

Write-Host "Waiting 5 seconds for app to start..."
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "Capturing logs..."
& $adb -s $device logcat -d | Out-File -FilePath "quest_launch_logcat.txt" -Encoding UTF8

Write-Host ""
Write-Host "=== Log saved to: quest_launch_logcat.txt ==="
Write-Host ""
Write-Host "IMM-related logs:"
Write-Host "================================"
Get-Content "quest_launch_logcat.txt" | Select-String "ImmUnityPlugin|ImmPlayerManager|ImmFeatureExamples|Unity|SIGSEGV|signal"

