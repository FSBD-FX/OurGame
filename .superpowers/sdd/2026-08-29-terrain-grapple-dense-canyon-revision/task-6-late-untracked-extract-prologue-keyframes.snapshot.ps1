[CmdletBinding()]
param(
    [string]$FramesPath,
    [string]$OutputPath,
    [string]$StageLogPath
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($FramesPath)) {
    $FramesPath = Join-Path $project 'screenshots\prologue\prologue-frames'
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $project 'screenshots\prologue\keyframes'
}
if ([string]::IsNullOrWhiteSpace($StageLogPath)) {
    $StageLogPath = Join-Path $project 'screenshots\prologue\prologue-frames.log'
}

$beats = @(
    'title_start','mode_hud','attack_energy','shield_overheat','grapple_reticles',
    'canyon_descent','canyon_basin','ascent_one','ascent_two','ascent_three',
    'optional_alcove','lost_temple','boss_intro','boss_attacks','death_retry',
    'completion_title_return'
)
if (-not (Test-Path -LiteralPath $StageLogPath -PathType Leaf)) {
    throw "Presentation stage log is missing: $StageLogPath"
}
$stageLines = @(Get-Content -LiteralPath $StageLogPath | Where-Object {
    $_ -match '^PROLOGUE_PRESENTATION_STAGE=(?<beat>[a-z0-9_]+) frame=(?<frame>\d+)\s*$'
})
$stageFrames = [System.Collections.Generic.List[int]]::new()
foreach ($beat in $beats) {
    $matches = @($stageLines | Where-Object {
        $_ -match "^PROLOGUE_PRESENTATION_STAGE=$([Regex]::Escape($beat)) frame=(?<frame>\d+)\s*$"
    })
    if ($matches.Count -ne 1) {
        throw "Presentation stage '$beat' must appear exactly once; found $($matches.Count)."
    }
    $null = $matches[0] -match ' frame=(?<frame>\d+)\s*$'
    $stageFrames.Add([int]$Matches['frame'])
}
for ($index = 1; $index -lt $stageFrames.Count; $index++) {
    if ($stageFrames[$index] -le $stageFrames[$index - 1]) {
        throw "Presentation stage frames must be strictly increasing: $($beats[$index - 1])=$($stageFrames[$index - 1]), $($beats[$index])=$($stageFrames[$index])."
    }
}

# The PNG writer records frame00000000.png for physics frame 1. ffprobe/ffmpeg are
# unnecessary on this lossless 1280x720@60 sequence, so the stage log is the timebase.
$frames = @(Get-ChildItem -LiteralPath $FramesPath -File -Filter 'frame*.png' | Sort-Object Name)
if ($frames.Count -lt 60) {
    throw "Keyframe extraction requires at least 60 source frames; got $($frames.Count)."
}
Add-Type -AssemblyName System.Drawing
$image = [System.Drawing.Image]::FromFile($frames[0].FullName)
try {
    if ($image.Width -ne 1280 -or $image.Height -ne 720) {
        throw "Keyframe source must be 1280x720; got $($image.Width)x$($image.Height)."
    }
}
finally { $image.Dispose() }

for ($index = 0; $index -lt $stageFrames.Count; $index++) {
    $sourceIndex = $stageFrames[$index] - 1
    if ($sourceIndex -lt 0 -or $sourceIndex -ge $frames.Count) {
        throw "Presentation stage '$($beats[$index])' frame $($stageFrames[$index]) is outside the $($frames.Count)-frame capture."
    }
}

if (Test-Path -LiteralPath $OutputPath -PathType Container) {
    Get-ChildItem -LiteralPath $OutputPath -File -Filter '*.png' | Remove-Item -Force
    $oldManifest = Join-Path $OutputPath 'manifest.txt'
    if (Test-Path -LiteralPath $oldManifest -PathType Leaf) {
        Remove-Item -LiteralPath $oldManifest -Force
    }
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
$manifest = [System.Collections.Generic.List[string]]::new()
for ($index = 0; $index -lt $beats.Count; $index++) {
    $sourceIndex = $stageFrames[$index] - 1
    $destination = Join-Path $OutputPath ('{0:D2}-{1}.png' -f ($index + 1), $beats[$index])
    Copy-Item -LiteralPath $frames[$sourceIndex].FullName -Destination $destination -Force
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $destination).Hash
    $manifest.Add("$($beats[$index])|source=$($frames[$sourceIndex].Name)|stage_frame=$($stageFrames[$index])|sha256=$hash")
    "PROLOGUE_KEYFRAME path=$destination beat=$($beats[$index]) stage_frame=$($stageFrames[$index]) source=$($frames[$sourceIndex].Name) sha256=$hash"
}
$manifestPath = Join-Path $OutputPath 'manifest.txt'
[System.IO.File]::WriteAllText($manifestPath, (($manifest -join "`n") + "`n"),
    [System.Text.UTF8Encoding]::new($false))
$manifestHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $manifestPath).Hash
"PROLOGUE_KEYFRAME_EXTRACTION_OK count=$($beats.Count) resolution=1280x720 fps=60 stage_bound=True manifest=$manifestPath sha256=$manifestHash"
