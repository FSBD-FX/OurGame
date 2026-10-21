param(
    [switch]$ValidateOnly,
    [string]$GodotPath = (Join-Path $PSScriptRoot '../../Tools/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64_console.exe'),
    [string]$DotnetRoot = (Join-Path $PSScriptRoot '../../Tools/dotnet-9.0.317')
)

$ErrorActionPreference = 'Stop'
$sampleProject = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sampleScene = 'scenes/prologue/sample/BasinQualitySample.tscn'
if (-not (Test-Path -LiteralPath $GodotPath -PathType Leaf)) { throw "Godot Mono missing: $GodotPath" }
$sampleDotnet = Join-Path $DotnetRoot 'dotnet.exe'
if (-not (Test-Path -LiteralPath $sampleDotnet -PathType Leaf)) { throw ".NET SDK missing: $sampleDotnet" }
if (-not (Test-Path -LiteralPath (Join-Path $sampleProject $sampleScene) -PathType Leaf)) {
    throw 'The Basin sample scene has not been generated yet. This development entry is not ready.'
}
$sampleVersion = (& $sampleDotnet --version).Trim()
if ($LASTEXITCODE -ne 0 -or $sampleVersion -notmatch '^9\.') { throw "Expected .NET SDK 9.x, got $sampleVersion" }
if ($ValidateOnly) {
    "BASIN_SAMPLE_LAUNCHER_VALIDATED scene=$sampleScene sdk=$sampleVersion development_only=True"
    exit 0
}

$sampleRunning = Get-Process -ErrorAction SilentlyContinue | Where-Object {
    $_.ProcessName -like 'Godot_v4.7.1-stable_mono_win64*' -and $_.MainWindowTitle -eq 'Knight (DEBUG)'
} | Select-Object -First 1
if ($sampleRunning) { throw "Close the existing Knight window first (PID $($sampleRunning.Id))." }
$env:DOTNET_ROOT = $DotnetRoot
$env:DOTNET_ROOT_X64 = $DotnetRoot
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:PATH = "$DotnetRoot;$env:PATH"
& $sampleDotnet build (Join-Path $sampleProject 'Knight.csproj') --no-restore
if ($LASTEXITCODE -ne 0) { throw "Sample build failed: $LASTEXITCODE" }

Write-Output 'Basin sample is in development. This is not a finished sample or a standalone game package.'
Write-Output 'Its saves and settings are separate from the full prologue. ESC opens the sample title-return button.'
# This is a development launch, not a grant for full-prologue human completion evidence.
& $GodotPath --path $sampleProject $sampleScene
exit $LASTEXITCODE
