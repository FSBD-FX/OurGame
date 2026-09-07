param(
    [string]$SourcePath = (Join-Path $PSScriptRoot '..\integration\PrologueGuidanceRuntimeTests.cs'),
    [switch]$SkipMutation,
    [switch]$RunPostPhases
)

$ErrorActionPreference = 'Stop'

$script:ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$script:EvidenceRoot = Join-Path $script:ProjectRoot 'screenshots\task4-capture-exit'
$script:GodotPath = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$dotnetRoot = 'G:\godot\dotnet-sdk'
$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:Path = "$dotnetRoot;$env:Path"

function Assert-ContainsLiteral {
    param([string]$Text, [string]$Needle, [string]$Label)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Task4 guidance objective ordering requires $Label."
    }
}

function Invoke-Task4Godot {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$ConsolePath
    )

    $stdout = "$ConsolePath.stdout"
    $stderr = "$ConsolePath.stderr"
    $process = Start-Process -FilePath $script:GodotPath -ArgumentList $Arguments -Wait -PassThru `
        -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $lines = @(
        if (Test-Path -LiteralPath $stdout -PathType Leaf) { Get-Content -LiteralPath $stdout }
        if (Test-Path -LiteralPath $stderr -PathType Leaf) { Get-Content -LiteralPath $stderr }
    )
    [System.IO.File]::WriteAllLines($ConsolePath, [string[]]$lines,
        [System.Text.UTF8Encoding]::new($false))
    [pscustomobject]@{ ExitCode = $process.ExitCode; Lines = $lines }
}

function Get-ExactLineCount {
    param([Parameter(Mandatory)][string]$Text, [Parameter(Mandatory)][string]$Line)
    return ([regex]::Matches($Text, '(?m)^' + [regex]::Escape($Line) + '$')).Count
}

function Invoke-PostPhase {
    param([Parameter(Mandatory)][string]$Label)

    $timestamp = Get-Date -Format 'yyyyMMddHHmmss'
    $log = Join-Path $script:EvidenceRoot "post-timing-$Label-$timestamp.log"
    $console = Join-Path $script:EvidenceRoot "post-timing-$Label-$timestamp.console.log"
    $outer = Join-Path $script:EvidenceRoot "post-timing-$Label-$timestamp.outer.log"
    $result = Invoke-Task4Godot -Arguments @(
        '--headless', '--path', $script:ProjectRoot,
        '--script', 'res://tests/RunPrologueTests.cs', '--log-file', $log,
        '--', '--phase=post'
    ) -ConsolePath $console

    $combined = ((Get-Content -LiteralPath $log -Raw),
        (Get-Content -LiteralPath $console -Raw)) -join [Environment]::NewLine
    $drain = Get-ExactLineCount -Text $combined -Line 'PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=post'
    $phase = Get-ExactLineCount -Text $combined -Line 'PROLOGUE_TEST_PHASE_OK phase=post'
    $testsOk = Get-ExactLineCount -Text $combined -Line 'PROLOGUE_TESTS_OK'
    $fatal = [regex]::IsMatch($combined,
        '0xC000001D|ERROR: FATAL:|gchandle\.is_released\(\)|Handle is not initialized')
    $objective = [regex]::IsMatch($combined,
        'real checkpoint/tutorial/exit route reaches Upper during OpenShortcut')
    [System.IO.File]::WriteAllLines($outer, [string[]]@(
        "POST_TIMING_NATIVE_EXIT=$($result.ExitCode)",
        "POST_TIMING_MARKERS drain=$drain phase=$phase tests_ok=$testsOk",
        "POST_TIMING_FATAL=$fatal",
        "POST_TIMING_OBJECTIVE_ASSERTION=$objective",
        "POST_TIMING_LOG=$log",
        "POST_TIMING_CONSOLE=$console"
    ), [System.Text.UTF8Encoding]::new($false))
    if ($result.ExitCode -ne 0 -or $drain -ne 1 -or $phase -ne 1 -or $testsOk -ne 1 -or
        $fatal -or $objective) {
        throw "Task4 post timing $Label failed; inspect $outer"
    }
    "TASK4_GUIDANCE_OBJECTIVE_POST_GREEN label=$Label exit=$($result.ExitCode) drain=$drain phase=$phase tests_ok=$testsOk fatal=$fatal"
}

$source = [System.IO.Path]::GetFullPath($SourcePath)
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Task4 guidance objective ordering source is missing: $source"
}

$text = [System.IO.File]::ReadAllText($source)
$exitCall = 'await TouchExit(tree, level, "Exits/ToUpper", PrologueRoomId.UpperPendulum);'
$waitCall = 'await WaitForUpperObjective(tree, level);'
$waitMethod = 'private static async Task WaitForUpperObjective(SceneTree tree,'
Assert-ContainsLiteral -Text $text -Needle $exitCall -Label 'the real Upper exit'
Assert-ContainsLiteral -Text $text -Needle $waitCall -Label 'the Upper objective ProcessFrame wait'
Assert-ContainsLiteral -Text $text -Needle $waitMethod -Label 'the bounded Upper objective helper'
Assert-ContainsLiteral -Text $text -Needle 'SceneTree.SignalName.ProcessFrame' -Label 'a process-frame boundary'
Assert-ContainsLiteral -Text $text -Needle 'const int maxProcessFrames = 5;' -Label 'the five-frame bound'
Assert-ContainsLiteral -Text $text -Needle 'process_frames={processFrames}/{maxProcessFrames}' -Label 'frame/current diagnostics'

$exitIndex = $text.IndexOf($exitCall, [StringComparison]::Ordinal)
$waitIndex = $text.IndexOf($waitCall, $exitIndex + $exitCall.Length, [StringComparison]::Ordinal)
if ($waitIndex -lt $exitIndex) {
    throw 'Task4 guidance objective ordering must wait after the Upper exit, not before it.'
}

if (-not $SkipMutation) {
    $tempRoot = [System.IO.Path]::GetTempPath()
    $tempPath = Join-Path $tempRoot ("knight-task4-guidance-order-" + [Guid]::NewGuid().ToString('N') + '.cs')
    try {
        $mutant = $text.Replace("        $waitCall`r`n", '')
        if ($mutant -ceq $text) { $mutant = $text.Replace("        $waitCall`n", '') }
        if ($mutant -ceq $text) { throw 'Task4 guidance ordering mutation could not remove the wait call.' }
        [System.IO.File]::WriteAllText($tempPath, $mutant, [System.Text.UTF8Encoding]::new($false))

        $hostExe = (Get-Process -Id $PID).Path
        & $hostExe -NoProfile -File $PSCommandPath -SourcePath $tempPath -SkipMutation
        if ($LASTEXITCODE -eq 0) {
            throw 'Task4 guidance ordering deletion mutation unexpectedly passed.'
        }
        "TASK4_GUIDANCE_OBJECTIVE_WAIT_DELETE_RED_OBSERVED exit=$LASTEXITCODE"
    }
    finally {
        if (Test-Path -LiteralPath $tempPath) { Remove-Item -LiteralPath $tempPath -Force }
    }
}

if ($RunPostPhases) {
    Invoke-PostPhase -Label 'first'
    Invoke-PostPhase -Label 'second'
}

'TASK4_GUIDANCE_OBJECTIVE_ORDERING_OK'
