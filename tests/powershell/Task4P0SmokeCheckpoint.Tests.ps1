[CmdletBinding()]
param(
    [string]$SourcePath,
    [string]$VerifierPath,
    [switch]$SkipMutation
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $project 'tests\smoke\P0CoreSliceSmoke.cs'
}
if ([string]::IsNullOrWhiteSpace($VerifierPath)) {
    $VerifierPath = Join-Path $project 'tools\verify-p0.ps1'
}
$evidenceRoot = Join-Path $project 'screenshots\task4-capture-exit'
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$dotnet = 'G:\godot\dotnet-sdk\dotnet.exe'
$env:DOTNET_ROOT = 'G:\godot\dotnet-sdk'
$env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:Path = "$env:DOTNET_ROOT;$env:Path"
$taskPrefix = 'knight-task4-p0-smoke-checkpoint-'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

function Assert-ContainsLiteral([string]$Text, [string]$Needle, [string]$Label) {
    Assert-True ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) $Label
}

function Test-ExactBytes([byte[]]$Expected, [byte[]]$Actual) {
    if ($Expected.Length -ne $Actual.Length) { return $false }
    for ($index = 0; $index -lt $Expected.Length; $index++) {
        if ($Expected[$index] -ne $Actual[$index]) { return $false }
    }
    return $true
}

function Assert-TempProjectPath([string]$Path) {
    $absolute = [System.IO.Path]::GetFullPath($Path)
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/') +
        [System.IO.Path]::DirectorySeparatorChar
    Assert-True ($absolute.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) `
        "Temp project is beneath the system Temp root: $absolute"
    Assert-True ((Split-Path -Leaf $absolute).StartsWith($taskPrefix, [StringComparison]::Ordinal)) `
        "Temp project has the task-specific prefix: $absolute"
    return $absolute
}

function Copy-ProjectEntry {
    param([System.IO.FileSystemInfo]$Entry, [string]$DestinationDirectory)
    $excludedDirectories = @('.git', '.worktrees', 'screenshots', 'bin', 'obj')
    $excludedFiles = @('*.log', '*.avi', '*.mp4', '*.mov', '*.mkv', '*.manifest.txt')
    if ($Entry.PSIsContainer) {
        if ($excludedDirectories -ccontains $Entry.Name) { return }
        $destination = Join-Path $DestinationDirectory $Entry.Name
        New-Item -ItemType Directory -Path $destination -Force -ErrorAction Stop | Out-Null
        foreach ($child in Get-ChildItem -LiteralPath $Entry.FullName -Force) {
            Copy-ProjectEntry -Entry $child -DestinationDirectory $destination
        }
        return
    }
    if (@($excludedFiles | Where-Object { $Entry.Name -like $_ }).Count -gt 0) { return }
    Copy-Item -LiteralPath $Entry.FullName -Destination $DestinationDirectory -Force -ErrorAction Stop
}

function New-TempProjectCopy {
    param(
        [scriptblock]$CopyFailureInjection,
        [string]$CopyPrefix = $taskPrefix
    )
    Assert-True ($CopyPrefix.StartsWith($taskPrefix, [StringComparison]::Ordinal)) `
        "Temp copy prefix remains task-specific: $CopyPrefix"
    $copy = Assert-TempProjectPath (Join-Path ([System.IO.Path]::GetTempPath()) (
        $CopyPrefix + [Guid]::NewGuid().ToString('N')))
    try {
        New-Item -ItemType Directory -Path $copy -ErrorAction Stop | Out-Null
        $copiedEntryCount = 0
        foreach ($entry in Get-ChildItem -LiteralPath $project -Force) {
            Copy-ProjectEntry -Entry $entry -DestinationDirectory $copy
            $copiedEntryCount++
            if ($null -ne $CopyFailureInjection -and $copiedEntryCount -eq 1) {
                & $CopyFailureInjection
            }
        }
        Assert-True (Test-Path -LiteralPath (Join-Path $copy '.godot\imported') -PathType Container) `
            'Temp copy retains Godot imported cache'
        return $copy
    }
    catch {
        $copyFailure = $_
        $verified = Assert-TempProjectPath $copy
        if (Test-Path -LiteralPath $verified -PathType Container) {
            Remove-Item -LiteralPath $verified -Recurse -Force -ErrorAction Stop
            Assert-True (-not (Test-Path -LiteralPath $verified)) `
                "partial Temp project is removed before the copy failure is rethrown: $verified"
        }
        throw $copyFailure
    }
}

function Remove-TempProjectCopy([string]$Path) {
    $verified = Assert-TempProjectPath $Path
    Assert-True (Test-Path -LiteralPath $verified -PathType Container) "Temp project exists before cleanup: $verified"
    Remove-Item -LiteralPath $verified -Recurse -Force -ErrorAction Stop
    Assert-True (-not (Test-Path -LiteralPath $verified)) "Temp project is removed: $verified"
}

function Get-TaskTempProjectCount([string]$Prefix = $taskPrefix) {
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    return @(Get-ChildItem -LiteralPath $tempRoot -Force | Where-Object {
        $_.PSIsContainer -and $_.Name.StartsWith($Prefix, [StringComparison]::Ordinal)
    }).Count
}

function Assert-InjectedCopyFailureCleansInternally {
    $copyFailurePrefix = $taskPrefix + 'copy-failure-'
    $beforeCount = Get-TaskTempProjectCount $copyFailurePrefix
    Assert-True ($beforeCount -eq 0) `
        "isolated injected-copy prefix starts empty: prefix=$copyFailurePrefix count=$beforeCount"
    $observed = $false
    try {
        New-TempProjectCopy -CopyPrefix $copyFailurePrefix -CopyFailureInjection {
            throw 'TASK4_P0_SMOKE_INJECTED_COPY_FAILURE'
        } | Out-Null
    }
    catch {
        $observed = $_.Exception.Message.Contains('TASK4_P0_SMOKE_INJECTED_COPY_FAILURE')
    }
    Assert-True $observed 'injected partial-copy failure is rethrown by the copy helper'
    $afterCount = Get-TaskTempProjectCount $copyFailurePrefix
    Assert-True ($afterCount -eq 0) `
        "partial-copy cleanup is internal to the helper before any caller finally: prefix=$copyFailurePrefix after=$afterCount"
    Write-Output "TASK4_P0_SMOKE_COPY_FAILURE_INTERNAL_CLEANUP_OK before=$beforeCount after=$afterCount"
}

function Invoke-TempBuild([string]$TempProject, [switch]$AllowRestore) {
    $arguments = @('build', (Join-Path $TempProject 'Knight.csproj'))
    if (-not $AllowRestore) { $arguments += '--no-restore' }
    & $dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Temp checkpoint mutant build failed exit=$LASTEXITCODE" }
}

function Invoke-TempSmoke([string]$TempProject, [string]$Label, [switch]$Verbose) {
    $stamp = Get-Date -Format 'yyyyMMddHHmmss'
    $log = Join-Path $evidenceRoot "p0-smoke-checkpoint-$Label-$stamp.log"
    $console = "$log.console"
    $stdout = "$console.stdout"
    $stderr = "$console.stderr"
    $arguments = @('--headless')
    if ($Verbose) { $arguments += '--verbose' }
    $arguments += @('--path', $TempProject,
        '--script', 'res://tests/smoke/P0CoreSliceSmoke.cs', '--log-file', $log)
    $process = Start-Process -FilePath $godot -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $engineLines = if (Test-Path -LiteralPath $log -PathType Leaf) { Get-Content -LiteralPath $log } else { @() }
    $stdoutLines = if (Test-Path -LiteralPath $stdout -PathType Leaf) { Get-Content -LiteralPath $stdout } else { @() }
    $stderrLines = if (Test-Path -LiteralPath $stderr -PathType Leaf) { Get-Content -LiteralPath $stderr } else { @() }
    $lines = @($stdoutLines + $stderrLines)
    [System.IO.File]::WriteAllLines($console, [string[]]$lines, [System.Text.UTF8Encoding]::new($false))
    $all = (($engineLines -join [Environment]::NewLine), ($lines -join [Environment]::NewLine)) -join [Environment]::NewLine
    [pscustomobject]@{
        ExitCode = $process.ExitCode; Log = $log; Console = $console; Stdout = $stdout; Stderr = $stderr
        EngineText = $engineLines -join [Environment]::NewLine
        StdoutText = $stdoutLines -join [Environment]::NewLine
        StderrText = $stderrLines -join [Environment]::NewLine
        Output = $all
    }
}

function Get-ExactLineCount([string]$Text, [string]$Line) {
    return @($Text -split "`r?`n" | Where-Object { $_ -ceq $Line }).Count
}

function Assert-MutantRed([string]$Name, [scriptblock]$Mutate) {
    $tempProject = $null
    try {
        $tempProject = New-TempProjectCopy
        Invoke-TempBuild $tempProject -AllowRestore
        $mutantPath = Join-Path $tempProject 'tests\smoke\P0CoreSliceSmoke.cs'
        $before = [System.IO.File]::ReadAllBytes($mutantPath)
        $sourceText = [System.Text.Encoding]::UTF8.GetString($before)
        $mutated = & $Mutate $sourceText
        Assert-True ($mutated -cne $sourceText) "$Name changes the real smoke source"
        [System.IO.File]::WriteAllText($mutantPath, $mutated, [System.Text.UTF8Encoding]::new($false))
        Invoke-TempBuild $tempProject
        $run = Invoke-TempSmoke $tempProject $Name
        Assert-True ($run.ExitCode -ne 0) "$Name exits nonzero"
        Assert-True (-not $run.Output.Contains('P0_CORE_SLICE_OK')) "$Name never prints the smoke success marker"
        Write-Output "TASK4_P0_SMOKE_${Name}_RED_OBSERVED exit=$($run.ExitCode) log=$($run.Log) console=$($run.Console)"
        [System.IO.File]::WriteAllBytes($mutantPath, $before)
        $after = [System.IO.File]::ReadAllBytes($mutantPath)
        Assert-True (Test-ExactBytes -Expected $before -Actual $after) "$Name restores exact Temp source bytes"
    }
    finally {
        if ($null -ne $tempProject) { Remove-TempProjectCopy $tempProject }
    }
}

function Assert-StaticSourceMutantRed([string]$Name, [scriptblock]$Mutate) {
    $path = Join-Path ([System.IO.Path]::GetTempPath()) ($taskPrefix + [Guid]::NewGuid().ToString('N') + '.cs')
    $absolute = [System.IO.Path]::GetFullPath($path)
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/') +
        [System.IO.Path]::DirectorySeparatorChar
    Assert-True ($absolute.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) `
        "$Name static mutant is beneath the system Temp root: $absolute"
    Assert-True ((Split-Path -Leaf $absolute).StartsWith($taskPrefix, [StringComparison]::Ordinal)) `
        "$Name static mutant has the task-specific prefix: $absolute"
    try {
        $mutated = & $Mutate ([System.IO.File]::ReadAllText($source))
        Assert-True ($mutated -cne $text) "$Name changes the static smoke source"
        [System.IO.File]::WriteAllText($absolute, $mutated, [System.Text.UTF8Encoding]::new($false))
        & powershell -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -SourcePath $absolute -SkipMutation
        Assert-True ($LASTEXITCODE -ne 0) "$Name static source mutation exits nonzero"
        Write-Output "TASK4_P0_SMOKE_${Name}_STATIC_RED_OBSERVED exit=$LASTEXITCODE"
    }
    finally {
        if (Test-Path -LiteralPath $absolute -PathType Leaf) {
            Remove-Item -LiteralPath $absolute -Force -ErrorAction Stop
            Assert-True (-not (Test-Path -LiteralPath $absolute)) "$Name static mutant is removed"
        }
    }
}

function Assert-StaticVerifierMutantRed([string]$Name, [scriptblock]$Mutate) {
    $path = Join-Path ([System.IO.Path]::GetTempPath()) ($taskPrefix + [Guid]::NewGuid().ToString('N') + '.ps1')
    $absolute = [System.IO.Path]::GetFullPath($path)
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/') +
        [System.IO.Path]::DirectorySeparatorChar
    Assert-True ($absolute.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) `
        "$Name verifier mutant is beneath the system Temp root: $absolute"
    Assert-True ((Split-Path -Leaf $absolute).StartsWith($taskPrefix, [StringComparison]::Ordinal)) `
        "$Name verifier mutant has the task-specific prefix: $absolute"
    try {
        $mutated = & $Mutate ([System.IO.File]::ReadAllText($verifier))
        Assert-True ($mutated -cne $verifierText) "$Name changes the verifier source"
        [System.IO.File]::WriteAllText($absolute, $mutated, [System.Text.UTF8Encoding]::new($false))
        & powershell -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath `
            -SourcePath $source -VerifierPath $absolute -SkipMutation
        Assert-True ($LASTEXITCODE -ne 0) "$Name static verifier mutation exits nonzero"
        Write-Output "TASK4_P0_SMOKE_${Name}_STATIC_RED_OBSERVED exit=$LASTEXITCODE"
    }
    finally {
        if (Test-Path -LiteralPath $absolute -PathType Leaf) {
            Remove-Item -LiteralPath $absolute -Force -ErrorAction Stop
            Assert-True (-not (Test-Path -LiteralPath $absolute)) "$Name verifier mutant is removed"
        }
    }
}

function Assert-DeletedTeardownBehavioralRed([string]$TeardownCall) {
    $tempProject = $null
    try {
        $tempProject = New-TempProjectCopy
        Invoke-TempBuild $tempProject -AllowRestore
        $mutantPath = Join-Path $tempProject 'tests\smoke\P0CoreSliceSmoke.cs'
        $before = [System.IO.File]::ReadAllBytes($mutantPath)
        $sourceText = [System.Text.Encoding]::UTF8.GetString($before)
        $mutated = [regex]::Replace($sourceText, [regex]::Escape($TeardownCall), '', 1)
        Assert-True ($mutated -cne $sourceText) 'deleted teardown mutation changes exactly the success-boundary call'
        [System.IO.File]::WriteAllText($mutantPath, $mutated, [System.Text.UTF8Encoding]::new($false))
        Invoke-TempBuild $tempProject
        $run = Invoke-TempSmoke $tempProject 'TEARDOWN_DELETE_VERBOSE' -Verbose
        Assert-True ($run.ExitCode -eq 0) 'deleted teardown fixture reaches the engine exit boundary'
        Assert-True ((Get-ExactLineCount $run.EngineText 'P0_CORE_SLICE_OK') -eq 1) `
            'deleted teardown fixture emits the engine success marker once'
        Assert-True ((Get-ExactLineCount $run.StdoutText 'P0_CORE_SLICE_OK') -eq 1) `
            'deleted teardown fixture emits the stdout success marker once'
        $engineDrain = @($run.EngineText -split "`r?`n" | Where-Object {
            $_ -match '^PROLOGUE_AUDIO_DRAIN_OK ' }).Count
        $stdoutDrain = @($run.StdoutText -split "`r?`n" | Where-Object {
            $_ -match '^PROLOGUE_AUDIO_DRAIN_OK ' }).Count
        Assert-True ($engineDrain -eq 0 -and $stdoutDrain -eq 0) `
            'deleted teardown fixture has no success-boundary audio drain marker in either stream'
        Write-Output "TASK4_P0_SMOKE_TEARDOWN_DELETE_BEHAVIORAL_RED_OBSERVED exit=$($run.ExitCode) engine_drain=$engineDrain stdout_drain=$stdoutDrain log=$($run.Log) console=$($run.Console)"
        Write-Output "TASK4_P0_SMOKE_TEARDOWN_DELETE_NATIVE_LEAK_UNREPRODUCED exit=$($run.ExitCode) log=$($run.Log) console=$($run.Console)"
        [System.IO.File]::WriteAllBytes($mutantPath, $before)
        Assert-True (Test-ExactBytes -Expected $before -Actual ([System.IO.File]::ReadAllBytes($mutantPath))) `
            'deleted teardown mutation restores exact Temp source bytes'
    }
    finally {
        if ($null -ne $tempProject) { Remove-TempProjectCopy $tempProject }
    }
}

function Assert-NormalVerboseTeardownGreen {
    $tempProject = $null
    try {
        $tempProject = New-TempProjectCopy
        Invoke-TempBuild $tempProject -AllowRestore
        $run = Invoke-TempSmoke $tempProject 'TEARDOWN_VERBOSE_GREEN' -Verbose
        Assert-True ($run.ExitCode -eq 0) 'normal verbose teardown exits zero'
        Assert-True ((Get-ExactLineCount $run.EngineText 'P0_CORE_SLICE_OK') -eq 1) `
            'normal verbose teardown emits the engine success marker once'
        Assert-True ((Get-ExactLineCount $run.StdoutText 'P0_CORE_SLICE_OK') -eq 1) `
            'normal verbose teardown emits the stdout success marker once'
        $engineDrainLines = @($run.EngineText -split "`r?`n" | Where-Object {
            $_ -match '^PROLOGUE_AUDIO_DRAIN_OK ' })
        $stdoutDrainLines = @($run.StdoutText -split "`r?`n" | Where-Object {
            $_ -match '^PROLOGUE_AUDIO_DRAIN_OK ' })
        Assert-True ($engineDrainLines.Count -eq 1) 'normal verbose teardown emits one engine audio drain marker'
        Assert-True ($stdoutDrainLines.Count -eq 1) 'normal verbose teardown emits one stdout audio drain marker'
        Assert-True ($run.EngineText.IndexOf($engineDrainLines[0], [StringComparison]::Ordinal) -lt
            $run.EngineText.IndexOf('P0_CORE_SLICE_OK', [StringComparison]::Ordinal)) `
            'normal verbose teardown emits the engine drain marker before success'
        Assert-True ($run.StdoutText.IndexOf($stdoutDrainLines[0], [StringComparison]::Ordinal) -lt
            $run.StdoutText.IndexOf('P0_CORE_SLICE_OK', [StringComparison]::Ordinal)) `
            'normal verbose teardown emits the stdout drain marker before success'
        foreach ($forbidden in @('ObjectDB instances were leaked', 'resources still in use',
            'FATAL:', 'godotsharp_internal_refcounted_disposed', 'Unhandled exception', 'Handle')) {
            Assert-True (-not $run.Output.Contains($forbidden)) `
                "normal verbose teardown excludes '$forbidden'"
        }
        Write-Output "TASK4_P0_SMOKE_TEARDOWN_VERBOSE_GREEN exit=$($run.ExitCode) engine_drain=1 stdout_drain=1 log=$($run.Log) console=$($run.Console)"
    }
    finally {
        if ($null -ne $tempProject) { Remove-TempProjectCopy $tempProject }
    }
}

function Assert-ConsoleOnlyLeakFixtureRed {
    $fixtureLog = Join-Path ([System.IO.Path]::GetTempPath()) ($taskPrefix + [Guid]::NewGuid().ToString('N') + '.log')
    $fixtureConsole = "$fixtureLog.console"
    try {
        [System.IO.File]::WriteAllText($fixtureLog, "P0_CORE_SLICE_OK`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText($fixtureConsole,
            "ERROR: 2 resources still in use at exit.`n", [System.Text.UTF8Encoding]::new($false))
        . (Join-Path $project 'tools\EvidenceChecks.ps1')
        $observed = $false
        try {
            Assert-NoEngineLogErrors -Paths @($fixtureLog, $fixtureConsole)
        }
        catch {
            $observed = $_.Exception.Message.Contains('resources still in use')
        }
        Assert-True $observed 'console-only leak fixture fails the engine error contract'
        Write-Output 'TASK4_P0_SMOKE_CONSOLE_ONLY_LEAK_RED_OBSERVED'
    }
    finally {
        foreach ($path in @($fixtureLog, $fixtureConsole)) {
            if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path -Force -ErrorAction Stop }
        }
    }
}

$source = [System.IO.Path]::GetFullPath($SourcePath)
Assert-True (Test-Path -LiteralPath $source -PathType Leaf) "checkpoint smoke source exists: $source"
$text = [System.IO.File]::ReadAllText($source)
$verifier = [System.IO.Path]::GetFullPath($VerifierPath)
Assert-True (Test-Path -LiteralPath $verifier -PathType Leaf) "P0 verifier source exists: $verifier"
$verifierText = [System.IO.File]::ReadAllText($verifier)
$contractScriptText = [System.IO.File]::ReadAllText($PSCommandPath)
foreach ($requirement in @(
    'function New-TempProjectCopy {',
    '[scriptblock]$CopyFailureInjection',
    '[string]$CopyPrefix = $taskPrefix',
    'catch {',
    'Assert-TempProjectPath $copy',
    'TASK4_P0_SMOKE_INJECTED_COPY_FAILURE',
    'Assert-InjectedCopyFailureCleansInternally')) {
    Assert-ContainsLiteral $contractScriptText $requirement `
        'checkpoint contract requires guarded internal cleanup for a partial Temp-project copy'
}
$triggerLookup = 'CheckpointTrigger checkpoint = _level.CurrentRoom.GetNode<CheckpointTrigger>("Checkpoint");'
$fixtureAtTrigger = 'Fixture(checkpoint.GlobalPosition);'
$waitCall = 'await WaitForCorridorCheckpoint(checkpoint);'
Assert-ContainsLiteral $text $triggerLookup 'checkpoint smoke resolves the authored trigger from the mounted room'
Assert-ContainsLiteral $text $fixtureAtTrigger 'checkpoint smoke places the player at the real trigger position'
Assert-ContainsLiteral $text $waitCall 'checkpoint smoke waits for the real checkpoint state boundary'
Assert-ContainsLiteral $text 'private static readonly StringName CorridorCheckpointEntrance = "checkpoint";' `
    'checkpoint smoke owns the expected checkpoint entrance explicitly'
foreach ($diagnostic in @('trigger={trigger.GlobalPosition}', 'player={_player.GlobalPosition}',
    'actual={finalSnapshot.Room}/{finalSnapshot.Entrance}', 'frames={physicsFrames}/{maxPhysicsFrames}')) {
    Assert-ContainsLiteral $text $diagnostic 'checkpoint smoke emits trigger/player/state/frame timeout diagnostics'
}
Assert-True ($text.IndexOf('Fixture(new Vector2(960, 198));', [StringComparison]::Ordinal) -lt 0) `
    'checkpoint smoke no longer targets stale coordinate (960,198)'

$traversalLookup = 'TraversalSegment2D segment = _level.CurrentRoom.GetNode<TraversalSegment2D>('
$traversalPath = '"TraversalContracts/entry_to_anchor_landing");'
$anchorLookup = 'ThreadTarget2D anchor = _level.CurrentRoom.GetNode<ThreadTarget2D>(segment.AssistTargetPath);'
$launchFixture = 'Fixture(_level.CurrentRoom.ToGlobal(segment.LaunchZone.GetCenter()));'
$previewRelease = 'if (!threadReleased && cast.State == ThreadCastState.Preview)'
$targetAssertion = 'TestAssert.Equal(anchor.GetInstanceId(), applied.TargetId,'
foreach ($requirement in @(
    $traversalLookup, $traversalPath, $anchorLookup, $launchFixture, $previewRelease,
    'ThreadCastState.Preview', 'ThreadCastState.Apply', $targetAssertion,
    'segment.LandingZone.HasPoint(_level.CurrentRoom.ToLocal(_player.GlobalPosition))',
    'distance={distance:0.0}', 'tap={ability.Tuning.TapAssistRange:0.0}',
    'hold={ability.Tuning.HoldRange:0.0}', 'aim={aim}', 'state={cast.State}',
    'failure={cast.Failure}', 'frames={routeFrames}/{ThreadRouteBudget}',
    'player={_player.GlobalPosition}', 'anchor={anchor.GlobalPosition}')) {
    Assert-ContainsLiteral $text $requirement 'thread smoke follows the authored traversal contract with bounded diagnostics'
}
Assert-True ($text.IndexOf('Fixture(new Vector2(210, 68));', [StringComparison]::Ordinal) -lt 0) `
    'thread smoke no longer uses the stale launch coordinate'

$completionLookup = 'RoomExit completion = _level.CurrentRoom.GetNode<RoomExit>("Exits/P0Complete");'
$completionFixture = 'Fixture(completion.GlobalPosition);'
$completionEntrance = 'Marker2D checkpoint = _level.CurrentRoom.GetNode<Marker2D>("Entrances/checkpoint");'
foreach ($requirement in @($completionLookup, $completionFixture, $completionEntrance,
    '_level.Player.GlobalPosition.DistanceTo(checkpoint.GlobalPosition) < 5f')) {
    Assert-ContainsLiteral $text $requirement 'completion smoke resolves the authored exit and destination entrance'
}
Assert-True ($text.IndexOf('Fixture(new Vector2(1570, 190));', [StringComparison]::Ordinal) -lt 0) `
    'completion smoke no longer uses the stale exit coordinate'
Assert-True ($text.IndexOf('GlobalPosition.X - 960', [StringComparison]::Ordinal) -lt 0) `
    'completion smoke no longer uses the stale destination coordinate'

$teardownCall = 'await TeardownLevel();'
Assert-True (([regex]::Matches($text, [regex]::Escape($teardownCall))).Count -eq 2) `
    'P0 smoke tears down the mounted level on both success and failure boundaries'
foreach ($requirement in @(
    'private async Task TeardownLevel()',
    'if (_level != null && GodotObject.IsInstanceValid(_level))',
    'await TestSceneTeardown.FreeSceneAfterAudioDrain(this, _level);',
    '_level = null;',
    '_player = null;',
    'GD.Print("P0_CORE_SLICE_OK");')) {
    Assert-ContainsLiteral $text $requirement 'P0 smoke frees its mounted level before terminal success'
}

$expectedWarnings = @(
    'WARNING: PresentationProfile: VisualScene root must implement IEntityVisual2D for VisualRoot.',
    "WARNING: PresentationProfile: cue 'Hurt' root must be Node2D.",
    "WARNING: RoomPresentationProfile: VisualScene root must be Node2D for 'VisualRoot'.",
    "WARNING: Guidance target 'MechanismBossExit' is missing in room 'BellChainMechanism'.",
    'WARNING: Rejected prologue flow transition Completed->Recovering.'
)
foreach ($warning in $expectedWarnings) {
    Assert-ContainsLiteral $verifierText $warning 'P0 verifier declares every intentional phase warning exactly'
}
foreach ($requirement in @(
    '$testsPreConsole = Join-Path $project',
    '$testsPostConsole = Join-Path $project',
    '$testsConsole = Join-Path $project',
    'Join-PhaseLogsDeterministically -OutputPath $testsConsole',
    'Assert-EngineLogContract -Path $testsLog `',
    'Assert-EngineLogContract -Path $testsConsole `',
    '-ExpectedWarnings $intentionalNegativeTestWarnings',
    '$smokeConsole = Join-Path $project',
    'Assert-ExactLogMarker -Path $smokeConsole -Marker ''P0_CORE_SLICE_OK''',
    'Invoke-MergedNativeCommand -FilePath $godot')) {
    Assert-ContainsLiteral $verifierText $requirement 'P0 verifier captures and validates native console output fail-closed'
}

if (-not $SkipMutation) {
    Assert-MutantRed -Name 'CHECKPOINT_LOOKUP_DELETE' -Mutate {
        param([string]$SourceText)
        $SourceText.Replace($triggerLookup, 'CheckpointTrigger checkpoint = null;')
    }
    Assert-MutantRed -Name 'CHECKPOINT_ENTRANCE_TAMPER' -Mutate {
        param([string]$SourceText)
        $SourceText.Replace('private static readonly StringName CorridorCheckpointEntrance = "checkpoint";',
            'private static readonly StringName CorridorCheckpointEntrance = "wrong_entrance";')
    }
    Assert-MutantRed -Name 'THREAD_EARLY_RELEASE' -Mutate {
        param([string]$SourceText)
        $SourceText.Replace($previewRelease, 'if (!threadReleased && routeFrames == 2)')
    }
    Assert-MutantRed -Name 'THREAD_TARGET_ID_TAMPER' -Mutate {
        param([string]$SourceText)
        $SourceText.Replace($targetAssertion, 'TestAssert.Equal(0UL, applied.TargetId,')
    }
    Assert-StaticSourceMutantRed -Name 'COMPLETION_EXIT_LOOKUP_DELETE' -Mutate {
        param([string]$SourceText)
        $SourceText.Replace($completionLookup, 'RoomExit completion = null;')
    }
    Assert-StaticSourceMutantRed -Name 'COMPLETION_ENTRANCE_LOOKUP_DELETE' -Mutate {
        param([string]$SourceText)
        $SourceText.Replace($completionEntrance, 'Marker2D checkpoint = null;')
    }
    Assert-DeletedTeardownBehavioralRed $teardownCall
    Assert-NormalVerboseTeardownGreen
    Assert-ConsoleOnlyLeakFixtureRed
    Assert-StaticVerifierMutantRed -Name 'VERIFIER_EXPECTED_WARNING_DELETE' -Mutate {
        param([string]$VerifierText)
        $VerifierText.Replace('-ExpectedWarnings $intentionalNegativeTestWarnings', '')
    }
    Assert-StaticVerifierMutantRed -Name 'VERIFIER_TEST_CONSOLE_SCAN_DELETE' -Mutate {
        param([string]$VerifierText)
        $VerifierText.Replace('Assert-EngineLogContract -Path $testsConsole `',
            '# deleted console scan `')
    }
}

Assert-InjectedCopyFailureCleansInternally

Write-Output 'TASK4_P0_SMOKE_CHECKPOINT_CONTRACT_OK'
