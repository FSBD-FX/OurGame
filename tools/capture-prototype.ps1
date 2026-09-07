param([switch]$FramesOnly)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'EvidenceChecks.ps1')
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$proof = Join-Path $project 'screenshots\prototype'
New-Item -ItemType Directory -Path $proof -Force | Out-Null
$resolvedProof = (Resolve-Path -LiteralPath $proof).Path
Remove-PrototypeCaptureOutputs -ProofPath $resolvedProof -FramesOnly:$FramesOnly
$startedUtc = [DateTime]::UtcNow
$movie = if ($FramesOnly) { Join-Path $proof 'frame.png' } else { Join-Path $proof 'prototype.avi' }
$captureLog = Join-Path $project 'capture-prototype.log'
& $godot --path $project --fixed-fps 60 --write-movie $movie --script 'res://tests/presentation/PrototypePresentation.cs' --log-file $captureLog
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$outputs = if ($FramesOnly) {
    @(Get-ChildItem -LiteralPath $resolvedProof -File -Filter 'frame*.png')
} else {
    @(Get-Item -LiteralPath (Join-Path $resolvedProof 'prototype.avi') -ErrorAction SilentlyContinue)
}
$outputPaths = @($outputs | ForEach-Object { $_.FullName })
Assert-FreshNonEmptyOutputs -Paths $outputPaths -StartedUtc $startedUtc
Assert-FreshNonEmptyOutputs -Paths @($captureLog) -StartedUtc $startedUtc
Assert-ExactLogMarker -Path $captureLog -Marker 'PRESENTATION_COMPLETE frame=720'
Assert-NoEngineLogErrors -Paths @($captureLog)
$outputs | Select-Object Name,Length,LastWriteTime
