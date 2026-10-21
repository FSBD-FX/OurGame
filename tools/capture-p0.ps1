param([switch]$FramesOnly)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'EvidenceChecks.ps1')
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$dotnetRoot = 'G:\godot\dotnet-sdk'
$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:Path = "$dotnetRoot;$env:Path"
& "$dotnetRoot\dotnet.exe" build (Join-Path $project 'Knight.csproj')
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$screenshots = Join-Path $project 'screenshots'
New-Item -ItemType Directory -Path $screenshots -Force | Out-Null
$gdignore = Join-Path $screenshots '.gdignore'
if (-not (Test-Path -LiteralPath $gdignore -PathType Leaf)) {
    [System.IO.File]::WriteAllText($gdignore, [string]::Empty, [System.Text.UTF8Encoding]::new($false))
}
$proof = Join-Path $screenshots 'p0'
New-Item -ItemType Directory -Path $proof -Force | Out-Null
Get-ChildItem -LiteralPath $proof -File | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }
$startedUtc = [DateTime]::UtcNow
$movie = if ($FramesOnly) { Join-Path $proof 'frame.png' } else { Join-Path $proof 'p0.avi' }
$log = Join-Path $project 'capture-p0.log'
& $godot --path $project --fixed-fps 60 --write-movie $movie --script 'res://tests/presentation/P0Presentation.cs' --log-file $log
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$outputs = if ($FramesOnly) {
    @(Get-ChildItem -LiteralPath $proof -File -Filter 'frame*.png')
} else {
    @(Get-Item -LiteralPath (Join-Path $proof 'p0.avi') -ErrorAction SilentlyContinue)
}
$paths = @($outputs | ForEach-Object { $_.FullName })
Assert-FreshNonEmptyOutputs -Paths $paths -StartedUtc $startedUtc
Assert-FreshNonEmptyOutputs -Paths @($log) -StartedUtc $startedUtc
Assert-ExactLogMarker -Path $log -Marker 'P0_PRESENTATION_COMPLETE frame=600'
Assert-NoEngineLogErrors -Paths @($log)
$outputs | Select-Object Name,Length,LastWriteTime
