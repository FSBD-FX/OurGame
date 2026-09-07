[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$task4Script = Join-Path $project 'tests\powershell\Task4CaptureExit.ps1'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

$output = & powershell -NoProfile -ExecutionPolicy Bypass -File $task4Script -OnlyFailurePath 2>&1
$output | ForEach-Object { $_ }
Assert-True ($LASTEXITCODE -eq 0) 'forced-failure focused runner exits zero after observing the intended nonzero child'
$text = $output -join "`n"
Assert-True ($text -match 'TASK4_FAILURE_PATH_RED_OBSERVED exit=1 failure_frame=300') `
    'forced-failure runner proves its exact frame-300 visibility-bounds source'

Write-Output 'TASK4_FORCED_FAILURE_CONTRACT_OK'
