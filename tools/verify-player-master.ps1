[CmdletBinding()]
param(
    [string]$ExternalRoot,
    [string]$Stage
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$python = 'E:\OurGame\ArtTools\.venvs\agent-sprite-forge\Scripts\python.exe'
if (-not (Test-Path -LiteralPath $python -PathType Leaf)) {
    throw "Required Python interpreter was not found: $python"
}

Push-Location $projectRoot
try {
    & $python -m unittest discover -s tests\art -p 'test_complete_master_*.py' -v
    if ($LASTEXITCODE -ne 0) {
        throw "Complete-master tests failed with exit code $LASTEXITCODE"
    }
    Write-Output 'COMPLETE_MASTER_TESTS_OK'

    if ([string]::IsNullOrWhiteSpace($ExternalRoot)) {
        return
    }
    if ([string]::IsNullOrWhiteSpace($Stage)) {
        throw '-Stage is required when -ExternalRoot is provided'
    }

    $startedNs = & $python -c 'import time; print(time.time_ns())'
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not record verification start time'
    }

    $prepareScripts = @{
        structure = 'tools\art\complete_master_structure.py'
        body = 'tools\art\complete_master_body.py'
        spoolCore = 'tools\art\complete_master_spool_core.py'
        mantle = 'tools\art\complete_master_mantle.py'
        weaponHands = 'tools\art\complete_master_weapon_hands.py'
        lanternTether = 'tools\art\complete_master_lantern_tether.py'
        final = 'tools\art\complete_master_review.py'
        a2 = 'tools\art\complete_master_review.py'
    }
    if ($prepareScripts.ContainsKey($Stage)) {
        $prepareScript = Join-Path $projectRoot $prepareScripts[$Stage]
        if (Test-Path -LiteralPath $prepareScript -PathType Leaf) {
            & $python $prepareScript --external-root $ExternalRoot
            if ($LASTEXITCODE -ne 0) {
                throw "Stage preparation failed with exit code $LASTEXITCODE"
            }
        }
    }

    & $python tools\art\complete_master_qc.py `
        --root $ExternalRoot `
        --stage $Stage `
        --verification-started-ns $startedNs
    if ($LASTEXITCODE -ne 0) {
        throw "Complete-master QC failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}
