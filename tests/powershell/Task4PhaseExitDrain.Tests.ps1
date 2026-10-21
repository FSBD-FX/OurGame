[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$task4Script = Join-Path $project 'tests\powershell\Task4CaptureExit.ps1'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

$output = & powershell -NoProfile -ExecutionPolicy Bypass -File $task4Script -OnlyPhaseExitBoundary 2>&1
$output | ForEach-Object { $_ }
Assert-True ($LASTEXITCODE -eq 0) 'phase-exit Temp mutation and two green phase pairs exit zero'
$text = $output -join "`n"
Assert-True ($text -match 'TASK4_PHASE_EXIT_DELETE_RED_OBSERVED') `
    'deleting the phase-exit drain is rejected by the focused output contract'
Assert-True ($text -match 'TASK4_PHASE_EXIT_EARLY_MARKER_RED_OBSERVED') `
    'moving a phase-exit marker before finalization is rejected by runtime ordering'
Assert-True ($text -match 'TASK4_PHASE_EXIT_MUTATION_ISOLATION_OK') `
    'both phase-exit variants restore their Temp-only sources and clean the copy'
Assert-True (([regex]::Matches($text, 'TASK4_PHASE_EXIT_GREEN phase=pre pair=(?:one|two) exit=0')).Count -eq 2) `
    'two fresh pre phases pass with direct native exit zero'
Assert-True (([regex]::Matches($text, 'TASK4_PHASE_EXIT_GREEN phase=post pair=(?:one|two) exit=0')).Count -eq 2) `
    'two fresh post phases pass with direct native exit zero'

Write-Output 'TASK4_PHASE_EXIT_DRAIN_CONTRACT_OK'
