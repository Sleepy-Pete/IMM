$adb = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$device = "2G0YC1ZF98028F"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CHECKING APK ASSETS & DATA FILES" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Find the APK path
$apkPath = & $adb -s $device shell "pm path com.DefaultCompany.IMMUnityTest"
$apkPath = $apkPath -replace "package:", ""
$apkPath = $apkPath.Trim()
Write-Host "APK path: $apkPath" -ForegroundColor Cyan
Write-Host ""

# List all files in APK
Write-Host "Listing APK contents..." -ForegroundColor Yellow
$allFiles = & $adb -s $device shell "unzip -l '$apkPath'"

# Check for Unity data files
Write-Host ""
Write-Host "[1] Checking for Unity data files..." -ForegroundColor Magenta
$dataFiles = $allFiles | Select-String "assets/bin/Data"
if ($dataFiles) {
    Write-Host "  Found Unity data files:" -ForegroundColor Green
    $dataFiles | Select-Object -First 10 | ForEach-Object { Write-Host "    $_" }
} else {
    Write-Host "  NO Unity data files found!" -ForegroundColor Red
    Write-Host "  This means the build is incomplete!" -ForegroundColor Red
}

# Check for scene data
Write-Host ""
Write-Host "[2] Checking for scene data..." -ForegroundColor Magenta
$sceneFiles = $allFiles | Select-String "level|scene|sharedassets"
if ($sceneFiles) {
    Write-Host "  Found scene files:" -ForegroundColor Green
    $sceneFiles | Select-Object -First 10 | ForEach-Object { Write-Host "    $_" }
} else {
    Write-Host "  NO scene files found!" -ForegroundColor Red
}

# Check for IL2CPP metadata
Write-Host ""
Write-Host "[3] Checking for IL2CPP metadata..." -ForegroundColor Magenta
$metadata = $allFiles | Select-String "global-metadata.dat"
if ($metadata) {
    Write-Host "  Found IL2CPP metadata:" -ForegroundColor Green
    $metadata | ForEach-Object { Write-Host "    $_" }
} else {
    Write-Host "  NO IL2CPP metadata found!" -ForegroundColor Red
    Write-Host "  Scripts cannot run without this!" -ForegroundColor Red
}

# Check for boot.config
Write-Host ""
Write-Host "[4] Checking for boot.config..." -ForegroundColor Magenta
$bootConfig = $allFiles | Select-String "boot.config"
if ($bootConfig) {
    Write-Host "  Found boot.config:" -ForegroundColor Green
    $bootConfig | ForEach-Object { Write-Host "    $_" }
} else {
    Write-Host "  NO boot.config found!" -ForegroundColor Red
}

# Check for IMM files
Write-Host ""
Write-Host "[5] Checking for IMM example files..." -ForegroundColor Magenta
$immFiles = $allFiles | Select-String "\.imm"
if ($immFiles) {
    Write-Host "  Found IMM files:" -ForegroundColor Green
    $immFiles | ForEach-Object { Write-Host "    $_" }
} else {
    Write-Host "  No IMM files in APK (may be loaded from external storage)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Full APK listing saved to: apk_contents.txt" -ForegroundColor Cyan
$allFiles | Out-File "apk_contents.txt" -Encoding UTF8
Write-Host "========================================" -ForegroundColor Cyan

