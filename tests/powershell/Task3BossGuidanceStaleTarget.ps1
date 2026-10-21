[CmdletBinding()]
param(
    [string]$GodotPath = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe',
    [string]$RouteLog = 'task3-round3-stale-target-route.log'
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$source = Join-Path $project 'scripts\prologue\guidance\PrologueGuidanceModel.cs'
$routeLogPath = Join-Path $project $RouteLog
$originalBytes = [System.IO.File]::ReadAllBytes($source)
$originalHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$sourceWasMutated = $false
$blockerEnvironment = 'KNIGHT_PRESENTATION_FORCE_BOSS_PROMPT_CANDIDATES_BLOCKED'
$previousBlockerValue = [Environment]::GetEnvironmentVariable($blockerEnvironment, 'Process')

function Get-TextHash([string]$text) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
}

try {
    $sourceText = [System.Text.Encoding]::UTF8.GetString($originalBytes)
    $matches = [regex]::Matches($sourceText,
        [regex]::Escape('PrologueGuidanceTargetKey.BossBody'))
    "TASK3_STALE_TARGET_MATCH source=$source hash=$originalHash count=$($matches.Count)"
    if ($matches.Count -ne 1) {
        throw "Expected exactly one active BossBody guidance branch in $source."
    }
    $needle = $matches[0].Value
    $replacement = 'PrologueGuidanceTargetKey.BossActivation'
    $mutatedText = $sourceText.Replace($needle, $replacement)
    $mutationDiffId = Get-TextHash "$needle`n=>`n$replacement"
    [System.IO.File]::WriteAllBytes($source,
        [System.Text.Encoding]::UTF8.GetBytes($mutatedText))
    $sourceWasMutated = $true
    $mutatedHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    "TASK3_STALE_TARGET_MUTATION_SOURCE original_sha256=$originalHash mutated_sha256=$mutatedHash diff_id=$mutationDiffId"

    [Environment]::SetEnvironmentVariable($blockerEnvironment, '1', 'Process')
    & dotnet build Knight.csproj --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Stale BossActivation mutation source failed to build.' }

    & $GodotPath --headless --path $project --fixed-fps 60 `
        --script res://tests/presentation/ProloguePresentation.cs --log-file $routeLogPath
    $routeExit = $LASTEXITCODE
    if ($routeExit -eq 0) {
        throw 'Stale BossActivation guidance unexpectedly passed intentional-suppression classification.'
    }
    $routeOutput = Get-Content -LiteralPath $routeLogPath -Raw
    foreach ($kind in 'BossSweep','BossLeapSlam','BossGroundWave','BossHammerThrow') {
        if ($routeOutput -notmatch "PROLOGUE_BOSS_GUIDANCE_CLEAR_FAIL kind=$kind") {
            throw "Stale BossActivation guidance did not fail the real route for $kind."
        }
    }
    if ($routeOutput -notmatch 'REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL') {
        throw 'Stale BossActivation guidance lacked the named aggregate assertion.'
    }
    "TASK3_STALE_TARGET_RED_OK exit=$routeExit all_attacks=4 assertion=REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL"
}
finally {
    [Environment]::SetEnvironmentVariable($blockerEnvironment, $previousBlockerValue, 'Process')
    if ($sourceWasMutated) {
        [System.IO.File]::WriteAllBytes($source, $originalBytes)
        $restoredHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
        if ($restoredHash -ne $originalHash) {
            throw "Stale target mutation restoration hash mismatch: expected $originalHash got $restoredHash"
        }
        "TASK3_STALE_TARGET_MUTATION_RESTORED_SHA256=$restoredHash"
    }
}
