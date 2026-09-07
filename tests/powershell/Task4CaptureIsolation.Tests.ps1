[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $project 'tools\EvidenceChecks.ps1')
$task4Script = Join-Path $project 'tests\powershell\Task4CaptureExit.ps1'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

$source = Get-Content -LiteralPath $task4Script -Raw
Assert-True (-not [regex]::IsMatch($source,
        '\[System\.IO\.File\]::WriteAll(?:Text|Bytes)\(\$sourcePath')) `
    'Task4 capture mutations never write a shared source path'
Assert-True ($source -match 'TASK4_MUTATION_TEMP_COPY_CREATED path=\$tempProject includes=\.godot') `
    'Task4 mutation evidence accurately records that the required .godot cache is copied'

$sharedPrologue = Join-Path $project 'tests\presentation\ProloguePresentation.cs'
$sharedTeardown = Join-Path $project 'tests\TestSceneTeardown.cs'
$beforePrologue = Get-EvidenceSha256 -LiteralPath $sharedPrologue
$beforeTeardown = Get-EvidenceSha256 -LiteralPath $sharedTeardown
$output = & powershell -NoProfile -ExecutionPolicy Bypass -File $task4Script -OnlyMutationIsolation 2>&1
$output | ForEach-Object { $_ }
Assert-True ($LASTEXITCODE -eq 0) 'isolated mutation runner exits zero'
Assert-True (($output -join "`n") -match 'TASK4_MUTATION_ISOLATION_OK') `
    'isolated mutation runner reports both real mutant boundaries complete'
Assert-True ((Get-EvidenceSha256 -LiteralPath $sharedPrologue) -ceq $beforePrologue) `
    'isolated no-drain movie mutation leaves shared ProloguePresentation bytes unchanged'
Assert-True ((Get-EvidenceSha256 -LiteralPath $sharedTeardown) -ceq $beforeTeardown) `
    'isolated marker-order mutation leaves shared TestSceneTeardown bytes unchanged'

Write-Output 'TASK4_CAPTURE_ISOLATION_OK'
