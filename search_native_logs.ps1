$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "Searching for ImmUnityPlugin logs..."
$nativeLogs = & $adb -s $device logcat -d | Select-String "ImmUnityPlugin"

if ($nativeLogs) {
    Write-Host "Found $($nativeLogs.Count) ImmUnityPlugin log lines:"
    Write-Host "================================"
    $nativeLogs | ForEach-Object { Write-Host $_ }
} else {
    Write-Host "NO ImmUnityPlugin logs found!"
    Write-Host ""
    Write-Host "This means the native plugin Init() function was never called."
    Write-Host "Possible reasons:"
    Write-Host "  1. ImmPlayerManager script is not in the scene"
    Write-Host "  2. ImmPlayerManager.Initialize() is not being called"
    Write-Host "  3. The native plugin library failed to load"
}

