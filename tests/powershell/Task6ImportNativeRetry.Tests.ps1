[CmdletBinding()]
param(
    [string]$ProjectRoot,
    [string]$EvidenceRoot
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
. (Join-Path $ProjectRoot 'tools\EvidenceChecks.ps1')
$retryHelper = Join-Path $ProjectRoot 'tools\Task6ImportRetry.ps1'
if (Test-Path -LiteralPath $retryHelper -PathType Leaf) {
    . $retryHelper
}

if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) {
    $EvidenceRoot = Join-Path -Path ([IO.Path]::GetTempPath()) -ChildPath ('knight-task6-import-native-retry-' + [guid]::NewGuid().ToString('N'))
}
$EvidenceRoot = [IO.Path]::GetFullPath($EvidenceRoot)
if (Test-Path -LiteralPath $EvidenceRoot) {
    throw "Task 6 import-retry evidence root must be fresh: $EvidenceRoot"
}
New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null

function Assert-Task6ImportRetry([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Task 6 import-retry assertion failed: $Message" }
}

function Remove-Task6OwnedTempDirectory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return }
    $resolved = [IO.Path]::GetFullPath($Path).TrimEnd('\')
    $temp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($temp, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Task 6 import-retry test refuses to delete a non-temp directory: $resolved"
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}

function New-Task6ImportRetryFixture([string]$Name) {
    $root = Join-Path -Path ([IO.Path]::GetTempPath()) -ChildPath ('knight-task6-import-retry-fixture-' + $Name + '-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    & git -C $root init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Task 6 import-retry fixture cannot initialize its temporary Git repository.' }
    [IO.File]::WriteAllText((Join-Path $root 'tracked.txt'), 'canonical' + [Environment]::NewLine,
        [Text.UTF8Encoding]::new($false))
    & git -C $root add --all
    if ($LASTEXITCODE -ne 0) { throw 'Task 6 import-retry fixture cannot stage its tracked file.' }
    & git -C $root -c user.name='Task6 Test' -c user.email='task6@example.invalid' `
        commit --quiet -m 'fixture'
    if ($LASTEXITCODE -ne 0) { throw 'Task 6 import-retry fixture cannot create its local commit.' }
    return $root
}

function Invoke-Task6ImportRetryScenario([string]$Name, [string]$Mode) {
    $candidate = New-Task6ImportRetryFixture -Name $Name
    $scenarioEvidence = Join-Path $EvidenceRoot $Name
    New-Item -ItemType Directory -Path $scenarioEvidence -Force | Out-Null
    $state = [pscustomobject]@{
        Candidate = $candidate
        Mode = $Mode
        Attempts = 0
        RetryStatusCount = -1
        RetryCacheExists = $true
    }
    $prepare = {
        param([int]$Attempt)
        if ($Attempt -eq 2) {
            $state.RetryStatusCount = @(& git -C $state.Candidate status --porcelain=v1 --untracked-files=all).Count
            $state.RetryCacheExists = Test-Path -LiteralPath (Join-Path $state.Candidate '.godot')
        }
        $cache = Join-Path $state.Candidate '.godot'
        New-Item -ItemType Directory -Path $cache -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $cache ('attempt-' + $Attempt + '.cache')), 'owned cache',
            [Text.UTF8Encoding]::new($false))
    }.GetNewClosure()
    $invoke = {
        param([int]$Attempt, [string]$LogPath, [string]$ConsolePath)
        $state.Attempts++
        if ($Attempt -eq 1) {
            [IO.File]::WriteAllText((Join-Path $state.Candidate 'tracked.txt'), 'mutated' + [Environment]::NewLine,
                [Text.UTF8Encoding]::new($false))
            [IO.File]::WriteAllText((Join-Path $state.Candidate 'created.uid'), 'derived sidecar',
                [Text.UTF8Encoding]::new($false))
            if ($state.Mode -ceq 'cleanup-mismatch') {
                [IO.File]::WriteAllText((Join-Path $state.Candidate 'unexpected.txt'), 'must not be deleted',
                    [Text.UTF8Encoding]::new($false))
            }
        }
        $hasMarker = $state.Mode -cne 'markerless-native'
        $logText = if ($hasMarker) { 'VERIFICATION_EDITOR_GC_DRAIN_OK' + [Environment]::NewLine } else { 'import interrupted' + [Environment]::NewLine }
        [IO.File]::WriteAllText($LogPath, $logText, [Text.UTF8Encoding]::new($false))
        [IO.File]::WriteAllText($ConsolePath, 'attempt console' + [Environment]::NewLine,
            [Text.UTF8Encoding]::new($false))
        $exitCode = 0
        if ($state.Mode -ceq 'retry-success' -and $Attempt -eq 1) { $exitCode = -1073741819 }
        if ($state.Mode -ceq 'two-native-crashes') { $exitCode = -1073741819 }
        if ($state.Mode -ceq 'markerless-native') { $exitCode = -1073741819 }
        if ($state.Mode -ceq 'different-exit') { $exitCode = -1 }
        if ($state.Mode -ceq 'cleanup-mismatch') { $exitCode = -1073741819 }
        return [pscustomobject]@{ ExitCode = $exitCode; Lines = @('attempt=' + $Attempt) }
    }.GetNewClosure()
    try {
        $output = @(& Invoke-Task6ImportWithBoundedNativeRetry -ProjectRoot $candidate `
            -EvidenceRoot $scenarioEvidence -PrepareAttempt $prepare -InvokeAttempt $invoke 2>&1)
        return [pscustomobject]@{ Candidate = $candidate; Evidence = $scenarioEvidence; State = $state; Output = $output; Error = $null }
    }
    catch {
        return [pscustomobject]@{ Candidate = $candidate; Evidence = $scenarioEvidence; State = $state; Output = @(); Error = $_ }
    }
}

Describe 'Task 6 native import retry boundary' {
    It 'retries one marker-bound native access violation only after exact candidate restoration' {
        $run = Invoke-Task6ImportRetryScenario -Name 'retry-success' -Mode 'retry-success'
        try {
            Assert-Task6ImportRetry ($null -eq $run.Error) "retry-success completes: $($run.Error)"
            Assert-Task6ImportRetry ($run.State.Attempts -eq 2) 'native access violation gets exactly one retry'
            Assert-Task6ImportRetry ($run.State.RetryStatusCount -eq 0) 'retry starts after tracked and sidecar restoration'
            Assert-Task6ImportRetry (-not $run.State.RetryCacheExists) 'retry starts after owned Godot cache deletion'
            $terminal = @($run.Output | ForEach-Object { $_.ToString() } | Where-Object {
                    $_ -ceq 'PROLOGUE_IMPORT_NATIVE_RETRY_OK attempts=2 first_exit=-1073741819 restored=True'
                })
            Assert-Task6ImportRetry ($terminal.Count -eq 1) 'retry emits one explicit terminal marker'
            foreach ($attempt in 1, 2) {
                $metadata = Join-Path $run.Evidence ('verify-prologue-import-attempt' + $attempt + '.metadata.txt')
                Assert-Task6ImportRetry (Test-Path -LiteralPath $metadata -PathType Leaf) "attempt $attempt persists independent metadata"
                $metadataLines = @(Get-Content -LiteralPath $metadata)
                Assert-Task6ImportRetry (@($metadataLines | Where-Object { $_ -match '^exit_code=-?\d+$' }).Count -eq 1) `
                    "attempt $attempt records one exit code"
                Assert-Task6ImportRetry (@($metadataLines | Where-Object { $_ -ceq 'gc_drain_marker_count=1' }).Count -eq 1) `
                    "attempt $attempt records one marker count"
            }
        }
        finally { Remove-Task6OwnedTempDirectory -Path $run.Candidate }
    }

    It 'rejects a second native access violation instead of attempting a third import' {
        $run = Invoke-Task6ImportRetryScenario -Name 'two-native-crashes' -Mode 'two-native-crashes'
        try {
            Assert-Task6ImportRetry ($null -ne $run.Error) 'second native crash fails closed'
            Assert-Task6ImportRetry ($run.Error.Exception.Message -match 'BASE_IMPORT_NATIVE_RETRY_ATTEMPT2_FAILED exit=-1073741819') `
                'second native crash has the explicit attempt-two label'
            Assert-Task6ImportRetry ($run.State.Attempts -eq 2) 'second native crash never starts a third attempt'
        }
        finally { Remove-Task6OwnedTempDirectory -Path $run.Candidate }
    }

    It 'rejects a native crash that lacks the required GC-drain marker' {
        $run = Invoke-Task6ImportRetryScenario -Name 'markerless-native' -Mode 'markerless-native'
        try {
            Assert-Task6ImportRetry ($null -ne $run.Error) 'markerless native crash fails closed'
            Assert-Task6ImportRetry ($run.Error.Exception.Message -match 'BASE_IMPORT_NATIVE_RETRY_MARKER_MISSING') `
                'markerless native crash identifies the missing marker'
            Assert-Task6ImportRetry ($run.State.Attempts -eq 1) 'markerless native crash never retries'
        }
        finally { Remove-Task6OwnedTempDirectory -Path $run.Candidate }
    }

    It 'rejects a non-native nonzero exit even when the marker exists' {
        $run = Invoke-Task6ImportRetryScenario -Name 'different-exit' -Mode 'different-exit'
        try {
            Assert-Task6ImportRetry ($null -ne $run.Error) 'different nonzero exit fails closed'
            Assert-Task6ImportRetry ($run.Error.Exception.Message -match 'BASE_IMPORT_NATIVE_RETRY_UNEXPECTED_EXIT exit=-1') `
                'different exit identifies its actual code'
            Assert-Task6ImportRetry ($run.State.Attempts -eq 1) 'different exit never retries'
        }
        finally { Remove-Task6OwnedTempDirectory -Path $run.Candidate }
    }

    It 'refuses a retry when exact cleanup encounters a non-sidecar untracked file' {
        $run = Invoke-Task6ImportRetryScenario -Name 'cleanup-mismatch' -Mode 'cleanup-mismatch'
        try {
            Assert-Task6ImportRetry ($null -ne $run.Error) 'cleanup mismatch fails closed'
            Assert-Task6ImportRetry ($run.Error.Exception.Message -match 'BASE_IMPORT_NATIVE_RETRY_CLEANUP_UNTRACKED_REFUSED') `
                'cleanup mismatch identifies the refused non-sidecar file'
            Assert-Task6ImportRetry ($run.State.Attempts -eq 1) 'cleanup mismatch never retries'
        }
        finally { Remove-Task6OwnedTempDirectory -Path $run.Candidate }
    }

    It 'records a first-pass success as one non-restored attempt' {
        $run = Invoke-Task6ImportRetryScenario -Name 'first-pass' -Mode 'first-pass'
        try {
            Assert-Task6ImportRetry ($null -eq $run.Error) "first-pass completes: $($run.Error)"
            Assert-Task6ImportRetry ($run.State.Attempts -eq 1) 'first-pass success has one attempt'
            $terminal = @($run.Output | ForEach-Object { $_.ToString() } | Where-Object {
                    $_ -ceq 'PROLOGUE_IMPORT_NATIVE_RETRY_OK attempts=1 first_exit=0 restored=False'
                })
            Assert-Task6ImportRetry ($terminal.Count -eq 1) 'first-pass success emits one explicit terminal marker'
        }
        finally { Remove-Task6OwnedTempDirectory -Path $run.Candidate }
    }
}
