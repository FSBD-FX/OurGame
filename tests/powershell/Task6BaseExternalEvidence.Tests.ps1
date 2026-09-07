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
    $EvidenceRoot = Join-Path ([IO.Path]::GetTempPath()) `
        ('knight-task6-base-external-evidence-' + [guid]::NewGuid().ToString('N'))
}
$EvidenceRoot = [IO.Path]::GetFullPath($EvidenceRoot)
if ([IO.Path]::GetFullPath($EvidenceRoot).TrimEnd('\').StartsWith(
        ($ProjectRoot.TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Task 6 BASE external-evidence test root must be outside the candidate worktree.'
}
if (Test-Path -LiteralPath $EvidenceRoot) {
    throw "Task 6 BASE external-evidence test root must be fresh: $EvidenceRoot"
}
New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null

function Assert-Task6BaseExternalEvidence([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Task 6 BASE external-evidence assertion failed: $Message" }
}

function Get-Task6BaseExternalSha256([string]$LiteralPath) {
    # Pester 3 can execute this file in a child runspace whose module auto-loading state differs
    # from the launcher.  Hash fixtures with the framework directly instead of assuming the
    # Microsoft.PowerShell.Utility Get-FileHash cmdlet is discoverable in that runspace.
    $stream = [IO.File]::OpenRead($LiteralPath)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
        $stream.Dispose()
    }
}

function Get-Task6BaseExternalChildHost {
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
    throw 'Task 6 BASE external-evidence test cannot locate a PowerShell child host.'
}

function Invoke-Task6BaseExternalEvidenceGate([string]$Name, [string[]]$Arguments) {
    $base = Join-Path $ProjectRoot 'tools\verify-prologue-base-hardgates.ps1'
    $childHost = Get-Task6BaseExternalChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $base @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    $log = Join-Path $EvidenceRoot ($Name + '.child.log')
    [IO.File]::WriteAllText($log, (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
        [Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{ ExitCode = $exitCode; Text = (($output | ForEach-Object { $_.ToString() }) -join "`n"); Log = $log }
}

function Write-Task6BaseExternalManifest([string]$Path, [string]$Root, [string]$CreatedAfterUtc) {
    $lines = @(
        'schema=task6-base-external-evidence-v1',
        ('run_id=' + [guid]::NewGuid().ToString()),
        ('candidate_root=' + $ProjectRoot),
        ('evidence_root=' + $Root),
        ('created_after_utc=' + $CreatedAfterUtc),
        'log_count=0'
    )
    [IO.File]::WriteAllLines($Path, $lines, [Text.UTF8Encoding]::new($false))
}

function Write-Task6FreshBaseExternalManifest([string]$Path, [string]$Root, [DateTime]$CreatedAfterUtc) {
    $files = @(
        'verify-prologue-attack-presentation.log',
        'verify-prologue-base-attack-presentation.log',
        'verify-prologue-smoke.log',
        'verify-prologue-generated-runtime-semantics.txt'
    )
    foreach ($relative in $files) {
        $file = Join-Path $Root $relative
        if ($relative -ceq 'verify-prologue-generated-runtime-semantics.txt') {
            $semanticLines = @(
                ('PROLOGUE_GENERATED_RUNTIME_SEMANTICS_OK sha=' + ('a' * 64) + ' count=6'),
                ('path=scenes/prologue/sectors/CanyonEntrance.tscn sha256=' + ('b' * 64)),
                ('path=scenes/prologue/sectors/CanyonDescent.tscn sha256=' + ('c' * 64)),
                ('path=scenes/prologue/sectors/CanyonBasin.tscn sha256=' + ('d' * 64)),
                ('path=scenes/prologue/sectors/CanyonAscent.tscn sha256=' + ('e' * 64)),
                ('path=scenes/prologue/sectors/LostTemple.tscn sha256=' + ('f' * 64)),
                ('path=scenes/prologue/PrologueLevel.tscn sha256=' + ('0' * 64))
            )
            [IO.File]::WriteAllLines($file, $semanticLines, [Text.UTF8Encoding]::new($false))
        }
        else {
            [IO.File]::WriteAllText($file, "fresh external evidence $relative`n", [Text.UTF8Encoding]::new($false))
        }
    }
    $lines = @(
        'schema=task6-base-external-evidence-v1',
        ('run_id=' + [guid]::NewGuid().ToString()),
        ('candidate_root=' + $ProjectRoot),
        ('evidence_root=' + $Root),
        ('created_after_utc=' + $CreatedAfterUtc.ToUniversalTime().ToString('o')),
        ('generated_runtime_sha=' + ('a' * 64)),
        ('log_count=' + $files.Count)
    )
    foreach ($relative in $files) {
        $file = Join-Path $Root $relative
        $lines += ('file=' + $relative + '|' +
            (Get-Task6BaseExternalSha256 -LiteralPath $file) + '|' +
            (Get-Item -LiteralPath $file).LastWriteTimeUtc.ToString('o'))
    }
    [IO.File]::WriteAllLines($Path, $lines, [Text.UTF8Encoding]::new($false))
}

function Repair-Task6BaseExternalManifestFileHash([string]$Manifest, [string]$Root, [string]$Relative) {
    $path = Join-Path $Root $Relative
    $prefix = 'file=' + $Relative + '|'
    $lines = @(Get-Content -LiteralPath $Manifest)
    $replaced = $false
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index].StartsWith($prefix, [StringComparison]::Ordinal)) {
            $lines[$index] = $prefix +
                (Get-Task6BaseExternalSha256 -LiteralPath $path) + '|' +
                (Get-Item -LiteralPath $path).LastWriteTimeUtc.ToString('o')
            $replaced = $true
            break
        }
    }
    Assert-Task6BaseExternalEvidence $replaced "manifest contains hash-bound file row: $Relative"
    [IO.File]::WriteAllLines($Manifest, $lines, [Text.UTF8Encoding]::new($false))
}

Describe 'Task 6 BASE external evidence boundary' {
    It 'requires an external root before a normal BASE run can create project-root logs' {
        $result = Invoke-Task6BaseExternalEvidenceGate -Name 'normal-root-required' -Arguments @('-SuppressTerminal')
        Assert-Task6BaseExternalEvidence ($result.ExitCode -ne 0) 'normal BASE without an external root produces a failing child gate'
        Assert-Task6BaseExternalEvidence ($result.Text -match 'BASE_EXTERNAL_EVIDENCE_ROOT_REQUIRED') `
            'normal BASE without an external root has the explicit fail-closed label'
    }

    It 'rejects a missing explicit BASE evidence manifest rather than falling back to project-root logs' {
        $result = Invoke-Task6BaseExternalEvidenceGate -Name 'missing-manifest' -Arguments @(
            '-ExternalEvidenceManifestOnly', '-ExternalEvidenceNotBeforeUtc', [DateTime]::UtcNow.ToString('o'))
        Assert-Task6BaseExternalEvidence ($result.ExitCode -ne 0) 'missing external manifest produces a failing child gate'
        Assert-Task6BaseExternalEvidence ($result.Text -match 'BASE_EXTERNAL_EVIDENCE_MANIFEST_REQUIRED') `
            'missing external manifest has the explicit fail-closed label'
    }

    It 'rejects a stale external BASE evidence manifest before reading any fallback path' {
        $root = Join-Path $EvidenceRoot 'stale-root'
        New-Item -ItemType Directory -Path $root -Force | Out-Null
        $manifest = Join-Path $root 'base-evidence-manifest.txt'
        Write-Task6BaseExternalManifest -Path $manifest -Root $root -CreatedAfterUtc '2000-01-01T00:00:00.0000000Z'
        $result = Invoke-Task6BaseExternalEvidenceGate -Name 'stale-manifest' -Arguments @(
            '-ExternalEvidenceManifestOnly', '-ExternalEvidenceManifest', $manifest,
            '-ExternalEvidenceNotBeforeUtc', [DateTime]::UtcNow.ToString('o'))
        Assert-Task6BaseExternalEvidence ($result.ExitCode -ne 0) 'stale external manifest produces a failing child gate'
        Assert-Task6BaseExternalEvidence ($result.Text -match 'BASE_EXTERNAL_EVIDENCE_STALE') `
            'stale external manifest has the explicit fail-closed label'
    }

    It 'rejects a manifest whose declared evidence root is inside the candidate project' {
        $manifest = Join-Path $EvidenceRoot 'project-root-fallback-manifest.txt'
        Write-Task6BaseExternalManifest -Path $manifest -Root $ProjectRoot `
            -CreatedAfterUtc [DateTime]::UtcNow.ToString('o')
        $result = Invoke-Task6BaseExternalEvidenceGate -Name 'project-root-fallback' -Arguments @(
            '-ExternalEvidenceManifestOnly', '-ExternalEvidenceManifest', $manifest,
            '-ExternalEvidenceNotBeforeUtc', [DateTime]::UtcNow.AddMinutes(-1).ToString('o'))
        Assert-Task6BaseExternalEvidence ($result.ExitCode -ne 0) 'project-root manifest produces a failing child gate'
        Assert-Task6BaseExternalEvidence ($result.Text -match 'BASE_EXTERNAL_EVIDENCE_ROOT_INSIDE_PROJECT') `
            'project-root manifest has the explicit fail-closed label'
    }

    It 'rejects a manifest stored outside its declared external evidence root' {
        $root = Join-Path $EvidenceRoot 'declared-root'
        New-Item -ItemType Directory -Path $root -Force | Out-Null
        $manifest = Join-Path $EvidenceRoot 'path-outside-root-manifest.txt'
        Write-Task6BaseExternalManifest -Path $manifest -Root $root `
            -CreatedAfterUtc [DateTime]::UtcNow.ToString('o')
        $result = Invoke-Task6BaseExternalEvidenceGate -Name 'path-outside-root' -Arguments @(
            '-ExternalEvidenceManifestOnly', '-ExternalEvidenceManifest', $manifest,
            '-ExternalEvidenceNotBeforeUtc', [DateTime]::UtcNow.AddMinutes(-1).ToString('o'))
        Assert-Task6BaseExternalEvidence ($result.ExitCode -ne 0) 'path-outside manifest produces a failing child gate'
        Assert-Task6BaseExternalEvidence ($result.Text -match 'BASE_EXTERNAL_EVIDENCE_PATH_OUTSIDE_ROOT') `
            'path-outside manifest has the explicit fail-closed label'
    }

    It 'accepts only a fresh hash-bound external manifest with required BASE evidence' {
        $root = Join-Path $EvidenceRoot 'fresh-root'
        New-Item -ItemType Directory -Path $root -Force | Out-Null
        $manifest = Join-Path $root 'base-evidence-manifest.txt'
        $notBeforeUtc = [DateTime]::UtcNow.AddMinutes(-1)
        Write-Task6FreshBaseExternalManifest -Path $manifest -Root $root -CreatedAfterUtc $notBeforeUtc
        $result = Invoke-Task6BaseExternalEvidenceGate -Name 'fresh-manifest' -Arguments @(
            '-ExternalEvidenceManifestOnly', '-ExternalEvidenceManifest', $manifest,
            '-ExternalEvidenceNotBeforeUtc', $notBeforeUtc.ToString('o'))
        Assert-Task6BaseExternalEvidence ($result.ExitCode -eq 0) 'fresh external manifest passes the actual child gate'
        Assert-Task6BaseExternalEvidence ((@($result.Text -split "`r?`n" | Where-Object {
                    $_ -match '^PROLOGUE_BASE_EXTERNAL_EVIDENCE_OK '
                }).Count) -eq 1) 'fresh external manifest emits one child terminal'
    }

    It 'rejects a hash-rebound generated-runtime semantic digest mutation through the actual child gate' {
        $root = Join-Path $EvidenceRoot 'semantic-digest-tamper-root'
        New-Item -ItemType Directory -Path $root -Force | Out-Null
        $manifest = Join-Path $root 'base-evidence-manifest.txt'
        $notBeforeUtc = [DateTime]::UtcNow.AddMinutes(-1)
        Write-Task6FreshBaseExternalManifest -Path $manifest -Root $root -CreatedAfterUtc $notBeforeUtc
        $semantic = Join-Path $root 'verify-prologue-generated-runtime-semantics.txt'
        $text = [IO.File]::ReadAllText($semantic)
        $mutated = $text.Replace(('sha=' + ('a' * 64)), ('sha=' + ('1' * 64)))
        Assert-Task6BaseExternalEvidence ($mutated -cne $text) 'semantic digest mutation changes copied evidence bytes'
        [IO.File]::WriteAllText($semantic, $mutated, [Text.UTF8Encoding]::new($false))
        Repair-Task6BaseExternalManifestFileHash -Manifest $manifest -Root $root `
            -Relative 'verify-prologue-generated-runtime-semantics.txt'
        $result = Invoke-Task6BaseExternalEvidenceGate -Name 'semantic-digest-tamper' -Arguments @(
            '-ExternalEvidenceManifestOnly', '-ExternalEvidenceManifest', $manifest,
            '-ExternalEvidenceNotBeforeUtc', $notBeforeUtc.ToString('o'))
        Assert-Task6BaseExternalEvidence ($result.ExitCode -ne 0) `
            'hash-rebound generated-runtime semantic digest mutation fails the actual child gate'
        Assert-Task6BaseExternalEvidence ($result.Text -match 'BASE_GENERATED_RUNTIME_SEMANTICS_HASH_MISMATCH') `
            'semantic digest mutation reports the explicit binding label'
    }

    It 'rejects a hash-rebound generated-runtime leaf topology path mutation through the actual child gate' {
        $root = Join-Path $EvidenceRoot 'topology-path-tamper-root'
        New-Item -ItemType Directory -Path $root -Force | Out-Null
        $manifest = Join-Path $root 'base-evidence-manifest.txt'
        $notBeforeUtc = [DateTime]::UtcNow.AddMinutes(-1)
        Write-Task6FreshBaseExternalManifest -Path $manifest -Root $root -CreatedAfterUtc $notBeforeUtc
        $semantic = Join-Path $root 'verify-prologue-generated-runtime-semantics.txt'
        $text = [IO.File]::ReadAllText($semantic)
        $mutated = $text.Replace('CanyonDescent.tscn', 'CanyonDescentTampered.tscn')
        Assert-Task6BaseExternalEvidence ($mutated -cne $text) 'topology path mutation changes copied evidence bytes'
        [IO.File]::WriteAllText($semantic, $mutated, [Text.UTF8Encoding]::new($false))
        Repair-Task6BaseExternalManifestFileHash -Manifest $manifest -Root $root `
            -Relative 'verify-prologue-generated-runtime-semantics.txt'
        $result = Invoke-Task6BaseExternalEvidenceGate -Name 'topology-path-tamper' -Arguments @(
            '-ExternalEvidenceManifestOnly', '-ExternalEvidenceManifest', $manifest,
            '-ExternalEvidenceNotBeforeUtc', $notBeforeUtc.ToString('o'))
        Assert-Task6BaseExternalEvidence ($result.ExitCode -ne 0) `
            'hash-rebound generated-runtime topology path mutation fails the actual child gate'
        Assert-Task6BaseExternalEvidence ($result.Text -match 'BASE_GENERATED_RUNTIME_SEMANTICS_PATH_MISMATCH') `
            'topology path mutation reports the explicit ordered-path label'
    }
}
