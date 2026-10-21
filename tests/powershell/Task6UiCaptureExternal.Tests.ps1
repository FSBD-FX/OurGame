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
if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) {
    $EvidenceRoot = Join-Path ([IO.Path]::GetTempPath()) (
        'knight-task6-ui-capture-external-' + [guid]::NewGuid().ToString('N'))
}
$EvidenceRoot = [IO.Path]::GetFullPath($EvidenceRoot).TrimEnd('\')
if ($EvidenceRoot.StartsWith(($ProjectRoot.TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Task 6 UI-capture test evidence must be outside the candidate worktree.'
}
if (Test-Path -LiteralPath $EvidenceRoot) {
    throw "Task 6 UI-capture test evidence root must be fresh: $EvidenceRoot"
}
New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null

$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$dotnet = 'G:\godot\dotnet-sdk\dotnet.exe'

function Assert-Task6UiCapture([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Task 6 external UI-capture assertion failed: $Message" }
}

function Get-Task6UiCaptureChildHost {
    $path = $null
    try { $path = (Get-Process -Id $PID -ErrorAction Stop).Path } catch { }
    if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path -LiteralPath $path -PathType Leaf)) {
        return $path
    }
    foreach ($name in @('pwsh', 'powershell.exe', 'powershell')) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($null -ne $command -and -not [string]::IsNullOrWhiteSpace($command.Source)) {
            return $command.Source
        }
    }
    throw 'Task 6 UI-capture test cannot resolve a compatible PowerShell host.'
}

function Invoke-Task6UiCapturePreflight([string]$CandidateRoot, [string]$Name) {
    $verificationRoot = Join-Path $EvidenceRoot ('preflight-' + $Name)
    $verify = Join-Path $ProjectRoot 'tools\verify-prologue.ps1'
    $hostPath = Get-Task6UiCaptureChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $hostPath -NoProfile -ExecutionPolicy Bypass -File $verify `
            -ProjectRoot $CandidateRoot -VerificationRoot $verificationRoot `
            -CleanCandidatePreflightOnly 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    [pscustomobject]@{
        ExitCode = $exitCode
        Text = (($output | ForEach-Object { $_.ToString() }) -join "`n")
        StateLog = Join-Path $verificationRoot 'candidate-clean-before.log'
    }
}

function Invoke-Task6UiCaptureRestore([string]$CandidateRoot, [string]$StateLog) {
    $verificationRoot = Join-Path $EvidenceRoot 'restore'
    $verify = Join-Path $ProjectRoot 'tools\verify-prologue.ps1'
    $hostPath = Get-Task6UiCaptureChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $hostPath -NoProfile -ExecutionPolicy Bypass -File $verify `
            -ProjectRoot $CandidateRoot -VerificationRoot $verificationRoot `
            -CleanCandidateRestoreOnly -CleanCandidateStateLog $StateLog 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    [pscustomobject]@{ ExitCode = $exitCode; Text = (($output | ForEach-Object { $_.ToString() }) -join "`n") }
}

function Invoke-Task6UiCaptureRunner {
    param(
        [Parameter(Mandatory)][string]$CandidateRoot,
        [Parameter(Mandatory)][string]$Name,
        [AllowNull()][string]$CaptureRoot,
        [AllowNull()][string]$CaptureEvidenceRoot
    )

    $root = Join-Path $EvidenceRoot $Name
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    $log = Join-Path $root 'ui-resolution.log'
    $console = Join-Path $root 'ui-resolution.console.log'
    $priorCapture = [Environment]::GetEnvironmentVariable('KNIGHT_UI_CAPTURE_ROOT', 'Process')
    $priorEvidence = [Environment]::GetEnvironmentVariable('KNIGHT_UI_CAPTURE_EVIDENCE_ROOT', 'Process')
    $priorCertification = [Environment]::GetEnvironmentVariable('KNIGHT_UI_CAPTURE_CERTIFICATION', 'Process')
    try {
        [Environment]::SetEnvironmentVariable('KNIGHT_UI_CAPTURE_ROOT', $CaptureRoot, 'Process')
        [Environment]::SetEnvironmentVariable('KNIGHT_UI_CAPTURE_EVIDENCE_ROOT', $CaptureEvidenceRoot, 'Process')
        [Environment]::SetEnvironmentVariable('KNIGHT_UI_CAPTURE_CERTIFICATION', '1', 'Process')
        $prior = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $output = & $godot --path $CandidateRoot --fixed-fps 60 --display-driver windows `
                --rendering-method gl_compatibility --audio-driver Dummy --script `
                'res://tests/presentation/PrologueUiResolutionCapture.cs' --log-file $log 2>&1
            $exitCode = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $prior }
        [IO.File]::WriteAllText($console, (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
            [Text.UTF8Encoding]::new($false))
        return [pscustomobject]@{ ExitCode = $exitCode; Log = $log; Console = $console }
    }
    finally {
        [Environment]::SetEnvironmentVariable('KNIGHT_UI_CAPTURE_ROOT', $priorCapture, 'Process')
        [Environment]::SetEnvironmentVariable('KNIGHT_UI_CAPTURE_EVIDENCE_ROOT', $priorEvidence, 'Process')
        [Environment]::SetEnvironmentVariable('KNIGHT_UI_CAPTURE_CERTIFICATION', $priorCertification, 'Process')
    }
}

function Invoke-Task6UiCaptureFocusedAudit([string]$CandidateRoot, [string]$AuditRoot) {
    $hostPath = Get-Task6UiCaptureChildHost
    $audit = Join-Path $CandidateRoot 'tools\audit-prologue-base-hardgates.ps1'
    $console = Join-Path $EvidenceRoot 'focused-audit.console.log'
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $hostPath -NoProfile -ExecutionPolicy Bypass -File $audit `
            -AuditRoot $AuditRoot -SkipBuild -UiResolutionOnly 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    [IO.File]::WriteAllText($console, (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
        [Text.UTF8Encoding]::new($false))
    [pscustomobject]@{ ExitCode = $exitCode; Console = $console }
}

function Invoke-Task6UiCaptureManifestGate([string]$CandidateRoot, [string]$AuditRoot, [string]$Name) {
    $hostPath = Get-Task6UiCaptureChildHost
    $audit = Join-Path $CandidateRoot 'tools\audit-prologue-base-hardgates.ps1'
    $console = Join-Path $EvidenceRoot ($Name + '.manifest.console.log')
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $hostPath -NoProfile -ExecutionPolicy Bypass -File $audit `
            -AuditRoot $AuditRoot -UiResolutionManifestOnly 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    [IO.File]::WriteAllText($console, (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
        [Text.UTF8Encoding]::new($false))
    [pscustomobject]@{ ExitCode = $exitCode; Console = $console }
}

function Copy-Task6UiCaptureFixture([string]$SourceRoot, [string]$DestinationRoot) {
    Copy-Item -LiteralPath $SourceRoot -Destination $DestinationRoot -Recurse
    foreach ($relative in @('ui-resolution-capture-manifest.txt', 'audit-prologue-ui-resolution.log')) {
        $path = Join-Path $DestinationRoot $relative
        $text = [IO.File]::ReadAllText($path)
        $updated = $text.Replace($SourceRoot, $DestinationRoot)
        Assert-Task6UiCapture ($updated -cne $text) "fixture path relocation changes $relative"
        [IO.File]::WriteAllText($path, $updated, [Text.UTF8Encoding]::new($false))
    }
}

$script:UiCaptureRepo = (& git -C $ProjectRoot rev-parse --show-toplevel).Trim()
$script:UiCaptureSuffix = [guid]::NewGuid().ToString('N')
$script:UiCaptureBranch = 'codex/task6-ui-capture-test-' + $script:UiCaptureSuffix
$script:UiCaptureCandidate = Join-Path ([IO.Path]::GetTempPath()) (
    'knight-task6-ui-capture-' + $script:UiCaptureSuffix)
$script:UiCaptureStateLog = $null
$script:UiCaptureAuditRoot = $null

Describe 'Task 6 external UI capture boundary' {
    BeforeAll {
        & git -C $script:UiCaptureRepo worktree add -b $script:UiCaptureBranch $script:UiCaptureCandidate HEAD | Out-Null
        Assert-Task6UiCapture ($LASTEXITCODE -eq 0) 'temporary named UI-capture candidate materializes'
        Assert-Task6UiCapture ((@(& git -C $script:UiCaptureCandidate status --porcelain=v1 --untracked-files=all)).Count -eq 0) `
            'temporary UI-capture candidate starts clean'
        Assert-Task6UiCapture (-not (Test-Path -LiteralPath (Join-Path $script:UiCaptureCandidate '.godot'))) `
            'temporary UI-capture candidate starts cache-free'
        $preflight = Invoke-Task6UiCapturePreflight -CandidateRoot $script:UiCaptureCandidate -Name 'before'
        Assert-Task6UiCapture ($preflight.ExitCode -eq 0) ('candidate preflight passes: ' + $preflight.Text)
        Assert-Task6UiCapture (Test-Path -LiteralPath $preflight.StateLog -PathType Leaf) 'preflight state exists'
        $script:UiCaptureStateLog = $preflight.StateLog
        & $dotnet build (Join-Path $script:UiCaptureCandidate 'Knight.csproj') | Out-Null
        Assert-Task6UiCapture ($LASTEXITCODE -eq 0) 'temporary UI-capture candidate builds after cache-free preflight'
    }

    It 'rejects missing capture-root environment through the actual Godot runner' {
        $result = Invoke-Task6UiCaptureRunner -CandidateRoot $script:UiCaptureCandidate -Name 'missing-root' `
            -CaptureRoot $null -CaptureEvidenceRoot (Join-Path $EvidenceRoot 'declared-root')
        $text = (Get-Content -LiteralPath $result.Log -Raw) + (Get-Content -LiteralPath $result.Console -Raw)
        Assert-Task6UiCapture ($result.ExitCode -ne 0) 'missing capture root fails the actual Godot child'
        Assert-Task6UiCapture ($text -match 'PROLOGUE_UI_CAPTURE_ROOT_REQUIRED') `
            'missing capture root carries the explicit fail-closed label'
    }

    It 'rejects candidate-internal and outside-evidence output roots through the actual Godot runner' {
        $declared = Join-Path $EvidenceRoot 'declared-root'
        $inside = Invoke-Task6UiCaptureRunner -CandidateRoot $script:UiCaptureCandidate -Name 'inside-project' `
            -CaptureRoot (Join-Path $script:UiCaptureCandidate 'screenshots\task6-ui-capture') -CaptureEvidenceRoot $declared
        $insideText = (Get-Content -LiteralPath $inside.Log -Raw) + (Get-Content -LiteralPath $inside.Console -Raw)
        Assert-Task6UiCapture ($inside.ExitCode -ne 0) 'candidate-internal root fails the actual Godot child'
        Assert-Task6UiCapture ($insideText -match 'PROLOGUE_UI_CAPTURE_ROOT_INSIDE_PROJECT') `
            'candidate-internal root carries the explicit fail-closed label'

        $outside = Invoke-Task6UiCaptureRunner -CandidateRoot $script:UiCaptureCandidate -Name 'outside-evidence' `
            -CaptureRoot (Join-Path $EvidenceRoot 'outside-declared-root') -CaptureEvidenceRoot $declared
        $outsideText = (Get-Content -LiteralPath $outside.Log -Raw) + (Get-Content -LiteralPath $outside.Console -Raw)
        Assert-Task6UiCapture ($outside.ExitCode -ne 0) 'outside-evidence root fails the actual Godot child'
        Assert-Task6UiCapture ($outsideText -match 'PROLOGUE_UI_CAPTURE_ROOT_OUTSIDE_EVIDENCE') `
            'outside-evidence root carries the explicit fail-closed label'
    }

    It 'writes a fresh four-resolution hash manifest entirely outside the candidate through the audit entrypoint' {
        $auditRoot = Join-Path $EvidenceRoot 'focused-audit-root'
        $result = Invoke-Task6UiCaptureFocusedAudit -CandidateRoot $script:UiCaptureCandidate -AuditRoot $auditRoot
        $text = Get-Content -LiteralPath $result.Console -Raw
        Assert-Task6UiCapture ($result.ExitCode -eq 0) ('focused UI audit passes: ' + $text)
        Assert-Task6UiCapture ($text -match '(?m)^TASK6_UI_CAPTURE_EXTERNAL_OK .*pngs=222 ' ) `
            'focused audit emits its hash-bound external capture terminal'
        $manifest = Join-Path $auditRoot 'ui-resolution-capture-manifest.txt'
        Assert-Task6UiCapture (Test-Path -LiteralPath $manifest -PathType Leaf) 'UI capture manifest exists externally'
        $manifestLines = @(Get-Content -LiteralPath $manifest)
        Assert-Task6UiCapture ((@($manifestLines | Where-Object { $_ -match '^file=' })).Count -eq 222) `
            'manifest binds every 4-resolution PNG path and hash'
        Assert-Task6UiCapture ((@($manifestLines | Where-Object { $_ -match '^resolution_count=4$' })).Count -eq 1) `
            'manifest records exactly four audited resolutions'
        Assert-Task6UiCapture (-not (Test-Path -LiteralPath (Join-Path $script:UiCaptureCandidate 'screenshots\task10-final-hud'))) `
            'audit creates no candidate-root screenshot fallback'
        $script:UiCaptureAuditRoot = $auditRoot
    }

    It 'rejects stale and hash-tampered external PNG evidence through the actual manifest child gate' {
        Assert-Task6UiCapture (-not [string]::IsNullOrWhiteSpace($script:UiCaptureAuditRoot)) `
            'fresh focused audit exists before manifest mutations'
        $hashRoot = Join-Path $EvidenceRoot 'hash-tamper-root'
        $staleRoot = Join-Path $EvidenceRoot 'stale-root'
        Copy-Task6UiCaptureFixture -SourceRoot $script:UiCaptureAuditRoot -DestinationRoot $hashRoot
        Copy-Task6UiCaptureFixture -SourceRoot $script:UiCaptureAuditRoot -DestinationRoot $staleRoot
        $hashTarget = Get-ChildItem -LiteralPath (Join-Path $hashRoot 'ui-resolution-captures') `
            -File -Recurse -Filter '*.png' | Select-Object -First 1
        $hashBytes = [IO.File]::ReadAllBytes($hashTarget.FullName)
        $hashBytes[0] = $hashBytes[0] -bxor 1
        [IO.File]::WriteAllBytes($hashTarget.FullName, $hashBytes)
        $hashResult = Invoke-Task6UiCaptureManifestGate -CandidateRoot $script:UiCaptureCandidate -AuditRoot $hashRoot -Name 'hash-tamper'
        $hashText = Get-Content -LiteralPath $hashResult.Console -Raw
        Assert-Task6UiCapture ($hashResult.ExitCode -ne 0) 'hash-tampered PNG fails actual manifest child gate'
        Assert-Task6UiCapture ($hashText -match 'PROLOGUE_UI_CAPTURE_HASH_MISMATCH') `
            'hash tamper reports the explicit fail-closed label'

        $staleTarget = Get-ChildItem -LiteralPath (Join-Path $staleRoot 'ui-resolution-captures') `
            -File -Recurse -Filter '*.png' | Select-Object -First 1
        $staleTarget.LastWriteTimeUtc = $staleTarget.LastWriteTimeUtc.AddMinutes(-5)
        $staleResult = Invoke-Task6UiCaptureManifestGate -CandidateRoot $script:UiCaptureCandidate -AuditRoot $staleRoot -Name 'stale-tamper'
        $staleText = Get-Content -LiteralPath $staleResult.Console -Raw
        Assert-Task6UiCapture ($staleResult.ExitCode -ne 0) 'stale PNG fails actual manifest child gate'
        Assert-Task6UiCapture ($staleText -match 'PROLOGUE_UI_CAPTURE_NOT_FRESH') `
            'stale PNG reports the explicit fail-closed label'
    }

    AfterAll {
        if (-not [string]::IsNullOrWhiteSpace($script:UiCaptureStateLog)) {
            $restore = Invoke-Task6UiCaptureRestore -CandidateRoot $script:UiCaptureCandidate -StateLog $script:UiCaptureStateLog
            Assert-Task6UiCapture ($restore.ExitCode -eq 0) ('candidate cleanup succeeds: ' + $restore.Text)
            Assert-Task6UiCapture ((@(& git -C $script:UiCaptureCandidate status --porcelain=v1 --untracked-files=all)).Count -eq 0) `
                'candidate cleanup restores empty porcelain status'
            Assert-Task6UiCapture (-not (Test-Path -LiteralPath (Join-Path $script:UiCaptureCandidate '.godot'))) `
                'candidate cleanup removes only this run cache'
        }
        if (Test-Path -LiteralPath $script:UiCaptureCandidate) {
            $resolved = (Resolve-Path -LiteralPath $script:UiCaptureCandidate).Path
            Assert-Task6UiCapture ($resolved.StartsWith(([IO.Path]::GetTempPath()).TrimEnd('\'),
                    [StringComparison]::OrdinalIgnoreCase)) 'test cleanup target stays inside temp root'
            & git -C $script:UiCaptureRepo worktree remove --force $resolved 2>$null
        }
        & git -C $script:UiCaptureRepo branch -D $script:UiCaptureBranch 2>$null
    }
}
