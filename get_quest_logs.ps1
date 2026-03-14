$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "Capturing logcat from Quest device..."
& $adb -s $device logcat -d | Out-File -FilePath "quest_logcat.txt" -Encoding UTF8

Write-Host "Filtering for IMM-related logs..."
Get-Content "quest_logcat.txt" | Select-String "ImmUnityPlugin|ImmPlayerManager|ImmFeatureExamples|Init\(\)|SIGSEGV" | Out-File -FilePath "quest_imm_logs.txt" -Encoding UTF8

Write-Host ""
Write-Host "=== Full log saved to: quest_logcat.txt ==="
Write-Host "=== Filtered log saved to: quest_imm_logs.txt ==="
Write-Host ""
Write-Host "Last 50 IMM-related log lines:"
Write-Host "================================"
Get-Content "quest_imm_logs.txt" | Select-Object -Last 50

