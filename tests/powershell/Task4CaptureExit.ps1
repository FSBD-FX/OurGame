[CmdletBinding()]
param(
    [string]$GodotPath = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe',
    [switch]$SkipGreenCapture,
    [switch]$OnlyMarkerOrder,
    [switch]$OnlyFailurePath,
    [switch]$OnlyMutationIsolation,
    [switch]$OnlyPhaseExitBoundary
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $project 'tools\EvidenceChecks.ps1')
$dotnet = 'G:\godot\dotnet-sdk\dotnet.exe'
$evidenceRoot = Join-Path $project 'screenshots\task4-capture-exit'
$task4TempPrefix = 'knight-task4-capture-mutation-'
New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

function Get-ByteSha256 {
    param([Parameter(Mandatory)][byte[]]$Bytes)

    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try { return (([BitConverter]::ToString($algorithm.ComputeHash($Bytes)) -replace '-', '')) }
    finally { $algorithm.Dispose() }
}

function Assert-ExactBytes {
    param(
        [Parameter(Mandatory)][byte[]]$Expected,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label
    )

    [byte[]]$actual = [System.IO.File]::ReadAllBytes($Path)
    if ($actual.Length -ne $Expected.Length) {
        throw "$Label byte length changed: expected $($Expected.Length), actual $($actual.Length)."
    }
    for ($index = 0; $index -lt $Expected.Length; $index++) {
        if ($actual[$index] -ne $Expected[$index]) {
            throw "$Label byte mismatch at offset $index."
        }
    }
}

function Invoke-Task4Build {
    param(
        [Parameter(Mandatory)][string]$ProjectRoot,
        [switch]$AllowRestore
    )

    $arguments = @('build', (Join-Path $ProjectRoot 'Knight.csproj'))
    if (-not $AllowRestore) { $arguments += '--no-restore' }
    & $dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Task 4 focused build failed exit=$LASTEXITCODE root=$ProjectRoot." }
}

function Invoke-Task4Godot {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$ConsolePath
    )

    $stdout = "$ConsolePath.stdout"
    $stderr = "$ConsolePath.stderr"
    $process = Start-Process -FilePath $GodotPath -ArgumentList $Arguments -Wait -PassThru `
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
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Line)

    return @((Get-Content -LiteralPath $Path) | Where-Object { $_ -ceq $Line }).Count
}

function Assert-NoTask4NativeFatal {
    param([Parameter(Mandatory)][string[]]$Paths)

    $output = ($Paths | ForEach-Object {
        if (Test-Path -LiteralPath $_ -PathType Leaf) { Get-Content -LiteralPath $_ -Raw }
    }) -join [Environment]::NewLine
    foreach ($signature in @('0xC000001D', 'ERROR: FATAL:', 'gchandle.is_released()')) {
        if ($output.Contains($signature)) {
            throw "Task 4 phase process emitted native-fatal signature '$signature'."
        }
    }
}

function Assert-Task4TempProjectPath {
    param([Parameter(Mandatory)][string]$Path)

    $absolutePath = [System.IO.Path]::GetFullPath($Path)
    $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd('\', '/') +
        [System.IO.Path]::DirectorySeparatorChar
    if (-not $absolutePath.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Task 4 mutation path is outside the system Temp root: $absolutePath"
    }
    $leaf = Split-Path -Leaf $absolutePath.TrimEnd('\', '/')
    if (-not $leaf.StartsWith($task4TempPrefix, [System.StringComparison]::Ordinal)) {
        throw "Task 4 mutation path lacks required task prefix '$task4TempPrefix': $absolutePath"
    }
    return $absolutePath
}

function Remove-Task4TempProjectCopy {
    param([Parameter(Mandatory)][string]$Path)

    $verifiedPath = Assert-Task4TempProjectPath -Path $Path
    if (-not (Test-Path -LiteralPath $verifiedPath -PathType Container)) {
        throw "Task 4 mutation cleanup target is missing: $verifiedPath"
    }
    Remove-Item -LiteralPath $verifiedPath -Recurse -Force -ErrorAction Stop
    if (Test-Path -LiteralPath $verifiedPath) {
        throw "Task 4 mutation cleanup left its verified Temp copy behind: $verifiedPath"
    }
    "TASK4_MUTATION_TEMP_COPY_REMOVED path=$verifiedPath"
}

function Copy-Task4MutationEntry {
    param(
        [Parameter(Mandatory)][System.IO.FileSystemInfo]$Entry,
        [Parameter(Mandatory)][string]$DestinationDirectory,
        [Parameter(Mandatory)][string[]]$ExcludedDirectories,
        [Parameter(Mandatory)][string[]]$ExcludedFilePatterns
    )

    if ($Entry.PSIsContainer) {
        if ($ExcludedDirectories -contains $Entry.Name) { return }
        $destination = Join-Path $DestinationDirectory $Entry.Name
        New-Item -ItemType Directory -Path $destination -Force -ErrorAction Stop | Out-Null
        foreach ($child in Get-ChildItem -LiteralPath $Entry.FullName -Force) {
            Copy-Task4MutationEntry -Entry $child -DestinationDirectory $destination `
                -ExcludedDirectories $ExcludedDirectories -ExcludedFilePatterns $ExcludedFilePatterns
        }
        return
    }
    if (@($ExcludedFilePatterns | Where-Object { $Entry.Name -like $_ }).Count -gt 0) { return }
    Copy-Item -LiteralPath $Entry.FullName -Destination $DestinationDirectory -Force -ErrorAction Stop
}

function New-Task4MutationProjectCopy {
    $tempCopy = Join-Path ([System.IO.Path]::GetTempPath()) ($task4TempPrefix + [guid]::NewGuid().ToString('N'))
    $verifiedCopy = Assert-Task4TempProjectPath -Path $tempCopy
    if (Test-Path -LiteralPath $verifiedCopy) {
        throw "Task 4 mutation Temp copy already exists: $verifiedCopy"
    }

    $excludedDirectories = @('.git', '.worktrees', 'screenshots', 'bin', 'obj')
    $excludedFilePatterns = @('*.log', '*.avi', '*.mp4', '*.mov', '*.mkv', '*.manifest.txt')
    try {
        New-Item -ItemType Directory -Path $verifiedCopy -ErrorAction Stop | Out-Null
        foreach ($entry in Get-ChildItem -LiteralPath $project -Force) {
            Copy-Task4MutationEntry -Entry $entry -DestinationDirectory $verifiedCopy `
                -ExcludedDirectories $excludedDirectories -ExcludedFilePatterns $excludedFilePatterns
        }
        foreach ($relativePath in @(
            'Knight.csproj', 'project.godot', 'tests\presentation\ProloguePresentation.cs',
            'tests\TestSceneTeardown.cs', 'tests\RunTask4CaptureTeardownTests.cs')) {
            $copyPath = Join-Path $verifiedCopy $relativePath
            if (-not (Test-Path -LiteralPath $copyPath -PathType Leaf)) {
                throw "Task 4 mutation copy omitted required project file: $relativePath"
            }
            Assert-ExactBytes -Expected ([System.IO.File]::ReadAllBytes((Join-Path $project $relativePath))) `
                -Path $copyPath -Label "Task 4 mutation copy $relativePath"
        }
        $knownAudioImport = Get-ChildItem -LiteralPath (Join-Path $project '.godot\imported') `
            -File -Filter 'preview.wav-*.sample' | Select-Object -First 1
        if ($null -eq $knownAudioImport) {
            throw 'Task 4 mutation copy could not locate the known preview.wav imported audio sample.'
        }
        $importRelativePath = $knownAudioImport.FullName.Substring($project.Length).TrimStart('\', '/')
        $copyAudioImport = Join-Path $verifiedCopy $importRelativePath
        if (-not (Test-Path -LiteralPath $copyAudioImport -PathType Leaf)) {
            throw "Task 4 mutation copy omitted known imported audio sample: $importRelativePath"
        }
        Assert-ExactBytes -Expected ([System.IO.File]::ReadAllBytes($knownAudioImport.FullName)) `
            -Path $copyAudioImport -Label 'Task 4 mutation imported preview.wav sample'
        return $verifiedCopy
    }
    catch {
        $copyFailure = $_.Exception.Message
        if (Test-Path -LiteralPath $verifiedCopy) {
            try { Remove-Task4TempProjectCopy -Path $verifiedCopy | Out-Null }
            catch { throw "Task 4 mutation copy failed: $copyFailure; cleanup also failed: $($_.Exception.Message)" }
        }
        throw "Task 4 mutation copy failed: $copyFailure"
    }
}

function Assert-KnownNativeTeardownCrash {
    param(
        [Parameter(Mandatory)][int]$ExitCode,
        [Parameter(Mandatory)][string[]]$LogPaths
    )

    $output = ($LogPaths | ForEach-Object {
        if (Test-Path -LiteralPath $_ -PathType Leaf) { Get-Content -LiteralPath $_ -Raw }
    }) -join [Environment]::NewLine
    $knownIllegalInstruction = $ExitCode -eq -1073741795 -or
        $output -match '(?i)(?:0xC000001D|-1073741795)'
    $managedNativeFinalize = $output -match
        '(?s)GodotObject\.Finalize.*godotsharp_internal_refcounted_disposed'
    if (-not ($knownIllegalInstruction -or $managedNativeFinalize)) {
        throw "No-drain capture failed nonzero ($ExitCode) without the expected native teardown crash signature."
    }
}

function Invoke-NoDrainMovieDiagnostic {
    param([Parameter(Mandatory)][string]$MutantProject)

    $mutantSourcePath = Join-Path $MutantProject 'tests\presentation\ProloguePresentation.cs'
    [byte[]]$snapshot = [System.IO.File]::ReadAllBytes($mutantSourcePath)
    $snapshotHash = Get-ByteSha256 -Bytes $snapshot
    $timestamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
    $movie = Join-Path $evidenceRoot "no-drain-prologue-$timestamp.avi"
    $log = Join-Path $evidenceRoot "no-drain-prologue-$timestamp.log"
    $console = Join-Path $evidenceRoot "no-drain-prologue-$timestamp.console.log"
    try {
        $source = [System.Text.Encoding]::UTF8.GetString($snapshot)
        $removed = [regex]::Replace($source,
            '(?m)^\s*TestSceneTeardown\.DrainManagedWrappersBeforeQuit\("prologue"\);\r?\n',
            [string]::Empty)
        if ($removed -ceq $source) { throw 'No-drain mutation did not remove the real Prologue success-path drain.' }
        [System.IO.File]::WriteAllText($mutantSourcePath, $removed, [System.Text.UTF8Encoding]::new($false))
        Invoke-Task4Build -ProjectRoot $MutantProject
        $startedUtc = [DateTime]::UtcNow
        $result = Invoke-Task4Godot -Arguments @(
            '--path', $MutantProject, '--fixed-fps', '60', '--write-movie', $movie,
            '--script', 'res://tests/presentation/ProloguePresentation.cs', '--log-file', $log
        ) -ConsolePath $console
        $result.Lines | ForEach-Object { $_ }
        Assert-FreshNonEmptyOutputs -Paths @($movie, $log, $console) -StartedUtc $startedUtc
        if ((Get-ExactLineCount -Path $log -Line 'PROLOGUE_PRESENTATION_COMPLETE frame=2989') -ne 1) {
            throw 'No-drain capture did not reach the actual 2989-frame completion boundary.'
        }
        if ((Get-ExactLineCount -Path $log -Line 'PROLOGUE_CAPTURE_TEARDOWN_OK capture=prologue') -ne 0) {
            throw 'No-drain capture unexpectedly emitted the post-drain success marker.'
        }
        if ($result.ExitCode -eq 0) {
            Assert-NoEngineLogErrors -Paths @($log, $console)
            "TASK4_NO_DRAIN_NATIVE_CRASH_UNREPRODUCED exit=0 completion=1 teardown=0 native_process=clean log=$log console=$console movie=$movie"
        }
        else {
            Assert-KnownNativeTeardownCrash -ExitCode $result.ExitCode -LogPaths @($log, $console)
            "TASK4_NO_DRAIN_NATIVE_CRASH_REPRODUCED exit=$($result.ExitCode) completion=1 teardown=0 log=$log console=$console movie=$movie"
        }
    }
    finally {
        [System.IO.File]::WriteAllBytes($mutantSourcePath, $snapshot)
        Assert-ExactBytes -Expected $snapshot -Path $mutantSourcePath -Label 'isolated ProloguePresentation restoration'
        $restoredHash = Get-ByteSha256 -Bytes ([System.IO.File]::ReadAllBytes($mutantSourcePath))
        if ($restoredHash -cne $snapshotHash) {
            throw "Isolated ProloguePresentation restoration failed: expected $snapshotHash actual $restoredHash."
        }
        Invoke-Task4Build -ProjectRoot $MutantProject
        "TASK4_NO_DRAIN_MUTANT_SOURCE_RESTORED sha256=$restoredHash path=$mutantSourcePath"
    }
}

function Invoke-MarkerOrderRed {
    param([Parameter(Mandatory)][string]$MutantProject)

    $mutantSourcePath = Join-Path $MutantProject 'tests\TestSceneTeardown.cs'
    [byte[]]$snapshot = [System.IO.File]::ReadAllBytes($mutantSourcePath)
    $snapshotHash = Get-ByteSha256 -Bytes $snapshot
    $timestamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
    $log = Join-Path $evidenceRoot "marker-before-finalizer-$timestamp.log"
    $console = Join-Path $evidenceRoot "marker-before-finalizer-$timestamp.console.log"
    $expected = @'
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        postFinalizersDrained?.Invoke();
        successMarker($"PROLOGUE_CAPTURE_TEARDOWN_OK capture={capture}");
'@
    $mutated = @'
        GC.Collect();
        successMarker($"PROLOGUE_CAPTURE_TEARDOWN_OK capture={capture}");
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        postFinalizersDrained?.Invoke();
'@
    try {
        $source = [System.Text.Encoding]::UTF8.GetString($snapshot)
        $changed = $source.Replace($expected, $mutated)
        if ($changed -ceq $source) {
            throw 'Marker-order mutation could not locate the reviewed post-finalizer teardown sequence.'
        }
        [System.IO.File]::WriteAllText($mutantSourcePath, $changed, [System.Text.UTF8Encoding]::new($false))
        Invoke-Task4Build -ProjectRoot $MutantProject
        $result = Invoke-Task4Godot -Arguments @(
            '--headless', '--path', $MutantProject,
            '--script', 'res://tests/RunTask4CaptureTeardownTests.cs', '--log-file', $log
        ) -ConsolePath $console
        $result.Lines | ForEach-Object { $_ }
        if ($result.ExitCode -eq 0) {
            throw 'Marker-before-finalizer mutation exited zero; the behavioral ordering probe did not fail.'
        }
        $markerLine = 'PROLOGUE_CAPTURE_TEARDOWN_OK capture=finalizer-order'
        $markerIndex = [Array]::IndexOf(@(Get-Content -LiteralPath $log), $markerLine)
        $finalizerIndex = [Array]::IndexOf(@(Get-Content -LiteralPath $log),
            'TASK4_TEARDOWN_FINALIZER_OBSERVED count=1')
        if ($markerIndex -lt 0 -or ($finalizerIndex -ge 0 -and $finalizerIndex -lt $markerIndex)) {
            throw 'Marker-before-finalizer mutation did not expose the runtime marker-before-finalizer ordering.'
        }
        if ((Get-ExactLineCount -Path $log -Line 'TASK4_TEARDOWN_ORDER_OK sequence=finalizer,marker finalizers=1') -ne 0) {
            throw 'Marker-before-finalizer mutation incorrectly emitted the ordering success marker.'
        }
        "TASK4_MARKER_ORDER_RED_OBSERVED exit=$($result.ExitCode) marker_index=$markerIndex finalizer_index=$finalizerIndex log=$log console=$console"
    }
    finally {
        [System.IO.File]::WriteAllBytes($mutantSourcePath, $snapshot)
        Assert-ExactBytes -Expected $snapshot -Path $mutantSourcePath -Label 'isolated TestSceneTeardown restoration'
        $restoredHash = Get-ByteSha256 -Bytes ([System.IO.File]::ReadAllBytes($mutantSourcePath))
        if ($restoredHash -cne $snapshotHash) {
            throw "Isolated TestSceneTeardown restoration failed: expected $snapshotHash actual $restoredHash."
        }
        Invoke-Task4Build -ProjectRoot $MutantProject
        "TASK4_MARKER_ORDER_MUTANT_SOURCE_RESTORED sha256=$restoredHash path=$mutantSourcePath"
    }
}

function Invoke-MarkerOrderGreen {
    param([Parameter(Mandatory)][string]$ProjectRoot)

    $timestamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
    $log = Join-Path $evidenceRoot "marker-order-green-$timestamp.log"
    $console = Join-Path $evidenceRoot "marker-order-green-$timestamp.console.log"
    $result = Invoke-Task4Godot -Arguments @(
        '--headless', '--path', $ProjectRoot,
        '--script', 'res://tests/RunTask4CaptureTeardownTests.cs', '--log-file', $log
    ) -ConsolePath $console
    $result.Lines | ForEach-Object { $_ }
    if ($result.ExitCode -ne 0) { throw "Marker-order GREEN exited $($result.ExitCode)." }
    $expected = @(
        'TASK4_TEARDOWN_FINALIZER_OBSERVED count=1',
        'PROLOGUE_CAPTURE_TEARDOWN_OK capture=finalizer-order',
        'TASK4_TEARDOWN_ORDER_OK sequence=finalizer,marker finalizers=1',
        'TASK4_TEARDOWN_ORDER_TESTS_OK')
    $lines = @(Get-Content -LiteralPath $log)
    $indices = @($expected | ForEach-Object { [Array]::IndexOf([string[]]$lines, $_) })
    if (@($indices | Where-Object { $_ -lt 0 }).Count -ne 0 -or $indices[0] -ge $indices[1]) {
        throw "Marker-order GREEN did not emit finalizer before teardown marker: indices=$($indices -join ',')."
    }
    "TASK4_MARKER_ORDER_GREEN exit=0 indices=$($indices -join ',') log=$log console=$console"
}

function Invoke-IsolatedMutationBoundaries {
    param(
        [switch]$RunNoDrain,
        [switch]$RunMarkerOrder
    )

    $tempProject = $null
    try {
        $tempProject = New-Task4MutationProjectCopy
        "TASK4_MUTATION_TEMP_COPY_CREATED path=$tempProject includes=.godot excludes=.git,.worktrees,screenshots,bin,obj,large-local-logs"
        Invoke-Task4Build -ProjectRoot $tempProject -AllowRestore
        if ($RunNoDrain) { Invoke-NoDrainMovieDiagnostic -MutantProject $tempProject }
        if ($RunMarkerOrder) {
            Invoke-MarkerOrderRed -MutantProject $tempProject
            Invoke-MarkerOrderGreen -ProjectRoot $tempProject
        }
    }
    finally {
        if ($null -ne $tempProject) { Remove-Task4TempProjectCopy -Path $tempProject }
    }
    'TASK4_MUTATION_ISOLATION_OK'
}

function Invoke-Task4PrologueTests {
    param(
        [Parameter(Mandatory)][string]$ProjectRoot,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][ValidateSet('pre', 'post')][string]$Phase
    )

    $timestamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
    $log = Join-Path $evidenceRoot "$Label-$timestamp.log"
    $console = Join-Path $evidenceRoot "$Label-$timestamp.console.log"
    $result = Invoke-Task4Godot -Arguments @(
        '--headless', '--path', $ProjectRoot,
        '--script', 'res://tests/RunPrologueTests.cs', '--log-file', $log,
        '--', "--phase=$Phase"
    ) -ConsolePath $console
    $result.Lines | ForEach-Object { $_ }
    return [pscustomobject]@{ Result = $result; Log = $log; Console = $console }
}

function Invoke-PhaseExitDrainDeleteRed {
    param([Parameter(Mandatory)][string]$MutantProject)

    $mutantSourcePath = Join-Path $MutantProject 'tests\RunPrologueTests.cs'
    [byte[]]$snapshot = [System.IO.File]::ReadAllBytes($mutantSourcePath)
    $snapshotHash = Get-ByteSha256 -Bytes $snapshot
    try {
        $source = [System.Text.Encoding]::UTF8.GetString($snapshot)
        $changed = [regex]::Replace($source,
            '(?m)^\s*TestSceneTeardown\.DrainManagedWrappersAtProcessExit\("pre"\);\r?\n',
            [string]::Empty)
        if ($changed -ceq $source) { throw 'Phase-exit delete mutation did not remove the actual pre drain.' }
        [System.IO.File]::WriteAllText($mutantSourcePath, $changed, [System.Text.UTF8Encoding]::new($false))
        Invoke-Task4Build -ProjectRoot $MutantProject
        $run = Invoke-Task4PrologueTests -ProjectRoot $MutantProject -Label 'phase-exit-delete-red' -Phase 'pre'
        $markerCount = Get-ExactLineCount -Path $run.Log -Line 'PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=pre'
        if ($markerCount -ne 0) { throw "Phase-exit delete mutation emitted pre marker count=$markerCount." }
        $phaseOk = Get-ExactLineCount -Path $run.Log -Line 'PROLOGUE_TEST_PHASE_OK phase=pre'
        $output = ((Get-Content -LiteralPath $run.Log -Raw), (Get-Content -LiteralPath $run.Console -Raw)) -join [Environment]::NewLine
        $nativeFatal = $output.Contains('0xC000001D') -or $output.Contains('ERROR: FATAL:')
        "TASK4_PHASE_EXIT_DELETE_RED_OBSERVED child_exit=$($run.Result.ExitCode) marker=0 phase_ok=$phaseOk native_fatal=$nativeFatal log=$($run.Log) console=$($run.Console)"
    }
    finally {
        [System.IO.File]::WriteAllBytes($mutantSourcePath, $snapshot)
        Assert-ExactBytes -Expected $snapshot -Path $mutantSourcePath -Label 'isolated RunPrologueTests phase-exit delete restoration'
        $restoredHash = Get-ByteSha256 -Bytes ([System.IO.File]::ReadAllBytes($mutantSourcePath))
        if ($restoredHash -cne $snapshotHash) {
            throw "Isolated RunPrologueTests phase-exit delete restoration failed: expected $snapshotHash actual $restoredHash."
        }
        Invoke-Task4Build -ProjectRoot $MutantProject
        "TASK4_PHASE_EXIT_DELETE_MUTANT_SOURCE_RESTORED sha256=$restoredHash path=$mutantSourcePath"
    }
}

function Invoke-PhaseExitDrainEarlyMarkerRed {
    param([Parameter(Mandatory)][string]$MutantProject)

    $mutantSourcePath = Join-Path $MutantProject 'tests\TestSceneTeardown.cs'
    [byte[]]$snapshot = [System.IO.File]::ReadAllBytes($mutantSourcePath)
    $snapshotHash = Get-ByteSha256 -Bytes $snapshot
    $expected = @'
        DrainManagedWrappersCore();
        int finalized = ManagedWrapperProcessExitFinalizerProbe.Finalized;
        if (finalized != 1)
            throw new InvalidOperationException(
                $"managed wrapper process-exit probe expected 1 finalizer, observed {finalized}");
        GD.Print($"TASK4_MANAGED_WRAPPER_FINALIZER_OBSERVED phase={phase} count={finalized}");
        GD.Print($"PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase={phase}");
'@
    $mutated = @'
        GD.Print($"PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase={phase}");
        DrainManagedWrappersCore();
        int finalized = ManagedWrapperProcessExitFinalizerProbe.Finalized;
        if (finalized != 1)
            throw new InvalidOperationException(
                $"managed wrapper process-exit probe expected 1 finalizer, observed {finalized}");
        GD.Print($"TASK4_MANAGED_WRAPPER_FINALIZER_OBSERVED phase={phase} count={finalized}");
'@
    try {
        $source = [System.Text.Encoding]::UTF8.GetString($snapshot)
        $sourceNewline = if ($source.Contains("`r`n")) { "`r`n" } else { "`n" }
        $expectedWithNativeNewlines = $expected.Replace("`n", $sourceNewline)
        $mutatedWithNativeNewlines = $mutated.Replace("`n", $sourceNewline)
        $changed = $source.Replace($expectedWithNativeNewlines, $mutatedWithNativeNewlines)
        if ($changed -ceq $source) { throw 'Phase-exit early-marker mutation did not find the finalizer-before-marker sequence.' }
        [System.IO.File]::WriteAllText($mutantSourcePath, $changed, [System.Text.UTF8Encoding]::new($false))
        Invoke-Task4Build -ProjectRoot $MutantProject
        $run = Invoke-Task4PrologueTests -ProjectRoot $MutantProject -Label 'phase-exit-early-marker-red' -Phase 'pre'
        $lines = @(Get-Content -LiteralPath $run.Log)
        $marker = 'PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=pre'
        $finalizer = 'TASK4_MANAGED_WRAPPER_FINALIZER_OBSERVED phase=pre count=1'
        $markerIndex = [Array]::IndexOf([string[]]$lines, $marker)
        $finalizerIndex = [Array]::IndexOf([string[]]$lines, $finalizer)
        if ($markerIndex -lt 0 -or $finalizerIndex -lt 0 -or $markerIndex -ge $finalizerIndex) {
            throw "Phase-exit early-marker mutation did not expose marker-before-finalizer output: marker_index=$markerIndex finalizer_index=$finalizerIndex."
        }
        if ((Get-ExactLineCount -Path $run.Log -Line $marker) -ne 1) {
            throw 'Phase-exit early-marker mutation did not emit exactly one pre marker.'
        }
        "TASK4_PHASE_EXIT_EARLY_MARKER_RED_OBSERVED child_exit=$($run.Result.ExitCode) marker_index=$markerIndex finalizer_index=$finalizerIndex log=$($run.Log) console=$($run.Console)"
    }
    finally {
        [System.IO.File]::WriteAllBytes($mutantSourcePath, $snapshot)
        Assert-ExactBytes -Expected $snapshot -Path $mutantSourcePath -Label 'isolated TestSceneTeardown phase-exit marker restoration'
        $restoredHash = Get-ByteSha256 -Bytes ([System.IO.File]::ReadAllBytes($mutantSourcePath))
        if ($restoredHash -cne $snapshotHash) {
            throw "Isolated TestSceneTeardown phase-exit marker restoration failed: expected $snapshotHash actual $restoredHash."
        }
        Invoke-Task4Build -ProjectRoot $MutantProject
        "TASK4_PHASE_EXIT_EARLY_MARKER_MUTANT_SOURCE_RESTORED sha256=$restoredHash path=$mutantSourcePath"
    }
}

function Invoke-PhaseExitPairGreen {
    param(
        [Parameter(Mandatory)][string]$ProjectRoot,
        [Parameter(Mandatory)][string]$Pair
    )

    foreach ($phase in @('pre', 'post')) {
        $run = Invoke-Task4PrologueTests -ProjectRoot $ProjectRoot -Label "phase-exit-$Pair-$phase" -Phase $phase
        if ($run.Result.ExitCode -ne 0) { throw "Phase-exit $Pair/$phase exited $($run.Result.ExitCode)." }
        Assert-ExactLogMarker -Path $run.Log -Marker "TASK4_MANAGED_WRAPPER_FINALIZER_OBSERVED phase=$phase count=1"
        Assert-ExactLogMarker -Path $run.Log -Marker "PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=$phase"
        Assert-ExactLogMarker -Path $run.Log -Marker "PROLOGUE_TEST_PHASE_OK phase=$phase"
        $testsOk = Get-ExactLineCount -Path $run.Log -Line 'PROLOGUE_TESTS_OK'
        if (($phase -eq 'pre' -and $testsOk -ne 0) -or ($phase -eq 'post' -and $testsOk -ne 1)) {
            throw "Phase-exit $Pair/$phase expected PROLOGUE_TESTS_OK count $(if ($phase -eq 'post') { 1 } else { 0 }); found $testsOk."
        }
        Assert-NoTask4NativeFatal -Paths @($run.Log, $run.Console)
        "TASK4_PHASE_EXIT_GREEN phase=$phase pair=$Pair exit=0 log=$($run.Log) console=$($run.Console)"
    }
}

function Invoke-IsolatedPhaseExitMutations {
    $tempProject = $null
    try {
        $tempProject = New-Task4MutationProjectCopy
        "TASK4_PHASE_EXIT_TEMP_COPY_CREATED path=$tempProject includes=.godot excludes=.git,.worktrees,screenshots,bin,obj,large-local-logs"
        Invoke-Task4Build -ProjectRoot $tempProject -AllowRestore
        Invoke-PhaseExitDrainDeleteRed -MutantProject $tempProject
        Invoke-PhaseExitDrainEarlyMarkerRed -MutantProject $tempProject
    }
    finally {
        if ($null -ne $tempProject) { Remove-Task4TempProjectCopy -Path $tempProject }
    }
    'TASK4_PHASE_EXIT_MUTATION_ISOLATION_OK'
}

function Invoke-ExpectedPresentationFailure {
    $timestamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
    $log = Join-Path $evidenceRoot "forced-presentation-failure-$timestamp.log"
    $console = Join-Path $evidenceRoot "forced-presentation-failure-$timestamp.console.log"
    $variable = 'KNIGHT_PRESENTATION_FORCE_VISIBILITY_BOUNDS_FAILURE'
    $prior = [Environment]::GetEnvironmentVariable($variable, 'Process')
    try {
        [Environment]::SetEnvironmentVariable($variable, '1', 'Process')
        $result = Invoke-Task4Godot -Arguments @(
            '--path', $project, '--fixed-fps', '60',
            '--script', 'res://tests/presentation/ProloguePresentation.cs', '--log-file', $log
        ) -ConsolePath $console
        $result.Lines | ForEach-Object { $_ }
        if ($result.ExitCode -eq 0) { throw 'Forced presentation failure exited zero.' }
        if ((Get-ExactLineCount -Path $log -Line 'PROLOGUE_CAPTURE_TEARDOWN_OK capture=prologue') -ne 0) {
            throw 'Forced presentation failure printed the teardown success marker.'
        }
        if ((Get-ExactLineCount -Path $log -Line 'PROLOGUE_PRESENTATION_COMPLETE frame=2989') -ne 0) {
            throw 'Forced presentation failure printed the completion marker.'
        }
        $failureCount = @((Get-Content -LiteralPath $log) |
            Where-Object { $_ -match '^ERROR:\s+PROLOGUE_PRESENTATION_FAIL frame=300:' }).Count
        if ($failureCount -ne 1) {
            throw "Forced presentation failure expected exactly one PROLOGUE_PRESENTATION_FAIL frame=300; found $failureCount."
        }
        $failureOutput = ((Get-Content -LiteralPath $log -Raw),
            (Get-Content -LiteralPath $console -Raw)) -join [Environment]::NewLine
        foreach ($requiredSignature in @(
            'motion visibility transition=', 'frame=300', 'path=player/feet-heading-weapon')) {
            if (-not $failureOutput.Contains($requiredSignature)) {
                throw "Forced presentation failure omitted required visibility-bounds signature: $requiredSignature"
            }
        }
        "TASK4_FAILURE_PATH_RED_OBSERVED exit=$($result.ExitCode) failure_frame=300 log=$log console=$console"
    }
    finally {
        [Environment]::SetEnvironmentVariable($variable, $prior, 'Process')
    }
}

if ($OnlyMutationIsolation) {
    Invoke-IsolatedMutationBoundaries -RunNoDrain -RunMarkerOrder
    exit 0
}
if ($OnlyPhaseExitBoundary) {
    Invoke-IsolatedPhaseExitMutations
    Invoke-PhaseExitPairGreen -ProjectRoot $project -Pair 'one'
    Invoke-PhaseExitPairGreen -ProjectRoot $project -Pair 'two'
    exit 0
}
if ($OnlyMarkerOrder) {
    Invoke-IsolatedMutationBoundaries -RunMarkerOrder
    'TASK4_CAPTURE_EXIT_MUTATION_TESTS_OK mode=marker-order-only'
    exit 0
}
if ($OnlyFailurePath) {
    Invoke-ExpectedPresentationFailure
    'TASK4_CAPTURE_EXIT_MUTATION_TESTS_OK mode=failure-path-only'
    exit 0
}

Invoke-IsolatedMutationBoundaries -RunNoDrain -RunMarkerOrder
Invoke-ExpectedPresentationFailure

if ($SkipGreenCapture) {
    'TASK4_CAPTURE_EXIT_MUTATION_TESTS_OK mode=defensive-red-only'
    exit 0
}

$timestamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$movie = Join-Path $evidenceRoot "prologue-green-$timestamp.avi"
$log = Join-Path $evidenceRoot "prologue-green-$timestamp.log"
$console = Join-Path $evidenceRoot "prologue-green-$timestamp.console.log"
$startedUtc = [DateTime]::UtcNow
$result = Invoke-Task4Godot -Arguments @(
    '--path', $project, '--fixed-fps', '60', '--write-movie', $movie,
    '--script', 'res://tests/presentation/ProloguePresentation.cs', '--log-file', $log
) -ConsolePath $console
$result.Lines | ForEach-Object { $_ }
if ($result.ExitCode -ne 0) {
    throw "Focused prologue capture exited $($result.ExitCode); finalizer teardown must complete before Godot native shutdown."
}
Assert-FreshNonEmptyOutputs -Paths @($movie, $log, $console) -StartedUtc $startedUtc
Assert-AviCaptureContract -Path $movie -ExpectedFrameCount 2989 -ExpectedWidth 1280 -ExpectedHeight 720 -ExpectedFps 60
Assert-ExactLogMarker -Path $log -Marker 'PROLOGUE_PRESENTATION_COMPLETE frame=2989'
Assert-ExactLogMarker -Path $log -Marker 'PROLOGUE_CAPTURE_TEARDOWN_OK capture=prologue'
Assert-NoEngineLogErrors -Paths @($log, $console)
"TASK4_CAPTURE_EXIT_GREEN_OK capture=prologue frames=2989 dimensions=1280x720 fps=60 log=$log console=$console movie=$movie"
