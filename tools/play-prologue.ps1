param(
    [switch]$OpenReportOnly,
    [switch]$ValidateOnly,
    [string]$GodotPath = (Join-Path $PSScriptRoot '../../Tools/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe'),
    [string]$DotnetRoot = (Join-Path $PSScriptRoot '../../Tools/dotnet-9.0.317'),
    [string]$DotnetVersionOverride
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'PlaytestEvidenceGrant.ps1')
. (Join-Path $PSScriptRoot 'Assert-PrologueDeliveredScenes.ps1')
$godot = $GodotPath
$dotnetRoot = $DotnetRoot
$reportDirectory = Join-Path $env:APPDATA 'Godot\app_userdata\Knight\playtests\prologue'
$latestReport = Join-Path $reportDirectory 'latest.md'

if ($OpenReportOnly) {
    if (-not (Test-Path -LiteralPath $latestReport -PathType Leaf)) {
        throw "No playtest report exists yet. Start a run first: $latestReport"
    }
    Start-Process -FilePath $latestReport
    "Opened playtest report: $latestReport"
    exit 0
}

if (-not (Test-Path -LiteralPath $godot -PathType Leaf)) {
    throw "Godot 4.7.1 Mono executable is missing: $godot"
}
if (-not (Test-Path -LiteralPath (Join-Path $dotnetRoot 'dotnet.exe') -PathType Leaf)) {
    throw ".NET SDK is missing: $dotnetRoot"
}

$dotnet = Join-Path $dotnetRoot 'dotnet.exe'
$dotnetVersion = if ($DotnetVersionOverride) { $DotnetVersionOverride } else { (& $dotnet --version).Trim() }
$dotnetMajor = 0
if (-not [int]::TryParse(($dotnetVersion -split '\.')[0], [ref]$dotnetMajor) -or $dotnetMajor -ne 9) {
    throw "Knight requires .NET SDK 9.x; found '$dotnetVersion' at $dotnetRoot"
}
if ($ValidateOnly) {
    "PLAY_PROLOGUE_RUNTIME_OK Godot=$godot .NET=$dotnetVersion"
    exit 0
}

$runningGame = Get-Process -ErrorAction SilentlyContinue | Where-Object {
    $_.ProcessName -like 'Godot_v4.7.1-stable_mono_win64*' -and $_.MainWindowTitle -eq 'Knight (DEBUG)'
} | Select-Object -First 1
if ($runningGame) {
    throw "A Knight playtest is already running (PID $($runningGame.Id)); close it before starting a new evidence run."
}

Assert-PrologueDeliveredScenes -ProjectRoot $project

$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:Path = "$dotnetRoot;$env:Path"
& $dotnet build (Join-Path $project 'Knight.csproj')
if ($LASTEXITCODE) { throw "Knight build failed with exit code $LASTEXITCODE" }
$godotConsole = Join-Path ([System.IO.Path]::GetDirectoryName($godot)) `
    (([System.IO.Path]::GetFileNameWithoutExtension($godot)) + '_console.exe')
if (-not (Test-Path -LiteralPath $godotConsole -PathType Leaf)) { $godotConsole = $godot }
$importLog = Join-Path $env:TEMP ("knight-play-import-{0}.log" -f [Guid]::NewGuid().ToString('N'))
try {
    & $godotConsole --headless --path $project --import --log-file $importLog
    if ($LASTEXITCODE) { throw "Knight import failed with exit code $LASTEXITCODE" }
    if (-not (Test-Path -LiteralPath $importLog -PathType Leaf)) {
        throw "Knight import did not create its verification log: $importLog"
    }
    $importText = Get-Content -LiteralPath $importLog -Raw
    if ($importText -match '(?m)^\s*(ERROR:|SCRIPT ERROR:|Parse Error)') {
        throw "Knight import reported engine errors; see $importLog"
    }
}
finally {
    if (Test-Path -LiteralPath $importLog) { Remove-Item -LiteralPath $importLog -Force }
}
$game = Start-HandsOnPlaytestProcess -FilePath $godot -ArgumentList @('--path', $project) `
    -ProjectPath $project
"Started Knight playtest (PID $($game.Id))."
"The current run is autosaved under: $reportDirectory"
"After closing the game, open the latest report with:"
"powershell -ExecutionPolicy Bypass -File `"$PSCommandPath`" -OpenReportOnly"
