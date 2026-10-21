[CmdletBinding()]
param(
    [string]$GodotPath = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe',
    [string]$RouteLog = 'task3-round2-real-route-suppression.log'
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$source = Join-Path $project 'scripts\ui\ProtectedPromptPlacer.cs'
$routeLogPath = Join-Path $project $RouteLog
$originalBytes = [System.IO.File]::ReadAllBytes($source)
$originalHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$sourceWasMutated = $false

function Get-TextHash([string]$text) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
}

try {
    $sourceText = [System.Text.Encoding]::UTF8.GetString($originalBytes)
    $needle = '            if (!IntersectsProtected(candidate, protectedRegions)) return candidate;'
    $replacement = '            if (false) return candidate;'
    if ([regex]::Matches($sourceText, [regex]::Escape($needle)).Count -ne 2) {
        throw "Expected exactly two protected prompt candidate acceptance branches in $source."
    }
    $mutatedText = $sourceText.Replace($needle, $replacement)
    $mutationDiffId = Get-TextHash "$needle`n=>`n$replacement (two branches)"
    [System.IO.File]::WriteAllBytes($source,
        [System.Text.Encoding]::UTF8.GetBytes($mutatedText))
    $sourceWasMutated = $true
    $mutatedHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    "TASK3_SUPPRESSION_MUTATION_SOURCE original_sha256=$originalHash mutated_sha256=$mutatedHash diff_id=$mutationDiffId"

    & dotnet build Knight.csproj --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'All-blocked mutation source failed to build.' }

    & $GodotPath --headless --path $project --fixed-fps 60 `
        --script res://tests/presentation/ProloguePresentation.cs --log-file $routeLogPath
    $routeExit = $LASTEXITCODE
    if ($routeExit -eq 0) {
        throw 'False all-blocked placer mutation unexpectedly passed the independent Boss guidance oracle.'
    }
    $routeOutput = Get-Content -LiteralPath $routeLogPath -Raw
    foreach ($kind in 'BossSweep','BossLeapSlam','BossGroundWave','BossHammerThrow') {
        if ($routeOutput -notmatch "PROLOGUE_BOSS_GUIDANCE_CLEAR_FAIL kind=$kind") {
            throw "False all-blocked placer mutation did not fail the real route for $kind."
        }
    }
    if ($routeOutput -notmatch 'REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL') {
        throw 'False all-blocked placer mutation lacked the named aggregate assertion.'
    }
    if ($routeOutput -match 'PROLOGUE_BOSS_GUIDANCE_SUPPRESSED') {
        throw 'False all-blocked placer mutation was accepted as an intentional suppression.'
    }
    "TASK3_REAL_ROUTE_FALSE_SUPPRESSION_RED_OK exit=$routeExit all_attacks=4 assertion=REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL"
}
finally {
    if ($sourceWasMutated) {
        [System.IO.File]::WriteAllBytes($source, $originalBytes)
        $restoredHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
        if ($restoredHash -ne $originalHash) {
            throw "False suppression mutation restoration hash mismatch: expected $originalHash got $restoredHash"
        }
        "TASK3_SUPPRESSION_MUTATION_RESTORED_SHA256=$restoredHash"
    }
}
