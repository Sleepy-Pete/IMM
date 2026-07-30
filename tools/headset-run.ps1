<#
.SYNOPSIS
    One headset run: precheck -> flags -> arm capture -> launch -> observe -> force-stop -> verdict.

.DESCRIPTION
    The manual sequence that has repeatedly cost us evidence when a step was skipped:
    the log ring wraps in ~50 s under piLog, so capture MUST be streaming before launch;
    a doffed launch without keep-alive broadcasts gets reaped by Android; the flag file
    silently reverts, so it is always read back after writing; and the app is always
    force-stopped when the run ends.

    Nothing here is IMM-specific beyond the package name and the log tags.

.EXAMPLE
    .\headset-run.ps1 -DocFile TheQuantumRace.imm -Seconds 120 -Label qr_audio
    .\headset-run.ps1 -DocFile TheQuantumRace.imm -Flags IMM_AUDIO_NO_SPATIAL=1 -Label qr_nospatial
    .\headset-run.ps1 -Attended -Seconds 600 -Label qr_listen   # launches, then waits for you
#>
[CmdletBinding()]
param(
    [string]   $DocFile = "",
    [string[]] $Flags = @(),
    [int]      $Seconds = 90,
    [string]   $Label = "run",
    [string]   $Serial = "<HEADSET_IP>:5555",
    [string]   $Package = "com.ImmersiveFoundation.IMMUnityTest",
    [switch]   $Attended,
    [switch]   $NoLaunch
)

$ErrorActionPreference = "Stop"
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
if (-not (Test-Path $adb)) { throw "adb not found at $adb" }

$stamp   = Get-Date -Format "yyyyMMdd_HHmm"
$outDir  = Join-Path $PSScriptRoot "..\..\captures"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$logPath = Join-Path $outDir "$stamp`_$Label.log"

function Adb { & $adb -s $Serial @args }

# ---------------------------------------------------------------- 1. PRECHECK
Write-Host "== PRECHECK" -ForegroundColor Cyan
& $adb connect $Serial | Out-Null
$state = (Adb get-state) 2>$null
if ($state -ne "device") { throw "headset not reachable ($Serial): state=$state. Power it on / re-arm WiFi adb." }

$battery = (Adb shell "dumpsys battery | grep level").Trim()
$wake    = (Adb shell "dumpsys power | grep 'mWakefulness='").Trim()
Write-Host "  $battery / $wake"
if ($wake -notmatch "Awake") { Write-Warning "headset is not awake - it may sleep mid-run" }

# The installed APK, not the one on disk: a build that silently dropped a document
# cost us a whole session reading a genuine 404 as a mystery.
$apk = (Adb shell "pm path $Package").Trim().Replace("package:", "")
Write-Host "  installed docs in $([System.IO.Path]::GetFileName($apk)):"
Adb shell "unzip -l $apk 2>/dev/null | grep -i '\.imm'" | ForEach-Object { Write-Host "    $_" }

# ------------------------------------------------------------------- 2. FLAGS
$flagLines = @()
if ($DocFile) { $flagLines += "IMM_UNITY_DOC_FILE=$DocFile" }
$flagLines += $Flags
$flagFile = "/sdcard/Android/data/$Package/files/imm_debug_flags.txt"

Write-Host "== FLAGS" -ForegroundColor Cyan
if ($flagLines.Count -gt 0) {
    $tmp = Join-Path $env:TEMP "imm_debug_flags.txt"
    Set-Content -Path $tmp -Value ($flagLines -join "`n") -Encoding ascii -NoNewline
    Adb push $tmp $flagFile | Out-Null
} else {
    Adb shell "rm -f $flagFile" | Out-Null
}
# Always read back. A silent typo or revert reads as "the change did nothing".
$readBack = Adb shell "cat $flagFile 2>/dev/null || echo '(empty)'"
$readBack | ForEach-Object { Write-Host "  $_" }

# ------------------------------------------------------------- 3. ARM CAPTURE
Write-Host "== ARM CAPTURE -> $logPath" -ForegroundColor Cyan
Adb shell "am force-stop $Package" | Out-Null
Adb logcat -c 2>$null | Out-Null

$logJob = Start-Job -ScriptBlock {
    param($adb, $serial, $path)
    & $adb -s $serial logcat -v time Unity:V ImmUnityPlugin:V ImmRenderReporter:V piLog:V VrApi:V AndroidRuntime:E DEBUG:V *:S |
        Out-File -FilePath $path -Encoding utf8
} -ArgumentList $adb, $Serial, $logPath
Start-Sleep -Milliseconds 800

# ------------------------------------------------------------------ 4. LAUNCH
if (-not $NoLaunch) {
    Write-Host "== LAUNCH (am start; launcher intents wedge on ClearActivity)" -ForegroundColor Cyan
    Adb shell "am start -n $Package/com.unity3d.player.UnityPlayerActivity" | Out-Null

    # Proximity-close keep-alive: without it a doffed launch is reaped as a cached process.
    $keepJob = Start-Job -ScriptBlock {
        param($adb, $serial, $seconds)
        $deadline = (Get-Date).AddSeconds($seconds + 30)
        while ((Get-Date) -lt $deadline) {
            & $adb -s $serial shell "am broadcast -a com.oculus.vrpowermanager.prox_close" 2>$null | Out-Null
            Start-Sleep -Seconds 3
        }
    } -ArgumentList $adb, $Serial, $Seconds
}

# ----------------------------------------------------------------- 5. OBSERVE
if ($Attended) {
    Write-Host "== ATTENDED: app is running. Press ENTER when you are done in the headset." -ForegroundColor Yellow
    [void](Read-Host)
} else {
    Write-Host "== OBSERVE $Seconds s" -ForegroundColor Cyan
    Start-Sleep -Seconds $Seconds
}

# --------------------------------------------------------- 6. TEARDOWN ALWAYS
Write-Host "== TEARDOWN" -ForegroundColor Cyan
Adb shell "am force-stop $Package" | Out-Null
Start-Sleep -Seconds 2
Stop-Job $logJob -ErrorAction SilentlyContinue; Remove-Job $logJob -Force -ErrorAction SilentlyContinue
if ($keepJob) { Stop-Job $keepJob -ErrorAction SilentlyContinue; Remove-Job $keepJob -Force -ErrorAction SilentlyContinue }

# ----------------------------------------------------------------- 7. VERDICT
Write-Host "== VERDICT" -ForegroundColor Cyan
if (-not (Test-Path $logPath)) { Write-Warning "no log captured"; return }
$log = Get-Content $logPath
Write-Host ("  {0} lines captured" -f $log.Count)

$markers = [ordered]@{
    "doc override"      = 'Document override from flag file'
    "load bytes"        = 'Loaded \d+ bytes from StreamingAssets'
    "load FAILED"       = 'Failed to load from StreamingAssets'
    "[IMM_AUDIO]"       = '\[IMM_AUDIO\]'
    "sound objects"     = 'Add OGG OPUS sound object'
    "[IMM_PICFMT]"      = '\[IMM_PICFMT\]'
    "[IMM_PICCULL]"     = '\[IMM_PICCULL\]'
    "MSAA"              = 'MSAA 4x ACTIVE'
    "FFR"               = 'native FFR ACTIVE'
    "viewpoint driver"  = '\[IMM_VIEWPOINT\]'
    "crash"             = 'FATAL EXCEPTION|signal \d+ \(SIG'
}
foreach ($k in $markers.Keys) {
    $hits = $log | Select-String -Pattern $markers[$k]
    $n = $hits.Count
    $color = if ($n -gt 0) { "Green" } else { "DarkGray" }
    Write-Host ("  {0,-18} {1,4}" -f $k, $n) -ForegroundColor $color
    if ($n -gt 0 -and $k -match 'IMM_AUDIO|PICFMT|PICCULL|FAILED|crash') {
        $hits | Select-Object -First 6 | ForEach-Object { Write-Host "      $($_.Line)" -ForegroundColor Gray }
    }
}

$fps = $log | Select-String -Pattern 'FPS=(\d+)/' -AllMatches |
       ForEach-Object { $_.Matches } | ForEach-Object { [int]$_.Groups[1].Value }
if ($fps.Count -gt 0) {
    $stats = $fps | Measure-Object -Minimum -Maximum -Average
    Write-Host ("  FPS  n={0}  min={1}  avg={2:N1}  max={3}" -f $stats.Count, $stats.Minimum, $stats.Average, $stats.Maximum)
}
Write-Host "  log: $logPath"
