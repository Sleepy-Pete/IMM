Write-Host "Extracting all logs from PID 32586..." -ForegroundColor Yellow

Get-Content 'startup_log.txt' | Select-String ' 32586 ' | Out-File 'app_all_logs.txt' -Encoding UTF8

$lineCount = (Get-Content 'app_all_logs.txt' | Measure-Object -Line).Lines
Write-Host "Total lines from PID 32586: $lineCount" -ForegroundColor Green
Write-Host ""

if ($lineCount -eq 0) {
    Write-Host "WARNING: No logs found for PID 32586!" -ForegroundColor Red
    Write-Host "The app process may have died or never fully started." -ForegroundColor Red
} else {
    Write-Host "Displaying all app logs:" -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan
    Get-Content 'app_all_logs.txt'
}

