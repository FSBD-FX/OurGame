[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$AviPath,
    [Parameter(Mandatory)][string]$StageLogPath,
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$StagePrefix = 'PROLOGUE_PRESENTATION_STAGE',
    [string[]]$Beats,
    [string]$BeatsCsv
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Task6AviStreaming.ps1')
. (Join-Path $PSScriptRoot 'EvidenceChecks.ps1')

# This extractor reads Godot's MJPEG AVI chunks directly. It intentionally extracts only the
# named proof frames, rather than producing thousands of disposable full-flow PNGs.
$defaultBeats = @(
    'title_start', 'mode_hud', 'attack_energy', 'recovery', 'canyon_descent', 'grapple_reticles',
    'canyon_basin', 'checkpoint_basin', 'shield_overheat',
    'ascent_one', 'ascent_two', 'ascent_three', 'optional_alcove', 'lost_temple',
    'checkpoint_temple', 'boss_intro', 'death_retry', 'completion', 'boost_attached', 'boost_start',
    'attached_boost_consumed', 'boost_arc', 'boost_second_rejected', 'boost_land', 'boss_sweep',
    'boss_leap_slam', 'boss_ground_wave', 'boss_hammer_throw', 'boss_attack_contract',
    'completion_title_return'
)
if (-not [string]::IsNullOrWhiteSpace($BeatsCsv)) {
    if ($null -ne $Beats -and $Beats.Count -gt 0) {
        throw 'Specify only -Beats or -BeatsCsv, never both.'
    }
    $Beats = @($BeatsCsv -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_.Length -gt 0 })
}
if ($null -eq $Beats -or $Beats.Count -eq 0) { $Beats = $defaultBeats }

if (-not (Test-Path -LiteralPath $StageLogPath -PathType Leaf)) {
    throw "Presentation stage log is missing: $StageLogPath"
}
if (Test-Path -LiteralPath $OutputPath) {
    throw "Keyframe output must be a fresh directory: $OutputPath"
}

$lines = @(Get-Content -LiteralPath $StageLogPath)
$stageFrames = [ordered]@{}
$last = 0
foreach ($beat in $Beats) {
    $stageMatches = @($lines | Where-Object {
        $_ -match ('^' + [regex]::Escape($StagePrefix) + '=' + [regex]::Escape($beat) +
            ' frame=(?<frame>\d+)\s*$')
    })
    if ($stageMatches.Count -ne 1) {
        throw "Presentation stage '$beat' must appear exactly once; found $($stageMatches.Count)."
    }
    $null = $stageMatches[0] -match ' frame=(?<frame>\d+)\s*$'
    $frame = [int]$Matches['frame']
    if ($frame -le $last) {
        throw "Presentation stages must be strictly increasing: $beat=$frame after $last"
    }
    $last = $frame
    $stageFrames[$beat] = $frame
}

$aviFrames = Get-Task6AviMjpegIndex -Path $AviPath
$frames = $aviFrames.Frames
if ($frames.Count -lt 60) { throw "AVI has too few MJPEG video frames: $($frames.Count)" }
$fps = [Math]::Round(1000000.0 / [double]$aviFrames.Header.MicrosecondsPerFrame)
if ($aviFrames.Header.Width -ne 1280 -or $aviFrames.Header.Height -ne 720 -or $fps -ne 60) {
    throw "AVI keyframe source fails the 1280x720/60 contract: $($aviFrames.Header.Width)x$($aviFrames.Header.Height)@$fps"
}
foreach ($entry in $stageFrames.GetEnumerator()) {
    if ($entry.Value -lt 1 -or $entry.Value -gt $frames.Count) {
        throw "Stage '$($entry.Key)' points outside the AVI: frame=$($entry.Value) count=$($frames.Count)"
    }
}

Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
$manifest = [Collections.Generic.List[string]]::new()
$aviHash = Get-EvidenceSha256 -LiteralPath $AviPath
$serial = 0
$boostMotionBeats = @('boost_start', 'boost_arc', 'boost_second_rejected', 'boost_land')
$centerSamples = [ordered]@{}

function Export-Task6AviFramePng {
    param(
        [Parameter(Mandatory)][int]$FrameNumber,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][int]$Serial
    )
    if ($FrameNumber -lt 1 -or $FrameNumber -gt $frames.Count) {
        throw "Task6 keyframe '$Name' points outside the AVI: frame=$FrameNumber count=$($frames.Count)"
    }
    $sourceFrame = $frames[$FrameNumber - 1]
    $temporaryJpeg = Join-Path $OutputPath ('._task6-{0:D2}-{1}.jpg' -f $Serial, $Name)
    $destination = Join-Path $OutputPath ('{0:D2}-{1}.png' -f $Serial, $Name)
    Copy-Task6AviFrameToFile -AviPath $AviPath -Offset $sourceFrame.Offset -Size $sourceFrame.Size `
        -Destination $temporaryJpeg
    $image = $null
    try {
        $image = [Drawing.Image]::FromFile($temporaryJpeg)
        if ($image.Width -ne 1280 -or $image.Height -ne 720) {
            throw "AVI frame '$Name' has wrong dimensions $($image.Width)x$($image.Height)"
        }
        $image.Save($destination, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        if ($null -ne $image) { $image.Dispose() }
        if (Test-Path -LiteralPath $temporaryJpeg) { Remove-Item -LiteralPath $temporaryJpeg -Force }
    }
    $sample = Get-Task6ImageSample -Path $destination
    return [pscustomobject]@{
        Frame = $FrameNumber; Name = $Name; Path = $destination;
        Sha256 = (Get-EvidenceSha256 -LiteralPath $destination); Sample = $sample
    }
}

foreach ($entry in $stageFrames.GetEnumerator()) {
    $serial++
    $center = Export-Task6AviFramePng -FrameNumber ([int]$entry.Value) -Name $entry.Key -Serial $serial
    $manifest.Add("$($entry.Key)|avi_frame=$($center.Frame)|png=$(Split-Path -Leaf $center.Path)|sha256=$($center.Sha256)")
    $centerSamples[$entry.Key] = $center.Sample
    "PROLOGUE_KEYFRAME path=$($center.Path) beat=$($entry.Key) avi_frame=$($center.Frame) sha256=$($center.Sha256)"

    if ($boostMotionBeats -contains $entry.Key) {
        $serial++
        $previous = Export-Task6AviFramePng -FrameNumber ([int]$entry.Value - 1) `
            -Name ($entry.Key + '__previous') -Serial $serial
        $serial++
        $next = Export-Task6AviFramePng -FrameNumber ([int]$entry.Value + 1) `
            -Name ($entry.Key + '__next') -Serial $serial
        $previousDelta = Get-Task6ImageSampleDelta -Left $previous.Sample.Bytes -Right $center.Sample.Bytes
        $nextDelta = Get-Task6ImageSampleDelta -Left $center.Sample.Bytes -Right $next.Sample.Bytes
        if ($previousDelta.ChangedPoints -le 0 -or $previousDelta.AbsoluteDelta -le 0 -or
            $nextDelta.ChangedPoints -le 0 -or $nextDelta.AbsoluteDelta -le 0) {
            throw "Task6 boost '$($entry.Key)' has no visible neighbouring-frame motion."
        }
        $manifest.Add("$($entry.Key)__previous|avi_frame=$($previous.Frame)|png=$(Split-Path -Leaf $previous.Path)|sha256=$($previous.Sha256)")
        $manifest.Add("$($entry.Key)__next|avi_frame=$($next.Frame)|png=$(Split-Path -Leaf $next.Path)|sha256=$($next.Sha256)")
        $manifest.Add("$($entry.Key)__motion|center_frame=$($center.Frame)|previous_frame=$($previous.Frame)|next_frame=$($next.Frame)|previous_png_sha256=$($previous.Sha256)|center_png_sha256=$($center.Sha256)|next_png_sha256=$($next.Sha256)|previous_sample_delta=$($previousDelta.AbsoluteDelta)|previous_changed_points=$($previousDelta.ChangedPoints)|next_sample_delta=$($nextDelta.AbsoluteDelta)|next_changed_points=$($nextDelta.ChangedPoints)|sample_points=$($center.Sample.Points)")
        "TASK6_BOOST_NEIGHBOR_MOTION_OK beat=$($entry.Key) previous_frame=$($previous.Frame) center_frame=$($center.Frame) next_frame=$($next.Frame) previous_delta=$($previousDelta.AbsoluteDelta) next_delta=$($nextDelta.AbsoluteDelta)"
    }
}

[string[]]$centerNames = @($centerSamples.Keys)
$centerSampleHashes = @($centerSamples.Values | ForEach-Object { $_.Sha256 } | Sort-Object -Unique)
[long]$changedTransitions = 0
[long]$totalSampleDelta = 0
for ($index = 0; $index -lt $centerNames.Count - 1; $index++) {
    $delta = Get-Task6ImageSampleDelta -Left $centerSamples[$centerNames[$index]].Bytes `
        -Right $centerSamples[$centerNames[$index + 1]].Bytes
    if ($delta.ChangedPoints -gt 0 -and $delta.AbsoluteDelta -gt 0) { $changedTransitions++ }
    $totalSampleDelta += $delta.AbsoluteDelta
}
# A whole-flow proof needs broad motion across its thirty stages.  A Boss-retry proof has
# exactly two independently bound stages, so its strongest possible global transition count
# is one.  Scale the frozen-movie minimum to the number of requested stages; a duplicate or
# static two-stage Boss capture still fails on both distinctness and its sole transition.
$minimumDistinct = [Math]::Min(8, $centerNames.Count)
$minimumTransitions = [Math]::Min(4, [Math]::Max(0, $centerNames.Count - 1))
if ($centerSampleHashes.Count -lt $minimumDistinct -or $changedTransitions -lt $minimumTransitions -or
    $totalSampleDelta -le 0) {
    throw "Task6 AVI appears frozen at distributed samples: distinct=$($centerSampleHashes.Count) changed=$changedTransitions delta=$totalSampleDelta"
}
$manifest.Add("TASK6_GLOBAL_MOTION|stages=$($centerNames.Count)|distinct_sample_hashes=$($centerSampleHashes.Count)|changed_transitions=$changedTransitions|total_sample_delta=$totalSampleDelta|sample_points=$($centerSamples[$centerNames[0]].Points)")
$manifest.Insert(0, "avi_sha256=$aviHash")
$manifest.Insert(1, "avi_frames=$($frames.Count)")
$manifestPath = Join-Path $OutputPath 'manifest.txt'
[IO.File]::WriteAllText($manifestPath, (($manifest -join "`n") + "`n"),
    [Text.UTF8Encoding]::new($false))
$manifestHash = Get-EvidenceSha256 -LiteralPath $manifestPath
"PROLOGUE_KEYFRAME_EXTRACTION_OK count=$($stageFrames.Count) resolution=1280x720 fps=60 " +
    "avi=$AviPath avi_frames=$($frames.Count) manifest=$manifestPath sha256=$manifestHash " +
    "motion_distinct=$($centerSampleHashes.Count) motion_transitions=$changedTransitions"
