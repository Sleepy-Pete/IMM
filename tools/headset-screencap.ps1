<#
.SYNOPSIS
    Launch a document at a chapter, wait, and grab the headset's framebuffer as a PNG.

.DESCRIPTION
    Counters say what the renderer BELIEVES it did. This says what is on the panel.
    One eye only - useless for stereo questions, decisive for "is it there at all,
    and if so what colour is it".
#>
param(
    [string]   $DocFile = "TheQuantumRace.imm",
    [string[]] $Flags = @(),
    [int]      $WaitSeconds = 45,
    [int[]]    $ExtraShotsAt = @(),
    [string]   $Label = "shot",
    [string]   $Serial = $env:IMM_HEADSET_SERIAL,
    [string]   $Package = "com.ImmersiveFoundation.IMMUnityTest"
)

$ErrorActionPreference = "Stop"
if (-not $Serial) {
    throw "No headset address. Pass -Serial <ip>:5555, or set IMM_HEADSET_SERIAL once for the session."
}
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
function Adb { & $adb -s $Serial @args }

$stamp  = Get-Date -Format "yyyyMMdd_HHmm"
$outDir = Join-Path $PSScriptRoot "..\..\captures"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

& $adb connect $Serial | Out-Null

$flagLines = @("IMM_UNITY_DOC_FILE=$DocFile") + $Flags
$flagFile  = "/sdcard/Android/data/$Package/files/imm_debug_flags.txt"
$tmp = Join-Path $env:TEMP "imm_debug_flags.txt"
Set-Content -Path $tmp -Value ($flagLines -join "`n") -Encoding ascii -NoNewline
Adb push $tmp $flagFile | Out-Null
Write-Host "flags:"; Adb shell "cat $flagFile" | ForEach-Object { Write-Host "  $_" }

Adb shell "am force-stop $Package" | Out-Null
Adb logcat -c 2>$null | Out-Null
$logPath = Join-Path $outDir "$stamp`_$Label.log"
$logJob = Start-Job -ScriptBlock {
    param($adb,$serial,$path)
    & $adb -s $serial logcat -v time Unity:V ImmUnityPlugin:V ImmRenderReporter:V piLog:V VrApi:V *:S | Out-File -FilePath $path -Encoding utf8
} -ArgumentList $adb,$Serial,$logPath
Start-Sleep -Milliseconds 500

Adb shell "am start -n $Package/com.unity3d.player.UnityPlayerActivity" | Out-Null
$keepJob = Start-Job -ScriptBlock {
    param($adb,$serial,$seconds)
    $deadline=(Get-Date).AddSeconds($seconds+30)
    while((Get-Date) -lt $deadline){ & $adb -s $serial shell "am broadcast -a com.oculus.vrpowermanager.prox_close" 2>$null | Out-Null; Start-Sleep -Seconds 3 }
} -ArgumentList $adb,$Serial,($WaitSeconds + ($ExtraShotsAt | Measure-Object -Maximum).Maximum)

$shotTimes = @($WaitSeconds) + $ExtraShotsAt | Sort-Object -Unique
$prev = 0
foreach ($t in $shotTimes) {
    Start-Sleep -Seconds ($t - $prev); $prev = $t
    $dev = "/sdcard/imm_$Label`_$t.png"
    Adb shell "screencap -p $dev" | Out-Null
    $local = Join-Path $outDir "$stamp`_$Label`_t$t.png"
    Adb pull $dev $local | Out-Null
    Adb shell "rm -f $dev" | Out-Null
    $size = (Get-Item $local -ErrorAction SilentlyContinue).Length
    Write-Host "shot t=${t}s -> $local ($size bytes)"
}

Adb shell "am force-stop $Package" | Out-Null
Start-Sleep -Seconds 2
Stop-Job $logJob,$keepJob -ErrorAction SilentlyContinue
Remove-Job $logJob,$keepJob -Force -ErrorAction SilentlyContinue
Write-Host "log: $logPath"
Get-Content $logPath | Select-String -Pattern "IMM_PICPLACE|IMM_PICUP|IMM_PICCULL" | Select-Object -First 12 | ForEach-Object { Write-Host "  $($_.Line)" }
