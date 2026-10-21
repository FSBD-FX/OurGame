[CmdletBinding()]
param(
    [string]$ProjectRoot,
    [string]$CaptureMaterializationEvidenceRoot
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path

function Assert-Task6Certification([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Task 6 clean-candidate assertion failed: $Message" }
}

function Get-Task6CertificationChildHost {
    $processPath = $null
    try { $processPath = (Get-Process -Id $PID -ErrorAction Stop).Path } catch { }
    if (-not [string]::IsNullOrWhiteSpace($processPath) -and
        (Test-Path -LiteralPath $processPath -PathType Leaf)) {
        return $processPath
    }
    foreach ($name in @('pwsh', 'powershell.exe', 'powershell')) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($null -ne $command -and -not [string]::IsNullOrWhiteSpace($command.Source)) {
            return $command.Source
        }
    }
    throw 'Task 6 clean-candidate test cannot locate a PowerShell child host.'
}

function Invoke-Task6CleanCandidatePreflight([string]$CandidateRoot, [string]$Name) {
    $verificationRoot = Join-Path -Path ([IO.Path]::GetTempPath()) `
        -ChildPath ('knight-task6-clean-preflight-' + $Name + '-' + [guid]::NewGuid().ToString('N'))
    $verify = Join-Path $ProjectRoot 'tools\verify-prologue.ps1'
    $childHost = Get-Task6CertificationChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $verify `
            -ProjectRoot $CandidateRoot -CleanCandidatePreflightOnly `
            -VerificationRoot $verificationRoot 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = @($output | ForEach-Object { $_.ToString() })
        VerificationRoot = $verificationRoot
    }
}

function Invoke-Task6CleanCandidateRestore([string]$CandidateRoot, [string]$StateLog, [string]$Name) {
    $verificationRoot = Join-Path -Path ([IO.Path]::GetTempPath()) `
        -ChildPath ('knight-task6-clean-restore-' + $Name + '-' + [guid]::NewGuid().ToString('N'))
    $verify = Join-Path $ProjectRoot 'tools\verify-prologue.ps1'
    $childHost = Get-Task6CertificationChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $verify `
            -ProjectRoot $CandidateRoot -CleanCandidateRestoreOnly `
            -CleanCandidateStateLog $StateLog -VerificationRoot $verificationRoot 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = @($output | ForEach-Object { $_.ToString() })
        VerificationRoot = $verificationRoot
    }
}

function Invoke-Task6RealEditorImport([string]$CandidateRoot, [string]$EvidenceRoot) {
    $godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
    $log = Join-Path $EvidenceRoot 'clean-import.log'
    $console = Join-Path $EvidenceRoot 'clean-import.console.log'
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        & $godot --headless --editor --path $CandidateRoot --quit-after 1200 --log-file $log `
            1>$console 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    return [pscustomobject]@{ ExitCode = $exitCode; Log = $log; Console = $console }
}

function Get-Task6MissingUidWarningCount([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return -1 }
    return @((Get-Content -LiteralPath $Path) | Where-Object {
        $_ -match '^WARNING: Missing \.uid file for path "[^"]+"\. The file was re-created from cache\.$'
    }).Count
}

function Invoke-Task6CapturePreRecord([string]$CandidateRoot, [string]$CaptureRoot, [string]$AuditRoot,
    [string]$OutputLog) {
    $capture = Join-Path $CandidateRoot 'tools\capture-prologue.ps1'
    $childHost = Get-Task6CertificationChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $capture `
            -CaptureRoot $CaptureRoot -AuditRoot $AuditRoot -PreRecordProbeOnly 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    [IO.File]::WriteAllText($OutputLog, (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
        [Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{ ExitCode = $exitCode; Output = @($output | ForEach-Object { $_.ToString() }) }
}

function Assert-Task6NoUnexpectedEngineErrors([string[]]$Paths, [string]$Label) {
    foreach ($path in $Paths) {
        Assert-Task6Certification (Test-Path -LiteralPath $path -PathType Leaf) "$Label exists: $path"
        $errors = @((Get-Content -LiteralPath $path) | Where-Object {
                $_ -match '^(ERROR:|FATAL:|.*\bleak\b)'
            })
        Assert-Task6Certification ($errors.Count -eq 0) "$Label has no unexpected engine errors: $path"
    }
}

Describe 'Task 6 clean named certification candidate' {
    It 'accepts the current clean candidate only when every runtime and verifier dependency is committed' {
        $result = Invoke-Task6CleanCandidatePreflight -CandidateRoot $ProjectRoot -Name 'current'
        Assert-Task6Certification ($result.ExitCode -eq 0) (
            'clean candidate preflight succeeds: ' + ($result.Output -join "`n"))
        Assert-Task6Certification ((@($result.Output | Where-Object {
                    $_ -match '^TASK6_CLEAN_CANDIDATE_OK branch=codex/[^\s]+ head=[A-Fa-f0-9]{40} dependencies=\d+$'
                }).Count) -eq 1) 'clean candidate emits one committed-dependency terminal marker'
        $statusLog = Join-Path $result.VerificationRoot 'candidate-clean-before.log'
        Assert-Task6Certification (Test-Path -LiteralPath $statusLog -PathType Leaf) 'preflight persists the clean status log'
        Assert-Task6Certification -Condition ((@((Get-Content -LiteralPath $statusLog) | Where-Object {
                    $_ -ceq 'git_status_porcelain=<empty>'
                }).Count) -eq 1) `
            -Message 'preflight log records an empty porcelain status'
    }

    It 'rejects a dirty generated scene from a separately materialized clean worktree' {
        $repo = (& git -C $ProjectRoot rev-parse --show-toplevel).Trim()
        Assert-Task6Certification (-not [string]::IsNullOrWhiteSpace($repo)) 'candidate repository root resolves'
        $dirtyWorktree = Join-Path -Path ([IO.Path]::GetTempPath()) `
            -ChildPath ('knight-task6-dirty-scene-' + [guid]::NewGuid().ToString('N'))
        try {
            & git -C $repo worktree add --detach $dirtyWorktree HEAD | Out-Null
            Assert-Task6Certification ($LASTEXITCODE -eq 0) 'temporary clean worktree materializes from candidate HEAD'
            $scene = Join-Path $dirtyWorktree 'scenes\prologue\PrologueLevel.tscn'
            Assert-Task6Certification (Test-Path -LiteralPath $scene -PathType Leaf) 'generated main scene exists in temporary worktree'
            [IO.File]::AppendAllText($scene, "`n; Task6CleanCandidateCertification dirty-scene RED`n", [Text.UTF8Encoding]::new($false))
            $status = @(& git -C $dirtyWorktree status --porcelain=v1 --untracked-files=all)
            Assert-Task6Certification ($status.Count -gt 0) 'temporary generated scene mutation is visible to Git'

            $result = Invoke-Task6CleanCandidatePreflight -CandidateRoot $dirtyWorktree -Name 'dirty-scene'
            Assert-Task6Certification ($result.ExitCode -ne 0) 'clean candidate preflight rejects a dirty generated scene'
            Assert-Task6Certification -Condition (($result.Output -join "`n") -match 'candidate working tree is dirty') `
                -Message 'dirty-scene rejection identifies the clean-tree boundary'
        }
        finally {
            if (Test-Path -LiteralPath $dirtyWorktree) {
                & git -C $repo worktree remove --force $dirtyWorktree 2>$null
            }
        }
    }

    It 'rejects a pre-existing ignored Godot cache before certification can consume stale UID references' {
        $repo = (& git -C $ProjectRoot rev-parse --show-toplevel).Trim()
        $suffix = [guid]::NewGuid().ToString('N')
        $branch = 'codex/task6-preexisting-cache-' + $suffix
        $worktree = Join-Path -Path ([IO.Path]::GetTempPath()) -ChildPath ('knight-task6-preexisting-cache-' + $suffix)
        try {
            & git -C $repo worktree add -b $branch $worktree HEAD | Out-Null
            Assert-Task6Certification ($LASTEXITCODE -eq 0) 'temporary named cache worktree materializes from candidate HEAD'
            $cache = Join-Path $worktree '.godot'
            New-Item -ItemType Directory -Path $cache -ErrorAction Stop | Out-Null
            [IO.File]::WriteAllText((Join-Path $cache 'preexisting-cache.txt'), 'Task6 cache RED',
                [Text.UTF8Encoding]::new($false))
            $result = Invoke-Task6CleanCandidatePreflight -CandidateRoot $worktree -Name 'preexisting-cache'
            Assert-Task6Certification ($result.ExitCode -ne 0) 'clean candidate preflight rejects a pre-existing ignored cache'
            Assert-Task6Certification (($result.Output -join "`n") -match 'TASK6_CLEAN_CANDIDATE_CACHE_PREEXISTING') `
                'cache rejection identifies the stale-cache boundary'
        }
        finally {
            if (Test-Path -LiteralPath $worktree) {
                & git -C $repo worktree remove --force $worktree 2>$null
            }
            & git -C $repo branch -D $branch 2>$null
        }
    }

    It 'restores only the real editor-import side effects recorded from a clean temporary worktree' {
        $repo = (& git -C $ProjectRoot rev-parse --show-toplevel).Trim()
        $suffix = [guid]::NewGuid().ToString('N')
        $branch = 'codex/task6-clean-import-' + $suffix
        $worktree = Join-Path -Path ([IO.Path]::GetTempPath()) -ChildPath ('knight-task6-clean-import-' + $suffix)
        $evidenceRoot = Join-Path -Path ([IO.Path]::GetTempPath()) -ChildPath ('knight-task6-clean-import-evidence-' + $suffix)
        try {
            & git -C $repo worktree add -b $branch $worktree HEAD | Out-Null
            Assert-Task6Certification ($LASTEXITCODE -eq 0) 'temporary named worktree materializes from candidate HEAD'
            Assert-Task6Certification ((@(& git -C $worktree status --porcelain=v1 --untracked-files=all)).Count -eq 0) `
                'temporary import worktree starts clean'
            Assert-Task6Certification (-not (Test-Path -LiteralPath (Join-Path $worktree '.godot'))) `
                'temporary import worktree starts without an ignored Godot cache'
            New-Item -ItemType Directory -Path $evidenceRoot -ErrorAction Stop | Out-Null
            $firstEvidenceRoot = Join-Path $evidenceRoot 'first-import'
            $secondEvidenceRoot = Join-Path $evidenceRoot 'second-import'
            New-Item -ItemType Directory -Path $firstEvidenceRoot -ErrorAction Stop | Out-Null
            New-Item -ItemType Directory -Path $secondEvidenceRoot -ErrorAction Stop | Out-Null
            $preflight = Invoke-Task6CleanCandidatePreflight -CandidateRoot $worktree -Name 'real-import'
            Assert-Task6Certification ($preflight.ExitCode -eq 0) 'temporary named worktree preflight succeeds before import'
            $stateLog = Join-Path $preflight.VerificationRoot 'candidate-clean-before.log'
            Assert-Task6Certification (Test-Path -LiteralPath $stateLog -PathType Leaf) 'preflight persists restoration state'

            $import = Invoke-Task6RealEditorImport -CandidateRoot $worktree -EvidenceRoot $firstEvidenceRoot
            Assert-Task6Certification ($import.ExitCode -eq 0) 'real editor import completes before cleanup'
            Assert-Task6Certification (Test-Path -LiteralPath $import.Log -PathType Leaf) 'real editor import writes an external log'
            $firstUidWarnings = Get-Task6MissingUidWarningCount -Path $import.Log
            Assert-Task6Certification ($firstUidWarnings -eq 0) `
                'first import from a cache-free candidate produces no missing-UID warnings'
            $dirtyAfterImport = @(& git -C $worktree status --porcelain=v1 --untracked-files=all)
            Assert-Task6Certification ($dirtyAfterImport.Count -gt 0) 'real editor import creates a dirty candidate RED'
            Assert-Task6Certification (($dirtyAfterImport -join "`n") -match 'default_bus_layout\.tres') `
                'real editor import changes a tracked configuration file'
            Assert-Task6Certification (($dirtyAfterImport -join "`n") -match '\.(uid|import)$') `
                'real editor import creates derived uid/import files'

            $restore = Invoke-Task6CleanCandidateRestore -CandidateRoot $worktree -StateLog $stateLog -Name 'real-import'
            Assert-Task6Certification ($restore.ExitCode -eq 0) (
                'recorded import cleanup succeeds: ' + ($restore.Output -join "`n"))
            Assert-Task6Certification ((@($restore.Output | Where-Object {
                    $_ -match '^TASK6_CLEAN_CANDIDATE_RESTORE_OK restored=\d+ created=\d+ cache_deleted=True$'
                }).Count) -eq 1) 'cleanup emits one explicit terminal marker'
            Assert-Task6Certification ((@(& git -C $worktree status --porcelain=v1 --untracked-files=all)).Count -eq 0) `
                'recorded cleanup restores the temporary candidate to an empty porcelain status'
            Assert-Task6Certification (-not (Test-Path -LiteralPath (Join-Path $worktree '.godot'))) `
                'cleanup deletes only the cache created after the cache-free preflight'
            $cacheExistsAfterFirstCleanup = Test-Path -LiteralPath (Join-Path $worktree '.godot')
            $statusAfterFirstCleanup = @(& git -C $worktree status --porcelain=v1 --untracked-files=all)
            $afterLog = Join-Path $restore.VerificationRoot 'candidate-clean-after.log'
            Assert-Task6Certification (Test-Path -LiteralPath $afterLog -PathType Leaf) 'cleanup persists path/count/hash evidence'

            $secondPreflight = Invoke-Task6CleanCandidatePreflight -CandidateRoot $worktree -Name 'real-import-second'
            Assert-Task6Certification ($secondPreflight.ExitCode -eq 0) 'same worktree preflight succeeds after cache cleanup'
            $secondImport = Invoke-Task6RealEditorImport -CandidateRoot $worktree -EvidenceRoot $secondEvidenceRoot
            Assert-Task6Certification ($secondImport.ExitCode -eq 0) 'second real editor import completes after cache cleanup'
            $secondUidWarnings = Get-Task6MissingUidWarningCount -Path $secondImport.Log
            Assert-Task6Certification ($secondUidWarnings -eq 0) `
                'second import from the same cleaned worktree produces no missing-UID warnings'
            $secondStateLog = Join-Path $secondPreflight.VerificationRoot 'candidate-clean-before.log'
            $secondRestore = Invoke-Task6CleanCandidateRestore -CandidateRoot $worktree -StateLog $secondStateLog -Name 'real-import-second'
            Assert-Task6Certification ($secondRestore.ExitCode -eq 0) (
                'second recorded import cleanup succeeds: ' + ($secondRestore.Output -join "`n"))
            Assert-Task6Certification ((@($secondRestore.Output | Where-Object {
                    $_ -match '^TASK6_CLEAN_CANDIDATE_RESTORE_OK restored=\d+ created=\d+ cache_deleted=True$'
                }).Count) -eq 1) 'second cleanup emits one cache-deletion terminal marker'
            Assert-Task6Certification ((@(& git -C $worktree status --porcelain=v1 --untracked-files=all)).Count -eq 0) `
                'second cleanup restores the same temporary candidate to an empty porcelain status'
            Assert-Task6Certification (-not (Test-Path -LiteralPath (Join-Path $worktree '.godot'))) `
                'second cleanup removes the second import cache before a new proof can start'
            [IO.File]::WriteAllLines((Join-Path $evidenceRoot 'cache-lifecycle-summary.txt'), @(
                    'first_uid_warnings=' + $firstUidWarnings,
                    'second_uid_warnings=' + $secondUidWarnings,
                    'cache_exists_after_first_cleanup=' + $cacheExistsAfterFirstCleanup,
                    'cache_exists_after_second_cleanup=' + (Test-Path -LiteralPath (Join-Path $worktree '.godot')),
                    'git_status_after_first_cleanup=' + ($statusAfterFirstCleanup -join '|'),
                    'git_status_after_second_cleanup=' + ((@(& git -C $worktree status --porcelain=v1 --untracked-files=all) -join '|')),
                    'first_log=' + $import.Log,
                    'second_log=' + $secondImport.Log
                ), [Text.UTF8Encoding]::new($false))
        }
        finally {
            if (Test-Path -LiteralPath $worktree) {
                & git -C $repo worktree remove --force $worktree 2>$null
            }
            & git -C $repo branch -D $branch 2>$null
        }
    }

}

Describe 'Task 6 clean capture materialization' {
    It 'runs the Task11 critical marker from a fresh clean capture pre-record materialization' {
        # This catches the exact regression where capture validated stale generated scenes but
        # started its critical runtime before generating the active canyon route.
        $repo = (& git -C $ProjectRoot rev-parse --show-toplevel).Trim()
        $suffix = [guid]::NewGuid().ToString('N')
        $branch = 'codex/task6-capture-prerecord-' + $suffix
        $worktree = Join-Path -Path ([IO.Path]::GetTempPath()) `
            -ChildPath ('knight-task6-capture-prerecord-' + $suffix)
        $evidenceRoot = if ([string]::IsNullOrWhiteSpace($CaptureMaterializationEvidenceRoot)) {
            Join-Path -Path ([IO.Path]::GetTempPath()) `
                -ChildPath ('knight-task6-capture-prerecord-evidence-' + $suffix)
        }
        else { [IO.Path]::GetFullPath($CaptureMaterializationEvidenceRoot) }
        $captureRoot = Join-Path $evidenceRoot 'capture'
        $auditRoot = Join-Path $evidenceRoot 'audit'
        $childOutput = Join-Path $evidenceRoot 'capture-prerecord.child.log'
        $stateLog = $null
        $worktreeRemoved = $false
        $restoreSucceeded = $false
        try {
            Assert-Task6Certification (-not (Test-Path -LiteralPath $worktree)) `
                'fresh capture materialization worktree path is absent before creation'
            Assert-Task6Certification (-not (Test-Path -LiteralPath $evidenceRoot)) `
                'fresh capture materialization evidence root is absent before creation'
            Assert-Task6Certification ((@(& git -C $repo branch --list -- $branch)).Count -eq 0) `
                'fresh capture materialization branch name is unused'
            New-Item -ItemType Directory -Path $evidenceRoot -ErrorAction Stop | Out-Null
            & git -C $repo worktree add -b $branch $worktree HEAD | Out-Null
            Assert-Task6Certification ($LASTEXITCODE -eq 0) 'fresh named capture materialization worktree is created from HEAD'
            Assert-Task6Certification ((@(& git -C $worktree status --porcelain=v1 --untracked-files=all)).Count -eq 0) `
                'fresh named capture materialization worktree starts clean'
            Assert-Task6Certification (-not (Test-Path -LiteralPath (Join-Path $worktree '.godot'))) `
                'fresh named capture materialization worktree starts cache-free'

            $preflight = Invoke-Task6CleanCandidatePreflight -CandidateRoot $worktree -Name 'capture-prerecord'
            Assert-Task6Certification ($preflight.ExitCode -eq 0) (
                'fresh capture materialization preflight succeeds: ' + ($preflight.Output -join "`n"))
            $stateLog = Join-Path $preflight.VerificationRoot 'candidate-clean-before.log'
            Assert-Task6Certification (Test-Path -LiteralPath $stateLog -PathType Leaf) `
                'fresh capture materialization records exact preflight state'

            $capture = Invoke-Task6CapturePreRecord -CandidateRoot $worktree -CaptureRoot $captureRoot `
                -AuditRoot $auditRoot -OutputLog $childOutput
            Assert-Task6Certification ($capture.ExitCode -eq 0) (
                'fresh capture pre-record completes Task11 critical proof: ' + ($capture.Output -join "`n"))
            $criticalLog = Join-Path $captureRoot 'task11-critical.log'
            Assert-Task6Certification (Test-Path -LiteralPath $criticalLog -PathType Leaf) `
                'fresh capture pre-record writes a Task11 critical log'
            Assert-Task6Certification ((@((Get-Content -LiteralPath $criticalLog) | Where-Object {
                        $_ -ceq 'PROLOGUE_CRITICAL_PATH_OK'
                    }).Count) -eq 1) 'fresh capture pre-record emits exactly one Task11 critical terminal marker'
        }
        finally {
            if ($null -ne $stateLog) {
                $restore = Invoke-Task6CleanCandidateRestore -CandidateRoot $worktree -StateLog $stateLog `
                    -Name 'capture-prerecord'
                Assert-Task6Certification ($restore.ExitCode -eq 0) (
                    'fresh capture pre-record cleanup succeeds: ' + ($restore.Output -join "`n"))
                Assert-Task6Certification ((@($restore.Output | Where-Object {
                            $_ -match '^TASK6_CLEAN_CANDIDATE_RESTORE_OK restored=\d+ created=\d+ cache_deleted=(True|False)$'
                        }).Count) -eq 1) 'fresh capture pre-record cleanup emits one restore marker'
                Assert-Task6Certification ((@(& git -C $worktree status --porcelain=v1 --untracked-files=all)).Count -eq 0) `
                    'fresh capture pre-record cleanup restores an empty candidate status'
                Assert-Task6Certification (-not (Test-Path -LiteralPath (Join-Path $worktree '.godot'))) `
                    'fresh capture pre-record cleanup removes its created cache'
                $restoreSucceeded = $true
            }
            [IO.File]::WriteAllLines((Join-Path $evidenceRoot 'capture-prerecord-summary.txt'), @(
                    'candidate_worktree=' + $worktree,
                    'candidate_branch=' + $branch,
                    'capture_root=' + $captureRoot,
                    'audit_root=' + $auditRoot,
                    'child_output=' + $childOutput,
                    'preflight_state=' + $stateLog,
                    'outer_restore_succeeded=' + $restoreSucceeded
                ), [Text.UTF8Encoding]::new($false))
            if ($restoreSucceeded -and (Test-Path -LiteralPath $worktree)) {
                & git -C $repo worktree remove $worktree 2>$null
                Assert-Task6Certification ($LASTEXITCODE -eq 0) 'owned clean capture materialization worktree is removed'
                $worktreeRemoved = $true
            }
            if ($worktreeRemoved) {
                & git -C $repo branch -D $branch 2>$null
                Assert-Task6Certification ($LASTEXITCODE -eq 0) 'owned clean capture materialization branch is removed'
            }
        }
    }
}

Describe 'Task 6 imported capture materialization' {
    It 'records imported six-builder materialization and restores it before the capture probe returns' {
        # A capture can only certify the generated canyon it actually executed.  This catches
        # a missing import/build plan, a reordered leaf-to-main plan, error-bearing import, or
        # a capture that leaves derived bytes/cache behind for the next verifier.
        $repo = (& git -C $ProjectRoot rev-parse --show-toplevel).Trim()
        $suffix = [guid]::NewGuid().ToString('N')
        $branch = 'codex/task6-capture-materialization-' + $suffix
        $worktree = Join-Path -Path ([IO.Path]::GetTempPath()) `
            -ChildPath ('knight-task6-capture-materialization-' + $suffix)
        $evidenceRoot = if ([string]::IsNullOrWhiteSpace($CaptureMaterializationEvidenceRoot)) {
            Join-Path -Path ([IO.Path]::GetTempPath()) `
                -ChildPath ('knight-task6-capture-materialization-evidence-' + $suffix)
        }
        else {
            [IO.Path]::GetFullPath($CaptureMaterializationEvidenceRoot + '-materialization')
        }
        $captureRoot = Join-Path $evidenceRoot 'capture'
        $auditRoot = Join-Path $evidenceRoot 'audit'
        $childOutput = Join-Path $evidenceRoot 'capture-prerecord.child.log'
        $stateLog = $null
        $worktreeRemoved = $false
        $restoreSucceeded = $false
        try {
            Assert-Task6Certification (-not (Test-Path -LiteralPath $worktree)) `
                'fresh imported materialization worktree path is absent before creation'
            Assert-Task6Certification (-not (Test-Path -LiteralPath $evidenceRoot)) `
                'fresh imported materialization evidence root is absent before creation'
            Assert-Task6Certification ((@(& git -C $repo branch --list -- $branch)).Count -eq 0) `
                'fresh imported materialization branch name is unused'
            New-Item -ItemType Directory -Path $evidenceRoot -ErrorAction Stop | Out-Null
            & git -C $repo worktree add -b $branch $worktree HEAD | Out-Null
            Assert-Task6Certification ($LASTEXITCODE -eq 0) 'fresh named imported materialization worktree is created from HEAD'
            Assert-Task6Certification ((@(& git -C $worktree status --porcelain=v1 --untracked-files=all)).Count -eq 0) `
                'fresh named imported materialization worktree starts clean'
            Assert-Task6Certification (-not (Test-Path -LiteralPath (Join-Path $worktree '.godot'))) `
                'fresh named imported materialization worktree starts cache-free'

            $preflight = Invoke-Task6CleanCandidatePreflight -CandidateRoot $worktree -Name 'capture-materialization'
            Assert-Task6Certification ($preflight.ExitCode -eq 0) (
                'fresh imported materialization preflight succeeds: ' + ($preflight.Output -join "`n"))
            $stateLog = Join-Path $preflight.VerificationRoot 'candidate-clean-before.log'
            Assert-Task6Certification (Test-Path -LiteralPath $stateLog -PathType Leaf) `
                'fresh imported materialization records exact preflight state'

            $capture = Invoke-Task6CapturePreRecord -CandidateRoot $worktree -CaptureRoot $captureRoot `
                -AuditRoot $auditRoot -OutputLog $childOutput
            $materializationManifest = Join-Path $captureRoot 'task6-capture-materialization.manifest.txt'
            Assert-Task6Certification (Test-Path -LiteralPath $materializationManifest -PathType Leaf) `
                'capture records a bound imported six-builder materialization manifest'
            Assert-Task6Certification ($capture.ExitCode -eq 0) (
                'capture imported materialization completes its pre-record probe: ' + ($capture.Output -join "`n"))
            $materialization = @(Get-Content -LiteralPath $materializationManifest)
            $expectedBuilders = @(
                'builder_01=BuildCanyonEntrance|script=res://tools/build/prologue/BuildCanyonEntrance.cs',
                'builder_02=BuildCanyonDescent|script=res://tools/build/prologue/BuildCanyonDescent.cs',
                'builder_03=BuildCanyonBasin|script=res://tools/build/prologue/BuildCanyonBasin.cs',
                'builder_04=BuildCanyonAscent|script=res://tools/build/prologue/BuildCanyonAscent.cs',
                'builder_05=BuildLostTemple|script=res://tools/build/prologue/BuildLostTemple.cs',
                'builder_06=BuildPrologueLevel|script=res://tools/build/prologue/BuildPrologueLevel.cs'
            )
            Assert-Task6Certification ((@($materialization | Where-Object {
                        $_ -ceq 'schema=task6-capture-materialization-v1'
                    }).Count) -eq 1) 'capture materialization manifest has one schema marker'
            Assert-Task6Certification ((@($materialization | Where-Object {
                        $_ -ceq 'builder_count=6'
                    }).Count) -eq 1) 'capture materialization manifest binds exactly six builders'
            foreach ($expectedBuilder in $expectedBuilders) {
                Assert-Task6Certification ((@($materialization | Where-Object { $_ -ceq $expectedBuilder }).Count) -eq 1) `
                    "capture materialization records exact leaf-to-main builder: $expectedBuilder"
            }
            Assert-Task6Certification ((@($materialization | Where-Object {
                        $_ -match '^generated_runtime_semantics_sha256=[a-f0-9]{64}$'
                    }).Count) -eq 1) 'capture materialization binds one generated runtime semantic digest'
            Assert-Task6Certification ((@($materialization | Where-Object {
                        $_ -ceq 'generated_runtime_semantics_count=6'
                    }).Count) -eq 1) 'capture materialization binds six runtime semantic scenes'
            $importLogLine = @($materialization | Where-Object { $_ -match '^import_log=' })
            $importConsoleLine = @($materialization | Where-Object { $_ -match '^import_console=' })
            Assert-Task6Certification ($importLogLine.Count -eq 1) 'capture materialization binds one formal import log'
            Assert-Task6Certification ($importConsoleLine.Count -eq 1) 'capture materialization binds one formal import console'
            $importLog = $importLogLine[0].Substring('import_log='.Length)
            $importConsole = $importConsoleLine[0].Substring('import_console='.Length)
            Assert-Task6NoUnexpectedEngineErrors -Paths @($importLog, $importConsole) -Label 'capture formal import'
            $criticalLog = Join-Path $captureRoot 'task11-critical.log'
            $criticalConsole = Join-Path $captureRoot 'task11-critical.console.log'
            Assert-Task6NoUnexpectedEngineErrors -Paths @($criticalLog, $criticalConsole) -Label 'capture Task11 critical'
            Assert-Task6Certification ((@((Get-Content -LiteralPath $criticalLog) | Where-Object {
                        $_ -ceq 'PROLOGUE_CRITICAL_PATH_OK'
                    }).Count) -eq 1) 'capture imported materialization emits one Task11 critical terminal marker'
            $cleanupLog = Join-Path $captureRoot 'task6-capture-cleanup.log'
            Assert-Task6Certification (Test-Path -LiteralPath $cleanupLog -PathType Leaf) `
                'capture persists its own exact candidate-cleanup log'
            Assert-Task6Certification ((@((Get-Content -LiteralPath $cleanupLog) | Where-Object {
                        $_ -match '^TASK6_CAPTURE_CLEANUP_OK restored=\d+ created=\d+ cache_deleted=True status=empty$'
                    }).Count) -eq 1) 'capture cleanup reports exact restore and owned cache deletion'
            Assert-Task6Certification ((@(& git -C $worktree status --porcelain=v1 --untracked-files=all)).Count -eq 0) `
                'capture own cleanup returns the candidate to an empty status before test cleanup'
            Assert-Task6Certification (-not (Test-Path -LiteralPath (Join-Path $worktree '.godot'))) `
                'capture own cleanup removes its cache before test cleanup'
        }
        finally {
            if ($null -ne $stateLog) {
                $restore = Invoke-Task6CleanCandidateRestore -CandidateRoot $worktree -StateLog $stateLog `
                    -Name 'capture-materialization'
                Assert-Task6Certification ($restore.ExitCode -eq 0) (
                    'fresh imported materialization outer cleanup succeeds: ' + ($restore.Output -join "`n"))
                Assert-Task6Certification ((@(& git -C $worktree status --porcelain=v1 --untracked-files=all)).Count -eq 0) `
                    'fresh imported materialization outer cleanup restores an empty status'
                Assert-Task6Certification (-not (Test-Path -LiteralPath (Join-Path $worktree '.godot'))) `
                    'fresh imported materialization outer cleanup removes its cache'
                $restoreSucceeded = $true
            }
            [IO.File]::WriteAllLines((Join-Path $evidenceRoot 'capture-materialization-summary.txt'), @(
                    'candidate_worktree=' + $worktree,
                    'candidate_branch=' + $branch,
                    'capture_root=' + $captureRoot,
                    'audit_root=' + $auditRoot,
                    'child_output=' + $childOutput,
                    'preflight_state=' + $stateLog,
                    'outer_restore_succeeded=' + $restoreSucceeded
                ), [Text.UTF8Encoding]::new($false))
            if ($restoreSucceeded -and (Test-Path -LiteralPath $worktree)) {
                & git -C $repo worktree remove $worktree 2>$null
                Assert-Task6Certification ($LASTEXITCODE -eq 0) 'owned imported materialization worktree is removed'
                $worktreeRemoved = $true
            }
            if ($worktreeRemoved) {
                & git -C $repo branch -D $branch 2>$null
                Assert-Task6Certification ($LASTEXITCODE -eq 0) 'owned imported materialization branch is removed'
            }
        }
    }
}
