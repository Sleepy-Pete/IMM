# quill_layer_map.ps1 - map a Quill project's layer tree to a timecoded table.
#
# Purpose: turn "the big rock is missing in the crash scene around 4:10" into a
# specific layer name, so it can be matched against the runtime [IMM_NODRAW] /
# [IMM_KEYS] telemetry from the Quest build.
#
# Quill times are piTicks: 12600 ticks/second (libImmCore piTick.cpp), so
# seconds = ticks / 12600.
#
#   .\quill_layer_map.ps1 -Project "F:\...\Masterfile_ABC_v43_O_ms_df"
#   .\quill_layer_map.ps1 -Project <dir> -AtTime "4:10"     # layers live then
#   .\quill_layer_map.ps1 -Project <dir> -Name "rock"       # find by name
#
# Output columns:
#   Path        full layer path (matches the runtime layer names in logcat)
#   Type        Paint / Group / Picture / Sound / Viewpoint / Camera
#   Visible     authored static visibility
#   Opacity     authored static opacity
#   FirstOn     first time a Visibility key turns it ON  (timecode)
#   LastOff     last time a Visibility key turns it OFF  (timecode)
#   VisKeys     number of visibility keys
#   XfKeys      transform keys, with the stepped count in StepXf
#   StepXf      transform keys authored as Interpolation:None (hold+snap)

param(
    [Parameter(Mandatory = $true)][string]$Project,
    [string]$AtTime = "",
    [string]$Name = "",
    [string]$Csv = ""
)

$ErrorActionPreference = "Stop"
$TICKS_PER_SECOND = 12600.0

function Convert-TicksToTimecode([double]$ticks) {
    $s = $ticks / $TICKS_PER_SECOND
    $m = [math]::Floor($s / 60)
    $r = $s - ($m * 60)
    return ("{0}:{1:00.0}" -f $m, $r)
}

function Convert-TimecodeToTicks([string]$tc) {
    if ($tc -match '^(\d+):([\d.]+)$') { return ([double]$Matches[1] * 60 + [double]$Matches[2]) * $TICKS_PER_SECOND }
    if ($tc -match '^[\d.]+$') { return [double]$tc * $TICKS_PER_SECOND }
    throw "Could not parse time '$tc' (use M:SS or seconds)"
}

$jsonPath = Join-Path $Project "Quill.json"
if (-not (Test-Path $jsonPath)) { throw "No Quill.json in $Project" }

Write-Host "Reading $jsonPath ($([math]::Round((Get-Item $jsonPath).Length/1MB,1)) MB)..." -ForegroundColor Cyan
$doc = Get-Content $jsonPath -Raw | ConvertFrom-Json
$seq = $doc.Sequence

Write-Host ("Title '{0}'  Framerate {1}  DefaultViewpoint {2}" -f `
    $seq.Metadata.Title, $seq.Framerate, $seq.DefaultViewpoint) -ForegroundColor Cyan

$rows = New-Object System.Collections.Generic.List[object]

function Read-Layer($layer, [string]$parentPath) {
    if ($null -eq $layer) { return }
    $path = if ($parentPath) { "$parentPath/$($layer.Name)" } else { $layer.Name }

    $visKeys = @(); $xfKeys = @(); $opKeys = @()
    if ($layer.Animation -and $layer.Animation.Keys) {
        $k = $layer.Animation.Keys
        if ($k.Visibility) { $visKeys = @($k.Visibility) }
        if ($k.Transform)  { $xfKeys  = @($k.Transform) }
        if ($k.Opacity)    { $opKeys  = @($k.Opacity) }
    }

    $onTicks  = @($visKeys | Where-Object { $_.Value -eq $true }  | ForEach-Object { [double]$_.Time })
    $offTicks = @($visKeys | Where-Object { $_.Value -eq $false } | ForEach-Object { [double]$_.Time })
    $steppedXf = @($xfKeys | Where-Object { $_.Interpolation -eq "None" }).Count

    $rows.Add([pscustomobject]@{
        Path      = $path
        Type      = $layer.Type
        Visible   = $layer.Visible
        Opacity   = $layer.Opacity
        FirstOnT  = if ($onTicks.Count)  { ($onTicks  | Measure-Object -Minimum).Minimum } else { $null }
        LastOffT  = if ($offTicks.Count) { ($offTicks | Measure-Object -Maximum).Maximum } else { $null }
        VisKeys   = $visKeys.Count
        XfKeys    = $xfKeys.Count
        StepXf    = $steppedXf
        OpKeys    = $opKeys.Count
        Timeline  = if ($layer.Animation) { $layer.Animation.Timeline } else { $null }
        StartOff  = if ($layer.Animation) { $layer.Animation.StartOffset } else { $null }
    })

    if ($layer.Implementation -and $layer.Implementation.Children) {
        foreach ($c in $layer.Implementation.Children) { Read-Layer $c $path }
    }
}

Read-Layer $seq.RootLayer ""
Write-Host "Parsed $($rows.Count) layers." -ForegroundColor Green

$view = $rows

if ($Name) {
    $view = $view | Where-Object { $_.Path -like "*$Name*" }
    Write-Host "Filter: name like '*$Name*' -> $($view.Count) layers" -ForegroundColor Yellow
}

if ($AtTime) {
    $t = Convert-TimecodeToTicks $AtTime
    # "live at t": turned on at or before t, and not turned off after that on-key
    $view = $view | Where-Object {
        $_.Type -ne "Group" -and (
            ($null -eq $_.FirstOnT -and $_.Visible) -or
            ($null -ne $_.FirstOnT -and $_.FirstOnT -le $t -and ($null -eq $_.LastOffT -or $_.LastOffT -ge $t))
        )
    }
    Write-Host "Filter: live at $AtTime ($([int]$t) ticks) -> $($view.Count) layers" -ForegroundColor Yellow
}

$out = $view | Select-Object Path, Type, Visible, Opacity,
    @{n='FirstOn'; e={ if ($null -ne $_.FirstOnT) { Convert-TicksToTimecode $_.FirstOnT } else { "-" } }},
    @{n='LastOff'; e={ if ($null -ne $_.LastOffT) { Convert-TicksToTimecode $_.LastOffT } else { "-" } }},
    VisKeys, XfKeys, StepXf, OpKeys

if ($Csv) {
    $out | Export-Csv -Path $Csv -NoTypeInformation -Encoding utf8
    Write-Host "Wrote $Csv" -ForegroundColor Green
} else {
    $out | Format-Table -AutoSize | Out-String -Width 200
}

Write-Host ""
Write-Host "Type histogram:" -ForegroundColor Cyan
$rows | Group-Object Type | Sort-Object Count -Descending | Format-Table Count, Name -AutoSize | Out-String -Width 60
