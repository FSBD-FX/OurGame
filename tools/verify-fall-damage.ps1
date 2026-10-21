param(
    [ValidateSet('fall','prototype','auto-pull','release','proof')][string]$Suite='fall',
    [switch]$ExpectRed,
    [switch]$Rendered
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceRoot='E:/OurGame/Knight/.evidence/october07-fall-damage'
$caseRoot=Join-Path $evidenceRoot ($Suite+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+[guid]::NewGuid().ToString('N'))
$null=New-Item -ItemType Directory -Path $caseRoot
$protectedRoot='C:/Users/22499/AppData/Roaming/Godot/app_userdata/Knight'
function Get-ProtectedInventory {
    $rows=[Collections.Generic.List[string]]::new()
    foreach($name in @('knight','playtests','logs')) {
        $target=Join-Path $protectedRoot $name
        $rows.Add("ROOT|$target|$(Test-Path -LiteralPath $target)")
        if(!(Test-Path -LiteralPath $target)){continue}
        foreach($entry in Get-ChildItem -LiteralPath $target -Recurse -Force | Sort-Object FullName) {
            if(($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw "Protected reparse: $($entry.FullName)"}
            if($entry.PSIsContainer){$rows.Add("DIR|$($entry.FullName)")}
            else{$rows.Add("FILE|$($entry.FullName)|$($entry.Length)|$((Get-FileHash -LiteralPath $entry.FullName -Algorithm SHA256).Hash)")}
        }
    }
    return [string]::Join("`n",$rows)
}
$before=Get-ProtectedInventory
$runner=@{
    fall=@('res://tests/RunFallDamageTests.cs','FALL_DAMAGE_TESTS_OK')
    prototype=@('res://tests/RunPrototypeTests.cs','PROTOTYPE_TESTS_OK')
    'auto-pull'=@('res://tests/RunAutoPullClawTests.cs','AUTO_PULL_CLAW_TESTS_OK')
    release=@('res://tests/RunGrappleReleaseTests.cs','GRAPPLE_RELEASE_TESTS_OK')
    proof=@('res://tests/presentation/PlayerFallDamageProof.cs','FALL_DAMAGE_PROOF_OK')
}[$Suite]
$stdout=Join-Path $caseRoot 'stdout.log'
$stderr=Join-Path $caseRoot 'stderr.log'
$argv=@('--fixed-fps','60','--path',$projectRoot,'--script',$runner[0],
    '--log-file',(Join-Path $caseRoot 'godot.log'))
if(!$Rendered){$argv=@('--headless')+$argv}
if($Suite -eq 'proof') {
    if(!$Rendered){throw 'Movie proof requires a rendered hardware renderer'}
    $argv+=@('--rendering-method','mobile','--rendering-driver','vulkan',
        '--write-movie',(Join-Path $caseRoot 'fall-damage-original.avi'))
}
$argv+=@('--',"--case-storage-root=$caseRoot")
$childEnv=@{DOTNET_ROOT='G:/godot/dotnet-sdk';DOTNET_ROOT_X64='G:/godot/dotnet-sdk'}
$process=Start-Process -FilePath 'G:/godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe' `
    -ArgumentList $argv -WorkingDirectory $projectRoot -WindowStyle Hidden -Environment $childEnv `
    -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
try {
    if(!$process.WaitForExit(55000)) {
        $process.Kill()
        throw "Owned test timed out; only PID $($process.Id) stopped"
    }
    $process.Refresh()
    $combined=(Get-Content -LiteralPath $stdout -Raw)+(Get-Content -LiteralPath $stderr -Raw)
    $warnings=@([regex]::Matches($combined,'(?m)^WARNING:.*') | ForEach-Object {$_.Value.TrimEnd()})
    $expectedWarning='WARNING: MechanicalClawController isolated a throwing TargetResidencyChanged observer: intentional residency observer failure'
    $unexpectedWarning=$warnings.Count -gt 0
    if($Suite -eq 'auto-pull' -and $warnings.Count -eq 2 -and
        @($warnings | Where-Object {$_ -cne $expectedWarning}).Count -eq 0) {
        $unexpectedWarning=$false
        Write-Output 'EXPECTED_RESIDENCY_TEST_WARNINGS=2'
    }
    if((Get-ProtectedInventory) -cne $before){throw 'Protected player saves/playtests/logs changed'}
    Get-Content -LiteralPath $stdout
    Get-Content -LiteralPath $stderr
    Write-Output "EVIDENCE_CASE=$caseRoot"
    Write-Output "PROTECTED_USERDATA_UNCHANGED=True"
    if($ExpectRed) {
        if($process.ExitCode -eq 0 -or $combined -notmatch 'Assertion failed:') {
            throw 'Expected a behavioral assertion failure, not success/compiler/launch failure'
        }
        Write-Output 'FALL_EXPECTED_RED_OK'
    } elseif($process.ExitCode -ne 0 -or $combined -notmatch [regex]::Escape($runner[1]) -or
        $combined -match '(?m)^ERROR:' -or $unexpectedWarning) {
        throw "Suite failed: $Suite; exit=$($process.ExitCode)"
    }
} finally {
    $process.Dispose()
}
