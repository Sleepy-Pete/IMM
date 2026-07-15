$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "Searching for errors..."
$errors = & $adb -s $device logcat -d | Select-String "DllNotFound|UnsatisfiedLinkError|libImmUnityPlugin|AndroidJNI|JNI ERROR"

if ($errors) {
    Write-Host "Found $($errors.Count) error-related log lines:"
    Write-Host "================================"
    $errors | ForEach-Object { Write-Host $_ }
} else {
    Write-Host "No DLL/JNI errors found"
}

