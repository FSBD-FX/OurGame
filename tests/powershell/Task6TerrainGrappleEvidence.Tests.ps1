[CmdletBinding()]
param([string]$ProjectRoot)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}
. (Join-Path $ProjectRoot 'tools\EvidenceChecks.ps1')
. (Join-Path $ProjectRoot 'tools\Task6AviStreaming.ps1')

$captureRoot = $env:KNIGHT_TASK6_MUTATION_CAPTURE_ROOT
$mutationRoot = $env:KNIGHT_TASK6_MUTATION_ROOT
$evidenceLog = $env:KNIGHT_TASK6_PESTER_EVIDENCE_LOG
$sourceMutationRoot = $env:KNIGHT_TASK6_SOURCE_MUTATION_ROOT
$baseEvidenceManifest = $env:KNIGHT_TASK6_BASE_EVIDENCE_MANIFEST
$baseEvidenceNotBeforeUtc = $env:KNIGHT_TASK6_BASE_EVIDENCE_NOT_BEFORE_UTC
$script:EvidenceMarkers = [Collections.Generic.List[string]]::new()
$script:CanonicalAccepted = $false
$script:MutationCount = 0
$script:HostProbeCount = 0
$script:HostFingerprints = @{}

function Assert-Task6([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Task 6 evidence assertion failed: $Message" }
}

function Get-Task6ChildHost {
    $processPath = $null
    try { $processPath = (Get-Process -Id $PID -ErrorAction Stop).Path } catch { }
    if (-not [string]::IsNullOrWhiteSpace($processPath) -and
        (Test-Path -LiteralPath $processPath -PathType Leaf)) {
        return $processPath
    }
    foreach ($name in @('pwsh', 'powershell.exe', 'powershell')) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($null -ne $command) { return $command.Source }
    }
    throw 'Task 6 cannot locate a PowerShell child host.'
}

function Get-Task6BoundBaseExternalEvidence {
    Assert-Task6 (-not [string]::IsNullOrWhiteSpace($baseEvidenceManifest)) `
        'Pester requires an explicit BASE external-evidence manifest; project-root fallback is forbidden'
    Assert-Task6 (-not [string]::IsNullOrWhiteSpace($baseEvidenceNotBeforeUtc)) `
        'Pester requires an explicit BASE external-evidence freshness boundary'
    $notBeforeUtc = [DateTime]::MinValue
    Assert-Task6 ([DateTime]::TryParse($baseEvidenceNotBeforeUtc,
            [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind,
            [ref]$notBeforeUtc)) 'Pester BASE external-evidence freshness boundary is a round-trip UTC timestamp'

    $base = Join-Path $ProjectRoot 'tools\verify-prologue-base-hardgates.ps1'
    $childHost = Get-Task6ChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $base `
            -ExternalEvidenceManifestOnly -ExternalEvidenceManifest $baseEvidenceManifest `
            -ExternalEvidenceNotBeforeUtc $notBeforeUtc.ToUniversalTime().ToString('o') 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    $childLog = Join-Path $mutationRoot 'base-external-evidence.verify.log'
    [IO.File]::WriteAllText($childLog, (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
        [Text.UTF8Encoding]::new($false))
    Assert-Task6 ($exitCode -eq 0) (
        'Pester BASE external-evidence child gate rejects missing, stale, path-outside, and project-root fallback manifests: ' +
        (($output | ForEach-Object { $_.ToString() }) -join "`n"))
    $terminal = @($output | Where-Object { $_.ToString() -match '^PROLOGUE_BASE_EXTERNAL_EVIDENCE_OK ' })
    Assert-Task6 ($terminal.Count -eq 1) 'Pester BASE external-evidence child gate emits exactly one terminal'

    $files = @{}
    foreach ($line in @(Get-Content -LiteralPath $baseEvidenceManifest)) {
        if ($line -match '^file=(?<relative>[^|]+)\|[A-Fa-f0-9]{64}\|.+$') {
            $files[$Matches['relative'].Replace('/', '\')] = $true
        }
    }
    foreach ($required in @(
        'verify-prologue-attack-presentation.log',
        'verify-prologue-base-attack-presentation.log',
        'verify-prologue-smoke.log'
    )) {
        Assert-Task6 ($files.ContainsKey($required)) "Pester external manifest binds BASE log: $required"
    }
    return [pscustomobject]@{
        Manifest = (Resolve-Path -LiteralPath $baseEvidenceManifest).Path
        ChildLog = $childLog
        Files = $files
        NotBeforeUtc = $notBeforeUtc.ToUniversalTime()
    }
}

function Get-Task6BoundBaseEvidenceLog([string]$RelativePath) {
    Assert-Task6 ($null -ne $script:BaseExternalEvidence) 'BASE external evidence is validated before consumers resolve logs'
    $root = (Get-Task6ManifestValues -Path $script:BaseExternalEvidence.Manifest)['evidence_root']
    Assert-Task6 (-not [string]::IsNullOrWhiteSpace([string]$root)) 'validated BASE manifest has an evidence root'
    $path = Join-Path $root $RelativePath
    Assert-Task6 (Test-Path -LiteralPath $path -PathType Leaf) "validated BASE evidence log exists: $RelativePath"
    return $path
}

function Get-Task6ManifestValues([string]$Path) {
    Assert-Task6 (Test-Path -LiteralPath $Path -PathType Leaf) "capture manifest exists: $Path"
    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $parts = $line -split '=', 2
        if ($parts.Count -eq 2) { $values[$parts[0]] = $parts[1] }
    }
    return $values
}

function Set-Task6ManifestValue([string]$Path, [string]$Name, [string]$Value) {
    $lines = @((Get-Content -LiteralPath $Path) | ForEach-Object {
        if ($_ -match ('^' + [regex]::Escape($Name) + '=')) { "$Name=$Value" } else { $_ }
    })
    $matchingLineCount = @($lines | Where-Object { $_ -ceq "$Name=$Value" }).Count
    Assert-Task6 ($matchingLineCount -eq 1) "manifest has one rewritten $Name"
    [IO.File]::WriteAllText($Path, (($lines -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
}

function Set-Task6ManifestHash([string]$Manifest, [string]$Name, [string]$Path) {
    $hash = Get-EvidenceSha256 -LiteralPath $Path
    Set-Task6ManifestValue -Path $Manifest -Name ($Name + '_sha256') -Value $hash
}

$script:FingerprintRelativePaths = @(
    'project.godot', 'Knight.csproj',
    'tests\smoke\PrologueCriticalPathSmokeTask6.cs',
    'tests\presentation\ProloguePresentationTask6.cs',
    'tests\presentation\PrologueBaseAttackPresentation.cs',
    'tests\presentation\BossRetryPresentationTask6.cs',
    'tests\presentation\PrologueVisualAuditTask6.cs',
    'tests\presentation\PrologueUiResolutionCapture.cs',
    'tests\RunTask4AudioVfxTests.cs',
    'tests\integration\PrologueCanyonTraversalTests.cs',
    'tests\integration\PrologueCanyonGeneratedMainRouteTests.cs',
    'tests\integration\PrologueTask9GeneratedMainTests.cs',
    'tests\integration\FallenBellkeeperRuntimeTests.cs',
    'tests\RunTask4UiTests.cs',
    'scripts\prologue\guidance\PrologueGuidanceTargetView.cs',
    'assets\localization\prologue-ui.json',
    'scripts\prologue\PrologueCompletionController.cs',
    'scripts\player\PlayerInputReader.cs',
    'scripts\ui\PrologueActionModeVisual.cs',
    'scripts\ui\PrologueFormalHud.cs',
    'scripts\ui\WorldTargetMarkerVisual.cs',
    'scripts\ui\ProtectedPromptPlacer.cs',
    'tests\powershell\Task6TerrainGrappleEvidence.Tests.ps1',
    'tests\powershell\Task6ExactFrameContract.Tests.ps1',
    'tests\powershell\Task6BaseExternalEvidence.Tests.ps1',
    'tests\powershell\Task6CaptureHostCompatibility.Tests.ps1',
    'tests\powershell\Task6CleanCandidateCertification.Tests.ps1',
    'tests\powershell\Task6ImportNativeRetry.Tests.ps1',
    'tests\powershell\Task6UiCaptureExternal.Tests.ps1',
    'tools\EvidenceChecks.ps1', 'tools\Task6AviStreaming.ps1',
    'tools\audit-prologue-production.ps1', 'tools\audit-prologue-base-hardgates.ps1',
    'tools\audit-prologue-task6-focused.ps1', 'tools\capture-prologue.ps1',
    'tools\extract-prologue-keyframes.ps1', 'tools\verify-prologue.ps1',
    'tools\Task6ImportRetry.ps1', 'tools\verify-prologue-base-hardgates.ps1',
    'scenes\prologue\sectors\CanyonEntrance.tscn',
    'scenes\prologue\sectors\CanyonDescent.tscn',
    'scenes\prologue\sectors\CanyonBasin.tscn',
    'scenes\prologue\sectors\CanyonAscent.tscn',
    'scenes\prologue\sectors\LostTemple.tscn',
    'scenes\prologue\PrologueLevel.tscn'
)

function Get-Task6CandidateFingerprint([string]$CandidateProject) {
    $orderedRelativePaths = @($script:FingerprintRelativePaths)
    [Array]::Sort($orderedRelativePaths, [StringComparer]::Ordinal)
    $lines = foreach ($relative in $orderedRelativePaths) {
        $path = Join-Path $CandidateProject $relative
        Assert-Task6 (Test-Path -LiteralPath $path -PathType Leaf) "fingerprint source exists: $relative"
        "$relative=$(Get-EvidenceSha256 -LiteralPath $path)"
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes((($lines -join "`n") + "`n"))
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($bytes)) -replace '-', '') }
    finally { $algorithm.Dispose() }
}

function Get-Task6MaterializedTreeHash([string]$Root) {
    $resolvedRoot = ([IO.Path]::GetFullPath($Root)).TrimEnd('\\') + '\\'
    $lines = @(
        Get-ChildItem -LiteralPath $Root -File -Recurse | Sort-Object FullName | ForEach-Object {
            $relative = $_.FullName.Substring($resolvedRoot.Length).Replace('\\', '/')
            "$relative=$(Get-EvidenceSha256 -LiteralPath $_.FullName)"
        }
    )
    Assert-Task6 ($lines.Count -gt 0) "materialized source tree has files: $Root"
    $bytes = [Text.Encoding]::UTF8.GetBytes((($lines -join "`n") + "`n"))
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($bytes)) -replace '-', '') }
    finally { $algorithm.Dispose() }
}

function Set-Task6BundleCandidateFingerprint($Bundle) {
    $fingerprint = Get-Task6CandidateFingerprint $Bundle.CandidateProject
    Set-Task6ManifestValue -Path $Bundle.Manifest -Name 'candidate_runtime_sha256' -Value $fingerprint
    return $fingerprint
}

function Replace-Task6ExactLine([string]$Path, [string]$Expected, [string]$Replacement) {
    $lines = @(Get-Content -LiteralPath $Path)
    $count = @($lines | Where-Object { $_ -ceq $Expected }).Count
    Assert-Task6 ($count -eq 1) "mutation source has exactly one '$Expected' in $Path"
    $rewritten = @($lines | ForEach-Object { if ($_ -ceq $Expected) { $Replacement } else { $_ } })
    [IO.File]::WriteAllText($Path, (($rewritten -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
}

function New-Task6EvidenceMutation([string]$Name) {
    $root = Join-Path $mutationRoot $Name
    Assert-Task6 (-not (Test-Path -LiteralPath $root)) "mutation root is fresh: $Name"
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    $sourceManifest = Join-Path $captureRoot 'capture-manifest.txt'
    $manifest = Join-Path $root 'capture-manifest.txt'
    Copy-Item -LiteralPath $sourceManifest -Destination $manifest -Force
    $values = Get-Task6ManifestValues $sourceManifest
    $paths = @{}
    foreach ($key in @(
        'task11_materialization_log', 'task11_materialization_console', 'task11_critical_log', 'task11_critical_console',
        'task11_ui_log', 'task11_ui_console', 'audit_log', 'audit_console',
        'full_flow_log', 'full_flow_console', 'boss_retry_log', 'boss_retry_console')) {
        Assert-Task6 ($values.ContainsKey($key)) "capture manifest binds $key"
        $source = [string]$values[$key]
        Assert-Task6 (Test-Path -LiteralPath $source -PathType Leaf) "capture evidence exists: $key"
        $destination = Join-Path $root ($key + ([IO.Path]::GetExtension($source)))
        Copy-Item -LiteralPath $source -Destination $destination -Force
        Set-Task6ManifestValue -Path $manifest -Name $key -Value $destination
        $paths[$key] = $destination
    }
    Set-Task6ManifestValue -Path $manifest -Name 'capture_root' -Value $root
    [pscustomobject]@{ Root = $root; Manifest = $manifest; Paths = $paths }
}

function Copy-Task6CompletionKeyframeMutation($Bundle) {
    # Copy the real decoded keyframe bundle rather than deriving a result from source text.
    # The child verifier will still bind it to the unmodified canonical AVI SHA, then decode
    # the copied completion PNG and reject its visibly wrong pixels.
    $values = Get-Task6ManifestValues $Bundle.Manifest
    $sourceManifest = [string]$values['full_flow_keyframe_manifest']
    Assert-Task6 (Test-Path -LiteralPath $sourceManifest -PathType Leaf) 'canonical completion keyframe manifest exists'
    $sourceDirectory = Split-Path -Parent $sourceManifest
    $destinationDirectory = Join-Path $Bundle.Root 'full-flow-keyframes'
    Copy-Item -LiteralPath $sourceDirectory -Destination $destinationDirectory -Recurse -Force
    $destinationManifest = Join-Path $destinationDirectory 'manifest.txt'
    Assert-Task6 (Test-Path -LiteralPath $destinationManifest -PathType Leaf) 'copied completion keyframe manifest exists'

    $sourceBossFrame = Join-Path $destinationDirectory '16-boss_intro.png'
    $destinationCompletionFrame = Join-Path $destinationDirectory '18-completion.png'
    Assert-Task6 (Test-Path -LiteralPath $sourceBossFrame -PathType Leaf) 'copied Boss frame exists for visual mutation'
    Assert-Task6 (Test-Path -LiteralPath $destinationCompletionFrame -PathType Leaf) 'copied completion frame exists for visual mutation'
    Copy-Item -LiteralPath $sourceBossFrame -Destination $destinationCompletionFrame -Force

    $manifestText = [IO.File]::ReadAllText($destinationManifest)
    $completion = [regex]::Match($manifestText,
        '(?m)^completion\|avi_frame=(?<frame>\d+)\|png=18-completion\.png\|sha256=[A-Fa-f0-9]{64}$')
    Assert-Task6 $completion.Success 'copied manifest binds its completion PNG'
    $replacement = 'completion|avi_frame=' + $completion.Groups['frame'].Value +
        '|png=18-completion.png|sha256=' + (Get-EvidenceSha256 -LiteralPath $destinationCompletionFrame)
    $rewritten = [regex]::Replace($manifestText,
        '(?m)^completion\|avi_frame=\d+\|png=18-completion\.png\|sha256=[A-Fa-f0-9]{64}$', $replacement, 1)
    Assert-Task6 ($rewritten -cne $manifestText) 'completion visual mutation rewrites copied keyframe manifest hash'
    [IO.File]::WriteAllText($destinationManifest, $rewritten, [Text.UTF8Encoding]::new($false))
    Set-Task6ManifestValue $Bundle.Manifest 'full_flow_keyframe_manifest' $destinationManifest
    Set-Task6ManifestHash $Bundle.Manifest 'full_flow_keyframe_manifest' $destinationManifest
    Set-Task6ManifestValue $Bundle.Manifest 'full_flow_completion_card_png' $destinationCompletionFrame
    Set-Task6ManifestHash $Bundle.Manifest 'full_flow_completion_card_png' $destinationCompletionFrame
    return $destinationCompletionFrame
}

function Copy-Task6NamedKeyframeMutation($Bundle, [string]$TargetStage, [string]$ReplacementStage,
    [string]$CapturePointerName) {
    # Mutate a copied decoded keyframe tree and repair only its copied hashes.  The real child
    # verifier still binds the tree to the original AVI SHA and must reject the substituted
    # visible surface; this is deliberately stronger than checking a source string in memory.
    $values = Get-Task6ManifestValues $Bundle.Manifest
    $sourceManifest = [string]$values['full_flow_keyframe_manifest']
    Assert-Task6 (Test-Path -LiteralPath $sourceManifest -PathType Leaf) 'canonical named-keyframe manifest exists'
    $sourceDirectory = Split-Path -Parent $sourceManifest
    $destinationDirectory = Join-Path $Bundle.Root 'full-flow-keyframes'
    Copy-Item -LiteralPath $sourceDirectory -Destination $destinationDirectory -Recurse -Force
    $destinationManifest = Join-Path $destinationDirectory 'manifest.txt'
    Assert-Task6 (Test-Path -LiteralPath $destinationManifest -PathType Leaf) 'copied named-keyframe manifest exists'
    $manifestText = [IO.File]::ReadAllText($destinationManifest)
    $targetPattern = '(?m)^' + [regex]::Escape($TargetStage) +
        '\|avi_frame=(?<frame>\d+)\|png=(?<png>[^|]+\.png)\|sha256=[A-Fa-f0-9]{64}$'
    $replacementPattern = '(?m)^' + [regex]::Escape($ReplacementStage) +
        '\|avi_frame=\d+\|png=(?<png>[^|]+\.png)\|sha256=[A-Fa-f0-9]{64}$'
    $target = [regex]::Match($manifestText, $targetPattern)
    $replacement = [regex]::Match($manifestText, $replacementPattern)
    Assert-Task6 $target.Success "copied keyframe manifest has $TargetStage"
    Assert-Task6 $replacement.Success "copied keyframe manifest has $ReplacementStage"
    $targetPng = Join-Path $destinationDirectory $target.Groups['png'].Value
    $replacementPng = Join-Path $destinationDirectory $replacement.Groups['png'].Value
    Assert-Task6 (Test-Path -LiteralPath $targetPng -PathType Leaf) "copied target PNG exists: $TargetStage"
    Assert-Task6 (Test-Path -LiteralPath $replacementPng -PathType Leaf) "copied replacement PNG exists: $ReplacementStage"
    Copy-Item -LiteralPath $replacementPng -Destination $targetPng -Force
    $replacementLine = $TargetStage + '|avi_frame=' + $target.Groups['frame'].Value + '|png=' +
        $target.Groups['png'].Value + '|sha256=' + (Get-EvidenceSha256 -LiteralPath $targetPng)
    $rewritten = [regex]::Replace($manifestText, $targetPattern, $replacementLine, 1)
    Assert-Task6 ($rewritten -cne $manifestText) "named-keyframe mutation rewrites $TargetStage hash"
    [IO.File]::WriteAllText($destinationManifest, $rewritten, [Text.UTF8Encoding]::new($false))
    Set-Task6ManifestValue $Bundle.Manifest 'full_flow_keyframe_manifest' $destinationManifest
    Set-Task6ManifestHash $Bundle.Manifest 'full_flow_keyframe_manifest' $destinationManifest
    Set-Task6ManifestValue $Bundle.Manifest $CapturePointerName $targetPng
    Set-Task6ManifestHash $Bundle.Manifest $CapturePointerName $targetPng
    return $targetPng
}

function New-Task6SourceMutationProject([string]$Name) {
    $root = Join-Path $sourceMutationRoot ('source-' + $Name)
    Assert-Task6 (-not (Test-Path -LiteralPath $root)) "source mutation root is fresh: $Name"
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    $relativePaths = @($script:FingerprintRelativePaths + @(
        'docs\playtests\2026-08-29-terrain-grapple-dense-canyon-validation.md'
    ) | Sort-Object -Unique)
    foreach ($relative in $relativePaths) {
        $source = Join-Path $ProjectRoot $relative
        Assert-Task6 (Test-Path -LiteralPath $source -PathType Leaf) "source mutation seed exists: $relative"
        $destination = Join-Path $root $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination $destination -Force
    }
    return $root
}

function Invoke-Task6EvidenceGate {
    param([string]$Name, [string]$EvidenceRoot, [string]$CandidateProject = $ProjectRoot,
        [switch]$ExpectFailure)

    $childHost = Get-Task6ChildHost
    $runRoot = Join-Path $mutationRoot ('verify-' + $Name)
    $verify = Join-Path $ProjectRoot 'tools\verify-prologue.ps1'
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $verify -ProjectRoot $CandidateProject `
            -CaptureEvidenceOnly -CaptureRoot $EvidenceRoot -VerificationRoot $runRoot 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    [IO.File]::WriteAllText((Join-Path $mutationRoot ($Name + '.verify.log')),
        (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
        [Text.UTF8Encoding]::new($false))
    if ($ExpectFailure) {
        Assert-Task6 ($exitCode -ne 0) "actual verifier evidence gate rejects mutation: $Name"
    }
    else {
        Assert-Task6 ($exitCode -eq 0) "actual verifier evidence gate accepts canonical bundle: $Name"
        Assert-Task6 ((@($output | Where-Object { $_.ToString() -match '^PROLOGUE_CAPTURE_EVIDENCE_OK ' }).Count) -eq 1) `
            "canonical evidence gate emits one terminal marker"
    }
    return [pscustomobject]@{ ExitCode = $exitCode; Output = $output }
}

function Invoke-Task6HostProbe {
    $capture = Join-Path $ProjectRoot 'tools\capture-prologue.ps1'
    foreach ($name in @('powershell', 'pwsh')) {
        $hostCommand = Get-Command $name -ErrorAction SilentlyContinue
        Assert-Task6 ($null -ne $hostCommand) "required PowerShell host is available: $name"
        $prior = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
        $output = & $hostCommand.Source -NoProfile -ExecutionPolicy Bypass -File $capture -HostProbeOnly 2>&1
            $exitCode = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $prior }
        [IO.File]::WriteAllText((Join-Path $mutationRoot ("host-$name.log")),
            (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
            [Text.UTF8Encoding]::new($false))
        Assert-Task6 ($exitCode -eq 0) "capture host probe succeeds in $name"
        Assert-Task6 ((@($output | Where-Object { $_.ToString() -match '^TASK6_CAPTURE_HOST_OK ' }).Count) -eq 1) `
            "capture host probe emits one marker in $name"
        try {
            $ErrorActionPreference = 'Continue'
            $fingerprintOutput = & $hostCommand.Source -NoProfile -ExecutionPolicy Bypass -File $capture `
                -RuntimeFingerprintProbeOnly 2>&1
            $fingerprintExit = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $prior }
        [IO.File]::AppendAllText((Join-Path $mutationRoot ("host-$name.log")),
            (($fingerprintOutput | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
            [Text.UTF8Encoding]::new($false))
        Assert-Task6 ($fingerprintExit -eq 0) "capture runtime fingerprint probe succeeds in $name"
        Assert-Task6 ((@($fingerprintOutput | Where-Object {
                $_.ToString() -match '^TASK6_CAPTURE_RUNTIME_FINGERPRINT_OK '
            }).Count) -eq 1) "capture runtime fingerprint probe emits one marker in $name"
        $fingerprintMarker = @($fingerprintOutput | Where-Object {
            $_.ToString() -match '^TASK6_CAPTURE_RUNTIME_FINGERPRINT_OK '
        })[0].ToString()
        $fingerprintMatch = [regex]::Match($fingerprintMarker,
            'candidate_runtime_sha256=(?<hash>[A-Fa-f0-9]{64})')
        Assert-Task6 $fingerprintMatch.Success "capture runtime fingerprint marker has SHA-256 in $name"
        $script:HostFingerprints[$name] = $fingerprintMatch.Groups['hash'].Value
        $script:HostProbeCount++
    }
    Assert-Task6 ($script:HostFingerprints['powershell'] -ceq $script:HostFingerprints['pwsh']) `
        'capture runtime fingerprint is invariant across Windows PowerShell and pwsh'
}

function Invoke-Task6FrozenAviExtractorRed {
    # This is deliberately not a source-substring assertion.  It finds two decoded, identical
    # title-tail samples from the runtime-bound AVI, then passes those distinct frame numbers to
    # a fresh child extraction process.  The extractor must reject the resulting real frozen
    # two-stage movie with the same bounded streaming decoder used by capture.
    $values = Get-Task6ManifestValues (Join-Path $captureRoot 'capture-manifest.txt')
    $avi = [string]$values['full_flow_avi']
    $fullFlowLog = [string]$values['full_flow_log']
    Assert-Task6 (Test-Path -LiteralPath $avi -PathType Leaf) 'runtime-bound full-flow AVI exists for frozen RED'
    Assert-Task6 (Test-Path -LiteralPath $fullFlowLog -PathType Leaf) 'runtime-bound full-flow log exists for frozen RED'

    $titleMatches = @((Get-Content -LiteralPath $fullFlowLog) | ForEach-Object {
        [regex]::Match($_, '^PROLOGUE_PRESENTATION_STAGE=completion_title_return frame=(?<frame>\d+)\s*$')
    } | Where-Object { $_.Success })
    Assert-Task6 ($titleMatches.Count -eq 1) 'runtime full-flow has one completion-title stage for frozen RED'
    [long]$titleFrame = [long]$titleMatches[0].Groups['frame'].Value

    $probeRoot = Join-Path $mutationRoot 'avi-frozen-title-tail'
    Assert-Task6 (-not (Test-Path -LiteralPath $probeRoot)) 'frozen AVI mutation root is fresh'
    New-Item -ItemType Directory -Path $probeRoot -Force | Out-Null
    $index = Get-Task6AviMjpegIndex -Path $avi
    $seenSamples = @{}
    $pair = $null
    [long]$lastFrame = [Math]::Min([long]$index.Header.TotalFrames, $titleFrame + 35L)
    for ([long]$frame = $titleFrame; $frame -le $lastFrame; $frame++) {
        $jpeg = Join-Path $probeRoot ('title-tail-{0}.jpg' -f $frame)
        $entry = $index.Frames[[int]($frame - 1L)]
        Copy-Task6AviFrameToFile -AviPath $avi -Offset $entry.Offset -Size $entry.Size -Destination $jpeg
        $sample = Get-Task6ImageSample -Path $jpeg
        if ($seenSamples.ContainsKey($sample.Sha256)) {
            $pair = [pscustomobject]@{ First = [long]$seenSamples[$sample.Sha256]; Second = $frame }
            break
        }
        $seenSamples[$sample.Sha256] = $frame
    }
    Assert-Task6 ($null -ne $pair) 'completion title tail contains a real repeated decoded frame sample'

    $stageLog = Join-Path $probeRoot 'frozen-stage.log'
    [IO.File]::WriteAllText($stageLog, (
        "PROLOGUE_PRESENTATION_STAGE=boss_intro frame=$($pair.First)`n" +
        "PROLOGUE_PRESENTATION_STAGE=boss_retry_completion frame=$($pair.Second)`n"
    ), [Text.UTF8Encoding]::new($false))
    $extractRoot = Join-Path $probeRoot 'keyframes'
    $extract = Join-Path $ProjectRoot 'tools\extract-prologue-keyframes.ps1'
    $childHost = Get-Task6ChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $extract -AviPath $avi `
            -StageLogPath $stageLog -OutputPath $extractRoot -StagePrefix 'PROLOGUE_PRESENTATION_STAGE' `
            -BeatsCsv 'boss_intro,boss_retry_completion' 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    $childLog = Join-Path $probeRoot 'actual-child-extractor.log'
    [IO.File]::WriteAllText($childLog, (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
        [Text.UTF8Encoding]::new($false))
    Assert-Task6 ($exitCode -ne 0) 'actual child extractor rejects a decoded frozen AVI sample pair'
    $outputText = ($output | ForEach-Object { $_.ToString() }) -join "`n"
    Assert-Task6 ($outputText -match 'Task6 AVI appears frozen at distributed samples: distinct=1 changed=0 delta=0') `
        'actual child extractor reports the frozen-stream predicate'
    Assert-Task6CanonicalUntouched
    $script:EvidenceMarkers.Add("TASK6_FROZEN_MOVIE_RED_OBSERVED name=avi-frozen-title-tail frames=$($pair.First),$($pair.Second) command=actual_child_extractor nonzero=True canonical_manifest_sha256=$($script:CanonicalManifestHash) canonical_candidate_sha256=$($script:CanonicalCandidateHash)")
    return $childLog
}

function Invoke-Task6BaseEvidenceGate {
    param([string]$Name, [string]$EvidenceLog, [ValidateSet('Presentation', 'AttackPresentation', 'CriticalSmoke')][string]$Kind,
        [switch]$ExpectFailure)

    $childHost = Get-Task6ChildHost
    $base = Join-Path $ProjectRoot 'tools\verify-prologue-base-hardgates.ps1'
    $runLog = Join-Path $mutationRoot ($Name + '.base-' + $Kind + '.verify.log')
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $base)
    if ($Kind -ceq 'Presentation') {
        $arguments += @('-PresentationEvidenceOnly', '-PresentationEvidenceLog', $EvidenceLog)
        $expectedTerminal = 'PROLOGUE_BASE_PRESENTATION_EVIDENCE_OK '
    }
    elseif ($Kind -ceq 'AttackPresentation') {
        $arguments += @('-AttackPresentationEvidenceOnly', '-AttackPresentationEvidenceLog', $EvidenceLog)
        $expectedTerminal = 'PROLOGUE_BASE_ATTACK_PRESENTATION_EVIDENCE_OK '
    }
    else {
        $arguments += @('-CriticalSmokeEvidenceOnly', '-CriticalSmokeEvidenceLog', $EvidenceLog)
        $expectedTerminal = 'PROLOGUE_BASE_CRITICAL_SMOKE_EVIDENCE_OK '
    }
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost @arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    [IO.File]::WriteAllText($runLog, (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
        [Text.UTF8Encoding]::new($false))
    if ($ExpectFailure) {
        Assert-Task6 ($exitCode -ne 0) "actual BASE $Kind evidence gate rejects mutation: $Name"
    }
    else {
        Assert-Task6 ($exitCode -eq 0) "actual BASE $Kind evidence gate accepts canonical evidence: $Name"
        Assert-Task6 ((@($output | Where-Object { $_.ToString().StartsWith($expectedTerminal) }).Count) -eq 1) `
            "BASE $Kind evidence gate emits one terminal"
    }
    return [pscustomobject]@{ ExitCode = $exitCode; Log = $runLog }
}

function Invoke-Task6VerifierTerminalGate {
    param([string]$Name, [string]$TerminalLog, [switch]$ExpectFailure)
    $childHost = Get-Task6ChildHost
    $verify = Join-Path $ProjectRoot 'tools\verify-prologue.ps1'
    $runRoot = Join-Path $mutationRoot ('terminal-verify-' + $Name)
    $runLog = Join-Path $mutationRoot ($Name + '.terminal.verify.log')
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $verify `
            -VerifierTerminalOnly -VerifierTerminalLog $TerminalLog -VerificationRoot $runRoot 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    [IO.File]::WriteAllText($runLog, (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
        [Text.UTF8Encoding]::new($false))
    if ($ExpectFailure) {
        Assert-Task6 ($exitCode -ne 0) "actual verifier terminal gate rejects mutation: $Name"
    }
    else {
        Assert-Task6 ($exitCode -eq 0) "actual verifier terminal gate accepts canonical terminal: $Name"
        Assert-Task6 ((@($output | Where-Object { $_.ToString() -match '^PROLOGUE_VERIFY_TERMINAL_EVIDENCE_OK ' }).Count) -eq 1) `
            'verifier terminal gate emits one terminal'
    }
    return [pscustomobject]@{ ExitCode = $exitCode; Log = $runLog }
}

function Add-Task6MutationEvidence([string]$Name, [string]$Path, [string]$ExpectedLabel) {
    Assert-Task6CanonicalUntouched
    $script:MutationCount++
    $script:EvidenceMarkers.Add("TASK6_MUTATION_RED_OBSERVED name=$Name")
    $script:EvidenceMarkers.Add("TASK6_MUTATION_LEDGER name=$Name path=$Path command=actual_child_verifier nonzero=True expected=$ExpectedLabel canonical_manifest_sha256=$($script:CanonicalManifestHash) canonical_candidate_sha256=$($script:CanonicalCandidateHash) restored_hashes=True")
}

function Assert-Task6CanonicalUntouched {
    $manifest = Join-Path $captureRoot 'capture-manifest.txt'
    Assert-Task6 ((Get-EvidenceSha256 -LiteralPath $manifest) -ceq $script:CanonicalManifestHash) `
        'isolated mutations leave canonical capture manifest bytes untouched'
    Assert-Task6 ((Get-Task6CandidateFingerprint $ProjectRoot) -ceq $script:CanonicalCandidateHash) `
        'isolated mutations leave canonical candidate bytes untouched'
}

function New-Task6StandaloneEvidenceLog([string]$Name, [string]$SourcePath) {
    Assert-Task6 (Test-Path -LiteralPath $SourcePath -PathType Leaf) "source evidence exists: $SourcePath"
    $destination = Join-Path $mutationRoot $Name
    Assert-Task6 (-not (Test-Path -LiteralPath $destination)) "standalone evidence destination is fresh: $Name"
    Copy-Item -LiteralPath $SourcePath -Destination $destination -Force
    return $destination
}

function Invoke-Task6BundleMutation([string]$Name, [scriptblock]$Mutate, [string]$ExpectedLabel, [switch]$NoRecord) {
    $bundle = New-Task6EvidenceMutation $Name
    $bundle | Add-Member -MemberType NoteProperty -Name CandidateProject -Value $ProjectRoot
    & $Mutate $bundle
    if ($bundle.CandidateProject -cne $ProjectRoot) {
        $candidateRuntimeHash = Get-Task6CandidateFingerprint $bundle.CandidateProject
        $candidateTreeHash = Get-Task6MaterializedTreeHash $bundle.CandidateProject
        $script:EvidenceMarkers.Add("TASK6_SOURCE_MUTATION_MATERIALIZATION_OK name=$Name root=$($bundle.CandidateProject) candidate_runtime_sha256=$candidateRuntimeHash materialized_tree_sha256=$candidateTreeHash")
    }
    Invoke-Task6EvidenceGate -Name $Name -EvidenceRoot $bundle.Root -CandidateProject $bundle.CandidateProject `
        -ExpectFailure | Out-Null
    if (-not $NoRecord) { Add-Task6MutationEvidence -Name $Name -Path $bundle.Root -ExpectedLabel $ExpectedLabel }
    return $bundle
}

Assert-Task6 (-not [string]::IsNullOrWhiteSpace($captureRoot))
    'full verifier supplies a runtime-bound capture root for real RED mutations'
Assert-Task6 (-not [string]::IsNullOrWhiteSpace($mutationRoot))
    'full verifier supplies a fresh mutation evidence root'
Assert-Task6 (Test-Path -LiteralPath $captureRoot -PathType Container) 'capture root exists'
Assert-Task6 (-not (Test-Path -LiteralPath $mutationRoot)) 'mutation root starts fresh'
New-Item -ItemType Directory -Path $mutationRoot -Force | Out-Null
$script:BaseExternalEvidence = Get-Task6BoundBaseExternalEvidence
$script:EvidenceMarkers.Add("TASK6_BASE_EXTERNAL_EVIDENCE_CONSUMED manifest=$($script:BaseExternalEvidence.Manifest) " +
    "manifest_sha256=$(Get-EvidenceSha256 -LiteralPath $script:BaseExternalEvidence.Manifest) " +
    "not_before_utc=$($script:BaseExternalEvidence.NotBeforeUtc.ToString('o'))")
if ([string]::IsNullOrWhiteSpace($sourceMutationRoot)) {
    $sourceMutationRoot = Join-Path ([IO.Path]::GetTempPath()) `
        ('knight-task6-source-mutations-' + [guid]::NewGuid().ToString('N'))
}
$sourceMutationRoot = [IO.Path]::GetFullPath($sourceMutationRoot)
$projectRootFull = ([IO.Path]::GetFullPath($ProjectRoot)).TrimEnd('\') + '\'
Assert-Task6 (-not $sourceMutationRoot.StartsWith($projectRootFull, [StringComparison]::OrdinalIgnoreCase)) `
    "source mutation root is outside the project: $sourceMutationRoot"
Assert-Task6 (-not (Test-Path -LiteralPath $sourceMutationRoot)) 'source mutation root starts fresh'
New-Item -ItemType Directory -Path $sourceMutationRoot -Force | Out-Null
$script:EvidenceMarkers.Add("TASK6_SOURCE_MUTATION_ROOT_OK root=$sourceMutationRoot outside_project=True")
$script:CanonicalManifestHash = Get-EvidenceSha256 -LiteralPath (Join-Path $captureRoot 'capture-manifest.txt')
$script:CanonicalCandidateHash = Get-Task6CandidateFingerprint $ProjectRoot

Describe 'Task 6 terrain-grapple runtime evidence contract' {
    It 'accepts the canonical runtime-bound capture bundle through the verifier gate' {
        Invoke-Task6EvidenceGate -Name 'canonical' -EvidenceRoot $captureRoot | Out-Null
        $script:CanonicalAccepted = $true
    }

    It 'keeps the postdraw visual-binding coverage matrix across runtime, capture, verifier, and actual RED' {
        # This static matrix is a completeness audit only.  The neighbouring mutation test still
        # copies real evidence roots and invokes the child verifier; no row is certified merely
        # because these strings occur in source.
        $runtime = Get-Content -LiteralPath (Join-Path $ProjectRoot 'tests\presentation\ProloguePresentationTask6.cs') -Raw
        $capture = Get-Content -LiteralPath (Join-Path $ProjectRoot 'tools\capture-prologue.ps1') -Raw
        $verifier = Get-Content -LiteralPath (Join-Path $ProjectRoot 'tools\verify-prologue.ps1') -Raw
        $pester = Get-Content -LiteralPath $PSCommandPath -Raw
        $rows = @(
            [pscustomobject]@{ Beat = 'mode_hud'; Runtime = 'TASK6_MODE_HUD_DISPLAY_OK'; Capture = 'Assert-Task6ModeHudDisplayBinding'; Verifier = 'Get-Task6ModeHudRenderedFrame'; Red = 'terrain-marker-delete-mode-hud-visual' },
            [pscustomobject]@{ Beat = 'boost_attached'; Runtime = '"boost_attached"'; Capture = "Stage = 'boost_attached'"; Verifier = "Stage = 'boost_attached'"; Red = 'terrain-marker-delete-second-boost-visual' },
            [pscustomobject]@{ Beat = 'boost_start'; Runtime = '"boost_start"'; Capture = "Stage = 'boost_start'"; Verifier = "Stage = 'boost_start'"; Red = 'avi-header-tamper-timeline' },
            [pscustomobject]@{ Beat = 'attached_boost_consumed'; Runtime = '"attached_boost_consumed"'; Capture = "Stage = 'attached_boost_consumed'"; Verifier = "Stage = 'attached_boost_consumed'"; Red = 'terrain-marker-delete-second-boost-visual' },
            [pscustomobject]@{ Beat = 'boost_arc'; Runtime = '"boost_arc"'; Capture = "Stage = 'boost_arc'"; Verifier = "Stage = 'boost_arc'"; Red = 'terrain-marker-delete-second-boost-visual' },
            [pscustomobject]@{ Beat = 'boost_second_rejected'; Runtime = '"boost_second_rejected"'; Capture = "Stage = 'boost_second_rejected'"; Verifier = "Stage = 'boost_second_rejected'"; Red = 'terrain-marker-delete-second-boost-visual' },
            [pscustomobject]@{ Beat = 'boost_second_rejected_offset'; Runtime = 'movieFrameOffset: 2UL'; Capture = "Offset = '2'"; Verifier = "Offset = '2'"; Red = 'terrain-marker-stale-second-boost-offset' },
            [pscustomobject]@{ Beat = 'boost_land'; Runtime = '"boost_land"'; Capture = "Stage = 'boost_land'"; Verifier = "Stage = 'boost_land'"; Red = 'terrain-marker-delete-second-boost-visual' },
            [pscustomobject]@{ Beat = 'boss_sweep'; Runtime = 'TASK6_BOSS_ATTACK_DISPLAY_OK'; Capture = "Stage = 'boss_sweep'"; Verifier = "Stage = 'boss_sweep'"; Red = 'terrain-marker-delete-boss-telegraph-display' },
            [pscustomobject]@{ Beat = 'boss_leap_slam'; Runtime = 'TASK6_BOSS_ATTACK_DISPLAY_OK'; Capture = "Stage = 'boss_leap_slam'"; Verifier = "Stage = 'boss_leap_slam'"; Red = 'terrain-marker-delete-boss-telegraph-display' },
            [pscustomobject]@{ Beat = 'boss_ground_wave'; Runtime = 'TASK6_BOSS_ATTACK_DISPLAY_OK'; Capture = "Stage = 'boss_ground_wave'"; Verifier = "Stage = 'boss_ground_wave'"; Red = 'terrain-marker-delete-boss-telegraph-display' },
            [pscustomobject]@{ Beat = 'boss_hammer_throw'; Runtime = 'TASK6_BOSS_ATTACK_DISPLAY_OK'; Capture = "Stage = 'boss_hammer_throw'"; Verifier = "Stage = 'boss_hammer_throw'"; Red = 'terrain-marker-delete-boss-telegraph-display' },
            [pscustomobject]@{ Beat = 'completion'; Runtime = 'TASK6_COMPLETION_DISPLAY_OK'; Capture = 'Assert-Task6CompletionDisplayBinding'; Verifier = 'Get-Task6CompletionRenderedFrame'; Red = 'terrain-marker-delete-completion-card-visual' }
        )
        foreach ($row in $rows) {
            Assert-Task6 $runtime.Contains($row.Runtime) "matrix runtime producer: $($row.Beat)"
            Assert-Task6 $capture.Contains($row.Capture) "matrix capture binder: $($row.Beat)"
            Assert-Task6 $verifier.Contains($row.Verifier) "matrix verifier binder: $($row.Beat)"
            Assert-Task6 $pester.Contains($row.Red) "matrix actual child RED: $($row.Beat)"
        }
        $script:EvidenceMarkers.Add('TASK6_VISUAL_BINDING_MATRIX_OK rows=13 runtime_postdraw=True capture_frame_bound=True verifier_frame_bound=True actual_child_red=True')
    }

    It 'runs capture child-host compatibility probes under Windows PowerShell and pwsh' {
        Invoke-Task6HostProbe
        Assert-Task6 ($script:HostProbeCount -eq 2) 'both supported hosts completed an actual capture probe'
    }

    It 'rejects fifteen real isolated evidence and candidate mutations through child gates' {
        # One named mutation may exercise closely related bypass variants, but every rejection below
        # is a fresh copied evidence/source root passed to a real verifier or BASE gate child.
        Invoke-Task6BundleMutation -Name 'terrain-marker-delete' -NoRecord -ExpectedLabel 'terrain_marker_required' -Mutate {
            param($bundle)
            Replace-Task6ExactLine $bundle.Paths['task11_critical_log'] `
                'PROLOGUE_TERRAIN_GRAPPLE_OK wall=True ceiling=True underside=True edge=True ice=IceSlip forbidden=Forbidden range=620' 'REMOVED'
            Set-Task6ManifestHash $bundle.Manifest 'task11_critical_log' $bundle.Paths['task11_critical_log']
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'terrain-marker-delete-fall-v3' -NoRecord -ExpectedLabel 'fall_context_v3_required' -Mutate {
            param($bundle)
            Replace-Task6ExactLine $bundle.Paths['task11_critical_log'] `
                'PROLOGUE_FALL_CONTEXT_OK version=3 fields=position,sector,last_support,checkpoint,claw,inputs' `
                'PROLOGUE_FALL_CONTEXT_OK version=2 fields=position,sector,last_support,checkpoint,claw,inputs'
            Set-Task6ManifestHash $bundle.Manifest 'task11_critical_log' $bundle.Paths['task11_critical_log']
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'terrain-marker-delete-completion-display' -NoRecord `
            -ExpectedLabel 'completion_display_post_draw_required' -Mutate {
                param($bundle)
                $path = $bundle.Paths['full_flow_log']
                $text = [IO.File]::ReadAllText($path)
                $rewritten = [regex]::Replace($text,
                    '(?m)^TASK6_COMPLETION_DISPLAY_OK frame=\d+ completion_visible=True boss_visible=False rendered_post_draw=True movie_frame_offset=1\r?\n?',
                    '', 1)
                Assert-Task6 ($rewritten -cne $text) 'completion display mutation removes one copied runtime marker'
                [IO.File]::WriteAllText($path, $rewritten, [Text.UTF8Encoding]::new($false))
                Set-Task6ManifestHash $bundle.Manifest 'full_flow_log' $path
            } | Out-Null
        Invoke-Task6BundleMutation -Name 'terrain-marker-delete-completion-card-visual' -NoRecord `
            -ExpectedLabel 'completion_card_pixels_required' -Mutate {
                param($bundle)
                $mutatedPng = Copy-Task6CompletionKeyframeMutation $bundle
                Assert-Task6 (Test-Path -LiteralPath $mutatedPng -PathType Leaf) `
                    'completion visual mutation writes an isolated copied PNG'
            } | Out-Null
        Invoke-Task6BundleMutation -Name 'terrain-marker-delete-mode-hud-visual' -NoRecord `
            -ExpectedLabel 'mode_hud_pixels_required' -Mutate {
                param($bundle)
                $mutatedPng = Copy-Task6NamedKeyframeMutation $bundle -TargetStage 'mode_hud' `
                    -ReplacementStage 'title_start' -CapturePointerName 'full_flow_mode_hud_png'
                Assert-Task6 (Test-Path -LiteralPath $mutatedPng -PathType Leaf) `
                    'mode-HUD visual mutation writes an isolated copied PNG'
            } | Out-Null
        Invoke-Task6BundleMutation -Name 'terrain-marker-delete-second-boost-visual' -NoRecord `
            -ExpectedLabel 'second_boost_rejection_pixels_required' -Mutate {
                param($bundle)
                $mutatedPng = Copy-Task6NamedKeyframeMutation $bundle -TargetStage 'boost_second_rejected' `
                    -ReplacementStage 'boost_second_rejected__previous' `
                    -CapturePointerName 'full_flow_second_boost_rejected_png'
                Assert-Task6 (Test-Path -LiteralPath $mutatedPng -PathType Leaf) `
                    'second-boost visual mutation writes an isolated copied PNG'
            } | Out-Null
        Invoke-Task6BundleMutation -Name 'terrain-marker-stale-second-boost-offset' -NoRecord `
            -ExpectedLabel 'second_boost_movie_offset_required' -Mutate {
                param($bundle)
                $path = $bundle.Paths['full_flow_log']
                $text = [IO.File]::ReadAllText($path)
                $rewritten = [regex]::Replace($text,
                    '(?m)^(TASK6_BOOST_DISPLAY_OK stage=boost_second_rejected frame=\d+ state=Reel cast=\d+ boost_consumed=True second_rejected=True rendered_post_draw=True movie_frame_offset=)2$',
                    '${1}1', 1)
                Assert-Task6 ($rewritten -cne $text) 'second-boost offset mutation rewrites one copied runtime marker'
                [IO.File]::WriteAllText($path, $rewritten, [Text.UTF8Encoding]::new($false))
                Set-Task6ManifestHash $bundle.Manifest 'full_flow_log' $path
            } | Out-Null
        Invoke-Task6BundleMutation -Name 'terrain-marker-delete-boss-telegraph-display' -NoRecord `
            -ExpectedLabel 'boss_telegraph_post_draw_required' -Mutate {
                param($bundle)
                $path = $bundle.Paths['full_flow_log']
                $text = [IO.File]::ReadAllText($path)
                $rewritten = [regex]::Replace($text,
                    '(?m)^TASK6_BOSS_ATTACK_DISPLAY_OK attack=GroundWave stage=boss_ground_wave frame=\d+ formal_pose=boss_groundwave_raise telegraph=True rendered_post_draw=True movie_frame_offset=1\r?\n?',
                    '', 1)
                Assert-Task6 ($rewritten -cne $text) 'Boss telegraph mutation removes one copied runtime marker'
                [IO.File]::WriteAllText($path, $rewritten, [Text.UTF8Encoding]::new($false))
                Set-Task6ManifestHash $bundle.Manifest 'full_flow_log' $path
            } | Out-Null
        $script:EvidenceMarkers.Add("TASK6_COMPLETION_DISPLAY_RED_OBSERVED name=terrain-marker-delete " +
            "path=$(Join-Path $mutationRoot 'terrain-marker-delete-completion-display') " +
            "command=actual_child_verifier nonzero=True expected=completion_display_post_draw_required")
        $script:EvidenceMarkers.Add("TASK6_COMPLETION_CARD_VISUAL_RED_OBSERVED name=terrain-marker-delete " +
            "path=$(Join-Path $mutationRoot 'terrain-marker-delete-completion-card-visual') " +
            "command=actual_child_verifier nonzero=True expected=completion_card_pixels_required")
        $script:EvidenceMarkers.Add("TASK6_MODE_HUD_VISUAL_RED_OBSERVED name=terrain-marker-delete " +
            "path=$(Join-Path $mutationRoot 'terrain-marker-delete-mode-hud-visual') " +
            "command=actual_child_verifier nonzero=True expected=mode_hud_pixels_required")
        $script:EvidenceMarkers.Add("TASK6_SECOND_BOOST_VISUAL_RED_OBSERVED name=terrain-marker-delete " +
            "path=$(Join-Path $mutationRoot 'terrain-marker-delete-second-boost-visual') " +
            "command=actual_child_verifier nonzero=True expected=second_boost_rejection_pixels_required")
        $script:EvidenceMarkers.Add("TASK6_SECOND_BOOST_OFFSET_RED_OBSERVED name=terrain-marker-stale-second-boost-offset " +
            "path=$(Join-Path $mutationRoot 'terrain-marker-stale-second-boost-offset') " +
            "command=actual_child_verifier nonzero=True expected=second_boost_movie_offset_required")
        $script:EvidenceMarkers.Add("TASK6_BOSS_TELEGRAPH_DISPLAY_RED_OBSERVED name=terrain-marker-delete " +
            "path=$(Join-Path $mutationRoot 'terrain-marker-delete-boss-telegraph-display') " +
            "command=actual_child_verifier nonzero=True expected=boss_telegraph_post_draw_required")
        Add-Task6MutationEvidence -Name 'terrain-marker-delete' -Path (Join-Path $mutationRoot 'terrain-marker-delete') `
            -ExpectedLabel 'terrain_fall_v3_completion_mode_hud_boost_boss_display_and_card_pixels_required'

        Invoke-Task6BundleMutation -Name 'task11-input-marker-delete' -NoRecord -ExpectedLabel 'task11_input_marker_required' -Mutate {
            param($bundle)
            Replace-Task6ExactLine $bundle.Paths['task11_critical_log'] `
                'PROLOGUE_INPUT_PROOF_OK q=True e=True mouse=True slot1=True' 'REMOVED'
            Set-Task6ManifestHash $bundle.Manifest 'task11_critical_log' $bundle.Paths['task11_critical_log']
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'task11-input-marker-rename' -NoRecord -ExpectedLabel 'task11_marker_rename_required' -Mutate {
            param($bundle)
            Replace-Task6ExactLine $bundle.Paths['task11_critical_log'] `
                'PROLOGUE_INPUT_PROOF_OK q=True e=True mouse=True slot1=True' `
                'PROLOGUE_INPUT_PROOF_RENAMED q=True e=True mouse=True slot1=True'
            Set-Task6ManifestHash $bundle.Manifest 'task11_critical_log' $bundle.Paths['task11_critical_log']
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'task11-input-marker-duplicate' -NoRecord -ExpectedLabel 'task11_marker_exactly_once' -Mutate {
            param($bundle)
            Add-Content -LiteralPath $bundle.Paths['task11_critical_log'] -Value 'PROLOGUE_INPUT_PROOF_OK q=True e=True mouse=True slot1=True'
            Set-Task6ManifestHash $bundle.Manifest 'task11_critical_log' $bundle.Paths['task11_critical_log']
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'task11-input-marker-forge' -NoRecord -ExpectedLabel 'task11_marker_bound_log_required' -Mutate {
            param($bundle)
            Replace-Task6ExactLine $bundle.Paths['task11_critical_log'] `
                'PROLOGUE_INPUT_PROOF_OK q=True e=True mouse=True slot1=True' 'REMOVED'
            Add-Content -LiteralPath $bundle.Paths['full_flow_log'] -Value 'PROLOGUE_INPUT_PROOF_OK q=True e=True mouse=True slot1=True'
            Set-Task6ManifestHash $bundle.Manifest 'task11_critical_log' $bundle.Paths['task11_critical_log']
            Set-Task6ManifestHash $bundle.Manifest 'full_flow_log' $bundle.Paths['full_flow_log']
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'presentation-input-legacy-os-warp' -NoRecord -ExpectedLabel 'event_adapter_os_warp_false_required' -Mutate {
            param($bundle)
            Replace-Task6ExactLine $bundle.Paths['full_flow_log'] `
                'TASK6_INPUT_VIEWPORT_BASELINE_OK sampled=(0, 0) events=viewport+inputmap os_warp=False' `
                'TASK6_INPUT_VIEWPORT_BASELINE_OK sampled=(0, 0) events=os-warp+viewport+inputmap'
            Set-Task6ManifestHash $bundle.Manifest 'full_flow_log' $bundle.Paths['full_flow_log']
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'presentation-input-reticle-binding-false' -NoRecord -ExpectedLabel 'event_adapter_reticle_binding_required' -Mutate {
            param($bundle)
            Replace-Task6ExactLine $bundle.Paths['full_flow_log'] `
                'TASK6_INPUT_RETICLE_BINDING_OK sampled=(1120, 620) reticle=(1120, 620) adapter=True reticle_visible=True events=viewport+inputmap os_warp=False' `
                'TASK6_INPUT_RETICLE_BINDING_OK sampled=(1120, 620) reticle=(1120, 620) adapter=True reticle_visible=False events=viewport+inputmap os_warp=False'
            Set-Task6ManifestHash $bundle.Manifest 'full_flow_log' $bundle.Paths['full_flow_log']
        } | Out-Null
        Add-Task6MutationEvidence -Name 'task11-input-marker-delete' -Path (Join-Path $mutationRoot 'task11-input-marker-delete') `
            -ExpectedLabel 'task11_delete_rename_duplicate_forge_and_event_adapter_binding_rejected'
        Invoke-Task6BundleMutation -Name 'task11-input-mouse-false' -ExpectedLabel 'task11_input_mouse_true_required' -Mutate {
            param($bundle)
            Replace-Task6ExactLine $bundle.Paths['task11_critical_log'] `
                'PROLOGUE_INPUT_PROOF_OK q=True e=True mouse=True slot1=True' `
                'PROLOGUE_INPUT_PROOF_OK q=True e=True mouse=False slot1=True'
            Set-Task6ManifestHash $bundle.Manifest 'task11_critical_log' $bundle.Paths['task11_critical_log']
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'micro-scene-count' -ExpectedLabel 'fifteen_micro_scenes_required' -Mutate {
            param($bundle)
            Replace-Task6ExactLine $bundle.Paths['audit_log'] `
                'PROLOGUE_AUDIT_TOPOLOGY_OK sectors=5 micro_scenes=15 claw_roles=4' `
                'PROLOGUE_AUDIT_TOPOLOGY_OK sectors=5 micro_scenes=14 claw_roles=4'
            Set-Task6ManifestHash $bundle.Manifest 'audit_log' $bundle.Paths['audit_log']
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'claw-role-count' -ExpectedLabel 'four_claw_roles_required' -Mutate {
            param($bundle)
            Replace-Task6ExactLine $bundle.Paths['audit_log'] `
                'PROLOGUE_AUDIT_TOPOLOGY_OK sectors=5 micro_scenes=15 claw_roles=4' `
                'PROLOGUE_AUDIT_TOPOLOGY_OK sectors=5 micro_scenes=15 claw_roles=3'
            Set-Task6ManifestHash $bundle.Manifest 'audit_log' $bundle.Paths['audit_log']
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'mandatory-dash-stale-action' -NoRecord -ExpectedLabel 'inputmap_only_source_policy' -Mutate {
            param($bundle)
            $candidate = New-Task6SourceMutationProject 'mandatory-dash-stale-action'
            Add-Content -LiteralPath (Join-Path $candidate 'tests\presentation\ProloguePresentationTask6.cs') `
                -Value 'InputActions.Dash; FallenBellkeeperModelTests.Run();'
            $bundle.CandidateProject = $candidate
            Set-Task6BundleCandidateFingerprint $bundle | Out-Null
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'mandatory-dash-stale-action-human-boundary' -NoRecord -ExpectedLabel 'human_handoff_source_policy' -Mutate {
            param($bundle)
            $candidate = New-Task6SourceMutationProject 'mandatory-dash-stale-action-human-boundary'
            $validation = Join-Path $candidate 'docs\playtests\2026-08-29-terrain-grapple-dense-canyon-validation.md'
            $text = [IO.File]::ReadAllText($validation)
            Assert-Task6 ($text.Contains('No automatic HandsOnLauncher report')) 'canonical validation keeps human-report boundary'
            [IO.File]::WriteAllText($validation, $text.Replace('No automatic HandsOnLauncher report',
                'Automated HandsOnLauncher report'), [Text.UTF8Encoding]::new($false))
            $bundle.CandidateProject = $candidate
            Set-Task6BundleCandidateFingerprint $bundle | Out-Null
        } | Out-Null
        Add-Task6MutationEvidence -Name 'mandatory-dash-stale-action' `
            -Path (Join-Path $mutationRoot 'mandatory-dash-stale-action') -ExpectedLabel 'dash_direct_authority_and_human_boundary_rejected'

        Invoke-Task6BundleMutation -Name 'second-boost-reject' -ExpectedLabel 'second_boost_reject_required' -Mutate {
            param($bundle)
            Replace-Task6ExactLine $bundle.Paths['task11_critical_log'] `
                'PROLOGUE_ATTACHED_BOOST_OK cast_retained=True first=True second_rejected=True no_air_double_jump=True' `
                'PROLOGUE_ATTACHED_BOOST_OK cast_retained=True first=True second_rejected=False no_air_double_jump=True'
            Set-Task6ManifestHash $bundle.Manifest 'task11_critical_log' $bundle.Paths['task11_critical_log']
        } | Out-Null

        $basePresentation = New-Task6StandaloneEvidenceLog -Name 'base-presentation-order.log' `
            -SourcePath (Get-Task6BoundBaseEvidenceLog 'verify-prologue-attack-presentation.log')
        $basePresentationLines = @(Get-Content -LiteralPath $basePresentation)
        $first = [array]::FindIndex($basePresentationLines, [Predicate[string]]{
                param($line) $line -match '^PROLOGUE_PRESENTATION_STAGE=canyon_descent frame=\d+$'
            })
        $second = [array]::FindIndex($basePresentationLines, [Predicate[string]]{
                param($line) $line -match '^PROLOGUE_PRESENTATION_STAGE=ascent_one frame=\d+$'
            })
        Assert-Task6 ($first -ge 0 -and $second -gt $first) 'BASE Task6 presentation source has ordered InputMap traversal stages'
        $swap = $basePresentationLines[$first]; $basePresentationLines[$first] = $basePresentationLines[$second]; $basePresentationLines[$second] = $swap
        [IO.File]::WriteAllText($basePresentation, (($basePresentationLines -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
        Invoke-Task6BaseEvidenceGate -Name 'base-presentation-order' -EvidenceLog $basePresentation -Kind Presentation -ExpectFailure | Out-Null
        Add-Task6MutationEvidence -Name 'base-presentation-order' -Path $basePresentation -ExpectedLabel 'base_ordered_inputmap_traversal_required'

        $baseHammer = New-Task6StandaloneEvidenceLog -Name 'boss-hammer-throw-delete.log' `
            -SourcePath (Get-Task6BoundBaseEvidenceLog 'verify-prologue-base-attack-presentation.log')
        Replace-Task6ExactLine $baseHammer 'PROLOGUE_ATTACK_SEQUENCE_OK Boss/BossHammerThrow' 'REMOVED'
        Invoke-Task6BaseEvidenceGate -Name 'boss-hammer-throw-delete' -EvidenceLog $baseHammer -Kind AttackPresentation -ExpectFailure | Out-Null
        Add-Task6MutationEvidence -Name 'boss-hammer-throw-delete' -Path $baseHammer -ExpectedLabel 'base_boss_hammer_throw_required'

        $baseSmoke = New-Task6StandaloneEvidenceLog -Name 'thread-death-cleanup-delete.log' `
            -SourcePath (Get-Task6BoundBaseEvidenceLog 'verify-prologue-smoke.log')
        Replace-Task6ExactLine $baseSmoke 'TASK6_CLAW_DEATH_CLEANUP_OK physical=True idle=True generated_main=True' 'REMOVED'
        Invoke-Task6BaseEvidenceGate -Name 'thread-death-cleanup-delete' -EvidenceLog $baseSmoke -Kind CriticalSmoke -ExpectFailure | Out-Null
        Add-Task6MutationEvidence -Name 'thread-death-cleanup-delete' -Path $baseSmoke -ExpectedLabel 'base_claw_death_cleanup_required'

        $baseBudget = New-Task6StandaloneEvidenceLog -Name 'budget-exceeded.log' `
            -SourcePath (Get-Task6BoundBaseEvidenceLog 'verify-prologue-smoke.log')
        $budgetText = [IO.File]::ReadAllText($baseBudget)
        $budgetChanged = [regex]::Replace($budgetText,
            'PROLOGUE_FRAME_BUDGET_OK used=\d+ total=12000 headroom_percent=\d+(?:\.\d+)?',
            'PROLOGUE_FRAME_BUDGET_OK used=10801 total=12000 headroom_percent=9.99', 1)
        Assert-Task6 ($budgetChanged -cne $budgetText) 'BASE budget mutation rewrites a real smoke log'
        [IO.File]::WriteAllText($baseBudget, $budgetChanged, [Text.UTF8Encoding]::new($false))
        Invoke-Task6BaseEvidenceGate -Name 'budget-exceeded' -EvidenceLog $baseBudget -Kind CriticalSmoke -ExpectFailure | Out-Null
        Add-Task6MutationEvidence -Name 'budget-exceeded' -Path $baseBudget -ExpectedLabel 'base_frame_budget_headroom_required'

        foreach ($failureVariant in @('ERROR: injected Task6 mutation', 'ObjectDB instances were leaked', 'fatal injected Task6 mutation')) {
            $failureName = 'fatal-error-leak-' + (($failureVariant -replace '[^A-Za-z]+', '-').Trim('-').ToLowerInvariant())
            Invoke-Task6BundleMutation -Name $failureName -NoRecord -ExpectedLabel 'engine_failure_rejected' -Mutate {
                param($bundle)
                Add-Content -LiteralPath $bundle.Paths['full_flow_console'] -Value $failureVariant
                Set-Task6ManifestHash $bundle.Manifest 'full_flow_console' $bundle.Paths['full_flow_console']
            } | Out-Null
        }
        Add-Task6MutationEvidence -Name 'fatal-error-leak' -Path (Join-Path $mutationRoot 'fatal-error-leak-error-injected-task-mutation') `
            -ExpectedLabel 'error_leak_and_fatal_rejected'

        Invoke-Task6BundleMutation -Name 'avi-header-tamper-container' -NoRecord -ExpectedLabel 'streaming_avi_header_required' -Mutate {
            param($bundle)
            $badAvi = Join-Path $bundle.Root 'tampered-full-flow.avi'
            [IO.File]::WriteAllBytes($badAvi, [byte[]](0x52, 0x49, 0x46, 0x46))
            Set-Task6ManifestValue $bundle.Manifest 'full_flow_avi' $badAvi
            Set-Task6ManifestHash $bundle.Manifest 'full_flow_avi' $badAvi
        } | Out-Null
        Invoke-Task6BundleMutation -Name 'avi-header-tamper-timeline' -NoRecord -ExpectedLabel 'full_flow_timeline_required' -Mutate {
            param($bundle)
            $path = $bundle.Paths['full_flow_log']
            $text = [IO.File]::ReadAllText($path)
            $attached = [regex]::Match($text,
                '(?m)^PROLOGUE_PRESENTATION_STAGE=boost_attached frame=(?<frame>\d+)$')
            Assert-Task6 $attached.Success 'canonical full-flow log exposes the attached-boost frame'
            [long]$tooShort = [long]$attached.Groups['frame'].Value + 17L
            $rewritten = [regex]::Replace($text,
                '(?m)^PROLOGUE_PRESENTATION_STAGE=boost_start frame=\d+$',
                "PROLOGUE_PRESENTATION_STAGE=boost_start frame=$tooShort", 1)
            Assert-Task6 ($rewritten -cne $text) 'timeline mutation rewrites a copied runtime stage log'
            [IO.File]::WriteAllText($path, $rewritten, [Text.UTF8Encoding]::new($false))
            Set-Task6ManifestHash $bundle.Manifest 'full_flow_log' $path
        } | Out-Null
        $frozenExtractorLog = Invoke-Task6FrozenAviExtractorRed
        Add-Task6MutationEvidence -Name 'avi-header-tamper' -Path (Join-Path $mutationRoot 'avi-header-tamper-timeline') `
            -ExpectedLabel 'streaming_avi_header_full_flow_timeline_and_decoded_frozen_movie_required'
        Invoke-Task6BundleMutation -Name 'manifest-hash-tamper' -ExpectedLabel 'manifest_hash_binding_required' -Mutate {
            param($bundle)
            Set-Task6ManifestValue $bundle.Manifest 'full_flow_log_sha256' ('0' * 64)
        } | Out-Null

        $terminalCanonical = New-Task6StandaloneEvidenceLog -Name 'verify-terminal-canonical.log' `
            -SourcePath (Join-Path $mutationRoot 'canonical.verify.log')
        [IO.File]::WriteAllText($terminalCanonical, "PROLOGUE_VERIFY_OK root=isolated candidate=test`n", [Text.UTF8Encoding]::new($false))
        Invoke-Task6VerifierTerminalGate -Name 'verify-terminal-canonical' -TerminalLog $terminalCanonical | Out-Null
        $terminalMissing = New-Task6StandaloneEvidenceLog -Name 'verify-terminal-missing.log' -SourcePath $terminalCanonical
        [IO.File]::WriteAllText($terminalMissing, "capture summary only`n", [Text.UTF8Encoding]::new($false))
        Invoke-Task6VerifierTerminalGate -Name 'verify-terminal-missing' -TerminalLog $terminalMissing -ExpectFailure | Out-Null
        $terminalDuplicate = New-Task6StandaloneEvidenceLog -Name 'verify-terminal-duplicate.log' -SourcePath $terminalCanonical
        Add-Content -LiteralPath $terminalDuplicate -Value 'PROLOGUE_VERIFY_OK root=isolated candidate=test'
        Invoke-Task6VerifierTerminalGate -Name 'verify-terminal-duplicate' -TerminalLog $terminalDuplicate -ExpectFailure | Out-Null
        Add-Task6MutationEvidence -Name 'verify-terminal-duplicate' -Path $terminalDuplicate `
            -ExpectedLabel 'verify_terminal_missing_and_duplicate_rejected'
    }
}

Describe 'Task 6 decoded transient visual binding' {
    It 'requires postdraw HUD and second-boost-rejection visual surfaces' {
        # This is intentionally a decoded-evidence test, not a source-string assertion.  The
        # named PNGs are the exact runtime-bound AVI frames selected by the extractor, so a
        # pre-draw transition frame or the rope-still-visible rejection frame must fail here.
        $values = Get-Task6ManifestValues (Join-Path $captureRoot 'capture-manifest.txt')
        $keyframeManifest = [string]$values['full_flow_keyframe_manifest']
        Assert-Task6 (Test-Path -LiteralPath $keyframeManifest -PathType Leaf) 'full-flow keyframe manifest exists'
        $keyframeRoot = Split-Path -Parent $keyframeManifest
        $lines = @(Get-Content -LiteralPath $keyframeManifest)
        $mode = @($lines | Where-Object { $_ -match '^mode_hud\|avi_frame=\d+\|png=(?<png>[^|]+)\|sha256=[A-Fa-f0-9]{64}$' })
        $rejected = @($lines | Where-Object { $_ -match '^boost_second_rejected\|avi_frame=\d+\|png=(?<png>[^|]+)\|sha256=[A-Fa-f0-9]{64}$' })
        Assert-Task6 ($mode.Count -eq 1) 'keyframe manifest has one mode-HUD PNG'
        Assert-Task6 ($rejected.Count -eq 1) 'keyframe manifest has one second-rejected PNG'
        $modePath = Join-Path $keyframeRoot ([regex]::Match($mode[0], 'png=(?<png>[^|]+)').Groups['png'].Value)
        $rejectedPath = Join-Path $keyframeRoot ([regex]::Match($rejected[0], 'png=(?<png>[^|]+)').Groups['png'].Value)
        $modeVisual = Assert-Task6ModeHudVisual -Path $modePath
        $rejectionVisual = Assert-Task6SecondBoostRejectedVisual -Path $rejectedPath
        Assert-Task6 ($modeVisual.CreamSamples -ge 500) 'mode HUD has readable formal surface pixels'
        Assert-Task6 ($rejectionVisual.PurpleRingSamples -ge 100) 'second boost rejection has visible rejection ring pixels'
    }
}

if ([string]::IsNullOrWhiteSpace($env:KNIGHT_TASK6_FOCUSED_TDD)) {
    if (-not $script:CanonicalAccepted -or $script:MutationCount -ne 15 -or $script:HostProbeCount -ne 2) {
        throw "Task 6 Pester must not certify incomplete real evidence mutations; canonical=$script:CanonicalAccepted mutations=$script:MutationCount hosts=$script:HostProbeCount"
    }
    $script:EvidenceMarkers.Add('TASK6_HOST_COMPAT_OK windows_powershell=True pwsh=True runtime_fingerprint=True')
    $script:EvidenceMarkers.Add('TASK6_PESTER_CONTRACT_OK canonical=1 mutations=15 host_probes=2 human_gate=open')
    if (-not [string]::IsNullOrWhiteSpace($evidenceLog)) {
        if (Test-Path -LiteralPath $evidenceLog) { throw "Task 6 Pester evidence log must be fresh: $evidenceLog" }
        [IO.File]::WriteAllText($evidenceLog, (($script:EvidenceMarkers -join "`n") + "`n"),
            [Text.UTF8Encoding]::new($false))
    }
    Write-Output 'TASK6_PESTER_CONTRACT_OK canonical=1 mutations=15 host_probes=2 human_gate=open'
}
