[CmdletBinding()]
param(
    [string]$GodotPath = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe',
    [string]$RouteLog = 'task3-round3-live-suppression-green.log'
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$placementSource = Join-Path $project 'scripts\ui\ProtectedPromptPlacer.cs'
$presentationSource = Join-Path $project 'tests\presentation\ProloguePresentation.cs'
$routeLogPath = Join-Path $project $RouteLog
$placementHashBefore = (Get-FileHash -LiteralPath $placementSource -Algorithm SHA256).Hash
$presentationHashBefore = (Get-FileHash -LiteralPath $presentationSource -Algorithm SHA256).Hash
$blockerEnvironment = 'KNIGHT_PRESENTATION_FORCE_BOSS_PROMPT_CANDIDATES_BLOCKED'
$previousBlockerValue = [Environment]::GetEnvironmentVariable($blockerEnvironment, 'Process')

try {
    "TASK3_LIVE_SUPPRESSION_SOURCE placement_sha256=$placementHashBefore presentation_sha256=$presentationHashBefore"
    [Environment]::SetEnvironmentVariable($blockerEnvironment, '1', 'Process')
    & dotnet build Knight.csproj --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Live all-blocked suppression fixture failed to build.' }

    & $GodotPath --headless --path $project --fixed-fps 60 `
        --script res://tests/presentation/ProloguePresentation.cs --log-file $routeLogPath
    if ($LASTEXITCODE -ne 0) { throw "Live all-blocked suppression route failed exit=$LASTEXITCODE." }
    $routeOutput = Get-Content -LiteralPath $routeLogPath -Raw
    if ($routeOutput -notmatch 'PROLOGUE_BOSS_GUIDANCE_INDEPENDENT_BLOCKERS_OK candidates=10 padding=8') {
        throw 'Live all-blocked fixture did not create the ten authoritative collision blockers.'
    }
    foreach ($kind in 'BossSweep','BossLeapSlam','BossGroundWave','BossHammerThrow') {
        if ($routeOutput -notmatch "PROLOGUE_BOSS_GUIDANCE_SUPPRESSED kind=$kind .*independent_candidates=10 blocked=10") {
            throw "Live all-blocked suppression was not independently proven for $kind."
        }
        if ($routeOutput -notmatch "PROLOGUE_BOSS_GUIDANCE_CLEAR_OK kind=$kind") {
            throw "Live all-blocked suppression did not retain clearance evidence for $kind."
        }
    }
    if ($routeOutput -match 'PROLOGUE_BOSS_GUIDANCE_CLEAR_FAIL') {
        throw 'Live all-blocked suppression emitted a Boss guidance failure.'
    }
    'TASK3_LIVE_ALL_BLOCKED_GREEN_OK attacks=4 candidates=10 padding=8 card_hidden=true'
}
finally {
    [Environment]::SetEnvironmentVariable($blockerEnvironment, $previousBlockerValue, 'Process')
    $placementHashAfter = (Get-FileHash -LiteralPath $placementSource -Algorithm SHA256).Hash
    $presentationHashAfter = (Get-FileHash -LiteralPath $presentationSource -Algorithm SHA256).Hash
    if ($placementHashAfter -ne $placementHashBefore -or $presentationHashAfter -ne $presentationHashBefore) {
        throw 'Live all-blocked fixture modified a source file.'
    }
    "TASK3_LIVE_SUPPRESSION_SOURCE_UNCHANGED placement_sha256=$placementHashAfter presentation_sha256=$presentationHashAfter"
}
