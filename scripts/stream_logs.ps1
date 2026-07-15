$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  LIVE LOG STREAM (Press Ctrl+C to stop)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clear logs first
Write-Host "Clearing old logs..." -ForegroundColor Yellow
& $adb -s $device logcat -c
Write-Host "Starting live stream..." -ForegroundColor Green
Write-Host ""

# Stream logs with color coding
& $adb -s $device logcat | ForEach-Object {
    $line = $_
    
    # Color code based on content
    if ($line -match "ImmPlayerManager|ImmFeatureExamples|ImmUnityPlugin") {
        Write-Host $line -ForegroundColor Green
    }
    elseif ($line -match "Exception|Error|FATAL") {
        Write-Host $line -ForegroundColor Red
    }
    elseif ($line -match "Unity.*:") {
        Write-Host $line -ForegroundColor Cyan
    }
    elseif ($line -match "DEBUG|Info") {
        Write-Host $line -ForegroundColor Gray
    }
    else {
        Write-Host $line
    }
}

